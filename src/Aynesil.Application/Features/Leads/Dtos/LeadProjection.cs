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
        => (
            from l in db.Leads.AsNoTracking()
            where l.Id == leadId
            join src in db.RefValues.AsNoTracking() on l.SourceId equals src.Id into srcG
            from src in srcG.DefaultIfEmpty()
            join stat in db.RefValues.AsNoTracking() on l.StatusId equals stat.Id into statG
            from stat in statG.DefaultIfEmpty()
            join stg in db.RefValues.AsNoTracking() on l.PipelineStageId equals stg.Id into stgG
            from stg in stgG.DefaultIfEmpty()
            join camp in db.Campuses.AsNoTracking() on l.CampusId equals camp.Id into campG
            from camp in campG.DefaultIfEmpty()
            join usr in db.UserAccounts.AsNoTracking() on l.AssignedToId equals usr.Id into usrG
            from usr in usrG.DefaultIfEmpty()
            select new LeadDto(
                l.Id, l.CorporationId,
                l.CampusId, camp != null ? camp.Name : null,
                l.SourceId, src != null ? src.Code : null,
                l.StatusId, stat != null ? stat.Code : null,
                l.PipelineStageId, stg != null ? stg.Code : null,
                l.ChildName, l.ChildBirthDate,
                l.ContactName, l.ContactPhone, l.ContactEmail,
                l.PresentingNeed, l.ReferralDetail,
                l.AssignedToId, usr != null ? usr.FullName : null,
                l.Score, l.ConvertedStudentId, l.ConvertedAt,
                l.ConvertedStudentId != null,
                l.CreatedAt, l.UpdatedAt, l.RowVersion)
        ).FirstOrDefaultAsync(ct);
}
