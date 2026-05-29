# JobEngine Full Deployment Plan (Azure + AWS)

This document provides a practical, production-oriented deployment plan for JobEngine in both Azure and AWS.

It answers:
- What to deploy in each cloud
- How to deploy it step by step
- Which choices are best for cost, reliability, and operations

---

## 1. Assumptions About JobEngine

Based on the existing docs, JobEngine includes:
- API services (Auth + Job APIs)
- Worker service(s) for async execution
- PostgreSQL database
- Redis for locking/caching
- RabbitMQ for messaging
- Optional React frontend
- CI/CD from GitHub

If your actual services differ, keep the same infrastructure pattern and swap service names.

---

## 2. Deployment Strategies

Choose one of these strategies before implementation.

### Strategy A: Single Cloud Primary (Recommended First)
- Run full production in one cloud (Azure or AWS)
- Keep Infrastructure as Code ready for the other cloud
- Add cross-cloud DR later

Advice:
- Best for speed and team focus
- Lowest operational complexity
- Easiest to troubleshoot

### Strategy B: Active-Passive Multi-Cloud
- Primary cloud handles production traffic
- Secondary cloud is warm standby (lower scale)
- Database backups replicated cross-cloud

Advice:
- Better resilience against cloud-wide incidents
- Higher cost and process complexity
- Strongly recommended only after stable single-cloud operations

### Strategy C: Active-Active Multi-Cloud (Not Recommended Initially)
- Both clouds serve production at same time
- Requires complex data consistency and routing rules

Advice:
- Very high complexity for this architecture
- Only consider for strict global availability requirements

---

## 3. What To Deploy In Each Cloud

## Azure Target Stack

| JobEngine Component | Azure Service |
| --- | --- |
| API services (.NET) | Azure Container Apps (or App Service) |
| Worker services | Azure Container Apps Jobs or Container Apps |
| PostgreSQL | Azure Database for PostgreSQL Flexible Server |
| Redis | Azure Cache for Redis |
| RabbitMQ | CloudAMQP (managed) or RabbitMQ on VM |
| Frontend (React) | Azure Static Web Apps |
| Secrets | Azure Key Vault |
| Container registry | Azure Container Registry (ACR) |
| Monitoring | Azure Monitor + Application Insights + Log Analytics |
| DNS/TLS | Azure DNS + managed certificates |

Advice:
- Prefer Container Apps if you want containerized scaling without full AKS complexity.
- Use App Service only if you want simpler non-container hosting.

## AWS Target Stack

| JobEngine Component | AWS Service |
| --- | --- |
| API services (.NET) | AWS ECS Fargate (or App Runner) |
| Worker services | AWS ECS Fargate services |
| PostgreSQL | Amazon RDS for PostgreSQL |
| Redis | Amazon ElastiCache for Redis |
| RabbitMQ | Amazon MQ for RabbitMQ |
| Frontend (React) | S3 + CloudFront |
| Secrets | AWS Secrets Manager |
| Container registry | Amazon ECR |
| Monitoring | CloudWatch Logs + Metrics + X-Ray |
| DNS/TLS | Route 53 + ACM |

Advice:
- Prefer ECS Fargate for balanced control and low ops overhead.
- Use App Runner only for simpler web APIs; worker patterns are cleaner on ECS.

---

## 4. Reference Architecture (Both Clouds)

1. Client calls API through HTTPS endpoint.
2. API validates auth and writes jobs to PostgreSQL.
3. API publishes execution messages to RabbitMQ.
4. Worker consumes from RabbitMQ and processes jobs.
5. Worker uses Redis for distributed locks/idempotency.
6. Worker updates PostgreSQL status and emits completion events.
7. Logs, traces, and metrics are centralized in cloud monitoring.

Key design choices:
- Keep service-to-service communication private (VNet/VPC).
- Expose only API and frontend publicly.
- Keep DB, Redis, and MQ in private subnets/networks.

---

## 5. Environment Plan

Create separate environments in each cloud:
- dev: fast iteration, lower cost
- staging: production-like validation
- prod: high availability and tighter security

Naming convention example:
- Azure: je-dev-api, je-stg-worker, je-prd-postgres
- AWS: je-dev-api, je-stg-worker, je-prd-rds

Advice:
- Never share databases between staging and prod.
- Use separate credentials and secret scopes per environment.

---

## 6. Azure Deployment Plan (Step-by-Step)

## Phase 1: Foundation

1. Create subscription/resource groups:
- rg-je-dev
- rg-je-stg
- rg-je-prd

2. Create networking:
- VNet + subnets (app, data, management)
- NSGs with minimal inbound rules

3. Create shared services:
- ACR
- Key Vault
- Log Analytics workspace

## Phase 2: Data + Messaging

1. Deploy Azure Database for PostgreSQL Flexible Server
- Zone redundant for prod
- Private networking enabled
- Automated backups + retention policy

2. Deploy Azure Cache for Redis
- Enable TLS
- Configure memory/eviction policy by workload

3. Deploy RabbitMQ
- Preferred: CloudAMQP managed plan
- Alternative: RabbitMQ on hardened Linux VM

## Phase 3: Application Services

1. Build and push images to ACR:
- auth-api
- job-api
- execution-worker

2. Deploy Azure Container Apps
- Separate app per service
- Configure min/max replicas
- Enable Dapr only if needed (avoid extra complexity initially)

3. Configure environment variables/secrets
- Pull secrets from Key Vault references
- Set DB, Redis, MQ connection values

