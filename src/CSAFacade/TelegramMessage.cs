using TL;
namespace CSAFacade;

// 自定义消息类，用于封装Telegram的消息
public class TelegramMessage
{
    public long Id { get; set; }
    public string Text { get; set; }
    public DateTime Date { get; set; }
    public long FromId { get; set; }
    public bool IsOutgoing { get; set; }
    public MessageMedia Media { get; set; }

    public static TelegramMessage FromMessageBase(MessageBase messageBase)
    {
        if (messageBase is Message message)
        {
            return new TelegramMessage
            {
                Id = message.ID,
                Text = message.message,
                Date = message.date,
                FromId = message.from_id?.ID ?? 0,
                IsOutgoing = message.flags.HasFlag(Message.Flags.out_),//.out_msg), // 修正这里
                Media = message.media
            };
        }
        
        return null;
    }
}