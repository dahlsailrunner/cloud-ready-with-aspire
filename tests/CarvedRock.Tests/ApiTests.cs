using System.Net.Http.Json;

namespace CarvedRock.Tests;

public class ApiTests
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task GetAllProductsReturnsAllProducts()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CarvedRock_AppHost>(cancellationToken);

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });

        await using var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        // Act
        using var httpClient = app.CreateHttpClient("api");
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        var response = await httpClient.GetFromJsonAsync<List<ProductModel>>("/product", cancellationToken);

        // Assert
        Assert.Equal(50, response?.Count);
    }
}