namespace TabFlow.Shared.Domain.DataProtection;

/// <summary>
/// Marks an entity property with its personal-data classification
/// per the taxonomy in
/// <c>/doc/docs/explanation/concepts/data-protection.md</c>.
///
/// At model-build time the
/// <c>ModelBuilderExtensions.ApplyDataClassComments</c> extension
/// reads every property's attribute and emits a
/// <c>DataClass: &lt;Classification&gt;</c> column comment via EF
/// Core's <c>HasComment</c>. Migration generation lifts the
/// comment into a <c>COMMENT ON COLUMN</c> SQL statement, so the
/// classification is visible in the live database schema and in
/// generated schema dumps.
///
/// Acceptance criterion: AC-122. Ledger: TD-0007.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class DataClassAttribute : Attribute
{
    public DataClassification Classification { get; }

    public DataClassAttribute(DataClassification classification)
    {
        Classification = classification;
    }
}
