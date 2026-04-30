using FluentAssertions;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Tenant.Services;
using Tenant.Tests.Infrastructure;
using Xunit;

namespace Tenant.Tests.Services;

/// <summary>
/// Integration-tier tests for <see cref="CartService"/>.
/// Uses the transactional fixture from TD-0010: every <c>[Fact]</c>
/// opens a database transaction in
/// <see cref="TenantTransactionalTestBase.InitializeAsync"/> and the
/// transaction is rolled back in <see cref="TenantTransactionalTestBase.DisposeAsync"/>,
/// so tests stay hermetic without an `EnsureDeletedAsync` per case.
/// </summary>
[Trait("Category", "Integration")]
[Collection(nameof(TenantDatabaseCollection))]
public sealed class CartServiceTests : TenantTransactionalTestBase
{
    public CartServiceTests(TenantDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task AddItemAsync_PersistsCartItem_WithMenuItemPriceAndName()
    {
        var category = Category.Create("Test Category", 1);
        Db.Categories.Add(category);

        var menuItem = MenuItem.Create(category.Id, "Test Item", 10.99m);
        Db.MenuItems.Add(menuItem);

        var session = CustomerSession.Open(Guid.NewGuid());
        Db.CustomerSessions.Add(session);
        var deviceCookie = $"cookie-{Guid.NewGuid():N}";
        var ticket = session.IssueTicket(deviceCookie);
        Db.CustomerAccessTickets.Add(ticket);
        await Db.SaveChangesAsync();

        var service = new CartService(Db);
        var request = new AddCartItemRequest(session.Id, menuItem.Id, 2, "No onions");

        var result = await service.AddItemAsync(request, deviceCookie);

        result.MenuItemId.Should().Be(menuItem.Id);
        result.MenuItemName.Should().Be("Test Item");
        result.Quantity.Should().Be(2);
        result.UnitPrice.Should().Be(10.99m);
        result.Note.Should().Be("No onions");

        var persisted = await Db.CartItems.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task RemoveItemAsync_RemovesPersistedRow()
    {
        var session = CustomerSession.Open(Guid.NewGuid());
        Db.CustomerSessions.Add(session);
        var deviceCookie = $"cookie-{Guid.NewGuid():N}";
        var ticket = session.IssueTicket(deviceCookie);
        Db.CustomerAccessTickets.Add(ticket);

        var cartItem = CartItem.Create(session.Id, Guid.NewGuid(), 1, null);
        Db.CartItems.Add(cartItem);
        await Db.SaveChangesAsync();

        var service = new CartService(Db);
        await service.RemoveItemAsync(cartItem.Id, deviceCookie);

        var item = await Db.CartItems.FindAsync(cartItem.Id);
        item.Should().BeNull();
    }

    [Fact]
    public async Task AddItemAsync_MismatchedDeviceCookie_Throws()
    {
        var category = Category.Create("Test Category", 1);
        Db.Categories.Add(category);

        var menuItem = MenuItem.Create(category.Id, "Test Item", 10.99m);
        Db.MenuItems.Add(menuItem);

        var session = CustomerSession.Open(Guid.NewGuid());
        Db.CustomerSessions.Add(session);
        var ticket = session.IssueTicket($"cookie-{Guid.NewGuid():N}");
        Db.CustomerAccessTickets.Add(ticket);
        await Db.SaveChangesAsync();

        var service = new CartService(Db);
        var request = new AddCartItemRequest(session.Id, menuItem.Id, 2, "No onions");

        var act = () => service.AddItemAsync(request, "wrong-cookie");
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Device cookie mismatch*");
    }
}
