using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPet_Simulator.MutiPlatform;

/// <summary>
/// 同一进程中的桌宠按前缀区分设置和存档，文件名与 Windows 版通用。
/// </summary>
/// <remarks>默认桌宠使用 Setting.lps 和 Saves/Save_100001.lps；名为“小黑”的桌宠使用
/// Setting-小黑.lps 和 Saves/Save-小黑_100001.lps。连字符属于前缀。</remarks>
public static class MultiPetStore
{
    /// <summary>
    /// 设置文件名的前半段
    /// </summary>
    public const string SettingPrefix = "Setting";

    /// <summary>
    /// 设置文件的扩展名
    /// </summary>
    public const string SettingExtension = ".lps";

    /// <summary>
    /// 将桌宠名称转换为存档前缀。
    /// </summary>
    /// <returns>默认桌宠使用空前缀，其他名称以连字符开头。</returns>
    public static string Normalize(string? name)
    {
        var trimmed = name?.Trim().Trim('-') ?? string.Empty;
        return string.IsNullOrEmpty(trimmed) ? string.Empty : "-" + trimmed;
    }

    /// <summary>
    /// 把前缀变回给玩家看的名字
    /// </summary>
    public static string DisplayName(string prefix)
        => string.IsNullOrEmpty(prefix) ? string.Empty : prefix.TrimStart('-');

    /// <summary>
    /// 某个前缀对应的设置文件路径
    /// </summary>
    public static string SettingPath(string dataRoot, string prefix)
        => Path.Combine(dataRoot, SettingPrefix + prefix + SettingExtension);

    /// <summary>
    /// 列出数据目录中的桌宠前缀。
    /// </summary>
    /// <returns>默认桌宠的空前缀始终排在第一位。</returns>
    public static List<string> List(string dataRoot)
    {
        var result = new List<string> { string.Empty };
        if (Directory.Exists(dataRoot))
        {
            foreach (var file in new DirectoryInfo(dataRoot).GetFiles(SettingPrefix + "*" + SettingExtension))
            {
                var name = Path.GetFileNameWithoutExtension(file.Name);
                // "Setting" 之后的部分就是前缀
                var prefix = name.Length > SettingPrefix.Length
                    ? name.Substring(SettingPrefix.Length)
                    : string.Empty;
                // 旧版本多开留下过一个空名字的 Setting-.lps, 它不是一只真的桌宠
                if (prefix == "-")
                    continue;
                if (!result.Contains(prefix, StringComparer.Ordinal))
                    result.Add(prefix);
            }
        }
        return result;
    }

    /// <summary>
    /// 从命令行参数里取出要开哪只
    /// </summary>
    /// <returns>没指定时返回 null</returns>
    /// 认两种写法: `--prefix 小黑` 和 Windows 版那种 `prefix#小黑:|`
    public static string? ReadPrefixArgument(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--prefix" && i + 1 < args.Length)
                return Normalize(args[i + 1]);
            if (args[i].StartsWith("prefix#", StringComparison.Ordinal))
                return Normalize(args[i].Substring(7).TrimEnd('|').TrimEnd(':'));
        }
        return null;
    }

    /// <summary>
    /// 数据目录里有没有 startup_ 标记指定默认开哪只
    /// </summary>
    /// <remarks>文件名由 startup_ 和桌宠名称组成。</remarks>
    public static string? ReadStartupMarker(string dataRoot)
    {
        if (!Directory.Exists(dataRoot))
            return null;
        var file = new DirectoryInfo(dataRoot).EnumerateFiles("startup_*").FirstOrDefault();
        return file == null ? null : Normalize(file.Name.Substring(8));
    }

    /// <summary>
    /// 写入默认桌宠的启动标记
    /// </summary>
    /// <param name="prefix">空前缀表示不指定</param>
    public static void WriteStartupMarker(string dataRoot, string prefix)
    {
        if (!Directory.Exists(dataRoot))
            return;
        foreach (var old in new DirectoryInfo(dataRoot).GetFiles("startup_*"))
        {
            try { old.Delete(); }
            catch (IOException) { }
        }
        if (string.IsNullOrEmpty(prefix))
            return;
        try { File.WriteAllText(Path.Combine(dataRoot, "startup_" + DisplayName(prefix)), string.Empty); }
        catch (IOException) { }
    }
}
