param([switch]$Test)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$out=Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if(!(Test-Path $csc)){$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'}
$exe=Join-Path $out 'MU Editor Phoenix.exe'
$sources=Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object {$_.FullName}
& $csc /nologo /target:winexe /platform:anycpu /optimize+ "/out:$exe" "/win32manifest:$root\src\app.manifest" /r:System.dll /r:System.Core.dll /r:System.Data.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.Xml.Linq.dll /r:System.Security.dll /r:System.Web.Extensions.dll $sources
if($LASTEXITCODE -ne 0){throw 'Build failed'}
foreach($name in @('README.md','LICENSE','CHANGELOG.md')){if(Test-Path (Join-Path $root $name)){Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $out $name) -Force}}
if($Test){& (Join-Path $PSScriptRoot 'Test.ps1')}
