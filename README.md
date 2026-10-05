# FoxESS Monitor

Widget moderno de desktop (Windows x64) para monitorização em tempo real de inversores solares FoxESS (com suporte nativo a inversores híbridos da série H1, baterias de armazenamento e inversores de rede).

O widget apresenta uma interface escura translúcida, pode ser arrastado pelo cabeçalho e redimensionado pelas bordas. O painel adapta a largura e distribui a área de dados responsivamente. Inclui integração completa com a área de notificação do Windows (System Tray), controlos de janela e persistência de posicionamento.

---

## Novidades da Versão 1.2.0

- 🔋 **Suporte Completo a Baterias Híbridas**:
  - Deteção e cálculo em tempo real da potência de carga e descarga da bateria (`batChargePower`, `batDischargePower`, `batPower`).
  - Apresentação do estado de atividade da bateria: *"A carregar X.XX kW"*, *"A descarregar X.XX kW"*, *"Bateria cheia"* ou *"Em repouso"*.
  - **Barra Visual de SoC**: Indicador gráfico dinâmico com escala de cores (Verde ≥50%, Âmbar 20–49%, Vermelho <20%).

- ⚡ **Balanço Energético e Autossuficiência Doméstica**:
  - **Cálculo Físico Exato do Consumo**: A fórmula de consumo doméstico agora compensa o fluxo de carga/descarga da bateria (`Consumo = PV + Importação - Exportação + Descarga - Carga`), garantindo valores fidedignos mesmo durante carregamento solar intensivo.
  - **Emblema de Autossuficiência**: Apresentação visual da taxa de cobertura solar do consumo da casa (ex.: `☀️ 100% Solar` ou `⚡ 65% Solar`).

- 🪟 **Gestão de Janela e Persistência**:
  - O widget memoriza e restaura a sua posição e tamanho no ecrã entre reinicializações, com validação de limites de ecrãs/monitores múltiplos.
  - Adicionado botão **Fechar (`✕`)** no cabeçalho, com comportamento configurável (fechar aplicação ou minimizar para o tabuleiro).

- ⌨️ **Atalhos de Teclado**:
  - `F5`: Atualizar dados imediatamente.
  - `Esc`: Minimizar para o tabuleiro do sistema.
  - `Ctrl+,` ou `Ctrl+S`: Abrir janela de definições.
  - `Ctrl+Q`: Encerrar o FoxESS Monitor.

- 🔔 **Melhorias no Tabuleiro do Sistema (System Tray)**:
  - **Tooltip Dinâmico**: Ao passar o rato pelo ícone junto ao relógio, vê imediatamente o resumo: `FoxESS: 3.42kW | Bat: 78% | Casa: 1.26kW`.
  - **Menu de Contexto Rápido**: Opções para alternar diretamente o *Modo Demonstração* e *Sempre no Topo* com um clique.

- ⚙️ **Ecrã de Definições Modernizado (Dark UI)**:
  - Interface escura consistente com o widget.
  - Botão de alternância de visibilidade da chave API (`👁` / `🙈`) para conferência e colagem segura.
  - Indicação do estado da credencial (*"✓ Chave guardada e protegida com DPAPI"*).
  - Novas opções: Lembrar posição da janela e comportamento do botão fechar.

- 🛡️ **Robustez de Rede e Diagnóstico**:
  - Tradução amigável dos códigos de erro da API FoxESS (`errno` 40256, 40257, 41807, 41808, 41809, 41810) para português claro.
  - Tratamento gracioso de perdas de ligação e tempos limite (*timeouts*).
  - Rotação de segurança do ficheiro de registo local (`%APPDATA%\FoxESS Monitor\FoxESS Monitor.log`) com limite de 5 MB.

- 🚀 **Modo Demonstração Realista**:
  - Simulação dinâmica baseada na hora do dia e curvas solares reais, permitindo explorar todas as funcionalidades sem necessidade de credenciais ou hardware ativo.

---

## Como Utilizar

1. **Instalação**:
   - Execute o instalador `dist/FoxESS-Monitor-Setup-x64.exe` ou execute diretamente a versão autónoma em `publish/FoxESS Monitor.exe`.
2. **Primeira Execução**:
   - O widget inicia por padrão em **Modo Demonstração** (sem chamadas de rede).
   - Clique em **⚙** (ou prima `Ctrl+,`) para abrir as Definições.
   - Desmarque a opção "Usar modo demonstração".
   - Introduza o seu **Número de Série do Inversor** e a sua **Chave API FoxESS**.
   - Clique em **Testar ligação** para verificar o endpoint, chave e número de série antes de guardar.
