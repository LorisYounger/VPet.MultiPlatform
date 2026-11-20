namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 食物的推荐价与超模判定
/// </summary>
/// <remarks>基础推荐价和超模阈值用于自动购买及投喂提醒。Windows 宿主还会在基础价格上计入 Buff 等效果。</remarks>
public static class FoodPricing
{
    /// <summary>
    /// 按食物属性计算基础推荐价
    /// </summary>
    public static double RealPrice(double exp, double strength, double strengthFood,
        double strengthDrink, double feeling, double health, double likability)
        => (exp / 3 + strength / 5 + strengthDrink / 3 + strengthFood / 2 + feeling / 6) / 3
            + health + likability * 10;

    /// <summary>
    /// 超模判定的价格比例
    /// </summary>
    public const double OverLoadRate = 0.7;

    /// <summary>
    /// 计算超模阈值时从推荐价中扣除的固定金额
    /// </summary>
    public const double OverLoadOffset = 10;

    /// <summary>
    /// 判断标价是否低于超模阈值
    /// </summary>
    /// <param name="price">标价</param>
    /// <param name="realPrice">推荐价</param>
    /// <remarks>标价低于（推荐价减去固定金额）乘以价格比例时判为超模。</remarks>
    public static bool IsOverLoad(double price, double realPrice)
        => price < (realPrice - OverLoadOffset) * OverLoadRate;
}
