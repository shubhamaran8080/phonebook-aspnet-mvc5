# ---------------------------------------------------------------------------
# Container entry point for the Phonebook ASP.NET MVC 5 application.
#
#   1. Injects the SQL Server connection string (and optional demo-user
#      settings) from environment variables into Web.config.
#   2. Starts IIS (w3svc) through ServiceMonitor.exe.
#
# Secrets are read from the environment only; they are never printed.
# ---------------------------------------------------------------------------
$ErrorActionPreference = 'Stop'

$webConfig = 'C:\inetpub\wwwroot\Web.config'

function Get-FirstEnvValue {
    param([string[]]$Names)
    foreach ($name in $Names) {
        $value = [Environment]::GetEnvironmentVariable($name)
        if (-not [string]::IsNullOrWhiteSpace($value)) { return $value }
    }
    return $null
}

# --- Connection string -----------------------------------------------------
$connectionString = Get-FirstEnvValue @('DEFAULT_CONNECTION_STRING', 'ConnectionStrings__DefaultConnection')

if ($connectionString) {
    [xml]$doc = Get-Content -Path $webConfig -Raw
    $node = $doc.configuration.connectionStrings.add | Where-Object { $_.name -eq 'DefaultConnection' }

    if ($null -eq $node) {
        $node = $doc.CreateElement('add')
        $node.SetAttribute('name', 'DefaultConnection')
        $node.SetAttribute('providerName', 'System.Data.SqlClient')
        [void]$doc.configuration.connectionStrings.AppendChild($node)
    }

    $node.connectionString = $connectionString
    $doc.Save($webConfig)
    Write-Host 'Applied DefaultConnection from the environment.'
}
else {
    Write-Warning 'DEFAULT_CONNECTION_STRING is not set. The application will not reach SQL Server.'
}

# --- Optional demo-user overrides -----------------------------------------
$overrides = [ordered]@{
    'DemoUserEnabled'  = Get-FirstEnvValue @('DEMO_USER_ENABLED')
    'DemoUserUsername' = Get-FirstEnvValue @('DEMO_USER_USERNAME')
    'DemoUserEmail'    = Get-FirstEnvValue @('DEMO_USER_EMAIL')
    'DemoUserPassword' = Get-FirstEnvValue @('DEMO_USER_PASSWORD')
}

if ($overrides.Values | Where-Object { $_ }) {
    [xml]$doc = Get-Content -Path $webConfig -Raw
    foreach ($key in $overrides.Keys) {
        $value = $overrides[$key]
        if (-not $value) { continue }

        $node = $doc.configuration.appSettings.add | Where-Object { $_.key -eq $key }
        if ($null -eq $node) {
            $node = $doc.CreateElement('add')
            $node.SetAttribute('key', $key)
            $node.SetAttribute('value', $value)
            [void]$doc.configuration.appSettings.AppendChild($node)
        }
        else {
            $node.value = $value
        }
    }
    $doc.Save($webConfig)
    Write-Host 'Applied demo-user overrides from the environment.'
}

# --- Start IIS -------------------------------------------------------------
Write-Host 'Starting IIS (w3svc)...'
& C:\ServiceMonitor.exe w3svc
