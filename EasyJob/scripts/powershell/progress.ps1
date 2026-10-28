# Runs for ~8 seconds. Start it several times (also in other tabs) to see separate run panels.
$id = Get-Random -Maximum 1000
for ($i = 1; $i -le 20; $i++) {
    Write-Host ("[run {0}] step {1}/20" -f $id, $i)
    Start-Sleep -Milliseconds 400
}
Write-Host "[run $id] finished" -ForegroundColor Green
