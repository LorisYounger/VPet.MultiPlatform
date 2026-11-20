# VPet.MultiPlatform

VPet 的 Avalonia 桌面宿主和 [Unified 插件契约](VPet-Simulator.Unified/README.md)。WPF 本体和官方 MOD 由 [VPet 主库](https://github.com/LorisYounger/VPet) 维护。

| 源码基线 | 提交 |
| --- | --- |
| 构建所用的 VPet 主库基线 | [`352a9f28`](https://github.com/LorisYounger/VPet/commit/352a9f2809541b5b5bbd9adf78cf95926135aad1) |
| 跨平台初始源码 | `24429d71f81b8433e5136c4de4d368ef08dde928` |

## 构建和运行

构建需要 .NET 10 SDK 和上表所列版本的 VPet 主库。按 `VPet/VPet-Simulator.MutiPlatform/` 布局检出时，构建会从父目录读取主库源码。两个仓库分别检出时，在构建命令后加 `-p:VPetRoot="主库绝对路径"`。

```powershell
dotnet build VPet-Simulator.MutiPlatform/VPet-Simulator.MutiPlatform.csproj -r win-x64
.\VPet-Simulator.MutiPlatform\mklink.bat
```

先构建再建链接。Windows 开发版从应用输出目录的 `mod` 目录符号链接读取主库 `VPet-Simulator.Windows/mod`。主库在其他位置时，把主库根目录作为批处理的第一个参数。Linux/macOS 开发时在对应输出目录创建指向同一资源目录的符号链接。应用默认把本机设置、存档、缓存写入运行目录。更多运行参数见[应用说明](VPet-Simulator.MutiPlatform/README.md)。

## 跟随主库更新

1. 比较上表主库提交和目标提交。GameSave、IGameSave、IController、IFood、Statistics、PetActivity、工坊验证客户端、Unified 物品注册、访客表适配和插件数据视图（UnifiedPluginHost.Wrappers.cs）直接编译主库版本。主库公开枚举有变化时，由本仓库同步 Unified 和 Avalonia 的对应名称、数值。
2. 检查变动涉及的宿主实现，重点看两边的 UnifiedPluginHost、CoreMOD、Core 数值逻辑和同名窗口。WPF 有新增界面或规则时，逐项移植到 Avalonia。
3. 构建各目标，用同一个 UnifiedPlugin DLL 检查加载、读档和生命周期，再更新上表主库提交。发布目录与本地签名资料保留在 Git 之外。

## Unified NuGet 包

Windows 本体通过 NuGet 引用 Unified，跨平台宿主直接引用本仓库的 Unified 工程。

在本仓库根目录运行 `dotnet pack VPet-Simulator.Unified/VPet-Simulator.Unified.csproj -c Release`。给 `bin/Release/net8.0/VPet-Simulator.Unified.dll` 签名后，在同一目录运行 `dotnet pack VPet-Simulator.Unified/VPet-Simulator.Unified.csproj -c Release --no-build -o VPet-Simulator.Unified/bin/Release/signed`，用已签名的 DLL 重新打包。
