using Aynesil.Domain.Modules.Education.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for the Goals bounded context (goal library, templates, student goals, progress).
/// Complex projections and analytics should bypass this and use IAppDbContext directly.
/// </summary>
public interface IGoalRepository : IRepository<StudentGoal>
{
    /// <summary>Returns a student goal with progress records and parent/child goals included.</summary>
    Task<StudentGoal?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all active goals for a student, optionally filtered by horizon.</summary>
    Task<IReadOnlyList<StudentGoal>> GetByStudentAsync(
        Guid corporationId,
        Guid studentId,
        string? horizon = null,
        string? status = null,
        CancellationToken ct = default);

    /// <summary>Returns goal templates visible to a corporation (platform + tenant-owned).</summary>
    Task<IReadOnlyList<GoalTemplate>> GetTemplatesAsync(
        Guid? corporationId,
        Guid? libraryId = null,
        Guid? categoryId = null,
        Guid? developmentAreaId = null,
        CancellationToken ct = default);
}
