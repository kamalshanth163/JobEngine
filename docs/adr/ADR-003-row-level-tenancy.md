# ADR-003: Row-level tenancy vs schema-per-tenant

**Status:** Accepted  |  **Date:** 2026-03-15

## Context

Multi-tenant SaaS platforms have three main isolation strategies:
1. **Separate databases**
2. **Schema-per-tenant**
3. **Row-level isolation** — shared tables, tenant_id column on every row

## Decision

**Row-level isolation** using EF Core global query filters on `tenant_id`.

## Rationale

With row-level isolation, one migration run handles all tenants. The EF Core global query filter makes it harder to accidentally query across tenant boundaries.
