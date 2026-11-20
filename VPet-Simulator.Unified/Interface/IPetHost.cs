using LinePutScript;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// 跨平台语音 MOD 提供的播放器
/// </summary>
/// <remarks>消息栏根据剩余播放时间决定何时收起气泡。</remarks>
public interface IVoicePlayer : IDisposable
{
    /// <summary>是否正在播放</summary>
    bool IsPlaying { get; }
    /// <summary>剩余播放时间，未知时返回 TimeSpan.Zero</summary>
    TimeSpan Remaining { get; }
    /// <summary>音量，范围为 0 到 1</summary>
    double Volume { get; set; }
    /// <summary>播放</summary>
    void Play(string path);
    /// <summary>停止播放</summary>
    void Stop();
    /// <summary>播放结束时触发</summary>
    event Action? Completed;
}

/// <summary>
/// 物品注册表
/// </summary>
public interface IItemRegistry
{
    /// <summary>当前背包中的物品。每次读取时获取列表，物品属性随宿主更新。</summary>
    IReadOnlyList<IItemInfo> All { get; }

    /// <summary>添加物品。背包中已有同名物品时，将数量加到已有物品上。</summary>
    void Add(UnifiedItem item);

    /// <summary>
    /// 注册自定义物品的创建器
    /// </summary>
    /// <param name="itemType">物品类型名</param>
    /// <param name="creator">从存档行创建物品，返回 null 表示未创建</param>
    /// <remarks>建议在插件构造函数中注册，此时尚未读档。在 LoadPlugin 中注册时，Windows 宿主会用背包中同类型普通物品的存档字段重新创建物品。</remarks>
    void RegisterCreator(string itemType, Func<ILine, UnifiedItem?> creator);

    /// <summary>
    /// 注册指定物品类型的使用处理器
    /// </summary>
    /// <param name="itemType">物品类型名</param>
    /// <param name="action">处理器按注册顺序执行，返回 true 时结束本次处理。</param>
    void RegisterUseAction(string itemType, Func<IItemInfo, bool> action);

    /// <summary>投喂并结算数值，此操作不扣钱</summary>
    void Take(IFoodInfo food);
    /// <summary>记录物品使用情况，供统计和成就使用</summary>
    void TakeHandle(IFoodInfo food, int count, string from);

    /// <summary>投喂时触发</summary>
    event Action<IFoodInfo>? TakeItem;
    /// <summary>上次投喂时间</summary>
    DateTime LastTakeItemTime { get; }
}

/// <summary>
/// 桌宠的活动、动画、对话和语音
/// </summary>
/// <remarks>需要操作界面控件的插件可通过 IPetHost.GetService 获取宿主专属接口。</remarks>
public interface IPetView
{
    /// <summary>当前活动</summary>
    PetWorkingState State { get; set; }
    /// <summary>当前工作，空闲时为 null</summary>
    IWorkInfo? NowWork { get; }
    /// <summary>上次和桌宠互动的时间</summary>
    DateTime LastInteractionTime { get; set; }

    /// <summary>按名字播动画</summary>
    void Display(string name, PetAnimatType animat, Action? endAction = null);
    /// <summary>按类型播动画</summary>
    void Display(PetGraphType type, PetAnimatType animat, Action? endAction = null);
    /// <summary>回到默认动画</summary>
    void DisplayToNomal();
    /// <summary>播放带图片的动画，如喂食动画</summary>
    /// <param name="graphName">动画名</param>
    /// <param name="imagePath">动画中显示的图片路径</param>
    void DisplayFoodAnimation(string graphName, string imagePath);

    /// <summary>说话</summary>
    void Say(string text, string? graphName = null, bool force = false, string? desc = null);
    /// <summary>说话时随机选择表情</summary>
    void SayRnd(string text, bool force = false, string? desc = null);
    /// <summary>在桌宠身上显示浮动文字</summary>
    void LabelDisplayShow(string text, int time = 2000);

    /// <summary>摸头时触发</summary>
    event Action? TouchHead;
    /// <summary>摸身体时触发</summary>
    event Action? TouchBody;
    /// <summary>说话时触发</summary>
    event Action<string>? OnSay;
    /// <summary>开始工作时触发</summary>
    event Action<IWorkInfo>? WorkStart;
    /// <summary>工作结束时触发，参数依次为工作、收益和用时（分钟）</summary>
    event Action<IWorkInfo, double, double>? WorkEnd;
    /// <summary>每个逻辑周期在计时器线程触发一次</summary>
    event Action? Tick;

