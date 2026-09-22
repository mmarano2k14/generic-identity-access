$ErrorActionPreference = "Stop"

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql was not found on PATH."
}

$databaseName = if ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) {
    $env:IDENTITY_ACCESS_POSTGRES_DATABASE
} else {
    "generic_identity_access_default"
}

$postgresUser = if ($env:IDENTITY_ACCESS_POSTGRES_USER) {
    $env:IDENTITY_ACCESS_POSTGRES_USER
} else {
    "postgres"
}

$validationFile = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "validate-oidc-refresh-token.sql"))

Write-Host "Validating OIDC refresh-token rotation invariants in '$databaseName'..."

& psql `
    -U $postgresUser `
    -v ON_ERROR_STOP=1 `
    -d $databaseName `
    -f $validationFile

if ($LASTEXITCODE -ne 0) {
    throw "OIDC refresh-token validation failed with exit code $LASTEXITCODE."
}

Write-Host "OIDC refresh-token validation passed."
