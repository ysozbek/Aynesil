using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── LinkGuardianToStudentCommand ──────────────────────────────────────────────

public record LinkGuardianToStudentCommand(
    Guid StudentId,
    Guid GuardianId,
    Guid? RelationshipId,
    bool IsPrimary,
    bool HasCustody,
    bool PortalAccess,
    bool FinancialResponsible) : IRequest<StudentGuardianDto>;

public class LinkGuardianToStudentCommandValidator
    : AbstractValidator<LinkGuardianToStudentCommand>
{
    public LinkGuardianToStudentCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.GuardianId).NotEmpty();
    }
}

public sealed class LinkGuardianToStudentCommandHandler
    : IRequestHandler<LinkGuardianToStudentCommand, StudentGuardianDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public LinkGuardianToStudentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<StudentGuardianDto> Handle(LinkGuardianToStudentCommand req, CancellationToken ct)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == req.StudentId, ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var guardian = await _db.Guardians
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == req.GuardianId, ct)
            ?? throw new KeyNotFoundException($"Guardian {req.GuardianId} not found.");

        if (guardian.CorporationId != student.CorporationId)
            throw new InvalidOperationException("Guardian and student must belong to the same corporation.");

        var existing = await _db.StudentGuardians
            .AnyAsync(sg => sg.StudentId == req.StudentId && sg.GuardianId == req.GuardianId, ct);
        if (existing)
            throw new InvalidOperationException("This guardian is already linked to the student.");

        var link = new StudentGuardian
        {
            CorporationId       = student.CorporationId,
            StudentId           = req.StudentId,
            GuardianId          = req.GuardianId,
            RelationshipId      = req.RelationshipId,
            IsPrimary           = req.IsPrimary,
            HasCustody          = req.HasCustody,
            PortalAccess        = req.PortalAccess,
            FinancialResponsible = req.FinancialResponsible
        };

        link.AddDomainEvent(new Aynesil.Domain.Modules.Students.Events.GuardianLinkedEvent(
            req.StudentId, req.GuardianId, student.CorporationId,
            req.IsPrimary, req.PortalAccess, _currentUser.UserId));

        _db.StudentGuardians.Add(link);
        await _db.SaveChangesAsync(ct);

        return StudentProjection.ToStudentGuardianDto(link, guardian);
    }
}

// ── UpdateGuardianLinkCommand ─────────────────────────────────────────────────

public record UpdateGuardianLinkCommand(
    Guid LinkId,
    Guid? RelationshipId,
    bool IsPrimary,
    bool HasCustody,
    bool PortalAccess,
    bool FinancialResponsible) : IRequest<StudentGuardianDto>;

public sealed class UpdateGuardianLinkCommandHandler
    : IRequestHandler<UpdateGuardianLinkCommand, StudentGuardianDto>
{
    private readonly IAppDbContext _db;

    public UpdateGuardianLinkCommandHandler(IAppDbContext db) => _db = db;

    public async Task<StudentGuardianDto> Handle(UpdateGuardianLinkCommand req, CancellationToken ct)
    {
        var link = await _db.StudentGuardians
            .FirstOrDefaultAsync(sg => sg.Id == req.LinkId, ct)
            ?? throw new KeyNotFoundException($"Guardian link {req.LinkId} not found.");

        link.RelationshipId      = req.RelationshipId;
        link.IsPrimary           = req.IsPrimary;
        link.HasCustody          = req.HasCustody;
        link.PortalAccess        = req.PortalAccess;
        link.FinancialResponsible = req.FinancialResponsible;

        await _db.SaveChangesAsync(ct);

        var guardian = await _db.Guardians
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == link.GuardianId, ct);

        return StudentProjection.ToStudentGuardianDto(link, guardian);
    }
}

// ── UnlinkGuardianFromStudentCommand ─────────────────────────────────────────

public record UnlinkGuardianFromStudentCommand(Guid LinkId) : IRequest;

public sealed class UnlinkGuardianFromStudentCommandHandler
    : IRequestHandler<UnlinkGuardianFromStudentCommand>
{
    private readonly IAppDbContext _db;

    public UnlinkGuardianFromStudentCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UnlinkGuardianFromStudentCommand req, CancellationToken ct)
    {
        var link = await _db.StudentGuardians
            .FirstOrDefaultAsync(sg => sg.Id == req.LinkId, ct)
            ?? throw new KeyNotFoundException($"Guardian link {req.LinkId} not found.");

        _db.StudentGuardians.Remove(link);
        await _db.SaveChangesAsync(ct);
    }
}
