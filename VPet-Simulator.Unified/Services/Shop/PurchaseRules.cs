using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 商店赊账、私房钱、自动购买和每日礼包的规则
/// </summary>
/// <remarks>规则取自 Windows 版 winBetterBuy、MainWindow.lowStrength 和 everydaygift。宿主负责选择具体食物并显示提示。</remarks>
public static class PurchaseRules
{
    /// <summary>
    /// 赊账上限。价格和经验均低于此值时可赊账。
    /// </summary>
    public const double CreditLimit = 1000;

    /// <summary>
    /// 私房钱数额
    /// </summary>
    public const double LoanAmount = 1000;

    /// <summary>
    /// 余额降至此值或更低时，桌宠拿出私房钱
    /// </summary>
    public const double LoanGiveMoney = 1;

    /// <summary>
    /// 余额达到此值时，桌宠收回已给出的私房钱
    /// </summary>
    public const double LoanTakeBackMoney = 11000;

    /// <summary>
    /// 自动购买要求的最低余额
    /// </summary>
    public const double AutoBuyMinMoney = 100;

    /// <summary>
    /// 自动购买预算占当前余额的比例
    /// </summary>
    public const double AutoBuyBudgetRate = 0.8;

    /// <summary>
    /// 自动购买的扣款倍率，含跑腿费
    /// </summary>
    public const double AutoBuyPriceRate = 1.2;

    /// <summary>
    /// 自动购买正餐或饮料时，饱腹度或口渴度相对于体力上限的触发比例
    /// </summary>
    public const double LowRate = 0.70;

    /// <summary>
    /// 私房钱处理方式
    /// </summary>
    public enum LoanAction
    {
        /// <summary>无需处理</summary>
        None,
        /// <summary>拿私房钱出来给玩家</summary>
        Give,
        /// <summary>把私房钱偷偷收回去</summary>
        TakeBack,
        /// <summary>已给过私房钱，提醒玩家可以赊账</summary>
        RemindCredit,
    }

    /// <summary>
    /// 根据进店时的余额和借款状态决定如何处理私房钱
    /// </summary>
    /// <param name="money">当前金钱</param>
    /// <param name="alreadyLoaned">存档中的 self 标记，表示桌宠是否已给出私房钱</param>
    public static LoanAction CheckLoan(double money, bool alreadyLoaned)
    {
        if (money <= LoanGiveMoney)
            return alreadyLoaned ? LoanAction.RemindCredit : LoanAction.Give;
        if (money >= LoanTakeBackMoney && alreadyLoaned)
            return LoanAction.TakeBack;
        return LoanAction.None;
    }

    /// <summary>
    /// 判断余额及赊账规则是否允许购买
    /// </summary>
    /// <param name="price">价格</param>
    /// <param name="exp">经验值</param>
    /// <param name="money">当前金钱</param>
    /// <remarks>价格或经验达到赊账上限时，余额必须高于标价。</remarks>
    public static bool CanAfford(double price, double exp, double money)
        => !((price >= CreditLimit || exp >= CreditLimit) && price >= money);

    /// <summary>
    /// 自动购买的物品类别
    /// </summary>
    public enum AutoBuyNeed
    {
        /// <summary>无需购买</summary>
        None,
        /// <summary>饱腹度低，购买正餐</summary>
        Meal,
        /// <summary>口渴度低，购买饮料</summary>
        Drink,
        /// <summary>心情低，购买礼物</summary>
        Gift,
        /// <summary>心情低且未启用自动买礼物，改买零食</summary>
        Snack,
    }

    /// <summary>
    /// 判断是否需要自动购买并选择类别
    /// </summary>
    /// <param name="strengthFood">饱腹(含存储)</param>
    /// <param name="strengthDrink">口渴(含存储)</param>
    /// <param name="strengthMax">体力上限</param>
    /// <param name="feeling">心情</param>
    /// <param name="feelingMax">心情上限</param>
    /// <param name="money">当前金钱</param>
    /// <param name="autoGift">是否启用自动购买礼物</param>
    /// <remarks>依次检查饱腹度、口渴度和心情，优先满足第一项需求。</remarks>
    public static AutoBuyNeed WhatToBuy(double strengthFood, double strengthDrink, double strengthMax,
        double feeling, double feelingMax, double money, bool autoGift)
    {
        if (money < AutoBuyMinMoney)
            return AutoBuyNeed.None;
        var low = strengthMax * LowRate;
        if (strengthFood < low)
            return AutoBuyNeed.Meal;
        if (strengthDrink < low)
            return AutoBuyNeed.Drink;
        if (feeling < feelingMax * 0.50)
            return autoGift ? AutoBuyNeed.Gift : AutoBuyNeed.Snack;
        return AutoBuyNeed.None;
    }

    /// <summary>
    /// 本次自动购买的预算
    /// </summary>
    public static double AutoBuyBudget(double money) => money * AutoBuyBudgetRate;

    /// <summary>
    /// 本次自动购买的实际扣款额
    /// </summary>
    /// <remarks>按标价加收两成跑腿费。</remarks>
    public static double AutoBuyCost(double price) => price * AutoBuyPriceRate;

    /// <summary>
    /// 判断食物是否符合自动购买条件
    /// </summary>
    /// <param name="price">价格</param>
    /// <param name="health">健康影响</param>
    /// <param name="exp">经验</param>
    /// <param name="likability">好感度影响</param>
    /// <param name="budget">本次预算</param>
    /// <param name="isOverLoad">是否超模</param>
    public static bool IsAutoBuyCandidate(double price, double health, double exp,
        double likability, double budget, bool isOverLoad)
        => price >= 2 && health >= -5 && exp >= -10 && likability >= 0
            && price < budget && !isOverLoad;

    /// <summary>
    /// 自动买正餐时要求的最低饱腹回复
    /// </summary>
    public static double MealThreshold(double strengthMax) => Math.Min(strengthMax * 0.20, 100);

    /// <summary>
    /// 自动买饮料时要求的最低解渴量
    /// </summary>
    public static double DrinkThreshold(double strengthMax) => Math.Min(strengthMax * 0.20, 50);

    /// <summary>
    /// 自动买礼物时要求的最低心情回复
    /// </summary>
    public static double GiftThreshold(double feelingMax) => Math.Min(feelingMax * 0.10, 50);

    /// <summary>
    /// 自动买零食时要求的最低心情回复
    /// </summary>
    public static double SnackThreshold(double feelingMax) => Math.Min(feelingMax * 0.10, 40);

    /// <summary>
    /// 每日礼包的设置行名
    /// </summary>
    public const string DailyGiftLineName = "dailydata";

    /// <summary>
    /// 每日礼包的设置子项名
    /// </summary>
    public const string DailyGiftSubName = "everydaygift";

    /// <summary>
    /// 判断今天是否可以领取每日礼包
    /// </summary>
    /// <param name="lastGiftDayOfYear">上次领取日期在当年的序号</param>
    /// <param name="now">现在</param>
    /// <remarks>存档只记录一年中的第几天。跨年后若日期序号与上次相同，当天仍无法再领。</remarks>
    public static bool ShouldGiveDailyGift(int lastGiftDayOfYear, DateTime now)
        => lastGiftDayOfYear != now.DayOfYear;
}
