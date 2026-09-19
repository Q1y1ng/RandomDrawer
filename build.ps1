# 幸运之子——摇号机  编译脚本（位置无关：可整个文件夹拷走）
# 用 .NET Framework 自带的 csc.exe 编译，不需要安装 .NET SDK / Visual Studio。
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$root   = $PSScriptRoot
$srcDir = Join-Path $root 'src'
$outDir = Join-Path $root 'dist'
$icon   = Join-Path $root 'assets\app.ico'
$outExe = Join-Path $outDir '幸运之子——摇号机.exe'

$csc = Join-Path $env:windir 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:windir 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw '找不到 csc.exe（需要 .NET Framework 4.x，Windows 自带）' }

if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }

$sources = @(Get-ChildItem -LiteralPath $srcDir -Filter *.cs | ForEach-Object { $_.FullName })
Write-Host ("编译 " + $sources.Count + " 个源文件 ...")

& $csc -nologo -utf8output -target:winexe -platform:x86 -optimize+ "-win32icon:$icon" "-out:$outExe" $sources
if ($LASTEXITCODE -ne 0) { throw ("编译失败（退出码 " + $LASTEXITCODE + "）") }

$f = Get-Item -LiteralPath $outExe
Write-Host ("完成: " + $f.FullName)
Write-Host ("大小: " + $f.Length + " 字节    时间: " + $f.LastWriteTime)
