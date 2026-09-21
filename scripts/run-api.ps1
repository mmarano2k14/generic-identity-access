[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    & dotnet run --project src/IdentityAccess.Api --launch-profile http
    if ($LASTEXITCODE -ne 0) { throw "API exited with a non-zero status." }
} finally {
    Pop-Location
}
