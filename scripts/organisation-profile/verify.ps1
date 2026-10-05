param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [switch]$SkipPostgreSql
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$solution = Join-Path $root "IdentityAccess.sln"

if (-not (Test-Path -LiteralPath $solution -PathType Leaf)) {
    throw "IdentityAccess.sln was not found."
}

Write-Host "Restoring integrated .NET solution..."
dotnet restore $solution
if ($LASTEXITCODE -ne 0) {
    throw "Integrated solution restore failed."
}

Write-Host "Building integrated .NET solution..."
dotnet build `
    $solution `
    -c $Configuration `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "Integrated solution build failed."
}

Write-Host "Running integrated .NET test suite..."
dotnet test `
    $solution `
    -c $Configuration `
    --no-build `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "Integrated .NET test suite failed."
}

Write-Host "Running OrganisationProfile foundation probe..."
dotnet run `
    --project (Join-Path $root "tests\OrganisationProfile.FoundationProbe\OrganisationProfile.FoundationProbe.csproj") `
    -c $Configuration `
    --no-build `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "OrganisationProfile foundation probe failed."
}

Write-Host "Running OrganisationProfile composition separation validation..."
& (Join-Path $PSScriptRoot "verify-composition-separation.ps1")
if (-not $?) {
    throw "OrganisationProfile composition separation validation failed."
}

Write-Host "Running OrganisationProfile security integration validation..."
& (Join-Path $PSScriptRoot "verify-security-integration.ps1")
if (-not $?) {
    throw "OrganisationProfile security integration validation failed."
}

Write-Host "Running OrganisationProfile API + TypeScript SDK validation..."
& (Join-Path $PSScriptRoot "verify-api-sdk-integration.ps1")
if (-not $?) {
    throw "OrganisationProfile API + TypeScript SDK validation failed."
}

Write-Host "Running OrganisationProfile administration UI validation..."
& (Join-Path $PSScriptRoot "verify-administration-ui.ps1")
if (-not $?) {
    throw "OrganisationProfile administration UI validation failed."
}

Write-Host "Running OrganisationProfile Qualification and Hardening source hardening validation..."
& (Join-Path $PSScriptRoot "verify-qualification-source.ps1")
if (-not $?) {
    throw "OrganisationProfile Qualification and Hardening source hardening validation failed."
}

if ($SkipPostgreSql) {
    Write-Warning "PostgreSQL checks skipped. OrganisationProfile Qualification and Hardening verification is PARTIAL."
    return
}

& (Join-Path $PSScriptRoot "postgresql\verify-migration-integrity.ps1")
if (-not $?) {
    throw "OrganisationProfile migration integrity failed."
}

& (Join-Path $PSScriptRoot "postgresql\verify-schema.ps1")
if (-not $?) {
    throw "OrganisationProfile PostgreSQL schema verification failed."
}

& (Join-Path $PSScriptRoot "postgresql\verify-template-catalog.ps1")
if (-not $?) {
    throw "OrganisationProfile template catalog schema verification failed."
}

& (Join-Path $PSScriptRoot "postgresql\verify-effective-composition.ps1")
if (-not $?) {
    throw "OrganisationProfile effective composition schema verification failed."
}

& (Join-Path $PSScriptRoot "postgresql\verify-store.ps1") `
    -Configuration $Configuration

if (-not $?) {
    throw "OrganisationProfile PostgreSQL store verification failed."
}

Write-Host "Running OrganisationProfile template catalog probe..."
dotnet run `
    --project (Join-Path $root "tests\OrganisationProfile.TemplateCatalogProbe\OrganisationProfile.TemplateCatalogProbe.csproj") `
    -c $Configuration `
    --no-build `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "OrganisationProfile template catalog probe failed."
}

Write-Host "Running OrganisationProfile effective composition probe..."
dotnet run `
    --project (Join-Path $root "tests\OrganisationProfile.CompositionProbe\OrganisationProfile.CompositionProbe.csproj") `
    -c $Configuration `
    --no-build `
    --no-restore

if ($LASTEXITCODE -ne 0) {
    throw "OrganisationProfile effective composition probe failed."
}

Write-Host "Running OrganisationProfile adversarial qualification probe..."
& (Join-Path $PSScriptRoot "postgresql\verify-qualification-probe.ps1") `
    -Configuration $Configuration

if (-not $?) {
    throw "OrganisationProfile adversarial qualification probe failed."
}

Write-Host "OrganisationProfile Qualification and Hardening automated verification: GREEN"
