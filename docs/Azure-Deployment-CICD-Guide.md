# JobEngine Azure Deployment + CI/CD Guide

This guide covers deploying the full JobEngine stack to Azure and setting up GitHub Actions CI/CD with:

- automated tests on every change,
- staging deployment after tests pass,
- manual approval before production deployment.

## 1) Target Azure Architecture

Recommended pragmatic production setup:

- Frontend (`frontend/jobengine-web`): Azure Static Web Apps
- API Gateway (`backend/gateway/ApiGateway`): Azure Container Apps
- Auth Service (`backend/services/AuthService/src/AuthService.Api`): Azure Container Apps
- Job Service (`backend/services/JobService/src/JobService.Api`): Azure Container Apps
- Worker Service (`backend/services/WorkerService/src/WorkerService`): Azure Container Apps (scale by replicas)
- Execution Service (`backend/services/ExecutionService/src/ExecutionService.Api`): Azure Container Apps
- Notification Service (`backend/services/NotificationService/src/NotificationService`): Azure Container Apps
- PostgreSQL: Azure Database for PostgreSQL Flexible Server
- Redis: Azure Cache for Redis
- RabbitMQ: CloudAMQP (managed) or self-hosted in Azure (VM/ACA)
- Container Registry: Azure Container Registry (ACR)
- Secrets and config: Azure Key Vault + Container App secrets/env vars
- Observability (optional): Azure Monitor / Log Analytics

## 2) Prerequisites

Install and configure:

- Azure CLI
- Docker
- GitHub repository admin access
- Azure subscription with permission to create resources

Login:

```bash
az login
az account set --subscription "<SUBSCRIPTION_NAME_OR_ID>"
```

## 3) Azure Resource Bootstrap

Use one resource group for all infra (or split by environment).

```bash
az group create -n rg-jobengine-prod -l eastus
```

Create ACR:

```bash
az acr create -g rg-jobengine-prod -n acrjobengineprod --sku Standard
```

Create Container Apps environment:

```bash
az monitor log-analytics workspace create \
  -g rg-jobengine-prod \
  -n law-jobengine-prod

LAW_ID=$(az monitor log-analytics workspace show -g rg-jobengine-prod -n law-jobengine-prod --query customerId -o tsv)
LAW_KEY=$(az monitor log-analytics workspace get-shared-keys -g rg-jobengine-prod -n law-jobengine-prod --query primarySharedKey -o tsv)

az containerapp env create \
  -g rg-jobengine-prod \
  -n cae-jobengine-prod \
  --logs-workspace-id "$LAW_ID" \
  --logs-workspace-key "$LAW_KEY" \
  -l eastus
```

Create PostgreSQL flexible server + DBs:

```bash
az postgres flexible-server create \
  -g rg-jobengine-prod \
  -n pg-jobengine-prod \
  -l eastus \
  --admin-user pgadmin \
  --admin-password "<STRONG_PASSWORD>" \
  --sku-name Standard_B2s \
  --version 16

az postgres flexible-server db create -g rg-jobengine-prod -s pg-jobengine-prod -d je_auth
az postgres flexible-server db create -g rg-jobengine-prod -s pg-jobengine-prod -d je_jobs
```

Create Redis:

```bash
az redis create \
  -g rg-jobengine-prod \
  -n redis-jobengine-prod \
  -l eastus \
  --sku Standard \
  --vm-size c1
```

## 4) Configuration and Secrets Strategy

Use environment variables per service (already supported by the project).

Examples:

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
- `ASPNETCORE_ENVIRONMENT`

Recommended:

- Store source of truth in Azure Key Vault.
- Sync into Container App secrets and map to env vars.
- Do not hardcode production credentials in GitHub secrets long-term.

## 5) Containerize and Push Images

The repo already includes Dockerfiles for gateway and services.

Image naming convention:

- `acrjobengineprod.azurecr.io/gateway:<sha>`
- `acrjobengineprod.azurecr.io/auth-service:<sha>`
- `acrjobengineprod.azurecr.io/job-service:<sha>`
- `acrjobengineprod.azurecr.io/worker-service:<sha>`
- `acrjobengineprod.azurecr.io/execution-service:<sha>`
- `acrjobengineprod.azurecr.io/notification-service:<sha>`

