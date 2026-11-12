using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Domain.Modules.Assessment.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Add section ───────────────────────────────────────────────────────────────

public record AddAssessmentSectionCommand(
    Guid TemplateId,
    string Code,
    int SortOrder,
    Guid? DevelopmentAreaId) : IRequest<AssessmentTemplateDto>;

public class AddAssessmentSectionCommandValidator : AbstractValidator<AddAssessmentSectionCommand>
{
    public AddAssessmentSectionCommandValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddAssessmentSectionCommandHandler
    : IRequestHandler<AddAssessmentSectionCommand, AssessmentTemplateDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public AddAssessmentSectionCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AssessmentTemplateDto> Handle(
        AddAssessmentSectionCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var template = await _db.AssessmentTemplates
            .FirstOrDefaultAsync(t => t.Id == req.TemplateId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "AssessmentTemplate"));
        // Global şablonlar RLS ile her kiracıya görünür; bölüm eklemek hepsini değiştirirdi (Y3).
        TenantOwnership.EnsureWritable(template.CorporationId, corporationId, "AssessmentTemplate");

        var section = AssessmentSection.Create(
            req.TemplateId, req.Code, req.SortOrder, req.DevelopmentAreaId);

        _db.AssessmentSections.Add(section);
        await _db.SaveChangesAsync(ct);

        return (await AssessmentProjection.LoadTemplateAsync(_db, template.Id, ct))!;
    }
}

// ── Update section ────────────────────────────────────────────────────────────

public record UpdateAssessmentSectionCommand(
    Guid SectionId,
    string Code,
    int SortOrder,
    Guid? DevelopmentAreaId) : IRequest<AssessmentSectionDto>;

public class UpdateAssessmentSectionCommandValidator : AbstractValidator<UpdateAssessmentSectionCommand>
{
    public UpdateAssessmentSectionCommandValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateAssessmentSectionCommandHandler
    : IRequestHandler<UpdateAssessmentSectionCommand, AssessmentSectionDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public UpdateAssessmentSectionCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<AssessmentSectionDto> Handle(
        UpdateAssessmentSectionCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        // assessment_section has no corporation_id: the template decides the tenant (Y3).
        var section = await _db.AssessmentSections
            .Include(s => s.Template)
            .FirstOrDefaultAsync(s => s.Id == req.SectionId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "AssessmentSection"));
        TenantOwnership.EnsureWritable(section.Template.CorporationId, corporationId, "AssessmentTemplate");

        section.Update(req.Code, req.SortOrder, req.DevelopmentAreaId);
        await _db.SaveChangesAsync(ct);

        return new AssessmentSectionDto(
            section.Id, section.TemplateId, section.Code, section.SortOrder,
            section.DevelopmentAreaId, null, []);
    }
}

// ── Delete section ────────────────────────────────────────────────────────────

public record DeleteAssessmentSectionCommand(Guid SectionId) : IRequest;

public sealed class DeleteAssessmentSectionCommandHandler
    : IRequestHandler<DeleteAssessmentSectionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public DeleteAssessmentSectionCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task Handle(DeleteAssessmentSectionCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        // assessment_section has no corporation_id: the template decides the tenant (Y3).
        var section = await _db.AssessmentSections
            .Include(s => s.Template)
            .FirstOrDefaultAsync(s => s.Id == req.SectionId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "AssessmentSection"));
        TenantOwnership.EnsureWritable(section.Template.CorporationId, corporationId, "AssessmentTemplate");

        _db.AssessmentSections.Remove(section);
        await _db.SaveChangesAsync(ct);
    }
}
