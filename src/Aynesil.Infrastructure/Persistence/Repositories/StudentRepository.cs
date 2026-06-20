using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Students.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aynesil.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IStudentRepository"/>.
/// All queries run within the active tenant RLS context (corporation_id GUC set by
/// TenantConnectionInterceptor) and respect the soft-delete query filter on Student.
/// </summary>
internal sealed class StudentRepository : GenericRepository<Student>, IStudentRepository
{
    public StudentRepository(AynesilDbContext context) : base(context) { }

    public async Task<Student?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(s => s.StatusHistory)
            .Include(s => s.Campuses)
            .Include(s => s.Guardians)
            .Include(s => s.EmergencyContacts)
            .Include(s => s.DevelopmentalProfiles)
            .Include(s => s.Diagnoses)
            .Include(s => s.MedicalReports)
            .Include(s => s.DevelopmentReports)
            .Include(s => s.ExternalInstitutionReports)
            .Include(s => s.CaseNotes)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Student?> GetByIdWithGuardiansAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(s => s.Guardians)
            .Include(s => s.EmergencyContacts)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<Student?> GetByIdWithCaseRecordsAsync(Guid id, CancellationToken ct = default)
        => await Set
            .Include(s => s.CaseNotes)
            .Include(s => s.MedicalReports)
            .Include(s => s.DevelopmentReports)
            .Include(s => s.ExternalInstitutionReports)
            .Include(s => s.Diagnoses)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<bool> StudentNoExistsAsync(
        Guid corporationId,
        string studentNo,
        Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var query = Set
            .AsNoTracking()
            .Where(s => s.CorporationId == corporationId && s.StudentNo == studentNo);

        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }
}
