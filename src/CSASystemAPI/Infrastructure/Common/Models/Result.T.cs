namespace CSASystemAPI.Infrastructure.Common.Models;

public class Result<T> : Result
{
    public T Value { get; private set; }

    protected internal Result(T value, bool isSuccess, string error) 
        : base(isSuccess, error)
    {
        Value = value;
    }
}