param(
    [switch]$Down,
    [switch]$Logs,
    [switch]$Status,
    [switch]$Rebuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$composeFile = Join-Path $scriptDir "docker-compose.yml"
$envFile = Join-Path $scriptDir ".env"

if (-not (Test-Path $composeFile)) {
    throw "Cannot find docker-compose.yml at '$composeFile'."
}

if (-not (Test-Path $envFile)) {
    throw "Cannot find .env file at '$envFile'."
}

function Assert-DockerReady {
    try {
        docker info | Out-Null
    }
    catch {
        throw "Docker daemon is not available. Start Docker Desktop first."
    }
}

function Invoke-Compose([string[]]$ComposeArgs) {
    docker compose --env-file $envFile -f $composeFile @ComposeArgs
}

Assert-DockerReady
Push-Location $repoRoot
try {
    if ($Down) {
        Write-Host "Stopping development stack..." -ForegroundColor Yellow
        Invoke-Compose @("down", "--remove-orphans")
        Write-Host "Development stack stopped." -ForegroundColor Green
        exit 0
    }

    if ($Logs) {
        Write-Host "Streaming development logs..." -ForegroundColor Cyan
        Invoke-Compose @("logs", "-f", "--tail", "200")
        exit 0
    }

    if ($Status) {
        Invoke-Compose @("ps")
        exit 0
    }

    if ($Rebuild) {
        Write-Host "Rebuilding and starting development stack..." -ForegroundColor Yellow
        Invoke-Compose @("up", "-d", "--build")
    }
    else {
        Write-Host "Starting development stack..." -ForegroundColor Yellow
        Invoke-Compose @("up", "-d")
    }

    Write-Host ""
    Write-Host "Development stack is running:" -ForegroundColor Green
    Write-Host "  APIGateway: http://localhost:5000"
    Write-Host "  Grafana:    http://localhost:3000"
    Write-Host "  Prometheus: http://localhost:9090"
    Write-Host "  Jaeger:     http://localhost:16686"
}
finally {
    Pop-Location
}
