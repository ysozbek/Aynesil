using Aynesil.Application.Common.Localization;
using Aynesil.Application.Common.Validation;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Finance.Dtos;
using Aynesil.Domain.Modules.Finance.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Finance.Commands;

// ── GrantScholarshipCommand ───────────────────────────────────────────────────

public record GrantScholarshipCommand(
    Guid StudentId,
    Guid? ScholarshipTypeId,
    decimal? Percentage,
    decimal? Amount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? Note) : IRequest<ScholarshipDto>;

public class GrantScholarshipCommandValidator : AbstractValidator<GrantScholarshipCommand>
{
    public GrantScholarshipCommandValidator()
    {
        RuleFor(x => x.StudentId).NotEmpty();

        RuleFor(x => x)
            .Must(x => (x.Percentage.HasValue) ^ (x.Amount.HasValue))
            .WithKey(MsgKeys.Validation.ScholarshipPercentOrAmount);

        RuleFor(x => x.Percentage)
            .InclusiveBetween(0.01m, 100m)
            .When(x => x.Percentage.HasValue)
            .WithKey(MsgKeys.Validation.MustBeBetween, Msg.FieldArg("Percentage"), 0.01m, 100);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.ValidTo)
            .GreaterThanOrEqualTo(x => x.ValidFrom!.Value)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue)
            .WithKey(MsgKeys.Validation.MustNotBeBefore, Msg.FieldArg("ValidTo"), Msg.FieldArg("ValidFrom"));

        RuleFor(x => x.Note).MaximumLength(1000).When(x => x.Note is not null);
    }
}

public sealed class GrantScholarshipCommandHandler
    : IRequestHandler<GrantScholarshipCommand, ScholarshipDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;

    public GrantScholarshipCommandHandler(IAppDbContext db, ICurrentUserService currentUser, ITenantContext tenant)
    {
        _tenant = tenant;
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<ScholarshipDto> Handle(GrantScholarshipCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var student = await _db.Students.AsNoTracking()
            .Where(s => s.CorporationId == corporationId && s.Id == req.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Student {req.StudentId} not found.");

        var scholarship = Scholarship.Grant(
            corporationId, req.StudentId,
            req.ScholarshipTypeId,
            req.Percentage, req.Amount,
            req.ValidFrom, req.ValidTo,
            _currentUser.UserId, req.Note);

        _db.Scholarships.Add(scholarship);
        await _db.SaveChangesAsync(ct);

        var studentName = $"{student.FirstName} {student.LastName}".Trim();

        return new ScholarshipDto(
            scholarship.Id, scholarship.CorporationId,
            scholarship.StudentId, studentName,
            scholarship.ScholarshipTypeId,
            scholarship.Percentage, scholarship.Amount,
            scholarship.ValidFrom, scholarship.ValidTo,
            scholarship.ApprovedBy, scholarship.Note,
            scholarship.CreatedAt, scholarship.UpdatedAt, scholarship.RowVersion);
    }
}

// ── UpdateScholarshipCommand ──────────────────────────────────────────────────

public record UpdateScholarshipCommand(
    Guid Id,
    Guid? ScholarshipTypeId,
    decimal? Percentage,
    decimal? Amount,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    string? Note,
    int RowVersion) : IRequest<ScholarshipDto>;

public class UpdateScholarshipCommandValidator : AbstractValidator<UpdateScholarshipCommand>
{
    public UpdateScholarshipCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x)
            .Must(x => (x.Percentage.HasValue) ^ (x.Amount.HasValue))
            .WithKey(MsgKeys.Validation.ScholarshipPercentOrAmount);

        RuleFor(x => x.Percentage)
            .InclusiveBetween(0.01m, 100m)
            .When(x => x.Percentage.HasValue)
            .WithKey(MsgKeys.Validation.MustBeBetween, Msg.FieldArg("Percentage"), 0.01m, 100);

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .When(x => x.Amount.HasValue);
    }
}

public sealed class UpdateScholarshipCommandHandler
    : IRequestHandler<UpdateScholarshipCommand, ScholarshipDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateScholarshipCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db          = db;
        _currentUser = currentUser;
    }

    public async Task<ScholarshipDto> Handle(UpdateScholarshipCommand req, CancellationToken ct)
    {
        var scholarship = await _db.Scholarships
            .FirstOrDefaultAsync(s => s.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Scholarship {req.Id} not found.");

        if (scholarship.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(
                "Scholarship was modified by another user. Please refresh and retry.");

        scholarship.Update(
            req.ScholarshipTypeId,
            req.Percentage, req.Amount,
            req.ValidFrom, req.ValidTo,
            _currentUser.UserId, req.Note);

        await _db.SaveChangesAsync(ct);

        var student = await _db.Students.AsNoTracking()
            .Where(s => s.Id == scholarship.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);

        var studentName = student is null
            ? "" : $"{student.FirstName} {student.LastName}".Trim();

        return new ScholarshipDto(
            scholarship.Id, scholarship.CorporationId,
            scholarship.StudentId, studentName,
            scholarship.ScholarshipTypeId,
            scholarship.Percentage, scholarship.Amount,
            scholarship.ValidFrom, scholarship.ValidTo,
            scholarship.ApprovedBy, scholarship.Note,
            scholarship.CreatedAt, scholarship.UpdatedAt, scholarship.RowVersion);
    }
}
