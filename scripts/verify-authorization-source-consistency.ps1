$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot ".."))

function Require-File([string]$RelativePath) {
    $path = Join-Path $repositoryRoot $RelativePath

    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required authorization source file is missing: $RelativePath"
    }

    return $path
}

function Require-Text(
    [string]$RelativePath,
    [string]$ExpectedText) {

    $path = Require-File $RelativePath
    $source = Get-Content $path -Raw

    if (-not $source.Contains($ExpectedText)) {
        throw "Authorization source consistency failure in '$RelativePath': missing '$ExpectedText'."
    }
}

function Reject-Text(
    [string]$RelativePath,
    [string]$RejectedText) {

    $path = Require-File $RelativePath
    $source = Get-Content $path -Raw

    if ($source.Contains($RejectedText)) {
        throw "Authorization source consistency failure in '$RelativePath': obsolete text '$RejectedText' remains."
    }
}

$requiredFiles = @(
    "src/IdentityAccess.Api/Security/AdministrationAuthorizationOptions.cs",
    "src/IdentityAccess.Api/Security/AdministrationAuthorizationServiceRegistration.cs",
    "src/IdentityAccess.Api/Security/AdministrationAuthorizationTarget.cs",
    "src/IdentityAccess.Api/Security/AdministrationAuthorizationTargetResolver.cs",
    "src/IdentityAccess.Api/Security/AdministrationRequestBoundary.cs",
    "src/IdentityAccess.Api/Security/RbacAdministrationRequestAuthorizer.cs",
    "src/IdentityAccess.Authorization/CapabilityGrantAuthorizationEvaluator.cs",
    "src/IdentityAccess.Authorization/IdentityAuthorizationService.cs",
    "src/IdentityAccess.Authorization/IdentityScopeAuthorizationRequest.cs",
    "src/IdentityAccess.Authorization/IdentityScopeAuthorizationService.cs",
    "src/IdentityAccess.Application/Authorization/IIdentityScopeAssignedCapabilityReader.cs",
    "src/IdentityAccess.Rbac.MultiplexedAdapter/IMultiplexedRbacCompatibilityProbe.cs",
    "src/IdentityAccess.Rbac.MultiplexedAdapter/MultiplexedRbacCompatibilityReport.cs",
    "src/IdentityAccess.Rbac.MultiplexedAdapter/MultiplexedRbacBinding.cs",
    "src/IdentityAccess.Rbac.MultiplexedAdapter/MultiplexedRbacBindingLoader.cs",
    "src/IdentityAccess.Rbac.MultiplexedAdapter/MultiplexedRbacBindingResult.cs",
    "src/IdentityAccess.Rbac.MultiplexedAdapter/MultiplexedRbacContractException.cs",
    "src/IdentityAccess.Infrastructure.PostgreSql/Migrations/0010_identity_scope_administration_authority.sql",
    "tests/IdentityAccess.Tests/Authorization/IdentityScopeAuthorizationTestRouteResolver.cs",
    "tests/IdentityAccess.Tests/Authorization/IdentityScopeAuthorizationTestGrantReader.cs",
    "tests/IdentityAccess.Tests/Authorization/IdentityScopeAuthorizationCapturingRbacAdapter.cs",
    "tests/IdentityAccess.Tests/Authorization/RbacAdministrationTestContextResolver.cs",
    "tests/IdentityAccess.Tests/Authorization/RbacAdministrationCapturingAuthorizationService.cs",
    "tests/IdentityAccess.Tests/Authorization/RbacAdministrationCapturingIdentityScopeAuthorizationService.cs"
)

foreach ($file in $requiredFiles) {
    Require-File $file | Out-Null
}

Require-Text `
    "src/IdentityAccess.Api/Security/AdministrationAccessFailureCode.cs" `
    "AuthenticationUnavailable"

Require-Text `
    "src/IdentityAccess.Api/Security/AdministrationAccessFailureCode.cs" `
    "AuthorizationTargetUnavailable"

Require-Text `
    "src/IdentityAccess.Api/Security/AdministrationAccessFailureCode.cs" `
    "AuthorizationTechnicalFailure"

Require-Text `
    "src/IdentityAccess.Api/IdentityAccess.Api.csproj" `
    "../IdentityAccess.Authorization/IdentityAccess.Authorization.csproj"

Require-Text `
    "src/IdentityAccess.Api/IdentityAccess.Api.csproj" `
    "../IdentityAccess.Rbac/IdentityAccess.Rbac.csproj"

