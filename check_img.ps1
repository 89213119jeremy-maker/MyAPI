Add-Type -AssemblyName System.Drawing
$img = [System.Drawing.Image]::FromFile("c:\Users\user\OneDrive - 逢甲大學\桌面\API\picture\earphone.jpg")
Write-Host "寬: $($img.Width)px, 高: $($img.Height)px"
$img.Dispose()
