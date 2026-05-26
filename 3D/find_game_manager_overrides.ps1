$content = Get-Content "Assets/Scenes/Hospital.unity"
$guid = "3f888dab6fa062d4a8c284b3c705a067"
$found = $false
for ($i = 0; $i -lt $content.Length; $i++) {
    $line = $content[$i]
    if ($line -like "*$guid*") {
        Write-Output "Found Game Manager PrefabInstance at line $($i + 1)"
        $found = $true
        # Print the next 150 lines to see modifications
        for ($j = $i; $j -lt [math]::Min($i + 150, $content.Length); $j++) {
            if ($content[$j] -match "propertyPath: room3SpawnPoint" -or $content[$j] -match "propertyPath: room4SpawnPoint") {
                Write-Output "$j : $($content[$j])"
                # Print context around this modification
                for ($k = $j - 3; $k -le $j + 3; $k++) {
                    $val = $content[$k]
                    Write-Output "  Line $k - $val"
                }
            }
        }
        break
    }
}
if (-not $found) {
    Write-Output "PrefabInstance for Game Manager not found in Hospital.unity"
}
