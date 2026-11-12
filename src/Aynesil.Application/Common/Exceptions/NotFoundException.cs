using Aynesil.Domain.Common;

namespace Aynesil.Application.Common.Exceptions;

public class NotFoundException : Exception, ILocalizableException
{
    public NotFoundException() : base() { }

    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.") { }

    public NotFoundException(MsgKey code, params object?[] args) : base(code.Value)
    {
        Code = code;
        Args = args;
    }

    public MsgKey? Code { get; }

    public IReadOnlyList<object?> Args { get; } = [];
}
