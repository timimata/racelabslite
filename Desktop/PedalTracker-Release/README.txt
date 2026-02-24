SOBRE O PROJETO:
----------------
O PedalTracker é uma aplicação para Windows desenvolvida para monitorizar e registar dados de pedais Fanatec, utilizada principalmente em simulação automóvel. Permite visualizar em tempo real e guardar a telemetria dos pedais (acelerador e travão), facilitando a análise de desempenho e a resolução de problemas de hardware ou condução. Inclui uma interface gráfica (WPF) e uma versão de consola.

ARQUITETURA E OBTENÇÃO DE DADOS:
---------------------------------
O projeto utiliza uma abordagem híbrida para recolha de dados:
1. Telemetria do Simulador: Utiliza a biblioteca IRSDKSharper para aceder aos dados do iRacing (ABS, tempos de volta, delta) via Memory Mapped Files.
2. Hardware Input: Utiliza DirectInput para leitura direta dos eixos físicos (RAW Input), permitindo diagnóstico de hardware independente do simulador.

O fluxo principal é:
- IRacingService liga-se ao iRacing e lê os dados de telemetria (ex: Throttle, Brake, ABSActive, tempos de volta).
- Os dados são enviados por eventos para a MainWindow, que atualiza os gráficos e indicadores visuais.
- O componente TelemetryChart desenha os gráficos dos pedais em tempo real.
- Os dados podem ser guardados para análise posterior na pasta Data/.

O código está organizado em:
- PedalTelemetry/Services/IRacingService.cs: ligação e leitura da telemetria do iRacing
- PedalTelemetry/TelemetryChart.cs: visualização gráfica dos dados dos pedais
- PedalTelemetry/MainWindow.xaml(.cs): interface gráfica e lógica de apresentação

========================================
PedalTracker v1.0.1
========================================

REQUISITOS:
-----------
- Windows 10/11
- .NET Runtime (Verificar INSTALL.bat ou usar versão Self-Contained)
- Pedais Fanatec conectados

COMO EXECUTAR:
--------------
1. Extrair todos os ficheiros para uma pasta
2. Executar "PedalTracker.Ui.exe" (interface gráfica)
   OU
   Executar "PedalTracker.exe" (versão consola)

CONFIGURAÇÃO:
-------------
- O programa irá detectar automaticamente os pedais Fanatec
- As configurações de eixos e sensibilidade podem ser alteradas no ficheiro 'appsettings.json' (se implementado) ou requerem recompilação na versão atual.
- Padrão atual:
  * Eixo Y  -> Acelerador
  * Eixo RZ -> Travão

RESOLUÇÃO DE PROBLEMAS:
-----------------------
Se os pedais não forem detectados:
1. Verifique se os pedais estão ligados no Windows (joy.cpl)
2. Verifique se o driver Fanatec está instalado
3. Execute o programa como Administrador

Se os eixos estiverem trocados:
- Verifique a configuração no código ou no ficheiro JSON de definições.

FICHEIROS INCLUÍDOS:
--------------------
- PedalTracker.Ui.exe      - Aplicação principal (WPF)
- PedalTracker.exe         - Versão consola
- PedalTracker.dll         - Biblioteca principal
- SharpDX.dll              - Dependência DirectInput
- SharpDX.DirectInput.dll  - Dependência DirectInput
- *.deps.json              - Dependências
- *.runtimeconfig.json     - Configuração runtime
- appsettings.json         - Ficheiro de configuração (Template)
- Data/                    - Pasta para guardar dados

ROADMAP (Futuro):
-----------------
[ ] Migração de SharpDX para Vortice.Windows (SharpDX descontinuado)
[ ] Interface de mapeamento de eixos (UI)
[ ] Suporte a múltiplos dispositivos USB simultâneos

========================================
Desenvolvido para monitorizar telemetria de pedais Fanatec
========================================
