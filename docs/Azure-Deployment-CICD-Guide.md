# JobEngine Azure Deployment + CI/CD Guide (GUI First)

This guide is written to use the Azure Portal and GitHub UI as much as possible.

Scope:

- Deploy backend services (gateway, auth, job, worker, execution, notification) to Azure Container Apps.
- Deploy frontend to Azure Static Web Apps.
- Configure CI/CD with staging, smoke check, and manual approval before production.

## 1) What You Will Create

- Resource group: `rg-jobengine-prod`
- Azure Container Registry: `acrjobengineprod`
- Log Analytics workspace: `law-jobengine-prod`
- Container Apps environment: `cae-jobengine-prod`
- PostgreSQL Flexible Server: `pg-jobengine-prod`
- Redis Cache: `redis-jobengine-prod`
- RabbitMQ: CloudAMQP (recommended managed option)
- Container Apps:
   - `ca-jobengine-gateway`
   - `ca-jobengine-auth-service`
   - `ca-jobengine-job-service`
   - `ca-jobengine-worker-service`
   - `ca-jobengine-execution-service`
   - `ca-jobengine-notification-service`
- Static Web App for frontend
- GitHub Environments:
  - `staging`
  - `production` (with required approval)

## 2) Before You Start

- You need Azure subscription access that can create resources.
- You need GitHub admin access to this repository.
- You should have Docker installed locally.

Optional but recommended:

- Keep naming exactly as this guide to avoid path and secret confusion.

## 3) Create Core Azure Resources in Portal (Click by Click)

### 3.1 Create Resource Group

1. Open Azure Portal.
2. In top search, type `Resource groups`.
3. Click `Resource groups`.
4. Click `Create`.
5. Select your Subscription.
6. Resource group name: `rg-jobengine-prod`.
7. Region: `East US`.
8. Click `Review + create`.
9. Click `Create`.

### 3.2 Create Azure Container Registry (ACR)

1. Search `Container registries`.
2. Click `Create`.
3. Resource group: `rg-jobengine-prod`.
4. Registry name: `acrjobengineprod`.
5. Location: `East US`.
6. SKU: `Standard`.
7. Click `Review + create`.
8. Click `Create`.

### 3.3 Create Log Analytics Workspace

1. Search `Log Analytics workspaces`.
2. Click `Create`.
3. Resource group: `rg-jobengine-prod`.
4. Name: `law-jobengine-prod`.
5. Region: `East US`.
6. Click `Review + create`.
7. Click `Create`.

### 3.4 Create Container Apps Environment

1. Search `Container Apps environments`.
2. Click `Create`.
3. Resource group: `rg-jobengine-prod`.
4. Name: `cae-jobengine-prod`.
5. Region: `East US`.
6. Open `Logs` section.
7. Select Log Analytics workspace: `law-jobengine-prod`.
8. Click `Review + create`.
9. Click `Create`.

### 3.5 Create PostgreSQL Flexible Server

1. Search `Azure Database for PostgreSQL flexible servers`.
2. Click `Create`.
3. Resource group: `rg-jobengine-prod`.
4. Server name: `pg-jobengine-prod`.
5. Region: `East US`.
6. PostgreSQL version: `16`.
7. Set admin username and strong password.
8. Compute tier: start with a small production-safe option.
9. Networking: allow access needed for Container Apps.
10. Click `Review + create`.
11. Click `Create`.

Create databases:

1. Open `pg-jobengine-prod`.
2. Left menu `Databases`.
3. Click `Add`.
4. Create `je_auth`.
5. Click `Add` again.
6. Create `je_jobs`.

### 3.6 Create Azure Redis Cache

1. Search `Azure Cache for Redis`.
2. Click `Create`.
3. Resource group: `rg-jobengine-prod`.
4. Name: `redis-jobengine-prod`.
5. Region: `East US`.
6. Pricing tier: `Standard`.
7. Click `Review + create`.
8. Click `Create`.

### 3.7 RabbitMQ

Recommended GUI path:

1. In Azure Marketplace, search `CloudAMQP`.
2. Choose a plan and create instance.
3. Open CloudAMQP dashboard.
4. Copy host, username, and password for later secrets.

