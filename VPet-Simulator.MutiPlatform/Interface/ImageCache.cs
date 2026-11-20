using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;

namespace VPet_Simulator.Windows.Interface;

/// <summary>
/// 按路径缓存解码好的位图
/// </summary>
/// <remarks>首次使用图片时解码。失败时通知 OnError 并缓存 null，动画循环可跳过损坏的图片。</remarks>
public class ImageCache : IDisposable
{
    private readonly Dictionary<string, Bitmap?> cache
        = new Dictionary<string, Bitmap?>(StringComparer.OrdinalIgnoreCase);
    private readonly object gate = new object();

    /// <summary>
    /// 图片解码失败时的错误通知
    /// </summary>
    public Action<string>? OnError { get; set; }

    /// <summary>
    /// 已缓存的路径数，包括解码失败的路径
    /// </summary>
    public int Count
    {
        get { lock (gate) return cache.Count; }
    }

    /// <summary>
    /// 取一张图, 取不到返回 null
    /// </summary>
    /// <param name="path">图片路径, null 直接返回 null</param>
    public Bitmap? Get(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        lock (gate)
        {
            if (cache.TryGetValue(path, out var cached))
                return cached;
        }

        Bitmap? bitmap = null;
        try
        {
            // avares:// 是程序集里的资源 (占位图), 其余是磁盘路径
            if (path.StartsWith("avares://", StringComparison.OrdinalIgnoreCase))
                bitmap = new Bitmap(AssetLoader.Open(new Uri(path)));
            else
                bitmap = new Bitmap(path);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"图片加载失败 {path}: {ex.Message}");
        }

        lock (gate)
        {
            // 其他线程已缓存该路径时，释放本次解码的位图。
            if (cache.TryGetValue(path, out var raced))
            {
                bitmap?.Dispose();
                return raced;
            }
            cache[path] = bitmap;
        }
        return bitmap;
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void Clear()
    {
        List<Bitmap?> old;
        lock (gate)
        {
            old = new List<Bitmap?>(cache.Values);
            cache.Clear();
        }
        foreach (var bitmap in old)
            bitmap?.Dispose();
    }

    /// <summary>
    /// 按 URI 取一张图: file:// 转成路径, avares:// 原样
    /// </summary>
    public Bitmap? Get(Uri? uri)
    {
        if (uri == null)
            return null;
        return Get(uri.IsFile ? uri.LocalPath : uri.OriginalString);
    }

    public void Dispose() => Clear();
}
