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
    (Join-Path $PSScriptRoot "validate-assigned-capability-projection.sql"))

$requiredManagedTables = @(
    "identity_access.managed_policies",
    "identity_access.managed_policy_versions",
    "identity_access.managed_policy_statements",
    "identity_access.managed_group_policy_bindings"
)

$requiredTableValues = ($requiredManagedTables | ForEach-Object {
    "('" + $_.Replace("'", "''") + "')"
}) -join ","

$missingManagedTables = & psql `
    -U $postgresUser `
    -v ON_ERROR_STOP=1 `
    -d $databaseName `
    -At `
    -c "SELECT required_table FROM (VALUES $requiredTableValues) AS required(required_table) WHERE to_regclass(required_table) IS NULL ORDER BY required_table;"

if ($LASTEXITCODE -ne 0) {
    throw "Managed-policy schema preflight failed with exit code $LASTEXITCODE."
}

if ($missingManagedTables) {
    $missingText = ($missingManagedTables | ForEach-Object { $_.Trim() } | Where-Object { $_ }) -join ", "
    throw "Managed-policy schema is not current. Missing: $missingText. Run .\scripts\postgresql\apply-default-schema.ps1 from the repository root, then rerun this gate."
}

Write-Host "Validating assigned-capability projection in '$databaseName'..."
& psql -U $postgresUser -v ON_ERROR_STOP=1 -d $databaseName -f $validationFile
if ($LASTEXITCODE -ne 0) {
    throw "Assigned-capability projection validation failed with exit code $LASTEXITCODE."
}
Write-Host "Assigned-capability projection validation passed."
