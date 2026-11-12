using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Commands;

// ── CreateProgramCommand ──────────────────────────────────────────────────────

public record CreateProgramCommand(
    string Code,
    string Name,
    Guid? ProgramTypeId,
    string? Description) : IRequest<ProgramDto>;

public class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
    }
}

public sealed class CreateProgramCommandHandler : IRequestHandler<CreateProgramCommand, ProgramDto>
{
    private readonly IAppDbContext _db;
    private readonly ITenantContext _tenant;

    public CreateProgramCommandHandler(IAppDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task<ProgramDto> Handle(CreateProgramCommand req, CancellationToken ct)
    {
        var corporationId = _tenant.RequireCorporationId();

        var codeTaken = await _db.EducationPrograms.AnyAsync(
            p => p.CorporationId == corporationId && p.Code == req.Code, ct);
        if (codeTaken)
            throw new BusinessRuleViolationException(MsgKeys.Rule.CodeAlreadyExists, new EnumValue("Entity", "Program"), req.Code);

        if (req.ProgramTypeId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == req.ProgramTypeId.Value && r.RefType!.Code == "program_type", ct);
            if (!valid)
                throw new NotFoundException(MsgKeys.Rule.InvalidRefValue, new EnumValue("RefType", "program_type"), req.ProgramTypeId);
        }

        var program = EducationProgram.Create(
            corporationId, req.Code, req.Name, req.ProgramTypeId, req.Description);

        _db.EducationPrograms.Add(program);
        await _db.SaveChangesAsync(ct);

        return (await ProgramProjection.LoadAsync(_db, program.Id, ct))!;
    }
}

// ── UpdateProgramCommand ──────────────────────────────────────────────────────

public record UpdateProgramCommand(
    Guid Id,
    string Code,
    string Name,
    Guid? ProgramTypeId,
    string? Description,
    int RowVersion) : IRequest<ProgramDto>;

public class UpdateProgramCommandValidator : AbstractValidator<UpdateProgramCommand>
{
    public UpdateProgramCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpdateProgramCommandHandler : IRequestHandler<UpdateProgramCommand, ProgramDto>
{
    private readonly IAppDbContext _db;

    public UpdateProgramCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ProgramDto> Handle(UpdateProgramCommand req, CancellationToken ct)
    {
        var program = await _db.EducationPrograms.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Program"));

        if (program.RowVersion != req.RowVersion)
            throw new BusinessRuleViolationException(MsgKeys.Error.Concurrency);

        var codeTaken = await _db.EducationPrograms.AnyAsync(
            p => p.CorporationId == program.CorporationId
              && p.Code == req.Code && p.Id != req.Id, ct);
        if (codeTaken)
            throw new BusinessRuleViolationException(MsgKeys.Rule.CodeAlreadyExists, new EnumValue("Entity", "Program"), req.Code);

        program.Update(req.Code, req.Name, req.ProgramTypeId, req.Description);
        await _db.SaveChangesAsync(ct);

        return (await ProgramProjection.LoadAsync(_db, program.Id, ct))!;
    }
}

// ── DeleteProgramCommand ──────────────────────────────────────────────────────

public record DeleteProgramCommand(Guid Id) : IRequest;

public sealed class DeleteProgramCommandHandler : IRequestHandler<DeleteProgramCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteProgramCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteProgramCommand req, CancellationToken ct)
    {
        var program = await _db.EducationPrograms.FirstOrDefaultAsync(p => p.Id == req.Id, ct)
            ?? throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Program"));

        var hasActiveAssignments = await _db.StudentPrograms.AnyAsync(
            sp => sp.ProgramId == req.Id && sp.Status == "active" && sp.DeletedAt == null, ct);
        if (hasActiveAssignments)
            throw new BusinessRuleViolationException(MsgKeys.Rule.ProgramHasAssignments);

        program.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── SetProgramTranslationCommand ──────────────────────────────────────────────

public record SetProgramTranslationCommand(
    Guid ProgramId,
    string Locale,
    string Name,
    string? Description) : IRequest<ProgramTranslationDto>;

public class SetProgramTranslationCommandValidator : AbstractValidator<SetProgramTranslationCommand>
{
    public SetProgramTranslationCommandValidator()
    {
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.Locale).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
    }
}

public sealed class SetProgramTranslationCommandHandler
    : IRequestHandler<SetProgramTranslationCommand, ProgramTranslationDto>
{
    private readonly IAppDbContext _db;

    public SetProgramTranslationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ProgramTranslationDto> Handle(SetProgramTranslationCommand req, CancellationToken ct)
    {
        var localeExists = await _db.Locales.AnyAsync(l => l.Code == req.Locale, ct);
        if (!localeExists)
            throw new NotFoundException(MsgKeys.Rule.LocaleNotSupported, req.Locale);

        var existing = await _db.ProgramTranslations
            .FirstOrDefaultAsync(t => t.ProgramId == req.ProgramId && t.Locale == req.Locale, ct);

        if (existing is not null)
        {
            existing.Name        = req.Name;
            existing.Description = req.Description;
        }
        else
        {
            var programExists = await _db.EducationPrograms.AnyAsync(p => p.Id == req.ProgramId, ct);
            if (!programExists)
                throw new NotFoundException(MsgKeys.Error.NotFound, new EnumValue("Entity", "Program"));

            _db.ProgramTranslations.Add(new ProgramTranslation
            {
                ProgramId   = req.ProgramId,
                Locale      = req.Locale,
                Name        = req.Name,
                Description = req.Description
            });
        }

        await _db.SaveChangesAsync(ct);
        return new ProgramTranslationDto(req.Locale, req.Name, req.Description);
    }
}
