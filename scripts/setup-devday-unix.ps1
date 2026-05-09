[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ForwardArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptToRun = if ($IsLinux) {
    Join-Path $PSScriptRoot 'setup-devday-linux.ps1'
}
elseif ($IsMacOS) {
    Join-Path $PSScriptRoot 'setup-devday-macos.ps1'
}
elseif ($IsWindows) {
    Join-Path $PSScriptRoot 'setup-devday-windows.ps1'
}
else {
    throw 'Unsupported operating system.'
}

if (-not (Test-Path $scriptToRun)) {
    throw "Script not found: $scriptToRun"
}

& $scriptToRun @ForwardArgs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
