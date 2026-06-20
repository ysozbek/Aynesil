using Aynesil.Application.Common.Interfaces;
using MediatR;

namespace Aynesil.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? UserAgent = null) : IRequest<LoginResult>;

public class RefreshTokenCommandValidator : FluentValidation.AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() =>
        RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    private readonly ITokenService _tokens;

    public RefreshTokenCommandHandler(ITokenService tokens) => _tokens = tokens;

    public async Task<LoginResult> Handle(RefreshTokenCommand req, CancellationToken ct)
    {
        var pair = await _tokens.RefreshAsync(req.RefreshToken, req.IpAddress, req.UserAgent, ct);
        // Return minimal info — caller already has user details from previous login
        return new LoginResult(pair.AccessToken, pair.RefreshToken, pair.ExpiresAt,
            string.Empty, null, null, Guid.Empty, Guid.Empty);
    }
}
