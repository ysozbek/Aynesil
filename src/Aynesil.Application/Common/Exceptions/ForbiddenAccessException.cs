using Aynesil.Domain.Common;

namespace Aynesil.Application.Common.Exceptions;

public class ForbiddenAccessException : Exception, ILocalizableException
{
    public ForbiddenAccessException() : base("Access to this resource is forbidden.") { }

    public ForbiddenAccessException(string message) : base(message) { }

    public ForbiddenAccessException(MsgKey code, params object?[] args) : base(code.Value)
    {
        Code = code;
        Args = args;
    }

    /// <summary>Verilmezse genel "yetkiniz yok" (Error.Forbidden) kullanılır.</summary>
    public MsgKey? Code { get; }

    public IReadOnlyList<object?> Args { get; } = [];
}
