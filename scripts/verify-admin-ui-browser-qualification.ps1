[CmdletBinding()]
param(
    [string]$AdminBaseUrl = "http://127.0.0.1:3000",
    [string]$ApiBaseUrl = "http://127.0.0.1:5080",
    [string]$EvidencePath,
    [switch]$SkipReachability
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($EvidencePath)) {
    $evidenceDirectory = Join-Path $root "artifacts/qualification"
    New-Item -ItemType Directory -Force -Path $evidenceDirectory | Out-Null
    $stamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $EvidencePath = Join-Path $evidenceDirectory "admin-ui-e2e-$stamp.json"
}
else {
    $EvidencePath = [IO.Path]::GetFullPath($EvidencePath)
    $parent = Split-Path -Parent $EvidencePath
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}

function Assert-Reachable([string]$uri, [string]$label) {
    try {
        $response = Invoke-WebRequest -Uri $uri -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 10 -ErrorAction Stop
        if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 400) {
            throw "$label returned HTTP $($response.StatusCode)."
        }
        Write-Host "$label reachable: $uri"
    }
    catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -ge 300 -and [int]$_.Exception.Response.StatusCode -lt 400) {
            Write-Host "$label reachable with redirect: $uri"
            return
        }
        throw "$label is not reachable at $uri. $($_.Exception.Message)"
    }
}

if (-not $SkipReachability) {
    Assert-Reachable "$ApiBaseUrl/.well-known/openid-configuration" "OIDC discovery"
    Assert-Reachable "$AdminBaseUrl/login" "Admin login"
}

$checks = @(
    [ordered]@{ key = "login"; prompt = "OIDC sign-in completes and returns to the protected administration UI without a redirect loop" },
    [ordered]@{ key = "tenantList"; prompt = "Identity-scope administration shows the authorized tenant list with member counts" },
    [ordered]@{ key = "emptyTenant"; prompt = "A tenant can be created with 0 members and opened before any user is added" },
    [ordered]@{ key = "groupTemplates"; prompt = "Inside a tenant, groups show Template Yes/No and there is no separate Available group templates catalogue" },
    [ordered]@{ key = "tenantGroupCreate"; prompt = "An authorized actor can create a normal tenant group and Create from template clones only the reusable group policy definition" },
    [ordered]@{ key = "safeAddMember"; prompt = "Add member follows the actor scope: scope-wide search for super admin, exact-login/no global enumeration for tenant-scoped administration" },
    [ordered]@{ key = "manageGroups"; prompt = "Manage groups assigns only existing real tenant groups, including a reusable group when it exists in that tenant" },
    [ordered]@{ key = "templateAuthority"; prompt = "Only scope administration can mark/unmark a group as Template or mutate the policy definition of a reusable group" },
    [ordered]@{ key = "tenantIsolation"; prompt = "A tenant-scoped subject cannot enumerate or mutate another tenant's members/groups" },
    [ordered]@{ key = "allowDeny"; prompt = "Removing/restoring the relevant group or managed binding produces the expected DENY/ALLOW behavior on a protected operation" },
    [ordered]@{ key = "sessions"; prompt = "Sessions workspace loads under the authenticated admin context and session revocation remains protected" },
    [ordered]@{ key = "mfa"; prompt = "MFA workspace loads under the authenticated admin context and sensitive MFA administration remains protected" },
    [ordered]@{ key = "logout"; prompt = "Logout clears the admin session and protected pages require authentication again" }
)

$results = [ordered]@{}
Write-Host ""
Write-Host "R4 browser qualification"
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
    apiBaseUrl = $ApiBaseUrl
    checks = $results
    passed = ($failed.Count -eq 0)
    failedChecks = $failed
}

$evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
Write-Host "Browser qualification evidence written: $EvidencePath"

if ($failed.Count -gt 0) {
    throw "R4 browser qualification failed: $($failed -join ', ')"
}

Write-Host "R4 browser qualification passed."
