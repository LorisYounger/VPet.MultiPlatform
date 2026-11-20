using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 按程序集引用识别出的插件类型
/// </summary>
public enum PluginKind
{
    /// <summary>MOD 的依赖库</summary>
    Library,
    /// <summary>引用 Windows 专用程序集的插件</summary>
    LegacyWindows,
    /// <summary>引用统一契约的插件</summary>
    Unified,
}

/// <summary>
/// 通过程序集元数据识别插件类型
/// </summary>
/// <remarks>跨平台宿主据此跳过 Windows 专用插件并提示玩家。分类时只读取 PE 元数据中的程序集引用表，无需加载程序集或执行模块初始化器。</remarks>
public static class PluginClassifier
{
    /// <summary>
    /// 统一契约的程序集名
    /// </summary>
    public const string UnifiedInterfaceName = "VPet-Simulator.Unified";

    /// <summary>
    /// 用于识别 Windows 专用插件的程序集引用
    /// </summary>
    private static readonly string[] WindowsOnlyReferences =
    {
        "VPet-Simulator.Windows.Interface",
        "VPet-Simulator.Core",
        "PresentationFramework",
        "PresentationCore",
        "WindowsBase",
        "Panuon.WPF",
        "Panuon.WPF.UI",
    };

    /// <summary>
    /// 读取 DLL 的程序集引用并分类
    /// </summary>
    /// <param name="dllPath">dll 路径</param>
    /// <returns>缺少托管元数据或读取失败时按依赖库处理</returns>
    public static PluginKind Classify(string dllPath)
    {
        try
        {
            using var stream = File.OpenRead(dllPath);
            using var reader = new PEReader(stream);
            if (!reader.HasMetadata)
                return PluginKind.Library;
            var metadata = reader.GetMetadataReader();

            bool unified = false;
            foreach (var handle in metadata.AssemblyReferences)
            {
                var name = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
                if (WindowsOnlyReferences.Contains(name, StringComparer.OrdinalIgnoreCase))
                    return PluginKind.LegacyWindows;
                if (string.Equals(name, UnifiedInterfaceName, StringComparison.OrdinalIgnoreCase))
                    unified = true;
            }
            return unified ? PluginKind.Unified : PluginKind.Library;
        }
        catch (Exception)
        {
            return PluginKind.Library;
        }
    }

    /// <summary>
    /// 根据进程架构和 load.lps 判断是否跳过 DLL
    /// </summary>
    /// <param name="fileName">dll 文件名</param>
    /// <param name="rules">load.lps 的内容, 没有就传 null</param>
    /// <returns>跳过时返回 true</returns>
    /// <remarks>先检查文件名中的 x86 或 x64，再读取 load.lps 中对应文件的 skip 和 cpu 设置。</remarks>
    public static bool ShouldSkip(string fileName, LinePutScript.ILPS? rules)
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        bool is64 = arch == Architecture.X64 || arch == Architecture.Arm64;
        // 文件名中的 x86 和 x64 标记区分大小写，与 Windows 版 CoreMOD 一致。
        if (is64 && fileName.Contains("x86", StringComparison.Ordinal))
            return true;
        if (!is64 && fileName.Contains("x64", StringComparison.Ordinal))
            return true;

        var line = rules?.FindLine(fileName);
        if (line == null)
            return false;
        if (line.GetBool("skip"))
            return true;
        var cpu = line.GetString("cpu", "anycpu")!.ToLowerInvariant();
        if (cpu == "anycpu")
            return false;
        return cpu != (is64 ? "x64" : "x86");
    }
}
