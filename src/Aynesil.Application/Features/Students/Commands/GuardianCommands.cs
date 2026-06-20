using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Students.Dtos;
using Aynesil.Domain.Modules.Students.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Students.Commands;

// ── CreateGuardianCommand ─────────────────────────────────────────────────────

public record CreateGuardianCommand(
    Guid CorporationId,
    string FirstName,
    string LastName,
    string? NationalId,
    string? Email,
    string? Phone,
    string? Occupation,
    string? AddressLine) : IRequest<GuardianDto>;

public class CreateGuardianCommandValidator : AbstractValidator<CreateGuardianCommand>
{
    public CreateGuardianCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}

public sealed class CreateGuardianCommandHandler
    : IRequestHandler<CreateGuardianCommand, GuardianDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateGuardianCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GuardianDto> Handle(CreateGuardianCommand req, CancellationToken ct)
    {
        var guardian = Guardian.Create(
            req.CorporationId, req.FirstName, req.LastName,
            req.NationalId, req.Email, req.Phone, req.Occupation, req.AddressLine,
            _currentUser.UserId);

        _db.Guardians.Add(guardian);
        await _db.SaveChangesAsync(ct);

        return (await StudentProjection.LoadGuardianAsync(_db, guardian.Id, ct))!;
    }
}

// ── UpdateGuardianCommand ─────────────────────────────────────────────────────

public record UpdateGuardianCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? NationalId,
    string? Email,
    string? Phone,
    string? Occupation,
    string? AddressLine,
    int RowVersion) : IRequest<GuardianDto>;

public class UpdateGuardianCommandValidator : AbstractValidator<UpdateGuardianCommand>
{
    public UpdateGuardianCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
    }
}

public sealed class UpdateGuardianCommandHandler
    : IRequestHandler<UpdateGuardianCommand, GuardianDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateGuardianCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<GuardianDto> Handle(UpdateGuardianCommand req, CancellationToken ct)
    {
        var guardian = await _db.Guardians
            .FirstOrDefaultAsync(g => g.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Guardian {req.Id} not found.");

        if (guardian.RowVersion != req.RowVersion)
            throw new InvalidOperationException("The guardian record was modified by another user. Please refresh and retry.");

        guardian.UpdateContactInfo(
            req.FirstName, req.LastName, req.NationalId,
            req.Email, req.Phone, req.Occupation, req.AddressLine,
            _currentUser.UserId);

        await _db.SaveChangesAsync(ct);

        return (await StudentProjection.LoadGuardianAsync(_db, guardian.Id, ct))!;
    }
}

// ── DeleteGuardianCommand ─────────────────────────────────────────────────────

public record DeleteGuardianCommand(Guid Id) : IRequest;

public sealed class DeleteGuardianCommandHandler : IRequestHandler<DeleteGuardianCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteGuardianCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteGuardianCommand req, CancellationToken ct)
    {
        var guardian = await _db.Guardians
            .FirstOrDefaultAsync(g => g.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Guardian {req.Id} not found.");

        guardian.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
