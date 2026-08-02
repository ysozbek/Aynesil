using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateConsultancyAgreementCommand ─────────────────────────────────────────

public record CreateConsultancyAgreementCommand(
    Guid CorporationId,
    Guid ConsultancyPlanId,
    string Title,
    Guid? AgreementTypeId,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    Guid? CreatedBy = null) : IRequest<ConsultancyAgreementDto>;

public class CreateConsultancyAgreementCommandValidator
    : AbstractValidator<CreateConsultancyAgreementCommand>
{
    public CreateConsultancyAgreementCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.ConsultancyPlanId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x)
            .Must(x => !(x.StartDate.HasValue && x.EndDate.HasValue && x.EndDate < x.StartDate))
            .WithMessage("End date cannot be before start date.");
    }
}

public sealed class CreateConsultancyAgreementCommandHandler
    : IRequestHandler<CreateConsultancyAgreementCommand, ConsultancyAgreementDto>
{
    private readonly IAppDbContext _db;

    public CreateConsultancyAgreementCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ConsultancyAgreementDto> Handle(
        CreateConsultancyAgreementCommand req, CancellationToken ct)
    {
        var plan = await _db.ConsultancyPlans.AsNoTracking()
            .Where(p => p.Id == req.ConsultancyPlanId)
            .Select(p => new { p.Id, p.InstitutionId, p.Name, p.Status })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Consultancy plan {req.ConsultancyPlanId} not found.");

        if (plan.Status is "cancelled")
            throw new InvalidOperationException("Cannot create an agreement for a cancelled plan.");

        var agreement = ConsultancyAgreement.Create(
            req.CorporationId, req.ConsultancyPlanId, plan.InstitutionId,
            req.Title, req.AgreementTypeId, req.Description,
            req.StartDate, req.EndDate, req.CreatedBy);

        _db.ConsultancyAgreements.Add(agreement);
        await _db.SaveChangesAsync(ct);

        return await BuildDto(agreement.Id, ct);
    }

    private async Task<ConsultancyAgreementDto> BuildDto(Guid id, CancellationToken ct)
        => await ProjectAgreementDto(_db, id, ct)
           ?? throw new KeyNotFoundException($"Agreement {id} not found after save.");

    internal static async Task<ConsultancyAgreementDto?> ProjectAgreementDto(
        IAppDbContext db, Guid id, CancellationToken ct)
        => await (
            from a in db.ConsultancyAgreements.AsNoTracking()
            where a.Id == id
            join p  in db.ConsultancyPlans.AsNoTracking()  on a.ConsultancyPlanId equals p.Id
            join i  in db.Institutions.AsNoTracking()       on a.InstitutionId      equals i.Id
            join typ in db.RefValues.AsNoTracking()
                on a.AgreementTypeId equals typ.Id into typGrp
            from typ in typGrp.DefaultIfEmpty()
            select new ConsultancyAgreementDto(
                a.Id, a.CorporationId,
                a.ConsultancyPlanId, p.Name,
                a.InstitutionId, i.Name,
                a.AgreementTypeId, typ != null ? typ.Code : null,
                a.Title, a.Description,
                a.StartDate, a.EndDate, a.SignedDate,
                a.Status, a.FileId, a.SignedByName,
                a.CreatedAt, a.CreatedBy, a.UpdatedAt, a.UpdatedBy, a.RowVersion)
        ).FirstOrDefaultAsync(ct);
}

// ── UpdateConsultancyAgreementCommand ─────────────────────────────────────────

public record UpdateConsultancyAgreementCommand(
    Guid Id,
    string Title,
    Guid? AgreementTypeId,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateConsultancyAgreementCommandValidator
    : AbstractValidator<UpdateConsultancyAgreementCommand>
{
    public UpdateConsultancyAgreementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => !(x.StartDate.HasValue && x.EndDate.HasValue && x.EndDate < x.StartDate))
            .WithMessage("End date cannot be before start date.");
    }
}

