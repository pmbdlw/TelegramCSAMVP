namespace CSAFacade;

public class LoginCodeRequiredException : Exception
{
    public string PhoneNumber { get; }
    public string CodeHash { get; }
    public string PhoneCodeHash { get; }

    public LoginCodeRequiredException(string phoneNumber, string codeHash, string phoneCodeHash) 
        : base("Verification code required")
    {
        PhoneNumber = phoneNumber;
        CodeHash = codeHash;
        PhoneCodeHash = phoneCodeHash;
    }
}