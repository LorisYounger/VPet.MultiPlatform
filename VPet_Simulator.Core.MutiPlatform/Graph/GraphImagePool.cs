using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System.Collections.Generic;

namespace VPet_Simulator.Core.MutiPlatform.Graph;

/// <summary>
/// 两层动画缓冲各自复用 Image。切换动画时，旧层保持可见，直到新层画出首帧。
/// </summary>
internal static class GraphImagePool
{
    /// <summary>
    /// 在 UI 线程为目标层分配或复用对应动画类型的 Image。
    /// </summary>
    public static Image Attach(Decorator parent, GraphCore core, string kind)
    {
        Dispatcher.UIThread.VerifyAccess();

        var first = Obtain(core, $"Image1.{kind}");
        var second = Obtain(core, $"Image2.{kind}");

        // 等待新动画解码首帧时，清除目标层残留的画面。
        if (parent.Child is Image previousImage)
            previousImage.Source = null;

        if (ReferenceEquals(parent.Child, first))
            return first;
        if (ReferenceEquals(parent.Child, second))
            return second;

        var target = second.Parent == null ? second : first;
        if (target.Parent is Decorator old && !ReferenceEquals(old, parent))
            old.Child = null;
        target.Source = null;
        parent.Child = target;
        return target;
    }

    private static Image Obtain(GraphCore core, string key)
    {
        if (core.CommUIElements.TryGetValue(key, out var existing) && existing is Image image)
            return image;
        var created = new Image { Width = 500 };
        core.CommUIElements[key] = created;
        return created;
    }

    /// <summary>
    /// 在 UI 线程清除隐藏缓冲层和食物动画对这些位图的引用，然后释放位图。
    /// 隐藏层再次显示时，Avalonia 可能先绘制旧帧。
    /// </summary>
    public static void ReleaseBitmaps(GraphCore core, IReadOnlyCollection<Bitmap> bitmaps)
    {
        if (bitmaps.Count == 0)
            return;
        var set = new HashSet<Bitmap>(bitmaps);
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var element in core.CommUIElements.Values)
            {
                if (element is Image image && image.Source is Bitmap b && set.Contains(b))
                    image.Source = null;
                if (element is Grid composite)
                    foreach (var child in composite.Children)
                        if (child is Image layer && layer.Source is Bitmap bitmap && set.Contains(bitmap))
                            layer.Source = null;
            }
            foreach (var bitmap in set)
                bitmap.Dispose();
        });
    }
}
