$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw 'Instale o .NET 8 SDK para compilar.' }
$out = Join-Path $here 'publish'
& dotnet publish (Join-Path $here 'FoxESSMonitor.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $out
if ($LASTEXITCODE -ne 0) { throw 'A compilação do FoxESS Monitor falhou.' }
Write-Host "Executável criado em: $out"
