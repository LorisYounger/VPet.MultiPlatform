using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 投喂的吃腻度与统计
/// </summary>
/// <remarks>连续投喂同一种食物时，效果随吃腻度降低。统计项沿用 Windows 版 MainWindow.TakeItem 使用的名称，供年度报告和成就读取。</remarks>
public static class FeedingRules
{
    /// <summary>
    /// 记录各食物吃腻效果结束时间的存档行名
    /// </summary>
    public const string BuyTimeLineName = "buytime";

    /// <summary>
    /// 吃腻时保留的最低效果比例
    /// </summary>
    public const double MinEffectiveness = 0.5;

    /// <summary>
    /// 食物类型
    /// </summary>
    /// <remarks>数值与 Windows 版 Food.FoodType 和跨平台版 FoodItem.FoodType 对应。</remarks>
    public enum FoodKind
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
    /// 计算吃腻效果的剩余小时数
    /// </summary>
    /// <param name="boredUntil">存档记录的吃腻效果结束时间</param>
    /// <param name="now">现在</param>
    public static double RemainingBoredom(DateTime boredUntil, DateTime now)
        => boredUntil > now ? (boredUntil - now).TotalHours : 0;

    /// <summary>
    /// 计算本次投喂的效果比例
    /// </summary>
    /// <param name="boredomHours">吃腻效果的剩余小时数</param>
    /// <param name="isGift">是否为礼品。礼品的效果衰减较慢。</param>
    /// <remarks>效果随剩余小时数的平方衰减，最低保留五成。</remarks>
    public static double Effectiveness(double boredomHours, bool isGift)
        => Math.Max(MinEffectiveness, 1 - boredomHours * boredomHours * (isGift ? 0.01 : 0.02));

    /// <summary>
    /// 计算本次投喂增加的吃腻时长（小时）
    /// </summary>
    /// <param name="likability">本次投喂增加的好感度</param>
    /// <param name="feeling">本次投喂增加的心情</param>
    /// <remarks>好感度和心情增益越高，增加的时长越短，范围为半小时到四小时。</remarks>
    public static double AddedBoredom(double likability, double feeling)
        => Math.Max(0.5, Math.Min(4, 2 - (likability + feeling / 2) / 5));

    // ---- 统计项名 ----

    /// <summary>购买总次数的统计项名</summary>
    public const string BuyTimesStat = "stat_buytimes";

    /// <summary>累计消费金额的统计项名</summary>
    public const string TotalSpendStat = "stat_betterbuy";

    /// <summary>指定食物购买次数的统计项名</summary>
    public static string BuyCountStat(string foodName) => "buy_" + foodName;

    /// <summary>
    /// 按类型分开记的花费统计项名
    /// </summary>
    /// <returns>这个类型不单独记账时返回 null</returns>
    /// <remarks>收藏类型没有独立的花费统计项。</remarks>
    public static string? SpendStat(FoodKind kind) => kind switch
    {
        FoodKind.Food => "stat_bb_food",
        FoodKind.Drink => "stat_bb_drink",
        FoodKind.Drug => "stat_bb_drug",
        FoodKind.Snack => "stat_bb_snack",
        FoodKind.Functional => "stat_bb_functional",
        FoodKind.Meal => "stat_bb_meal",
        FoodKind.Gift => "stat_bb_gift",
        _ => null,
    };

    /// <summary>药品累计加了多少经验</summary>
    public const string DrugExpStat = "stat_bb_drug_exp";

    /// <summary>礼品累计加了多少好感</summary>
    public const string GiftLikeStat = "stat_bb_gift_like";
}
