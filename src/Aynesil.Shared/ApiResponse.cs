namespace Aynesil.Shared;

/// <summary>
/// Unified envelope for all API responses.
/// Success: { success: true, data: T, message: null, errors: null }
/// Error:   { success: false, data: null, message: "...", errors: { field: ["msg"] } }
/// Record type required for 'with' expression support in ExceptionMiddleware.
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> Fail(IDictionary<string, string[]> errors) =>
        new() { Success = false, Message = "Validation failed.", Errors = errors };
}

/// <summary>Non-generic convenience for responses with no payload.</summary>
public record ApiResponse : ApiResponse<object?>
{
    public static ApiResponse OkNoContent(string? message = null) =>
        new() { Success = true, Message = message };

    public static new ApiResponse Fail(string message, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };
}
