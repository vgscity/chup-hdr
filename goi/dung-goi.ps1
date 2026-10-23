# Dựng gói MSIX từ thư mục publish. Chạy trên Windows (GitHub Actions windows-latest hoặc máy có Windows SDK).
#   goi/dung-goi.ps1 -App goi/ra/app -IdentityName "12345VGS.VGSChupHDR" -Publisher "CN=xxxx" -PublisherDisplay "VGS"
# Ra: goi/ra/VgsChupHdr-store.msix (KHÔNG ký, nộp Partner Center – Store tự ký)
#     goi/ra/VgsChupHdr-test.msix + VgsChupHdr-test.cer (ký tự, để cài thử trên máy: cài .cer vào Trusted People trước)
param(
  [string]$App = "goi/ra/app",
  [string]$IdentityName = "VGS.VGSChupHDR",
  [string]$Publisher = "CN=VGS",
  [string]$PublisherDisplay = "VGS",
  [string]$Version = ""
)
$ErrorActionPreference = "Stop"
$goc = Split-Path -Parent $PSScriptRoot
Set-Location $goc

if (-not $Version) {
  $Version = ([xml](Get-Content "VgsChupHdr.csproj")).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
}
if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') { $Version = "$Version.0" }   # Store cần 4 số, số cuối 0

$kits = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin" -Directory | Where-Object Name -like "10.*" | Sort-Object Name -Descending | Select-Object -First 1
$makeappx = Join-Path $kits.FullName "x64\makeappx.exe"
$signtool = Join-Path $kits.FullName "x64\signtool.exe"
if (-not (Test-Path $makeappx)) { throw "Không thấy makeappx.exe trong Windows SDK" }

$ra = (New-Item -ItemType Directory -Force "goi/ra").FullName
$App = (Resolve-Path $App).Path
$assets = Join-Path $App "Assets"
New-Item -ItemType Directory -Force $assets | Out-Null
Get-ChildItem "goi/anh" -Filter "*.png" | Where-Object { $_.Name -match '^(Square|Wide|StoreLogo)' } | Copy-Item -Destination $assets -Force

$mf = Get-Content "goi/AppxManifest.xml" -Raw
$mf = $mf.Replace("__IDENTITY_NAME__", $IdentityName).Replace("__PUBLISHER__", $Publisher).Replace("__PUBLISHER_DISPLAY__", $PublisherDisplay).Replace("__VERSION__", $Version)
[IO.File]::WriteAllText((Join-Path $App "AppxManifest.xml"), $mf, (New-Object Text.UTF8Encoding $false))

# Dọn file không cần trong gói
Get-ChildItem $App -Filter "*.pdb" | Remove-Item -Force

$store = Join-Path $ra "VgsChupHdr-store.msix"
& $makeappx pack /d $App /p $store /o
if ($LASTEXITCODE -ne 0 -or -not (Test-Path $store)) { throw "makeappx thất bại (mã $LASTEXITCODE)" }
Write-Host "Đã dựng $store ($Version, $IdentityName, $Publisher)"

# Bản test ký tự
$test = Join-Path $ra "VgsChupHdr-test.msix"
Copy-Item $store $test -Force
$cert = New-SelfSignedCertificate -Type Custom -Subject $Publisher -KeyUsage DigitalSignature -FriendlyName "VGS Chụp HDR test" `
  -CertStoreLocation "Cert:\CurrentUser\My" -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}") -NotAfter (Get-Date).AddYears(3)
$mk = ConvertTo-SecureString -String "vgs-test" -Force -AsPlainText
$pfx = Join-Path $ra "test.pfx"
Export-PfxCertificate -Cert $cert -FilePath $pfx -Password $mk | Out-Null
Export-Certificate -Cert $cert -FilePath (Join-Path $ra "VgsChupHdr-test.cer") | Out-Null
& $signtool sign /fd SHA256 /f $pfx /p "vgs-test" $test
if ($LASTEXITCODE -ne 0) { throw "signtool thất bại (mã $LASTEXITCODE)" }
Remove-Item $pfx -Force
Write-Host "Đã ký $test bằng chứng chỉ tự tạo ($ra/VgsChupHdr-test.cer)"
