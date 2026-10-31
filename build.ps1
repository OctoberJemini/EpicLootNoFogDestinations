param(
 [Parameter(Mandatory=$true)][string]$ManagedPath,
 [Parameter(Mandatory=$true)][string]$BepInExPath,
 [Parameter(Mandatory=$true)][string]$EpicLootAssembly,
 [Parameter(Mandatory=$true)][string]$CompilerPath
)
$ErrorActionPreference='Stop'
$refs=@('mscorlib','System','System.Core','netstandard','assembly_valheim','assembly_utils','assembly_guiutils','UnityEngine','UnityEngine.CoreModule','UnityEngine.UI','UnityEngine.UIModule') | ForEach-Object { '/reference:'+(Join-Path $ManagedPath "$_.dll") }
$refs+=@("/reference:$BepInExPath/core/BepInEx.dll","/reference:$BepInExPath/core/0Harmony.dll","/reference:$EpicLootAssembly")
$out=Join-Path $PSScriptRoot 'artifacts/package'
New-Item -ItemType Directory -Force "$out/plugins/EpicLootNoFogDestinations" | Out-Null
$sources=Get-ChildItem "$PSScriptRoot/src" -Filter '*.cs' | ForEach-Object FullName
& $CompilerPath /nologo /target:library /optimize+ /nostdlib+ /nullable:enable /langversion:latest /warnaserror+ "/out:$out/plugins/EpicLootNoFogDestinations/Valheim.EpicLootNoFogDestinations.dll" @refs @sources
if($LASTEXITCODE -ne 0){throw 'Build failed'}
Copy-Item "$PSScriptRoot/thunderstore/*" $out -Force
Copy-Item "$PSScriptRoot/README.md","$PSScriptRoot/CHANGELOG.md","$PSScriptRoot/LICENSE","$PSScriptRoot/THIRD_PARTY_NOTICES.md" $out -Force
$version=(Get-Content "$out/manifest.json" -Raw | ConvertFrom-Json).version_number
Compress-Archive -Path "$out/*" -DestinationPath "$PSScriptRoot/artifacts/Epic_Loot_No_Fog_Destinations-$version.zip" -Force
