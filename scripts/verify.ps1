[CmdletBinding()]
param([ValidateSet("Debug", "Release")][string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest
$root = Split-Path -Parent $PSScriptRoot
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 10 SDK is required. Run dotnet --list-sdks after installation."
}
Push-Location $root
try {
    & (Join-Path $root "scripts/verify-authorization-source-consistency.ps1")
    if (-not $?) { throw "Authorization source consistency validation failed." }

    & (Join-Path $root "scripts/verify-oidc-source-consistency.ps1")
    if (-not $?) { throw "OIDC source consistency validation failed." }

    & (Join-Path $root "scripts/verify-typescript-source-consistency.ps1")
    if (-not $?) { throw "TypeScript source consistency validation failed." }

    if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
        throw "Node.js/npm is required for the TypeScript connector validation."
    }
    $typescriptRoot = Join-Path $root "clients/typescript"
    $localTypeScriptCompiler = Join-Path $typescriptRoot "node_modules/.bin/tsc.cmd"
    $localTypeScriptCompilerUnix = Join-Path $typescriptRoot "node_modules/.bin/tsc"
    if (-not ((Test-Path $localTypeScriptCompiler -PathType Leaf) -or (Test-Path $localTypeScriptCompilerUnix -PathType Leaf))) {
        Write-Host "Installing pinned TypeScript development dependencies..."
        Push-Location $typescriptRoot
        try {
            & npm install --ignore-scripts --no-audit --no-fund --package-lock=false
            if ($LASTEXITCODE -ne 0) { throw "TypeScript dependency restore failed." }
        } finally {
            Pop-Location
        }
    }

    Push-Location $typescriptRoot
    try {
        & npm test
        if ($LASTEXITCODE -ne 0) { throw "TypeScript tests failed." }
        & npm run typecheck
        if ($LASTEXITCODE -ne 0) { throw "TypeScript typecheck failed." }
    } finally {
        Pop-Location
    }

    & dotnet --info
    if ($LASTEXITCODE -ne 0) { throw "SDK check failed." }
    & dotnet restore IdentityAccess.sln
    if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed." }
    & dotnet build IdentityAccess.sln --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
    & dotnet test IdentityAccess.sln --configuration $Configuration --no-build --no-restore `
        --logger "trx;LogFileName=identity-access.trx" --results-directory (Join-Path $root "artifacts/test-results")
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
} finally {
    Pop-Location
}
