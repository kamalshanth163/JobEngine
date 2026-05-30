# Developer Commands: Docker, Dotnet, and EF Core

This guide is a command cookbook for common developer scenarios in this repository.

All commands assume you are at repository root:

    C:/Users/USER/Desktop/JobEngine/JobEngine-Repo/JobEngine

## 1) Docker commands

### 1.1 Start and stop main stack

Start all services:

    docker compose up --build

Start in background:

    docker compose up --build -d

Stop and remove containers:

    docker compose down

Stop and remove containers, network, and volumes:

    docker compose down -v

List running services:

    docker compose ps

List all services including exited:

    docker compose ps -a

### 1.2 Start and stop debug stack

Debug overlay keeps production compose untouched and runs SDK containers for attach debugging.

Bring debug stack up:

    docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d

Show debug stack services:

    docker compose -f docker-compose.yml -f docker-compose.debug.yml ps

Bring debug stack down:

    docker compose -f docker-compose.yml -f docker-compose.debug.yml down

Validate merged compose files:

    docker compose -f docker-compose.yml -f docker-compose.debug.yml config

### 1.3 Rebuild or restart one service

Rebuild and start one service:

    docker compose up --build -d job-service

Restart one service:

    docker compose restart job-service

Stop one service:

    docker compose stop job-service

Start one stopped service:

    docker compose start job-service

### 1.4 Logs and diagnostics

Tail logs for all services:

    docker compose logs -f

Tail logs for one service:

    docker compose logs -f gateway

Tail recent lines for one service:

    docker compose logs --tail 200 gateway

Inspect one container status:

    docker inspect je-debug-gateway

Show environment variables inside a container:

    docker inspect je-debug-gateway --format '{{range .Config.Env}}{{println .}}{{end}}'

### 1.5 Execute shell inside containers

Open shell:

    docker exec -it je-debug-job-service sh

Check running processes:

    docker exec je-debug-execution-service sh -lc "ps -eo pid,args"

### 1.6 Database checks (Postgres)

Connect to postgres container shell:

    docker exec -it jobengine-postgres-1 sh

Run psql and list databases:

    psql -U postgres -c "\\l"

Check jobs table rows:

    psql -U postgres -d je_jobs -c "select count(*) from \"Jobs\";"

## 2) Dotnet commands

### 2.1 Restore and build

Restore solution (slnx):

    dotnet restore backend/JobEngine.slnx

Build all projects from slnx:

    dotnet build backend/JobEngine.slnx

Build classic sln (editor fallback):

    dotnet build backend/JobEngine.sln

Build one project:

    dotnet build backend/services/JobService/src/JobService.Api/JobService.Api.csproj

Clean one project:

    dotnet clean backend/services/JobService/src/JobService.Api/JobService.Api.csproj

### 2.2 Run services locally (outside Docker)

Run gateway:

    dotnet run --project backend/gateway/ApiGateway/ApiGateway.csproj

Run auth service:

    dotnet run --project backend/services/AuthService/src/AuthService.Api/AuthService.Api.csproj

Run job service:

    dotnet run --project backend/services/JobService/src/JobService.Api/JobService.Api.csproj

Run execution service:

    dotnet run --project backend/services/ExecutionService/src/ExecutionService.Api/ExecutionService.Api.csproj

Run worker service:

    dotnet run --project backend/services/WorkerService/src/WorkerService/WorkerService.csproj

Run notification service:

    dotnet run --project backend/services/NotificationService/src/NotificationService/NotificationService.csproj

### 2.3 Tests

Run all tests:

    dotnet test backend/JobEngine.slnx

Run one test project:

    dotnet test backend/tests/JobService.Tests/JobService.Tests.csproj

Run one named test:

    dotnet test backend/tests/JobService.Tests/JobService.Tests.csproj --filter "FullyQualifiedName~TestName"

### 2.4 NuGet package operations

Add package to project:

    dotnet add backend/services/JobService/src/JobService.Api/JobService.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer --version 10.0.5

Remove package:

    dotnet remove backend/services/JobService/src/JobService.Api/JobService.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer

List project packages:

    dotnet list backend/services/JobService/src/JobService.Api/JobService.Api.csproj package

## 3) EF Core commands

