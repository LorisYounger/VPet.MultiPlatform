using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 年度报告的指标与分档
/// </summary>
/// <remarks>阈值取自 Windows 版 winCharacterPanel.GenRank。档位从 1 开始，宿主据此绘制星级并选择对应台词。</remarks>
public static class StatsSummary
{
    /// <summary>
    /// 按升序阈值计算档位
    /// </summary>
    /// <param name="value">待分档的数值</param>
    /// <param name="thresholds">从小到大的阈值</param>
    /// <returns>1 到阈值个数加 1</returns>
    /// <remarks>小于首个阈值时为 1 档，每达到一个阈值升一档。</remarks>
    public static int Tier(double value, params double[] thresholds)
    {
        for (int i = 0; i < thresholds.Length; i++)
        {
            if (value < thresholds[i])
                return i + 1;
        }
        return thresholds.Length + 1;
    }

    /// <summary>
    /// 把排行榜名次折算成 0~1 的百分位
    /// </summary>
    /// <param name="globalRank">排行榜名次，未上榜时传 null</param>
    /// <param name="entryCount">榜上总人数</param>
    /// <returns>百分位越大，名次越靠前。未上榜或榜单为空时返回 0。</returns>
    public static double RankPercentile(int? globalRank, double entryCount)
    {
        if (globalRank == null || entryCount <= 0)
            return 0;
        return 1 - (globalRank.Value - 1) / entryCount;
    }

    // ---- 派生指标 ----

    /// <summary>秒换算成小时</summary>
    public static double Hours(long seconds) => seconds / 3600.0;

    /// <summary>平均每天陪伴多少小时</summary>
    public static double HoursPerDay(double hours, double days) => days <= 0 ? 0 : hours / days;

    /// <summary>工作时长占总在线时长的比例</summary>
    public static double WorkRatio(long workSeconds, long totalSeconds)
        => totalSeconds <= 0 ? 0 : (double)workSeconds / totalSeconds;

    /// <summary>自动购买占总购买次数的比例</summary>
    public static double AutoBuyRatio(int autoBuyTimes, int buyTimes)
        => buyTimes <= 0 ? 0 : (double)autoBuyTimes / buyTimes;

    // ---- 各项分档 ----

    /// <summary>陪伴时长百分位低于此值时使用“多陪陪我”的台词</summary>
    public const double CompanionRankSplit = 0.5;

    /// <summary>每天陪伴多少小时 → 1(同学) 到 5(女鹅)</summary>
    public static int CompanionTier(double hoursPerDay) => Tier(hoursPerDay, 2, 4, 7, 10);

    /// <summary>桌宠等级 → 1(小学) 到 5(砖家)</summary>
    public static int StudyTier(int level) => Tier(level, 20, 40, 60, 80);

    /// <summary>单次学习收益的排行百分位 → 5(垫底) 到 1(顶尖)</summary>
    public static int StudyExpTier(double rankPercentile) => 6 - Tier(rankPercentile, 0.25, 0.4, 0.55, 0.75);

    /// <summary>单次打工收益的排行百分位 → 4(垫底) 到 1(顶尖)</summary>
    public static int WorkMoneyTier(double rankPercentile) => 5 - Tier(rankPercentile, 0.25, 0.5, 0.75);

    /// <summary>打工时长占比的排行百分位 → 1(摸鱼) 到 6(卷王)</summary>
    public static int WorkTimeTier(double rankPercentile) => Tier(rankPercentile, 0.25, 0.35, 0.45, 0.55, 0.75);

    /// <summary>自动购买占比 → 4(亲力亲为) 到 1(全自动)</summary>
    public static int AutoBuyTier(double autoBuyRatio) => 5 - Tier(autoBuyRatio, 0.25, 0.5, 0.75);

    /// <summary>创意工坊 MOD 数的排行百分位 → 3(没装) 到 1(大师)</summary>
    /// <param name="modCount">已安装的 MOD 数量</param>
    /// <param name="rankPercentile">排行百分位</param>
    /// <remarks>未安装 MOD 时直接返回 3 档。</remarks>
    public static int ModTier(int modCount, double rankPercentile)
        => modCount == 0 ? 3 : 4 - Tier(rankPercentile, 0.3, 0.7);

    /// <summary>显示特殊好感度图标的阈值，超过此值时显示</summary>
    public const double LikabilityIconThreshold = 100;

    // ---- 移动距离 ----

    /// <summary>
    /// 长度换算使用的每英寸像素数
    /// </summary>
    public const double DpiPerInch = 96;

    /// <summary>
    /// 把像素折算成合适的长度单位
    /// </summary>
    /// <param name="px">像素</param>
    /// <returns>格式化后的数值和单位，像素保留整数，其余单位保留一位小数</returns>
    /// <remarks>按像素数选择显示单位，再按每英寸 96 像素换算长度。</remarks>
    public static (string Value, string Unit) LengthFromPixels(long px)
    {
        if (px < 37795)
            return (px.ToString(), "px");
        if (px < 3779527)
            return ((px * 2.54 / DpiPerInch).ToString("f1"), "cm");
        if (px < 377952755)
            return ((px * 2.54 / (DpiPerInch * 100)).ToString("f1"), "m");
        return ((px * 2.54 / (DpiPerInch * 100000)).ToString("f1"), "km");
    }
}
