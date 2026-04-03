# Production application stack

Runs **PostgreSQL, Redis, RabbitMQ, API Gateway, Catalog, Inventory, Order, Payment, and Shipping** from images you build or pull.

**Monitoring** is a separate project: `docker-composes/monitoring/docker-compose.yml`. Deploy production first so the shared Docker network exists, then start monitoring.

## 1) Configure environment

Edit `docker-composes/production/.env`:

- Application images (`APIGATEWAY_IMAGE`, …, `SHIPPING_IMAGE`)
- `ESHOP_APP_NETWORK_NAME` (must match `docker-composes/monitoring/.env`)
- Observability endpoints (`JAEGER_OTLP_ENDPOINT`, `TEMPO_OTLP_ENDPOINT`, `LOKI_ENDPOINT`) — defaults target monitoring container names on the shared network
- Secrets: `POSTGRES_PASSWORD`, `REDIS_PASSWORD`, `RABBITMQ_PASSWORD`

## 2) Run

From repository root:

```powershell
docker compose --env-file .\docker-composes\production\.env -f .\docker-composes\production\docker-compose.yml up -d
```

Or:

```powershell
cd docker-composes\production
.\run-all.ps1
```

## 3) Monitoring (optional, separate)

```powershell
cd docker-composes\monitoring
.\run-all.ps1
```

See `docker-composes/monitoring/docker-compose.yml` for image builds (Coolify: set build context to **`docker-composes/monitoring`**, dockerfile e.g. `images/prometheus/Dockerfile`).

## Notes

- Postgres init script: `docker-composes/postgres/01-create-databases.sql`
- Data volumes: `postgres_data`, `redis_data`, `rabbitmq_data`
