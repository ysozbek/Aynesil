using Aynesil.Application.Common.Interfaces;
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

    public AddAssessmentSectionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<AssessmentTemplateDto> Handle(
        AddAssessmentSectionCommand req, CancellationToken ct)
    {
        var template = await _db.AssessmentTemplates
            .FirstOrDefaultAsync(t => t.Id == req.TemplateId, ct)
            ?? throw new KeyNotFoundException($"Assessment template {req.TemplateId} not found.");

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

    public UpdateAssessmentSectionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<AssessmentSectionDto> Handle(
        UpdateAssessmentSectionCommand req, CancellationToken ct)
    {
        var section = await _db.AssessmentSections
            .FirstOrDefaultAsync(s => s.Id == req.SectionId, ct)
            ?? throw new KeyNotFoundException($"Assessment section {req.SectionId} not found.");

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

    public DeleteAssessmentSectionCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteAssessmentSectionCommand req, CancellationToken ct)
    {
        var section = await _db.AssessmentSections
            .FirstOrDefaultAsync(s => s.Id == req.SectionId, ct)
            ?? throw new KeyNotFoundException($"Assessment section {req.SectionId} not found.");

        _db.AssessmentSections.Remove(section);
        await _db.SaveChangesAsync(ct);
    }
}
