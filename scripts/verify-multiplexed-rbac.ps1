param(
    [Parameter(Mandatory = $true)]
    [string]$ReferenceDirectory
)

$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "verify-authorization-source-consistency.ps1")
$reference = (Resolve-Path $ReferenceDirectory).Path
$core = Join-Path $reference "Multiplexed.Rbac.Core.dll"
$abstractions = Join-Path $reference "Multiplexed.Abstractions.dll"

if (-not (Test-Path $core)) { throw "Multiplexed.Rbac.Core.dll not found in $reference" }
if (-not (Test-Path $abstractions)) { throw "Multiplexed.Abstractions.dll not found in $reference" }

$previous = $env:MULTIPLEXED_RBAC_REFERENCE_DIR
try {
    $env:MULTIPLEXED_RBAC_REFERENCE_DIR = $reference
    dotnet test .\tests\IdentityAccess.Rbac.MultiplexedIntegrationTests\IdentityAccess.Rbac.MultiplexedIntegrationTests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { throw "External RBAC compatibility tests failed." }
}
finally {
    if ($null -eq $previous) {
        Remove-Item Env:MULTIPLEXED_RBAC_REFERENCE_DIR -ErrorAction SilentlyContinue
    } else {
        $env:MULTIPLEXED_RBAC_REFERENCE_DIR = $previous
    }
}
