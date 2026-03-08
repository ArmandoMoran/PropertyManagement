C:\SWAM DOCUMENTS\PROPERTIES\CURRENT Properties\1 MASTER\MASTER Property Information.docx = 'C:\SWAM DOCUMENTS\PROPERTIES\CURRENT Properties\1 MASTER\MASTER Property Information.docx'
C:\Users\arman\AppData\Local\Temp\temp_docx.zip = "C:\Users\arman\AppData\Local\Temp\temp_docx.zip"
C:\Users\arman\AppData\Local\Temp\temp_docx_extract = "C:\Users\arman\AppData\Local\Temp\temp_docx_extract"
Copy-Item $docxPath -Destination $tempZip -Force
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse -Force }
Expand-Archive -Path $tempZip -DestinationPath $tempDir -Force
[xml]$docxXml = Get-Content "$tempDir\word\document.xml"
$textNodes = $docxXml.SelectNodes("//*[local-name()='t']")
$fullText = ($textNodes | Select-Object -ExpandProperty '#text') -join " "
$fullText | Out-File "C:\Users\arman\AppData\Local\Temp\docx_text.txt" -Encoding utf8
Write-Host "Extraction complete. First 500 chars:"
$fullText.Substring(0, [math]::Min(500, $fullText.Length))
