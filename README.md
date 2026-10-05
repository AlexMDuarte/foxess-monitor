# FoxESS Monitor

Widget Windows x64 para FoxESS H1-3.7-E-G2.

O widget usa o logótipo FoxESS fornecido, pode ser arrastado pelo cabeçalho e redimensionado pelas bordas. O painel adapta a largura à janela e distribui a área de dados pela altura disponível. Arraste a área do cabeçalho (fora dos botões) para mover. O ícone aparece na janela, na área de notificação, no executável e nos atalhos/instalador. Os ficheiros de marca estão em `Assets/`.

## Utilizar

1. Instale com `FoxESS-Monitor-Setup-x64.exe` ou execute `FoxESS Monitor.exe` em `outputs/FoxESS-Monitor`.
2. O widget inicia em modo demonstração, sem fazer pedidos de rede. Use ⚙ para desativar a demo e configurar a chave API FoxESS. O botão “Testar ligação” verifica endpoint, chave e número de série antes de guardar.
3. Opcionalmente ative “Iniciar com o Windows” e “Manter widget sempre no topo”. O botão — oculta para o ícone junto ao relógio; clique duplo no ícone reabre.

A chave é cifrada com DPAPI do Windows e guardada em `%APPDATA%\FoxESS Monitor\settings.json`, vinculada ao utilizador e ao PC. Não é enviada a um backend externo nem ao frontend/browser. `appsettings.example.json` é apenas referência e não deve conter uma chave real.

O diagnóstico e os erros são registados em `%APPDATA%\FoxESS Monitor\FoxESS Monitor.log`. O ficheiro não inclui a API key.

## Dados

O widget consulta a FoxESS Open API V1 e mostra PV total/PV1/PV2, carga, importação/exportação, SoC, produção diária, estado, temperatura quando publicada pela API e hora de leitura. Campos indisponíveis no equipamento aparecem como “—”. O consumo da casa é estimado a partir de PV e potência líquida da rede quando a variável de carga falta.

Endpoint padrão: `https://www.foxesscloud.com` (apenas domínio, sem `/op/...`). O endereço `developer-eu.foxesscloud.com` é o portal de programadores, não o endpoint da API. Atualização padrão: 5 minutos. Cada ciclo usa até três chamadas: leitura V1, produção diária e detalhe do dispositivo. A documentação FoxESS indica limite de 1.440 chamadas por inversor/dia e máximo de uma chamada por segundo por interface; o widget mantém pelo menos cinco minutos entre atualizações. Erros de rede/API/rate limit são apresentados no widget e podem ser repetidos pelo botão ↻.

## Compilar

Requer Windows e .NET 8 SDK. Na pasta deste projeto:

```powershell
dotnet publish .\FoxESSMonitor.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\..\outputs\FoxESS-Monitor
```

Para recriar o instalador, instale Inno Setup e compile `FoxESSMonitor.iss`. A distribuição publicada é autónoma e não requer .NET previamente instalado. O script `Assets/Generate-Icons.ps1` regenera PNG e ICO a partir do SVG, se necessário.

## Privacidade e limitações

- API key local, cifrada por DPAPI. A aplicação só comunica com o endpoint HTTPS configurado.
- A FoxESS Cloud não fornece atualização instantânea; cinco minutos é o intervalo predefinido.
- A disponibilidade e os nomes de variáveis dependem do modelo/firmware/conta. A temperatura e a SoC podem não estar disponíveis.
- “Atualizar agora” envia pedidos imediatamente e deve ser usado com moderação para respeitar limites da FoxESS.
- Atualização automática aqui significa atualização dos dados. O programa não se autoatualiza; instale uma nova versão manualmente quando fornecida.
