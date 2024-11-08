namespace CSAFacade;
public class LoginCodeRequestEventArgs : EventArgs
{
    public string PhoneNumber { get; }
    public TaskCompletionSource<string> CodeTaskSource { get; }

    public LoginCodeRequestEventArgs(string phoneNumber)
    {
        PhoneNumber = phoneNumber;
        CodeTaskSource = new TaskCompletionSource<string>();
    }
}