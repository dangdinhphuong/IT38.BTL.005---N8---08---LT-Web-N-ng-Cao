using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Couppa.Api.Tests.Integration;

public class AuthCartFlowIntegrationTests : IClassFixture<MvcWebApplicationFactory>
{
    private readonly MvcWebApplicationFactory _factory;

    public AuthCartFlowIntegrationTests(MvcWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task RegisterLoginAddToCartAndViewCart_Succeeds()
    {
        long productId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var category = new Category
            {
                Name = "Test",
                Slug = $"test-{Guid.NewGuid():N}",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();
            var product = new Product
            {
                Sku = $"TEST-{Guid.NewGuid():N}",
                Name = "Test product",
                CategoryId = category.Id,
                Price = 100,
                StockQuantity = 5,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            productId = product.Id;
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var csrf = await GetCsrfTokenAsync(client);
        var email = $"integration-{Guid.NewGuid():N}@example.com";

        var register = await PostFormAsync(client, "/Account/Register", csrf,
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = "Abc12345",
                ["ConfirmPassword"] = "Abc12345",
                ["FullName"] = "Integration User"
            });
        Assert.Equal(HttpStatusCode.Redirect, register.StatusCode);

        var login = await PostFormAsync(client, "/Account/Login", csrf,
            new Dictionary<string, string>
            {
                ["Email"] = email,
                ["Password"] = "Abc12345"
            });
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        csrf = await GetCsrfTokenAsync(client);

        using var addRequest = new HttpRequestMessage(HttpMethod.Post, "/Cart/AddItem")
        {
            Content = JsonContent.Create(new { productId, quantity = 1 })
        };
        addRequest.Headers.Add("RequestVerificationToken", csrf);
        addRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var add = await client.SendAsync(addRequest);
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        var cart = await client.GetAsync("/Cart/Index");
        Assert.Equal(HttpStatusCode.OK, cart.StatusCode);
        Assert.Contains("Test product", await cart.Content.ReadAsStringAsync());
    }

    private static async Task<string> GetCsrfTokenAsync(HttpClient client)
    {
        var response = await client.GetFromJsonAsync<JsonElement>("/Account/AntiForgeryToken");
        return response.GetProperty("data").GetProperty("token").GetString()!;
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client,
        string path,
        string csrfToken,
        IReadOnlyDictionary<string, string> values)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new FormUrlEncodedContent(values)
        };
        request.Headers.Add("RequestVerificationToken", csrfToken);
        return await client.SendAsync(request);
    }
}

public sealed class MvcWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"couppa-test-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
        });
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                service => service.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
