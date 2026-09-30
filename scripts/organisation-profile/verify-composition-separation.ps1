$ErrorActionPreference = "Stop"

$root =
    Resolve-Path (Join-Path $PSScriptRoot "..\..")


function Require-OneDeclaredTypePerSourceFile {
    $sourceDirectories = @(
        (Join-Path $root "src\OrganisationProfile.Domain"),
        (Join-Path $root "src\OrganisationProfile.Application"),
        (Join-Path $root "src\OrganisationProfile.Infrastructure.PostgreSql")
    )

    $typePattern =
        '(?m)^\s*(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|ref|unsafe|new)\s+)*(?:record\s+(?:class\s+|struct\s+)?|class\s+|struct\s+|interface\s+|enum\s+)(?<name>[A-Za-z_][A-Za-z0-9_]*)'

    foreach ($directory in $sourceDirectories) {
        Get-ChildItem $directory -Filter *.cs -Recurse |
            Where-Object {
                $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]'
            } |
            ForEach-Object {
                $source =
                    [System.IO.File]::ReadAllText($_.FullName)

                $matches =
                    [System.Text.RegularExpressions.Regex]::Matches(
                        $source,
                        $typePattern)

                if ($matches.Count -ne 1) {
                    $declared =
                        $matches |
                        ForEach-Object {
                            $_.Groups["name"].Value
                        }

                    throw "$($_.FullName) declares $($matches.Count) types: $($declared -join ', ')"
                }

                $typeName =
                    $matches[0].Groups["name"].Value

                if ($typeName -ne $_.BaseName) {
                    throw "$($_.FullName) declares '$typeName' but its file name is '$($_.BaseName)'."
                }

                if ($source -match '(?m)^\s*namespace\s+[\w.]+\s*;\s*$') {
                    throw "$($_.FullName) uses a file-scoped namespace."
                }
            }
    }
}

function Read-Source {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $path = Join-Path $root $RelativePath

    if (-not (Test-Path $path)) {
        throw "Required OrganisationProfile source '$RelativePath' is missing."
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
        throw "'$RelativePath' is missing required composition boundary '$Text'."
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
        throw "'$RelativePath' violates composition separation with '$Text'."
    }
}

function Require-MaximumLines {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [int]$Maximum
    )

    $path = Join-Path $root $RelativePath
    $count = [System.IO.File]::ReadAllLines($path).Length

    if ($count -gt $Maximum) {
        throw "'$RelativePath' has $count lines and exceeds focused limit $Maximum."
    }
}

$compositionService =
    "src\OrganisationProfile.Application\Composition\OrganisationProfileCompositionService.cs"

$overrideService =
    "src\OrganisationProfile.Application\Composition\OrganisationProfileDomainOverrideService.cs"

$resolver =
    "src\OrganisationProfile.Application\Composition\OrganisationProfileCompositionResolver.cs"

$validator =
    "src\OrganisationProfile.Application\Registry\OrganisationProfileDomainRegistryValidator.cs"

$publication =
    "src\OrganisationProfile.Application\Templates\OrganisationProfileTemplatePublicationService.cs"

$versionFacade =
    "src\OrganisationProfile.Infrastructure.PostgreSql\PostgreSqlOrganisationProfileVersionStore.cs"

Require-Text $compositionService "OrganisationProfileCompositionResolver"
Require-Text $compositionService "OrganisationProfileDomainRegistryValidator"
Require-Text $compositionService "IOrganisationProfileVersionStore"

Require-Text $overrideService "IOrganisationProfileDomainOverrideStore"
Require-Text $overrideService "RequireSelectableAsync"

Require-Text $publication "RequireSelectableAsync"

Require-Text $resolver "OrganisationProfileDomainOverrideOperation.Disable"
Forbid-Text $resolver "Npgsql"
Forbid-Text $resolver "IDomainRegistryReader"

Require-Text $validator "IDomainRegistryReader"
Forbid-Text $validator "Npgsql"

Require-Text $versionFacade "PostgreSqlOrganisationProfileVersionReader"
Require-Text $versionFacade "PostgreSqlOrganisationProfileVersionWriter"

$profileSourceDirectories = @(
    (Join-Path $root "src\OrganisationProfile.Domain"),
    (Join-Path $root "src\OrganisationProfile.Application"),
    (Join-Path $root "src\OrganisationProfile.Infrastructure.PostgreSql")
)

foreach ($forbiddenType in @(
    "OrganisationProfileManager",
    "OrganisationProfileCompositionManager",
    "OrganisationProfileDomainManager",
    "DomainRegistryStore"
)) {
    $matches =
        $profileSourceDirectories |
        ForEach-Object {
            Get-ChildItem $_ -Filter *.cs -Recurse
        } |
        Select-String `
            -SimpleMatch `
            -Pattern $forbiddenType

    if ($matches) {
        throw "Forbidden god/boundary type '$forbiddenType' was found."
    }
}

$applicationRoot =
    Join-Path $root "src\OrganisationProfile.Application"

$applicationSource =
    (
        Get-ChildItem $applicationRoot -Filter *.cs -Recurse |
        ForEach-Object {
            [System.IO.File]::ReadAllText($_.FullName)
        }
    ) -join "`n"

foreach ($forbiddenInfrastructure in @(
    "Npgsql",
    "organization_directory.",
    "identity_access."
)) {
    if ($applicationSource.Contains($forbiddenInfrastructure)) {
        throw "Application layer leaked infrastructure dependency '$forbiddenInfrastructure'."
    }
}

Require-MaximumLines $compositionService 170
Require-MaximumLines $overrideService 150
Require-MaximumLines $resolver 100
Require-MaximumLines $validator 110
Require-MaximumLines $publication 180
Require-MaximumLines $versionFacade 100

Require-OneDeclaredTypePerSourceFile

Write-Host "OrganisationProfile composition separation validation: GREEN"
