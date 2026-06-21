using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Educators.Dtos;
using Aynesil.Domain.Modules.Educators.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Educators.Commands;

// ── CreateEducatorCommand ─────────────────────────────────────────────────────

public record CreateEducatorCommand(
    Guid CorporationId,
    string FirstName,
    string LastName,
    Guid? TitleId,
    string? Email,
    string? Phone,
    string? EmploymentType,
    DateOnly? HireDate,
    Guid? PrimaryCampusId) : IRequest<EducatorDto>;

public class CreateEducatorCommandValidator : AbstractValidator<CreateEducatorCommand>
{
    public CreateEducatorCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.EmploymentType)
            .Must(v => v is null || new[] { "full_time", "part_time", "contractor" }.Contains(v))
            .WithMessage("EmploymentType must be full_time, part_time, or contractor.");
    }
}

public sealed class CreateEducatorCommandHandler : IRequestHandler<CreateEducatorCommand, EducatorDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateEducatorCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducatorDto> Handle(CreateEducatorCommand req, CancellationToken ct)
    {
        if (req.TitleId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == req.TitleId.Value && r.RefType!.Code == "educator_title", ct);
            if (!valid)
                throw new KeyNotFoundException($"Invalid educator_title ref_value: {req.TitleId}");
        }

        var educator = Educator.Create(
            req.CorporationId, req.FirstName, req.LastName,
            req.TitleId, req.Email, req.Phone,
            req.EmploymentType, req.HireDate, req.PrimaryCampusId,
            _currentUser.UserId);

        _db.Educators.Add(educator);
        await _db.SaveChangesAsync(ct);

        return (await EducatorProjection.LoadAsync(_db, educator.Id, ct))!;
    }
}

// ── UpdateEducatorCommand ─────────────────────────────────────────────────────

public record UpdateEducatorCommand(
    Guid Id,
    string FirstName,
    string LastName,
    Guid? TitleId,
    string? Email,
    string? Phone,
    string? EmploymentType,
    DateOnly? HireDate,
    Guid? PrimaryCampusId,
    int RowVersion) : IRequest<EducatorDto>;

public class UpdateEducatorCommandValidator : AbstractValidator<UpdateEducatorCommand>
{
    public UpdateEducatorCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => x.Email is not null);
        RuleFor(x => x.EmploymentType)
            .Must(v => v is null || new[] { "full_time", "part_time", "contractor" }.Contains(v))
            .WithMessage("EmploymentType must be full_time, part_time, or contractor.");
    }
}

public sealed class UpdateEducatorCommandHandler : IRequestHandler<UpdateEducatorCommand, EducatorDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateEducatorCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducatorDto> Handle(UpdateEducatorCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators.FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Educator {req.Id} not found.");

        if (educator.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "The educator record was modified by another user. Please refresh and retry.");

        if (req.TitleId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == req.TitleId.Value && r.RefType!.Code == "educator_title", ct);
            if (!valid)
                throw new KeyNotFoundException($"Invalid educator_title ref_value: {req.TitleId}");
        }

        educator.UpdateProfile(
            req.FirstName, req.LastName, req.TitleId,
            req.Email, req.Phone, req.EmploymentType,
            req.HireDate, req.PrimaryCampusId, _currentUser.UserId);

        await _db.SaveChangesAsync(ct);
        return (await EducatorProjection.LoadAsync(_db, educator.Id, ct))!;
    }
}

// ── DeleteEducatorCommand ─────────────────────────────────────────────────────

public record DeleteEducatorCommand(Guid Id) : IRequest;

public sealed class DeleteEducatorCommandHandler : IRequestHandler<DeleteEducatorCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteEducatorCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteEducatorCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators.FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Educator {req.Id} not found.");

        educator.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}

// ── ActivateEducatorCommand / DeactivateEducatorCommand ───────────────────────

public record ActivateEducatorCommand(Guid Id) : IRequest<EducatorDto>;

public sealed class ActivateEducatorCommandHandler : IRequestHandler<ActivateEducatorCommand, EducatorDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ActivateEducatorCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducatorDto> Handle(ActivateEducatorCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators.FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Educator {req.Id} not found.");

        educator.Activate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await EducatorProjection.LoadAsync(_db, educator.Id, ct))!;
    }
}

public record DeactivateEducatorCommand(Guid Id) : IRequest<EducatorDto>;

public sealed class DeactivateEducatorCommandHandler : IRequestHandler<DeactivateEducatorCommand, EducatorDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeactivateEducatorCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<EducatorDto> Handle(DeactivateEducatorCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators.FirstOrDefaultAsync(e => e.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"Educator {req.Id} not found.");

        educator.Deactivate(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
        return (await EducatorProjection.LoadAsync(_db, educator.Id, ct))!;
    }
}
