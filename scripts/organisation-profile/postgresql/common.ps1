function Get-OrganisationProfileConnectionString {
    $connectionString =
        $env:ORGANISATION_PROFILE_POSTGRES_DEFAULT

    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        $connectionString =
            $env:IDENTITY_ACCESS_POSTGRES_DEFAULT
    }

    return $connectionString
}

function Get-OrganisationProfilePostgresSettings {
    $connectionString = Get-OrganisationProfileConnectionString

    $values = @{}

    if (-not [string]::IsNullOrWhiteSpace($connectionString)) {
        foreach ($segment in $connectionString -split ';') {
            if ([string]::IsNullOrWhiteSpace($segment)) {
                continue
            }

            $parts = $segment -split '=', 2
            if ($parts.Count -ne 2) {
                continue
            }

            $values[$parts[0].Trim().ToLowerInvariant()] =
                $parts[1].Trim()
        }
    }

    $hostName = if ($values.ContainsKey("host")) {
        $values["host"]
    } else {
        "127.0.0.1"
    }

    $port = if ($values.ContainsKey("port")) {
        $values["port"]
    } else {
        "5432"
    }

    $database = if ($env:ORGANISATION_PROFILE_POSTGRES_DATABASE) {
        $env:ORGANISATION_PROFILE_POSTGRES_DATABASE
    } elseif ($env:IDENTITY_ACCESS_POSTGRES_DATABASE) {
        $env:IDENTITY_ACCESS_POSTGRES_DATABASE
    } elseif ($values.ContainsKey("database")) {
        $values["database"]
    } else {
        "generic_identity_access_default"
    }

    $user = if ($env:ORGANISATION_PROFILE_POSTGRES_USER) {
        $env:ORGANISATION_PROFILE_POSTGRES_USER
    } elseif ($env:IDENTITY_ACCESS_POSTGRES_USER) {
        $env:IDENTITY_ACCESS_POSTGRES_USER
    } elseif ($values.ContainsKey("username")) {
        $values["username"]
    } elseif ($values.ContainsKey("user id")) {
        $values["user id"]
    } else {
        "postgres"
    }

    $password = if ($values.ContainsKey("password")) {
        $values["password"]
    } elseif ($env:PGPASSWORD) {
        $env:PGPASSWORD
    } else {
        $null
    }

    return [pscustomobject]@{
        Host = $hostName
        Port = $port
        Database = $database
        User = $user
        Password = $password
    }
}

function Invoke-OrganisationProfilePsqlText {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Sql
    )

    $settings =
        Get-OrganisationProfilePostgresSettings

    $previousPassword = $env:PGPASSWORD

    try {
        if ($settings.Password) {
            $env:PGPASSWORD = $settings.Password
        }

        $output = & psql `
            -h $settings.Host `
            -p $settings.Port `
            -U $settings.User `
            -d $settings.Database `
            -v ON_ERROR_STOP=1 `
            -At `
            -F "|" `
            -c $Sql

        if ($LASTEXITCODE -ne 0) {
            throw "PostgreSQL command failed."
        }

        if ($null -eq $output) {
            return ""
        }

        return ($output | Out-String).Trim()
    }
    finally {
        if ($null -eq $previousPassword) {
            Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        } else {
            $env:PGPASSWORD = $previousPassword
        }
    }
}

function Invoke-OrganisationProfilePsqlFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [switch]$SingleTransaction
    )

    $settings =
        Get-OrganisationProfilePostgresSettings

    $previousPassword = $env:PGPASSWORD

    try {
        if ($settings.Password) {
            $env:PGPASSWORD = $settings.Password
        }

        $arguments = @(
            "-h", $settings.Host,
            "-p", $settings.Port,
            "-U", $settings.User,
            "-d", $settings.Database,
            "-v", "ON_ERROR_STOP=1"
        )

        if ($SingleTransaction) {
            $arguments += "--single-transaction"
        }

        $arguments += @("-f", $Path)

        & psql @arguments

        if ($LASTEXITCODE -ne 0) {
            throw "PostgreSQL file '$Path' failed."
        }
    }
    finally {
        if ($null -eq $previousPassword) {
            Remove-Item Env:PGPASSWORD -ErrorAction SilentlyContinue
        } else {
            $env:PGPASSWORD = $previousPassword
        }
    }
}

function Get-OrganisationProfileMigrationChecksum {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $sql = [System.IO.File]::ReadAllText($Path)
    $canonical =
        $sql.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes =
        [System.Text.UTF8Encoding]::new($false).GetBytes($canonical)

    $sha =
        [System.Security.Cryptography.SHA256]::Create()

    try {
        return -join (
            $sha.ComputeHash($bytes) |
                ForEach-Object { $_.ToString("x2") }
        )
    }
    finally {
        $sha.Dispose()
    }
}

function Get-OrganisationProfileProbeConnectionString {
    $connectionString =
        Get-OrganisationProfileConnectionString

    if (-not [string]::IsNullOrWhiteSpace($connectionString)) {
        return $connectionString
    }

    $settings =
        Get-OrganisationProfilePostgresSettings

    if ([string]::IsNullOrWhiteSpace($settings.Password)) {
        throw "Set ORGANISATION_PROFILE_POSTGRES_DEFAULT, IDENTITY_ACCESS_POSTGRES_DEFAULT, or PGPASSWORD before running the PostgreSQL probe."
    }

    return "Host=$($settings.Host);Port=$($settings.Port);Database=$($settings.Database);Username=$($settings.User);Password=$($settings.Password)"
}
