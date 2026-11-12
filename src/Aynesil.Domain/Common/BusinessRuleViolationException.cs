namespace Aynesil.Domain.Common;

/// <summary>
/// Thrown by domain entities and application handlers when a business rule is violated
/// (invalid workflow transition, duplicate enrollment, record in the wrong state, ...).
/// Mapped to HTTP 409 by ExceptionMiddleware.
/// <para>
/// Deliberately NOT an <see cref="InvalidOperationException"/>: that type is also thrown by the
/// framework for programmer/system errors, and those must surface as HTTP 500, not 409.
/// </para>
/// </summary>
public class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException() { }

    public BusinessRuleViolationException(string message) : base(message) { }

    public BusinessRuleViolationException(string message, Exception innerException)
        : base(message, innerException) { }
}
