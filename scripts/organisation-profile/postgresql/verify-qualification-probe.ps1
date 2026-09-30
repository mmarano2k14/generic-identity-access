param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

. (Join-Path $PSScriptRoot "common.ps1")

$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..")
$probe = Join-Path $root "tests\OrganisationProfile.QualificationProbe\OrganisationProfile.QualificationProbe.csproj"

if (-not (Test-Path -LiteralPath $probe -PathType Leaf)) {
    throw "OrganisationProfile qualification probe project is missing."
}

$connectionString = Get-OrganisationProfileProbeConnectionString
$previous = $env:ORGANISATION_PROFILE_POSTGRES_DEFAULT

try {
    $env:ORGANISATION_PROFILE_POSTGRES_DEFAULT = $connectionString

    dotnet run `
        --project $probe `
        -c $Configuration `
        --no-build `
        --no-restore

    if ($LASTEXITCODE -ne 0) {
        throw "OrganisationProfile qualification/adversarial probe failed."
    }
}
finally {
    if ($null -eq $previous) {
        Remove-Item Env:ORGANISATION_PROFILE_POSTGRES_DEFAULT -ErrorAction SilentlyContinue
    } else {
        $env:ORGANISATION_PROFILE_POSTGRES_DEFAULT = $previous
    }
}

Write-Host "OrganisationProfile qualification/adversarial probe: GREEN"
