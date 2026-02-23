========================================
PedalTracker v1.0.1
========================================

REQUISITOS:
-----------
- Windows 10/11
- .NET 10.0 Runtime (será instalado automaticamente se necessário)
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
- Os dados são guardados na pasta "Data"
- Os eixos estão configurados para:
  * Y = Acelerador (Throttle)
  * RZ = Travão (Brake)

RESOLUÇÃO DE PROBLEMAS:
-----------------------
Se os pedais não forem detectados:
1. Verifique se os pedais estão ligados no Windows (joy.cpl)
2. Verifique se o driver Fanatec está instalado
3. Execute o programa como Administrador

Se os eixos estiverem trocados:
- É necessário reconfigurar o código fonte

FICHEIROS INCLUÍDOS:
--------------------
- PedalTracker.Ui.exe      - Aplicação principal (WPF)
- PedalTracker.exe         - Versão consola
- PedalTracker.dll         - Biblioteca principal
- SharpDX.dll              - Dependência DirectInput
- SharpDX.DirectInput.dll  - Dependência DirectInput
- *.deps.json              - Dependências
- *.runtimeconfig.json     - Configuração runtime
- Data/                    - Pasta para guardar dados

========================================
Desenvolvido para monitorizar telemetria de pedais Fanatec
========================================
