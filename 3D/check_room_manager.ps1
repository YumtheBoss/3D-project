$content = Get-Content "Assets/Scenes/Hospital.unity"
$inRoomManager = $false
$lines = @()
for ($i = 0; $i -lt $content.Length; $i++) {
    $line = $content[$i]
    if ($line -match "RoomManager:") {
        $inRoomManager = $true
        $lines += "Found RoomManager at line $($i + 1)"
    } elseif ($inRoomManager) {
        if ($line -like "---*") {
            $inRoomManager = $false
        } else {
            $lines += $line
        }
    }
}
$lines | Out-File -FilePath "room_manager_fields.txt"
Write-Output "Done"
