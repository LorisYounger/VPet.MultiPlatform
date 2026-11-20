using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 从 MOD 的 info.lps 及子目录读取的元数据
/// </summary>
public class ModMetadata
{
    /// <summary>MOD 名称，同时用作主键</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>简介</summary>
    public string Intro { get; set; } = string.Empty;
    /// <summary>作者</summary>
    public string Author { get; set; } = string.Empty;
    /// <summary>作者的 Steam 号</summary>
    public long AuthorID { get; set; }
    /// <summary>创意工坊物品号</summary>
    public ulong ItemID { get; set; }
    /// <summary>要求的游戏版本</summary>
    public int GameVer { get; set; }
    /// <summary>MOD 自己的版本</summary>
    public int Ver { get; set; }
    /// <summary>缓存失效日期</summary>
    public DateTime CacheDate { get; set; }
    /// <summary>要跳过加载的 dll 文件名</summary>
    public List<string> DllSkip { get; } = new List<string>();
    /// <summary>MOD 名称和简介的翻译，停用时仍可显示</summary>
    public List<(string Culture, ILine Line)> PreTranslations { get; } = new List<(string, ILine)>();
    /// <summary>MOD 目录</summary>
    public DirectoryInfo? Path { get; set; }
    /// <summary>MOD 提供的内容类型，对应子目录名</summary>
    public HashSet<string> ContentTags { get; } = new HashSet<string>();
    /// <summary>解析过程中遇到的问题</summary>
    public List<string> Warnings { get; } = new List<string>();
}

/// <summary>
/// 读取 MOD 的 info.lps 并识别内容目录
/// </summary>
/// <remarks>字段名和取值方式沿用 Windows 版 CoreMOD 的格式。</remarks>
public static class ModInfoReader
{
    /// <summary>
    /// 解析一个 MOD 目录
    /// </summary>
    /// <returns>目录中缺少 info.lps 时返回 null</returns>
    public static ModMetadata? Parse(DirectoryInfo directory)
    {
        var infoPath = System.IO.Path.Combine(directory.FullName, "info.lps");
        if (!File.Exists(infoPath))
            return null;

        var mod = new ModMetadata { Path = directory };
        try
        {
            var info = new LpsDocument(File.ReadAllText(infoPath));
            mod.Name = info.FindLine("vupmod")?.Info ?? string.Empty;
            mod.Intro = info.FindLine("intro")?.Info ?? string.Empty;
            mod.GameVer = info.FindSub("gamever")?.InfoToInt ?? 0;
            mod.Ver = info.FindSub("ver")?.InfoToInt ?? 0;

            // 作者名后的方括号标记不计入姓名。
            var author = info.FindSub("author")?.Info ?? string.Empty;
            int bracket = author.IndexOf('[');
            mod.Author = bracket >= 0 ? author.Substring(0, bracket) : author;

            // authorid 和 itemid 各占一行，gamever、ver 和 author 则是子项。
            var authorId = info.FindLine("authorid");
            if (authorId != null)
                mod.AuthorID = authorId.InfoToInt64;
            var itemId = info.FindLine("itemid");
            if (itemId != null && ulong.TryParse(itemId.Info, out var id))
                mod.ItemID = id;

            mod.CacheDate = info.GetDateTime("cachedate", DateTime.MinValue);
            // 忽略将来的清理日期，避免缓存持续失效。
            if (mod.CacheDate > DateTime.Now)
                mod.CacheDate = DateTime.MinValue;

            foreach (var skip in info["dllskip"])
                mod.DllSkip.Add(skip.Name);

            // 名称和简介的翻译在 MOD 停用时也要保留。
            foreach (var line in info.FindAllLine("lang"))
                mod.PreTranslations.Add((line.Info, line));
        }
        catch (Exception ex)
        {
            mod.Warnings.Add($"无法解析 info.lps（{ex.Message}）");
            return mod;
        }

        foreach (var sub in directory.EnumerateDirectories())
            mod.ContentTags.Add(sub.Name.ToLowerInvariant());

        return mod;
    }

    /// <summary>
    /// 扫描一组 MOD 根目录
    /// </summary>
    /// <param name="roots">MOD 根目录, 按优先级排列</param>
    /// <returns>同名目录只扫描优先级最高的一份。不同目录声明同一 MOD 名称时，后出现的条目带有重名提示。</returns>
    public static List<ModMetadata> ScanAll(IEnumerable<string> roots)
    {
        var result = new List<ModMetadata>();
        var seenDirectory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenName = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                continue;
            foreach (var directory in new DirectoryInfo(root).EnumerateDirectories()
                .OrderBy(d => d.Name, StringComparer.Ordinal))
            {
                // 安装目录和用户数据目录中可能包含同名目录。
                if (!seenDirectory.Add(directory.Name))
                    continue;
                var mod = Parse(directory);
                if (mod == null)
                    continue;
                if (!string.IsNullOrEmpty(mod.Name) && !seenName.Add(mod.Name))
                    mod.Warnings.Add("MOD 名称重复");
                result.Add(mod);
            }
        }
        return result;
    }
}