Require-Text `
    "src/IdentityAccess.Api/IdentityAccess.Api.csproj" `
    "../IdentityAccess.Rbac.MultiplexedAdapter/IdentityAccess.Rbac.MultiplexedAdapter.csproj"

Require-Text `
    "src/IdentityAccess.Authorization/IdentityAuthorizationService.cs" `
    "RbacTrnCompiler trnCompiler"

Require-Text `
    "src/IdentityAccess.Authorization/IdentityScopeAuthorizationService.cs" `
    "RbacTrnCompiler trnCompiler"

Reject-Text `
    "src/IdentityAccess.Authorization/IdentityAuthorizationService.cs" `
    "CapabilityGrantAuthorizationEvaluator evaluator)"

Reject-Text `
    "src/IdentityAccess.Authorization/IdentityScopeAuthorizationService.cs" `
    "CapabilityGrantAuthorizationEvaluator evaluator)"

Require-Text `
    "tests/IdentityAccess.Tests/Authorization/IdentityScopeAuthorizationServiceTests.cs" `
    "IdentityScopeAuthorizationTestRouteResolver"

Require-Text `
    "tests/IdentityAccess.Tests/Authorization/IdentityScopeAuthorizationTestRouteResolver.cs" `
    "new ConnectionSecretReference("

Require-Text `
    "tests/IdentityAccess.Tests/Authorization/IdentityScopeAuthorizationTestRouteResolver.cs" `
    "configurationRevision: 1"

Require-Text `
    "tests/IdentityAccess.Tests/Authorization/IdentityScopeAuthorizationTestRouteResolver.cs" `
    "routeVersion: 1"



Require-Text `
    "src/IdentityAccess.Api/Security/RbacAdministrationRequestAuthorizer.cs" `
    "AuthorizeTenantTargetAsync"

Require-Text `
    "src/IdentityAccess.Api/Security/RbacAdministrationRequestAuthorizer.cs" `
    "AuthorizeIdentityScopeAsync"

Require-Text `
    "src/IdentityAccess.Api/Security/RbacAdministrationRequestAuthorizer.cs" `
    "scopeResult.Decision == IdentityAuthorizationDecision.Allowed"

Require-Text `
    "tests/IdentityAccess.Tests/Authorization/RbacAdministrationRequestAuthorizerTests.cs" `
    "Tenant_route_scope_allow_bypasses_tenant_authority"

Require-Text `
    "tests/IdentityAccess.Tests/Authorization/RbacAdministrationRequestAuthorizerTests.cs" `
    "Tenant_route_scope_deny_falls_back_to_tenant_allow"

Require-Text `
    "tests/IdentityAccess.Tests/Authorization/RbacAdministrationRequestAuthorizerTests.cs" `
    "Tenant_route_scope_technical_failure_with_tenant_deny_is_unavailable"

Require-Text `
    "scripts/authentication/bootstrap-dev-admin.ps1" `
    "Local Identity Scope Administration"

Require-Text `
    "scripts/authentication/bootstrap-dev-admin.ps1" `
    "'identity-access', '*', '*'"

Require-Text `
    "src/IdentityAccess.Rbac/RbacAuthorizationFailureCode.cs" `
    "ExternalLoadFailed"

Require-Text `
    "src/IdentityAccess.Rbac/RbacAuthorizationFailureCode.cs" `
    "ExternalContractMismatch"

Require-Text `
    "src/IdentityAccess.Api/Security/AdministrationAuthorizationServiceRegistration.cs" `
    "ProbeCompatibility"

Require-Text `
    "src/IdentityAccess.Api/Security/AdministrationAuthorizationServiceRegistration.cs" `
    "compatibility.IsCompatible"

Require-Text `
    "src/IdentityAccess.Rbac.MultiplexedAdapter/MultiplexedRbacAuthorizationAdapter.cs" `
    "Lazy<MultiplexedRbacBindingResult>"

Require-Text `
    "src/IdentityAccess.Rbac.MultiplexedAdapter/MultiplexedRbacAuthorizationAdapter.cs" `
    "AccessorClearMethod.Invoke"

Require-Text `
    "src/IdentityAccess.Rbac.MultiplexedAdapter/MultiplexedRbacBindingLoader.cs" `
    "EnsureLoadedFromExpectedPath"

Write-Host "Authorization source consistency validation passed."
