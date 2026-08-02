using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Consultancy.Dtos;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Consultancy.Commands;

// ── CreateInstitutionCommand ──────────────────────────────────────────────────

public record CreateInstitutionCommand(
    Guid CorporationId,
    string Name,
    Guid? InstitutionTypeId,
    string? City,
    string? District,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    Guid? CreatedBy = null) : IRequest<InstitutionDto>;

public class CreateInstitutionCommandValidator : AbstractValidator<CreateInstitutionCommand>
{
    public CreateInstitutionCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
        RuleFor(x => x.ContactPhone).MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.ContactPhone));
    }
}

public sealed class CreateInstitutionCommandHandler
    : IRequestHandler<CreateInstitutionCommand, InstitutionDto>
{
    private readonly IAppDbContext _db;

    public CreateInstitutionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<InstitutionDto> Handle(
        CreateInstitutionCommand req, CancellationToken ct)
    {
        var institution = Institution.Create(
            req.CorporationId, req.Name, req.InstitutionTypeId,
            req.City, req.District,
            req.ContactName, req.ContactPhone, req.ContactEmail,
            req.CreatedBy);

        _db.Institutions.Add(institution);
        await _db.SaveChangesAsync(ct);

        var typeCode = req.InstitutionTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.InstitutionTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct)
            : null;

        return new InstitutionDto(
            institution.Id, institution.CorporationId,
            institution.InstitutionTypeId, typeCode,
            institution.Name, institution.City, institution.District,
            institution.ContactName, institution.ContactPhone, institution.ContactEmail,
            institution.CreatedAt, institution.UpdatedAt, institution.RowVersion);
    }
}

// ── UpdateInstitutionCommand ──────────────────────────────────────────────────

public record UpdateInstitutionCommand(
    Guid Id,
    string Name,
    Guid? InstitutionTypeId,
    string? City,
    string? District,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    int RowVersion,
    Guid? UpdatedBy = null) : IRequest;

public class UpdateInstitutionCommandValidator : AbstractValidator<UpdateInstitutionCommand>
{
    public UpdateInstitutionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RowVersion).GreaterThan(0);
        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}

public sealed class UpdateInstitutionCommandHandler : IRequestHandler<UpdateInstitutionCommand>
{
    private readonly IAppDbContext _db;

    public UpdateInstitutionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(UpdateInstitutionCommand req, CancellationToken ct)
    {
        var institution = await _db.Institutions
            .FirstOrDefaultAsync(i => i.Id == req.Id && i.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Institution {req.Id} not found.");

        institution.Update(
            req.Name, req.InstitutionTypeId,
            req.City, req.District,
            req.ContactName, req.ContactPhone, req.ContactEmail,
            req.UpdatedBy);

        await _db.SaveChangesAsync(ct);
    }
}

// ── DeleteInstitutionCommand ──────────────────────────────────────────────────

public record DeleteInstitutionCommand(Guid Id, Guid? DeletedBy = null) : IRequest;

public sealed class DeleteInstitutionCommandHandler : IRequestHandler<DeleteInstitutionCommand>
{
    private readonly IAppDbContext _db;

    public DeleteInstitutionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteInstitutionCommand req, CancellationToken ct)
    {
        var institution = await _db.Institutions
            .FirstOrDefaultAsync(i => i.Id == req.Id && i.DeletedAt == null, ct)
            ?? throw new KeyNotFoundException($"Institution {req.Id} not found.");

        var hasActivePlans = await _db.ConsultancyPlans
            .AnyAsync(p => p.InstitutionId == req.Id
                        && p.Status == "active", ct);

        if (hasActivePlans)
            throw new InvalidOperationException(
                "Cannot delete an institution with active consultancy plans.");

        institution.SoftDelete(req.DeletedBy);
        await _db.SaveChangesAsync(ct);
    }
}
