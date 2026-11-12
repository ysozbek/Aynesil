using Aynesil.Application.Common.Validation;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Common.Interfaces;
using ValidationException = Aynesil.Application.Common.Exceptions.ValidationException;
using Aynesil.Application.Features.Leads.Dtos;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Leads.Commands;

// ── Request ───────────────────────────────────────────────────────────────────
public record AssignLeadCommand(
    Guid LeadId,
    Guid UserId,
    int RowVersion) : IRequest<LeadDto>;

// ── Validator ─────────────────────────────────────────────────────────────────
public class AssignLeadCommandValidator : AbstractValidator<AssignLeadCommand>
{
    public AssignLeadCommandValidator()
    {
        RuleFor(x => x.LeadId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RowVersion).GreaterThan(0);
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────
public sealed class AssignLeadCommandHandler : IRequestHandler<AssignLeadCommand, LeadDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ITenantContext _tenant;

    public AssignLeadCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _db = db;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task<LeadDto> Handle(AssignLeadCommand req, CancellationToken ct)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == req.LeadId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Lead"));

        if (lead.RowVersion != req.RowVersion)
            throw new ValidationException([ValidationFailures.For(nameof(req.RowVersion), MsgKeys.Error.Concurrency)]);

        var userExists = await _db.UserAccounts.AnyAsync(u => u.Id == req.UserId, ct);
        if (!userExists)
            throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "UserAccount"));

        lead.Assign(req.UserId, _currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await LeadProjection.LoadAsync(_db, lead.Id, _tenant.Locale, ct))!;
    }
}
