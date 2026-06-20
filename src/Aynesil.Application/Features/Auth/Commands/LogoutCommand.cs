using Aynesil.Application.Common.Interfaces;
using MediatR;

namespace Aynesil.Application.Features.Auth.Commands;

public record LogoutCommand(string RefreshToken) : IRequest;

public class LogoutCommandValidator : FluentValidation.AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator() =>
        RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly ITokenService _tokens;
    public LogoutCommandHandler(ITokenService tokens) => _tokens = tokens;

    public async Task Handle(LogoutCommand req, CancellationToken ct) =>
        await _tokens.RevokeAsync(req.RefreshToken, ct);
}
