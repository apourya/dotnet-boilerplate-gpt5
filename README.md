# EnterpriseBoilerplate (.NET 8)

- Clean Architecture + DDD + CQRS + MediatR
- EF Core (Write) + Dapper (Read) + Redis cache + Mongo (read projection)
- RabbitMQ EventBus + Transactional Outbox + Idempotent Consumer (Worker)
- JWT Auth + Serilog + OpenTelemetry + Prometheus + Health Checks
- Dockerfiles + docker-compose + GitHub Actions CI

Quick start:
- Install .NET 8 and Docker Desktop
- docker compose up -d
- dotnet run --project src/EnterpriseBoilerplate.Api
- Swagger: http://localhost:8080/swagger
- Metrics: http://localhost:8080/metrics
- Health: http://localhost:8080/health

Default admin after first run (dev):
- username: admin
- password: Admin123!

