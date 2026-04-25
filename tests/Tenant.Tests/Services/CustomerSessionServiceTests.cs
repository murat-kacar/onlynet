using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Shared.Infrastructure.Data;
using TabFlow.Tenant.Services;
using Xunit;

namespace Tenant.Tests.Services;

// Integration tier per /doc/docs/explanation/concepts/test-taxonomy.md.
// Uses TenantDbContext.Database.EnsureCreatedAsync(); excluded from
// the Unit fast-path in CI.
[Trait("Category", "Integration")]
public class CustomerSessionServiceTests
{
    [Fact]
    public async Task OpenSessionAsync_ValidQrToken_ReturnsSessionResult()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql("Host=localhost;Database=tabflow_test;Username=postgres;Password=test")
            .Options;

        using var context = new TenantDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var tableId = Guid.NewGuid();
        var table = Station.Create("Table 1", "T1", "#FF0000", "Table", 1);
        context.Stations.Add(table);
        await context.SaveChangesAsync();

        var qrToken = QrToken.CreateJoinToken(tableId, "test-token", DateTimeOffset.UtcNow.AddHours(1));
        context.QrTokens.Add(qrToken);
        await context.SaveChangesAsync();

        var service = new CustomerSessionService(context);

        // Act
        var result = await service.OpenSessionAsync("test-token");

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.SessionId);
        Assert.NotEqual(Guid.Empty, result.TicketId);
    }

    [Fact]
    public async Task CloseSessionAsync_ValidSessionId_ClosesSession()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql("Host=localhost;Database=tabflow_test;Username=postgres;Password=test")
            .Options;

        using var context = new TenantDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var tableId = Guid.NewGuid();
        var table = Station.Create("Table 1", "T1", "#FF0000", "Table", 1);
        context.Stations.Add(table);
        
        var session = CustomerSession.Open(tableId);
        context.CustomerSessions.Add(session);
        await context.SaveChangesAsync();

        var service = new CustomerSessionService(context);

        // Act
        await service.CloseSessionAsync(session.Id);

        // Assert
        var updatedSession = await context.CustomerSessions.FindAsync(session.Id);
        Assert.NotNull(updatedSession);
        Assert.False(updatedSession.IsOpen);
    }
}
