namespace VPet_Simulator.Unified.Interface;

// 例如，两个宿主都编译主库的 IGameSave.cs。Windows 将 IGameSave.ModeType 编入
// VPet-Simulator.Core.dll，跨平台将它编入 VPet_Simulator.Core.Base.dll。
// 插件使用 VPet-Simulator.Unified.dll 中的 PetModeType，同一份插件 DLL 就能读写两端的 Save.Mode。
// 宿主按数值转换。下面前七个枚举目前手写，修改对应原枚举时要核对成员名和数值。
// 最后的 PetTextKind 用来选择宿主的四个文本集合，是 Unified 自己定义的分类。

/// <summary>
/// 动画类型
/// </summary>
public enum PetGraphType
{
    Common,
    Raised_Dynamic,
    Raised_Static,
    Move,
    Default,
    Touch_Head,
    Touch_Body,
    Idel,
    Sleep,
    Say,
    StateONE,
    StateTWO,
    StartUP,
    Shutdown,
    Work,
    Switch_Up,
    Switch_Down,
    Switch_Thirsty,
    Switch_Hunger,
    SideHide_Left_Main,
    SideHide_Left_Rise,
    SideHide_Right_Main,
    SideHide_Right_Rise,
}

/// <summary>
/// 动画的动作段
/// </summary>
public enum PetAnimatType
{
    /// <summary>单段动画</summary>
    Single,
    /// <summary>开始段</summary>
    A_Start,
    /// <summary>循环段</summary>
    B_Loop,
    /// <summary>结束段</summary>
    C_End,
}

/// <summary>
/// 桌宠当前的活动
/// </summary>
public enum PetWorkingState
{
    /// <summary>空闲</summary>
    Nomal,
    /// <summary>工作或学习中</summary>
    Work,
    /// <summary>睡觉</summary>
    Sleep,
    /// <summary>旅游中</summary>
    Travel,
    /// <summary>预留给插件的活动状态</summary>
    Empty,
}

/// <summary>
/// 桌宠的状态模式
/// </summary>
public enum PetModeType
{
    /// <summary>高兴</summary>
    Happy,
    /// <summary>普通</summary>
    Nomal,
    /// <summary>状态不佳</summary>
    PoorCondition,
    /// <summary>生病</summary>
    Ill,
}

/// <summary>
/// 食物类型
/// </summary>
public enum PetFoodType
{
    /// <summary>食物 (默认)</summary>
    Food,
    /// <summary>收藏 (自定义)</summary>
    Star,
    /// <summary>正餐</summary>
    Meal,
    /// <summary>零食</summary>
    Snack,
    /// <summary>饮料</summary>
    Drink,
    /// <summary>功能性</summary>
    Functional,
    /// <summary>药品</summary>
    Drug,
    /// <summary>礼品</summary>
    Gift,
}

/// <summary>
/// 工具栏上的一级菜单
/// </summary>
public enum PetMenuType
{
    /// <summary>投喂</summary>
    Feed,
    /// <summary>互动</summary>
    Interact,
    /// <summary>自定</summary>
    DIY,
    /// <summary>系统</summary>
    Setting,
}

/// <summary>
/// 照片类型
/// </summary>
public enum PetPhotoType
{
    /// <summary>全部</summary>
    ALL,
    /// <summary>插图</summary>
    Illustration,
    /// <summary>缩略图</summary>
    Thumbnail,
}

/// <summary>
/// 桌宠文本类型
/// </summary>
/// Windows 宿主按行名（如 lowfoodtext、clicktext）区分文本，插件按枚举值查询。
public enum PetTextKind
{
    /// <summary>饿了会说的话</summary>
    LowFood,
    /// <summary>渴了会说的话</summary>
    LowDrink,
    /// <summary>点击时说的话</summary>
    Click,
    /// <summary>对话框里的选项</summary>
    Select,
}
