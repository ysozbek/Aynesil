using Aynesil.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CS8619 // LeadDto? nullable is intentional via FirstOrDefaultAsync

namespace Aynesil.Application.Features.Leads.Dtos;

/// <summary>
/// Reusable LINQ projection helpers for the Leads feature.
/// Each method executes a single SQL query (LEFT JOINs to ref_value, campus, user_account)
/// so callers never need to run N+1 lookups just to get display codes.
/// </summary>
internal static class LeadProjection
{
    /// <summary>
    /// Builds the full <see cref="LeadDto"/> for a single lead.
    /// Returns null when no matching lead is found.
    /// Executes one query with LEFT JOINs to campus, ref_value (×3), and user_account.
    /// </summary>
    internal static Task<LeadDto?> LoadAsync(
        IAppDbContext db, Guid leadId, CancellationToken ct)
        => BuildBaseQuery(db)
            .Where(x => x.Id == leadId)
            .Select(x => ToLeadDto(x))
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Builds a queryable that can be further filtered, paged, and ordered before executing.
    /// </summary>
    internal static IQueryable<LeadListItemDto> BuildListQuery(IAppDbContext db)
        => BuildBaseQuery(db).Select(x => ToLeadListItemDto(x));

    // ── Internal join builder ─────────────────────────────────────────────────

    private record LeadJoin(
        Guid Id, Guid CorporationId,
        Guid? CampusId, string? CampusName,
        Guid? SourceId, string? SourceCode,
        Guid? StatusId, string? StatusCode,
        Guid? PipelineStageId, string? PipelineStageCode,
        string? ChildName, DateOnly? ChildBirthDate,
        string ContactName, string? ContactPhone, string? ContactEmail,
        string? PresentingNeed, string? ReferralDetail,
        Guid? AssignedToId, string? AssignedToName,
        int? Score, Guid? ConvertedStudentId, DateTimeOffset? ConvertedAt,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, int RowVersion);

    private static IQueryable<LeadJoin> BuildBaseQuery(IAppDbContext db)
        => from l in db.Leads
           join src  in db.RefValues    on l.SourceId        equals src.Id  into srcG  from src  in srcG.DefaultIfEmpty()
           join stat in db.RefValues    on l.StatusId         equals stat.Id into statG from stat in statG.DefaultIfEmpty()
           join stg  in db.RefValues    on l.PipelineStageId  equals stg.Id  into stgG  from stg  in stgG.DefaultIfEmpty()
           join camp in db.Campuses     on l.CampusId         equals camp.Id into campG from camp in campG.DefaultIfEmpty()
           join usr  in db.UserAccounts on l.AssignedToId     equals usr.Id  into usrG  from usr  in usrG.DefaultIfEmpty()
           select new LeadJoin(
               l.Id, l.CorporationId,
               l.CampusId, camp == null ? null : camp.Name,
               l.SourceId, src  == null ? null : src.Code,
               l.StatusId, stat == null ? null : stat.Code,
               l.PipelineStageId, stg == null ? null : stg.Code,
               l.ChildName, l.ChildBirthDate,
               l.ContactName, l.ContactPhone, l.ContactEmail,
               l.PresentingNeed, l.ReferralDetail,
               l.AssignedToId, usr == null ? null : usr.FullName,
               l.Score, l.ConvertedStudentId, l.ConvertedAt,
               l.CreatedAt, l.UpdatedAt, l.RowVersion);

    private static LeadDto ToLeadDto(LeadJoin x)
        => new(x.Id, x.CorporationId,
               x.CampusId, x.CampusName,
               x.SourceId, x.SourceCode,
               x.StatusId, x.StatusCode,
               x.PipelineStageId, x.PipelineStageCode,
               x.ChildName, x.ChildBirthDate,
               x.ContactName, x.ContactPhone, x.ContactEmail,
               x.PresentingNeed, x.ReferralDetail,
               x.AssignedToId, x.AssignedToName,
               x.Score, x.ConvertedStudentId, x.ConvertedAt,
               x.ConvertedStudentId.HasValue,
               x.CreatedAt, x.UpdatedAt, x.RowVersion);

    private static LeadListItemDto ToLeadListItemDto(LeadJoin x)
        => new(x.Id, x.CorporationId,
               x.CampusId, x.CampusName,
               x.SourceId, x.SourceCode,
               x.StatusId, x.StatusCode,
               x.PipelineStageId, x.PipelineStageCode,
               x.ChildName,
               x.ContactName, x.ContactPhone, x.ContactEmail,
               x.AssignedToId, x.AssignedToName,
               x.Score, x.ConvertedStudentId.HasValue,
               x.CreatedAt);
}
