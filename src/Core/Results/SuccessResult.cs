namespace Core.Results;

public record class SuccessResult<T>(T? inputResult) : IResult<T>
{
    public string Message { get; set; } = "Success";
    public bool IsSuccess { get; set; } = true;
    public T? Result { get; set; } = inputResult;
}