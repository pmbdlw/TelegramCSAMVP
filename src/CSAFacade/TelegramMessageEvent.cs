namespace CSAFacade;

/// <summary>
/// 消息事件数据模型
/// </summary>
public class TelegramMessageEvent
{
    public string PhoneNumber { get; set; }
    public long MessageId { get; set; }
    public string Text { get; set; }
    public DateTime Date { get; set; }
    public long FromId { get; set; }
    public string FromName { get; set; }
    public long ChatId { get; set; }
    public string ChatTitle { get; set; }
    public bool IsOutgoing { get; set; }
    public string MessageType { get; set; }
}