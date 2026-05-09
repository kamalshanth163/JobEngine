# ADR-001: Microservices vs modular monolith

**Status:** Accepted
**Date:** 2026-03-15

## Context

JobEngine needs to process background jobs submitted by multiple tenants
across a distributed worker cluster. We evaluated two architectures:

1. **Modular monolith** — single deployable, modules communicate in-process
2. **Microservices** — independent deployables communicating over the network

## Decision

We chose **microservices** for this project.

## Rationale

The Worker and Execution services have fundamentally different scaling
profiles from the Auth and Job services. Separating them allows independent scaling and isolation.

## Consequences

We accept the operational overhead because the project goal is explicitly
to demonstrate distributed systems competence.
