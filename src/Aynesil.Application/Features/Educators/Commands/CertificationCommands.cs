using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Educators.Dtos;
using Aynesil.Domain.Modules.Educators.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Educators.Commands;

// ── AddCertificationCommand ───────────────────────────────────────────────────

public record AddCertificationCommand(
    Guid EducatorId,
    string Name,
    Guid? CertificationTypeId,
    string? Issuer,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    Guid? FileId) : IRequest<EducatorCertificationDto>;

public class AddCertificationCommandValidator : AbstractValidator<AddCertificationCommand>
{
    public AddCertificationCommandValidator()
    {
        RuleFor(x => x.EducatorId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ExpiresOn)
            .GreaterThan(x => x.IssuedOn)
            .When(x => x.IssuedOn.HasValue && x.ExpiresOn.HasValue)
            .WithMessage("ExpiresOn must be after IssuedOn.");
    }
}

public sealed class AddCertificationCommandHandler
    : IRequestHandler<AddCertificationCommand, EducatorCertificationDto>
{
    private readonly IAppDbContext _db;

    public AddCertificationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducatorCertificationDto> Handle(AddCertificationCommand req, CancellationToken ct)
    {
        var educator = await _db.Educators.FirstOrDefaultAsync(e => e.Id == req.EducatorId, ct)
            ?? throw new KeyNotFoundException($"Educator {req.EducatorId} not found.");

        if (req.CertificationTypeId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == req.CertificationTypeId.Value && r.RefType!.Code == "certification_type", ct);
            if (!valid)
                throw new KeyNotFoundException(
                    $"Invalid certification_type ref_value: {req.CertificationTypeId}");
        }

        var cert = EducatorCertification.Create(
            educator.CorporationId, req.EducatorId, req.Name,
            req.CertificationTypeId, req.Issuer, req.IssuedOn, req.ExpiresOn, req.FileId);

        _db.EducatorCertifications.Add(cert);
        await _db.SaveChangesAsync(ct);

        return EducatorProjection.ToCertificationDto(cert);
    }
}

// ── UpdateCertificationCommand ────────────────────────────────────────────────

public record UpdateCertificationCommand(
    Guid CertificationId,
    string Name,
    Guid? CertificationTypeId,
    string? Issuer,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    Guid? FileId,
    int RowVersion) : IRequest<EducatorCertificationDto>;

public class UpdateCertificationCommandValidator : AbstractValidator<UpdateCertificationCommand>
{
    public UpdateCertificationCommandValidator()
    {
        RuleFor(x => x.CertificationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.ExpiresOn)
            .GreaterThan(x => x.IssuedOn)
            .When(x => x.IssuedOn.HasValue && x.ExpiresOn.HasValue)
            .WithMessage("ExpiresOn must be after IssuedOn.");
    }
}

public sealed class UpdateCertificationCommandHandler
    : IRequestHandler<UpdateCertificationCommand, EducatorCertificationDto>
{
    private readonly IAppDbContext _db;

    public UpdateCertificationCommandHandler(IAppDbContext db) => _db = db;

    public async Task<EducatorCertificationDto> Handle(UpdateCertificationCommand req, CancellationToken ct)
    {
        var cert = await _db.EducatorCertifications
            .FirstOrDefaultAsync(c => c.Id == req.CertificationId, ct)
            ?? throw new KeyNotFoundException($"Certification {req.CertificationId} not found.");

        if (cert.RowVersion != req.RowVersion)
            throw new InvalidOperationException(
                "Certification was modified by another user. Please refresh and retry.");

        cert.Update(req.Name, req.CertificationTypeId, req.Issuer,
            req.IssuedOn, req.ExpiresOn, req.FileId);

        await _db.SaveChangesAsync(ct);
        return EducatorProjection.ToCertificationDto(cert);
    }
}

// ── DeleteCertificationCommand ────────────────────────────────────────────────

public record DeleteCertificationCommand(Guid CertificationId) : IRequest;

public sealed class DeleteCertificationCommandHandler : IRequestHandler<DeleteCertificationCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeleteCertificationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteCertificationCommand req, CancellationToken ct)
    {
        var cert = await _db.EducatorCertifications
            .FirstOrDefaultAsync(c => c.Id == req.CertificationId, ct)
            ?? throw new KeyNotFoundException($"Certification {req.CertificationId} not found.");

        cert.SoftDelete(_currentUser.UserId);
        await _db.SaveChangesAsync(ct);
    }
}
