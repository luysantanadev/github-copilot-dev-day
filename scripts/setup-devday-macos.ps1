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
        (Join-Path $HOME '.bash_profile'),
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

function Ensure-Homebrew {
    if (Test-Command brew) {
        return
    }

    Write-Step 'Installing Homebrew'
    Invoke-Bash @'
NONINTERACTIVE=1 /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
'@

    if (Test-Path '/opt/homebrew/bin/brew') {
        $env:PATH = "/opt/homebrew/bin:$env:PATH"
    }

    if (Test-Path '/usr/local/bin/brew') {
        $env:PATH = "/usr/local/bin:$env:PATH"
    }
}

function Install-PowerShellMacOS {
    Ensure-Homebrew
    Write-Step 'Installing PowerShell'
    Invoke-Bash 'brew list --cask powershell >/dev/null 2>&1 || brew install --cask powershell'
}

function Install-VSCodeMacOS {
    Ensure-Homebrew
    Write-Step 'Installing Visual Studio Code'
    Invoke-Bash 'brew list --cask visual-studio-code >/dev/null 2>&1 || brew install --cask visual-studio-code'
}

function Get-CodeCommand {
    if (Test-Command code) {
        return 'code'
    }

    $macCodePath = '/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code'
    if (Test-Path $macCodePath) {
        return $macCodePath
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

if (-not $IsMacOS) {
    throw 'This script is intended for macOS.'
}

Ensure-ProfileExports

if (-not $SkipDotNet) {
    Install-DotNet
}

if (-not $SkipNode) {
    Install-NvmAndNode
}

if (-not $SkipPowerShell) {
    Install-PowerShellMacOS
}

if (-not $SkipVSCode) {
    Install-VSCodeMacOS
}

if (-not $SkipExtensions) {
    Install-VSCodeExtensions
}

if (-not $SkipContext7Config) {
    Write-WorkspaceFiles
}

Write-Step 'macOS environment bootstrap completed'
