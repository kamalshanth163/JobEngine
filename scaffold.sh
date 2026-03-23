#!/bin/bash
# Run from the root folder where you want JobEngine to live

mkdir JobEngine && cd JobEngine
dotnet new sln -n JobEngine

# ── Shared libraries ─────────────────────────────────────
dotnet new classlib -n JobEngine.Shared.Contracts    -o shared/Contracts
dotnet new classlib -n JobEngine.Shared.Common       -o shared/Common
dotnet new classlib -n JobEngine.Shared.Logging      -o shared/Logging

# ── Auth Service ─────────────────────────────────────────
dotnet new classlib -n AuthService.Domain       -o services/AuthService/src/AuthService.Domain
dotnet new classlib -n AuthService.Application  -o services/AuthService/src/AuthService.Application
dotnet new classlib -n AuthService.Infrastructure -o services/AuthService/src/AuthService.Infrastructure
dotnet new webapi   -n AuthService.Api          -o services/AuthService/src/AuthService.Api

# ── Job Service ──────────────────────────────────────────
dotnet new classlib -n JobService.Domain        -o services/JobService/src/JobService.Domain
dotnet new classlib -n JobService.Application   -o services/JobService/src/JobService.Application
dotnet new classlib -n JobService.Infrastructure -o services/JobService/src/JobService.Infrastructure
dotnet new webapi   -n JobService.Api           -o services/JobService/src/JobService.Api

# ── Worker Service ───────────────────────────────────────
dotnet new worker   -n WorkerService            -o services/WorkerService/src/WorkerService

# ── Execution Service ────────────────────────────────────
dotnet new webapi   -n ExecutionService.Api     -o services/ExecutionService/src/ExecutionService.Api
dotnet new classlib -n ExecutionService.Core    -o services/ExecutionService/src/ExecutionService.Core

# ── Notification Service ─────────────────────────────────
dotnet new worker   -n NotificationService      -o services/NotificationService/src/NotificationService

# ── API Gateway ──────────────────────────────────────────
dotnet new webapi   -n ApiGateway               -o gateway/ApiGateway

# ── Tests ────────────────────────────────────────────────
dotnet new xunit    -n JobService.Tests         -o tests/JobService.Tests
dotnet new xunit    -n AuthService.Tests        -o tests/AuthService.Tests
dotnet new xunit    -n Integration.Tests        -o tests/Integration.Tests

# ── Add everything to solution ───────────────────────────
dotnet sln add **/*.csproj

# ── Project references (Clean Architecture boundaries) ───
# Application depends on Domain only
dotnet add services/JobService/src/JobService.Application \
  reference services/JobService/src/JobService.Domain
dotnet add services/JobService/src/JobService.Application \
  reference shared/Contracts

# Infrastructure depends on Application
dotnet add services/JobService/src/JobService.Infrastructure \
  reference services/JobService/src/JobService.Application

# API depends on Application + Infrastructure (for DI wiring)
dotnet add services/JobService/src/JobService.Api \
  reference services/JobService/src/JobService.Application
dotnet add services/JobService/src/JobService.Api \
  reference services/JobService/src/JobService.Infrastructure

# Same pattern for AuthService
dotnet add services/AuthService/src/AuthService.Application \
  reference services/AuthService/src/AuthService.Domain
dotnet add services/AuthService/src/AuthService.Infrastructure \
  reference services/AuthService/src/AuthService.Application
dotnet add services/AuthService/src/AuthService.Api \
  reference services/AuthService/src/AuthService.Application
dotnet add services/AuthService/src/AuthService.Api \
  reference services/AuthService/src/AuthService.Infrastructure

echo "✓ Solution scaffolded. Run: dotnet build"
