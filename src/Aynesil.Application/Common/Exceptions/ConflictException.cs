namespace Aynesil.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request conflicts with the current state of a resource
/// (duplicate enrollment, unique business key, invalid state transition, etc.).
/// Mapped to HTTP 409 by ExceptionMiddleware.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException() : base() { }

    public ConflictException(string message) : base(message) { }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException) { }
}
