param([string]$Server='localhost')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$build=Join-Path $root 'build'
$reports=Join-Path $root 'tests\reports'
New-Item -ItemType Directory -Force -Path $build,$reports | Out-Null
$csc=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$sources=Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object {$_.FullName}
$exe=Join-Path $build 'Phoenix.RewardDbTests.exe'
& $csc /nologo /target:exe /main:RewardDbTestRunner "/out:$exe" /r:System.dll /r:System.Core.dll /r:System.Data.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:System.Xml.dll /r:System.Xml.Linq.dll /r:System.Security.dll /r:System.Web.Extensions.dll $sources "$root\tests\Phoenix.RewardDbTests.cs"
if($LASTEXITCODE -ne 0){throw 'Database test build failed'}
& $exe $Server (Join-Path $reports 'database-rewards.json')
if($LASTEXITCODE -ne 0){throw 'Database integration tests failed'}