    /// <summary>播放语音</summary>
    void PlayVoice(string path);
    /// <summary>语音音量</summary>
    double VoiceVolume { get; set; }
    /// <summary>是否正在播放语音</summary>
    bool PlayingVoice { get; }
}

/// <summary>
/// Windows 和跨平台宿主提供给 MOD 的公共接口
/// </summary>
/// <remarks>
/// <para>只依赖此接口的插件可以在两个宿主使用同一份 DLL。宿主负责调度其界面操作，插件自行创建控件时须在 UI 线程执行。</para>
/// <para>插件构造函数在后台线程执行，此时只做注册，存档和动画尚未就绪。</para>
/// <para>LoadPlugin 在读档后执行，LoadDIY 在每次重建自定菜单时执行，GameLoaded 在动画就绪、桌宠启动后执行。这三个回调和 EndGame 都在 UI 线程执行。</para>
/// <para>Save 在发起保存的线程执行（自动保存计时器或 UI 线程）。Tick 和 WorkEnd 在计时器线程执行。</para>
/// </remarks>
public interface IPetHost
{
    // ---- 宿主 ----

    /// <summary>"Windows" 或 "MutiPlatform"</summary>
    string HostName { get; }
    /// <summary>宿主版本号</summary>
    int HostVersion { get; }
    /// <summary>当前窗口是否已连接 Steam</summary>
    bool IsSteamUser { get; }
    /// <summary>本窗口的联机访客表就绪时触发。每次重新打开都会提供当前访客表。</summary>
    event Action<IMPHost>? MutiPlayerHandle;
    /// <summary>当前线程是否为 UI 线程</summary>
    bool IsUIThread { get; }
    /// <summary>切到 UI 线程执行</summary>
    void RunOnUI(Action action);
    /// <summary>切到 UI 线程取值</summary>
    T RunOnUI<T>(Func<T> func);
    /// <summary>翻译</summary>
    string Translate(string text);
    /// <summary>翻译并格式化</summary>
    string Translate(string text, params object[] args);
    /// <summary>写一行日志</summary>
    void Log(string message);
    /// <summary>向玩家显示提示</summary>
    void ShowMessage(string text, string title = "");
    /// <summary>显示输入框</summary>
    void ShowInputBox(string title, string text, string defaultText, Action<string> end, bool allowMultiLine = false);

    /// <summary>
    /// 获取指定类型的服务
    /// </summary>
    /// <remarks>使用宿主专属类型的插件依赖对应平台。Windows 可提供 IMainWindow 和 Main，跨平台宿主可提供 PetMain 和 Window。</remarks>
    T? GetService<T>() where T : class;

    // ---- 插件与 MOD ----

    /// <summary>本插件所属的 MOD</summary>
    IPluginInfo Info { get; }
    /// <summary>所有扫描到的 MOD</summary>
    IEnumerable<IPluginInfo> Mods { get; }
    /// <summary>已启用的 MOD</summary>
    IEnumerable<IPluginInfo> OnMods { get; }
    /// <summary>MOD 根目录</summary>
    IReadOnlyList<string> ModPaths { get; }
    /// <summary>可写的数据目录</summary>
    string DataDirectory { get; }
    /// <summary>获取指定 MOD 的可写目录，目录不存在时创建</summary>
    string GetModStorage(string modName);
    /// <summary>多开前缀</summary>
    string PrefixSave { get; }

    // ---- 设置与存档 ----

    /// <summary>
    /// 读写 Setting.lps 中的一行自定义设置
    /// </summary>
    /// <remarks>对应 Windows 版 ISetting 的行索引器。</remarks>
    ILine Setting(string lineName);
    /// <summary>
    /// 读写随存档保存的自定义数据
    /// </summary>
    /// <remarks>使用本 MOD 的行名前缀，避免与其他 MOD 重名。</remarks>
    ILPS SaveData { get; }
    /// <summary>桌宠的数值</summary>
    IPetSave Save { get; }
    /// <summary>统计数据</summary>
    IPetStatistics Statistics { get; }
    /// <summary>防作弊检查是否还有效</summary>
    bool HashCheck { get; }
    /// <summary>修改游戏数值前调用此方法关闭防作弊检查</summary>
    void HashCheckOff();
    /// <summary>立刻存档</summary>
    void SaveGame();
    /// <summary>MOD 之间共享的资源字典</summary>
    IDictionary<string, object> DynamicResources { get; }

