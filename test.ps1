param(
 [Parameter(Mandatory=$true)][string]$ManagedPath,
 [Parameter(Mandatory=$true)][string]$BepInExPath,
 [Parameter(Mandatory=$true)][string]$EpicLootAssembly,
 [Parameter(Mandatory=$true)][string]$CompilerPath
)
$ErrorActionPreference='Stop'
$out=Join-Path $PSScriptRoot 'artifacts/tests'
New-Item -ItemType Directory -Force $out | Out-Null
$tests=Get-ChildItem "$PSScriptRoot/tests/PolicyTests" -Filter '*.cs' | ForEach-Object FullName
& $CompilerPath /nologo /target:exe /nullable:enable /langversion:latest /warnaserror+ "/out:$out/PolicyTests.exe" "$PSScriptRoot/src/CentreRules.cs" "$PSScriptRoot/src/PlacementRequest.cs" "$PSScriptRoot/src/CompatibilityWarning.cs" @tests
if($LASTEXITCODE -ne 0){throw 'Policy test build failed'}
& "$out/PolicyTests.exe"
if($LASTEXITCODE -ne 0){throw 'Policy tests failed'}
& $CompilerPath /nologo /target:exe /langversion:latest "/out:$out/CompatibilityTests.exe" "$PSScriptRoot/src/Compatibility.cs" "$PSScriptRoot/src/ApiContract.cs" "$PSScriptRoot/tests/CompatibilityTests.cs"
if($LASTEXITCODE -ne 0){throw 'Compatibility test build failed'}
'{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}' | Set-Content "$out/CompatibilityTests.runtimeconfig.json" -Encoding utf8
dotnet "$out/CompatibilityTests.exe" $EpicLootAssembly $ManagedPath "$BepInExPath/core" "$BepInExPath/plugins"
if($LASTEXITCODE -ne 0){throw 'Compatibility checks failed'}
