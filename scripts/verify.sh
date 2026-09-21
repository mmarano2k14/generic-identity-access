#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
command -v dotnet >/dev/null || { echo ".NET 10 SDK is required." >&2; exit 1; }
dotnet --info
dotnet restore IdentityAccess.sln
dotnet build IdentityAccess.sln --configuration Release --no-restore
dotnet test IdentityAccess.sln --configuration Release --no-build --no-restore \
  --logger 'trx;LogFileName=identity-access.trx' --results-directory artifacts/test-results
