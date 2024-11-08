using System.Text.Json;

namespace CSASystemAPI.Domain.Entities;

public class OperationLog
{
    public int CsId { get; set; }
    public string OperationType { get; set; } = default!;
    public JsonDocument OperationDetail { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public string? UserAgent { get; set; }
}