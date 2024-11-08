using TL;

namespace CSAFacade;

public class TelegramAccountInfo
{
    public long Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsVerified { get; set; }
    public bool IsPremium { get; set; }
    public int? MaxAccounts { get; set; }

    public static TelegramAccountInfo FromUser(User user)
    {
        return new TelegramAccountInfo
        {
            Id = user.ID,
            FirstName = user.first_name,
            LastName = user.last_name,
            Username = user.username,
            PhoneNumber = user.phone,
            IsVerified = user.flags.HasFlag(User.Flags.verified),
            IsPremium = user.flags.HasFlag(User.Flags.premium),
            MaxAccounts = null // 需要通过其他API获取
        };
    }
}