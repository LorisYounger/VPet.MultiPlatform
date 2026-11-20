using System;
using System.Collections.Generic;
using VPet_Simulator.Unified.Services;

namespace VPet_Simulator.Unified.Interface;

/// <summary>当前访客表。VPet 管理房间并收发消息。</summary>
public interface IMPHost
{
    ulong LobbyID { get; }
    ulong HostID { get; }
    bool IsHost { get; }
    bool Joinable { get; }
    bool IsGameRunning { get; set; }
    /// <summary>当前玩家。Level 是桌宠等级，成员存档尚未收到时为 null。</summary>
    (ulong FriendID, string Name, int? Level) Self { get; }
    /// <summary>当前访客，不含本人。请在 UI 线程枚举。从后台线程读取时，可调用 IPetHost.RunOnUI 完成枚举。</summary>
    IEnumerable<(ulong FriendID, string Name, int? Level)> Friends { get; }
    void Log(string message);

    bool SendMessage(ulong friendID, UnifiedMPMessage message);
    void SendMessageALL(UnifiedMPMessage message);
    /// <summary>收到消息时传入实际发件人的 ID 和消息。回调在宿主的收包线程执行，操作控件时请调用 RunOnUI。</summary>
    event Action<ulong, UnifiedMPMessage>? ReceivedMessage;
    event Action<ulong>? OnMemberJoined;
    event Action<ulong>? OnMemberLeave;
    event Action? ClosingMutiPlayer;

    /// <summary>宿主原生选项卡控件。平台专属视图可在 UI 线程使用。</summary>
    object TabControl { get; }
}

/// <summary>访客表的原始消息。Type 可用 MOD 自定义编号，To 是消息内的目标玩家。</summary>
public struct UnifiedMPMessage
{
    public int Type { get; set; }
    public ulong To { get; set; }
    /// <summary>消息正文，与 MPMessage.GetContent() 返回的原文一致。</summary>
    public string? Content { get; set; }

    public void SetContent(string content) => Content = content;
    public void SetContent(object content) => Content = MPProtocol.EncodeContent(content);
    public T? GetContent<T>() => MPProtocol.DecodeContent<T>(Content!);
}
