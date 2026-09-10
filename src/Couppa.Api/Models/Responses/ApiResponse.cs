namespace Couppa.Api.Models.Responses;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public ApiError? Error { get; init; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };

    public static ApiResponse<T> Fail(string code, string message, object? details = null) => new()
    {
        Success = false,
        Error = new ApiError { Code = code, Message = message, Details = details }
    };
}

public class ApiError
{
    public required string Code { get; init; }
    public required string Message { get; init; }
    public object? Details { get; init; }
}

public class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalItems { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
}
