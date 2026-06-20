using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Assessment.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Application.Features.Assessment.Dtos;

/// <summary>
/// Reusable LINQ projection helpers for the Assessment feature.
/// Template detail is fetched via Include/ThenInclude (one round-trip).
/// List queries use raw-projection selects to avoid loading child collections.
/// Session detail is fetched via Include/ThenInclude with responses + items.
/// </summary>
internal static class AssessmentProjection
{
    // ── Template ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads full template detail: sections, items, translations, type/category codes.
    /// Returns null when not found.
    /// </summary>
    internal static async Task<AssessmentTemplateDto?> LoadTemplateAsync(
        IAppDbContext db, Guid templateId, CancellationToken ct)
    {
        var template = await db.AssessmentTemplates
            .Include(t => t.Translations)
            .Include(t => t.Sections.OrderBy(s => s.SortOrder))
                .ThenInclude(s => s.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);

        if (template is null) return null;

        var typeCode = template.TypeId.HasValue
            ? await db.RefValues.Where(r => r.Id == template.TypeId).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var categoryCode = template.CategoryId.HasValue
            ? await db.RefValues.Where(r => r.Id == template.CategoryId).Select(r => r.Code).FirstOrDefaultAsync(ct)
            : null;

        var devAreaIds = template.Sections
            .Where(s => s.DevelopmentAreaId.HasValue)
            .Select(s => s.DevelopmentAreaId!.Value)
            .Distinct()
            .ToList();

        var devAreaCodes = devAreaIds.Count > 0
            ? await db.RefValues
                .Where(r => devAreaIds.Contains(r.Id))
                .Select(r => new { r.Id, r.Code })
                .ToListAsync(ct)
            : [];

        var devAreaMap = devAreaCodes.ToDictionary(x => x.Id, x => x.Code);

        return ToTemplateDto(template, typeCode, categoryCode, devAreaMap);
    }

    /// <summary>
    /// Builds a queryable for the template list — further filtered and paged by callers.
    /// Each row is a single SELECT with LEFT JOINs to ref_value (type, category).
    /// </summary>
    internal static IQueryable<AssessmentTemplateListItemDto> BuildTemplateListQuery(IAppDbContext db)
        => from t in db.AssessmentTemplates
           join typ in db.RefValues on t.TypeId     equals typ.Id into typG from typ in typG.DefaultIfEmpty()
           join cat in db.RefValues on t.CategoryId equals cat.Id into catG from cat in catG.DefaultIfEmpty()
           select new AssessmentTemplateListItemDto(
               t.Id, t.CorporationId,
               t.Code, t.Name,
               t.TypeId, typ == null ? null : typ.Code,
               t.CategoryId, cat == null ? null : cat.Code,
               t.ScoringModel, t.Version, t.IsActive,
               t.Sections.Count(),
               t.CreatedAt);

    // ── Session ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads full session detail: responses + item codes, template name, campus name.
    /// Returns null when not found.
    /// </summary>
    internal static async Task<AssessmentSessionDto?> LoadSessionAsync(
        IAppDbContext db, Guid sessionId, CancellationToken ct)
    {
        var session = await db.AssessmentSessions
            .Include(s => s.Responses)
                .ThenInclude(r => r.Item)
            .FirstOrDefaultAsync(s => s.Id == sessionId, ct);

        if (session is null) return null;

        var templateName = await db.AssessmentTemplates
            .Where(t => t.Id == session.TemplateId)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(ct);

        var campusName = session.CampusId.HasValue
            ? await db.Campuses.Where(c => c.Id == session.CampusId).Select(c => c.Name).FirstOrDefaultAsync(ct)
            : null;

        return ToSessionDto(session, templateName, campusName);
    }

