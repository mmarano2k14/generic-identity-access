$ErrorActionPreference = "Stop"
if (-not $env:IDENTITY_ACCESS_POSTGRES_DEFAULT -and -not $env:ORGANIZATION_DIRECTORY_POSTGRES_DEFAULT) { throw "IDENTITY_ACCESS_POSTGRES_DEFAULT is required for the shared-database Organization Directory store probe." }
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$project = Join-Path $root "tests\OrganizationDirectory.PostgreSqlProbe\OrganizationDirectory.PostgreSqlProbe.csproj"
& dotnet restore $project
if ($LASTEXITCODE -ne 0) { throw "Organization Directory PostgreSQL probe restore failed." }
& dotnet run --project $project --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Organization Directory PostgreSQL store probe failed." }
