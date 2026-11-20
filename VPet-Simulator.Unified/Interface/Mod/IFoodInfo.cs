using LinePutScript;
using LinePutScript.Converter;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Unified.Interface;

/// <summary>
/// 宿主中的食物及其当前属性
/// </summary>
/// <remarks>插件直接读写宿主的食物对象。宿主调整价格或收藏状态后，插件下次读取即可看到新值。</remarks>
public interface IFoodInfo
{
    /// <summary>食物名称，同时用作主键</summary>
    string Name { get; }
    /// <summary>翻译后的名字</summary>
    string TranslateName { get; }
    PetFoodType Type { get; set; }

    int Exp { get; set; }
    double Strength { get; set; }
    double StrengthFood { get; set; }
    double StrengthDrink { get; set; }
    double Feeling { get; set; }
    double Health { get; set; }
    double Likability { get; set; }

    /// <summary>价格</summary>
    double Price { get; set; }
    /// <summary>描述</summary>
    string Desc { get; set; }
    /// <summary>食用时播放的动画，留空时按食物类型推断</summary>
    string? Graph { get; set; }
    /// <summary>图片名称，留空时按食物名称查找</summary>
    string? Image { get; }
    /// <summary>图片的绝对路径，找不到时为 null</summary>
    string? ImagePath { get; }
    /// <summary>是否已收藏</summary>
    bool Star { get; set; }

    /// <summary>取食用时该播的动画名</summary>
    string GetGraph();
    /// <summary>按当前数值算出的推荐价格</summary>
    double RealPrice { get; }
    /// <summary>定价是否低于超模判定阈值</summary>
    bool IsOverLoad();
    /// <summary>序列化成一行</summary>
    ILine ToLine();
}

/// <summary>
/// 背包里的一件物品
/// </summary>
public interface IItemInfo
{
    string Name { get; }
    string TranslateName { get; }
    /// <summary>物品类型，决定使用时调用的处理器</summary>
    string ItemType { get; set; }
    string Desc { get; set; }
    /// <summary>给界面显示的完整描述</summary>
    string Description { get; }
    double Price { get; set; }
    int Count { get; set; }
    /// <summary>随存档保存的 MOD 自定义数据</summary>
    string Data { get; set; }
    bool CanUse { get; set; }
    bool Star { get; set; }
    /// <summary>是否标记为单件物品。使用时是否消耗由处理器决定。</summary>
    bool IsSingle { get; set; }
    /// <summary>在背包里是否可见</summary>
    bool Visibility { get; set; }
    string? Image { get; }
    string? ImagePath { get; }

    /// <summary>物品是食物时返回对应的食物视图，其他物品返回 null</summary>
    IFoodInfo? AsFood { get; }
    /// <summary>使用物品，是否消耗由使用处理器决定</summary>
    void Use();
    /// <summary>消耗指定数量</summary>
    void Consume(int count = 1);
    /// <summary>序列化成一行</summary>
    ILine ToLine();
}

/// <summary>
/// 自定义物品的创建数据
/// </summary>
/// <remarks>创建器返回这些字段，由宿主创建对应的物品。[Line] 子项名与 Windows 版 Item 一致，以便两端读取同一份存档。</remarks>
public class UnifiedItem
{
    // 存档中的 name 和 itemtype 固定使用小写，其余字段沿用属性名。
    [Line(name: "name")] public string Name { get; set; } = string.Empty;
    [Line(name: "itemtype")] public string ItemType { get; set; } = "Item";
    [Line(ignoreCase: true)] public string Desc { get; set; } = string.Empty;
    [Line(ignoreCase: true)] public double Price { get; set; }
    [Line(ignoreCase: true)] public int Count { get; set; } = 1;
    [Line(ignoreCase: true)] public string Data { get; set; } = string.Empty;
    [Line(ignoreCase: true)] public bool CanUse { get; set; } = true;
    [Line(ignoreCase: true)] public bool Star { get; set; }
    [Line(ignoreCase: true)] public bool IsSingle { get; set; }
    [Line(ignoreCase: true)] public bool Visibility { get; set; } = true;
    [Line(ignoreCase: true)] public string? Image { get; set; }
}

/// <summary>
/// 一张收藏照片
/// </summary>
public interface IPhotoInfo
{
    string Name { get; }
    string TranslateName { get; }
    string Description { get; }
    string Tags { get; }
    PetPhotoType Type { get; }
    /// <summary>是否已解锁</summary>
    bool IsUnlock { get; }
    /// <summary>是否已标为喜欢</summary>
    bool IsStar { get; set; }
    /// <summary>解锁它</summary>
    void Unlock();
}

/// <summary>
/// 一条桌宠对话文本
/// </summary>
public interface ITextInfo
{
    PetTextKind Kind { get; }
    /// <summary>对话原文，也用作翻译键</summary>
    string Text { get; }
    /// <summary>翻译后的文本</summary>
    string TranslateText { get; }
    /// <summary>内容标签</summary>
    string Tag { get; }
    ILine ToLine();
}

/// <summary>
/// 工作、学习或玩耍项目
/// </summary>
public interface IWorkInfo
{
    string Name { get; }
    string TranslateName { get; }
    /// <summary>Work / Study / Play</summary>
    string Type { get; }
    /// <summary>基础收益</summary>
    double MoneyBase { get; }
    /// <summary>时长(分钟)</summary>
    int Time { get; }
    /// <summary>等级要求</summary>
    int LevelLimit { get; }
}

/// <summary>
/// MOD 元数据
/// </summary>
public interface IPluginInfo
{
    string Name { get; }
    string Author { get; }
    long AuthorID { get; }
    ulong ItemID { get; }
    string Intro { get; }
    int GameVer { get; }
    int Ver { get; }
    /// <summary>MOD 目录</summary>
    string Path { get; }
    /// <summary>MOD 提供的内容类型</summary>
    IReadOnlyCollection<string> Tag { get; }
    /// <summary>玩家是否启用了它</summary>
    bool IsEnabled { get; }
}
