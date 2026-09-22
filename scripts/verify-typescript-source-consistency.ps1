[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "clients/typescript/src"
$required = @(
    "client.ts",
    "authorization-context.ts",
    "admin-ui-builder.ts",
    "require-capability.ts",
    "contracts.ts",
    "errors.ts",
    "index.ts"
)
foreach ($name in $required) {
    if (-not (Test-Path (Join-Path $src $name) -PathType Leaf)) {
        throw "TypeScript connector source is incomplete: missing $name."
    }
}
$client = Get-Content (Join-Path $src "client.ts") -Raw
$context = Get-Content (Join-Path $src "authorization-context.ts") -Raw
$builder = Get-Content (Join-Path $src "admin-ui-builder.ts") -Raw
$decorator = Get-Content (Join-Path $src "require-capability.ts") -Raw
$index = Get-Content (Join-Path $src "index.ts") -Raw
if ($client -notmatch 'export\s+class\s+IdentityAccessClient\b') { throw "IdentityAccessClient must be a class." }
if ($context -notmatch 'export\s+class\s+IdentityAuthorizationContext\b') { throw "IdentityAuthorizationContext must be a class." }
if ($builder -notmatch 'export\s+class\s+IdentityAccessAdminUiBuilder\b') { throw "IdentityAccessAdminUiBuilder must be a class." }
if ($decorator -notmatch 'export\s+function\s+RequireCapability\b') { throw "RequireCapability decorator is missing." }
if ($index -match 'createIdentityAccessClient') { throw "Legacy createIdentityAccessClient export must not return." }
$allSource = (Get-ChildItem $src -Filter *.ts -Recurse | ForEach-Object { Get-Content $_.FullName -Raw }) -join "`n"
if ($allSource -match 'export\s+function\s+createIdentityAccessClient\b') { throw "Legacy functional client factory must not return." }
if ($context -notmatch '\bisAllowed\s*\(') { throw "IdentityAuthorizationContext.isAllowed is required." }
Write-Host "TypeScript source consistency validation passed."
