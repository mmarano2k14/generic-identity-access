[CmdletBinding()]
param()
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$foreign = @(
  "OrganizationDirectory.sln",
  "PACK_MANIFEST.txt",
  "docs/IMPLEMENTATION_ROADMAP.md",
  "src/OrganizationDirectory.Api",
  "src/OrganizationDirectory.Application",
  "src/OrganizationDirectory.Contracts",
  "src/OrganizationDirectory.Domain",
  "src/OrganizationDirectory.Infrastructure.PostgreSql",
  "tests/OrganizationDirectory.Tests"
)
foreach ($rel in $foreign) {
  $path = Join-Path $root $rel
  if (Test-Path $path) {
    Write-Host "Removing foreign Organization Directory item: $rel"
    Remove-Item $path -Recurse -Force
  }
}
Write-Host "Identity Access cleanup completed."
