param([string]$Version='1.2.0')
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$exe=Join-Path $root 'dist\MU Editor Phoenix.exe'
if(!(Test-Path $exe)){throw 'Build the application first'}
$build=Join-Path $root 'build'
$release=Join-Path $root 'releases'
$stage=Join-Path $build ('package-'+[Guid]::NewGuid().ToString('N'))
$folder=Join-Path $stage 'MU-Editor-Phoenix'
New-Item -ItemType Directory -Force -Path $folder,$release | Out-Null
foreach($name in @('README.md','LICENSE','CHANGELOG.md','AGENTS.md')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination $folder}
Copy-Item -LiteralPath $exe -Destination $folder
foreach($name in @('src','scripts','docs','examples')){Copy-Item -LiteralPath (Join-Path $root $name) -Destination (Join-Path $folder $name) -Recurse}
$testFolder=Join-Path $folder 'tests'
New-Item -ItemType Directory -Force -Path $testFolder | Out-Null
Get-ChildItem -LiteralPath (Join-Path $root 'tests') -Filter '*.cs' | Copy-Item -Destination $testFolder
$zip=Join-Path $release ('MU-Editor-Phoenix-'+$Version+'.zip')
Compress-Archive -LiteralPath $folder -DestinationPath $zip -Force
$binary=Join-Path $release 'MU Editor Phoenix.exe'
Copy-Item -LiteralPath $exe -Destination $binary -Force
$sum=Join-Path $release 'SHA256SUMS.txt'
$lines=@($zip,$binary)|ForEach-Object {((Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant())+'  '+[IO.Path]::GetFileName($_)}
[IO.File]::WriteAllLines($sum,$lines,[Text.UTF8Encoding]::new($false))
Write-Output $zip
