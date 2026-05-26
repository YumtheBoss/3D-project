$content = Get-Content "Assets/Scenes/Hospital.unity"
$guid = "d51c6a728aa0ef24b963a574b1f31599"
$found = $false
for ($i = 0; $i -lt $content.Length; $i++) {
    $line = $content[$i]
    if ($line -like "*$guid*") {
        Write-Output "Found RoomManager instance at line $($i + 1)"
        $found = $true
        # Find the start of this MonoBehaviour block (usually starts with --- !u!114 &)
        $startLine = $i
        for ($j = $i; $j -ge 0; $j--) {
            if ($content[$j] -match "--- !u!114 &") {
                $startLine = $j
                break
            }
        }
        # Print 30 lines from startLine
        for ($j = $startLine; $j -lt [math]::Min($startLine + 30, $content.Length); $j++) {
            Write-Output $content[$j]
        }
        break
    }
}
if (-not $found) {
    Write-Output "RoomManager instance with guid $guid not found in Hospital.unity"
}
