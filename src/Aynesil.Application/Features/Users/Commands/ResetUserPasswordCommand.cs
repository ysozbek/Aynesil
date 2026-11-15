using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Identity;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Validation;
using Aynesil.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;

namespace Aynesil.Application.Features.Users.Commands;

/// <summary>
/// Kurum yöneticisi bir kullanıcının şifresini sıfırlar (e-posta sağlayıcısı olmadığı için "Şifremi unuttum"un yerine).
/// Yeni geçici şifre üretilir ve yanıtta <b>bir kez</b> döner; düz metin saklanmaz ve loglanmaz. Kullanıcı ilk girişte
/// şifresini değiştirmek zorundadır (must_change_password = true), açık oturumları iptal edilir ve hatalı deneme
/// kilidi kaldırılır. Başka kurumun kullanıcısı 404'tür (RLS'ye ek olarak sorguda kurum filtresi).
/// </summary>
public record ResetUserPasswordCommand(Guid UserId) : IRequest<ResetUserPasswordResult>;

/// <summary><see cref="TemporaryPassword"/> yalnızca bu yanıtta görünür.</summary>
public sealed record ResetUserPasswordResult(Guid UserId, string Username, string TemporaryPassword);

public class ResetUserPasswordCommandValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordCommandValidator() => RuleFor(x => x.UserId).NotEmpty();
}

public sealed class ResetUserPasswordCommandHandler : IRequestHandler<ResetUserPasswordCommand, ResetUserPasswordResult>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;

    public ResetUserPasswordCommandHandler(
        IAppDbContext db, ITenantContext tenant, ICurrentUserService currentUser,
        IPasswordService passwords, ITokenService tokens)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _passwords = passwords;
        _tokens = tokens;
    }

    public async Task<ResetUserPasswordResult> Handle(ResetUserPasswordCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.CorporationId
            ?? throw new UnauthorizedAccessException("Tenant context is required.");

        var user = await _db.UserAccounts
            .FirstOrDefaultAsync(u => u.Id == req.UserId && u.CorporationId == corporationId && u.DeletedAt == null, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "User"));

        if (_currentUser.UserId == req.UserId)
            throw new ValidationException([ValidationFailures.For(nameof(req.UserId), MsgKeys.Rule.CannotResetOwnPassword)]);

        var temporaryPassword = UserAccountLinks.GenerateTemporaryPassword();
        user.ResetPasswordByAdmin(_passwords.Hash(temporaryPassword), _currentUser.UserId);
        await _db.SaveChangesAsync(ct);

        // Eski şifreyle açılmış oturumlar düşer; kilitli hesap yeni geçici şifreyle hemen girebilir.
        await _tokens.RevokeAllAsync(user.Id, ct);
        await _passwords.ResetFailedAttemptsAsync(user.Id, ct);

        return new ResetUserPasswordResult(user.Id, user.Username, temporaryPassword);
    }
}
