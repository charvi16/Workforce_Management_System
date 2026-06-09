namespace WMS.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }

    public IReadOnlyCollection<string> Errors { get; set; } = Array.Empty<string>();

    public static ApiResponse<T> Ok(T data, string message)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> Fail(string message, params string[] errors)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }
}
