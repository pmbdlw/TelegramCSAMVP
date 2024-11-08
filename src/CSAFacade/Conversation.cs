using TL;

namespace CSAFacade;

public class Conversation
{
    public long Id { get; set; }
    public string Title { get; set; }
    public ConversationType Type { get; set; }
    /// <summary>
    /// 默认-1，代表未获取到数据
    /// </summary>
    public int UnreadCount { get; set; } = -1;
    public DateTime LastMessageDate { get; set; }
    public string LastMessageText { get; set; }
}
/// <summary>
/// Conversation type
/// </summary>
public enum ConversationType
{
    PrivateChat,
    Group,
    Channel
}