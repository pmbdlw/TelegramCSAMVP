namespace CSASystemAPI.Domain.Entities;

public class TgAccount
{
    public string PhoneNumber { get; set; } = default!;
    public string SessionString { get; set; } = default!;
    public int CsId { get; set; }
    public int? GroupId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastUsedAt { get; set; }
    public string ApiId { get; set; } = default!;
    public string ApiHash { get; set; } = default!;
}