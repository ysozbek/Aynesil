using Aynesil.Application.Common.Interfaces;
using Aynesil.Application.Features.Notifications.Dtos;
using Aynesil.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Notifications.Queries;

// ── GetMyNotificationsQuery ───────────────────────────────────────────────────

public class GetMyNotificationsQuery : PagedQuery, IRequest<PaginatedResult<NotificationListItemDto>>
{
    public Guid RecipientUserId { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Status { get; set; }
    public bool? IsRead { get; set; }
}

public sealed class GetMyNotificationsQueryHandler
    : IRequestHandler<GetMyNotificationsQuery, PaginatedResult<NotificationListItemDto>>
{
    private readonly IAppDbContext _db;

    public GetMyNotificationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<PaginatedResult<NotificationListItemDto>> Handle(
        GetMyNotificationsQuery req, CancellationToken ct)
    {
        var query =
            from n in _db.Notifications.AsNoTracking()
            join cat in _db.RefValues.AsNoTracking()
                on n.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            where n.RecipientUserId == req.RecipientUserId
            select new { n, CategoryCode = cat != null ? cat.Code : null };

        if (req.CategoryId.HasValue)
            query = query.Where(x => x.n.CategoryId == req.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(req.Status))
            query = query.Where(x => x.n.Status == req.Status);

        if (req.IsRead.HasValue)
        {
            if (req.IsRead.Value)
                query = query.Where(x => x.n.ReadAt != null);
            else
                query = query.Where(x => x.n.ReadAt == null);
        }

        var projected = query.Select(x => new NotificationListItemDto(
            x.n.Id,
            x.n.CategoryId,
            x.CategoryCode,
            x.n.Subject,
            x.n.Body,
            x.n.Status,
            x.n.CreatedAt,
            x.n.ReadAt,
            x.n.ReadAt != null));

        projected = req.SortBy?.ToLowerInvariant() switch
        {
            "createdat" => req.IsDescending
                ? projected.OrderByDescending(x => x.CreatedAt)
                : projected.OrderBy(x => x.CreatedAt),
            _ => projected.OrderByDescending(x => x.CreatedAt)
        };

        var total = await projected.CountAsync(ct);
        var items = await projected.Skip(req.Skip).Take(req.PageSize).ToListAsync(ct);
        return PaginatedResult<NotificationListItemDto>.Create(items, total, req.Page, req.PageSize);
    }
}

// ── GetUnreadCountQuery ───────────────────────────────────────────────────────

public record GetUnreadCountQuery(Guid RecipientUserId) : IRequest<UnreadCountDto>;

public sealed class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, UnreadCountDto>
{
    private readonly IAppDbContext _db;

    public GetUnreadCountQueryHandler(IAppDbContext db) => _db = db;

    public async Task<UnreadCountDto> Handle(GetUnreadCountQuery req, CancellationToken ct)
    {
        var count = await _db.Notifications.AsNoTracking()
            .CountAsync(n => n.RecipientUserId == req.RecipientUserId
                          && n.ReadAt == null
                          && n.Status != "cancelled", ct);
        return new UnreadCountDto(count);
    }
}

// ── GetNotificationPreferencesQuery ──────────────────────────────────────────

public record GetNotificationPreferencesQuery(Guid UserId)
    : IRequest<IReadOnlyList<NotificationPreferenceDto>>;

public sealed class GetNotificationPreferencesQueryHandler
    : IRequestHandler<GetNotificationPreferencesQuery, IReadOnlyList<NotificationPreferenceDto>>
{
    private readonly IAppDbContext _db;

    public GetNotificationPreferencesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<IReadOnlyList<NotificationPreferenceDto>> Handle(
        GetNotificationPreferencesQuery req, CancellationToken ct)
    {
        return await (
            from p in _db.NotificationPreferences.AsNoTracking()
            join cat in _db.RefValues.AsNoTracking()
                on p.CategoryId equals cat.Id into catGrp
            from cat in catGrp.DefaultIfEmpty()
            join ch in _db.RefValues.AsNoTracking()
                on p.ChannelId equals ch.Id into chGrp
            from ch in chGrp.DefaultIfEmpty()
            where p.UserId == req.UserId
            select new NotificationPreferenceDto(
                p.Id, p.UserId,
                p.CategoryId, cat != null ? cat.Code : null,
                p.ChannelId,  ch  != null ? ch.Code  : null,
                p.IsEnabled)
        ).ToListAsync(ct);
    }
}
