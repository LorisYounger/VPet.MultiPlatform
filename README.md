# VPet.MultiPlatform

VPet 的 Avalonia 桌面宿主和 [Unified 插件契约](VPet-Simulator.Unified/README.md)。WPF 本体和官方 MOD 由 [VPet 主库](https://github.com/LorisYounger/VPet) 维护。

| 源码基线 | 提交 |
| --- | --- |
| 构建所用的 VPet 主库基线 | [VPet 子模块](VPet)指向的提交 |
| 跨平台初始源码 | `24429d71f81b8433e5136c4de4d368ef08dde928` |

## 构建和运行

安装 Git 和 .NET 10 SDK，然后运行：

```powershell
git clone https://github.com/LorisYounger/VPet.MultiPlatform.git
cd VPet.MultiPlatform
dotnet build
dotnet run --no-build --project VPet-Simulator.MutiPlatform
```

首次构建会从 GitHub 拉取固定版本的 VPet 子模块。宿主编译其中的共享源码，并将官方桌宠资源复制到构建和发布目录的 `mod/0000_core`（约 1 GB）。也可以用 `git clone --recurse-submodules` 在克隆时下载。以后用 `git pull --recurse-submodules` 同步两个仓库。

同时开发主库时，可将本仓库克隆到主库根目录下，或传入 `-p:VPetRoot="主库绝对路径"`。这两种布局直接使用本地主库源码。Windows 开发时在构建后运行 `.\VPet-Simulator.MutiPlatform\mklink.bat "主库绝对路径"`，Linux/macOS 在输出目录创建指向主库 `VPet-Simulator.Windows/mod` 的 `mod` 符号链接。更多运行参数见[应用说明](VPet-Simulator.MutiPlatform/README.md)。

## 跟随主库更新

1. 比较上表主库提交和目标提交。GameSave、IGameSave、IController、IFood、Statistics、PetActivity、工坊验证客户端、Unified 物品注册、访客表适配和插件数据视图（UnifiedPluginHost.Wrappers.cs）直接编译主库版本。主库公开枚举有变化时，由本仓库同步 Unified 和 Avalonia 的对应名称、数值。
2. 检查变动涉及的宿主实现，重点看两边的 UnifiedPluginHost、CoreMOD、Core 数值逻辑和同名窗口。WPF 有新增界面或规则时，逐项移植到 Avalonia。
3. 将 `VPet` 子模块检出到目标提交。用独立克隆构建各目标，用同一个 UnifiedPlugin DLL 检查加载、读档和生命周期，再提交更新后的子模块指针。

## Unified NuGet 包

Windows 本体通过 NuGet 引用 Unified，跨平台宿主直接引用本仓库的 Unified 工程。

在本仓库根目录运行 `dotnet pack VPet-Simulator.Unified/VPet-Simulator.Unified.csproj -c Release`。给 `bin/Release/net8.0/VPet-Simulator.Unified.dll` 签名后，在同一目录运行 `dotnet pack VPet-Simulator.Unified/VPet-Simulator.Unified.csproj -c Release --no-build -o VPet-Simulator.Unified/bin/Release/signed`，用已签名的 DLL 重新打包。
