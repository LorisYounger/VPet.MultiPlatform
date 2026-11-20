using LinePutScript;
using System.Text;

namespace VPet_Simulator.Unified.Services;

/// <summary>
/// 多人联机消息格式
/// </summary>
/// <remarks>两端使用相同的消息类型数值、字段名和编码方式。宿主负责通过 Steam 收发消息。</remarks>
public static class MPProtocol
{
    // ---- 字段名 ----

    /// <summary>消息类型的行名</summary>
    public const string TypeLineName = "Type";

    /// <summary>消息内容的行名</summary>
    public const string ContentLineName = "Content";

    /// <summary>被操作者的行名</summary>
    public const string ToLineName = "To";

    // ---- 大厅数据的键 ----

    /// <summary>标记这是个桌宠联机大厅</summary>
    public const string LobbyMarkKey = "isvpets";

    /// <summary>大厅标记的值</summary>
    public const string LobbyMarkValue = "true";

    /// <summary>被踢出者的 SteamId</summary>
    public const string LobbyKickKey = "kick";

    /// <summary>是否不再接受新人</summary>
    public const string LobbyNoJoinKey = "nojoin";

    // ---- 消息类型 ----

    /// <summary>
    /// 消息类型
    /// </summary>
    /// <remarks>数值与 Windows 版 MPMessage.MSGType 一致。MOD 可用未占用的数值（含负数）定义自己的消息类型。</remarks>
    public enum MessageKind
    {
        /// <summary>一般是出错或者空消息</summary>
        Empty,
        /// <summary>聊天消息</summary>
        Chat,
        /// <summary>显示动画</summary>
        DispayGraph,
        /// <summary>交互</summary>
        Interact,
        /// <summary>喂食</summary>
        Feed,
    }

    /// <summary>
    /// 交互类型
    /// </summary>
    /// <remarks>数值与 Windows 版 MPMessage.Interact 一致。</remarks>
    public enum InteractKind
    {
        /// <summary>摸头</summary>
        TouchHead,
        /// <summary>摸身体</summary>
        TouchBody,
        /// <summary>捏脸</summary>
        TouchPinch,
    }

    /// <summary>
    /// 聊天可见范围
    /// </summary>
    /// <remarks>数值与 Windows 版 MPMessage.Chat.Type 一致。</remarks>
    public enum ChatKind
    {
        /// <summary>私有</summary>
        Private,
        /// <summary>半公开</summary>
        Internal,
        /// <summary>公开</summary>
        Public,
    }

    // ---- 消息内容 ----

    /// <summary>
    /// 聊天消息的内容
    /// </summary>
    /// <remarks>字段名和顺序须与 Windows 版 MPMessage.Chat 一致。LPSConvert 将聊天内容序列化为字符串，写入消息的 Content 行。</remarks>
    public struct Chat
    {
        /// <summary>聊天内容</summary>
        public string Content { get; set; }
        /// <summary>可见范围</summary>
        public ChatKind ChatType { get; set; }
        /// <summary>发送者名字</summary>
        public string SendName { get; set; }
        /// <summary>接收者名字</summary>
        public string ToName { get; set; }
    }

    /// <summary>
    /// 把消息内容序列化成字符串
    /// </summary>
    /// <remarks>序列化参数与 Windows 版 MPMessage.SetContent 相同。</remarks>
    public static string EncodeContent(object content)
        => LinePutScript.Converter.LPSConvert.GetObjectString(content, convertNoneLineAttribute: true);

    /// <summary>
    /// 从字符串反序列化消息内容
    /// </summary>
    public static T? DecodeContent<T>(string content)
        => (T?)LinePutScript.Converter.LPSConvert.GetStringObject(content, typeof(T), convertNoneLineAttribute: true);

    // ---- 封装 ----

    /// <summary>
    /// 创建联机消息
    /// </summary>
    /// <param name="kind">消息类型</param>
    /// <param name="to">被操作者, 广播传 0</param>
    /// <param name="content">已经序列化好的内容 (见 EncodeContent)</param>
    /// <remarks>Type、Content 和 To 各占一个顶层行，与 Windows 版 MPMessage 的 [Line] 属性对应。写成子项会使接收端读到默认值。</remarks>
    public static ILPS BuildMessage(MessageKind kind, ulong to, string content)
    {
        var document = new LPS();
        document.Add(new Line(TypeLineName, ((int)kind).ToString()));
        document.Add(new Line(ContentLineName, content));
        document.Add(new Line(ToLineName, to.ToString()));
        return document;
    }

    /// <summary>
    /// 读一条消息的类型
    /// </summary>
    public static int ReadKind(ILPS message) => message.FindLine(TypeLineName)?.InfoToInt ?? 0;

    /// <summary>
    /// 读一条消息的被操作者
    /// </summary>
    public static ulong ReadTo(ILPS message)
        => ulong.TryParse(message.FindLine(ToLineName)?.Info, out var value) ? value : 0;

    /// <summary>
    /// 读一条消息的内容
    /// </summary>
    public static string ReadContent(ILPS message) => message.FindLine(ContentLineName)?.Info ?? string.Empty;

    /// <summary>
    /// 将消息编码为 UTF-8 字节
    /// </summary>
    public static byte[] EncodeMessage(ILPS message) => Encode(message);

    /// <summary>
    /// 将消息编码为 UTF-8 字节
    /// </summary>
    /// <remarks>Steam P2P 保留包边界，消息直接使用 LPS 文本的 UTF-8 字节，无长度前缀或校验位。文本为 null 时返回空包。</remarks>
    public static byte[] Encode(ILPS message)
        => Encoding.UTF8.GetBytes(message.ToString() ?? string.Empty);

    /// <summary>
    /// 将收到的 UTF-8 数据包解析为 LPS 消息
    /// </summary>
    public static LPS Decode(byte[] data) => new LPS(Encoding.UTF8.GetString(data));
}
