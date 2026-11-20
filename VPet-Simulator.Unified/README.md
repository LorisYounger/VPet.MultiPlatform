# VPet-Simulator.Unified · 虚拟桌宠模拟器 MOD 开发包

![虚拟桌宠模拟器主图](https://raw.githubusercontent.com/LorisYounger/VPet/main/README.assets/%E4%B8%BB%E5%9B%BE.png)

虚拟桌宠模拟器（VPet）是一款支持互动、投喂和 Steam 创意工坊的开源桌宠。使用这个开发包编写代码 MOD，只需编译一份 DLL，就能在支持 Unified 的 Windows 本体和 Avalonia 跨平台版加载。[VPet 项目主页](https://github.com/LorisYounger/VPet) · [在 Steam 获取游戏](https://store.steampowered.com/app/1920960/VPet/)

## 写一个查看等级的插件

创建名为 `PetLevel` 的 .NET 8 类库，使用默认的 AnyCPU 平台。在项目目录安装开发包。

```shell
dotnet add package VPet-Simulator.Unified --version 1.0.1
```

入口类直接继承 `UnifiedPlugin`，通过公开构造函数接收 `IPetHost`。游戏每次重建「自定」菜单都会调用 `LoadDIY()`。示例在这里添加「查看等级」按钮，玩家点击时才读取当前桌宠的等级。

```csharp
using VPet_Simulator.Unified.Interface;

public class PetLevelPlugin : UnifiedPlugin
{
    public PetLevelPlugin(IPetHost host) : base(host) { }
    public override string PluginName => "PetLevel";

    public override void LoadDIY()
    {
        Host.AddMenuButton(PetMenuType.DIY, "查看等级",
            () => Host.ShowMessage($"当前等级为 {Host.Save.Level}"));
    }
}
```

运行 `dotnet build -c Release`，把生成的 `PetLevel.dll` 放入游戏的 MOD 目录。游戏提供 `VPet-Simulator.Unified.dll`，MOD 只需分发插件及其自带依赖。

```text
mod/
└── PetLevel/
    ├── info.lps
    └── plugin/
        └── PetLevel.dll
```

`info.lps` 的 `vupmod` 填写与 `PluginName` 相同的名称。将 `YourName` 换成作者名，`gamever` 填写测试插件时使用的游戏版本整数。

```text
vupmod#PetLevel:|author#YourName:|gamever#11075:|ver#10000:|
intro#在自定菜单中查看当前桌宠的等级:|
```

## 插件可以访问什么

- 用 `Host.Save` 读取等级和金钱，用 `Host.Statistics` 读写统计。插件自己的数据写入 `Host.SaveData`。
- 用 `Host.Items` 注册物品使用行为，调用 `Host.AddMenuButton` 添加菜单按钮，通过 `Host.Pet` 与桌宠互动。
- 订阅 `Host.MutiPlayerHandle` 取得当前访客表的 `IMPHost`。插件可以查看成员、接收自定义消息，也可以向好友发送消息或向房间广播。

原有 Windows 代码 MOD 可继续使用 `MainPlugin` 和 Windows.Interface。需要 WPF 或 Avalonia 原生控件的视图由相应平台实现。[Windows 插件包](https://www.nuget.org/packages/VPet-Simulator.Windows.Interface/) · [Windows 插件示例](https://github.com/LorisYounger/VPet.Plugin.Demo) · [反馈问题](https://github.com/LorisYounger/VPet/issues)
