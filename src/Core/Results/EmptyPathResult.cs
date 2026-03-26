namespace Core.Results;

public record class EmptyPathResult<T>(T? inputResult) : IResult<T>
{
    public string Message { get; set; } = "Passed empty path";
    public bool IsSuccess { get; set; }
    public T? Result { get; set; } = inputResult;
}