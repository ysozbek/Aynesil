using Aynesil.Domain.Interfaces;
using Aynesil.Domain.Modules.Students.Entities;

namespace Aynesil.Domain.Interfaces.Repositories;

/// <summary>
/// Repository contract for the Students bounded context.
/// Read queries that require complex projections or cross-table JOINs should bypass
/// this interface and use IAppDbContext directly in query handlers (no projection leakage
/// into the domain layer).
/// </summary>
public interface IStudentRepository : IRepository<Student>
{
    /// <summary>
    /// Returns the student with all sub-records loaded:
    /// status history, campuses, guardians, emergency contacts,
    /// developmental profiles, diagnoses, case records.
    /// Used for detail views and export.
    /// </summary>
    Task<Student?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the student with guardians and emergency contacts only.</summary>
    Task<Student?> GetByIdWithGuardiansAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the student with all case management records (notes, reports, diagnoses).</summary>
    Task<Student?> GetByIdWithCaseRecordsAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a student_no is already taken within the corporation.
    /// Pass excludeId to ignore the current student when validating an update.
    /// </summary>
    Task<bool> StudentNoExistsAsync(
        Guid corporationId,
        string studentNo,
        Guid? excludeId = null,
        CancellationToken ct = default);
}
