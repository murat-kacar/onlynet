using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Tenant.Services;
using Tenant.Tests.Infrastructure;
using Xunit;

namespace Tenant.Tests.Services;

/// <summary>
/// Integration-tier tests for <see cref="OrderService.SubmitAsync"/>
/// covering the AC-030 device-binding gate (TD-0017 step 4) and the
/// AC-035 idempotency gate (TD-0018 step 3). Each test seeds the
/// minimum graph (station, customer session, access ticket with
/// device cookie, fresh checkout-proof QR token, one cart item) and
/// then asserts behaviour via <c>OrderService</c>; the per-test
/// transaction in <see cref="TenantTransactionalTestBase"/> rolls
/// back the seed data on dispose.
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(TenantDatabaseCollection))]
public sealed class OrderServiceTests : TenantTransactionalTestBase
{
    public OrderServiceTests(TenantDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task SubmitAsync_HappyPath_PersistsOrder_ClosesSession_ConsumesToken()
    {
        var seed = await SeedSubmitGraphAsync();

        var service = new OrderService(Db);
        var request = new SubmitOrderRequest(
            seed.SessionId,
            seed.TicketId,
            seed.TableId,
            seed.CheckoutToken,
            IdempotencyKey: Guid.NewGuid().ToString(),
            Note: null);

        var result = await service.SubmitAsync(request, seed.DeviceCookieValue);

        result.OrderId.Should().NotBe(Guid.Empty);
        result.TotalAmount.Should().Be(seed.UnitPrice);

        var order = await Db.Orders.FindAsync(result.OrderId);
        order.Should().NotBeNull();

        var session = await Db.CustomerSessions.FindAsync(seed.SessionId);
        session!.IsOpen.Should().BeFalse("a successful submit closes the originating session per AC-036");

        var token = await Db.QrTokens.FindAsync(seed.CheckoutTokenId);
        token!.IsConsumed.Should().BeTrue("the checkout-proof token MUST be consumed in the same transaction as the order insert per AC-032");
    }

    /// <summary>
    /// TD-0017 step 4: a missing device cookie fails closed.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_MissingDeviceCookie_Throws()
    {
        var seed = await SeedSubmitGraphAsync();

        var service = new OrderService(Db);
        var request = new SubmitOrderRequest(
            seed.SessionId, seed.TicketId, seed.TableId,
            seed.CheckoutToken, Guid.NewGuid().ToString(), null);

        var act = () => service.SubmitAsync(request, deviceCookieValue: string.Empty);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Device cookie missing*");
    }

    /// <summary>
    /// TD-0017 step 4: a device cookie that does not match the
    /// ticket's stored value fails closed.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_MismatchedDeviceCookie_Throws()
    {
        var seed = await SeedSubmitGraphAsync();

        var service = new OrderService(Db);
        var request = new SubmitOrderRequest(
            seed.SessionId, seed.TicketId, seed.TableId,
            seed.CheckoutToken, Guid.NewGuid().ToString(), null);

        var act = () => service.SubmitAsync(request, deviceCookieValue: "different-cookie-value");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Device cookie mismatch*");
    }

    /// <summary>
    /// TD-0018 step 3: a second submit with the same idempotency key
    /// against the same session raises the unique-index violation.
    /// The first submit closes the session, so the second naturally
    /// also fails on the open-session gate; the raw idempotency
    /// constraint is exercised below by issuing two parallel orders
    /// against the **same** session before the first commits.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_DuplicateIdempotencyKey_RejectsSecond()
    {
        var seed = await SeedSubmitGraphAsync();
        var idempotencyKey = $"idem-{Guid.NewGuid():N}";

        var firstOrder = Order.Create(
            seed.TableId,
            seed.SessionId,
            seed.TicketId,
            idempotencyKey,
            new[] { OrderItem.Create(Guid.NewGuid(), "manual-seed", 1, 1m, seed.TableId, null) },
            note: null);
        Db.Orders.Add(firstOrder);
        await Db.SaveChangesAsync();

        var duplicate = Order.Create(
            seed.TableId,
            seed.SessionId,
            seed.TicketId,
            idempotencyKey,
            new[] { OrderItem.Create(Guid.NewGuid(), "manual-seed", 1, 1m, seed.TableId, null) },
            note: null);
        Db.Orders.Add(duplicate);

        var act = () => Db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>()
            .Where(ex => ex.InnerException != null &&
                ex.InnerException.Message.Contains("IX_orders_SessionId_IdempotencyKey", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record SubmitSeed(
        Guid TableId,
        Guid SessionId,
        Guid TicketId,
        string DeviceCookieValue,
        string CheckoutToken,
        Guid CheckoutTokenId,
        decimal UnitPrice);

    private async Task<SubmitSeed> SeedSubmitGraphAsync()
    {
        var station = Station.Create("Table A", $"T-{Guid.NewGuid():N}", "#000000", "Table", 1);
        Db.Stations.Add(station);

        var category = Category.Create("Beverages", 1);
        Db.Categories.Add(category);

        var menuItem = MenuItem.Create(category.Id, "Espresso", 4.50m);
        Db.MenuItems.Add(menuItem);

        var session = CustomerSession.Open(station.Id);
        Db.CustomerSessions.Add(session);

        var deviceCookie = $"cookie-{Guid.NewGuid():N}";
        var ticket = session.IssueTicket(deviceCookie);
        Db.CustomerAccessTickets.Add(ticket);

        var checkoutToken = QrToken.CreateCheckoutProof(
            station.Id,
            $"chk-{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow.AddMinutes(5));
        Db.QrTokens.Add(checkoutToken);

        var cartItem = CartItem.Create(session.Id, menuItem.Id, 1, null);
        Db.CartItems.Add(cartItem);

        await Db.SaveChangesAsync();

        return new SubmitSeed(
            station.Id,
            session.Id,
            ticket.Id,
            deviceCookie,
            checkoutToken.Value,
            checkoutToken.Id,
            menuItem.Price);
    }
}
