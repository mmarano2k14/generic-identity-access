$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath
    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required application security-catalog source file is missing: $RelativePath"
    }
    return $path
}

function Require-Text([string]$RelativePath, [string]$ExpectedText) {
    $path = Require-File $RelativePath
    $source = Get-Content $path -Raw
    if (-not $source.Contains($ExpectedText)) {
        throw "Application security-catalog source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text([string]$RelativePath, [string]$RejectedText) {
    $path = Require-File $RelativePath
    $source = Get-Content $path -Raw
    if ($source.Contains($RejectedText)) {
        throw "Application security-catalog source consistency failure in '$RelativePath': rejected text '$RejectedText' is present."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Domain/ApplicationSecurityManifestCapability.cs",
    "src/IdentityAccess.Domain/ApplicationSecurityManifest.cs",
    "src/IdentityAccess.Domain/RegisteredApplicationSecurityModel.cs",
    "src/IdentityAccess.Domain/CapabilityKey.cs",
    "src/IdentityAccess.Application/Storage/IApplicationSecurityCatalogStore.cs",
    "src/IdentityAccess.Application/Administration/IApplicationSecurityCatalogAdministrationService.cs",
    "src/IdentityAccess.Application/Administration/ApplicationSecurityManifestFingerprint.cs",
    "src/IdentityAccess.Application/Administration/ApplicationSecurityCatalogAdministrationService.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlApplicationSecurityCatalogStore.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0021_application_security_manifest_catalog.sql",
    "config/identity-access-admin-security-manifest.json",
    "scripts/authentication/bootstrap-dev-admin.ps1",
    "src/IdentityAccess.Api/Controllers/ApplicationSecurityModelsController.cs",
    "src/IdentityAccess.Api/Controllers/RegisterApplicationSecurityManifestRequest.cs",
    "src/IdentityAccess.Rbac/RbacTrnCompiler.cs",
    "tests/IdentityAccess.Tests/ApplicationSecurityManifestTests.cs",
    "tests/IdentityAccess.Tests/PostgreSql/ApplicationSecurityManifestCatalogSchemaTests.cs",
    "examples/nextjs/admin/server/IdentityAccessAdminSecurityManifestParser.ts",
    "examples/nextjs/admin/server/IdentityAccessAdminSecurityModelMutationService.ts",
    "examples/nextjs/admin/server/IdentityAccessAdminSecurityModelFailurePresentation.ts",
    "examples/nextjs/admin/app/identity/security-models/page.tsx",
    "examples/nextjs/admin/app/identity/actions.ts"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text "src/IdentityAccess.Domain/CapabilityKey.cs" "public string Resource { get; }"
Require-Text "src/IdentityAccess.Domain/CapabilityKey.cs" "public string Feature { get; }"
Require-Text "src/IdentityAccess.Domain/CapabilityKey.cs" "public string Action { get; }"
Require-Text "src/IdentityAccess.Domain/CapabilityKey.cs" "Project and authorization namespace belong to the RBAC execution context"
Reject-Text "src/IdentityAccess.Domain/CapabilityKey.cs" "public string Project { get; }"
Reject-Text "src/IdentityAccess.Domain/CapabilityKey.cs" "public string Namespace { get; }"

Require-Text "src/IdentityAccess.Rbac/RbacTrnCompiler.cs" 'return $"trn:{projectKey.Value}:{namespaceKey.Value}:{pattern.Resource}:{pattern.Feature}:{pattern.Action}";'
Require-Text "src/IdentityAccess.Domain/ApplicationSecurityManifest.cs" "public string RbacProject { get; }"
Require-Text "src/IdentityAccess.Domain/ApplicationSecurityManifest.cs" "public IReadOnlyList<string> RbacNamespaces { get; }"
Require-Text "src/IdentityAccess.Domain/ApplicationSecurityManifest.cs" "Duplicate application security capabilities are not allowed."

Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0021_application_security_manifest_catalog.sql" "application_security_model_registrations"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0021_application_security_manifest_catalog.sql" "application_security_namespaces"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0021_application_security_manifest_catalog.sql" "manifest_sha256"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0021_application_security_manifest_catalog.sql" "rbac_project"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlApplicationSecurityCatalogStore.cs" "SameManifest"
Require-Text "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlApplicationSecurityCatalogStore.cs" "IdentityConcurrencyException"

Require-Text "scripts/authentication/bootstrap-dev-admin.ps1" "identity-access-admin-security-manifest.json"
Require-Text "scripts/authentication/bootstrap-dev-admin.ps1" "ConvertFrom-Json"
Require-Text "scripts/authentication/bootstrap-dev-admin.ps1" '$manifestCapabilities'
Reject-Text "scripts/authentication/bootstrap-dev-admin.ps1" '$features = @('

Require-Text "src/IdentityAccess.Api/Controllers/ApplicationSecurityModelsController.cs" "IdentityAccessAdministrationCapabilities.SecurityModels"
Require-Text "src/IdentityAccess.Api/Controllers/ApplicationSecurityModelsController.cs" "new CapabilityKey(resource.Name, feature.Name, action.Name)"
Require-Text "src/IdentityAccess.Api/Controllers/ApplicationSecurityModelsController.cs" "new ApplicationSecurityManifest("


Require-Text "examples/nextjs/admin/server/IdentityAccessAdminSecurityManifestParser.ts" "manifestFile"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminSecurityManifestParser.ts" "JSON.parse"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminSecurityModelMutationService.ts" ".administration.securityModels.registerManifest"
Require-Text "examples/nextjs/admin/server/IdentityAccessAdminSecurityModelMutationService.ts" "manifest.applicationKey !== configuredApplicationKey"
Require-Text "examples/nextjs/admin/app/identity/security-models/page.tsx" "registerSecurityModelManifestAction"
Require-Text "examples/nextjs/admin/app/identity/security-models/page.tsx" 'name="manifestFile"'
Require-Text "examples/nextjs/admin/app/identity/security-models/page.tsx" 'type="file"'
Reject-Text "examples/nextjs/admin/app/identity/security-models/page.tsx" 'name="resource"'
Reject-Text "examples/nextjs/admin/app/identity/security-models/page.tsx" 'name="feature"'
Reject-Text "examples/nextjs/admin/app/identity/security-models/page.tsx" 'name="action"'
Reject-Text "examples/nextjs/admin/app/identity/security-models/page.tsx" '<textarea'
Require-Text "examples/nextjs/admin/app/identity/actions.ts" "IdentityAccessAdminSecurityModelMutationService"
Require-Text "examples/nextjs/admin/app/identity/actions.ts" "IdentityAccessAdminSecurityModelFailurePresentation"

$catalogService = Get-Content (Require-File "src/IdentityAccess.Application/Administration/ApplicationSecurityCatalogAdministrationService.cs") -Raw
$catalogStore = Get-Content (Require-File "src/IdentityAccess.Infrastructure.PostgreSql/Directory/PostgreSqlApplicationSecurityCatalogStore.cs") -Raw
if ($catalogService -match 'IsAllowed\s*\(' -or $catalogService -match 'authorization\.evaluate' -or $catalogStore -match 'IsAllowed\s*\(' -or $catalogStore -match 'authorization\.evaluate') {
    throw "Application security-catalog registration must not evaluate authorization or become a second RBAC engine."
}

Write-Host "Application security-catalog source consistency validation passed."
