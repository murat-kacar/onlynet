using Microsoft.EntityFrameworkCore;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Domain.Entities.Tenant;
using TabFlow.Shared.Infrastructure.Data;
using TabFlow.Tenant.Services;
using Xunit;

namespace Tenant.Tests.Services;

public class CartServiceTests
{
    [Fact]
    public async Task AddItemAsync_ValidRequest_AddsItemToCart()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql("Host=localhost;Database=tabflow_test;Username=postgres;Password=test")
            .Options;

        using var context = new TenantDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var sessionId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var menuItem = MenuItem.Create(Guid.NewGuid(), "Test Item", 10.99m);
        context.MenuItems.Add(menuItem);
        await context.SaveChangesAsync();

        var service = new CartService(context);
        var request = new AddCartItemRequest(sessionId, itemId, 2, "No onions");

        // Act
        var result = await service.AddItemAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(itemId, result.MenuItemId);
        Assert.Equal(2, result.Quantity);
    }

    [Fact]
    public async Task RemoveItemAsync_ValidItemId_RemovesItem()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql("Host=localhost;Database=tabflow_test;Username=postgres;Password=test")
            .Options;

        using var context = new TenantDbContext(options);
        await context.Database.EnsureCreatedAsync();
        
        var cartItem = CartItem.Create(Guid.NewGuid(), Guid.NewGuid(), 1, null);
        context.CartItems.Add(cartItem);
        await context.SaveChangesAsync();

        var service = new CartService(context);

        // Act
        await service.RemoveItemAsync(cartItem.Id);

        // Assert
        var item = await context.CartItems.FindAsync(cartItem.Id);
        Assert.Null(item);
    }
}
