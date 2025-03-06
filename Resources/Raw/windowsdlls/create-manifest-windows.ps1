# Get the current directory
$BasePath = Get-Location

# Generate a relative file list and save it to assets_manifest.txt
Get-ChildItem -Path . -Recurse -File | ForEach-Object { 
    $_.FullName.Replace($BasePath, ".") -replace "\\", "/" 
} | Set-Content -Path assets_manifest.txt