## 6) Deploy Container Apps

Create one Container App per service. Repeat pattern below per service:

```bash
az containerapp create \
  -g rg-jobengine-prod \
  -n ca-auth-service \
  --environment cae-jobengine-prod \
  --image acrjobengineprod.azurecr.io/auth-service:<tag> \
  --registry-server acrjobengineprod.azurecr.io \
  --registry-identity system \
  --target-port 8080 \
  --ingress internal \
  --secrets \
    conn-auth="<conn-string>" \
    redis-conn="<redis-conn>" \
    jwt-secret="<jwt-secret>" \
  --env-vars \
    ConnectionStrings__Auth=secretref:conn-auth \
    Redis__Connection=secretref:redis-conn \
    Jwt__Secret=secretref:jwt-secret \
    Jwt__Issuer=jobengine \
    Jwt__Audience=jobengine-clients \
    ASPNETCORE_ENVIRONMENT=Production
```

For the gateway, use external ingress and set reverse proxy destinations to internal service URLs.

## 7) Frontend Deployment (Static Web Apps)

Create Static Web App and connect to the GitHub repo.

Set frontend environment variable:

- `VITE_API_BASE_URL=https://<gateway-public-url>`

## 8) GitHub Environments and Approvals

Create GitHub Environments:

- `staging`
- `production`

Configure environment protection:

- `staging`: optional reviewer
- `production`: required reviewer(s), wait timer optional

This provides manual approval before production deployment.

## 9) Required GitHub Secrets

Repository secrets (minimum):

- `AZURE_CLIENT_ID`
- `AZURE_TENANT_ID`
- `AZURE_SUBSCRIPTION_ID`
- `ACR_NAME` (example: `acrjobengineprod`)
- `ACR_LOGIN_SERVER` (example: `acrjobengineprod.azurecr.io`)
- `AZURE_RG` (example: `rg-jobengine-prod`)

Environment secrets (staging/prod), as needed:

- service-specific connection strings
- RabbitMQ credentials
- JWT secret
- any webhook secrets

## 10) CI Workflow (Build + Test)

Create `.github/workflows/ci.yml`:

```yaml
name: CI

on:
  pull_request:
    branches: [main, dev]
  push:
    branches: [dev]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 10.0.x

      - name: Restore
        run: dotnet restore backend/JobEngine.sln

      - name: Build
        run: dotnet build backend/JobEngine.sln --no-restore --configuration Release

      - name: Run tests
        run: dotnet test backend/JobEngine.sln --no-build --configuration Release --logger "trx;LogFileName=test-results.trx"

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: 22

      - name: Frontend install and build
        working-directory: frontend/jobengine-web
        run: |
          npm ci
          npm run build
```

## 11) CD Workflow (Staging + Approval + Production)

Create `.github/workflows/cd.yml`:

