using LinePutScript;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// MOD 启用状态、代码放行记录和首次提示
/// </summary>
/// <remarks>沿用 Windows 版 Setting.lps 的 onmod、passmod 和 msgmod 行，两端可读取同一份设置。onmod 和 passmod 的子项名使用小写。</remarks>
public static class ModSwitchStore
{
    /// <summary>
    /// 始终启用的 MOD
    /// </summary>
    public static readonly IReadOnlyList<string> AlwaysOn = new[] { "Core", "PCat" };

    /// <summary>
    /// 这个 MOD 是否启用
    /// </summary>
    public static bool IsOn(ILPS settings, string modName)
    {
        foreach (var name in AlwaysOn)
        {
            if (name == modName)
                return true;
        }
        var line = settings.FindLine("onmod");
        if (line == null)
            return false;
        return line.Find(modName.ToLowerInvariant()) != null;
    }

    /// <summary>
    /// 启用这个 MOD
    /// </summary>
    public static void On(ILPS settings, string modName)
    {
        if (string.IsNullOrWhiteSpace(modName))
            return;
        settings.FindorAddLine("onmod").AddorReplaceSub(new Sub(modName.ToLowerInvariant()));
    }

    /// <summary>
    /// 停用这个 MOD
    /// </summary>
    public static void Off(ILPS settings, string modName)
    {
        settings.FindorAddLine("onmod").Remove(modName.ToLowerInvariant());
    }

    /// <summary>
    /// 玩家是否已经放行这个 MOD 的代码插件
    /// </summary>
    public static bool IsPassed(ILPS settings, string modName)
    {
        var line = settings.FindLine("passmod");
        if (line == null)
            return false;
        return line.Find(modName.ToLowerInvariant()) != null;
    }

    /// <summary>
    /// 放行这个 MOD 的代码插件
    /// </summary>
    public static void Pass(ILPS settings, string modName)
    {
        settings.FindorAddLine("passmod").AddorReplaceSub(new Sub(modName.ToLowerInvariant()));
    }

    /// <summary>
    /// 收回对这个 MOD 代码插件的放行
    /// </summary>
    public static void PassRemove(ILPS settings, string modName)
    {
        settings.FindorAddLine("passmod").Remove(modName.ToLowerInvariant());
    }

    /// <summary>
    /// 判断是否需要首次提示玩家此 MOD 包含代码插件，并记录提示状态
    /// </summary>
    /// <returns>首次调用返回 true，再次调用返回 false</returns>
    public static bool ShouldNotice(ILPS settings, string modName)
    {
        var line = settings.FindorAddLine("msgmod");
        if (line.GetBool(modName))
            return false;
        line.SetBool(modName, true);
        return true;
    }

    /// <summary>
    /// 记录放行时的插件 DLL 指纹
    /// </summary>
    /// <remarks>跨平台宿主用该指纹识别插件更新，以便在 DLL 变化后重新询问玩家。</remarks>
    public static void SetPassHash(ILPS settings, string modName, string hash)
    {
        settings.FindorAddLine("passmodhash").AddorReplaceSub(new Sub(modName.ToLowerInvariant(), hash));
    }

    /// <summary>
    /// 读取上次放行时的指纹，缺少记录时返回 null
    /// </summary>
    public static string? GetPassHash(ILPS settings, string modName)
    {
        return settings.FindLine("passmodhash")?.Find(modName.ToLowerInvariant())?.Info;
    }
}
