//跨平台: 原文复制自 VPet-Simulator.Windows.Interface/ISetting.cs; System.Windows.Point 换成 Avalonia.Point
using Avalonia;

namespace VPet_Simulator.Windows.Interface
{
    /// <summary>
    /// 桌宠的启动位置和上次退出位置，坐标使用 Avalonia.Point。
    /// </summary>
    public partial interface ISetting
    {

        /// <summary>
        /// 获取上次退出位置
        /// </summary>
        Point StartRecordLastPoint { get; }

        /// <summary>
        /// 获取或设置桌宠启动的位置
        /// </summary>
        Point StartRecordPoint { get; set; }
    }
}
