using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using TabFlow.Shared.Infrastructure.Data;
using Xunit;

namespace Shared.Tests.Infrastructure;

/// <summary>
/// Contract tests for <see cref="ModelBuilderExtensions.ApplyDataClassComments"/>.
/// The extension lifts every <c>[DataClass(...)]</c> attribute on an
/// entity property into an EF Core column comment of the form
/// <c>DataClass: &lt;Classification&gt;</c>; migration scaffolding
/// emits a <c>COMMENT ON COLUMN</c> SQL statement from each comment.
///
/// These tests exercise the model directly via
/// <c>InMemoryDatabase</c> so they live in the Unit tier (no real
/// PostgreSQL connection, no file system, no network).
/// Closes TD-0007 step 2 (regression coverage).
/// </summary>
[Trait("Category", "Unit")]
public sealed class DataClassCommentTests
{
    [Fact]
    public void TenantAuditEntry_ActorEmail_Carries_Sensitive_DataClassComment()
    {
        var model = BuildTenantModel();
        var entity = model.FindEntityType(typeof(TabFlow.Shared.Domain.Entities.Tenant.TenantAuditEntry))!;
        var property = entity.FindProperty(nameof(TabFlow.Shared.Domain.Entities.Tenant.TenantAuditEntry.ActorEmail))!;

        property.GetComment().Should().Be("DataClass: Sensitive");
    }

    [Fact]
    public void TenantAuditEntry_Action_Carries_Internal_DataClassComment()
    {
        var model = BuildTenantModel();
        var entity = model.FindEntityType(typeof(TabFlow.Shared.Domain.Entities.Tenant.TenantAuditEntry))!;
        var property = entity.FindProperty(nameof(TabFlow.Shared.Domain.Entities.Tenant.TenantAuditEntry.Action))!;

        property.GetComment().Should().Be("DataClass: Internal");
    }

    [Fact]
    public void CustomerAccessTicket_DeviceCookieValue_Carries_Restricted_DataClassComment()
    {
        var model = BuildTenantModel();
        var entity = model.FindEntityType(typeof(TabFlow.Shared.Domain.Entities.Tenant.CustomerAccessTicket))!;
        var property = entity.FindProperty(nameof(TabFlow.Shared.Domain.Entities.Tenant.CustomerAccessTicket.DeviceCookieValue))!;

        property.GetComment().Should().Be("DataClass: Restricted");
    }

    [Fact]
    public void PlatformAuditEntry_ActorEmail_Carries_Sensitive_DataClassComment()
    {
        var model = BuildPlatformModel();
        var entity = model.FindEntityType(typeof(TabFlow.Shared.Domain.Entities.Platform.PlatformAuditEntry))!;
        var property = entity.FindProperty(nameof(TabFlow.Shared.Domain.Entities.Platform.PlatformAuditEntry.ActorEmail))!;

        property.GetComment().Should().Be("DataClass: Sensitive");
    }

    [Fact]
    public void Unannotated_Property_Has_No_DataClass_Comment()
    {
        // CartItem.Quantity is operational, not personal; the
        // extension MUST NOT emit a DataClass comment on a property
        // that lacks the attribute.
        var model = BuildTenantModel();
        var entity = model.FindEntityType(typeof(TabFlow.Shared.Domain.Entities.Tenant.CartItem))!;
        var property = entity.FindProperty(nameof(TabFlow.Shared.Domain.Entities.Tenant.CartItem.Quantity))!;

        var comment = property.GetComment();
        // Either no comment at all or a comment that does not begin
        // with the DataClass marker.
        (comment is null || !comment.StartsWith("DataClass:", System.StringComparison.Ordinal))
            .Should().BeTrue();
    }

    private static IModel BuildTenantModel()
    {
        // UseNpgsql with a placeholder connection string is enough to
        // build the model graph; the model is constructed in-process
        // and the connection string is only consulted on the first
        // database operation, which the test does not perform.
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql("Host=localhost;Database=tabflow_test")
            .Options;
        using var context = new TenantDbContext(options);
        // EF Core 10's runtime model is read-optimised and drops
        // metadata such as column comments; the design-time model
        // exposes the full set used by migration scaffolding, which
        // is the surface this test exercises.
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static IModel BuildPlatformModel()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseNpgsql("Host=localhost;Database=tabflow_test")
            .Options;
        using var context = new PlatformDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }
}
