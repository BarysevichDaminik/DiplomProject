namespace Core.Results;

public interface IResult<T>
{
    string Message { get; set; }

    bool IsSuccess { get; set; }

    T? Result { get; set; }
}