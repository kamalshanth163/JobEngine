# JobEngine

A production-grade distributed job processing SaaS built in .NET 8.

## Architecture

JobEngine is a microservices platform with 6 independent services:

```
Client → API Gateway (YARP) → Auth Service     → PostgreSQL (je_auth)
                             → Job Service      → PostgreSQL (je_jobs)
                             → RabbitMQ ─────── → Worker Service (3 replicas)
                                                → Execution Service
                                                → Notification Service

```

## Quick start

```sh
git clone https://github.com/yourname/jobengine
cd jobengine
cp .env.example .env
docker compose up --build
```

Open:
- API Gateway: http://localhost:8080
- RabbitMQ UI: http://localhost:15672 (guest/guest)
- Grafana:     http://localhost:3001  (admin/admin)
- Prometheus:  http://localhost:9090

## Key concepts demonstrated

- Clean Architecture
- CQRS + MediatR
- Multi-tenancy with EF Core global query filters
- Distributed locking with Redis
- Exactly-once delivery techniques
- MassTransit + RabbitMQ
- OpenTelemetry + Prometheus + Grafana
- Integration tests with Testcontainers

## ADRs

See `docs/adr/` for architecture decision records that explain major design choices.
