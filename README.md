# iRacingWheelSync

Utilitário para Windows que sincroniza o GUID da instância do volante entre os arquivos de controle do iRacing e o dispositivo atualmente conectado ao computador. Isso ajuda o iRacing a reconhecer o volante quando o `InstanceGUID` muda.

## Como funciona

- Verifica a cada 3 segundos se a interface ou o simulador do iRacing está em execução. Enquanto algum deles estiver aberto, aguarda antes de acessar os arquivos.
- Lê os dispositivos de controle conectados pelo DirectInput e procura um `ProductGUID` já registrado nas configurações do iRacing.
- Compara o `InstanceGUID` atual do dispositivo com o GUID configurado.
- Se os GUIDs forem diferentes, substitui o GUID antigo pelo novo em `joyCalib.yaml` e troca a representação binária correspondente em `controls.cfg`.
- Registra eventos em `iRacingWheelSyncLog.log`, no diretório do executável, e mostra notificações do Windows em situações relevantes.

Se os GUIDs já forem iguais, nenhuma alteração é feita. Se o dispositivo ou os arquivos necessários não forem encontrados, o programa registra o estado e tenta novamente no próximo ciclo.

## Requisitos

- Windows.
- .NET 10 SDK para compilar o projeto.
- Um volante conectado e reconhecido pelo DirectInput.
- Os arquivos de configuração do iRacing existentes em `Documentos\iRacing\profiles\controls\Baseline`:
  - `joyCalib.yaml`
  - `controls.cfg`
- O volante já configurado pelo iRacing para que seu `ProductGUID` esteja presente em `joyCalib.yaml`.

O programa usa o pacote [Vortice.DirectInput](https://www.nuget.org/packages/Vortice.DirectInput/) (versão 3.8.3).

## Compilar e executar

Na pasta do repositório, restaure os pacotes e compile em Release:

```powershell
dotnet restore .\iRacingWheelSync.sln
dotnet build .\iRacingWheelSync.sln -c Release
```

Para iniciar pelo SDK:

```powershell
dotnet run --project .\iRacingWheelSync.csproj
```

Também é possível abrir a solução `iRacingWheelSync.sln` no Visual Studio ou Rider e executar o projeto. Inicie o programa com o volante conectado e feche a interface e o simulador do iRacing para permitir a sincronização. O utilitário permanece em execução e verifica o estado continuamente; para encerrá-lo, finalize o processo pelo Gerenciador de Tarefas.

## Release do Windows

O workflow manual em `.github/workflows/release.yml` restaura e publica uma versão `win-x64` autocontida em arquivo único. Para usá-lo no GitHub, abra **Actions**, selecione **Build Release** e execute **Run workflow**. O workflow cria uma release com o executável `iRacingWheelSync.exe`.

## Arquivos do projeto

| Arquivo | Responsabilidade |
| --- | --- |
| `Program.cs` | Monitoramento do iRacing, descoberta do dispositivo DirectInput, comparação dos GUIDs e atualização dos arquivos. |
| `LogService.cs` | Criação e escrita do arquivo de log ao lado do executável. |
| `NotificationService.cs` | Exibição de notificações toast do Windows. |
| `iRacingWheelSync.csproj` | Configuração do aplicativo .NET 10, ícone e dependência DirectInput. |
| `.github/workflows/release.yml` | Publicação manual do executável autocontido para Windows x64. |
| `Resources/fwheel.ico` | Ícone do aplicativo. |

## Observações

- O diretório `Baseline` é fixo no código; não há opção de linha de comando ou configuração para escolher outro perfil.
- O aplicativo altera diretamente `joyCalib.yaml` e `controls.cfg`. Faça uma cópia de segurança desses arquivos antes do primeiro uso.
- O programa não cria backups nem oferece uma interface para pausar ou encerrar o monitoramento.
- A sincronização só ocorre quando o iRacing está fechado e o volante configurado é encontrado nos arquivos e entre os dispositivos conectados.
