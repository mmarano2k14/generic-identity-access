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
    (Join-Path $PSScriptRoot "validate-permission-persistence.sql"))

Write-Host "Validating permission-policy persistence isolation in '$databaseName'..."
& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -f $validationFile
if ($LASTEXITCODE -ne 0) {
    throw "Permission-policy persistence validation failed with exit code $LASTEXITCODE."
}
Write-Host "Permission-policy persistence validation passed."
