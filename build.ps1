$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) { throw 'Instale o .NET 8 SDK para compilar.' }
$out = Join-Path (Split-Path -Parent $here) 'FoxESS-Monitor'
& dotnet publish (Join-Path $here 'FoxESSMonitor.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $out
