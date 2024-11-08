using System.Text.Json;
using CSASystemAPI.Domain.Constants;

namespace CSASystemAPI.Domain.Entities;

public class TGMessage
{
    public int TgAccountId { get; set; }
    public MessageType MessageType { get; set; }
    public string Content { get; set; } = default!;
    public JsonDocument? RawData { get; set; }
    public MessageStatus Status { get; set; }
    public int? SenderId { get; set; }
    public int? RecipientId { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
}