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

    & (Join-Path $root "scripts/verify-security-catalog-source-consistency.ps1")
    if (-not $?) { throw "Application security-catalog source consistency validation failed." }

    & (Join-Path $root "scripts/verify-managed-policy-catalog-source-consistency.ps1")
    if (-not $?) { throw "Managed policy catalog source consistency validation failed." }

    & (Join-Path $root "scripts/verify-managed-policy-binding-source-consistency.ps1")
    if (-not $?) { throw "Managed policy binding source consistency validation failed." }

    & (Join-Path $root "scripts/verify-managed-policy-publication-source-consistency.ps1")
    if (-not $?) { throw "Managed policy publication source consistency validation failed." }

    & (Join-Path $root "scripts/verify-managed-policy-administration-source-consistency.ps1")
    if (-not $?) { throw "Managed policy administration source consistency validation failed." }

    & (Join-Path $root "scripts/verify-managed-policy-binding-administration-source-consistency.ps1")
    if (-not $?) { throw "Managed policy binding administration source consistency validation failed." }

    & (Join-Path $root "scripts/verify-managed-policy-ui-source-consistency.ps1")
    if (-not $?) { throw "Managed policy UI source consistency validation failed." }

    & (Join-Path $root "scripts/verify-managed-policy-compatibility-closure-source-consistency.ps1")
    if (-not $?) { throw "Managed policy compatibility closure source consistency validation failed." }

    & (Join-Path $root "scripts/verify-multitenant-administration-context-source-consistency.ps1")
    if (-not $?) { throw "Multi-tenant administration context source consistency validation failed." }

    & (Join-Path $root "scripts/verify-tenant-membership-ui-source-consistency.ps1")
    if (-not $?) { throw "Tenant-centric membership UI source consistency validation failed." }

    & (Join-Path $root "scripts/verify-group-as-template-source-consistency.ps1")
    if (-not $?) { throw "Group-as-template source consistency validation failed." }

    & (Join-Path $root "scripts/verify-member-group-assignment-source-consistency.ps1")
    if (-not $?) { throw "Member group assignment and safe Add member source consistency validation failed." }

    & (Join-Path $root "scripts/verify-r4-final-source-consistency.ps1")
    if (-not $?) { throw "R4 final administration source consistency validation failed." }

    & (Join-Path $root "scripts/verify-oidc-source-consistency.ps1")
    if (-not $?) { throw "OIDC source consistency validation failed." }

    & (Join-Path $root "scripts/verify-mfa-source-consistency.ps1")
    if (-not $?) { throw "MFA source consistency validation failed." }

    & (Join-Path $root "scripts/verify-totp-source-consistency.ps1")
    if (-not $?) { throw "TOTP source consistency validation failed." }

    & (Join-Path $root "scripts/verify-recovery-source-consistency.ps1")
    if (-not $?) { throw "Recovery source consistency validation failed." }

    & (Join-Path $root "scripts/verify-webauthn-registration-source-consistency.ps1")
    if (-not $?) { throw "WebAuthn registration source consistency validation failed." }

    & (Join-Path $root "scripts/verify-webauthn-authentication-source-consistency.ps1")
    if (-not $?) { throw "WebAuthn authentication source consistency validation failed." }

    & (Join-Path $root "scripts/verify-mfa-integration-source-consistency.ps1")
    if (-not $?) { throw "MFA integration source consistency validation failed." }

    & (Join-Path $root "scripts/verify-mfa-session-assurance-source-consistency.ps1")
    if (-not $?) { throw "MFA session assurance source consistency validation failed." }

    & (Join-Path $root "scripts/verify-credential-security-source-consistency.ps1")
    if (-not $?) { throw "Credential security source consistency validation failed." }

    & (Join-Path $root "scripts/verify-typescript-source-consistency.ps1")
    if (-not $?) { throw "TypeScript source consistency validation failed." }

    & (Join-Path $root "scripts/shared-identity/verify.ps1")
    if (-not $?) { throw "Shared Identity integration validation failed." }

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

    $contractsTypeScriptCompiler = if (Test-Path $localTypeScriptCompiler -PathType Leaf) {
        $localTypeScriptCompiler
    } else {
        $localTypeScriptCompilerUnix
    }
    & $contractsTypeScriptCompiler -p (Join-Path $root "packages/contracts/tsconfig.json") --noEmit
    if ($LASTEXITCODE -ne 0) { throw "Shared Identity public contracts typecheck failed." }
    Write-Host "Shared Identity Pack 2 public contracts typecheck: GREEN"

    # The auth package is consumed through a local file link by external Next.js
    # applications during Packs 8-9. TypeScript and Turbopack resolve imports from
    # the real linked source path, so the auth package must have its declared local
    # dependencies installed beside that source. Its tsconfig path aliases alone are
    # not sufficient for an external consumer. The legacy client was built above,
    # so its dist export is ready before this dependency restore runs.
    $authPackageRoot = Join-Path $root "packages/auth"
    $authLegacyBridgePackage = Join-Path $authPackageRoot "node_modules/@identity-access/client/package.json"
    $authContractsPackage = Join-Path $authPackageRoot "node_modules/@generic-identity/contracts/package.json"
    if (-not ((Test-Path $authLegacyBridgePackage -PathType Leaf) -and (Test-Path $authContractsPackage -PathType Leaf))) {
        Write-Host "Installing Shared Identity auth linked-development dependencies..."
        Push-Location $authPackageRoot
        try {
            & npm install --ignore-scripts --no-audit --no-fund --package-lock=false
            if ($LASTEXITCODE -ne 0) { throw "Shared Identity auth dependency restore failed." }
        } finally {
            Pop-Location
        }
    }
    if (-not (Test-Path $authLegacyBridgePackage -PathType Leaf)) {
        throw "Shared Identity auth local @identity-access/client dependency is unavailable after restore."
    }
    if (-not (Test-Path $authContractsPackage -PathType Leaf)) {
        throw "Shared Identity auth local @generic-identity/contracts dependency is unavailable after restore."
    }

    & $contractsTypeScriptCompiler -p (Join-Path $root "packages/auth/tsconfig.json") --noEmit
    if ($LASTEXITCODE -ne 0) { throw "Shared Identity auth SDK typecheck failed." }
    Write-Host "Shared Identity Pack 3 auth SDK typecheck: GREEN"

    $reactPackageRoot = Join-Path $root "packages/react"
    $reactTypeScriptCompiler = Join-Path $reactPackageRoot "node_modules/.bin/tsc.cmd"
    $reactTypeScriptCompilerUnix = Join-Path $reactPackageRoot "node_modules/.bin/tsc"
    if (-not ((Test-Path $reactTypeScriptCompiler -PathType Leaf) -or (Test-Path $reactTypeScriptCompilerUnix -PathType Leaf))) {
        Write-Host "Installing pinned Shared Identity React development dependencies..."
        Push-Location $reactPackageRoot
        try {
            & npm install --ignore-scripts --no-audit --no-fund --package-lock=false
            if ($LASTEXITCODE -ne 0) { throw "Shared Identity React dependency restore failed." }
        } finally {
            Pop-Location
        }
    }

    Push-Location $reactPackageRoot
    try {
        & npm run typecheck
        if ($LASTEXITCODE -ne 0) { throw "Shared Identity React foundation typecheck failed." }
    } finally {
        Pop-Location
    }
    Write-Host "Shared Identity Pack 4 React foundation typecheck: GREEN"
    Write-Host "Shared Identity Pack 5 shared pages typecheck: GREEN"
    Write-Host "Shared Identity Pack 6 theme/component override typecheck: GREEN"

    $sharedNextRoot = Join-Path $root "packages/next"
    $sharedNextTypeScriptCompiler = Join-Path $sharedNextRoot "node_modules/.bin/tsc.cmd"
    $sharedNextTypeScriptCompilerUnix = Join-Path $sharedNextRoot "node_modules/.bin/tsc"
    if (-not ((Test-Path $sharedNextTypeScriptCompiler -PathType Leaf) -or (Test-Path $sharedNextTypeScriptCompilerUnix -PathType Leaf))) {
        Write-Host "Installing pinned Shared Identity Next.js development dependencies..."
        Push-Location $sharedNextRoot
        try {
            & npm install --ignore-scripts --no-audit --no-fund --package-lock=false
            if ($LASTEXITCODE -ne 0) { throw "Shared Identity Next.js dependency restore failed." }
        } finally {
            Pop-Location
        }
    }

    Push-Location $sharedNextRoot
    try {
        & npm run typecheck
        if ($LASTEXITCODE -ne 0) { throw "Shared Identity Next.js integration typecheck failed." }
    } finally {
        Pop-Location
    }
    Write-Host "Shared Identity Pack 7 Next.js integration typecheck: GREEN"

    $nextAdminRoot = Join-Path $root "examples/nextjs/admin"
    $nextBinary = Join-Path $nextAdminRoot "node_modules/.bin/next.cmd"
    $nextBinaryUnix = Join-Path $nextAdminRoot "node_modules/.bin/next"
    $hostTypeScriptCompiler = Join-Path $nextAdminRoot "node_modules/typescript/bin/tsc"
    $hostDependenciesReady = (
        ((Test-Path $nextBinary -PathType Leaf) -or (Test-Path $nextBinaryUnix -PathType Leaf)) -and
        (Test-Path $hostTypeScriptCompiler -PathType Leaf)
    )
    if (-not $hostDependenciesReady) {
        Write-Host "Installing pinned Next.js administration host dependencies..."
        Push-Location $nextAdminRoot
        try {
            & npm install --ignore-scripts --no-audit --no-fund --package-lock=false
            if ($LASTEXITCODE -ne 0) { throw "Next.js administration host dependency restore failed." }
        } finally {
            Pop-Location
        }
    }

    Push-Location $nextAdminRoot
    try {
        & npm run typecheck
        if ($LASTEXITCODE -ne 0) { throw "Next.js administration host typecheck failed." }
        & npm run build
        if ($LASTEXITCODE -ne 0) { throw "Next.js administration host build failed." }
    } finally {
        Pop-Location
    }

    & dotnet --info
    if ($LASTEXITCODE -ne 0) { throw "SDK check failed." }
    & dotnet restore IdentityAccess.sln
    if ($LASTEXITCODE -ne 0) { throw "NuGet restore failed." }
    & dotnet build IdentityAccess.sln --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
    & dotnet build tools/IdentityAccess.DevPasswordHasher/IdentityAccess.DevPasswordHasher.csproj --configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Development password hasher build failed." }
    & dotnet test IdentityAccess.sln --configuration $Configuration --no-build --no-restore `
        --logger "trx;LogFileName=identity-access.trx" --results-directory (Join-Path $root "artifacts/test-results")
    if ($LASTEXITCODE -ne 0) { throw "Tests failed." }
} finally {
    Pop-Location
}
