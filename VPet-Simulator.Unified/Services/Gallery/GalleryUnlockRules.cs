using System;
using System.Globalization;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 图库解锁的日期与条件判断
/// </summary>
/// <remarks>Windows 版保留 MOD 使用的 Photo.UnlockCondition 类型。这里提供农历换算、日期和时间范围判断，以及节日日期表。</remarks>
public static class GalleryUnlockRules
{
    /// <summary>
    /// 日期范围的默认天数，与 UnlockCondition.DateOffset 一致
    /// </summary>
    public const int DefaultDateOffset = 2;

    /// <summary>
    /// 时间范围的默认分钟数，与 UnlockCondition.TimeOffset 一致
    /// </summary>
    public const int DefaultTimeOffset = 60;

    /// <summary>
    /// 统计项缺失时的取值
    /// </summary>
    /// <remarks>取 -1，使缺失的统计项无法满足“至少 0 次”的条件。</remarks>
    public const int MissingStatValue = -1;

    /// <summary>
    /// 判断当前时间是否在起始日期之后、截止日期之前
    /// </summary>
    /// <param name="date">起始日期</param>
    /// <param name="now">现在</param>
    /// <param name="offsetDays">持续天数</param>
    public static bool InDateWindow(DateTime date, DateTime now, int offsetDays)
        => date < now && date.AddDays(offsetDays) > now;

    /// <summary>
    /// 判断当前时间是否在今天的指定时段内，包含起止时刻
    /// </summary>
    /// <param name="time">当天的起始时刻</param>
    /// <param name="now">现在</param>
    /// <param name="offsetMinutes">持续分钟数</param>
    public static bool InTimeWindow(TimeOnly time, DateTime now, int offsetMinutes)
    {
        var at = new DateTime(now.Year, now.Month, now.Day, time.Hour, time.Minute, time.Second);
        return at <= now && at.AddMinutes(offsetMinutes) >= now;
    }

    /// <summary>
    /// 将当年的农历日期换算为公历日期
    /// </summary>
    public static DateTime LunarDate(int month, int day)
        => LunarDate(month, day, DateTime.Now.Year);

    /// <summary>
    /// 将指定年份的农历日期换算为公历日期
    /// </summary>
    public static DateTime LunarDate(int month, int day, int year)
    {
        var lunar = new ChineseLunisolarCalendar();
        return lunar.ToDateTime(year, month, day, 0, 0, 0, 0);
    }

    /// <summary>
    /// 节假日
    /// </summary>
    /// <remarks>与 Windows 版 Photo.UnlockCondition.HolidayType 的数值对应，可按整数值转换。</remarks>
    public enum HolidayKind
    {
        /// <summary>不启用</summary>
        None,
        /// <summary>中秋</summary>
        Mid_Autumn_Festival,
        /// <summary>端午</summary>
        Dragon_Boat_Festival,
        /// <summary>新年</summary>
        New_Years_Day,
        /// <summary>春节</summary>
        Spring_Festival,
        /// <summary>圣诞</summary>
        Christmas,
        /// <summary>生日(玩家)</summary>
        Player_Birthday,
        /// <summary>七夕</summary>
        Qixi_Festival,
    }

    /// <summary>
    /// 获取指定节日在当前年份的日期
    /// </summary>
    /// <returns>玩家生日和未启用的节日返回 null</returns>
    public static DateTime? HolidayDate(HolidayKind kind, DateTime now) => kind switch
    {
        HolidayKind.Mid_Autumn_Festival => LunarDate(8, 15, now.Year),
        HolidayKind.Dragon_Boat_Festival => LunarDate(5, 5, now.Year),
        HolidayKind.New_Years_Day => new DateTime(now.Year, 1, 1),
        HolidayKind.Spring_Festival => LunarDate(1, 1, now.Year),
        HolidayKind.Christmas => new DateTime(now.Year, 12, 25),
        HolidayKind.Qixi_Festival => LunarDate(7, 7, now.Year),
        _ => null,
    };

    /// <summary>
    /// 按月和日判断是否为玩家生日
    /// </summary>
    public static bool IsBirthday(DateTime birthday, DateTime now)
        => now.Month == birthday.Month && now.Day == birthday.Day;

    /// <summary>
    /// 判断统计值是否达到要求
    /// </summary>
    public static bool StatSatisfied(int statValue, int required) => statValue >= required;

    /// <summary>
    /// 判断照片是否应自动解锁
    /// </summary>
    /// <param name="isUnlock">照片是否已解锁</param>
    /// <param name="sellBoth">达成条件后是否仍需购买</param>
    /// <param name="conditionMet">是否满足解锁条件</param>
    /// <remarks>需购买的照片由玩家在图库中解锁。</remarks>
    public static bool ShouldAutoUnlock(bool isUnlock, bool sellBoth, bool conditionMet)
        => !isUnlock && !sellBoth && conditionMet;
}
