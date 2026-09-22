param(
    [string]$OutputPath = ".\secrets\oidc-dev-signing-key.pem",
    [int]$KeySize = 3072
)

$ErrorActionPreference = "Stop"

if ($KeySize -lt 2048) {
    throw "OIDC RSA signing keys must be at least 2048 bits."
}

$fullPath = [System.IO.Path]::GetFullPath($OutputPath)
$directory = [System.IO.Path]::GetDirectoryName($fullPath)

if (-not [string]::IsNullOrWhiteSpace($directory)) {
    [System.IO.Directory]::CreateDirectory($directory) | Out-Null
}

$rsa = [System.Security.Cryptography.RSA]::Create()
$rsa.KeySize = $KeySize

try {
    $privateKey = $rsa.ExportPkcs8PrivateKey()
    $base64 = [System.Convert]::ToBase64String($privateKey)

    $lines = for ($offset = 0; $offset -lt $base64.Length; $offset += 64) {
        $length = [System.Math]::Min(64, $base64.Length - $offset)
        $base64.Substring($offset, $length)
    }

    $pem = @(
        "-----BEGIN PRIVATE KEY-----"
        $lines
        "-----END PRIVATE KEY-----"
        ""
    ) -join [System.Environment]::NewLine

    [System.IO.File]::WriteAllText(
        $fullPath,
        $pem,
        [System.Text.UTF8Encoding]::new($false))
}
finally {
    $rsa.Dispose()
}

Write-Host "Development OIDC signing key created:"
Write-Host $fullPath
Write-Host "Do not commit private signing keys to source control."
