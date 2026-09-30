$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root =
    Resolve-Path (Join-Path $PSScriptRoot "..\..")

function Read-Source {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $root $RelativePath

    if (-not (Test-Path $path -PathType Leaf)) {
        throw "Required OrganisationProfile security-integration source '$RelativePath' is missing."
    }

    return [System.IO.File]::ReadAllText($path)
}

function Require-Text {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    if (-not (Read-Source $RelativePath).Contains($Text)) {
        throw "'$RelativePath' is missing required security-integration marker '$Text'."
    }
}

function Forbid-Text {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string]$Text
    )

    if ((Read-Source $RelativePath).Contains($Text)) {
        throw "'$RelativePath' violates security separation with '$Text'."
    }
}

$apiProject =
    "src\IdentityAccess.Api\IdentityAccess.Api.csproj"

$program =
    "src\IdentityAccess.Api\Program.cs"

$registration =
    "src\IdentityAccess.Api\OrganisationProfileRegistration.cs"

$capabilities =
    "src\IdentityAccess.Api\Security\OrganisationProfileAdministrationCapabilities.cs"

$auditWriter =
    "src\IdentityAccess.Api\Security\OrganisationProfileSecurityAuditWriter.cs"

$domainFallback =
    "src\IdentityAccess.Api\OrganisationProfiles\UnavailableDomainRegistryReader.cs"

Require-Text $apiProject "../OrganisationProfile.Application/OrganisationProfile.Application.csproj"
Require-Text $apiProject "../OrganisationProfile.Domain/OrganisationProfile.Domain.csproj"
Require-Text $apiProject "../OrganisationProfile.Infrastructure.PostgreSql/OrganisationProfile.Infrastructure.PostgreSql.csproj"

Require-Text $program "builder.AddOrganizationDirectory();"
Require-Text $program "builder.AddOrganisationProfile();"

Require-Text $registration "TryAddSingleton"
Require-Text $registration "UnavailableDomainRegistryReader"
Require-Text $registration "PostgreSqlOrganisationProfileStore"
Require-Text $registration "PostgreSqlOrganisationProfileTemplateStore"
Require-Text $registration "PostgreSqlOrganisationProfileTemplateVersionStore"
Require-Text $registration "PostgreSqlOrganisationProfileDomainOverrideStore"
Require-Text $registration "PostgreSqlOrganisationProfileVersionStore"
Require-Text $registration "IOrganisationProfileSecurityAuditWriter"

Require-Text $capabilities "IdentityAccessAdministrationCapabilities.Resource"
Require-Text $capabilities 'public const string Profiles = "organisation-profile";'
Require-Text $capabilities 'public const string Templates = "organisation-profile-template";'
Require-Text $capabilities 'public const string DomainOverrides = "organisation-profile-domain-override";'
Require-Text $capabilities 'public const string EffectiveVersions = "organisation-profile-effective-version";'

Require-Text $auditWriter "IDatabaseRouteResolver"
Require-Text $auditWriter "ISecurityAuditWriter"
Require-Text $auditWriter "SecurityAuditOutcome.Succeeded"
Forbid-Text $auditWriter "IsAllowed("
Forbid-Text $auditWriter "authorization.evaluate"

Require-Text $domainFallback "Task.FromResult<DomainRegistryVersionState?>(null)"
Forbid-Text $domainFallback "Published"

$manifestPath =
    Join-Path $root "config\identity-access-admin-security-manifest.json"

if (-not (Test-Path $manifestPath -PathType Leaf)) {
    throw "Identity Access administration security manifest is missing."
}

$manifest =
    Get-Content $manifestPath -Raw |
    ConvertFrom-Json

if ([int]$manifest.modelVersion -ne 4) {
    throw "OrganisationProfile security integration requires administration modelVersion 4."
}

$resources =
    @(
        $manifest.resources |
        Where-Object {
            $_.name -eq "identity-access"
        }
    )

if ($resources.Count -ne 1) {
    throw "Administration manifest must contain exactly one identity-access resource."
}

$features =
    @(
        $resources[0].features |
        ForEach-Object {
            [string]$_.name
        }
    )

foreach ($requiredFeature in @(
    "organisation-profile",
    "organisation-profile-template",
    "organisation-profile-domain-override",
    "organisation-profile-effective-version"
)) {
    if ($features -notcontains $requiredFeature) {
        throw "OrganisationProfile administration manifest is missing '$requiredFeature'."
    }
}


$apiProblems =
    "src\IdentityAccess.Api\Http\ApiProblems.cs"

Require-Text $apiProblems "UnprocessableEntity("
Require-Text $apiProblems "OrganizationMembershipAdministrationUnavailable()"
Require-Text $apiProblems "OrganizationResourceScopeLinkAdministrationUnavailable()"

$auditEvents =
    "src\IdentityAccess.Application\Security\SecurityAuditEventType.cs"

Require-Text $auditEvents "OrganisationProfileCreated = 79"
Require-Text $auditEvents "OrganisationProfileDomainOverridesReplaced = 83"
Require-Text $auditEvents "OrganisationProfileEffectiveVersionResolved = 84"
Require-Text $auditEvents "OrganisationProfileTemplateVersionPublished = 90"
Require-Text $auditEvents "OrganisationProfileTemplateVersionRetired = 91"

foreach ($project in @(
    "OrganisationProfile.Domain",
    "OrganisationProfile.Application",
    "OrganisationProfile.Infrastructure.PostgreSql"
)) {
    $projectFile =
        "src\$project\$project.csproj"

    Forbid-Text $projectFile "IdentityAccess."
}

$profileSourceDirectories = @(
    (Join-Path $root "src\OrganisationProfile.Domain"),
    (Join-Path $root "src\OrganisationProfile.Application"),
    (Join-Path $root "src\OrganisationProfile.Infrastructure.PostgreSql")
)

foreach ($directory in $profileSourceDirectories) {
    $matches =
        Get-ChildItem $directory -Filter *.cs -Recurse |
        Where-Object {
            $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]'
        } |
        Select-String -SimpleMatch -Pattern "using IdentityAccess"

    if ($matches) {
        throw "Reusable OrganisationProfile source must not depend on Identity Access source namespaces."
    }
}

$registrationLineCount =
    [System.IO.File]::ReadAllLines(
        (Join-Path $root $registration)
    ).Length

if ($registrationLineCount -gt 180) {
    throw "OrganisationProfileRegistration.cs exceeds the focused composition-root limit."
}

Write-Host "OrganisationProfile security integration validation: GREEN"