4. Frontend deployment
- Azure Static Web Apps with environment-specific API URL

## Phase 4: Edge + Security

1. Configure custom domains and TLS certs
2. Add WAF (Azure Front Door or Application Gateway) for prod APIs
3. Restrict admin endpoints and enable IP allowlists where possible

## Phase 5: Observability + Reliability

1. Enable Application Insights for all apps
2. Configure alerts:
- API 5xx rate
- Worker failures
- Queue depth spikes
- DB CPU/storage thresholds

3. Set autoscaling rules
- API on CPU/RPS
- Worker on queue depth and CPU

---

## 7. AWS Deployment Plan (Step-by-Step)

## Phase 1: Foundation

1. Create accounts/projects or strict environment isolation with IAM boundaries
2. Create VPC per environment with private/public subnets across 2+ AZs
3. Create IAM roles and policies (least privilege)

## Phase 2: Data + Messaging

1. Deploy Amazon RDS PostgreSQL
- Multi-AZ for prod
- Automated backups + retention
- Performance Insights enabled

2. Deploy ElastiCache Redis
- TLS in transit
- Auth token enabled

3. Deploy Amazon MQ (RabbitMQ)
- Private broker endpoints
- Durable queues + mirrored policy where needed

## Phase 3: Application Services

1. Build and push images to ECR
2. Deploy ECS Fargate services:
- auth-api service
- job-api service
- execution-worker service

3. Place API behind ALB
- HTTPS listener with ACM certificate
- Health checks for each target group

4. Frontend deployment
- Build React app
- Upload to S3
- Serve through CloudFront with OAC

## Phase 4: Secrets + Security

1. Store secrets in Secrets Manager
2. Use ECS task roles for service access
3. Restrict network paths with security groups and NACLs
4. Add AWS WAF on CloudFront or ALB for prod

## Phase 5: Observability + Reliability

1. Send logs to CloudWatch Logs
2. Create CloudWatch alarms:
- 5xx, latency, task restarts
- Queue depth
- DB CPU/connections/storage

3. Configure autoscaling:
- API on CPU/memory/request count
- Worker on queue depth + CPU

---

## 8. CI/CD Plan For Both Clouds

Use GitHub Actions with branch strategy:
- main -> prod
- dev -> dev
- release/* -> staging/prod approvals

Pipeline stages:
1. Restore dependencies and build
2. Run unit/integration tests
3. Build container images
4. Security scanning (SAST + dependency scan + image scan)
5. Push image to registry (ACR/ECR)
6. Deploy to target cloud environment
7. Run smoke tests
8. Rollback automatically if smoke tests fail

Advice:
- Keep the deployment manifest values environment-driven.
- Use OIDC federation from GitHub to cloud IAM (avoid long-lived secrets).

---

## 9. Security Baseline Checklist

- Use private networking for DB/Redis/MQ.
- Enforce TLS everywhere.
- Store secrets in Key Vault/Secrets Manager only.
- Enable managed identity (Azure) or task roles (AWS).
- Rotate credentials and API keys regularly.
- Enable audit logs and access logs.
- Add WAF in front of public entry points.
- Apply least privilege IAM/RBAC policies.

---

## 10. Cost and Reliability Advice

Start lean, then scale by metrics.

Cost control:
- Use smaller SKUs in dev/staging.
- Schedule non-prod scale-down off-hours.
- Set cost alerts and budget thresholds.
- Right-size Redis/RDS/PostgreSQL after 2 weeks of metrics.

Reliability:
- Run prod across multiple AZs.
- Set clear SLOs (availability, latency, processing time).
- Define RTO/RPO and test disaster recovery quarterly.
- Document runbooks for queue backlog, DB failover, and worker crash loops.

---

## 11. Recommended Rollout Timeline

Week 1:
- Finalize IaC modules and environment templates
- Deploy dev in Azure and AWS

Week 2:
- Deploy staging in one primary cloud
- Load test APIs and worker throughput

Week 3:
- Deploy production in primary cloud
- Stabilize monitoring, alerts, and runbooks

Week 4:
- Stand up secondary cloud as DR (active-passive)
- Validate backup restore and failover drills

---

## 12. Practical Final Recommendation

If your team is small or schedule is tight:
1. Choose one cloud as primary now.
2. Use managed services for DB/Redis/RabbitMQ.
3. Keep worker scaling tied to queue depth.
4. Add second cloud as disaster recovery only after stable production.

If you already have strong cloud operations capacity:
1. Run primary in Azure or AWS based on team expertise.
2. Keep replicated IaC in the second cloud from day one.
3. Activate warm standby in the second cloud with regular failover tests.

---

## 13. Implementation Checklist

- [ ] Confirm primary cloud (Azure or AWS)
- [ ] Finalize environment naming conventions
- [ ] Provision network + security baseline
- [ ] Provision PostgreSQL + Redis + RabbitMQ
- [ ] Deploy API and worker services
- [ ] Deploy frontend and DNS/TLS
- [ ] Configure CI/CD with rollback
- [ ] Configure monitoring and alerting
- [ ] Run load tests and failover drills
- [ ] Sign off production readiness

---

## 14. Optional Next Improvements

- Move from Container Apps/ECS to Kubernetes only if platform needs demand it
- Add blue/green deployments for lower-risk releases
- Add distributed tracing correlation across API -> MQ -> worker
- Add chaos testing for queue and DB outage scenarios