3. **Controlos e Minimização**:
   - **Mover**: Clique e arraste na área vazia do cabeçalho.
   - **Minimizar**: Clique em **—** ou prima `Esc` para recolher para a área de notificação junto ao relógio.
   - **Reabrir**: Clique duas vezes no ícone da raposa no tabuleiro do sistema ou clique com o botão direito e selecione "Abrir Widget".
   - **Fechar**: Clique em **✕** ou prima `Ctrl+Q`.

---

## Dados e Variáveis Monitorizadas

O FoxESS Monitor comunica com a **FoxESS Open API V1** e apresenta:

| Métrica | Descrição |
| :--- | :--- |
| **Produção Solar** | Potência total gerada pelos painéis fotovoltaicos em tempo real (kW). |
| **Strings PV1 / PV2** | Potência individual de cada entrada/string solar do inversor. |
| **Consumo da Casa** | Potência consumida pela habitação (kW), medida diretamente ou calculada pelo balanço de potências. |
| **Rede Elétrica** | Indicação e potência de exportação (verde) ou importação (âmbar) da rede pública. |
| **Bateria (SoC)** | Percentagem de carga da bateria (%), fluxo de carga/descarga (kW) e barra gráfica colorida. |
| **Produção Hoje / Mês** | Total de energia gerada no dia de hoje (kWh) e total acumulado no mês corrente. |
| **Estado do Inversor** | Indicador luminoso e texto (*Online*, *Falha*, *Offline*). |
| **Temperatura & Hora** | Temperatura de funcionamento do inversor (°C) e hora da última sincronização. |

> [!NOTE]
> **Políticas de Pedidos FoxESS**:
> O endpoint padrão é `https://www.foxesscloud.com` (apenas o domínio base). O portal de programadores (`developer-eu.foxesscloud.com`) é apenas a página Web onde obtém a sua chave e é automaticamente normalizado se for colado.
> A FoxESS Cloud estipula um limite de 1.440 chamadas por inversor/dia; o widget aplica um intervalo mínimo de 5 minutos entre atualizações automáticas para garantir conformidade e evitar bloqueios.

---

## Segurança e Privacidade

- **Cifragem Local DPAPI**: A sua chave API é cifrada com a API de Proteção de Dados do Windows (`CryptProtectData`) antes de ser guardada em `%APPDATA%\FoxESS Monitor\settings.json`. A chave fica vinculada exclusivamente à sua conta de utilizador neste computador.
- **Sem Servidores Intermédios**: O programa comunica diretamente e exclusivamente via HTTPS com a API oficial da FoxESS Cloud. Nenhuma informação ou métrica é enviada para terceiros.
- **Registo Local Seguro**: O ficheiro de diagnóstico `%APPDATA%\FoxESS Monitor\FoxESS Monitor.log` omite chaves de autenticação e credenciais, e possui rotação automática com limite de 5 MB.

---

## Compilação e Empacotamento

### Pré-requisitos
- Windows 10/11 x64
- .NET 8 SDK
- Inno Setup 6 (opcional, para gerar o instalador `Setup.exe`)

### Compilar com o Script Automatizado
O script `build.ps1` localiza o SDK .NET e compila a aplicação autónoma:

```powershell
# Compilar executável autónomo (publish/FoxESS Monitor.exe)
.\build.ps1

# Compilar executável e gerar instalador Inno Setup (dist/FoxESS-Monitor-Setup-x64.exe)
.\build.ps1 -BuildInstaller
```

### Compilação Manual via .NET CLI
```powershell
dotnet publish .\FoxESSMonitor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o .\publish
```

---

## Histórico de Alterações (Changelog)

### Versão 1.2.0
- **Suporte a Baterias Híbridas**: Deteção de potência de carga/descarga, estado operacional e barra visual de SoC com cores dinâmicas.
- **Cálculo de Autossuficiência**: Indicador em tempo real da percentagem de consumo suprido por energia solar/bateria.
- **Fórmula de Consumo Aprimorada**: Compensação do carregamento e descarregamento da bateria na estimativa da carga doméstica.
- **Persistência de Janela**: Memorização de coordenadas e dimensões do widget no ecrã.
- **Controlos e Atalhos**: Adicionado botão Fechar (`✕`), atalhos `F5`, `Esc`, `Ctrl+,` e `Ctrl+Q`.
- **System Tray Dinâmico**: Resumo das métricas no tooltip do ícone e atalhos rápidos no menu de contexto.
- **Definições com Dark Mode**: Interface escura renovada, botão de visibilidade da chave API (`👁`) e novas opções.
- **Tradução de Erros da API**: Mensagens em português para códigos de erro da FoxESS Cloud e problemas de rede.
- **Gestão de Logs**: Rotação automática de ficheiro de log limitada a 5 MB.
- **Script de Compilação Aprimorado**: Suporte a deteção automática de SDK e parâmetro `-BuildInstaller`.

### Versão 1.1.5
- Versão inicial com suporte a FoxESS H1-3.7-E-G2, Modo Demonstração e cifragem DPAPI.
