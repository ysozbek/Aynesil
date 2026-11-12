namespace Aynesil.Shared;

/// <summary>Alan hatası: makine okunur kod + istek dilinde mesaj.</summary>
public sealed record ErrorDetail(string Code, string Message);

/// <summary>
/// Unified envelope for all API responses.
/// Success: { success: true, data: T, message: null, errors: null }
/// Error:   { success: false, data: null, message: "...", code: "Error.X",
///            errors: { field: ["msg"] }, errorDetails: { field: [{ code, message }] }, traceId }
/// <c>errors</c> biçimi (alan → string[]) mevcut istemciler için değişmez; <c>code</c> ve <c>errorDetails</c> eklemelidir.
/// Record type required for 'with' expression support in ExceptionMiddleware.
/// </summary>
public record ApiResponse<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public string? Code { get; init; }
    public IDictionary<string, string[]>? Errors { get; init; }
    public IDictionary<string, ErrorDetail[]>? ErrorDetails { get; init; }
    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };

    public static ApiResponse<T> Fail(string message, string code,
        IDictionary<string, string[]>? errors = null, IDictionary<string, ErrorDetail[]>? errorDetails = null) =>
        new() { Success = false, Message = message, Code = code, Errors = errors, ErrorDetails = errorDetails };
}

/// <summary>Non-generic convenience for responses with no payload.</summary>
public record ApiResponse : ApiResponse<object?>
{
    public static ApiResponse OkNoContent(string? message = null) =>
        new() { Success = true, Message = message };

    public static new ApiResponse Fail(string message, IDictionary<string, string[]>? errors = null) =>
        new() { Success = false, Message = message, Errors = errors };

    public static new ApiResponse Fail(string message, string code,
        IDictionary<string, string[]>? errors = null, IDictionary<string, ErrorDetail[]>? errorDetails = null) =>
        new() { Success = false, Message = message, Code = code, Errors = errors, ErrorDetails = errorDetails };
}
