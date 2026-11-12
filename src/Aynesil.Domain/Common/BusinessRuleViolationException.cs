namespace Aynesil.Domain.Common;

/// <summary>
/// Thrown by domain entities and application handlers when a business rule is violated
/// (invalid workflow transition, duplicate enrollment, record in the wrong state, ...).
/// Mapped to HTTP 409 by ExceptionMiddleware.
/// <para>
/// Deliberately NOT an <see cref="InvalidOperationException"/>: that type is also thrown by the
/// framework for programmer/system errors, and those must surface as HTTP 500, not 409.
/// </para>
/// <para>
/// Tercih edilen kullanım <c>new BusinessRuleViolationException(MsgKeys.X.Y, args)</c>: metin istek dilinde
/// middleware'de çözülür. Düz metin ctor'ları geçiş dönemine aittir (kodsuz → ham metin döner).
/// </para>
/// </summary>
public class BusinessRuleViolationException : Exception, ILocalizableException
{
    public BusinessRuleViolationException() { }

    public BusinessRuleViolationException(string message) : base(message) { }

    public BusinessRuleViolationException(string message, Exception innerException)
        : base(message, innerException) { }

    public BusinessRuleViolationException(MsgKey code, params object?[] args) : base(code.Value)
    {
        Code = code;
        Args = args;
    }

    public MsgKey? Code { get; }

    public IReadOnlyList<object?> Args { get; } = [];
}
