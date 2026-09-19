# 运行测试（抽号算法 + 配置读写），用 .NET Framework 自带的 csc.exe 编译
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$root  = $PSScriptRoot
$outDir = Join-Path $root 'tests\bin'
$outExe = Join-Path $outDir 'PickerTests.exe'

$csc = Join-Path $env:windir 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:windir 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw '找不到 csc.exe（需要 .NET Framework 4.x，Windows 自带）' }

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

$sources = @(
  (Join-Path $root 'src\Config.cs'),
  (Join-Path $root 'src\Picker.cs'),
  (Join-Path $root 'src\Settings.cs'),
  (Join-Path $root 'tests\PickerTests.cs')
)

Write-Host "编译测试程序 ..."
& $csc -nologo -utf8output -codepage:65001 -target:exe -platform:x86 -r:System.Windows.Forms.dll -out:$outExe $sources
if ($LASTEXITCODE -ne 0) { throw ("编译失败（退出码 " + $LASTEXITCODE + "）") }

Write-Host ""
& $outExe
$rc = $LASTEXITCODE
Write-Host ""
if ($rc -eq 0) { Write-Host "测试通过 ✓" } else { Write-Host ("测试有失败项：" + $rc + " ✗") }
exit $rc
