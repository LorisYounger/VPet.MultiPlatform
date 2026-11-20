# VPet 跨平台宿主 (Avalonia) 发布说明

跨平台版用 Avalonia 实现桌宠窗口，菜单和操作沿用 Windows 版。
拆仓后的源码基线与构建步骤见[仓库说明](../README.md)。

## 运行

```bash
dotnet run --project VPet-Simulator.MutiPlatform
```

- 独立克隆构建时，官方桌宠资源随宿主复制到 `mod/0000_core`。使用本地主库联调时，按[仓库说明](../README.md)链接资源目录。
- 游戏以可执行文件所在目录为准。本地 MOD 在 `mod/`，存档在 `Saves/`，备份在 `Saves_BKP/`，缓存在 `cache/`。默认桌宠读取 `Setting.lps`，多开桌宠读取 `Setting-名字.lps`。
- 升级后要继续使用原先保存在用户目录的数据，请将其复制到可执行文件所在目录。
- 使用 `--prefix <存档名>` 打开指定桌宠，使用 `+connect_lobby <房间号>` 加入访客表。

## 已知差异与手工修法

**启动后显示英文。** 旧版使用 InvariantCulture，语言匹配失败时会把 `language#en:|` 写入 `Setting.lps`。当前版本保留系统区域名，只调整数字格式。已有的 `language` 值仍视为玩家选择的语言。可用以下任一方式修改。

- 在「设置 → 系统 → 语言」中选择语言。
- 删除 `Setting.lps` 中的 `language#en:|` 一行，下次启动时游戏会重新匹配系统语言。

**鼠标穿透仅在 Windows 上生效。** Windows 通过 Win32 的 `WS_EX_TRANSPARENT` 实现穿透。Linux 和 macOS 保留托盘和设置页中的开关，切换后窗口仍会接收鼠标输入。

**Linux 默认使用 X11/XWayland。** 即使登录的是 GNOME Wayland 会话，默认也通过 XWayland 运行。
主桌宠和联机访客的移动均成对提交 X/Y 坐标，避免 X11 异步更新位置时横向位移被下一次写入覆盖。

**跨工作区显示：** Linux 的“设置 → 图形 → 在所有工作区显示桌宠和访客”默认开启，
对主桌宠、已打开及随后加入的联机访客发送 EWMH `_NET_WM_DESKTOP` 请求。
关闭时请求回到当前工作区；设置随存档保存。实际效果由窗口管理器决定，置顶是独立设置。

**原生 Wayland 实验模式：** 按 VPet 的 LinePutScript 启动参数格式，传入 `wayland` 或 `wayland:|`：

```bash
./VPet-Simulator.MutiPlatform wayland
./VPet-Simulator.MutiPlatform 'wayland:|'
```

Steam 启动选项填写 `wayland` 即可。该开关只在 Linux 生效，删除参数恢复 X11/XWayland。
该模式使用 `Avalonia.Wayland`。如果 Wayland 服务不可用，游戏会启动失败。
合成器需要支持 Avalonia 要求的协议版本（包括 `xdg_wm_base >= 3`）；老版本 WSLg/Weston 可能不满足。
软件内部重启会保留原生 Wayland 模式。

- 主桌宠和访客：长按可提起区域，出现提起动作后移动鼠标，由合成器拖动窗口。
  系统接管时会结束应用内的提起状态；点击和其他长按交互仍保留。
- 自动走动、贴边、程序定位、位置恢复以及程序置顶不可用。不会把无效全局坐标保存为退出位置。
- 跨工作区开关不可用。在 GNOME 窗口菜单中选择“始终位于可见工作区”；自动设置需要桌面扩展配合。
- 聊天框问题 #582 在原生 Wayland 下仍待验证，透明窗口、弹出菜单和输入也需实机检查。

**界面鸣谢。** 「设置 → 关于」沿用 Windows 版的鸣谢名单，其中包括 Panuon.WPF.UI。跨平台版的控件样式由 Avalonia 实现，见 `VPet_Simulator.Core.MutiPlatform/Display/basestyle.axaml`。

**Steam。** 游戏使用 `Lib/Steamworks/2.5.2/net6.0` 下的 Facepunch.Steamworks。Windows 仅支持 x64，使用 `Facepunch.Steamworks.Win64.dll` 和 `steam_api64.dll`。Linux x64 与 macOS 使用 `Facepunch.Steamworks.Posix.dll`，原生库分别为 `libsteam_api.so` 和 `libsteam_api.dylib`。这些原生库来自同版发布包的 `Release/Unity/redistributable_bin`，macOS 库包含 x64 和 arm64 两种架构。

构建和发布时会复制目标平台的库。默认目标是当前系统，跨平台发布时请指定 RID。

```bash
dotnet publish VPet-Simulator.MutiPlatform -c Release -r win-x64 --self-contained false
dotnet publish VPet-Simulator.MutiPlatform -c Release -r linux-x64 --self-contained false
dotnet publish VPet-Simulator.MutiPlatform -c Release -r osx-x64 --self-contained false
dotnet publish VPet-Simulator.MutiPlatform -c Release -r osx-arm64 --self-contained false
```

成就、云存档、创意工坊和访客表均使用 Steam。没有 Steam 客户端时，游戏以 `IsSteamUser = false` 运行。Linux 和 macOS 的 Steam 原生库仍待实机验证。

**代码 MOD。** 两个宿主都能加载基于 `VPet-Simulator.Unified` 编写的插件（命名空间 `VPet_Simulator.Unified.Interface`）。旧式 WPF 插件 `MainPlugin` 仅支持 Windows 版。

游戏把运行日志写入可执行文件旁的 `vpet.log`。