This repository uses local tool manifest for EF Core CLI.

Install or restore local tools:

    dotnet tool restore

Use EF command through local tool:

    dotnet dotnet-ef

### 3.1 AuthService migrations

Add migration:

    dotnet dotnet-ef migrations add <MigrationName> \
      --project backend/services/AuthService/src/AuthService.Infrastructure/AuthService.Infrastructure.csproj \
      --startup-project backend/services/AuthService/src/AuthService.Api/AuthService.Api.csproj \
      --output-dir Migrations

Update database:

    dotnet dotnet-ef database update \
      --project backend/services/AuthService/src/AuthService.Infrastructure/AuthService.Infrastructure.csproj \
      --startup-project backend/services/AuthService/src/AuthService.Api/AuthService.Api.csproj

List migrations:

    dotnet dotnet-ef migrations list \
      --project backend/services/AuthService/src/AuthService.Infrastructure/AuthService.Infrastructure.csproj \
      --startup-project backend/services/AuthService/src/AuthService.Api/AuthService.Api.csproj

Remove last migration (if not applied):

    dotnet dotnet-ef migrations remove \
      --project backend/services/AuthService/src/AuthService.Infrastructure/AuthService.Infrastructure.csproj \
      --startup-project backend/services/AuthService/src/AuthService.Api/AuthService.Api.csproj

### 3.2 JobService migrations

Add migration:

    dotnet dotnet-ef migrations add <MigrationName> \
      --project backend/services/JobService/src/JobService.Infrastructure/JobService.Infrastructure.csproj \
      --startup-project backend/services/JobService/src/JobService.Api/JobService.Api.csproj \
      --output-dir Persistence/Migrations

Update database:

    dotnet dotnet-ef database update \
      --project backend/services/JobService/src/JobService.Infrastructure/JobService.Infrastructure.csproj \
      --startup-project backend/services/JobService/src/JobService.Api/JobService.Api.csproj

List migrations:

    dotnet dotnet-ef migrations list \
      --project backend/services/JobService/src/JobService.Infrastructure/JobService.Infrastructure.csproj \
      --startup-project backend/services/JobService/src/JobService.Api/JobService.Api.csproj

Remove last migration (if not applied):

    dotnet dotnet-ef migrations remove \
      --project backend/services/JobService/src/JobService.Infrastructure/JobService.Infrastructure.csproj \
      --startup-project backend/services/JobService/src/JobService.Api/JobService.Api.csproj

### 3.3 Generate SQL scripts

Generate idempotent script for AuthService:

    dotnet dotnet-ef migrations script --idempotent \
      --project backend/services/AuthService/src/AuthService.Infrastructure/AuthService.Infrastructure.csproj \
      --startup-project backend/services/AuthService/src/AuthService.Api/AuthService.Api.csproj \
      --output auth-migrations.sql

Generate idempotent script for JobService:

    dotnet dotnet-ef migrations script --idempotent \
      --project backend/services/JobService/src/JobService.Infrastructure/JobService.Infrastructure.csproj \
      --startup-project backend/services/JobService/src/JobService.Api/JobService.Api.csproj \
      --output jobs-migrations.sql

## 4) Common workflow recipes

### 4.1 Add a new API endpoint and verify quickly

1. Build target service:

    dotnet build backend/services/ExecutionService/src/ExecutionService.Api/ExecutionService.Api.csproj

2. If running with Docker debug profile, restart only that service:

    docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d execution-service

3. Test through gateway:

    curl -i http://localhost:8080/health

### 4.2 Add a model change and apply migration

1. Edit entities/configuration.
2. Add migration using service commands above.
3. Update database.
4. Build and run tests:

    dotnet build backend/JobEngine.slnx
    dotnet test backend/JobEngine.slnx

### 4.3 Reset local Docker state (use carefully)

This removes containers, networks, and volumes for this stack:

    docker compose -f docker-compose.yml -f docker-compose.debug.yml down -v

Then start again:

    docker compose -f docker-compose.yml -f docker-compose.debug.yml up -d

## 5) VS Code debug tasks

From VS Code tasks:

- docker: debug up
- docker: debug down
- docker: debug ps

These are defined in .vscode/tasks.json.
