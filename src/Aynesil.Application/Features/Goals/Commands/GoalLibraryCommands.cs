using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Goals.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Goals.Commands;

// ── CreateGoalLibraryCommand ──────────────────────────────────────────────────

public record CreateGoalLibraryCommand(
    Guid? CorporationId,
    string Name,
    string? Description) : IRequest<GoalLibraryDto>;

public class CreateGoalLibraryCommandValidator : AbstractValidator<CreateGoalLibraryCommand>
{
    public CreateGoalLibraryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
    }
}

public sealed class CreateGoalLibraryCommandHandler
    : IRequestHandler<CreateGoalLibraryCommand, GoalLibraryDto>
{
    private readonly IAppDbContext _db;

    public CreateGoalLibraryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GoalLibraryDto> Handle(CreateGoalLibraryCommand req, CancellationToken ct)
    {
        var library = GoalLibrary.Create(req.CorporationId, req.Name, req.Description);

        _db.GoalLibraries.Add(library);
        await _db.SaveChangesAsync(ct);

        var templateCount = await _db.GoalTemplates.CountAsync(
            t => t.LibraryId == library.Id, ct);

        return new GoalLibraryDto(
            library.Id, library.CorporationId, library.Name, library.Description,
            templateCount, library.CreatedAt, library.UpdatedAt, library.RowVersion);
    }
}

// ── UpdateGoalLibraryCommand ──────────────────────────────────────────────────

public record UpdateGoalLibraryCommand(
    Guid Id,
    string Name,
    string? Description,
    int RowVersion) : IRequest<GoalLibraryDto>;

public class UpdateGoalLibraryCommandValidator : AbstractValidator<UpdateGoalLibraryCommand>
{
    public UpdateGoalLibraryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
    }
}

public sealed class UpdateGoalLibraryCommandHandler
    : IRequestHandler<UpdateGoalLibraryCommand, GoalLibraryDto>
{
    private readonly IAppDbContext _db;

    public UpdateGoalLibraryCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GoalLibraryDto> Handle(UpdateGoalLibraryCommand req, CancellationToken ct)
    {
        var library = await _db.GoalLibraries.FirstOrDefaultAsync(l => l.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"GoalLibrary {req.Id} not found.");

        if (library.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "The library was modified by another user. Please refresh and retry.");

        library.Update(req.Name, req.Description);
        await _db.SaveChangesAsync(ct);

        var templateCount = await _db.GoalTemplates.CountAsync(
            t => t.LibraryId == library.Id, ct);

        return new GoalLibraryDto(
            library.Id, library.CorporationId, library.Name, library.Description,
            templateCount, library.CreatedAt, library.UpdatedAt, library.RowVersion);
    }
}

// ── DeleteGoalLibraryCommand ──────────────────────────────────────────────────

public record DeleteGoalLibraryCommand(Guid Id) : IRequest;

public sealed class DeleteGoalLibraryCommandHandler : IRequestHandler<DeleteGoalLibraryCommand>
{
    private readonly IAppDbContext _db;

    public DeleteGoalLibraryCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteGoalLibraryCommand req, CancellationToken ct)
    {
        var library = await _db.GoalLibraries.FirstOrDefaultAsync(l => l.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"GoalLibrary {req.Id} not found.");

        _db.GoalLibraries.Remove(library);
        await _db.SaveChangesAsync(ct);
    }
}
