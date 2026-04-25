using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using TabFlow.Shared.Application.Services;
using TabFlow.Shared.Infrastructure.Data;
using Xunit;

namespace Tenant.Tests.Controllers;

public class OrdersControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrdersControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SubmitOrder_ValidRequest_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new SubmitOrderRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "checkout-proof",
            Guid.NewGuid().ToString(),
            null);

        // Act
        var response = await client.PostAsJsonAsync("/api/orders/submit", request);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }
}
