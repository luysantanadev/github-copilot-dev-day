# GitHub Copilot Dev Day Curitiba Lab

Repositório de apoio do lab usado no GitHub Copilot Dev Day em Curitiba, realizado em 9 de maio de 2026. A proposta do laboratório foi usar o GitHub Copilot para gerar uma aplicação full stack com .NET e Vue.js sem escrever código manualmente, guiando a implementação por prompts e refinamentos.

## Evento

- Nome: GitHub Copilot Dev Days | Curitiba
- Data: 9 de maio de 2026
- Horário: 9h às 18h
- Local: Arena Mario de Abreu - Bloco Verde, R. Imac. Conceição, 1155, Prado Velho, Curitiba - PR
- Organização no Luma: Felipe Augusto e Welington Silva
- Página do evento: <https://luma.com/v0nveavb>

Segundo a descrição oficial do evento, a programação combinou palestras pela manhã, pausa para almoço e networking entre 12h e 14h, e laboratórios práticos de 3 horas à tarde para explorar o GitHub Copilot em cenários reais de desenvolvimento.

## O Que Foi Construído No Lab

Os prompts deste repositório descrevem a criação de uma aplicação de lista de tarefas com:

- backend em ASP.NET Minimal API
- frontend em Vue.js
- logs de requisição
- configuração por `.env`
- persistência com SQLite
- autenticação básica com usuário `admin/admin`
- conteinerização com Docker e Docker Compose
- backend em .NET 10
- frontend com Node.js LTS gerenciado por NVM

Os arquivos-base usados no exercício estão em [prompts/01.zeroshot.prompts.md](prompts/01.zeroshot.prompts.md) e [prompts/02.context.prompt.md](prompts/02.context.prompt.md).

## Estrutura Atual

- [backend](backend): artefatos do backend `TodoApi` gerados durante o lab
- [frontend](frontend): dependências do frontend usadas no exercício
- [prompts](prompts): prompts utilizados para conduzir a geração da aplicação
- [scripts/setup-devday-windows.ps1](scripts/setup-devday-windows.ps1): bootstrap para Windows
- [scripts/setup-devday-linux.ps1](scripts/setup-devday-linux.ps1): bootstrap para Linux
- [scripts/setup-devday-macos.ps1](scripts/setup-devday-macos.ps1): bootstrap para macOS
- [scripts/setup-devday-unix.ps1](scripts/setup-devday-unix.ps1): launcher de compatibilidade que encaminha para o script correto
- [.vscode/mcp.json](.vscode/mcp.json): configuração de MCP do Context7 para o workspace
- [.vscode/extensions.json](.vscode/extensions.json): recomendações de extensões do VS Code

## Preparação Do Ambiente

Use o script da sua plataforma:

```powershell
pwsh ./scripts/setup-devday-windows.ps1
pwsh ./scripts/setup-devday-linux.ps1
pwsh ./scripts/setup-devday-macos.ps1
```

O launcher [scripts/setup-devday-unix.ps1](scripts/setup-devday-unix.ps1) também funciona e redireciona automaticamente para Linux, macOS ou Windows.

Os scripts fazem o seguinte:

- instala o SDK do .NET 10 no diretório do usuário
- instala o NVM e já define a versão LTS mais recente do Node.js como padrão
- instala ou atualiza o PowerShell
- instala o Visual Studio Code
- instala as extensões `GitHub.copilot` e `timheuer.awesome-copilot`
- grava a configuração do Context7 em `.vscode/mcp.json`

Execução:

```powershell
pwsh ./scripts/setup-devday-<plataforma>.ps1
```

Opções úteis:

```powershell
pwsh ./scripts/setup-devday-<plataforma>.ps1 -SkipPowerShell
pwsh ./scripts/setup-devday-<plataforma>.ps1 -SkipVSCode
pwsh ./scripts/setup-devday-<plataforma>.ps1 -SkipExtensions
```

Observações:

- Como o bootstrap principal está em PowerShell, a primeira instalação do `pwsh` em uma máquina totalmente limpa pode exigir o instalador oficial antes da execução do script.
- No Windows, o script usa `winget` para instalar .NET, PowerShell, VS Code e NVM for Windows.
- No macOS, o script usa Homebrew para instalar PowerShell e VS Code quando necessário.
- No Linux, a automação completa de PowerShell e VS Code está preparada para Ubuntu e Debian. Outras distribuições podem exigir adaptação.

## Instalar PowerShell Core

Windows (winget):

```powershell
winget install --id Microsoft.PowerShell --source winget --exact
```

Linux (`.deb` - Debian/Ubuntu):

```bash
sudo apt-get update
sudo apt-get install -y wget apt-transport-https software-properties-common gpg
source /etc/os-release
wget -q https://packages.microsoft.com/config/$ID/$VERSION_ID/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm -f packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y powershell
```

Linux (genérico RPM):

```bash
# Exemplo para Fedora/RHEL compatíveis com o pacote publicado pela Microsoft
sudo dnf install -y https://packages.microsoft.com/config/rhel/9/packages-microsoft-prod.rpm
sudo dnf install -y powershell
```

macOS (Homebrew):

```bash
brew install --cask powershell
```

## Context7 No VS Code

O arquivo [.vscode/mcp.json](.vscode/mcp.json) já deixa o workspace pronto para usar o servidor MCP remoto do Context7:

- servidor HTTP: `https://mcp.context7.com/mcp`
- chave pedida via input seguro do próprio VS Code
- sem gravar segredo diretamente no repositório

Ao abrir o workspace no VS Code, informe a chave da API do Context7 quando o cliente MCP solicitar.

## Objetivo Deste Repositório

Este repositório serve como material de apoio para demonstrar um fluxo de desenvolvimento assistido por IA com GitHub Copilot, desde a geração inicial via prompt até a preparação do ambiente, configuração de ferramentas e documentação do laboratório.
