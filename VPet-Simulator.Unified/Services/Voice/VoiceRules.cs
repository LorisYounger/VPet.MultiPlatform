using System;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 根据语音进度控制说话气泡的收起时机
/// </summary>
/// <remarks>文字显示完毕后，消息栏可根据剩余播放时间继续保留气泡。</remarks>
public static class VoiceRules
{
    /// <summary>
    /// 收起气泡前预留的语音时长（秒）
    /// </summary>
    /// <remarks>Windows 版 MessageBar 为气泡的渐隐动画预留两秒。</remarks>
    public const double HoldSeconds = 2;

    /// <summary>
    /// 语音播放期间是否继续显示气泡
    /// </summary>
    /// <param name="playingVoice">是否正在播放语音</param>
    /// <param name="remaining">剩余播放时间，未知时传 TimeSpan.Zero</param>
    /// <remarks>剩余时长大于两秒时继续显示。时长未知时允许收起气泡。</remarks>
    public static bool ShouldHoldBubble(bool playingVoice, TimeSpan remaining)
        => playingVoice && remaining.TotalSeconds > HoldSeconds;
}
