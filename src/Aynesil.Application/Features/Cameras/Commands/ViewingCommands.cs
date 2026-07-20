using System.Net;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Media.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aynesil.Application.Features.Cameras.Commands;

// ── GrantViewingAuthorizationCommand ─────────────────────────────────────────

/// <summary>
/// Grants a guardian time-limited access to a student's camera feed.
/// Pre-conditions (all must pass):
///   1. legal.student_consent with state='granted', consent_type=camera_viewing, valid_until not past.
///   2. students.guardian_portal_access.can_view_camera = true.
/// ValidTo is mandatory — open-ended authorizations are not permitted.
/// </summary>
public record GrantViewingAuthorizationCommand(
    Guid CorporationId,
    Guid GuardianId,
    Guid StudentId,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    Guid? SessionId = null,
    Guid? ConsentId = null,
    Guid? AccessTypeId = null,
    Guid? GrantedBy = null) : IRequest<Guid>;

public class GrantViewingAuthorizationCommandValidator
    : AbstractValidator<GrantViewingAuthorizationCommand>
{
    public GrantViewingAuthorizationCommandValidator()
    {
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.GuardianId).NotEmpty();
        RuleFor(x => x.StudentId).NotEmpty();
        RuleFor(x => x.ValidFrom).NotEmpty();
        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom)
            .WithMessage("ValidTo must be after ValidFrom.");
    }
}

public sealed class GrantViewingAuthorizationCommandHandler
    : IRequestHandler<GrantViewingAuthorizationCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ILogger<GrantViewingAuthorizationCommandHandler> _logger;

    public GrantViewingAuthorizationCommandHandler(
        IAppDbContext db,
        ILogger<GrantViewingAuthorizationCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Guid> Handle(GrantViewingAuthorizationCommand req, CancellationToken ct)
    {
        // ── Gate 1: guardian_portal_access.can_view_camera ─────────────────────
        var portalAccess = await _db.GuardianPortalAccesses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                gpa => gpa.GuardianId == req.GuardianId
                    && gpa.StudentId  == req.StudentId
                    && gpa.RevokedAt  == null, ct);

        if (portalAccess == null || !portalAccess.CanViewCamera)
            throw new InvalidOperationException(
                "Guardian does not have camera viewing access enabled in the portal for this student.");

        // ── Gate 2: active camera_viewing consent ──────────────────────────────
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);

        var consentTypeId = await _db.RefValues
            .AsNoTracking()
            .Where(rv => rv.Code == "camera_viewing" && rv.DeletedAt == null)
            .Select(rv => (Guid?)rv.Id)
            .FirstOrDefaultAsync(ct);

        var hasConsent = await _db.StudentConsents
            .AsNoTracking()
            .AnyAsync(sc =>
                sc.CorporationId == req.CorporationId
                && sc.StudentId  == req.StudentId
                && sc.GuardianId == req.GuardianId
                && sc.State      == "granted"
                && (consentTypeId == null || sc.ConsentTypeId == consentTypeId)
                && (sc.ValidUntil == null || sc.ValidUntil >= today), ct);

        if (!hasConsent)
            throw new InvalidOperationException(
                "A valid camera_viewing consent (KVKK) is required before granting viewing authorization.");

        var authorization = ViewingAuthorization.Grant(
            req.CorporationId, req.GuardianId, req.StudentId,
            req.ValidFrom, req.ValidTo,
            req.SessionId, req.ConsentId, req.AccessTypeId, req.GrantedBy);

        _db.ViewingAuthorizations.Add(authorization);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Security: ViewingAuthorization {AuthId} granted — guardian={GuardianId} student={StudentId} " +
            "validFrom={From} validTo={To} grantedBy={GrantedBy}",
            authorization.Id, req.GuardianId, req.StudentId,
            req.ValidFrom, req.ValidTo, req.GrantedBy);

        return authorization.Id;
    }
}

// ── RevokeViewingAuthorizationCommand ─────────────────────────────────────────

public record RevokeViewingAuthorizationCommand(Guid Id, Guid? RevokedBy = null) : IRequest;

