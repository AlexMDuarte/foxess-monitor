[CmdletBinding()]
param(
    [switch]$BuildInstaller
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

# Locate dotnet executable
$dotnetCmd = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnet = if ($dotnetCmd) { $dotnetCmd.Source } else { $null }

if (-not $dotnet) {
    $candidates = @(
        "$env:ProgramFiles\dotnet\dotnet.exe",
        "$env:LOCALAPPDATA\Microsoft\dotnet\dotnet.exe",
        (Join-Path $here "..\..\work\dotnet\sdk\dotnet.exe")
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) {
            $dotnet = (Resolve-Path $c).Path
            break
        }
    }
}

if (-not $dotnet) {
    throw 'O .NET 8 SDK não foi encontrado no PATH nem nos locais padrão. Instale o .NET 8 SDK para compilar.'
}

Write-Host "A utilizar .NET SDK: $dotnet" -ForegroundColor Cyan

# Publish self-contained executable
$out = Join-Path $here 'publish'
Write-Host "A compilar FoxESS Monitor (win-x64, self-contained)..." -ForegroundColor Cyan
& $dotnet publish (Join-Path $here 'FoxESSMonitor.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $out
if ($LASTEXITCODE -ne 0) {
    throw 'A compilação do FoxESS Monitor falhou.'
}
Write-Host "Executável criado com sucesso em: $out" -ForegroundColor Green

# Optional or automatic installer compilation if ISCC is found
if ($BuildInstaller) {
    $isccCmd = Get-Command iscc -ErrorAction SilentlyContinue
    $iscc = if ($isccCmd) { $isccCmd.Source } else { $null }

    if (-not $iscc) {
        $isccCandidates = @(
            "$env:ProgramFiles (x86)\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
            (Join-Path $here "..\..\work\innosetup\nuget\tools\ISCC.exe")
        )
        foreach ($ic in $isccCandidates) {
            if (Test-Path $ic) {
                $iscc = (Resolve-Path $ic).Path
                break
            }
        }
    }

    if ($iscc) {
        Write-Host "A criar instalador com Inno Setup ($iscc)..." -ForegroundColor Cyan
        $issFile = Join-Path $here 'FoxESSMonitor.iss'
        & $iscc $issFile
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Instalador criado em: $(Join-Path $here 'dist')" -ForegroundColor Green
        } else {
            Write-Warning "A criação do instalador Inno Setup falhou."
        }
    } else {
        Write-Warning "Inno Setup (ISCC.exe) não encontrado. O executável autónomo foi compilado, mas o instalador foi ignorado."
    }
}
