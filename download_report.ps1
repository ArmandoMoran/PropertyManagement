$ProgressPreference = 'SilentlyContinue'
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5100/api/reports/2025/excel" -OutFile "C:\repos\PropertyManagement_2025_AllProperties.xlsx" -PassThru
    Write-Host "Status: $($response.StatusCode)"
    Write-Host "File size: $((Get-Item 'C:\repos\PropertyManagement_2025_AllProperties.xlsx').Length) bytes"
    Write-Host "File saved to: C:\repos\PropertyManagement_2025_AllProperties.xlsx"
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}
