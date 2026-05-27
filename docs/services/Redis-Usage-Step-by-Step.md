# Redis In JobEngine: What It Does and How It Works

This document explains where Redis is used in the current system and what happens step by step.

## Quick Summary

Redis is used for three main purposes:

1. Worker idempotency: avoid processing the same job message more than once.
2. Worker distributed locking: prevent multiple workers from running the same job at the same time.
3. Auth token cache: speed up JWT validation by caching claim data.

## Where Redis Is Wired

- WorkerService creates one shared Redis connection (`IConnectionMultiplexer`) and one shared Redis database client (`IDatabase`).
- WorkerService uses `RedisLockManager` as the distributed lock implementation.
- AuthService creates an optional Redis connection for `JwtTokenService` (service still starts if Redis is unavailable).
- JobService also registers a Redis connection in DI, but there is no active runtime usage in `TenantQuotaService` yet.

## Step By Step: WorkerService Flow

### Step 0: Startup wiring

At startup, WorkerService:

1. Reads `Redis__Connection` from configuration.
2. Connects to Redis through `ConnectionMultiplexer.Connect(...)`.
3. Registers `IDatabase` from the multiplexer.
4. Registers `IDistributedLockManager` with `RedisLockManager`.

Result: each consumer can use Redis for idempotency and locks.

### Step 1: Receive `JobSubmittedEvent`

When a job message arrives, `JobSubmittedConsumer` runs.

### Step 2: Idempotency gate (first Redis write)

Consumer writes key:

- Key: `job:processed:{JobId}`
- Value: worker id (machine + short GUID)
- TTL: 24 hours
- Write mode: `SET ... NX` (only set if key does not exist)

Behavior:

- If set succeeds: message is considered first-time processing, continue.
- If set fails: this is a duplicate delivery, log warning and exit without throwing.

Why: RabbitMQ can redeliver messages; this avoids duplicate work.

### Step 3: Distributed lock (second Redis write)

Consumer requests lock via `RedisLockManager.TryAcquireAsync`.

The lock manager writes:

- Key format in manager: `lock:{resource}`
- Resource passed by consumer: `job:lock:{JobId}`
- Final Redis key: `lock:job:lock:{JobId}`
- Value: lock owner instance id
- TTL: 5 minutes
- Write mode: `SET ... NX` (atomic lock acquire)

Behavior:

- If acquired: proceed.
- If not acquired: another worker holds lock, log warning and return.

Why if idempotency already exists?

- Idempotency blocks duplicates over time.
- Lock prevents race conditions in edge timing windows while workers compete.

### Step 4: Lock keep-alive while job runs

`RedisLock` starts a timer that renews key expiry at 2/3 of TTL.

- TTL = 5 minutes
- Renew interval is about 3 minutes 20 seconds

Why: long-running jobs should not lose lock ownership due to TTL expiration.

### Step 5: Safe lock release

On dispose, lock executes Lua script:

- Read current key value.
- Delete key only if value matches this lock owner.

Why: prevents worker A from deleting worker B's lock if ownership changed after expiry/reacquire.

### Step 6: Continue job pipeline

With idempotency + lock in place, worker then:

1. Marks job `Running` in Postgres.
2. Calls ExecutionService over HTTP.
3. Marks job Completed/Failed.
4. Publishes result event.

Redis is only for concurrency safety, not primary job state.

## Step By Step: AuthService JWT Cache Flow

### Step 0: Startup wiring

`AuthService.Infrastructure` creates `JwtTokenService` with optional Redis:

1. Reads `Redis__Connection` (or `Redis:Connection`).
2. Tries to connect.
3. If connection fails, continues with `null` Redis (no crash).

### Step 1: On token generation

After creating JWT, service caches claims in Redis:

- Key: `jwt:{sha256(token)}`
- Value: serialized claim list
- TTL: token expiry remaining time

### Step 2: On token validation

Validation path:

1. Try Redis first with `jwt:{sha256(token)}`.
2. If present, rebuild `ClaimsPrincipal` from cache and return.
3. If absent, validate JWT cryptographically.
4. Cache validated claims back to Redis with token TTL.

Result: repeated validations can avoid full JWT re-validation cost.

## JobService Note

`JobService.Infrastructure` registers `IConnectionMultiplexer` and requires `Redis__Connection`.

Current state:

- `TenantQuotaService` does not read/write Redis.
- Redis registration appears to be preparatory/future-facing in this service.

## Redis Keys Used Today

- `job:processed:{JobId}` (idempotency marker, 24h)
- `lock:job:lock:{JobId}` (distributed lock, 5m with renewal)
- `jwt:{sha256(token)}` (Auth token claim cache, token TTL)

## Why Redis Fits This Role

- Atomic `SET NX` is good for simple distributed coordination.
- TTL makes stale lock/idempotency cleanup automatic.
- Very low-latency lookups are ideal for hot-path checks (consumer + auth).

## Operational Behavior If Redis Is Down

- WorkerService: Redis is required at startup; connection is not optional in current setup.
- AuthService: Redis is optional; auth still works, only cache acceleration is lost.
- JobService: startup currently expects Redis connection to be configured.
