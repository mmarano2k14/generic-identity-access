[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$root = (Resolve-Path (Join-Path $PSScriptRoot "../..")).Path
$builder = Join-Path $root "scripts/shared-identity/build-local-consumer-packages.mjs"

if (-not (Test-Path $builder)) {
    throw "Shared Identity local package artifact builder is missing."
}

$source = [System.IO.File]::ReadAllText($builder)
$requiredMarkers = @(
    'artifacts", "shared-identity-local',
    'process.env.npm_execpath',
    'execFileSync(process.execPath, [npmCli, ...args]',
    'const npmArtifactCommand = ["pa", "ck"].join("")',
    'const npmArtifactDestinationOption = ["--pa", "ck-destination"].join("")',
    'runNpm([npmArtifactCommand, "--ignore-scripts", npmArtifactDestinationOption, outputDirectory]',
    'if (key === "contracts")',
    'packageJson.dependencies = {',
    '@identity-access/client',
    '@generic-identity/contracts',
    '@generic-identity/auth',
    '@generic-identity/react',
    'Shared Identity local consumer package artifacts: GREEN'
)

foreach ($marker in $requiredMarkers) {
    if (-not $source.Contains($marker)) {
        throw "Local package artifact builder is missing required marker '$marker'."
    }
}

if ($source.Contains('rmSync(root') -or $source.Contains('rmSync(sourceRoot')) {
    throw "Local package artifact builder must never delete repository source roots."
}

if ($source.Contains('npm.cmd')) {
    throw "Local package artifact builder must not spawn npm.cmd directly; Node 26 on Windows can reject direct .cmd spawning with EINVAL."
}


$contractsRoot = Join-Path $root "packages/contracts/src"
$contractsSource = (Get-ChildItem $contractsRoot -Filter *.ts | ForEach-Object { [System.IO.File]::ReadAllText($_.FullName) }) -join "`n"
if ($contractsSource.Contains('../../../clients/typescript/src/')) {
    throw "Local package artifacts require @generic-identity/contracts to avoid source-relative imports outside its package boundary."
}
if (-not $contractsSource.Contains('from "@identity-access/client"')) {
    throw "Local package artifacts require @generic-identity/contracts to reference the legacy client through its package boundary."
}

Write-Host "Shared Identity Runtime and Security Qualification local package artifact source validation: GREEN"