    /// <summary>
    /// Builds a queryable for the session list — further filtered and paged by callers.
    /// </summary>
    internal static IQueryable<AssessmentSessionListItemDto> BuildSessionListQuery(IAppDbContext db)
        => from s in db.AssessmentSessions
           join t    in db.AssessmentTemplates on s.TemplateId equals t.Id  into tG   from t    in tG.DefaultIfEmpty()
           join camp in db.Campuses            on s.CampusId   equals camp.Id into campG from camp in campG.DefaultIfEmpty()
           select new AssessmentSessionListItemDto(
               s.Id, s.CorporationId,
               s.TemplateId, t == null ? null : t.Name, s.TemplateVersion,
               s.LeadId, s.StudentId,
               s.CampusId, camp == null ? null : camp.Name,
               s.AssessorId, s.ScheduledAt,
               s.Status, s.TotalScore,
               s.CreatedAt);

    // ── Report ────────────────────────────────────────────────────────────────

    internal static AssessmentReportDto ToReportDto(AssessmentReport r)
        => new(r.Id, r.CorporationId, r.AssessmentSessionId,
               r.Summary, r.Findings, r.FileId,
               r.FinalizedAt, r.FinalizedBy, r.IsFinalized,
               r.CreatedAt, r.UpdatedAt, r.RowVersion);

    // ── Recommendation ────────────────────────────────────────────────────────

    internal static ProgramRecommendationDto ToRecommendationDto(ProgramRecommendation r)
        => new(r.Id, r.CorporationId,
               r.AssessmentSessionId, r.LeadId, r.StudentId,
               r.RecommendedProgramId, r.RecommendedIntensity,
               r.Rationale, r.RecommendedBy,
               r.CreatedAt, r.UpdatedAt, r.RowVersion);

    // ── Private mappers ───────────────────────────────────────────────────────

    private static AssessmentTemplateDto ToTemplateDto(
        AssessmentTemplate t,
        string? typeCode,
        string? categoryCode,
        Dictionary<Guid, string> devAreaMap)
        => new(
            t.Id, t.CorporationId,
            t.Code, t.Name,
            t.TypeId, typeCode,
            t.CategoryId, categoryCode,
            t.ScoringModel, t.Version, t.IsActive,
            t.Translations.Select(tr => new AssessmentTranslationDto(tr.Locale, tr.Name, tr.Description))
                          .ToList().AsReadOnly(),
            t.Sections.OrderBy(s => s.SortOrder)
                      .Select(s => ToSectionDto(s, devAreaMap))
                      .ToList().AsReadOnly(),
            t.CreatedAt, t.UpdatedAt, t.RowVersion);

    private static AssessmentSectionDto ToSectionDto(
        AssessmentSection s, Dictionary<Guid, string> devAreaMap)
        => new(s.Id, s.TemplateId, s.Code, s.SortOrder,
               s.DevelopmentAreaId,
               s.DevelopmentAreaId.HasValue && devAreaMap.TryGetValue(s.DevelopmentAreaId.Value, out var code) ? code : null,
               s.Items.OrderBy(i => i.SortOrder).Select(ToItemDto).ToList().AsReadOnly());

    private static AssessmentItemDto ToItemDto(AssessmentItem i)
        => new(i.Id, i.SectionId, i.Code, i.Prompt,
               i.ResponseType, i.Choices, i.Weight, i.SortOrder);

    private static AssessmentSessionDto ToSessionDto(
        AssessmentSession s, string? templateName, string? campusName)
        => new(s.Id, s.CorporationId,
               s.TemplateId, templateName, s.TemplateVersion,
               s.LeadId, s.StudentId,
               s.CampusId, campusName,
               s.AssessorId, s.ScheduledAt, s.PerformedAt,
               s.Status, s.TotalScore,
               s.Responses.Select(ToResponseDto).ToList().AsReadOnly(),
               s.CreatedAt, s.UpdatedAt, s.RowVersion);

    internal static AssessmentResponseDto ToResponseDto(AssessmentResponse r)
        => new(r.Id, r.AssessmentSessionId, r.ItemId,
               r.Item?.Code,
               r.NumericValue, r.TextValue, r.ChoiceValue, r.Note);
}
