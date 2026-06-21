using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Scheduling.Dtos;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Scheduling.Commands;

// ── RequestMakeupCommand ──────────────────────────────────────────────────────

public record RequestMakeupCommand(
    Guid CorporationId,
    Guid StudentId,
    Guid? MissedSessionId,
    Guid? MissedReasonId,
    string? Note,
    DateOnly? ExpiresOn) : IRequest<MakeupRequestDto>;

public class RequestMakeupCommandValidator : AbstractValidator<RequestMakeupCommand>
{
    public RequestMakeupCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(2000).When(x => x.Note != null);
    }
}

public sealed class RequestMakeupCommandHandler : IRequestHandler<RequestMakeupCommand, MakeupRequestDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RequestMakeupCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<MakeupRequestDto> Handle(RequestMakeupCommand req, CancellationToken ct)
    {
        var makeup = MakeupRequest.Create(
            req.CorporationId, req.StudentId,
            req.MissedSessionId, req.MissedReasonId,
            req.Note, req.ExpiresOn, _currentUser.UserId);

        _db.MakeupRequests.Add(makeup);
        await _db.SaveChangesAsync(ct);

        return await LoadDtoAsync(_db, makeup.Id, ct);
    }

    internal static async Task<MakeupRequestDto> LoadDtoAsync(
        IAppDbContext db, Guid id, CancellationToken ct)
    {
        var m = await db.MakeupRequests.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new KeyNotFoundException($"MakeupRequest {id} not found.");

        var student = await db.Students.AsNoTracking()
            .Where(s => s.Id == m.StudentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync(ct);

        var name = student is null ? "" : $"{student.FirstName} {student.LastName}".Trim();
        return SchedulingProjection.ToMakeupDto(m, name);
    }
}

// ── ApproveMakeupCommand ──────────────────────────────────────────────────────

public record ApproveMakeupCommand(Guid Id, int RowVersion) : IRequest<MakeupRequestDto>;

public sealed class ApproveMakeupCommandHandler : IRequestHandler<ApproveMakeupCommand, MakeupRequestDto>
{
    private readonly IAppDbContext _db;

    public ApproveMakeupCommandHandler(IAppDbContext db) => _db = db;

    public async Task<MakeupRequestDto> Handle(ApproveMakeupCommand req, CancellationToken ct)
    {
        var makeup = await _db.MakeupRequests.FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"MakeupRequest {req.Id} not found.");

        if (makeup.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Makeup request was modified by another user. Please refresh and retry.");

        makeup.Approve();
        await _db.SaveChangesAsync(ct);
        return await RequestMakeupCommandHandler.LoadDtoAsync(_db, makeup.Id, ct);
    }
}

// ── RejectMakeupCommand ───────────────────────────────────────────────────────

public record RejectMakeupCommand(Guid Id, string? Note, int RowVersion) : IRequest<MakeupRequestDto>;

public sealed class RejectMakeupCommandHandler : IRequestHandler<RejectMakeupCommand, MakeupRequestDto>
{
    private readonly IAppDbContext _db;

    public RejectMakeupCommandHandler(IAppDbContext db) => _db = db;

    public async Task<MakeupRequestDto> Handle(RejectMakeupCommand req, CancellationToken ct)
    {
        var makeup = await _db.MakeupRequests.FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"MakeupRequest {req.Id} not found.");

        if (makeup.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Makeup request was modified by another user. Please refresh and retry.");

        makeup.Reject(req.Note);
        await _db.SaveChangesAsync(ct);
        return await RequestMakeupCommandHandler.LoadDtoAsync(_db, makeup.Id, ct);
    }
}

// ── AssignMakeupSessionCommand ────────────────────────────────────────────────

public record AssignMakeupSessionCommand(
    Guid Id,
    Guid MakeupSessionId,
    int RowVersion) : IRequest<MakeupRequestDto>;

public sealed class AssignMakeupSessionCommandHandler
    : IRequestHandler<AssignMakeupSessionCommand, MakeupRequestDto>
{
    private readonly IAppDbContext _db;

    public AssignMakeupSessionCommandHandler(IAppDbContext db) => _db = db;

    public async Task<MakeupRequestDto> Handle(AssignMakeupSessionCommand req, CancellationToken ct)
    {
        var makeup = await _db.MakeupRequests.FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"MakeupRequest {req.Id} not found.");

        if (makeup.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Makeup request was modified by another user. Please refresh and retry.");

        var sessionExists = await _db.Sessions.AnyAsync(s => s.Id == req.MakeupSessionId, ct);
        if (!sessionExists)
            throw new KeyNotFoundException($"Session {req.MakeupSessionId} not found.");

        makeup.AssignMakeupSession(req.MakeupSessionId);
        await _db.SaveChangesAsync(ct);
        return await RequestMakeupCommandHandler.LoadDtoAsync(_db, makeup.Id, ct);
    }
}

// ── CompleteMakeupCommand ─────────────────────────────────────────────────────

public record CompleteMakeupCommand(Guid Id, int RowVersion) : IRequest<MakeupRequestDto>;

public sealed class CompleteMakeupCommandHandler : IRequestHandler<CompleteMakeupCommand, MakeupRequestDto>
{
    private readonly IAppDbContext _db;

    public CompleteMakeupCommandHandler(IAppDbContext db) => _db = db;

    public async Task<MakeupRequestDto> Handle(CompleteMakeupCommand req, CancellationToken ct)
    {
        var makeup = await _db.MakeupRequests.FirstOrDefaultAsync(m => m.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"MakeupRequest {req.Id} not found.");

        if (makeup.RowVersion != req.RowVersion)
            throw new InvalidOperationException("Makeup request was modified by another user. Please refresh and retry.");

        makeup.MarkCompleted();
        await _db.SaveChangesAsync(ct);
        return await RequestMakeupCommandHandler.LoadDtoAsync(_db, makeup.Id, ct);
    }
}
