param(
    [string]$ExePath = "USB\SupportAI.exe",
    [string]$CertSubject = "CN=SupportAI USB"
)

$ErrorActionPreference = 'Stop'

# Crea cert autofirmado si no existe
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object Subject -eq $CertSubject | Select-Object -First 1
if (-not $cert) {
    Write-Host "Creando certificado autofirmado..." -ForegroundColor Yellow
    $cert = New-SelfSignedCertificate -Subject $CertSubject -CertStoreLocation Cert:\CurrentUser\My -CodeSigningCert -Type CodeSigningCert
}

# Firma
Set-AuthenticodeSignature -FilePath $ExePath -Certificate $cert -TimestampServer "http://timestamp.digicert.com"

Write-Host "Firmado: $ExePath" -ForegroundColor Green
