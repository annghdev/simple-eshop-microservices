# Development all-in-one compose

This stack runs all APIs, infrastructure, and monitoring in one Docker Compose project for local development.

## Run with Docker Compose

From repository root:

```powershell
docker compose --env-file .\development\.env -f .\development\docker-compose.yml up -d --build
```

or use helper script:

```powershell
.\development\run-all.ps1 -Rebuild
```

Stop:

```powershell
docker compose --env-file .\development\.env -f .\development\docker-compose.yml down
```

or:

```powershell
.\development\run-all.ps1 -Down
```

## Run from Visual Studio (1-click)

- Open `EShopMicroservices.sln`.
- Select startup project: `EShop.Development.Compose`.
- Choose profile: `Docker Compose (Development All-In-One)`.
- Press F5.

### Stop behavior in Visual Studio

- If you run with **F5 (debug)**, use **Shift+F5** to stop.
- If you run with **Ctrl+F5 (without debug)**, containers can remain running by design. In that case, run:

```powershell
docker compose --env-file .\development\.env -f .\development\docker-compose.yml down --remove-orphans
```


## Default endpoints

- APIGateway: `http://localhost:5000`
- Catalog API: `http://localhost:5001`
- Inventory API: `http://localhost:5002`
- Order API: `http://localhost:5003`
- Grafana: `http://localhost:3000`
- Prometheus: `http://localhost:9090`
- Jaeger: `http://localhost:16686`
