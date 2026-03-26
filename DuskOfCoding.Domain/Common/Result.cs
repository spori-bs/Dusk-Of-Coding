namespace DuskOfCoding.Domain.Common;

public readonly struct Result<T>
{
    public T? Value { get; }
    public string? ErrorMessage { get; }
    public bool IsSuccess { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        ErrorMessage = null;
    }

    private Result(string error)
    {
        IsSuccess = false;
        Value = default;
        ErrorMessage = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
}

public readonly struct Result
{
    public string? ErrorMessage { get; }
    public bool IsSuccess { get; }

    private Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        ErrorMessage = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}
