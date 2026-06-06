# JobService — Flows, State Machine, and Testing Guide

## Overview
JobService is the authoritative write-side for jobs: it accepts submissions quickly, persists a canonical record, publishes an event for workers to execute, and enforces safe state transitions and tenant isolation.

Why it exists:
- Submission and execution are separated so clients get fast responses.
- The JobService is the single source of truth for job state and history.
- Durable persistence + messaging allows retries and recovery without client involvement.

---

## Key pieces
- `Job` entity: fields include `Id, TenantId, Type, Payload, Status, Attempt, MaxAttempts, Priority, ScheduledAt, CreatedAt, StartedAt, CompletedAt, Error, Result, Logs`.
- Database: `je_jobs` / `Jobs` table with composite index on `(tenant_id, status, scheduled_at)` for worker polling performance.
- State machine methods on `Job`: `MarkQueued()`, `MarkRunning(workerId)`, `MarkCompleted()`, `MarkFailed(error)`, `RequeueForRetry()` — invalid transitions throw.
- Messaging: publishes `JobSubmittedEvent` (shared contract) so WorkerService consumes the same contract.
- Tenant scoping: JWT contains `tenant_id` (or gateway sets `X-Tenant-Id`) → `TenantContextMiddleware` populates `ITenantContext` → `JobsDbContext` applies a global query filter to automatically scope queries to the tenant.
- CQRS: Commands handle writes (MediatR + FluentValidation + pipeline), Queries use `AsNoTracking()` for fast reads.

---

## State machine (safety model)
1. Pending — job created, not yet pushed to queue (used for scheduled jobs).
2. Queued — saved to PostgreSQL then published to RabbitMQ. DB write happens first — if RabbitMQ publish fails, the job still exists and can be requeued.
3. Running — a Worker claimed it and is processing it right now.
4. Completed / Failed / Retrying / DeadLetter — terminal or near-terminal states. Invalid transitions throw immediately (e.g., cannot go from Completed back to Running).

The state machine enforces invariants and makes transitions explicit and auditable.

---

## Write flow: SubmitJobCommand (detailed)
1. Client calls `POST /api/v1/jobs` with job details and tenant-scoped auth.
2. Controller constructs `SubmitJobCommand` and sends it to MediatR.
3. Validation behavior (FluentValidation) runs first; if validation fails the command is rejected.
4. `ITenantQuotaService.EnforceAsync(tenantId)` is called to ensure tenant hasn't exceeded configured job quota.
5. `Job.Create(...)` constructs the domain aggregate with `Status = Pending`.
6. `IJobRepository.AddAsync(job)` + `IUnitOfWork.SaveChangesAsync()` persists the row (DB is the source of truth).
7. `IEventPublisher.PublishAsync(JobSubmittedEvent)` publishes the submission message for workers.
8. `job.MarkQueued()` and `SaveChangesAsync()` persist the queued status.

Rationale: persist-first-then-publish (outbox-ish) guarantees the job exists even if messaging fails.

---

## Worker flow (consume + execute)
1. Worker polls the DB for the next queued job with `GetNextQueuedAsync()` using `FOR UPDATE SKIP LOCKED` so multiple workers don't take the same job.
2. Worker calls `job.MarkRunning(workerId)` and saves; `Attempt` increments.
3. Worker processes the job payload.
   - On success: `MarkCompleted(result)` → save → optionally publish `JobCompletedEvent`.
   - On failure: `MarkFailed(error)` sets `Retrying` or `DeadLetter` depending on attempts → save; if `Retrying`, the job will be requeued (either by worker logic or a scheduler) after backoff.

This protects against duplicate work and ensures retries are bounded by `MaxAttempts`.

---

## Read flow: GetJobQuery and ListJobsQuery
- Queries are implemented with `AsNoTracking()` (no EF change-tracking) for ~30% faster reads and lower memory usage.
- Global tenant query filter adds `WHERE tenant_id = ?` automatically to every query.
- Attempting to read another tenant's job (different `tenant_id`) returns 404 / no results.

---

## Quota, RBAC, and Tenant Isolation
- Quota: `ITenantQuotaService` enforces per-tenant active-job limits before new job creation.
- RBAC: `role` claim in JWT (e.g., `admin`) used by policies for tenant-level operations (creating API keys, viewing tenant details).
- Tenant isolation: enforced both at token/HTTP middleware level and in DB via EF global filters.

---

