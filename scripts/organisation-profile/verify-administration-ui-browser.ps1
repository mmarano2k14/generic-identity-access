[CmdletBinding()]
param(
    [string]$AdminBaseUrl = "http://127.0.0.1:3000",
    [string]$EvidencePath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
    $directory = Join-Path $root "artifacts/qualification"
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $EvidencePath = Join-Path $directory "organisation-profile-ui-$stamp.json"
}

$checks = @(
    [ordered]@{ key = "workspace"; prompt = "Open /organisations/{organizationId}/profile in one authorized tenant and confirm the existing Organization is shown read-only" },
    [ordered]@{ key = "create"; prompt = "For an Organization without a profile, create an unpinned OrganisationProfile and confirm Organization identity is unchanged" },
    [ordered]@{ key = "template"; prompt = "Select an active published template version and confirm the pinned template/version are displayed after reload" },
    [ordered]@{ key = "templateForgery"; prompt = "Forge or alter a template selection to an unavailable version and confirm the server rejects it" },
    [ordered]@{ key = "overrides"; prompt = "Replace domain overrides using catalog-derived selections and confirm the complete override set reloads correctly" },
    [ordered]@{ key = "domainForgery"; prompt = "Forge an unpublished domain/version Enable selection and confirm the server rejects it" },
    [ordered]@{ key = "effectiveVersion"; prompt = "Resolve the effective profile and confirm immutable version/domain pins/content hash are displayed" },
    [ordered]@{ key = "concurrency"; prompt = "Submit a stale RowVersion mutation and confirm it is rejected without overwriting current state" },
    [ordered]@{ key = "lifecycle"; prompt = "Disable and re-enable the profile and confirm Organization identity/membership/scope links are unchanged" },
    [ordered]@{ key = "identityBoundary"; prompt = "Confirm OrganisationProfile is absent from the /identity navigation and the profile workspace exposes no Organization edit controls" }
)

$results = [ordered]@{}
Write-Host ""
Write-Host "OrganisationProfile Pack 7 browser qualification"
Write-Host "Use the real browser at $AdminBaseUrl and answer each gate after exercising it."
Write-Host ""

foreach ($check in $checks) {
    while ($true) {
        $answer = (Read-Host "$($check.prompt) [y/n]").Trim().ToLowerInvariant()
        if ($answer -in @("y", "yes")) {
            $results[$check.key] = $true
            break
        }
        if ($answer -in @("n", "no")) {
            $results[$check.key] = $false
            break
        }
        Write-Host "Enter y or n."
    }
}

$failed = @($results.GetEnumerator() | Where-Object { -not $_.Value } | ForEach-Object { $_.Key })
$evidence = [ordered]@{
    schemaVersion = 1
    qualifiedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    adminBaseUrl = $AdminBaseUrl
    checks = $results
    passed = ($failed.Count -eq 0)
    failedChecks = $failed
}

$evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
Write-Host "Browser qualification evidence written: $EvidencePath"

if ($failed.Count -gt 0) {
    throw "OrganisationProfile Pack 7 browser qualification failed: $($failed -join ', ')"
}

Write-Host "OrganisationProfile Pack 7 browser qualification passed."
