# ADR-004: SHA-256 for API key hashing (not bcrypt)

**Status:** Accepted  |  **Date:** 2026-03-15

## Context

API keys need to be stored securely. Bcrypt is the standard for
password hashing. Why not use bcrypt for API keys too?

## Decision

**SHA-256** for API key hashing, not bcrypt.

## Rationale

API keys are long (32+ random bytes), cryptographically generated,
and never chosen by users. They have no dictionary attack surface.

Bcrypt's value is its intentional slowness — it prevents brute-force
attacks against short, human-chosen passwords. For a 32-byte random
key, brute force is computationally infeasible regardless of hash speed.

## Implementation

```csharp
var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
CryptographicOperations.FixedTimeEquals(
    Encoding.UTF8.GetBytes(storedHash),
    Encoding.UTF8.GetBytes(computedHash));
```