## Database considerations
- Table: `Jobs` with fields matching the `Job` aggregate.
- Composite index: `(tenant_id, status, scheduled_at)` named e.g. `idx_jobs_tenant_status_scheduled` to accelerate worker polls (filter by `status='Queued'` and `scheduled_at <= now()`).
- Use native `jsonb` for the `Payload` column for efficient storage and querying.

---

## End-to-end testing recipes
Below are step-by-step checks and commands to verify all components work together.

### 1) Build & Migrations
- Build the solution or JobService projects:

```powershell
dotnet build services\JobService\src\JobService.Api\JobService.Api.csproj
```

- Apply EF migrations (dev): Program.cs auto-applies migrations in Development; or run manually:

```powershell
dotnet ef database update --project services\JobService\src\JobService.Infrastructure\JobService.Infrastructure.csproj --startup-project services\JobService\src\JobService.Api\JobService.Api.csproj --context JobsDbContext
```

### 2) Start infra (Postgres, RabbitMQ, Redis)
- Use repository `docker-compose.yml` which has Postgres, RabbitMQ, Redis, etc., or start services locally.

```powershell
docker-compose up -d postgres rabbitmq redis
```

### 3) Start services
- JobService API:
```powershell
dotnet run --project services\JobService\src\JobService.Api\JobService.Api.csproj
```
- WorkerService (consumes JobSubmittedEvent and executes jobs):
```powershell
dotnet run --project services\WorkerService\src\WorkerService\WorkerService.csproj
```

### 4) Submit a job (happy path)
- Acquire tenant-scoped JWT (from Auth service) or set `X-Tenant-Id` header from gateway.
- Example curl (replace token/host):

```bash
curl -X POST http://localhost:5XXX/api/v1/jobs \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"type":"email","payload":"{\"to\":\"me@example.com\",\"body\":\"hi\"}"}'
```

Expected:
- HTTP 201 with job id.
- DB row created with `Status = Pending` then `Queued`.
- Worker consumes and transitions to `Running` → `Completed`.

### 5) Verify DB and index
- Query job by id in psql:
```sql
SELECT * FROM "Jobs" WHERE "Id" = '<job-id>';
```
- Check index exists:
```
\d+ "Jobs"
```
Look for `idx_jobs_tenant_status_scheduled`.

### 6) Verify messaging (RabbitMQ UI)
- Open RabbitMQ management UI (http://localhost:15672) and check exchanges/queues and consumer counts.

### 7) Retry & Dead-letter testing
- Configure Worker to deliberately fail for a specific `Type`.
- Submit a job that triggers failure and observe `Attempt` increments until `DeadLetter` after `MaxAttempts`.
- Confirm `Status` transitions and `Error` field updates.

### 8) Tenant isolation test
- Submit job as Tenant A.
- Try `GET /api/v1/jobs/{id}` with a token for Tenant B — should return 404.
- This proves global query filter and middleware tenant context work.

### 9) Quota test
- Set `Tenants:DefaultJobQuota` small (e.g., 1) in configuration.
- Submit jobs beyond the quota and confirm `SubmitJobCommand` throws and request is rejected.

### 10) Unit & integration tests recommendations
- Unit tests:
  - Domain `Job` transitions (valid/invalid transitions raise/accept).
  - `SubmitJobHandler` with mocked `IJobRepository`, `IUnitOfWork`, `IEventPublisher`, `ITenantQuotaService`.
- Integration tests:
  - Start ephemeral Postgres & RabbitMQ via docker-compose and run tests that POST jobs and assert DB + messaging side effects.

Run unit tests (example):
```powershell
dotnet test tests\JobService.Tests\JobService.Tests.csproj
```

---

## Smoke checklist
- [ ] API accepts submission and returns 201
- [ ] Job row created in DB
- [ ] `Status` transitions Pending→Queued→Running→Completed or Retrying→DeadLetter
- [ ] `JobSubmittedEvent` published and Worker consumes it
- [ ] Retries and `Attempt` behave as expected
- [ ] Tenant isolation: tenant B cannot read tenant A jobs
- [ ] Composite index exists and worker polling uses it

---

## Next steps (optional)
- Add automated integration tests that run against docker-compose environment.
- Implement an explicit outbox table or transactional outbox pattern if you require strict publish guarantees without relying on immediate publish-after-save logic.
- Expose metrics for queue depth, processing latency, and retry counts.

---

This document lives in the repository so you can refer to it while implementing tests or debugging flows.