## 4) Build and Push Backend Images to ACR

You can keep deployment GUI-first, but image build/push is done by GitHub Actions once workflows are configured.

No manual container image push is needed if you follow sections 8 and 9.

## 5) Create Container Apps (One per Service)

Repeat this for each app.

Common values:

- Resource group: `rg-jobengine-prod`
- Environment: `cae-jobengine-prod`
- Registry: `acrjobengineprod.azurecr.io`
- Target port: `8080`

### 5.1 Create Gateway Container App

1. Search `Container Apps`.
2. Click `Create`.
3. Name: `ca-jobengine-gateway`.
4. Resource group: `rg-jobengine-prod`.
5. Container Apps environment: `cae-jobengine-prod`.
6. In `Container` tab:
   - Image source: `Azure Container Registry`.
   - Image: `jobengine-gateway` (tag can be latest created by CI/CD).
7. In `Ingress` tab:
   - Enable ingress: `On`.
   - Ingress traffic: `Accepting traffic from anywhere`.
   - Ingress type: `HTTP`.
   - Target port: `8080`.
8. In `Scale` tab:
   - Min replicas: `1`.
   - Max replicas: `2`.
9. Click `Review + create`.
10. Click `Create`.

### 5.2 Create Internal Service Container Apps

Create these app names the same way:

- `ca-jobengine-auth-service`
- `ca-jobengine-job-service`
- `ca-jobengine-worker-service`
- `ca-jobengine-execution-service`
- `ca-jobengine-notification-service`

For each of these:

1. Create app as above.
2. In `Ingress`:
   - Enable ingress: `On` for HTTP services (auth/job/execution), `Off` or internal-only for background services if not externally called.
   - If enabled, set to internal only.
3. Target port: `8080`.
4. For worker service scale:
   - Min replicas: `1`.
   - Max replicas: `3` (or higher as needed).

## 6) Configure Secrets and Environment Variables in Container Apps

Do this app by app.

### 6.1 Add Secrets

1. Open a Container App.
2. Left menu: `Secrets`.
3. Click `+ Add`.
4. Add required secrets (connection strings, redis, rabbitmq, jwt).
5. Click `Save`.

### 6.2 Add Environment Variables

1. Left menu: `Containers`.
2. Click container `Edit and deploy new revision`.
3. In environment variables, add keys used by that service.
4. For sensitive values, choose `Secret reference`.
5. Click `Deploy`.

Common keys:

- `ConnectionStrings__Auth`
- `ConnectionStrings__Jobs`
- `Redis__Connection`
- `RabbitMQ__Host`
- `RabbitMQ__Username`
- `RabbitMQ__Password`
- `Jwt__Secret`
- `Jwt__Issuer`
- `Jwt__Audience`
- `AuthService__Url`
- `ExecutionService__Url`
- `ASPNETCORE_ENVIRONMENT=Production`

Gateway-specific notes:

- Use external ingress for gateway.
- Reverse proxy destination variables must point to internal Container App URLs for auth, job, execution services.

## 7) Deploy Frontend with Static Web Apps (GUI)

1. Search `Static Web Apps`.
2. Click `Create`.
3. Resource group: `rg-jobengine-prod`.
4. Name: choose your frontend app name (for example `swa-jobengine-prod`).
5. Hosting plan: Free or Standard.
6. Deployment source: `GitHub`.
7. Authorize GitHub when prompted.
8. Select organization, repository, and branch (`main`).
9. Build preset: `Custom` if needed.
10. App location: `frontend/jobengine-web`.
11. API location: leave empty.
12. Output location: `dist`.
13. Click `Review + create`.
14. Click `Create`.

Set frontend API base URL:

1. Open the Static Web App.
2. Go to `Configuration` or `Environment variables`.
3. Add `VITE_API_BASE_URL`.
4. Value: gateway public URL, for example `https://<gateway-url>`.
5. Save and trigger redeploy.

## 8) Configure GitHub OIDC and Azure Access (Mostly GUI)

### 8.1 Create App Registration

1. In Azure Portal, search `App registrations`.
2. Click `New registration`.
3. Name: `gha-jobengine-oidc`.
4. Click `Register`.
5. Copy:
   - Application (client) ID
   - Directory (tenant) ID

