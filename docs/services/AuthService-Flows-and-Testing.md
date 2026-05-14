# AuthService — Flows, Claims, and Testing Guide

## Overview
AuthService is responsible for tenant and user lifecycle, authentication (JWT), and API key management. It provides the tokens and credentials other services rely on to enforce tenant scoping and RBAC.

Primary goals:
- Issue signed JWTs that carry `tenant_id`, `role`, and `tenant_slug` claims.
- Issue API keys (raw shown once) and persist only their SHA‑256 hash.
- Provide tenant management endpoints (register, login, create API keys, tenant lookup).
- Optionally cache validated token claims in Redis to speed validation and enable quick revocation patterns.

---

## Key concepts & claims
- `tenant_id` (GUID): authoritative tenant identifier used by downstream services for scoping.
- `tenant_slug`: human-friendly tenant identifier.
- `role`: RBAC claim (e.g., `admin`, `user`) used for authorization checks.
- Standard claims: `sub`, `exp`, `iat`, etc.

Tokens are signed with a symmetric key (HMAC). Recipients verify signature and expiry to trust claims without contacting AuthService.

---

## Endpoints (examples)
- `POST /api/v1/auth/register` — create tenant + admin user, returns access + refresh tokens.
- `POST /api/v1/auth/login` — authenticate user, returns access + refresh tokens.
- `POST /api/v1/auth/tenants/{tenantId}/keys` — create API key (returns raw key once).
- `GET /api/v1/auth/tenants/{tenantId}` — tenant details (name, admin email).

---

## Token issuance & validation flow
1. Register/Login: credentials validated → `IJwtTokenService.GenerateToken(user, tenant)` returns `(access, refresh, expiry)`.
2. Access tokens are short-lived; refresh tokens are longer-lived and used to obtain new access tokens.
3. API keys are raw tokens given once. Auth stores `SHA256(raw)` in the `ApiKeys` table and returns the raw value only in the response.
4. Validation: services validate signature and expiry locally. Optionally, `JwtTokenService` caches validated claims in Redis under `jwt:{sha256(token)}` for quick lookups and revocation checks.

Security practices used:
- Store only hashes for API keys (SHA‑256). Raw key is never persisted.
- Short access token lifetimes with refresh flow to minimize exposure.
- Always use HTTPS in production.
- Support key rotation and cache invalidation for revocation.

---

## Database: `je_auth`
Tables:
- `Tenants` (id, slug, name, quota, created_at)
- `Users` (id, tenant_id, email, password_hash, role, created_at)
- `ApiKeys` (id, tenant_id, key_hash, created_at, description)

API keys: raw key hashed with SHA‑256; DB stores only the hash and metadata.

---

## How downstream services use Auth tokens
- Downstream services extract `tenant_id` claim from access token and use it to scope data (global query filters/tenant context).
- `role` claim used for policy checks (e.g., only `admin` can create API keys).
- For machine-to-machine calls, API keys can be used instead of JWTs; gateway validates and forwards `X-Tenant-Id`.

---

## Testing guide
Follow these steps to exercise AuthService end-to-end locally.

### 1) Build & migrations
- Build project:
```powershell
dotnet build services\AuthService\src\AuthService.Api\AuthService.Api.csproj
```
- Apply EF migrations (Program.cs auto-applies in Development). Or run manually:
```powershell
dotnet ef database update --project services\AuthService\src\AuthService.Infrastructure\AuthService.Infrastructure.csproj --startup-project services\AuthService\src\AuthService.Api\AuthService.Api.csproj --context AuthDbContext
```

### 2) Start infra dependencies
- Start Postgres and Redis (docker-compose included) or run local instances:
```powershell
docker-compose up -d postgres redis
```

### 3) Run AuthService
```powershell
dotnet run --project services\AuthService\src\AuthService.Api\AuthService.Api.csproj
```

### 4) Register a tenant + admin
Example request (replace host/port):
```bash
curl -X POST http://localhost:5019/api/v1/auth/register \
  -H "Content-Type: application/json" \
  -d '{"tenantName":"acme","adminEmail":"admin@acme.test","password":"P@ssw0rd"}'
```
Expected:
- 201 Created with JSON containing `tenantId`, `accessToken`, `refreshToken`, `expiry`.
- DB rows: `Tenants` and `Users` created; `Users.password_hash` stores hashed password (e.g., BCrypt).

### 5) Login
```bash
curl -X POST http://localhost:5019/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@acme.test","password":"P@ssw0rd","tenantSlug":"acme"}'
```
Expected: returns new `accessToken` and `refreshToken`.

### 6) Inspect JWT payload
- Use jwt.io or a local JWT decode command to verify the payload includes `tenant_id`, `role`, `tenant_slug`, `exp`.

### 7) Create API key
- Use admin token to create an API key for the tenant:
```bash
curl -X POST http://localhost:5019/api/v1/auth/tenants/<tenantId>/keys \
  -H "Authorization: Bearer <ADMIN_ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"description":"worker-key"}'
```
Expected:
- Response contains `key` (raw) — shown only once.
- DB `ApiKeys.key_hash` contains SHA‑256 of that raw value.

To verify hash:
- Compute SHA‑256 of the returned key and compare to DB value.

### 8) Token validation & Redis cache
- When a downstream service sends `Authorization: Bearer <token>`, service verifies signature and expiry.
- If Redis is enabled, AuthService caches validated token claims under `jwt:{sha256(token)}` so subsequent validations can use the cache.
- Test: validate token once, then delete Redis key and confirm validation still works (signature-based) but revocation requires cache invalidation.

### 9) Get tenant details
- `GET /api/v1/auth/tenants/{tenantId}` should return tenant `name` and the admin `email`.
- Test with admin token and non-admin token to validate RBAC rules (if applicable).

### 10) Edge cases & security tests
- Invalid password: login returns 401.
- Expired token: service rejects with 401.
- API key reuse: calling the API key creation endpoint again generates a new raw key and stores a new hash.
- Revocation: remove/mark API key hash in DB or clear Redis cache entry to simulate revocation and test rejected calls.

### 11) Unit & integration tests suggestions
- Unit tests for `JwtTokenService.GenerateToken()` and `ValidateToken()` covering claims and expiry.
- Unit tests for `ApiKey` creation: ensure stored hash matches SHA‑256 of returned raw key.
- Integration tests: run AuthService with ephemeral Postgres/Redis and exercise register/login/create API key flows.

---

## Troubleshooting & tips
- If startup fails due to Redis connection parsing, AuthService DI is tolerant — ensure `Redis__Connection` is correct or leave it empty to disable caching.
- If migrations fail, check `Auth` connection string in configuration and the mounted `init-databases.sh` script in docker-compose.
- Keep access tokens short-lived and test refresh flow using `/token/refresh` (if implemented).

---

## Quick smoke checklist
- [ ] `POST /auth/register` creates tenant and admin, returns tokens
- [ ] `POST /auth/login` issues access + refresh tokens
- [ ] `GET /auth/tenants/{id}` returns tenant name and admin email
- [ ] `POST /auth/tenants/{id}/keys` returns raw key once; DB stores only SHA‑256 hash
- [ ] JWT includes `tenant_id`, `role`, `tenant_slug` and is verifiable with the signing key
- [ ] Redis caching of validated tokens functions correctly when enabled


This file lives at `docs/auth-service/AuthService-Flows-and-Testing.md` in the repository.
