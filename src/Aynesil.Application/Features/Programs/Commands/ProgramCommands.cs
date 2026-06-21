using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Commands;

// ── CreateProgramCommand ──────────────────────────────────────────────────────

public record CreateProgramCommand(
    Guid CorporationId,
    string Code,
    string Name,
    Guid? ProgramTypeId,
    string? Description) : IRequest<ProgramDto>;

public class CreateProgramCommandValidator : AbstractValidator<CreateProgramCommand>
{
    public CreateProgramCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
    }
}

public sealed class CreateProgramCommandHandler : IRequestHandler<CreateProgramCommand, ProgramDto>
{
    private readonly IAppDbContext _db;

    public CreateProgramCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ProgramDto> Handle(CreateProgramCommand req, CancellationToken ct)
    {
        var codeTaken = await _db.EducationPrograms.AnyAsync(
            p => p.CorporationId == req.CorporationId && p.Code == req.Code, ct);
        if (codeTaken)
            throw new InvalidOperationException(
                $"Program code '{req.Code}' is already in use within this corporation.");

        if (req.ProgramTypeId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == req.ProgramTypeId.Value && r.RefType!.Code == "program_type", ct);
            if (!valid)
                throw new KeyNotFoundException($"Invalid program_type ref_value: {req.ProgramTypeId}");
        }

        var program = EducationProgram.Create(
            req.CorporationId, req.Code, req.Name, req.ProgramTypeId, req.Description);

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
            ?? throw new KeyNotFoundException($"Program {req.Id} not found.");

        if (program.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Program was modified by another user. Please refresh and retry.");

        var codeTaken = await _db.EducationPrograms.AnyAsync(
            p => p.CorporationId == program.CorporationId
              && p.Code == req.Code && p.Id != req.Id, ct);
        if (codeTaken)
            throw new InvalidOperationException(
                $"Program code '{req.Code}' is already in use within this corporation.");

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
            ?? throw new KeyNotFoundException($"Program {req.Id} not found.");

        var hasActiveAssignments = await _db.StudentPrograms.AnyAsync(
            sp => sp.ProgramId == req.Id && sp.Status == "active" && sp.DeletedAt == null, ct);
        if (hasActiveAssignments)
            throw new InvalidOperationException(
                "Cannot delete a program with active student assignments. Deactivate it instead.");

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
            throw new KeyNotFoundException($"Locale '{req.Locale}' is not supported.");

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
                throw new KeyNotFoundException($"Program {req.ProgramId} not found.");

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
