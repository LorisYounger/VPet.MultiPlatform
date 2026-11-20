using LinePutScript;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 主题配色的解析与派生
/// </summary>
/// <remarks>主题 LPS 定义基础色。此处按固定透明度生成半透明变体，返回 ARGB 分量，由 WPF 或 Avalonia 宿主创建画刷。</remarks>
public static class ThemeRules
{
    /// <summary>
    /// 阴影颜色的键名
    /// </summary>
    /// <remarks>该资源以颜色值存入资源字典，其余配色用于创建画刷。</remarks>
    public const string ShadowColorKey = "ShadowColor";

    /// <summary>
    /// 要派生半透明变体的基础色
    /// </summary>
    public static readonly IReadOnlyList<string> DerivedBases = new[]
    {
        "Primary", "Secondary", "DARKPrimary",
    };

    /// <summary>
    /// 半透明变体的键名后缀与 Alpha 值
    /// </summary>
    /// <remarks>现有 MOD 主题依赖 Windows 版 MainWindow.LoadTheme 的这组透明度。</remarks>
    public static readonly IReadOnlyList<(string Suffix, byte Alpha)> DerivedAlphas = new[]
    {
        ("Trans", (byte)204),
        ("Trans4", (byte)44),
        ("TransA", (byte)170),
        ("TransE", (byte)238),
    };

    /// <summary>
    /// 主题资源的键名和 ARGB 分量
    /// </summary>
    public readonly struct ThemeColor
    {
        /// <summary>资源键名</summary>
        public string Key { get; }
        public byte A { get; }
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        /// <summary>是否以颜色值存入资源字典</summary>
        public bool IsRawColor { get; }

        public ThemeColor(string key, byte a, byte r, byte g, byte b, bool isRawColor)
        {
            Key = key;
            A = a;
            R = r;
            G = g;
            B = b;
            IsRawColor = isRawColor;
        }
    }

    /// <summary>
    /// 将主题配色展开为资源字典条目
    /// </summary>
    /// <param name="themeColor">主题的配色部分(已经去掉了首行)</param>
    /// <remarks>依次返回阴影色、名称不含 Color 的基础色和派生的半透明变体。</remarks>
    public static IEnumerable<ThemeColor> Expand(ILPS themeColor)
    {
        var shadow = themeColor.FindLine(ShadowColorKey)?.Info
            ?? themeColor.FindSub(ShadowColorKey)?.Info;
        if (shadow != null && TryParse(shadow, out var sc))
            yield return new ThemeColor(ShadowColorKey, sc.A, sc.R, sc.G, sc.B, true);

        foreach (var line in themeColor)
        {
            if (line.Name.Contains("Color"))
                continue;
            if (!TryParse(line.Info, out var c))
                continue;
            yield return new ThemeColor(line.Name, c.A, c.R, c.G, c.B, false);
        }

        foreach (var name in DerivedBases)
        {
            var baseColor = themeColor.FindLine(name)?.Info;
            if (baseColor == null || !TryParse(baseColor, out var b))
                continue;
            foreach (var (suffix, alpha) in DerivedAlphas)
                yield return new ThemeColor(name + suffix, alpha, b.R, b.G, b.B, false);
        }
    }

    /// <summary>
    /// 解析主题里写的颜色
    /// </summary>
    /// <param name="text">六位 RGB 或八位 ARGB 十六进制字符串，可带 # 前缀</param>
    /// <remarks>主题 LPS 常用不带 # 的六位 RGB。</remarks>
    public static bool TryParse(string? text, out (byte A, byte R, byte G, byte B) color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        var hex = text.Trim().TrimStart('#');
        if (hex.Length == 6)
        {
            if (!TryByte(hex, 0, out var r) || !TryByte(hex, 2, out var g) || !TryByte(hex, 4, out var b))
                return false;
            color = (255, r, g, b);
            return true;
        }
        if (hex.Length == 8)
        {
            if (!TryByte(hex, 0, out var a) || !TryByte(hex, 2, out var r)
                || !TryByte(hex, 4, out var g) || !TryByte(hex, 6, out var b))
                return false;
            color = (a, r, g, b);
            return true;
        }
        return false;
    }

    private static bool TryByte(string hex, int index, out byte value)
        => byte.TryParse(hex.AsSpan(index, 2), System.Globalization.NumberStyles.HexNumber,
            System.Globalization.CultureInfo.InvariantCulture, out value);
}
