[CmdletBinding()]
param(
    [string]$RimWorldPath = 'D:\Steam\steamapps\common\RimWorld',
    [string]$DeployPath
)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$game = [IO.Path]::GetFullPath($RimWorldPath)
$versionFile = Join-Path $game 'Version.txt'
if (-not (Test-Path -LiteralPath $versionFile) -or (Get-Content -LiteralPath $versionFile -Raw) -notmatch '^1\.7\.') {
    throw 'This project targets the RimWorld 1.7 / .NET 10 runtime.'
}
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null

& dotnet run --project (Join-Path $root 'Tests\FpsTps.Tests.csproj') -c Release --nologo | Tee-Object -FilePath (Join-Path $artifacts 'core-tests.txt')
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
& dotnet build (Join-Path $root 'Source\FpsTps\FpsTps.csproj') -c Release --nologo "-p:RimWorldPath=$game"
if ($LASTEXITCODE -ne 0) { throw 'Mod build failed.' }

$mod = Join-Path $root 'Mod'
$assemblies = Join-Path $mod '1.7\Assemblies'
New-Item -ItemType Directory -Path $assemblies -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'Source\FpsTps\bin\Release\net10.0\Anno.FpsTps.dll') -Destination $assemblies -Force
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $mod 'LICENSE') -Force
Get-ChildItem -LiteralPath $mod -Recurse -Filter '*.xml' | ForEach-Object { [xml](Get-Content -LiteralPath $_.FullName -Raw) | Out-Null }
[xml]$metadata = Get-Content -LiteralPath (Join-Path $mod 'About\About.xml') -Raw
if ($metadata.ModMetaData.modDependencies -or $metadata.ModMetaData.loadAfter) { throw 'Distribution must have no prerequisite Mods.' }
if ($metadata.ModMetaData.packageId -cne 'anno.fpsandtps' -or $metadata.ModMetaData.author -cne 'anno') { throw 'Invalid author/package ID.' }
$version = $metadata.ModMetaData.modVersion
[xml]$english = Get-Content -LiteralPath (Join-Path $mod 'Languages\English\Keyed\FpsTps.xml') -Raw
[xml]$chinese = Get-Content -LiteralPath (Join-Path $mod 'Languages\ChineseSimplified\Keyed\FpsTps.xml') -Raw
$enKeys = @($english.LanguageData.ChildNodes | Where-Object NodeType -eq Element | ForEach-Object Name | Sort-Object)
$zhKeys = @($chinese.LanguageData.ChildNodes | Where-Object NodeType -eq Element | ForEach-Object Name | Sort-Object)
if (Compare-Object $enKeys $zhKeys) { throw 'Localization key mismatch.' }
if (($enKeys | Select-Object -Unique).Count -ne $enKeys.Count) { throw 'Duplicate localization keys.' }
$unwanted = Get-ChildItem -LiteralPath $assemblies -File | Where-Object Name -ne 'Anno.FpsTps.dll'
if ($unwanted) { throw 'Unexpected assemblies in distribution. Do not redistribute game or Harmony binaries.' }

# Every stage is a new directory. No recursive deletion or mirroring of the user's Mod directory.
$stage = Join-Path $artifacts ('stage-' + [Guid]::NewGuid().ToString('N'))
$packageRoot = Join-Path $stage 'FPS&TPS'
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
Get-ChildItem -LiteralPath $mod -Force | Where-Object Name -ne '.git' | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $packageRoot -Recurse -Force
}
$publishedId = Join-Path $packageRoot 'About\PublishedFileId.txt'
if (Test-Path -LiteralPath $publishedId) { throw 'PublishedFileId.txt must not be included in a general release.' }
$zip = Join-Path $artifacts ("FPS-TPS-v$version-RimWorld-1.7.zip")
# Compress-Archive excludes hidden files; only public Mod content is staged here.
Compress-Archive -LiteralPath $packageRoot -DestinationPath $zip -Force

if ($DeployPath) {
    $destination = [IO.Path]::GetFullPath($DeployPath)
    if ($destination -eq $game -or $destination -eq [IO.Path]::GetPathRoot($destination)) { throw 'DeployPath must be a dedicated Mod directory.' }
    $existingMetadata = Join-Path $destination 'About\About.xml'
    if (Test-Path -LiteralPath $existingMetadata) {
        [xml]$existing = Get-Content -LiteralPath $existingMetadata -Raw
        if ($existing.ModMetaData.packageId -ne 'anno.fpsandtps') { throw 'DeployPath belongs to another mod.' }
    }
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Get-ChildItem -LiteralPath $packageRoot -Force | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $destination -Recurse -Force }
    Write-Output "Installed: $destination"
}
Write-Output "Release package: $zip"
