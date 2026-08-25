param(
    [ValidateSet("smoke", "load")]
    [string]$Profile = "smoke",

    [string]$BaseUrl = "http://host.docker.internal:8080",

    [string]$UserEmail = "viewer.demo@portmanagement.local"
)

$ErrorActionPreference = "Stop"
$k6Image = "grafana/k6:2.2.0@sha256:9bd01d6941fca969cb61bb57d2da5ee9b385fe2aa8881df3798c196564d6ace6"

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw "Docker não foi encontrado. Instale ou inicie o Docker Desktop."
}

if ([string]::IsNullOrWhiteSpace($env:DEMO_USER_PASSWORD)) {
    throw "Defina DEMO_USER_PASSWORD com a mesma senha usada no ambiente Docker."
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$testDirectory = Join-Path $repositoryRoot "tests\performance"
$resultDirectory = Join-Path $repositoryRoot "TestResults\performance"
$summaryFile = "/results/$Profile-summary.json"

New-Item -ItemType Directory -Path $resultDirectory -Force | Out-Null

$previousK6Password = $env:K6_USER_PASSWORD
$env:K6_USER_PASSWORD = $env:DEMO_USER_PASSWORD

try {
    & docker run --rm `
        --env "K6_BASE_URL=$BaseUrl" `
        --env "K6_PROFILE=$Profile" `
        --env "K6_USER_EMAIL=$UserEmail" `
        --env K6_USER_PASSWORD `
        --volume "${testDirectory}:/scripts:ro" `
        --volume "${resultDirectory}:/results" `
        $k6Image `
        run `
        "--summary-export=$summaryFile" `
        /scripts/port-management.js

    if ($LASTEXITCODE -ne 0) {
        throw "O perfil '$Profile' não atingiu os limites de desempenho definidos."
    }
}
finally {
    if ($null -eq $previousK6Password) {
        Remove-Item Env:K6_USER_PASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:K6_USER_PASSWORD = $previousK6Password
    }
}
