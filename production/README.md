# Production all-in-one compose

This stack runs APIs + infrastructure + monitoring from prebuilt container images.

## 1) Configure environment

Edit `production/.env` and set:

- Application image tags (`APIGATEWAY_IMAGE`, `CATALOG_IMAGE`, `INVENTORY_IMAGE`, `ORDER_IMAGE`)
- Runtime secrets (`POSTGRES_PASSWORD`, `REDIS_PASSWORD`, `RABBITMQ_PASSWORD`, `GRAFANA_ADMIN_PASSWORD`)
- Host ports if needed.

## 2) Run

From repository root:

```powershell
docker compose --env-file .\production\.env -f .\production\docker-compose.yml up -d
```

or use helper script:

```powershell
.\production\run-all.ps1
```

Stop:

```powershell
docker compose --env-file .\production\.env -f .\production\docker-compose.yml down
```

or:

```powershell
.\production\run-all.ps1 -Down
```

## Run from Visual Studio (optional)

- Open `EShopMicroservices.sln` or `EShopMicroservices.slnx`.
- Select startup project: `EShop.Production.Compose`.
- Choose profile: `Docker Compose (Production All-In-One - No Debug)`.
- Use `Ctrl+F5` (recommended) to avoid debugger attach for production-like runs.

### Stop behavior in Visual Studio

- Because the production profile uses **StartWithoutDebugging** for all services, stopping from VS can leave containers running.
- Stop explicitly with:

```powershell
docker compose --env-file .\production\.env -f .\production\docker-compose.yml down --remove-orphans
```

## Notes

- `production/prometheus.yml` scrapes metrics over internal Docker network names.
- Grafana dashboards and datasources are provisioned from `monitoring/grafana`.
