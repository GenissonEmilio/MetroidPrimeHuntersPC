param(
    [string]$Version = 'v0.2.0',
    [string]$DotnetPath = 'dotnet',
    [string]$PythonPath = 'python'
)
$ErrorActionPreference = 'Stop'
if ($Version -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+(?:-[a-zA-Z0-9.]+)?$') { throw 'Versao de pacote invalida.' }
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releasePath = Join-Path $projectRoot "artifacts\$Version"
$stageParent = Join-Path $projectRoot ('artifacts\staging-' + [Guid]::NewGuid().ToString('N'))
$stagePath = Join-Path $stageParent 'HuntersPC'
New-Item -ItemType Directory -Path $releasePath -Force | Out-Null
$zipPath = Join-Path $releasePath "MetroidPrimeHuntersPC-$Version-windows-x64.zip"
if (Test-Path -LiteralPath $zipPath) { throw "O ZIP ja existe. Preserve-o ou escolha outra versao: $zipPath" }

& (Join-Path $PSScriptRoot 'build-pc.ps1') -DotnetPath $DotnetPath -OutputPath $stagePath
Copy-Item -LiteralPath (Join-Path $projectRoot 'RELEASE_NOTES.md') -Destination $stagePath
[IO.File]::WriteAllText((Join-Path $stagePath 'LEIA-ME.txt'), @'
METROID PRIME HUNTERS PC - WINDOWS X64

1. Extraia a pasta inteira e abra HuntersPC.exe.
2. Arraste sua ROM USA revisao 0 ou Europa revisao 1 (.nds ou ZIP com uma ROM) para o menu.
3. Aguarde a importacao local e escolha TESTAR CONTROLES ou INICIAR / CONTINUAR.

WASD: mover. Mouse: camera/mira. Clique: disparar. Espaco: pular/boost.
C: alternar forma. E: visor. Q: scan/ataque alternativo. Roda/1-9: armas.
M: mapa. J: diario. Tab: alternar paineis. Esc: voltar/pausa. F11: tela cheia.

Este pacote nao inclui ROM, BIOS, saves ou dados extraidos do jogo.
O runtime .NET e as bibliotecas acompanham o pacote. Leia README-PC.md,
RELEASE_NOTES.md e as licencas antes de usar ou redistribuir.
'@, [Text.UTF8Encoding]::new($false))

$fileManifest = @(Get-ChildItem -LiteralPath $stagePath -Recurse -File | Sort-Object FullName | ForEach-Object {
    [PSCustomObject]@{
        Path = [IO.Path]::GetRelativePath($stagePath, $_.FullName).Replace('\','/')
        Bytes = $_.Length
        Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
})
$manifest = [PSCustomObject]@{
    Version = $Version
    Platform = 'windows-x64'
    SourceCommit = (& git -C $projectRoot rev-parse HEAD).Trim()
    GameDataIncluded = $false
    Files = $fileManifest
}
[IO.File]::WriteAllText((Join-Path $stagePath 'package-manifest.json'), ($manifest | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stageParent, $zipPath, [IO.Compression.CompressionLevel]::Optimal, $false)
& $PythonPath (Join-Path $PSScriptRoot 'audit-windows-package.py') $zipPath --output (Join-Path $releasePath 'package-audit.json')
if ($LASTEXITCODE -ne 0) { throw 'A auditoria do ZIP falhou. Nao publique esse pacote.' }
$hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText((Join-Path $releasePath 'SHA256SUMS.txt'), "$hash  $([IO.Path]::GetFileName($zipPath))`n", [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath (Join-Path $projectRoot 'RELEASE_NOTES.md') -Destination (Join-Path $releasePath 'release-notes.md')
Write-Output "ZIP auditado: $zipPath"
