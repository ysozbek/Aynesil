using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Assessment.Dtos;
using Aynesil.Domain.Modules.Assessment.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Commands;

// ── Add item ──────────────────────────────────────────────────────────────────

public record AddAssessmentItemCommand(
    Guid SectionId,
    string Code,
    string Prompt,
    string ResponseType,
    string? Choices,
    decimal Weight,
    int SortOrder) : IRequest<AssessmentItemDto>;

public class AddAssessmentItemCommandValidator : AbstractValidator<AddAssessmentItemCommand>
{
    private static readonly string[] ValidTypes =
        ["numeric", "scale", "boolean", "text", "choice"];

    public AddAssessmentItemCommandValidator()
    {
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Prompt).NotEmpty();
        RuleFor(x => x.ResponseType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("ResponseType"), EnumSets.ResponseType);
        RuleFor(x => x.Weight).GreaterThan(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class AddAssessmentItemCommandHandler
    : IRequestHandler<AddAssessmentItemCommand, AssessmentItemDto>
{
    private readonly IAppDbContext _db;

    public AddAssessmentItemCommandHandler(IAppDbContext db) => _db = db;

    public async Task<AssessmentItemDto> Handle(
        AddAssessmentItemCommand req, CancellationToken ct)
    {
        var section = await _db.AssessmentSections
            .FirstOrDefaultAsync(s => s.Id == req.SectionId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "AssessmentSection"));

        var item = AssessmentItem.Create(
            req.SectionId, req.Code, req.Prompt,
            req.ResponseType, req.Choices, req.Weight, req.SortOrder);

        _db.AssessmentItems.Add(item);
        await _db.SaveChangesAsync(ct);

        return new AssessmentItemDto(
            item.Id, item.SectionId, item.Code, item.Prompt,
            item.ResponseType, item.Choices, item.Weight, item.SortOrder);
    }
}

// ── Update item ───────────────────────────────────────────────────────────────

public record UpdateAssessmentItemCommand(
    Guid ItemId,
    string Code,
    string Prompt,
    string ResponseType,
    string? Choices,
    decimal Weight,
    int SortOrder) : IRequest<AssessmentItemDto>;

public class UpdateAssessmentItemCommandValidator : AbstractValidator<UpdateAssessmentItemCommand>
{
    private static readonly string[] ValidTypes =
        ["numeric", "scale", "boolean", "text", "choice"];

    public UpdateAssessmentItemCommandValidator()
    {
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Prompt).NotEmpty();
        RuleFor(x => x.ResponseType)
            .NotEmpty()
            .Must(t => ValidTypes.Contains(t))
            .WithKey(MsgKeys.Validation.MustBeOneOf, Msg.FieldArg("ResponseType"), EnumSets.ResponseType);
        RuleFor(x => x.Weight).GreaterThan(0);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateAssessmentItemCommandHandler
    : IRequestHandler<UpdateAssessmentItemCommand, AssessmentItemDto>
{
    private readonly IAppDbContext _db;

    public UpdateAssessmentItemCommandHandler(IAppDbContext db) => _db = db;

    public async Task<AssessmentItemDto> Handle(
        UpdateAssessmentItemCommand req, CancellationToken ct)
    {
        var item = await _db.AssessmentItems
            .FirstOrDefaultAsync(i => i.Id == req.ItemId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "AssessmentItem"));

        item.Update(req.Code, req.Prompt, req.ResponseType, req.Choices, req.Weight, req.SortOrder);
        await _db.SaveChangesAsync(ct);

        return new AssessmentItemDto(
            item.Id, item.SectionId, item.Code, item.Prompt,
            item.ResponseType, item.Choices, item.Weight, item.SortOrder);
    }
}

// ── Delete item ───────────────────────────────────────────────────────────────

public record DeleteAssessmentItemCommand(Guid ItemId) : IRequest;

public sealed class DeleteAssessmentItemCommandHandler : IRequestHandler<DeleteAssessmentItemCommand>
{
    private readonly IAppDbContext _db;

    public DeleteAssessmentItemCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteAssessmentItemCommand req, CancellationToken ct)
    {
        var item = await _db.AssessmentItems
            .FirstOrDefaultAsync(i => i.Id == req.ItemId, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "AssessmentItem"));

        _db.AssessmentItems.Remove(item);
        await _db.SaveChangesAsync(ct);
    }
}
