# ADR-002: RabbitMQ + MassTransit vs PostgreSQL SKIP LOCKED

**Status:** Accepted  |  **Date:** 2026-03-15

## Context

JobEngine needs to distribute jobs across workers. Two viable approaches:
1. **PostgreSQL SKIP LOCKED** — poll a jobs table, skip locked rows
2. **RabbitMQ** — push jobs to a message queue, workers consume

## Decision

We chose **RabbitMQ with MassTransit** for JobEngine.

## Rationale

JobEngine requires fan-out: when a job completes, both the Worker Service
AND the Notification Service need to react. RabbitMQ exchanges handle this
natively. SKIP LOCKED is point-to-point — implementing fan-out requires
a separate notification polling loop.

MassTransit also provides dead letter exchange configuration, consumer
retry policies, and message acknowledgement patterns that would require
significant custom code with SKIP LOCKED.
