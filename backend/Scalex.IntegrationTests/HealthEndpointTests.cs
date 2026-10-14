using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Scalex.Api;
using Xunit;

namespace Scalex.IntegrationTests;

// Scaffold placeholder (#25): delete once the first real endpoint test lands — any endpoint test proves boot implicitly.
public class HealthEndpointTests
{
    [Fact]
    public async Task Get_when_called_then_ok_is_returned()
    {
        // Arrange
        var expectedStatusCode = HttpStatusCode.OK;
        using var factory = new WebApplicationFactory<Program>();
        using var sut = factory.CreateClient();

        // Act
        using var response = await sut.GetAsync("/health");
        var actualStatusCode = response.StatusCode;

        // Assert
        Assert.Equal(expectedStatusCode, actualStatusCode);
    }
}
