using System;
using System.Collections.Generic;
using System.IO;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 按资源名查找 MOD 图片和文件的路径
/// </summary>
/// <remarks>资源键沿用 Windows 版 CoreMOD 和 Resources 的命名规则。MOD 可以用相同的键覆盖已有资源。
/// <code>
///   image/food/可乐.png     ->  food_可乐
///   image/food.png          ->  food
///   image/work/A/b.png      ->  work_a_b
///   file/pack/x.zip         ->  pack_x.zip
/// </code>
/// 图片键省略扩展名，文件键保留扩展名。索引保存路径，位图由 WPF 或 Avalonia 宿主解码。</remarks>
public class ResourceIndex
{
    private readonly Dictionary<string, string> sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 名字到路径的全部映射
    /// </summary>
    public IReadOnlyDictionary<string, string> Sources => sources;

    /// <summary>
    /// 递归收录一个目录下的图片
    /// </summary>
    /// <param name="directory">目录</param>
    /// <param name="prefix">键的前缀</param>
    /// <param name="settings">按遍历顺序接收 set_*.lps 的路径，由宿主解析设置</param>
    /// <remarks>图片名去掉 .png 并转为小写，各层目录用下划线连接。先收录本层图片，再处理子目录，最后读取本层 LPS 声明。后读到的同名键覆盖先前的值。</remarks>
    public void AddImages(DirectoryInfo directory, string prefix = "", List<string>? settings = null)
    {
        if (!directory.Exists)
            return;
        foreach (var file in directory.EnumerateFiles("*.png"))
        {
            sources[prefix + Path.GetFileNameWithoutExtension(file.Name).ToLowerInvariant()] = file.FullName;
        }
        foreach (var sub in directory.EnumerateDirectories())
        {
            AddImages(sub, prefix + sub.Name.ToLowerInvariant() + "_", settings);
        }
        foreach (var file in directory.EnumerateFiles("*.lps"))
        {
            // set_ 文件保存图片锚点等设置，由宿主单独读取。
            if (file.Name.StartsWith("set_", StringComparison.OrdinalIgnoreCase))
                settings?.Add(file.FullName);
            else
                AddDeclarations(new LinePutScript.LpsDocument(File.ReadAllText(file.FullName)), directory.FullName);
        }
    }

    /// <summary>
    /// 从 lps 声明里收录资源
    /// </summary>
    /// <param name="lps">声明表, 每行的 source 子项是相对 baseDirectory 的路径</param>
    /// <param name="baseDirectory">相对路径的基准目录</param>
    /// <remarks>只在写入索引时拼接路径，原声明中的相对路径可重复使用。</remarks>
    public void AddDeclarations(LinePutScript.ILPS lps, string baseDirectory = "")
    {
        foreach (var line in lps)
        {
            var source = line.Find("source");
            if (source == null)
                continue;
            sources[line.Name.ToLowerInvariant()] = string.IsNullOrEmpty(baseDirectory)
                ? source.Info
                : Path.Combine(baseDirectory, source.Info);
        }
    }

    /// <summary>
    /// 递归收录一个目录下的文件
    /// </summary>
    /// <param name="directory">目录</param>
    /// <param name="prefix">键的前缀</param>
    /// <remarks>文件键保留扩展名，图库可分别查找同名的 .zlps 和 .zip 文件。</remarks>
    public void AddFiles(DirectoryInfo directory, string prefix = "")
    {
        if (!directory.Exists)
            return;
        foreach (var file in directory.EnumerateFiles())
        {
            sources[prefix + file.Name.ToLowerInvariant()] = file.FullName;
        }
        foreach (var sub in directory.EnumerateDirectories())
        {
            AddFiles(sub, prefix + sub.Name.ToLowerInvariant() + "_");
        }
    }

    /// <summary>
    /// 直接登记一条映射
    /// </summary>
    public void Add(string name, string path) => sources[name.ToLowerInvariant()] = path;

    /// <summary>
    /// 查找资源路径
    /// </summary>
    /// <param name="name">资源名称</param>
    /// <param name="superior">找不到时退回的上级资源名</param>
    /// <returns>找不到返回 null</returns>
    /// <remarks>例如，食物缺少专属图片时，可传入 food 查找通用图片。</remarks>
    public string? Find(string name, string? superior = null)
    {
        if (sources.TryGetValue(name, out var path))
            return path;
        if (superior != null && sources.TryGetValue(superior, out path))
            return path;
        return null;
    }
}
