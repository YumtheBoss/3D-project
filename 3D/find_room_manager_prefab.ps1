$content = Get-Content "Assets/Scenes/Game Manager.prefab"
$guid = "d51c6a728aa0ef24b963a574b1f31599"
$found = $false
for ($i = 0; $i -lt $content.Length; $i++) {
    $line = $content[$i]
    if ($line -like "*$guid*") {
        Write-Output "Found RoomManager in Game Manager prefab at line $($i + 1)"
        $found = $true
        # Find start of block
        $startLine = $i
        for ($j = $i; $j -ge 0; $j--) {
            if ($content[$j] -match "--- !u!114 &") {
                $startLine = $j
                break
            }
        }
        for ($j = $startLine; $j -lt [math]::Min($startLine + 40, $content.Length); $j++) {
            Write-Output $content[$j]
        }
        break
    }
}
if (-not $found) {
    Write-Output "RoomManager not found in Game Manager prefab"
}
