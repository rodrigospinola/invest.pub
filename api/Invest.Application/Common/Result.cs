namespace Invest.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? ErrorCode { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ErrorField { get; private set; }

    private Result() { }

    public static Result<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    public static Result<T> Failure(string errorCode, string errorMessage, string? errorField = null) =>
        new() { IsSuccess = false, ErrorCode = errorCode, ErrorMessage = errorMessage, ErrorField = errorField };
}
