using LinePutScript;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 存档文件的命名、轮换与索引
/// </summary>
/// <remarks>沿用 Windows 版的存档命名规则。
/// <code>
///   存档   Saves/Save{前缀}_{递增编号}.lps
///   备份   Saves_BKP/Save{前缀}_{内容哈希 % 255 的十六进制}.lps
/// </code>
/// 同一多开前缀最多使用 255 个备份文件名，相同哈希值会覆盖同一份备份。轮换仅清理存档目录。</remarks>
public static class SaveCatalog
{
    /// <summary>
    /// 存档起始编号，与 Windows 版 Setting.SaveTimes 默认值一致
    /// </summary>
    public const int FirstSaveTimes = 100000;

    /// <summary>
    /// 存档目录名
    /// </summary>
    public const string SaveFolderName = "Saves";

    /// <summary>
    /// 备份目录名
    /// </summary>
    public const string BackupFolderName = "Saves_BKP";

    /// <summary>
    /// 存档目录
    /// </summary>
    public static string SaveDirectory(string rootDirectory)
        => System.IO.Path.Combine(rootDirectory, SaveFolderName);

    /// <summary>
    /// 备份目录
    /// </summary>
    public static string BackupDirectory(string rootDirectory)
        => System.IO.Path.Combine(rootDirectory, BackupFolderName);

    /// <summary>
    /// 一份存档文件
    /// </summary>
    public readonly struct SaveFile
    {
        /// <summary>文件完整路径</summary>
        public string Path { get; }
        /// <summary>文件名末尾的十进制编号。无法解析的后缀（如含字母的备份哈希）记为 0。</summary>
        public int Number { get; }
        /// <summary>最后写入时间</summary>
        public DateTime Time { get; }
        /// <summary>是否来自备份目录</summary>
        public bool IsBackup { get; }

        public SaveFile(string path, int number, DateTime time, bool isBackup)
        {
            Path = path;
            Number = number;
            Time = time;
            IsBackup = isBackup;
        }
    }

    /// <summary>
    /// 从文件名里取出编号
    /// </summary>
    /// <remarks>解析最后一个下划线与扩展名之间的十进制数字。解析失败返回 0，轮换时这些文件排在正编号之前。</remarks>
    public static int ParseNumber(string path)
    {
        var name = System.IO.Path.GetFileNameWithoutExtension(path);
        var tail = name.Split('_').Last();
        return int.TryParse(tail, out int i) ? i : 0;
    }

    /// <summary>
    /// 列出存档目录里的存档, 按编号从小到大
    /// </summary>
    public static List<SaveFile> List(string saveDirectory, string prefix)
        => Enumerate(saveDirectory, prefix, false).OrderBy(x => x.Number).ToList();

    /// <summary>
    /// 列出存档和备份目录里的全部存档, 按写入时间从新到旧
    /// </summary>
    public static List<SaveFile> ListAll(string saveDirectory, string backupDirectory, string prefix)
        => Enumerate(saveDirectory, prefix, false)
            .Concat(Enumerate(backupDirectory, prefix, true))
            .OrderByDescending(x => x.Time).ToList();

    /// <summary>
    /// 列出游戏根目录下两个存档目录里的全部存档, 按写入时间从新到旧
    /// </summary>
    public static List<SaveFile> ListAll(string rootDirectory, string prefix)
        => ListAll(SaveDirectory(rootDirectory), BackupDirectory(rootDirectory), prefix);

    private static IEnumerable<SaveFile> Enumerate(string directory, string prefix, bool isBackup)
    {
        if (!Directory.Exists(directory))
            yield break;
        foreach (var path in Directory.GetFiles(directory, $"Save{prefix}_*.lps"))
        {
            yield return new SaveFile(path, ParseNumber(path), File.GetLastWriteTime(path), isBackup);
        }
    }

    /// <summary>
    /// 取当前该用的存档编号
    /// </summary>
    /// <remarks>取设置值和目录中最大编号的较大值，避免从其他机器复制存档后沿用较小的编号。</remarks>
    public static int SyncSaveTimes(string saveDirectory, string prefix, int settingSaveTimes)
    {
        var files = List(saveDirectory, prefix);
        if (files.Count == 0)
            return settingSaveTimes;
        int last = files[files.Count - 1].Number;
        return settingSaveTimes < last ? last : settingSaveTimes;
    }

