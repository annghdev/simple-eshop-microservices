param(
    [switch]$Down,
    [switch]$Logs,
    [switch]$Status
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
        Write-Host "Stopping production stack..." -ForegroundColor Yellow
        Invoke-Compose @("down", "--remove-orphans")
        Write-Host "Production stack stopped." -ForegroundColor Green
        exit 0
    }

    if ($Logs) {
        Write-Host "Streaming production logs..." -ForegroundColor Cyan
        Invoke-Compose @("logs", "-f", "--tail", "200")
        exit 0
    }

    if ($Status) {
        Invoke-Compose @("ps")
        exit 0
    }

    Write-Host "Starting production stack..." -ForegroundColor Yellow
    Invoke-Compose @("up", "-d")

    Write-Host ""
    Write-Host "Production stack is running." -ForegroundColor Green
}
finally {
    Pop-Location
}