public class RevokeViewingAuthorizationCommandValidator
    : AbstractValidator<RevokeViewingAuthorizationCommand>
{
    public RevokeViewingAuthorizationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public sealed class RevokeViewingAuthorizationCommandHandler
    : IRequestHandler<RevokeViewingAuthorizationCommand>
{
    private readonly IAppDbContext _db;
    private readonly ILogger<RevokeViewingAuthorizationCommandHandler> _logger;

    public RevokeViewingAuthorizationCommandHandler(
        IAppDbContext db,
        ILogger<RevokeViewingAuthorizationCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(RevokeViewingAuthorizationCommand req, CancellationToken ct)
    {
        var auth = await _db.ViewingAuthorizations
            .FirstOrDefaultAsync(a => a.Id == req.Id, ct)
            ?? throw new KeyNotFoundException($"ViewingAuthorization {req.Id} not found.");

        auth.Revoke();
        await _db.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Security: ViewingAuthorization {AuthId} revoked by {RevokedBy}",
            req.Id, req.RevokedBy);
    }
}

// ── StartViewingSessionCommand ─────────────────────────────────────────────────

/// <summary>
/// Opens a viewing_log entry after validating that the authorization is currently valid
/// and the underlying consent has not been withdrawn.
/// Returns the composite key (Id, StartedAt) used to end the session later.
/// </summary>
public record StartViewingSessionCommand(
    Guid AuthorizationId,
    Guid CorporationId,
    Guid? GuardianId,
    Guid? UserId,
    Guid? SessionId,
    Guid? CameraId,
    string? IpAddress = null) : IRequest<StartViewingSessionResult>;

public record StartViewingSessionResult(long LogId, DateTimeOffset StartedAt);

public class StartViewingSessionCommandValidator : AbstractValidator<StartViewingSessionCommand>
{
    public StartViewingSessionCommandValidator()
    {
        RuleFor(x => x.AuthorizationId).NotEmpty();
        RuleFor(x => x.CorporationId).NotEmpty();
        RuleFor(x => x.IpAddress)
            .Must(ip => ip == null || IPAddress.TryParse(ip, out _))
            .WithMessage("IpAddress must be a valid IP address.");
    }
}

public sealed class StartViewingSessionCommandHandler
    : IRequestHandler<StartViewingSessionCommand, StartViewingSessionResult>
{
    private readonly IAppDbContext _db;
    private readonly ILogger<StartViewingSessionCommandHandler> _logger;

    public StartViewingSessionCommandHandler(
        IAppDbContext db,
        ILogger<StartViewingSessionCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<StartViewingSessionResult> Handle(
        StartViewingSessionCommand req, CancellationToken ct)
    {
        var auth = await _db.ViewingAuthorizations
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == req.AuthorizationId, ct)
            ?? throw new KeyNotFoundException(
                $"ViewingAuthorization {req.AuthorizationId} not found.");

        var now = DateTimeOffset.UtcNow;

        if (!auth.IsCurrentlyValid(now))
            throw new InvalidOperationException(
                "Viewing authorization is revoked or outside its valid time window.");

        // Consent re-check: ensure camera_viewing consent is still granted.
        var today = DateOnly.FromDateTime(now.Date);
        var consentTypeId = await _db.RefValues
            .AsNoTracking()
            .Where(rv => rv.Code == "camera_viewing" && rv.DeletedAt == null)
            .Select(rv => (Guid?)rv.Id)
            .FirstOrDefaultAsync(ct);

        var hasConsent = await _db.StudentConsents
            .AsNoTracking()
            .AnyAsync(sc =>
                sc.CorporationId == req.CorporationId
                && sc.StudentId  == auth.StudentId
                && sc.State      == "granted"
                && (consentTypeId == null || sc.ConsentTypeId == consentTypeId)
                && (sc.ValidUntil == null || sc.ValidUntil >= today), ct);

        if (!hasConsent)
            throw new InvalidOperationException(
                "Camera viewing consent has been withdrawn. Access denied.");

        IPAddress? parsedIp = null;
        if (!string.IsNullOrWhiteSpace(req.IpAddress))
            IPAddress.TryParse(req.IpAddress, out parsedIp);

        var log = ViewingLog.Start(
            req.CorporationId,
            req.GuardianId,
            req.UserId,
            req.SessionId,
            req.CameraId,
            req.AuthorizationId,
            parsedIp);

        _db.ViewingLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Security: Viewing session started — logId={LogId} auth={AuthId} " +
            "guardian={GuardianId} camera={CameraId} ip={Ip}",
            log.Id, req.AuthorizationId, req.GuardianId, req.CameraId, req.IpAddress);

        return new StartViewingSessionResult(log.Id, log.StartedAt);
    }
}

// ── EndViewingSessionCommand ───────────────────────────────────────────────────

public record EndViewingSessionCommand(
    long LogId,
    DateTimeOffset StartedAt,
    Guid? UserId = null) : IRequest;

public class EndViewingSessionCommandValidator : AbstractValidator<EndViewingSessionCommand>
{
    public EndViewingSessionCommandValidator()
    {
        RuleFor(x => x.LogId).GreaterThan(0);
        RuleFor(x => x.StartedAt).NotEmpty();
    }
}

public sealed class EndViewingSessionCommandHandler : IRequestHandler<EndViewingSessionCommand>
{
    private readonly IAppDbContext _db;
    private readonly ILogger<EndViewingSessionCommandHandler> _logger;

    public EndViewingSessionCommandHandler(
        IAppDbContext db,
        ILogger<EndViewingSessionCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(EndViewingSessionCommand req, CancellationToken ct)
    {
        var log = await _db.ViewingLogs
            .FirstOrDefaultAsync(l => l.Id == req.LogId && l.StartedAt == req.StartedAt, ct)
            ?? throw new KeyNotFoundException(
                $"ViewingLog {req.LogId} (startedAt={req.StartedAt:O}) not found.");

        var endedAt = DateTimeOffset.UtcNow;
        log.End(endedAt);
        await _db.SaveChangesAsync(ct);

        var duration = (int)(endedAt - log.StartedAt).TotalSeconds;
        _logger.LogInformation(
            "Security: Viewing session ended — logId={LogId} duration={DurationSec}s user={UserId}",
            req.LogId, duration, req.UserId);
    }
}
