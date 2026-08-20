$ErrorActionPreference = 'Stop'

$requirements = @(
    @{ Name = '.NET SDK 10'; Command = 'dotnet'; VersionArguments = @('--version') },
    @{ Name = 'Node.js'; Command = 'node'; VersionArguments = @('--version') },
    @{ Name = 'npm'; Command = 'npm.cmd'; VersionArguments = @('--version') },
    @{ Name = 'Docker'; Command = 'docker'; VersionArguments = @('--version') }
)

$failed = $false

foreach ($requirement in $requirements) {
    $command = Get-Command $requirement.Command -ErrorAction SilentlyContinue

    if ($null -eq $command) {
        Write-Host "[FALTA] $($requirement.Name)" -ForegroundColor Red
        $failed = $true
        continue
    }

    $version = & $requirement.Command @($requirement.VersionArguments) 2>$null
    Write-Host "[OK] $($requirement.Name): $version" -ForegroundColor Green
}

if ($failed) {
    Write-Error 'Instale os requisitos ausentes antes de executar a aplicação completa.'
}

