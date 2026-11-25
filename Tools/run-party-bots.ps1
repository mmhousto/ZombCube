<#
.SYNOPSIS
  Real multi-player Party bot run: starts several copies of a ZombCube Windows build that join one
  Photon room and play the Party map together from wave 49 through the boss.

.EXAMPLE
  .\Tools\run-party-bots.ps1 -Exe "C:\Builds\ZombCube\ZombCube.exe" -Map theatre -Players 3

  Each instance writes its result to .\bot-results\party-<map>-<n>.json and quits when the run ends.
  The first instance to join starts the game once all -Players instances are in the room.
  Uses Photon Cloud (internet needed). Nothing is saved to any profile or leaderboard.
#>
param(
    [Parameter(Mandatory = $true)][string]$Exe,
    [ValidateSet("arena", "spaceship", "theatre")][string]$Map = "arena",
    [ValidateRange(2, 4)][int]$Players = 2,
    [double]$Speed = 1,
    [int]$TimeoutMinutes = 60,
    [switch]$Windowed
)

$room = "zcbot-" + [guid]::NewGuid().ToString("N").Substring(0, 8)
$results = Join-Path (Get-Location) "bot-results"
New-Item -ItemType Directory -Force -Path $results | Out-Null

$procs = @()
for ($i = 1; $i -le $Players; $i++) {
    $result = Join-Path $results "party-$Map-$i.json"
    $args = @("-zcbot", "party", "-zcbotMap", $Map, "-zcbotPlayers", $Players, "-zcbotRoom", $room,
              "-zcbotSpeed", $Speed, "-zcbotTimeout", $TimeoutMinutes, "-zcbotResult", "`"$result`"", "-zcbotQuit",
              "-logFile", "`"$(Join-Path $results "party-$Map-$i.log")`"")
    if ($Windowed) { $args += @("-screen-fullscreen", "0", "-screen-width", "960", "-screen-height", "540") }
    Write-Host "Starting instance $i in room $room"
    $procs += Start-Process -FilePath $Exe -ArgumentList $args -PassThru
    Start-Sleep -Seconds 3
}

$procs | Wait-Process
$failed = 0
for ($i = 1; $i -le $Players; $i++) {
    $file = Join-Path $results "party-$Map-$i.json"
    if (Test-Path $file) { $json = Get-Content $file -Raw; Write-Host "Instance ${i}: $json"; if ($json -notmatch '"outcome":"pass"') { $failed++ } }
    else { Write-Host "Instance ${i}: no result (see party-$Map-$i.log)"; $failed++ }
}
if ($failed -gt 0) { Write-Host "Party bot run FAILED"; exit 1 } else { Write-Host "Party bot run PASSED" }