### 8.2 Grant Permissions

1. Open `Subscriptions`.
2. Select your subscription.
3. Left menu: `Access control (IAM)`.
4. Click `Add role assignment`.
5. Role: Contributor (or custom least-privilege role).
6. Assign to `gha-jobengine-oidc` application.
7. Save.
8. Copy Subscription ID.

### 8.3 Add Federated Credential

1. Open `gha-jobengine-oidc` app registration.
2. Left menu: `Certificates & secrets`.
3. Open `Federated credentials`.
4. Click `Add credential`.
5. Choose GitHub Actions scenario.
6. Fill organization, repository, branch (for example `main`).
7. Save.
8. Repeat for `dev` branch if needed.

## 9) Configure GitHub Repository Secrets and Environments (GUI)

### 9.1 Repository Secrets

1. Open repository on GitHub.
2. Go to `Settings`.
3. Left menu: `Secrets and variables` -> `Actions`.
4. Click `New repository secret` and add:
   - `AZURE_CLIENT_ID`
   - `AZURE_TENANT_ID`
   - `AZURE_SUBSCRIPTION_ID`
   - `ACR_NAME`
   - `ACR_LOGIN_SERVER`
   - `AZURE_RG`

### 9.2 Environments + Approval Gate

1. In GitHub repo settings, open `Environments`.
2. Click `New environment`, create `staging`.
3. Create `production`.
4. Open `production`.
5. Under `Deployment protection rules`, add required reviewer(s).
6. Save.

This is your manual approval checkpoint before production deploy.

## 10) CI/CD Workflow Files

Ensure workflow files exist:

- `.github/workflows/ci.yml`
- `.github/workflows/cd.yml`

Current repository conventions (after backend folder refactor):

- Backend solution path: `backend/JobEngine.sln`
- Dockerfiles under `backend/gateway` and `backend/services`

The CD workflow should build and push these image repositories:



- `jobengine-gateway`
- `jobengine-auth-service`
- `jobengine-job-service`
- `jobengine-worker-service`
- `jobengine-execution-service`
- `jobengine-notification-service`

## 11) First End-to-End Deployment (GUI Flow)

1. Push code to `dev`.
2. Open GitHub `Actions` tab.
3. Confirm CI passes.
4. Merge `dev` to `main`.
5. CD workflow starts automatically.
6. Verify staging deploy completes.
7. Verify smoke check step passes.
8. Open `Review deployments` when workflow waits.
9. Approve `production` deployment.
10. Verify production deployment completes.

## 12) Validation Checklist After Deployment

Backend:

- Gateway public URL responds.
- Auth API works via gateway.
- Job submit and status flow works.
- Worker replicas are running.
- Notification service receives events.

Frontend:

- Static Web App loads successfully.
- Login and API calls route through gateway URL.

Operations:

- Container App `Logs` show healthy startup.
- PostgreSQL, Redis, RabbitMQ metrics look healthy.

## 13) Rollback (GUI)

For a failed deployment:

1. Open target Container App.
2. Go to `Revisions`.
3. Find previous stable revision.
4. Set traffic back to stable revision (or activate stable revision).
5. Reduce traffic to bad revision to 0%.

## 14) Security Checklist

- Keep only gateway externally exposed.
- Keep internal services private/internal.
- Store secrets in Key Vault or Container App secrets.
- Avoid long-lived credentials in GitHub.
- Use OIDC federation for GitHub to Azure.
- Enable branch protection and required checks.

## 15) Quick Troubleshooting

If GitHub workflow fails:

1. Open the failed job in `Actions`.
2. Check failing step and exact command.
3. Verify required secrets exist and are named exactly.
4. Verify ACR image names/tags are correct.

If gateway returns 502/503:

1. Open `ca-jobengine-gateway` -> `Logs`.
2. Check reverse proxy destination env vars.
3. Confirm target services are healthy and internal URLs are correct.

If frontend cannot call API:

1. Verify `VITE_API_BASE_URL` in Static Web App settings.
2. Verify gateway ingress is external and HTTPS URL is correct.
3. Redeploy frontend after changing config.
