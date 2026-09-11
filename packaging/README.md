# 发布与打包

正式发行物由 `Build-Release.ps1` 统一生成。脚本从 MSBuild 的 `Version`
属性读取版本号，并依次执行锁定还原、Release 测试、两种发布和产物校验：

- `RePKG-Workbench-Setup-x64.exe`：依赖 .NET 10 Desktop Runtime 的用户级安装包，缺少运行时时会提示下载；
- `RePKG-Workbench-Portable-x64.zip`：包含运行时、无需安装 .NET 的便携包；
- `RePKG-Workbench-Portable-FDD-x64.zip`：不包含运行时、需要预先安装 .NET 10 Desktop Runtime (x64) 的轻量便携包。

```powershell
.\packaging\Build-Release.ps1
```

脚本会从 `PATH`、Inno Setup 卸载注册表项和标准安装目录自动查找
`ISCC.exe`，因此注册在系统中的自定义安装目录也可直接使用。对于未注册的
便携安装，可明确传入编译器路径：

```powershell
.\packaging\Build-Release.ps1 -IsccPath 'E:\Development\Inno Setup 6\ISCC.exe'
```

仅验证并生成发布目录和两种便携包时，可使用 `-SkipInstaller`。最终发行前不要
使用该选项。所有输出位于 `artifacts\release`，该目录不会进入 Git。

应用图标应包含从 16px 到 256px 的常用 Windows 尺寸。更换原始图标后运行：

```powershell
.\packaging\Build-WindowsIcon.ps1 -InputPath .\path\to\source.ico
```

## 正式发布检查

- 在 `Directory.Build.props` 中更新唯一的 `Version`；
- 使用代码签名证书签署应用 EXE 和安装包，避免 SmartScreen 显示未知发布者；
- 在干净的 Windows x64 虚拟机中分别验证安装、卸载和便携包；
- 发布 `packages\SHA256SUMS`，并核对随包 RePKG 的上游许可证；
- 商业使用 Inno Setup 时，根据其当前许可政策确认是否需要购买许可证。
