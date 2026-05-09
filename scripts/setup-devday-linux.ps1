[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path,
    [switch]$SkipDotNet,
    [switch]$SkipNode,
    [switch]$SkipPowerShell,
    [switch]$SkipVSCode,
    [switch]$SkipExtensions,
    [switch]$SkipContext7Config
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$dotNetChannel = '10.0'
$nvmVersion = 'v0.40.4'
$context7Url = 'https://mcp.context7.com/mcp'
$extensionIds = @(
    'GitHub.copilot',
    'timheuer.awesome-copilot'
)

function Write-Step {
    param([string]$Message)
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Test-Command {
    param([string]$Name)
    return $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Invoke-Bash {
    param([string]$Script)
    & /bin/bash -lc $Script
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed: $Script"
    }
}

function Get-LinuxInfo {
    $values = @{}
    foreach ($line in Get-Content -Path '/etc/os-release') {
        if ($line -match '^(?<key>[A-Z_]+)=(?<value>.*)$') {
            $values[$Matches.key] = $Matches.value.Trim('"')
        }
    }

    return [pscustomobject]@{
        Id = $values['ID']
        VersionId = $values['VERSION_ID']
    }
}

function Add-ProfileBlock {
    param(
        [string]$Path,
        [string]$Block
    )

    if (-not (Test-Path $Path)) {
        New-Item -ItemType File -Path $Path -Force | Out-Null
    }

    $current = Get-Content -Path $Path -Raw
    if ($current -notlike "*$Block*") {
        Add-Content -Path $Path -Value "`n$Block`n"
    }
}

function Ensure-ProfileExports {
    $dotNetBlock = @'
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools"
'@

    $nvmBlock = @'
export NVM_DIR="$([ -z "${XDG_CONFIG_HOME-}" ] && printf %s "${HOME}/.nvm" || printf %s "${XDG_CONFIG_HOME}/nvm")"
[ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh"
[ -s "$NVM_DIR/bash_completion" ] && . "$NVM_DIR/bash_completion"
'@

    foreach ($profile in @(
        (Join-Path $HOME '.bashrc'),
        (Join-Path $HOME '.zshrc'),
        (Join-Path $HOME '.profile')
    )) {
        Add-ProfileBlock -Path $profile -Block $dotNetBlock
        Add-ProfileBlock -Path $profile -Block $nvmBlock
    }
}

function Install-DotNet {
    if (Test-Command dotnet) {
        $hasDotNet10 = (& dotnet --list-sdks) -match '^10\.'
        if ($hasDotNet10) {
            Write-Step '.NET 10 SDK already installed'
            return
        }
    }

    Write-Step 'Installing .NET 10 SDK'
    $scriptPath = Join-Path ([System.IO.Path]::GetTempPath()) 'dotnet-install.sh'
    Invoke-Bash @"
curl -fsSL https://dot.net/v1/dotnet-install.sh -o '$scriptPath'
chmod +x '$scriptPath'
'$scriptPath' --channel '$dotNetChannel' --install-dir '$HOME/.dotnet'
"@
}

function Install-NvmAndNode {
    Write-Step 'Installing NVM and latest Node.js LTS'
    Invoke-Bash (@'
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/{0}/install.sh | bash
export NVM_DIR="$([ -z "${XDG_CONFIG_HOME-}" ] && printf %s "${HOME}/.nvm" || printf %s "${XDG_CONFIG_HOME}/nvm")"
[ -s "$NVM_DIR/nvm.sh" ] && . "$NVM_DIR/nvm.sh"
nvm install --lts
nvm alias default '"'"'lts/*'"'"'
'@ -f $nvmVersion)
}

function Install-PowerShellLinux {
    $linuxInfo = Get-LinuxInfo
    if ($linuxInfo.Id -notin @('ubuntu', 'debian')) {
        throw 'Automatic PowerShell installation is currently supported by this script only on Ubuntu and Debian.'
    }

    Write-Step 'Installing PowerShell'
    Invoke-Bash @'
if [ "$(id -u)" -eq 0 ]; then SUDO=""; else SUDO="sudo"; fi
$SUDO apt-get update
$SUDO apt-get install -y wget apt-transport-https software-properties-common gpg
source /etc/os-release
wget -q https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
$SUDO dpkg -i packages-microsoft-prod.deb
rm -f packages-microsoft-prod.deb
$SUDO apt-get update
$SUDO apt-get install -y powershell
'@
}

function Install-VSCodeLinux {
    $linuxInfo = Get-LinuxInfo
    if ($linuxInfo.Id -notin @('ubuntu', 'debian')) {
        if (Test-Command snap) {
            Write-Step 'Installing Visual Studio Code with snap'
            Invoke-Bash @'
if [ "$(id -u)" -eq 0 ]; then SUDO=""; else SUDO="sudo"; fi
$SUDO snap install --classic code
'@
            return
        }

        throw 'Automatic VS Code installation is currently supported by this script on Ubuntu, Debian, or Linux distributions with snap.'
    }

    Write-Step 'Installing Visual Studio Code'
    Invoke-Bash @'
if [ "$(id -u)" -eq 0 ]; then SUDO=""; else SUDO="sudo"; fi
$SUDO apt-get update
$SUDO apt-get install -y wget gpg apt-transport-https
wget -qO- https://packages.microsoft.com/keys/microsoft.asc | $SUDO gpg --dearmor -o /usr/share/keyrings/microsoft.gpg
printf "Types: deb\nURIs: https://packages.microsoft.com/repos/code\nSuites: stable\nComponents: main\nArchitectures: amd64,arm64,armhf\nSigned-By: /usr/share/keyrings/microsoft.gpg\n" | $SUDO tee /etc/apt/sources.list.d/vscode.sources > /dev/null
$SUDO apt-get update
$SUDO apt-get install -y code
'@
}

function Get-CodeCommand {
    if (Test-Command code) {
        return 'code'
    }
    throw 'VS Code CLI was not found after installation.'
}

function Install-VSCodeExtensions {
    $codeCommand = Get-CodeCommand
    Write-Step 'Installing VS Code extensions'

    foreach ($extensionId in $extensionIds) {
        & $codeCommand '--install-extension' $extensionId '--force'
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install VS Code extension: $extensionId"
        }
    }
}

function Write-WorkspaceFiles {
    Write-Step 'Writing VS Code workspace configuration'
    $vscodeDirectory = Join-Path $RepoRoot '.vscode'
    if (-not (Test-Path $vscodeDirectory)) {
        New-Item -ItemType Directory -Path $vscodeDirectory -Force | Out-Null
    }

    $mcpConfig = @{
        inputs = @(
            @{
                type = 'promptString'
                id = 'context7-api-key'
                description = 'Context7 API key'
                password = $true
            }
        )
        servers = @{
            context7 = @{
                type = 'http'
                url = $context7Url
                headers = @{
                    CONTEXT7_API_KEY = '${input:context7-api-key}'
                }
            }
        }
    }

    $extensionsConfig = @{
        recommendations = $extensionIds
    }

    $mcpConfig | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $vscodeDirectory 'mcp.json') -Encoding utf8
    $extensionsConfig | ConvertTo-Json -Depth 3 | Set-Content -Path (Join-Path $vscodeDirectory 'extensions.json') -Encoding utf8
}

if (-not $IsLinux) {
    throw 'This script is intended for Linux.'
}

Ensure-ProfileExports

if (-not $SkipDotNet) {
    Install-DotNet
}

if (-not $SkipNode) {
    Install-NvmAndNode
}

if (-not $SkipPowerShell) {
    Install-PowerShellLinux
}

if (-not $SkipVSCode) {
    Install-VSCodeLinux
}

if (-not $SkipExtensions) {
    Install-VSCodeExtensions
}

if (-not $SkipContext7Config) {
    Write-WorkspaceFiles
}

Write-Step 'Linux environment bootstrap completed'
