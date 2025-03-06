$path = "C:\code\NetworkMonitor\bin\Release\net9.0\win-x64\publish"
Get-ChildItem -Path $path -Filter *.dll | ForEach-Object {
    $file = $_.FullName
    $isManaged = $false

    try {
        # Try to load the assembly metadata
        [System.Reflection.AssemblyName]::GetAssemblyName($file) | Out-Null
        $isManaged = $true
    } catch {
        # If it fails, the DLL is native (unmanaged)
    }

    if (-not $isManaged) {
        Write-Host "⚠️ Native DLL (causes issues): $file"
    }
}
