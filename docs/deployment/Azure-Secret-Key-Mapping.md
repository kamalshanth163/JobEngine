# Azure Secret Key Mapping

Use this mapping when creating secrets in Azure Container Apps / Key Vault.

- Keep the app setting/env var name exactly as used by .NET (with `__`).
- Use the Azure secret key with lowercase and single `-`.
- In Container Apps, set env var `Name` to the app key and point it to the matching secret reference.

## Global Mapping

| App setting/env var name (.NET) | Azure secret key (Container Apps / Key Vault) | Example Key Vault secret URL suffix |
|---|---|---|
| `ConnectionStrings__Auth` | `connectionstrings-auth` | `/secrets/connectionstrings-auth` |
| `ConnectionStrings__Jobs` | `connectionstrings-jobs` | `/secrets/connectionstrings-jobs` |
| `Redis__Connection` | `redis-connection` | `/secrets/redis-connection` |
| `Jwt__Secret` | `jwt-secret` | `/secrets/jwt-secret` |
| `Jwt__Issuer` | `jwt-issuer` | `/secrets/jwt-issuer` |
| `Jwt__Audience` | `jwt-audience` | `/secrets/jwt-audience` |
| `RabbitMQ__Host` | `rabbitmq-host` | `/secrets/rabbitmq-host` |
| `RabbitMQ__Username` | `rabbitmq-username` | `/secrets/rabbitmq-username` |
| `RabbitMQ__Password` | `rabbitmq-password` | `/secrets/rabbitmq-password` |
| `AuthService__Url` | `authservice-url` | `/secrets/authservice-url` |
| `ExecutionService__Url` | `executionservice-url` | `/secrets/executionservice-url` |
| `ReverseProxy__Clusters__auth-cluster__Destinations__auth-service__Address` | `reverseproxy-clusters-auth-cluster-destinations-auth-service-address` | `/secrets/reverseproxy-clusters-auth-cluster-destinations-auth-service-address` |
| `ReverseProxy__Clusters__jobs-cluster__Destinations__job-service__Address` | `reverseproxy-clusters-jobs-cluster-destinations-job-service-address` | `/secrets/reverseproxy-clusters-jobs-cluster-destinations-job-service-address` |
| `ReverseProxy__Clusters__execution-cluster__Destinations__execution-service__Address` | `reverseproxy-clusters-execution-cluster-destinations-execution-service-address` | `/secrets/reverseproxy-clusters-execution-cluster-destinations-execution-service-address` |

## Per Service Quick Reference

| Service | App setting/env var names |
|---|---|
| auth-service | `ConnectionStrings__Auth`, `Redis__Connection`, `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience` |
| job-service | `ConnectionStrings__Jobs`, `RabbitMQ__Host`, `RabbitMQ__Username`, `RabbitMQ__Password`, `Redis__Connection`, `Jwt__Secret`, `Jwt__Issuer`, `Jwt__Audience`, `AuthService__Url` |
| worker-service | `RabbitMQ__Host`, `Redis__Connection`, `ExecutionService__Url`, `ConnectionStrings__Jobs` |
| notification-service | `RabbitMQ__Host`, `ConnectionStrings__Jobs` |
| gateway | `ReverseProxy__Clusters__auth-cluster__Destinations__auth-service__Address`, `ReverseProxy__Clusters__jobs-cluster__Destinations__job-service__Address`, `ReverseProxy__Clusters__execution-cluster__Destinations__execution-service__Address` |

## Container Apps Setup Pattern

For each variable:

1. Create a secret key with the Azure name from the table (for example `connectionstrings-auth`).
2. In the container app environment variables:
   - Name: `.NET app setting/env var name` (for example `ConnectionStrings__Auth`)
   - Source: `Secret reference`
   - Secret: matching Azure key (for example `connectionstrings-auth`)

If you use `Key Vault reference` as secret type, ensure the Container App has a managed identity and that identity has `get` access to secrets in Key Vault.
