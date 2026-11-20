using LinePutScript;
using LinePutScript.Localization;
using VPet_Simulator.Core.MutiPlatform.Display.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using VPet_Simulator.Core;
using VPet_Simulator.Core.MutiPlatform;
using VPet_Simulator.Core.MutiPlatform.Display;
using VPet_Simulator.Unified.Interface;
using VPet_Simulator.Windows.Interface;
using static VPet_Simulator.Core.MutiPlatform.GraphHelper;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// Avalonia 宿主提供给统一契约插件的接口
/// </summary>
/// <remarks>每个窗口中的每个统一插件实例各有一个宿主。统一插件可在两个宿主使用同一份 DLL。
/// 读档会替换 GameSavesData，宿主从当前窗口读取存档和统计数据。</remarks>
internal sealed partial class UnifiedPluginHost : IPetHost
{
    private readonly MainWindow mw;
    internal CoreMOD Mod { get; }
    private readonly List<IDisposable> closables = new();
    private readonly UnifiedWrap.SaveView saveView;
    private readonly UnifiedWrap.StatisticsView statisticsView;
    private readonly UnifiedItemRegistry itemRegistry;
    private readonly UnifiedPetView petView;

    /// <summary>
    /// 插件本体
    /// </summary>
    public UnifiedPlugin Plugin { get; private set; } = null!;

    private UnifiedPluginHost(MainWindow mw, CoreMOD mod)
    {
        this.mw = mw;
        Mod = mod;
        saveView = new UnifiedWrap.SaveView(mw);
        statisticsView = new UnifiedWrap.StatisticsView(mw);
        itemRegistry = new UnifiedItemRegistry(mw);
        petView = new UnifiedPetView(mw);
    }

    /// <summary>
    /// 建立宿主并实例化插件
    /// </summary>
    /// <remarks>CoreMOD 在读档前创建插件。插件在构造函数中注册的物品创建器会在读取对应背包物品时调用。</remarks>
    internal static UnifiedPluginHost? Create(MainWindow mw, CoreMOD mod, Type pluginType)
    {
        var host = new UnifiedPluginHost(mw, mod);
        mw.UnifiedHosts.Add(host);
        mw.GameSavesData.Statistics.StatisticChanged += host.statisticsView.OnChanged;
        host.itemRegistry.Attach();
        try
        {
            host.Plugin = (UnifiedPlugin)Activator.CreateInstance(pluginType, host)!;
            return host;
        }
        catch
        {
            // 插件在构造期间就能访问存档和订阅事件。
            mw.UnifiedHosts.Remove(host);
            host.statisticsView.Detach();
            host.itemRegistry.Detach();
            throw;
        }
    }

    // ---- 生命周期 ----

    internal void OnLoadPlugin()
    {
        petView.Attach();
        Plugin.LoadPlugin();
    }

    internal void OnSaveChanged(Statistics previous)
    {
        previous.StatisticChanged -= statisticsView.OnChanged;
        mw.GameSavesData.Statistics.StatisticChanged += statisticsView.OnChanged;
    }

    internal void OnEndGame()
    {
        try
        {
            Plugin.EndGame();
        }
        finally
        {
            petView.Detach();
            // 后注册的资源可能依赖先注册的资源，退出时先释放。
            for (int i = closables.Count - 1; i >= 0; i--)
            {
                try { closables[i].Dispose(); }
                catch (Exception ex) { Log($"Failed to dispose plugin resource: {ex.Message}"); }
            }
            closables.Clear();
            statisticsView.Detach();
            itemRegistry.Detach();
        }
    }

    // ---- 宿主能力 ----

    public string HostName => "MutiPlatform";
    public int HostVersion => mw.version;
    public bool IsUIThread => mw.Dispatcher.CheckAccess();
    public void RunOnUI(Action action) => mw.Dispatcher.Invoke(action);
    public T RunOnUI<T>(Func<T> func) => mw.Dispatcher.Invoke(func);
    public string Translate(string text) => LocalizeCore.Translate(text);
    public string Translate(string text, params object[] args) => LocalizeCore.Translate(text, args);
    public void Log(string message) => Console.WriteLine($"[{Plugin?.PluginName ?? Mod.Name}] {message}");
    public void ShowMessage(string text, string title = "")
        => RunOnUI(() => NoticeBox.Show(text, string.IsNullOrEmpty(title) ? Mod.Name : title));
    public void ShowInputBox(string title, string text, string defaultText, Action<string> end, bool allowMultiLine = false)
        => RunOnUI(() => mw.ShowInputBox(title, text, defaultText, end, allowMultiLine));

    /// <summary>
    /// 获取当前宿主提供的对象
    /// </summary>
    /// <remarks>依赖 Avalonia 专属类型的插件需要对应平台。插件构造期间 Main 尚未创建。</remarks>
    public T? GetService<T>() where T : class
        => new object?[] { mw, mw.Main, mw.Core, mw.Set, mw.GameSavesData, mw.Dispatcher }
            .OfType<T>().FirstOrDefault();

    // ---- 插件与 MOD ----

