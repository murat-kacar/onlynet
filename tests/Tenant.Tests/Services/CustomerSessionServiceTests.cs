using FluentAssertions;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Tenant.Services;
using Tenant.Tests.Infrastructure;
using Xunit;

namespace Tenant.Tests.Services;

[Trait("Category", "Integration")]
[Collection(nameof(TenantDatabaseCollection))]
public sealed class CustomerSessionServiceTests : TenantTransactionalTestBase
{
    public CustomerSessionServiceTests(TenantDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task OpenSessionAsync_FreshJoinToken_OpensSession_IssuesTicket_ConsumesToken()
    {
        var station = Station.Create("Table 1", $"T1-{Guid.NewGuid():N}", "#FF0000", "Table", 1);
        Db.Stations.Add(station);

        var qrToken = QrToken.CreateJoinToken(
            station.Id,
            $"join-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(1));
        Db.QrTokens.Add(qrToken);
        await Db.SaveChangesAsync();

        var service = new CustomerSessionService(Db);
        var result = await service.OpenSessionAsync(qrToken.Value);

        result.SessionId.Should().NotBe(Guid.Empty);
        result.TicketId.Should().NotBe(Guid.Empty);
        result.DeviceCookieValue.Should().NotBeNullOrEmpty();

        var retrievedSession = await Db.CustomerSessions.FindAsync(result.SessionId);
        retrievedSession.Should().NotBeNull();
        retrievedSession!.IsOpen.Should().BeTrue();
        retrievedSession.TableId.Should().Be(station.Id);

        var ticket = await Db.CustomerAccessTickets.FindAsync(result.TicketId);
        ticket.Should().NotBeNull();
        ticket!.DeviceCookieValue.Should().Be(result.DeviceCookieValue);

        var consumedToken = await Db.QrTokens.FindAsync(qrToken.Id);
        consumedToken!.IsConsumed.Should().BeTrue("join QR tokens are single-use per AC-022");
    }

    [Fact]
    public async Task OpenSessionAsync_ConsumedJoinToken_Throws()
    {
        var station = Station.Create("Table 1", $"T1-{Guid.NewGuid():N}", "#FF0000", "Table", 1);
        Db.Stations.Add(station);

        var qrToken = QrToken.CreateJoinToken(
            station.Id,
            $"join-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddHours(1));
        Db.QrTokens.Add(qrToken);
        await Db.SaveChangesAsync();

        var service = new CustomerSessionService(Db);
        await service.OpenSessionAsync(qrToken.Value);

        var act = () => service.OpenSessionAsync(qrToken.Value);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid or expired QR token*");
    }

    [Fact]
    public async Task CloseSessionAsync_OpenSession_ClosesIt()
    {
        var session = CustomerSession.Open(Guid.NewGuid());
        Db.CustomerSessions.Add(session);
        await Db.SaveChangesAsync();

        var service = new CustomerSessionService(Db);
        await service.CloseSessionAsync(session.Id);

        var updated = await Db.CustomerSessions.FindAsync(session.Id);
        updated.Should().NotBeNull();
        updated!.IsOpen.Should().BeFalse();
    }

    [Fact]
    public async Task GetSessionStateAsync_MismatchedDeviceCookie_ReturnsNull()
    {
        var station = Station.Create("Table 1", $"T1-{Guid.NewGuid():N}", "#FF0000", "Table", 1);
        Db.Stations.Add(station);

        var session = CustomerSession.Open(station.Id);
        Db.CustomerSessions.Add(session);
        var ticket = session.IssueTicket($"cookie-{Guid.NewGuid():N}");
        Db.CustomerAccessTickets.Add(ticket);
        await Db.SaveChangesAsync();

        var service = new CustomerSessionService(Db);
        var state = await service.GetSessionStateAsync(ticket.Id, "wrong-cookie");

        state.Should().BeNull("session-state reads require the browser's access-ticket cookie");
    }

    [Fact]
    public async Task CloseSessionAsync_InvalidatesAccessTickets()
    {
        var session = CustomerSession.Open(Guid.NewGuid());
        Db.CustomerSessions.Add(session);
        var ticket = session.IssueTicket($"cookie-{Guid.NewGuid():N}");
        Db.CustomerAccessTickets.Add(ticket);
        await Db.SaveChangesAsync();

        var service = new CustomerSessionService(Db);
        await service.CloseSessionAsync(session.Id);

        var updatedTicket = await Db.CustomerAccessTickets.FindAsync(ticket.Id);
        updatedTicket!.IsValid.Should().BeFalse("closing a table session invalidates every access ticket");
    }
}
