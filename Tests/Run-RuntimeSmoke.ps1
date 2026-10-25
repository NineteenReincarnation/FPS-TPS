[CmdletBinding()]
param(
    [string]$RimWorldPath = 'D:\Steam\steamapps\common\RimWorld',
    [string]$ModPath = 'D:\Steam\steamapps\common\RimWorld\Mods\FPS&TPS',
    [string]$HarmonyPath
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$game = [IO.Path]::GetFullPath($RimWorldPath)
$mod = [IO.Path]::GetFullPath($ModPath)
if (Get-Process -Name RimWorld -ErrorAction SilentlyContinue) { throw 'Close existing RimWorld processes before the isolated runtime smoke test.' }
[xml]$about = Get-Content -LiteralPath (Join-Path $mod 'About\About.xml') -Raw
if ($about.ModMetaData.packageId -ne 'anno.fpsandtps') { throw 'ModPath is not this mod.' }
$buildArgs = @('build', (Join-Path $PSScriptRoot 'RuntimeSmoke\RuntimeSmoke.csproj'), '-c', 'Release', '--nologo', "-p:RimWorldPath=$game")
if ($HarmonyPath) { $buildArgs += "-p:HarmonyPath=$HarmonyPath" }
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Runtime probe build failed.' }
$compiled = Join-Path $root 'Source\FpsTps\bin\Release\net10.0\Anno.FpsTps.dll'
$installed = Join-Path $mod '1.7\Assemblies\Anno.FpsTps.dll'
if ((Get-FileHash -LiteralPath $compiled).Hash -ne (Get-FileHash -LiteralPath $installed).Hash) { throw 'Installed DLL is stale. Run Build.ps1 -DeployPath first.' }
$probe = Join-Path $mod '1.7\Assemblies\Anno.FpsTps.RuntimeSmoke.dll'
if (Test-Path -LiteralPath $probe) { throw 'Existing runtime probe found. Remove it before running a new test.' }
$data = Join-Path $root ('.test-data\runtime-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $data 'Config') -Force | Out-Null
@'
<?xml version="1.0" encoding="utf-8"?>
<ModsConfigData><version>1.7</version><activeMods><li>brrainz.harmony</li><li>ludeon.rimworld</li><li>anno.fpsandtps</li></activeMods><knownExpansions /></ModsConfigData>
'@ | Set-Content -LiteralPath (Join-Path $data 'Config\ModsConfig.xml') -Encoding utf8
@'
<?xml version="1.0" encoding="utf-8"?>
<PrefsData><devMode>False</devMode><langFolderName>ChineseSimplified (简体中文)</langFolderName></PrefsData>
'@ | Set-Content -LiteralPath (Join-Path $data 'Config\Prefs.xml') -Encoding utf8
$artifacts = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Path $artifacts -Force | Out-Null
$process = $null
try {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'RuntimeSmoke\bin\Release\net10.0\Anno.FpsTps.RuntimeSmoke.dll') -Destination $probe
    $arguments = @('-Headless', '-QuitAfterBoot', '-FpsTpsSmokeTest', ('"-savedatafolder=' + $data + '"'))
    $process = Start-Process -FilePath (Join-Path $game 'RimWorld.exe') -ArgumentList $arguments -WorkingDirectory $game -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $data 'stdout.txt') -RedirectStandardError (Join-Path $data 'stderr.txt')
    if (-not $process.WaitForExit(60000)) { throw 'Headless startup exceeded 60 seconds; see isolated logs.' }
    if ($process.ExitCode -ne 0) { throw "Headless runtime exited with code $($process.ExitCode)." }
    $result = Join-Path $data 'runtime-smoke-results.txt'
    if (-not (Test-Path -LiteralPath $result)) { throw 'Runtime test report was not produced.' }
    $lines = Get-Content -LiteralPath $result
    $lines | Write-Output
    if (($lines -match '^FAIL ') -or -not ($lines -match '^RESULT ')) { throw 'Runtime smoke test failed.' }
    Copy-Item -LiteralPath $result -Destination (Join-Path $artifacts 'runtime-smoke-results.txt') -Force
} finally {
    if ($process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit(10000) | Out-Null }
    # Preserve the exact temporary probe outside the installed mod for review/recovery.
    if (Test-Path -LiteralPath $probe) { Move-Item -LiteralPath $probe -Destination (Join-Path $data 'Anno.FpsTps.RuntimeSmoke.dll') }
}
