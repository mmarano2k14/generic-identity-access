[CmdletBinding()]
param([ValidateSet("Debug", "Release")][string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 10 SDK is required. Run dotnet --list-sdks after installation."
}
Push-Location $root
try {
    & dotnet --info
    if ($LASTEXITCODE -ne 0) { throw "SDK check failed." }
    & dotnet restore IdentityAccess.sln
    if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed." }
    & dotnet build IdentityAccess.sln --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
    & dotnet test IdentityAccess.sln --configuration $Configuration --no-build --no-restore `
        --logger "trx;LogFileName=identity-access.trx" --results-directory (Join-Path $root "artifacts/test-results")
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
} finally {
    Pop-Location
}