public sealed class UpdateConsultancyAgreementCommandHandler
    : IRequestHandler<UpdateConsultancyAgreementCommand>
{
    private readonly IAppDbContext _db;

    public UpdateConsultancyAgreementCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateConsultancyAgreementCommand req, CancellationToken ct)
    {
        var agreement = await _db.ConsultancyAgreements
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Agreement {req.Id} not found.");

        agreement.Update(req.Title, req.AgreementTypeId, req.Description,
            req.StartDate, req.EndDate, req.UpdatedBy);

        await _db.SaveChangesAsync(ct);
    }
}

// ── SendConsultancyAgreementCommand ───────────────────────────────────────────

public record SendConsultancyAgreementCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class SendConsultancyAgreementCommandHandler
    : IRequestHandler<SendConsultancyAgreementCommand>
{
    private readonly IAppDbContext _db;

    public SendConsultancyAgreementCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SendConsultancyAgreementCommand req, CancellationToken ct)
    {
        var agreement = await _db.ConsultancyAgreements
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Agreement {req.Id} not found.");

        agreement.Send(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── SignConsultancyAgreementCommand ───────────────────────────────────────────

public record SignConsultancyAgreementCommand(
    Guid Id,
    DateOnly SignedDate,
    string? SignedByName,
    Guid? FileId,
    Guid? UpdatedBy = null) : IRequest;

public class SignConsultancyAgreementCommandValidator
    : AbstractValidator<SignConsultancyAgreementCommand>
{
    public SignConsultancyAgreementCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.SignedDate).NotEmpty();
        RuleFor(x => x.SignedByName).MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.SignedByName));
    }
}

public sealed class SignConsultancyAgreementCommandHandler
    : IRequestHandler<SignConsultancyAgreementCommand>
{
    private readonly IAppDbContext _db;

    public SignConsultancyAgreementCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(SignConsultancyAgreementCommand req, CancellationToken ct)
    {
        var agreement = await _db.ConsultancyAgreements
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Agreement {req.Id} not found.");

        agreement.Sign(req.SignedDate, req.SignedByName, req.FileId, req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── ExpireConsultancyAgreementCommand ─────────────────────────────────────────

public record ExpireConsultancyAgreementCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class ExpireConsultancyAgreementCommandHandler
    : IRequestHandler<ExpireConsultancyAgreementCommand>
{
    private readonly IAppDbContext _db;

    public ExpireConsultancyAgreementCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(ExpireConsultancyAgreementCommand req, CancellationToken ct)
    {
        var agreement = await _db.ConsultancyAgreements
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Agreement {req.Id} not found.");

        agreement.MarkExpired(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── CancelConsultancyAgreementCommand ─────────────────────────────────────────

public record CancelConsultancyAgreementCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class CancelConsultancyAgreementCommandHandler
    : IRequestHandler<CancelConsultancyAgreementCommand>
{
    private readonly IAppDbContext _db;

    public CancelConsultancyAgreementCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(CancelConsultancyAgreementCommand req, CancellationToken ct)
    {
        var agreement = await _db.ConsultancyAgreements
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Agreement {req.Id} not found.");

        agreement.Cancel(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteConsultancyAgreementCommand ─────────────────────────────────────────

public record DeleteConsultancyAgreementCommand(Guid Id, Guid? DeletedBy = null) : IRequest;

public sealed class DeleteConsultancyAgreementCommandHandler
    : IRequestHandler<DeleteConsultancyAgreementCommand>
{
    private readonly IAppDbContext _db;

    public DeleteConsultancyAgreementCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteConsultancyAgreementCommand req, CancellationToken ct)
    {
        var agreement = await _db.ConsultancyAgreements
            .FirstOrDefaultAsync(a => a.Id == req.Id && a.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Agreement {req.Id} not found.");

        if (agreement.IsSigned)
            throw new InvalidOperationException(
                "A signed or expired agreement cannot be deleted.");

        agreement.SoftDelete(req.DeletedBy);
        await _db.SaveChangesAsync(ct);
    }
}
