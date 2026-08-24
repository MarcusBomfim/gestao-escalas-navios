[CmdletBinding()]
param(
    [string]$Solution = "backend/PortManagement.slnx"
)

$ErrorActionPreference = "Stop"

$output = & dotnet list $Solution package `
    --vulnerable `
    --include-transitive `
    --format json `
    --no-restore 2>&1

if ($LASTEXITCODE -ne 0) {
    $output | ForEach-Object { Write-Error $_ }
    exit $LASTEXITCODE
}

try {
    $report = ($output -join [Environment]::NewLine) | ConvertFrom-Json
}
catch {
    Write-Error "A resposta da auditoria NuGet nao contem JSON valido."
    exit 1
}

$findings = foreach ($project in @($report.projects)) {
    foreach ($framework in @($project.frameworks)) {
        foreach ($collectionName in @("topLevelPackages", "transitivePackages")) {
            foreach ($package in @($framework.$collectionName)) {
                foreach ($vulnerability in @($package.vulnerabilities)) {
                    if ($null -eq $vulnerability) {
                        continue
                    }

                    [PSCustomObject]@{
                        Project = $project.path
                        Framework = $framework.framework
                        Package = $package.id
                        Version = $package.resolvedVersion
                        Severity = $vulnerability.severity
                        Advisory = $vulnerability.advisoryUrl
                    }
                }
            }
        }
    }
}

if (@($findings).Count -gt 0) {
    $findings | Format-Table -AutoSize | Out-String | Write-Host
    Write-Error "Foram encontradas dependencias NuGet vulneraveis."
    exit 1
}

Write-Host "Nenhuma vulnerabilidade conhecida foi encontrada nas dependencias NuGet."
