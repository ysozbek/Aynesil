using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Legal.Dtos;
using Aynesil.Domain.Modules.Legal.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Legal.Commands;

// ── GrantConsentCommand ───────────────────────────────────────────────────────

public record GrantConsentCommand(
    Guid CorporationId,
    Guid StudentId,
    Guid? GuardianId,
    Guid? TemplateId,
    Guid? ConsentTypeId,
    DateOnly? ValidUntil,
    Guid? EvidenceFileId,
    Guid? CreatedBy = null) : IRequest<StudentConsentDto>;

public class GrantConsentCommandValidator : AbstractValidator<GrantConsentCommand>
{
    public GrantConsentCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.TemplateId.HasValue || x.ConsentTypeId.HasValue)
            .WithMessage("Either TemplateId or ConsentTypeId must be provided.");
    }
}

public sealed class GrantConsentCommandHandler
    : IRequestHandler<GrantConsentCommand, StudentConsentDto>
{
    private readonly IAppDbContext _db;

    public GrantConsentCommandHandler(IAppDbContext db) => _db = db;

    public async Task<StudentConsentDto> Handle(GrantConsentCommand req, CancellationToken ct)
    {
        var studentExists = await _db.Students
            .AnyAsync(s => s.Id == req.StudentId && s.DeletedAt == null, ct);

        if (!studentExists)
            throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        int? templateVersion = null;
        Guid? resolvedConsentTypeId = req.ConsentTypeId;

        if (req.TemplateId.HasValue)
        {
            var tpl = await _db.ConsentTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == req.TemplateId.Value, ct)
                ?? throw new KeyNotFoundException($"Consent template {req.TemplateId} not found.");

            if (!tpl.IsCurrent)
                throw new InvalidOperationException(
                    "Consents may only be granted against the current version of a template.");

            templateVersion          = tpl.Version;
            resolvedConsentTypeId ??= tpl.ConsentTypeId;
        }

        var consent = StudentConsent.Grant(
            req.CorporationId, req.StudentId,
            req.GuardianId,
            req.TemplateId, templateVersion,
            resolvedConsentTypeId,
            req.ValidUntil, req.EvidenceFileId, req.CreatedBy);

        _db.StudentConsents.Add(consent);
        await _db.SaveChangesAsync(ct);

        return await BuildDtoAsync(consent.Id, ct);
    }

    internal async Task<StudentConsentDto> BuildDtoAsync(Guid id, CancellationToken ct)
    {
        var c = await _db.StudentConsents.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == id, ct);

        var studentName = await _db.Students.AsNoTracking()
            .Where(s => s.Id == c.StudentId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync(ct);

        string? typeCode = null;
        if (c.ConsentTypeId.HasValue)
            typeCode = await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == c.ConsentTypeId.Value)
                .Select(r => r.Code)
                .FirstOrDefaultAsync(ct);

        string? templateCode = null;
        if (c.TemplateId.HasValue)
            templateCode = await _db.ConsentTemplates.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(t => t.Id == c.TemplateId.Value)
                .Select(t => t.Code)
                .FirstOrDefaultAsync(ct);

        return new StudentConsentDto(
            c.Id, c.CorporationId, c.StudentId, studentName,
            c.ConsentTypeId, typeCode,
            c.TemplateId, templateCode, c.TemplateVersion,
            c.GuardianId, c.State,
            c.GrantedAt, c.WithdrawnAt, c.ValidUntil, c.EvidenceFileId,
            c.CreatedAt, c.CreatedBy, c.UpdatedAt, c.RowVersion);
    }
}

// ── WithdrawConsentCommand ────────────────────────────────────────────────────

public record WithdrawConsentCommand(Guid Id, Guid? UpdatedBy = null) : IRequest;

public sealed class WithdrawConsentCommandHandler : IRequestHandler<WithdrawConsentCommand>
{
    private readonly IAppDbContext _db;

    public WithdrawConsentCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(WithdrawConsentCommand req, CancellationToken ct)
    {
        var consent = await _db.StudentConsents
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student consent {req.Id} not found.");

        consent.Withdraw(req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}

// ── AttachConsentEvidenceCommand ──────────────────────────────────────────────

public record AttachConsentEvidenceCommand(
    Guid Id,
    Guid EvidenceFileId,
    Guid? UpdatedBy = null) : IRequest;

public class AttachConsentEvidenceCommandValidator
    : AbstractValidator<AttachConsentEvidenceCommand>
{
    public AttachConsentEvidenceCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.EvidenceFileId).NotEmpty();
    }
}

public sealed class AttachConsentEvidenceCommandHandler
    : IRequestHandler<AttachConsentEvidenceCommand>
{
    private readonly IAppDbContext _db;

    public AttachConsentEvidenceCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(AttachConsentEvidenceCommand req, CancellationToken ct)
    {
        var consent = await _db.StudentConsents
            .FirstOrDefaultAsync(c => c.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Student consent {req.Id} not found.");

        var fileExists = await _db.FileObjects
            .AnyAsync(f => f.Id == req.EvidenceFileId, ct);

        if (!fileExists)
            throw new KeyNotFoundException($"File {req.EvidenceFileId} not found.");

        consent.AttachEvidence(req.EvidenceFileId, req.UpdatedBy);
        await _db.SaveChangesAsync(ct);
    }
}
