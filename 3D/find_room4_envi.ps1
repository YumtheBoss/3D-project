$content = Get-Content "Assets/Scenes/Hospital.unity"
$guid = "d512f626eba5962478172bfc4b8dd6ad"
$found = $false
for ($i = 0; $i -lt $content.Length; $i++) {
    $line = $content[$i]
    if ($line -like "*$guid*") {
        Write-Output "Found Room4Environment at line $($i + 1)"
        $found = $true
        # Find start of block
        $startLine = $i
        for ($j = $i; $j -ge 0; $j--) {
            if ($content[$j] -match "--- !u!114 &") {
                $startLine = $j
                break
            }
        }
        for ($j = $startLine; $j -lt [math]::Min($startLine + 35, $content.Length); $j++) {
            Write-Output $content[$j]
        }
        break
    }
}
if (-not $found) {
    Write-Output "Room4Environment instance not found in Hospital.unity"
}
