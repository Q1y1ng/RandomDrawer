# 更新日志

本文件记录本工程的所有重要改动。格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

## [未发布]

### 新增

- 从原 `幸运之子——摇号机.exe` 反汇编（dnfile + dncil 读 IL）**逐条还原**出的可编译 C# 源码工程：
  - `src/Program.cs`、`src/Config.cs`、`src/MainForm.cs`、`src/AdminPanel.cs`、`src/AssemblyInfo.cs`
  - 界面与行为与原程序一致，作为后续改造的基线
- `build.cmd` / `build.ps1`：用 Windows 自带的 `csc.exe` 编译（无需 .NET SDK / Visual Studio），脚本位置无关
- `assets/app.ico`：从原 exe 提取的程序图标（16×16 + 32×32）
- `README.md`：还原过程、原程序行为、构建与使用说明

### 验证

- 控件树（类名 / 文本 / 坐标 / 尺寸）与原程序 **15 项全部一致**
- 界面截图与原程序**像素级完全一致**（ImageMagick `AE=0`、`RMSE=0`）
- 装配名 `幸运之子——摇号机`、CLR v4.0.30319、32 位 PE、图标与版本资源均一致

### 备注

- 本版仍与原程序一样把配置写在注册表 `HKCU\Software\随机抽号器`；改为 `settings.ini` 的改动在下一提交
