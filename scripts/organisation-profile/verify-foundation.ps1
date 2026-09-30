param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$sourceRoot = Join-Path $root "src"
$profileSourceRoots = @(
    (Join-Path $sourceRoot "OrganisationProfile.Domain"),
    (Join-Path $sourceRoot "OrganisationProfile.Application")
)

$probeProject = Join-Path $root "tests\OrganisationProfile.FoundationProbe\OrganisationProfile.FoundationProbe.csproj"

function Require-File {
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path $Path)) {
        throw "Required OrganisationProfile foundation file '$Path' was not found."
    }
}

function Assert-SourceLayout {
    param([Parameter(Mandatory = $true)][string]$Directory)

    $typePattern = '^\s*(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|ref|unsafe|new)\s+)*(?:record\s+(?:class\s+|struct\s+)?|class\s+|struct\s+|interface\s+|enum\s+)(?<name>[A-Za-z_][A-Za-z0-9_]*)'

    foreach ($file in Get-ChildItem $Directory -Filter *.cs -Recurse) {
        $content = [System.IO.File]::ReadAllText($file.FullName)

        if ($content -match '^\s*namespace\s+[\w.]+\s*;\s*$') {
            throw "File-scoped namespace is not allowed in '$($file.FullName)'."
        }

        $matches = [regex]::Matches(
            $content,
            $typePattern,
            [System.Text.RegularExpressions.RegexOptions]::Multiline)

        if ($matches.Count -ne 1) {
            throw "'$($file.FullName)' must declare exactly one top-level type."
        }

        $declaredName = $matches[0].Groups["name"].Value
        if ($file.BaseName -ne $declaredName) {
            throw "'$($file.FullName)' must match declared type '$declaredName'."
        }
    }
}

Require-File $probeProject

foreach ($directory in $profileSourceRoots) {
    if (-not (Test-Path $directory)) {
        throw "Required OrganisationProfile source directory '$directory' was not found."
    }

    Assert-SourceLayout $directory
}

$allSource = ($profileSourceRoots | ForEach-Object {
    Get-ChildItem $_ -Filter *.cs -Recurse | ForEach-Object {
        [System.IO.File]::ReadAllText($_.FullName)
    }
}) -join "`n"

foreach ($forbidden in @(
    "BusinessProfile",
    "Shopify",
    "Stripe",
    "Xero",
    "Credential",
    "Password",
    "Secret"
)) {
    if ($allSource.Contains($forbidden)) {
        throw "OrganisationProfile foundation source contains forbidden cross-boundary concept '$forbidden'."
    }
}

$applicationProject = [System.IO.File]::ReadAllText(
    (Join-Path $sourceRoot "OrganisationProfile.Application\OrganisationProfile.Application.csproj"))

foreach ($forbiddenDependency in @(
    "IdentityAccess.",
    "OrganizationDirectory."
)) {
    if ($applicationProject.Contains($forbiddenDependency)) {
        throw "OrganisationProfile Application must use narrow adapter contracts instead of '$forbiddenDependency' project dependencies."
    }
}

if ($allSource.Contains("OrganisationProfileManager")) {
    throw "God-service name OrganisationProfileManager is forbidden."
}

Write-Host "Restoring OrganisationProfile foundation probe..."
dotnet restore $probeProject
if (-not $?) { throw "OrganisationProfile foundation restore failed." }

Write-Host "Building OrganisationProfile foundation..."
dotnet build $probeProject -c $Configuration --no-restore
if (-not $?) { throw "OrganisationProfile foundation build failed." }

Write-Host "Running OrganisationProfile foundation contract probe..."
dotnet run --project $probeProject -c $Configuration --no-build
if (-not $?) { throw "OrganisationProfile foundation contract probe failed." }

Write-Host "OrganisationProfile Pack 1 verification: GREEN"
