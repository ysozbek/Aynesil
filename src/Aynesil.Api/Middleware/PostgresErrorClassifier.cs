namespace Aynesil.Api.Middleware;

/// <summary>
/// Classifies PostgreSQL errors surfaced through EF Core / Npgsql without a compile-time
/// dependency on Npgsql (properties are read by name, as in <c>IsUniqueViolation</c>).
/// </summary>
public static class PostgresErrorClassifier
{
    public enum ForeignKeyViolation
    {
        None,

        /// <summary>
        /// The written row references a record that does not exist or belongs to another tenant
        /// (composite tenant FK, or the global-or-tenant reference trigger).
        /// </summary>
        MissingOrForeignReference,

        /// <summary>The deleted/updated record is still referenced by other rows.</summary>
        StillReferenced,
    }

    /// <summary>
    /// Finds a foreign_key_violation (23503) anywhere in the exception chain and tells the two
    /// directions apart. <c>Detail</c> ("... is still referenced from table ...") is used when
    /// available, but Npgsql redacts it unless the connection string sets
    /// <c>Include Error Detail=true</c>; <c>MessageText</c> is never redacted and PostgreSQL
    /// words it as "update or delete on table ..." for the referenced side and
    /// "insert or update on table ..." for the referencing side.
    /// </summary>
    public static ForeignKeyViolation ClassifyForeignKeyViolation(Exception ex, out string? constraintName)
    {
        constraintName = null;
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            if (Read(e, "SqlState") != "23503") continue;

            constraintName = Read(e, "ConstraintName");
            var detail = Read(e, "Detail") ?? string.Empty;
            var message = Read(e, "MessageText") ?? string.Empty;

            var stillReferenced =
                detail.Contains("is still referenced", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("update or delete on table", StringComparison.OrdinalIgnoreCase);

            return stillReferenced ? ForeignKeyViolation.StillReferenced : ForeignKeyViolation.MissingOrForeignReference;
        }
        return ForeignKeyViolation.None;
    }

    private static string? Read(Exception e, string property) =>
        e.GetType().GetProperty(property)?.GetValue(e) as string;
}
