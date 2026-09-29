param([ValidateSet("Debug", "Release")][string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$testProject = Join-Path $root "tests\OrganizationDirectory.Tests\OrganizationDirectory.Tests.csproj"
$probeProject = Join-Path $root "tests\OrganizationDirectory.PostgreSqlProbe\OrganizationDirectory.PostgreSqlProbe.csproj"
& dotnet restore $testProject
if ($LASTEXITCODE -ne 0) { throw "Organization Directory restore failed." }
& dotnet build $testProject -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "Organization Directory build failed." }
& dotnet test $testProject -c $Configuration --no-build --no-restore
if ($LASTEXITCODE -ne 0) { throw "Organization Directory tests failed." }
& dotnet restore $probeProject
if ($LASTEXITCODE -ne 0) { throw "Organization Directory probe restore failed." }
& dotnet build $probeProject -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { throw "Organization Directory probe build failed." }
$separationScript = Join-Path $root "scripts\organization-directory\qualification\verify-separation.ps1"
if (-not (Test-Path $separationScript)) { throw "Organization Directory separation verification script is missing." }
& $separationScript
if ($LASTEXITCODE -ne 0) { throw "Organization Directory separation verification failed." }
Write-Host "Organization Directory repository verification: GREEN"
