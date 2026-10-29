param(
    [string]$RomPath,
    [string]$DotnetPath = 'dotnet',
    [string]$OutputPath,
    [switch]$SkipOpenAL
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$outputPath = if ($OutputPath) { [IO.Path]::GetFullPath($OutputPath) } else { Join-Path $projectRoot 'dist\HuntersPC' }
if ($RomPath) { $RomPath = (Resolve-Path -LiteralPath $RomPath).Path }

& $DotnetPath publish (Join-Path $projectRoot 'src\MphRead\MphRead.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false "-p:PathMap=$projectRoot=/src" -o $outputPath --nologo
if ($LASTEXITCODE -ne 0) { throw 'Falha ao publicar Hunters PC.' }

Copy-Item -LiteralPath (Join-Path $projectRoot 'LICENSE') -Destination $outputPath
Copy-Item -LiteralPath (Join-Path $projectRoot 'README-PC.md') -Destination $outputPath
Copy-Item -LiteralPath (Join-Path $projectRoot 'THIRD-PARTY-PC.md') -Destination $outputPath
Copy-Item -LiteralPath (Join-Path $projectRoot 'README-UPSTREAM.md') -Destination $outputPath
New-Item -ItemType Directory -Path (Join-Path $outputPath 'licenses\nuget') -Force | Out-Null
$packageCache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget\packages' }
$depsFile = Get-Content -Raw -LiteralPath (Join-Path $outputPath 'HuntersPC.deps.json') | ConvertFrom-Json
$packageSpecs = @($depsFile.libraries.PSObject.Properties | Where-Object { $_.Value.type -in @('package','runtimepack') } | ForEach-Object { $_.Name.ToLowerInvariant() -replace '^runtimepack\.', '' })
foreach ($packageSpec in $packageSpecs) {
    $packagePath = Join-Path $packageCache $packageSpec
    if (!(Test-Path -LiteralPath $packagePath)) { continue }
    $packageName = ($packageSpec -split '/')[0]
    $licenseDestination = Join-Path $outputPath "licenses\nuget\$packageName"
    New-Item -ItemType Directory -Path $licenseDestination -Force | Out-Null
    Get-ChildItem -LiteralPath $packagePath -File | Where-Object { $_.Name -match '(?i)license|notice|\.nuspec$' } | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $licenseDestination
    }
}

# Native dependencies retain their upstream notices even when a NuGet wrapper
# only carries a license expression in its nuspec.
$nativeNotices = @{
    'opentk' = 'https://raw.githubusercontent.com/opentk/opentk/4.9.4/LICENSE.md'
    'glfw' = 'https://raw.githubusercontent.com/glfw/glfw/3.4/LICENSE.md'
}
foreach ($dependency in $nativeNotices.Keys) {
    $noticePath = Join-Path $outputPath "licenses\$dependency"
    New-Item -ItemType Directory -Path $noticePath -Force | Out-Null
    Invoke-WebRequest $nativeNotices[$dependency] -OutFile (Join-Path $noticePath 'LICENSE.md')
}

if (!$SkipOpenAL) {
    $cachePath = Join-Path $projectRoot 'local\dependencies'
    New-Item -ItemType Directory -Path $cachePath -Force | Out-Null
    $archivePath = Join-Path $cachePath 'openal-soft-1.25.2-bin.zip'
    $expectedHash = '67A0C4B800BD860C93C04F38CAF8CBE4875F9C84700AC430EFC451F70E265434'
    if (!(Test-Path -LiteralPath $archivePath)) {
        Invoke-WebRequest 'https://github.com/kcat/openal-soft/releases/download/1.25.2/openal-soft-1.25.2-bin.zip' -OutFile $archivePath
    }
    if ((Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash -ne $expectedHash) { throw 'Checksum OpenAL Soft divergente.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $licensePath = Join-Path $outputPath 'licenses\openal-soft'
        New-Item -ItemType Directory -Path $licensePath -Force | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry('openal-soft-1.25.2-bin/bin/Win64/soft_oal.dll'), (Join-Path $outputPath 'OpenAL32.dll'), $true)
        foreach ($name in @('COPYING', 'LICENSE-pffft')) {
            [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry("openal-soft-1.25.2-bin/$name"), (Join-Path $licensePath $name), $true)
        }
    }
    finally { $archive.Dispose() }
}

if ($RomPath) {
    & $DotnetPath (Join-Path $outputPath 'HuntersPC.dll') --pc-import $RomPath
    if ($LASTEXITCODE -ne 0) { throw "Falha na importacao. Consulte $outputPath\hunters-error.log" }
}
Write-Output "Pronto: $outputPath\HuntersPC.exe"
