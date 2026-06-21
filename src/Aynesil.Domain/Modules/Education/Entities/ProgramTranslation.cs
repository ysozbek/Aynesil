namespace Aynesil.Domain.Modules.Education.Entities;

/// <summary>
/// Per-locale display text for a program.
/// Maps to education.program_translation.
/// Primary key is (program_id, locale) — there is no surrogate id column in the DB.
/// Cascade-deleted when the parent program is deleted.
/// </summary>
public class ProgramTranslation
{
    public Guid ProgramId { get; set; }
    public string Locale { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public EducationProgram Program { get; set; } = null!;
}
