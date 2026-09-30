[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$EvidencePath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

if (-not (Test-Path -LiteralPath $EvidencePath -PathType Leaf)) {
    throw "Browser qualification evidence '$EvidencePath' does not exist."
}

$raw = [System.IO.File]::ReadAllText((Resolve-Path -LiteralPath $EvidencePath))
$evidence = $raw | ConvertFrom-Json

if ($null -eq $evidence) {
    throw "Browser qualification evidence is empty or invalid JSON."
}

if ($evidence.schemaVersion -ne 1) {
    throw "Unsupported browser qualification evidence schema version '$($evidence.schemaVersion)'."
}

$requiredChecks = @(
    "workspace",
    "create",
    "template",
    "templateForgery",
    "overrides",
    "domainForgery",
    "effectiveVersion",
    "concurrency",
    "lifecycle",
    "identityBoundary"
)

if ($null -eq $evidence.checks) {
    throw "Browser qualification evidence has no checks object."
}

$properties = @($evidence.checks.PSObject.Properties)

foreach ($check in $requiredChecks) {
    $property = @($properties | Where-Object { $_.Name -eq $check })
    if ($property.Count -ne 1) {
        throw "Browser qualification evidence is missing required check '$check'."
    }

    if ($property[0].Value -ne $true) {
        throw "Browser qualification check '$check' did not pass."
    }
}

if ($evidence.passed -ne $true) {
    throw "Browser qualification evidence is not marked as passed."
}

Write-Host "OrganisationProfile browser qualification evidence: GREEN"
