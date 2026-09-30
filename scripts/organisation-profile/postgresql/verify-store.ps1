param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "common.ps1")

$root =
    Resolve-Path (Join-Path $PSScriptRoot "..\..\..")

$probe =
    Join-Path $root "tests\OrganisationProfile.PostgreSqlProbe\OrganisationProfile.PostgreSqlProbe.csproj"

if (-not (Test-Path $probe)) {
    throw "OrganisationProfile PostgreSQL probe project is missing."
}

$connectionString =
    Get-OrganisationProfileProbeConnectionString

$previous =
    $env:ORGANISATION_PROFILE_POSTGRES_DEFAULT

try {
    $env:ORGANISATION_PROFILE_POSTGRES_DEFAULT =
        $connectionString

    dotnet run `
        --project $probe `
        -c $Configuration

    if (-not $?) {
        throw "OrganisationProfile PostgreSQL persistence probe failed."
    }
}
finally {
    if ($null -eq $previous) {
        Remove-Item Env:ORGANISATION_PROFILE_POSTGRES_DEFAULT `
            -ErrorAction SilentlyContinue
    } else {
        $env:ORGANISATION_PROFILE_POSTGRES_DEFAULT =
            $previous
    }
}

Write-Host "OrganisationProfile PostgreSQL store verification: GREEN"