    /// <summary>
    /// 按编号从新到旧列出候选存档
    /// </summary>
    /// <remarks>调用方依次读档，采用第一份能解析的存档。</remarks>
    public static List<string> LoadCandidates(string saveDirectory, string prefix)
        => List(saveDirectory, prefix).Select(x => x.Path).Reverse().ToList();

    /// <summary>
    /// 写入存档
    /// </summary>
    /// <param name="lps">存档文档</param>
    /// <param name="saveDirectory">存档目录</param>
    /// <param name="backupDirectory">备份目录</param>
    /// <param name="prefix">多开前缀</param>
    /// <param name="saveTimes">本次使用的编号, 调用方负责递增并存回设置</param>
    /// <param name="backupMaxNum">写入前轮换时保留的旧存档数量上限</param>
    /// <returns>写出的存档文件路径</returns>
    /// <remarks>备份文件名使用 LPS 文档的 GetHashCode。LPS_D 的哈希可跨进程复用，而字符串的 GetHashCode 随进程变化。</remarks>
    public static string Write(ILPS lps, string saveDirectory, string backupDirectory,
        string prefix, int saveTimes, int backupMaxNum)
    {
        var content = lps.ToString();
        if (content == null)
            throw new ArgumentException("存档内容为空", nameof(lps));

        Directory.CreateDirectory(saveDirectory);
        Directory.CreateDirectory(backupDirectory);

        // 写入新存档前，先按编号从小到大清理超出限额的旧存档。
        var files = List(saveDirectory, prefix);
        while (files.Count > backupMaxNum)
        {
            File.Delete(files[0].Path);
            files.RemoveAt(0);
        }

        var savePath = System.IO.Path.Combine(saveDirectory, $"Save{prefix}_{saveTimes}.lps");
        // 内容哈希相同的备份使用同一文件名。
        int hash = Math.Abs(lps.GetHashCode() % 255);
        var backupPath = System.IO.Path.Combine(backupDirectory, $"Save{prefix}_{hash:X}.lps");

        WriteAtomic(savePath, content);
        WriteAtomic(backupPath, content);
        return savePath;
    }

    /// <summary>
    /// 写完临时文件后替换目标存档
    /// </summary>
    /// <remarks>写入临时文件时若程序中断，目标存档仍保留原内容。</remarks>
    private static void WriteAtomic(string path, string content)
    {
        var temp = path + ".tmp";
        File.WriteAllText(temp, content);
        File.Move(temp, path, true);
    }

    /// <summary>
    /// Steam 云存档的路径前缀
    /// </summary>
    public const string CloudPrefix = "VPetCloud/";

    /// <summary>
    /// Steam 云存档的文件名
    /// </summary>
    /// <remarks>文件名后缀沿用 Windows 格式，取时间的 Ticks 除以 60000 后的十六进制值。</remarks>
    public static string CloudName(string prefix, DateTime time)
        => $"{CloudPrefix}Save{prefix}_{time.Ticks / 60000:X}.lps";

    /// <summary>
    /// 判断云存档路径是否匹配指定前缀
    /// </summary>
    public static bool IsCloudName(string name, string prefix)
        => name.StartsWith($"{CloudPrefix}Save{prefix}_", StringComparison.Ordinal)
            && name.EndsWith(".lps", StringComparison.Ordinal);

    /// <summary>
    /// 解析云存档名中的时间，解析失败时返回 DateTime.MinValue
    /// </summary>
    /// <remarks>时间后缀按十六进制读取，与 CloudName 的编码一致，供云存档按时间排序。</remarks>
    public static DateTime ParseCloudTime(string name)
    {
        var tail = System.IO.Path.GetFileNameWithoutExtension(name).Split('_').Last();
        if (!long.TryParse(tail, System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture, out long minutes))
            return DateTime.MinValue;
        long ticks = minutes * 60000;
        if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks)
            return DateTime.MinValue;
        return new DateTime(ticks);
    }

    /// <summary>
    /// 按前缀筛选云存档，并从旧到新排列
    /// </summary>
    public static List<string> ListCloud(IEnumerable<string> names, string prefix)
        => names.Where(x => IsCloudName(x, prefix)).OrderBy(ParseCloudTime).ToList();
}
