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

function Install-WingetPackage {
    param([string]$Id)
    & winget install --id $Id --source winget --exact --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to install package with winget: $Id"
    }
}

function Ensure-NvmInPath {
    $nvmHome = Join-Path $env:ProgramFiles 'nvm'
    if (Test-Path $nvmHome) {
        if ($env:PATH -notlike "*$nvmHome*") {
            $env:PATH = "$nvmHome;$env:PATH"
        }
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
    Install-WingetPackage -Id 'Microsoft.DotNet.SDK.10'
}

function Install-NvmAndNode {
    Write-Step 'Installing NVM for Windows'
    Install-WingetPackage -Id 'CoreyButler.NVMforWindows'

    Ensure-NvmInPath
    if (-not (Test-Command nvm)) {
        throw 'NVM for Windows was installed, but nvm is not available in this shell. Open a new terminal and run again with -SkipNode:$false.'
    }

    Write-Step 'Installing latest Node.js LTS'
    & nvm install lts
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to install Node.js LTS with nvm.'
    }

    & nvm use lts
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to set Node.js LTS as active version with nvm.'
    }
}

function Install-PowerShellWindows {
    Write-Step 'Installing PowerShell'
    Install-WingetPackage -Id 'Microsoft.PowerShell'
}

function Install-VSCodeWindows {
    Write-Step 'Installing Visual Studio Code'
    Install-WingetPackage -Id 'Microsoft.VisualStudioCode'
}

function Get-CodeCommand {
    if (Test-Command code) {
        return 'code'
    }

    $candidates = @(
        (Join-Path $env:LocalAppData 'Programs\Microsoft VS Code\bin\code.cmd'),
        (Join-Path $env:ProgramFiles 'Microsoft VS Code\bin\code.cmd')
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
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
                url = 'https://mcp.context7.com/mcp'
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

if (-not $IsWindows) {
    throw 'This script is intended for Windows.'
}

if (-not (Test-Command winget)) {
    throw 'winget is required on Windows to run this script.'
}

if (-not $SkipDotNet) {
    Install-DotNet
}

if (-not $SkipNode) {
    Install-NvmAndNode
}

if (-not $SkipPowerShell) {
    Install-PowerShellWindows
}

if (-not $SkipVSCode) {
    Install-VSCodeWindows
}

if (-not $SkipExtensions) {
    Install-VSCodeExtensions
}

if (-not $SkipContext7Config) {
    Write-WorkspaceFiles
}

Write-Step 'Windows environment bootstrap completed'
