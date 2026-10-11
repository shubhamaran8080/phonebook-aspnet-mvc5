# ---------------------------------------------------------------------------
# Removes the development connection string from a published Web.config so
# that no database credential is baked into the Docker image.
#
# The real connection string is supplied at run time from the
# DEFAULT_CONNECTION_STRING environment variable (see docker/entrypoint.ps1).
#
# This script never introduces a credential and never deletes any data.
# ---------------------------------------------------------------------------
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -Path $Path)) {
    throw "Web.config not found at: $Path"
}

[xml]$doc = Get-Content -Path $Path -Raw

$node = $doc.configuration.connectionStrings.add | Where-Object { $_.name -eq 'DefaultConnection' }
if ($null -ne $node) {
    $node.connectionString = ''
    $doc.Save($Path)
    Write-Host 'DefaultConnection cleared in the image; supply DEFAULT_CONNECTION_STRING at run time.'
}
else {
    Write-Host 'No DefaultConnection entry found - nothing to clear.'
}