```yaml
name: CD

on:
  push:
    branches: [main]

permissions:
  id-token: write
  contents: read

jobs:
  build-and-push-images:
    runs-on: ubuntu-latest
    outputs:
      image_tag: ${{ steps.vars.outputs.image_tag }}

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set vars
        id: vars
        run: echo "image_tag=${GITHUB_SHA}" >> $GITHUB_OUTPUT

      - name: Azure login (OIDC)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: ACR login
        run: az acr login -n ${{ secrets.ACR_NAME }}

      - name: Build and push images
        run: |
          TAG=${{ steps.vars.outputs.image_tag }}
          ACR=${{ secrets.ACR_LOGIN_SERVER }}

          docker build -f backend/gateway/ApiGateway/Dockerfile -t $ACR/gateway:$TAG .
          docker build -f backend/services/AuthService/src/Dockerfile -t $ACR/auth-service:$TAG .
          docker build -f backend/services/JobService/src/Dockerfile -t $ACR/job-service:$TAG .
          docker build -f backend/services/WorkerService/src/WorkerService/Dockerfile -t $ACR/worker-service:$TAG .
          docker build -f backend/services/ExecutionService/src/Dockerfile -t $ACR/execution-service:$TAG .
          docker build -f backend/services/NotificationService/src/NotificationService/Dockerfile -t $ACR/notification-service:$TAG .

          docker push $ACR/gateway:$TAG
          docker push $ACR/auth-service:$TAG
          docker push $ACR/job-service:$TAG
          docker push $ACR/worker-service:$TAG
          docker push $ACR/execution-service:$TAG
          docker push $ACR/notification-service:$TAG

  deploy-staging:
    runs-on: ubuntu-latest
    needs: build-and-push-images
    environment: staging

    steps:
      - name: Azure login (OIDC)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy staging revision
        run: |
          TAG=${{ needs.build-and-push-images.outputs.image_tag }}
          ACR=${{ secrets.ACR_LOGIN_SERVER }}
          RG=${{ secrets.AZURE_RG }}

          az containerapp update -g $RG -n ca-gateway --image $ACR/gateway:$TAG
          az containerapp update -g $RG -n ca-auth-service --image $ACR/auth-service:$TAG
          az containerapp update -g $RG -n ca-job-service --image $ACR/job-service:$TAG
          az containerapp update -g $RG -n ca-worker-service --image $ACR/worker-service:$TAG
          az containerapp update -g $RG -n ca-execution-service --image $ACR/execution-service:$TAG
          az containerapp update -g $RG -n ca-notification-service --image $ACR/notification-service:$TAG

  smoke-tests-staging:
    runs-on: ubuntu-latest
    needs: deploy-staging

    steps:
      - name: Gateway health
        run: |
          curl -f https://<staging-gateway-url>/health

  deploy-production:
    runs-on: ubuntu-latest
    needs: [build-and-push-images, smoke-tests-staging]
    environment: production

    steps:
      - name: Azure login (OIDC)
        uses: azure/login@v2
        with:
          client-id: ${{ secrets.AZURE_CLIENT_ID }}
          tenant-id: ${{ secrets.AZURE_TENANT_ID }}
          subscription-id: ${{ secrets.AZURE_SUBSCRIPTION_ID }}

      - name: Deploy production revision
        run: |
          TAG=${{ needs.build-and-push-images.outputs.image_tag }}
          ACR=${{ secrets.ACR_LOGIN_SERVER }}
          RG=${{ secrets.AZURE_RG }}

          az containerapp update -g $RG -n ca-gateway --image $ACR/gateway:$TAG
          az containerapp update -g $RG -n ca-auth-service --image $ACR/auth-service:$TAG
          az containerapp update -g $RG -n ca-job-service --image $ACR/job-service:$TAG
          az containerapp update -g $RG -n ca-worker-service --image $ACR/worker-service:$TAG
          az containerapp update -g $RG -n ca-execution-service --image $ACR/execution-service:$TAG
          az containerapp update -g $RG -n ca-notification-service --image $ACR/notification-service:$TAG
```

Important:

- Production deployment will pause for manual approval if environment protection is configured for `production`.
- This satisfies the tests + approval gate requirement.

## 12) Recommended Release Policy

- PR to `dev`: run CI only.
- Merge `dev` to `main`: run CI + CD staging + smoke tests + manual approval + production.

## 13) Post-Deployment Validation

Verify:

- Gateway health endpoint returns 200.
- Auth login/register through gateway works.
- Job submit and status flow works end-to-end.
- RabbitMQ queue depth and worker throughput are healthy.
- Notification webhooks are delivered and signed.
- Logs are visible for all services.

## 14) Rollback Plan

Container Apps supports revision-based rollback:

1. List revisions.
2. Activate previous stable revision.
3. Deactivate bad revision.

Example:

```bash
az containerapp revision list -g rg-jobengine-prod -n ca-job-service -o table
az containerapp revision activate -g rg-jobengine-prod -n ca-job-service --revision <revision-name>
```

## 15) Security and Hardening Checklist

- Use OIDC federation from GitHub to Azure (avoid static service principal secrets).
- Restrict public ingress to gateway only.
- Keep internal services on internal ingress.
- Store secrets in Key Vault and rotate regularly.
- Enable branch protection (required reviews, status checks).
- Enable Dependabot and container image scanning.

---

If needed, this guide can be split into:

- `docs/Azure-Infrastructure-Setup.md`
- `docs/GitHub-Actions-CI-CD.md`
- `docs/Runbook-Rollback-and-Operations.md`
