using Aynesil.Domain.Modules.Education.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for the Education Plan (BEP/IEP) bounded context.
/// Complex projections and report queries should bypass this and use IAppDbContext directly.
/// </summary>
public interface IEducationPlanRepository : IRepository<EducationPlan>
{
    /// <summary>
    /// Returns the plan with all sub-records: plan goals, reviews, approvals, revisions.
    /// </summary>
    Task<EducationPlan?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns all plans for a student in a corporation, optionally filtered by status.
    /// </summary>
    Task<IReadOnlyList<EducationPlan>> GetByStudentAsync(
        Guid corporationId,
        Guid studentId,
        string? status = null,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the guardian-visible (approved, guardian_visible = true) plan for a student.
    /// Used by the parent portal.
    /// </summary>
    Task<EducationPlan?> GetGuardianVisiblePlanAsync(
        Guid corporationId,
        Guid studentId,
        CancellationToken ct = default);
}
