using CSASystemAPI.Domain.Constants;


namespace CSASystemAPI.Domain.Entities;

public class CSAccount
{
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public int? ParentId { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int LoginCount { get; set; }

    // 导航属性
    public CSAccount? Parent { get; set; }
    public ICollection<CSAccount> Subordinates { get; set; } = new List<CSAccount>();
    public ICollection<TgAccount> ManagedAccounts { get; set; } = new List<TgAccount>();
    public ICollection<TGMessage> SentMessages { get; set; } = new List<TGMessage>();
    public ICollection<TGMessage> ReceivedMessages { get; set; } = new List<TGMessage>();
}