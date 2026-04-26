namespace TabFlow.Shared.Domain.DataProtection;

/// <summary>
/// Personal-data classification taxonomy as declared in
/// <c>/doc/docs/explanation/concepts/data-protection.md</c>.
/// Every entity property whose value can name an identifiable
/// person — directly (e.g. an email) or indirectly through linkage
/// (e.g. a session-bound cookie value) — MUST carry a
/// <see cref="DataClassAttribute"/> with one of the four values
/// below. Properties whose value is operational (totals, table
/// numbers, identifiers) carry the <see cref="Internal"/> class so
/// the schema-dump audit can distinguish "operational, classified"
/// from "personal, classified" from "missing classification".
/// </summary>
public enum DataClassification
{
    /// <summary>
    /// Safe to expose without restriction (e.g. a tenant trading
    /// name, public menu item names).
    /// </summary>
    Public,

    /// <summary>
    /// Operational data, not personal, but not for public exposure
    /// (e.g. order totals, table numbers, audit event keys).
    /// </summary>
    Internal,

    /// <summary>
    /// Personal data within KVKK / GDPR scope (e.g. staff email,
    /// audit-log actor IP, customer-session language preference
    /// once tied to a returning customer identifier).
    /// </summary>
    Sensitive,

    /// <summary>
    /// Special-category personal data, payment data, or
    /// authentication secrets (e.g. password hashes, session
    /// cookies, payment tokens).
    /// </summary>
    Restricted,
}
