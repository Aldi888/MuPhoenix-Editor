param()
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$exe=Join-Path $root 'dist\MU Editor Phoenix.exe'
$reports=Join-Path $root 'tests\reports'
New-Item -ItemType Directory -Force -Path $reports | Out-Null
foreach($test in @(@('--self-test','regression.json'),@('--v2-self-test','workspace.json'),@('--grant-self-test','grants.json'))){
 $report=Join-Path $reports $test[1]
 $arguments=$test[0]+' "'+$report+'"'
 $process=Start-Process -FilePath $exe -ArgumentList $arguments -Wait -PassThru
 if($process.ExitCode -ne 0){throw ('Test failed: '+$test[0])}
 $result=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
 if($result.status -ne 'PASS'){throw ('Invalid test report: '+$report)}
 Write-Output ('PASS '+$test[0])
}
