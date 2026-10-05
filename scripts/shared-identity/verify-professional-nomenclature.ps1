[CmdletBinding()]
param(
    [string[]]$ForbiddenConsumerNames = @()
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$legacyMilestoneWord = ('pa' + 'ck')

$textExtensions = @(
    '.md', '.txt', '.ps1', '.psm1', '.json', '.jsonc',
    '.ts', '.tsx', '.js', '.mjs', '.cjs',
    '.cs', '.csproj', '.props', '.targets',
    '.yml', '.yaml', '.xml', '.sln', '.toml',
    '.ini', '.config', '.sh', '.cmd', '.bat',
    '.sql', '.http', '.css', '.sha256'
)

$excludedDirectoryNames = @(
    '.git', 'node_modules', 'bin', 'obj', '.next',
    'dist', 'build', 'coverage', 'TestResults', '.vs',
    '.idea', '.turbo', 'artifacts'
)

$files = @(
    Get-ChildItem -Path $root -Recurse -File |
        Where-Object {
            $fullName = $_.FullName
            foreach ($directoryName in $excludedDirectoryNames) {
                $separatorToken = [System.IO.Path]::DirectorySeparatorChar + $directoryName + [System.IO.Path]::DirectorySeparatorChar
                if ($fullName.IndexOf($separatorToken, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
                    return $false
                }
            }

            return ($textExtensions -contains $_.Extension) -or
                $_.Name -in @('README', 'LICENSE', '.gitignore', '.gitattributes', '.editorconfig')
        }
)

$legacyNumberedPattern = '(?i)\b' + [regex]::Escape($legacyMilestoneWord) + 's?\s*\d+\b|' +
    [regex]::Escape($legacyMilestoneWord.ToUpperInvariant()) + '_\d+|' +
    [regex]::Escape($legacyMilestoneWord) + '-\d+|verify-' +
    [regex]::Escape($legacyMilestoneWord) + '|qualify-' +
    [regex]::Escape($legacyMilestoneWord)

$legacyStandalonePattern = '(?i)\b' + [regex]::Escape($legacyMilestoneWord) + '\b'

$rootFullPath = [System.IO.Path]::GetFullPath($root)
$directorySeparator = [System.IO.Path]::DirectorySeparatorChar.ToString()
if (-not $rootFullPath.EndsWith($directorySeparator)) {
    $rootPrefix = $rootFullPath + $directorySeparator
}
else {
    $rootPrefix = $rootFullPath
}

$consumerNames = @(
    $ForbiddenConsumerNames |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Trim() } |
        Sort-Object -Unique
)

foreach ($file in $files) {
    $fileFullPath = [System.IO.Path]::GetFullPath($file.FullName)
    if (-not $fileFullPath.StartsWith($rootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Repository file is outside the expected root: $fileFullPath"
    }

    $relative = $fileFullPath.Substring($rootPrefix.Length).Replace('\', '/')

    foreach ($consumerName in $consumerNames) {
        if ($relative.IndexOf($consumerName, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Consumer-specific product name found in repository path: $relative"
        }
    }

    if ([regex]::IsMatch($relative, $legacyNumberedPattern)) {
        throw "Numbered delivery nomenclature found in repository path: $relative"
    }

    $content = [System.IO.File]::ReadAllText($file.FullName)

    foreach ($consumerName in $consumerNames) {
        if ($content.IndexOf($consumerName, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Consumer-specific product name found in repository content: $relative"
        }
    }

    if ([regex]::IsMatch($content, $legacyNumberedPattern)) {
        throw "Numbered delivery nomenclature found in repository content: $relative"
    }

    if ([regex]::IsMatch($content, $legacyStandalonePattern)) {
        throw "Legacy delivery word found in repository content: $relative"
    }
}

Write-Host "Generic Identity professional nomenclature validation: GREEN"