    public IPluginInfo Info => new UnifiedWrap.PluginInfoView(Mod, mw);
    public IEnumerable<IPluginInfo> Mods => mw.ModInfo.Select(x => (IPluginInfo)new UnifiedWrap.PluginInfoView(x, mw));
    public IEnumerable<IPluginInfo> OnMods => mw.OnModInfo.Select(x => (IPluginInfo)new UnifiedWrap.PluginInfoView(x, mw));
    public IReadOnlyList<string> ModPaths => mw.MODPath.Select(x => x.FullName).ToList();
    public string DataDirectory => AppPaths.DataRoot;
    public string GetModStorage(string modName) => ExtensionValue.GetMODStorage(modName);
    public string PrefixSave => mw.PrefixSave;

    // ---- 设置与存档 ----

    public ILine Setting(string lineName) => mw.Set[lineName];
    public ILPS SaveData => mw.GameSavesData.Data;
    public IPetSave Save => saveView;
    public IPetStatistics Statistics => statisticsView;
    public bool HashCheck => mw.HashCheck;
    public void HashCheckOff() => mw.HashCheckOff();
    public void SaveGame() => mw.Save();
    public IDictionary<string, object> DynamicResources => mw.DynamicResources;

    // ---- 数据表 ----

    public IReadOnlyList<IFoodInfo> Foods => mw.Foods.Select(UnifiedWrap.Of).ToList();
    public IReadOnlyList<IPhotoInfo> Photos => mw.Photos.Select(x => UnifiedWrap.Of(x, mw)).ToList();

    public IReadOnlyList<ITextInfo> Texts(PetTextKind kind) => kind switch
    {
        PetTextKind.LowFood => mw.LowFoodText.Select(x => (ITextInfo)new UnifiedWrap.TextView(x, kind)).ToList(),
        PetTextKind.LowDrink => mw.LowDrinkText.Select(x => (ITextInfo)new UnifiedWrap.TextView(x, kind)).ToList(),
        PetTextKind.Click => mw.ClickTexts.Select(x => (ITextInfo)new UnifiedWrap.TextView(x, kind)).ToList(),
        PetTextKind.Select => mw.SelectTexts.Select(x => (ITextInfo)new UnifiedWrap.TextView(x, kind)).ToList(),
        _ => new List<ITextInfo>(),
    };

    public void AddText(PetTextKind kind, ILine line)
    {
        switch (kind)
        {
            case PetTextKind.LowFood:
                Add(mw.LowFoodText, LinePutScript.Converter.LPSConvert.DeserializeObject<LowText>(line));
                break;
            case PetTextKind.LowDrink:
                Add(mw.LowDrinkText, LinePutScript.Converter.LPSConvert.DeserializeObject<LowText>(line));
                break;
            case PetTextKind.Click:
                Add(mw.ClickTexts, LinePutScript.Converter.LPSConvert.DeserializeObject<ClickText>(line));
                break;
            case PetTextKind.Select:
                Add(mw.SelectTexts, LinePutScript.Converter.LPSConvert.DeserializeObject<SelectText>(line));
                break;
        }

        void Add<T>(List<T> list, T? text) where T : class
        {
            if (text == null)
                Log($"Skipped text line that could not be read as {typeof(T).Name}: {line}");
            else
                list.Add(text);
        }
    }

    public ITextInfo? GetClickText()
    {
        var text = mw.GetClickText();
        return text == null ? null : new UnifiedWrap.TextView(text, PetTextKind.Click);
    }

    public string? FindImagePath(string name, string? superior = null)
        => mw.ImageSources.FindSource(name) ?? (superior == null ? null : mw.ImageSources.FindSource(superior));
    public string? FindFilePath(string name) => mw.FileSources.FindSource(name);

    // ---- 物品与桌宠 ----

    public IItemRegistry Items => itemRegistry;
    public IPetView Pet => petView;

    // ---- 界面挂点 ----

    public void AddMenuButton(PetMenuType menu, string displayName, Action click)
        => RunOnUI(() => mw.Main.ToolBar.AddMenuButton((ToolBar.MenuType)(int)menu, displayName, click));

    /// <summary>
    /// 往一级菜单下的分组里加按钮
    /// </summary>
    /// <remarks>同一菜单下的按钮共用同名分组。首次添加时创建分组。</remarks>
    public void AddMenuButton(PetMenuType menu, string groupName, string displayName, Action click)
        => RunOnUI(() => mw.Main.ToolBar.AddMenuButton((ToolBar.MenuType)(int)menu, groupName, displayName, click));

    public void RegisterClosable(IDisposable closable) => closables.Add(closable);

    /// <summary>
    /// 登记语音播放器
    /// </summary>
    /// <remarks>Avalonia 宿主用这个播放器播放语音并同步口型。其他插件可从 DynamicResources 取得播放器。</remarks>
    public void RegisterVoicePlayer(IVoicePlayer player)
    {
        DynamicResources["UnifiedVoicePlayer"] = player;
        RegisterClosable(player);
        mw.SetVoicePlayer(player);
    }

    // ---- 杂项 ----

    public void SetZoomLevel(double level) => RunOnUI(() => mw.SetZoomLevel(level));
    public void Close() => RunOnUI(() => mw.Close());
    public void Restart() => RunOnUI(() => mw.Restart());

    public event Action? NewDay
    {
        add => mw.Event_NewDay += value;
        remove => mw.Event_NewDay -= value;
    }
}
