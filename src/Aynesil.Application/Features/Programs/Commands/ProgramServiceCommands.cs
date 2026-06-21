using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Programs.Dtos;
using Aynesil.Domain.Modules.Education.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Programs.Commands;

// ── AddProgramServiceCommand ──────────────────────────────────────────────────

public record AddProgramServiceCommand(
    Guid ProgramId,
    string Name,
    Guid? ServiceTypeId,
    int? DefaultDurationMinutes,
    decimal? DefaultSessionsPerWeek,
    int SortOrder) : IRequest<ProgramServiceDto>;

public class AddProgramServiceCommandValidator : AbstractValidator<AddProgramServiceCommand>
{
    public AddProgramServiceCommandValidator()
    {
        RuleFor(x => x.ProgramId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DefaultDurationMinutes).GreaterThan(0).When(x => x.DefaultDurationMinutes.HasValue);
        RuleFor(x => x.DefaultSessionsPerWeek).GreaterThan(0).When(x => x.DefaultSessionsPerWeek.HasValue);
    }
}

public sealed class AddProgramServiceCommandHandler
    : IRequestHandler<AddProgramServiceCommand, ProgramServiceDto>
{
    private readonly IAppDbContext _db;

    public AddProgramServiceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ProgramServiceDto> Handle(AddProgramServiceCommand req, CancellationToken ct)
    {
        var program = await _db.EducationPrograms.FirstOrDefaultAsync(p => p.Id == req.ProgramId, ct)
            ?? throw new KeyNotFoundException($"Program {req.ProgramId} not found.");

        if (req.ServiceTypeId.HasValue)
        {
            var valid = await _db.RefValues.AnyAsync(
                r => r.Id == req.ServiceTypeId.Value && r.RefType!.Code == "service_type", ct);
            if (!valid)
                throw new KeyNotFoundException($"Invalid service_type ref_value: {req.ServiceTypeId}");
        }

        var service = new ProgramService
        {
            CorporationId          = program.CorporationId,
            ProgramId              = req.ProgramId,
            Name                   = req.Name,
            ServiceTypeId          = req.ServiceTypeId,
            DefaultDurationMinutes = req.DefaultDurationMinutes,
            DefaultSessionsPerWeek = req.DefaultSessionsPerWeek,
            SortOrder              = req.SortOrder
        };

        _db.ProgramServices.Add(service);
        await _db.SaveChangesAsync(ct);

        var stLabel = req.ServiceTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.ServiceTypeId.Value)
                .Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        return new ProgramServiceDto(
            service.Id, service.ServiceTypeId, stLabel, service.Name,
            service.DefaultDurationMinutes, service.DefaultSessionsPerWeek, service.SortOrder);
    }
}

// ── UpdateProgramServiceCommand ───────────────────────────────────────────────

public record UpdateProgramServiceCommand(
    Guid ServiceId,
    string Name,
    Guid? ServiceTypeId,
    int? DefaultDurationMinutes,
    decimal? DefaultSessionsPerWeek,
    int SortOrder) : IRequest<ProgramServiceDto>;

public class UpdateProgramServiceCommandValidator : AbstractValidator<UpdateProgramServiceCommand>
{
    public UpdateProgramServiceCommandValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DefaultDurationMinutes).GreaterThan(0).When(x => x.DefaultDurationMinutes.HasValue);
    }
}

public sealed class UpdateProgramServiceCommandHandler
    : IRequestHandler<UpdateProgramServiceCommand, ProgramServiceDto>
{
    private readonly IAppDbContext _db;

    public UpdateProgramServiceCommandHandler(IAppDbContext db) => _db = db;

    public async Task<ProgramServiceDto> Handle(UpdateProgramServiceCommand req, CancellationToken ct)
    {
        var service = await _db.ProgramServices.FirstOrDefaultAsync(s => s.Id == req.ServiceId, ct)
            ?? throw new KeyNotFoundException($"Program service {req.ServiceId} not found.");

        service.Name                   = req.Name;
        service.ServiceTypeId          = req.ServiceTypeId;
        service.DefaultDurationMinutes = req.DefaultDurationMinutes;
        service.DefaultSessionsPerWeek = req.DefaultSessionsPerWeek;
        service.SortOrder              = req.SortOrder;

        await _db.SaveChangesAsync(ct);

        var stLabel = req.ServiceTypeId.HasValue
            ? await _db.RefValues.AsNoTracking()
                .Where(r => r.Id == req.ServiceTypeId.Value)
                .Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        return new ProgramServiceDto(
            service.Id, service.ServiceTypeId, stLabel, service.Name,
            service.DefaultDurationMinutes, service.DefaultSessionsPerWeek, service.SortOrder);
    }
}

// ── DeleteProgramServiceCommand ───────────────────────────────────────────────

public record DeleteProgramServiceCommand(Guid ServiceId) : IRequest;

public sealed class DeleteProgramServiceCommandHandler : IRequestHandler<DeleteProgramServiceCommand>
{
    private readonly IAppDbContext _db;

    public DeleteProgramServiceCommandHandler(IAppDbContext db) => _db = db;

    public async Task Handle(DeleteProgramServiceCommand req, CancellationToken ct)
    {
        var service = await _db.ProgramServices.FirstOrDefaultAsync(s => s.Id == req.ServiceId, ct)
            ?? throw new KeyNotFoundException($"Program service {req.ServiceId} not found.");

        _db.ProgramServices.Remove(service);
        await _db.SaveChangesAsync(ct);
    }
}
