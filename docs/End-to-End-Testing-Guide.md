# JobEngine — End-to-End Testing Guide

All requests go through the **API Gateway on port 8080**.  
Direct service ports are listed for debugging only.

| Service | Via Gateway | Direct (docker) |
|---|---|---|
| Gateway | `http://localhost:8080` | — |
| Auth Service | `/api/v1/auth/*` | `http://localhost:8082` |
| Job Service | `/api/v1/jobs/*` | `http://localhost:8081` |
| Execution Service | `/api/v1/execute` | `http://localhost:8084` |
| RabbitMQ UI | — | `http://localhost:15672` (guest/guest) |

---

## Prerequisites

```bash
docker compose up --build -d
```

Wait ~10 seconds for all services to be healthy before sending requests.

---

## Step 1 — Register a Tenant

Creates a tenant + admin user in one call.

```http
POST http://localhost:8080/api/v1/auth/register
Content-Type: application/json

{
  "tenantName": "Acme Corp",
  "slug": "acme",
  "adminEmail": "admin@acme.com",
  "adminPassword": "Password123!"
}
```

**Expected response: `201 Created`**

```json
{
  "tenantId": "<tenant-guid>",
  "userId": "<user-guid>",
  "token": "<jwt-token>"
}
```

Save `tenantId` and `token` — used in all subsequent requests.

---

## Step 2 — Login

```http
POST http://localhost:8080/api/v1/auth/login
Content-Type: application/json

{
  "email": "admin@acme.com",
  "password": "Password123!",
  "tenantSlug": "acme"
}
```

**Expected response: `200 OK`**

```json
{
  "token": "<jwt-token>",
  "expiresAt": "..."
}
```

Save the `token` — include it as `Authorization: Bearer <token>` in all job requests.

---

## Step 3 — Submit a Job (with optional webhook)

```http
POST http://localhost:8080/api/v1/jobs
Content-Type: application/json
Authorization: Bearer <token>

{
  "type": "sample-job",
  "payload": "{\"input\": \"hello world\"}",
  "priority": 0,
  "maxAttempts": 3,
  "webhookUrl": "https://webhook.site/<your-id>",
  "webhookSecret": "my-test-secret"
}
```

> `webhookUrl` and `webhookSecret` are optional. Get a free test URL at https://webhook.site.

**Expected response: `201 Created`**

```json
"<job-guid>"
```

Save the `jobId`.

---

## Step 4 — Poll Job Status

```http
GET http://localhost:8080/api/v1/jobs/<job-guid>
Authorization: Bearer <token>
```

**Expected status progression:**

| Status | Meaning |
|---|---|
| `Pending` | Saved to DB, not yet queued |
| `Queued` | Published to RabbitMQ |
| `Running` | Worker claimed it |
| `Completed` | Execution Service returned success |
| `Failed` / `Retrying` | Execution failed, will retry |
| `DeadLetter` | All retries exhausted |

Poll every 2–3 seconds until `Completed` or `DeadLetter`.

---

## Step 5 — Verify Webhook Delivery (if configured)

If you provided a `webhookUrl` in Step 3, go to https://webhook.site and check for an incoming `POST` request.

**Expected request on webhook.site:**

```
POST /your-id
X-JobEngine-Event: job.completed
X-JobEngine-Signature: sha256=<hmac-hex>
X-JobEngine-Timestamp: <unix-timestamp>
Content-Type: application/json

{
  "jobId": "<job-guid>",
  "tenantId": "<tenant-guid>",
  "result": "...",
  "completedAt": "...",
  "webhookUrl": "...",
  "webhookSecret": "..."
}
```

**Verify the signature (optional):**  
Compute `HMAC-SHA256(body, webhookSecret)` and compare with the `X-JobEngine-Signature` header value (strip the `sha256=` prefix). They must match.

---

## Step 6 — List All Jobs

```http
GET http://localhost:8080/api/v1/jobs
Authorization: Bearer <token>
```

Returns all jobs for your tenant. Tenant isolation is enforced — you cannot see another tenant's jobs.

---

## Step 7 — Create an API Key (optional)

```http
POST http://localhost:8080/api/v1/auth/tenants/<tenant-guid>/keys
Content-Type: application/json
Authorization: Bearer <token>

{
  "name": "production-key"
}
```

**Expected response: `201 Created`**

```json
{
  "id": "<key-guid>",
  "key": "<raw-api-key>"
}
```

> The raw key is shown **once only** — store it securely. The stored value is hashed.

---

## Debugging

### RabbitMQ queues
Open `http://localhost:15672` → Login: `guest` / `guest`  
Check the `job-submitted` queue has consumers. After a job is processed, check `JobCompletedEvent` / `JobFailedEvent` queues.

### View logs per service
```bash
docker compose logs -f worker-service
docker compose logs -f notification-service
docker compose logs -f job-service
```

### Check DB directly
```bash
docker compose exec postgres psql -U postgres -d je_jobs -c "SELECT id, status, webhook_url, created_at FROM \"Jobs\" ORDER BY created_at DESC LIMIT 10;"
```

---

## Azure Production

Replace `http://localhost:8080` with your gateway external URL:

```
https://ca-jobengine-gateway.<env>.centralindia.azurecontainerapps.io
```

All other steps are identical — the JWT and routing behaviour is the same.