    // ---- 数据表 ----

    /// <summary>当前食物列表。列表中的食物属性随宿主更新。</summary>
    IReadOnlyList<IFoodInfo> Foods { get; }
    /// <summary>当前照片列表。列表中的照片属性随宿主更新。</summary>
    IReadOnlyList<IPhotoInfo> Photos { get; }
    /// <summary>某一类说话文本</summary>
    IReadOnlyList<ITextInfo> Texts(PetTextKind kind);
    /// <summary>加一条说话文本</summary>
    void AddText(PetTextKind kind, ILine line);
    /// <summary>按当前时间和状态挑一条点击文本</summary>
    ITextInfo? GetClickText();
    /// <summary>查找图片路径，缺少专属图片时查找指定的上级资源</summary>
    string? FindImagePath(string name, string? superior = null);
    /// <summary>找 MOD 提供的文件</summary>
    string? FindFilePath(string name);

    // ---- 物品与桌宠 ----

    /// <summary>物品注册表</summary>
    IItemRegistry Items { get; }
    /// <summary>桌宠本体</summary>
    IPetView Pet { get; }

    // ---- 界面挂点 ----

    /// <summary>往工具栏某个一级菜单下加一个按钮</summary>
    void AddMenuButton(PetMenuType menu, string displayName, Action click);
    /// <summary>往工具栏某个一级菜单下的分组里加一个按钮</summary>
    void AddMenuButton(PetMenuType menu, string groupName, string displayName, Action click);
    /// <summary>登记退出时需要释放的窗口、计时器等资源</summary>
    void RegisterClosable(IDisposable closable);
    /// <summary>提供语音播放器</summary>
    void RegisterVoicePlayer(IVoicePlayer player);

    // ---- 杂项 ----

    void SetZoomLevel(double level);
    void Close();
    void Restart();
    /// <summary>新的一天开始时触发</summary>
    event Action? NewDay;
}

/// <summary>
/// 跨平台 MOD 的插件入口
/// </summary>
/// <remarks>两个宿主都只加载直接继承 UnifiedPlugin 的插件入口。旧版 MainPlugin 仍由 Windows 宿主加载。</remarks>
public abstract class UnifiedPlugin
{
    /// <summary>
    /// 插件名称，必须与 MOD 名称一致
    /// </summary>
    public abstract string PluginName { get; }

    /// <summary>
    /// 宿主
    /// </summary>
    public IPetHost Host { get; }

    /// <summary>
    /// 在后台线程构造插件
    /// </summary>
    /// <remarks>存档和动画尚未就绪。此时只注册物品创建器和使用处理器，读档后再访问游戏数据。</remarks>
    protected UnifiedPlugin(IPetHost host)
    {
        Host = host;
    }

    /// <summary>
    /// 读档后初始化插件
    /// </summary>
    /// <remarks>可在此订阅事件、添加菜单按钮或创建插件界面。</remarks>
    public virtual void LoadPlugin() { }

    /// <summary>
    /// 桌宠启动后调用
    /// </summary>
    /// <remarks>可在此修改已加载的内容。</remarks>
    public virtual void GameLoaded() { }

    /// <summary>
    /// 重建自定菜单
    /// </summary>
    /// <remarks>每次重建前，宿主都会清空自定菜单。</remarks>
    public virtual void LoadDIY() { }

    /// <summary>
    /// 保存插件数据
    /// </summary>
    /// <remarks>将数据写入 Host.SaveData 或 Host.Setting。</remarks>
    public virtual void Save() { }

    /// <summary>
    /// 打开本插件的设置界面
    /// </summary>
    /// <remarks>重写此方法后，宿主会显示插件的设置入口。</remarks>
    public virtual void Setting() { }

    /// <summary>
    /// 退出
    /// </summary>
    public virtual void EndGame() { }
}
