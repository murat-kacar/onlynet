using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Domain.DataProtection;

namespace TabFlow.Shared.Infrastructure.Data;

/// <summary>
/// EF Core <see cref="ModelBuilder"/> extensions used by both hosts.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Walks every entity in the model, finds every property whose
    /// CLR member carries a <see cref="DataClassAttribute"/>, and
    /// emits a column comment in the form
    /// <c>DataClass: &lt;Classification&gt;</c>. Migration scaffolding
    /// lifts the comment into a <c>COMMENT ON COLUMN</c> SQL
    /// statement, so the classification is visible in the live
    /// database schema and in the generated schema dump.
    ///
    /// Idempotent: a property with an existing column comment is
    /// **not** overwritten unless the existing comment also begins
    /// with <c>DataClass:</c>; non-DataClass comments authored by
    /// hand take precedence over the attribute.
    ///
    /// Introduced under TD-0007 step 1 (PR #32).
    /// </summary>
    public static ModelBuilder ApplyDataClassComments(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entity.ClrType;
            if (clrType is null)
            {
                continue;
            }

            foreach (var property in entity.GetProperties())
            {
                var clrProperty = clrType.GetProperty(
                    property.Name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                var attribute = clrProperty?.GetCustomAttribute<DataClassAttribute>(inherit: true);
                if (attribute is null)
                {
                    continue;
                }

                var newComment = $"DataClass: {attribute.Classification}";
                var existing = property.GetComment();
                if (string.IsNullOrEmpty(existing) ||
                    existing.StartsWith("DataClass:", StringComparison.Ordinal))
                {
                    property.SetComment(newComment);
                }
            }
        }

        return modelBuilder;
    }
}
