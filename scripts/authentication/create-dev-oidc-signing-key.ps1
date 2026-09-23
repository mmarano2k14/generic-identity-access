[CmdletBinding()]
param(
    [string]$OutputPath = ".\secrets\oidc-dev-signing-key.pem",
    [int]$KeySize = 3072
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if ($KeySize -lt 2048 -or $KeySize -gt 16384) {
    throw "OIDC RSA signing keys must be between 2048 and 16384 bits."
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet was not found on PATH."
}

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$generatorProject = Join-Path $root 'tools\IdentityAccess.DevOidcSigningKey\IdentityAccess.DevOidcSigningKey.csproj'
$generatorDll = Join-Path $root 'tools\IdentityAccess.DevOidcSigningKey\bin\Release\net10.0\IdentityAccess.DevOidcSigningKey.dll'
$fullPath = [System.IO.Path]::GetFullPath($OutputPath)

& dotnet build $generatorProject -c Release --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    throw "Development OIDC signing-key generator build failed."
}

& dotnet $generatorDll $fullPath $KeySize
if ($LASTEXITCODE -ne 0) {
    throw "Development OIDC signing-key generation failed."
}
if (-not (Test-Path -LiteralPath $fullPath)) {
    throw "Development OIDC signing-key generator did not create the expected PEM file."
}

Write-Host "Development OIDC signing key created:"
Write-Host $fullPath
Write-Host "Do not commit private signing keys to source control."
