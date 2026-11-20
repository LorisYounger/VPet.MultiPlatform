using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 打工套餐的签约条件与日程工作占比
/// </summary>
/// <remarks>计算规则取自 Windows 版 ScheduleTask.Package 和 winWorkMenu。</remarks>
public static class SchedulePackageRules
{
    /// <summary>
    /// 计算套餐签约费用
    /// </summary>
    /// <param name="basePrice">套餐的基础价</param>
    /// <param name="level">签约档次</param>
    public static double SignPrice(double basePrice, int level) => basePrice * (200 * level - 100);

    /// <summary>
    /// 计算签约后可用的等级
    /// </summary>
    public static int GrantedLevel(int level, double levelInNeed) => (int)(level / levelInNeed);

    /// <summary>
    /// 套餐的到期时间
    /// </summary>
    public static DateTime EndTime(DateTime now, int durationDays) => now.AddDays(durationDays);

    /// <summary>
    /// 判断套餐是否仍在有效期内
    /// </summary>
    public static bool IsActive(DateTime endTime, DateTime now) => now < endTime;

    /// <summary>
    /// 日程工作占比超过此值时标红
    /// </summary>
    public const double WorkRatioWarning = 0.71;

    /// <summary>
    /// 工作占总时长的比例
    /// </summary>
    public static double WorkRatio(int workTime, int restTime)
        => workTime + restTime == 0 ? 0 : workTime / (double)(workTime + restTime);
}
