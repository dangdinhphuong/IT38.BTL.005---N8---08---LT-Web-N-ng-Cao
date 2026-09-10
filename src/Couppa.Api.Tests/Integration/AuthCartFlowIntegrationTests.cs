using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Couppa.Api.Tests.Integration;

/// <summary>
/// Integration test cho luồng chính Task 14: Register -&gt; Login -&gt; Add to Cart -&gt; View Cart,
/// chạy qua <see cref="WebApplicationFactory{TEntryPoint}"/> (toàn bộ pipeline thật: routing,
/// middleware CSRF/Exception, Authentication cookie, Session...), thay vì gọi Service trực tiếp
/// như 8 file unit test đã có.
///
/// KHÔNG dùng PostgreSQL thật (môi trường này không có DB chạy sẵn) -> <see cref="CustomWebApplicationFactory"/>
/// thay <c>AppDbContext</c> bằng EF Core InMemory. Environment được set thành "Testing" để tránh
/// nhánh <c>if (app.Environment.IsDevelopment()) DbSeeder.SeedAsync(db)</c> trong Program.cs — test
/// tự seed Role + Category + Product cần thiết trong từng test case, không phụ thuộc dữ liệu random
/// của DbSeeder (tránh flaky do stock/giá random).
///
/// RỦI RO / GIẢ ĐỊNH KHÔNG TỰ XÁC NHẬN ĐƯỢC (môi trường không có .NET SDK, không thể chạy `dotnet test`):
/// 1. CORS Middleware (`app.UseCors("Frontend")`) chỉ có tác dụng THỰC SỰ chặn request khi trình
///    duyệt gửi kèm header Origin và tự chặn response phía client - đây là hành vi CHỈ trình duyệt
///    thực hiện. `WebApplicationFactory`/`HttpClient` KHÔNG mô phỏng hành vi đó (không tự set Origin,
///    và dù response thiếu header Access-Control-Allow-Origin thì HttpClient vẫn đọc được body bình
///    thường) -> giả định request trong test này sẽ KHÔNG bị CORS chặn dù không set Origin header.
///    Đây là giả định hợp lý cho TestServer nhưng khác hành vi trình duyệt thật.
/// 2. Cookie Auth + Session dùng `SameSiteMode.None` + `CookieSecurePolicy.Always` (bắt buộc cookie
///    Secure). `TestServer` giao tiếp in-memory (không qua HTTPS thật) - theo tài liệu ASP.NET Core,
///    `WebApplicationFactory` mặc định set request scheme là "http", nhưng `TestServer` vẫn cho phép
///    set cookie Secure và client mặc định (`HandleCookies = true`) vẫn lưu/gửi lại cookie đó ở các
///    request tiếp theo trong cùng `HttpClient` bất kể Secure - đây là hành vi đã quan sát phổ biến
///    với TestServer, nhưng KHÔNG có cách tự chạy để xác nhận 100% trong môi trường này.
/// 3. Đã gọi GET /api/auth/csrf-token trước để lấy cookie "couppa.csrf" + token, rồi đính token đó
///    vào header "X-CSRF-TOKEN" cho POST /api/cart/items (CsrfValidationMiddleware không loại trừ
///    endpoint này). Nếu `IAntiforgery` yêu cầu thêm điều kiện nào đó về cookie Secure/SameSite mà
///    TestServer không mô phỏng đúng, bước POST /api/cart/items có thể trả 403 CSRF_TOKEN_INVALID
///    thay vì 201 - đây là điểm rủi ro cao nhất của test này, không tự xác nhận được vì không chạy được.
/// 4. Rate Limiter policy "auth-login" (5 lần/phút/IP) áp dụng theo `RemoteIpAddress` - trong
///    TestServer, các request thường có cùng 1 IP giả lập nội bộ; test này chỉ gọi login 1 lần
///    nên không dự kiến bị 429, nhưng nếu chạy lại (retry) nhiều lần liên tiếp trong cùng 1 phút có
///    thể bị Rate Limit chặn ở lần chạy sau.
/// </summary>
public class AuthCartFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthCartFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task FullFlow_RegisterLoginAddToCartViewCart_Succeeds()
    {
        // WebApplicationFactoryClientOptions.HandleCookies mặc định = true -> HttpClient tự lưu và
        // gửi lại mọi cookie (couppa.auth, couppa.session, couppa.csrf) nhận được qua CookieContainer
        // nội bộ cho các request tiếp theo trong cùng client, mô phỏng đúng hành vi trình duyệt giữ
        // phiên đăng nhập xuyên suốt flow Register -> Login -> AddToCart -> ViewCart.
        var client = _factory.CreateClient();

        // ---------- Chuẩn bị: seed 1 Category + 1 Product Active/còn hàng trực tiếp qua AppDbContext
        // (không qua HTTP) để đơn giản hóa test, đúng yêu cầu của task. ----------
        long productId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTimeOffset.UtcNow;
            var category = new Category
            {
                Name = "Điện thoại Test",
                Slug = "dien-thoai-test",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Categories.Add(category);
            await db.SaveChangesAsync();

            var product = new Product
            {
                Sku = "SKU-INTEGRATION-001",
                Name = "Sản phẩm Test Integration",
                CategoryId = category.Id,
                ShortDescription = "Sản phẩm dùng cho Integration Test",
                Price = 1_000_000m,
                StockQuantity = 10,
                IsActive = true,
                IsFeatured = false,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            productId = product.Id;
        }

        // ---------- Lấy CSRF token trước (GET không đổi trạng thái, nằm ngoài danh sách bị
        // CsrfValidationMiddleware chặn) - cần cho các bước POST/PUT/DELETE phía sau vào /api/cart/**. ----------
        var csrfResponse = await client.GetAsync("/api/auth/csrf-token");
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);
        var csrfBody = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var csrfToken = csrfBody.GetProperty("data").GetProperty("token").GetString();
        Assert.False(string.IsNullOrEmpty(csrfToken));

        // ---------- 1) POST /api/auth/register -> 201 ----------
        // Nằm trong danh sách loại trừ CSRF (CsrfValidationMiddleware.ExcludedPaths) nên không cần header X-CSRF-TOKEN.
        var email = $"integration.{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequest
        {
            Email = email,
            Password = "Abc12345",
            FullName = "Người Dùng Integration Test"
        };
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest, JsonOptions);
        var registerBodyText = await registerResponse.Content.ReadAsStringAsync();
        Assert.True(
            registerResponse.StatusCode == HttpStatusCode.Created,
            $"Register kỳ vọng 201 nhưng nhận {(int)registerResponse.StatusCode}. Body: {registerBodyText}");

        // ---------- 2) POST /api/auth/login -> 200, giữ cookie qua HttpClient (HandleCookies=true) ----------
        // Cũng nằm trong danh sách loại trừ CSRF.
        var loginRequest = new LoginRequest { Email = email, Password = "Abc12345" };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest, JsonOptions);
        var loginBodyText = await loginResponse.Content.ReadAsStringAsync();
        Assert.True(
            loginResponse.StatusCode == HttpStatusCode.OK,
            $"Login kỳ vọng 200 nhưng nhận {(int)loginResponse.StatusCode}. Body: {loginBodyText}");

        var loginBody = JsonSerializer.Deserialize<JsonElement>(loginBodyText, JsonOptions);
        Assert.True(loginBody.GetProperty("success").GetBoolean());
        Assert.Equal(email, loginBody.GetProperty("data").GetProperty("email").GetString());

        // ---------- 3) Category + Product active/còn hàng đã seed sẵn ở bước chuẩn bị phía trên. ----------

        // ---------- 4) POST /api/cart/items -> 201 (dùng cùng HttpClient đã login) ----------
        var addCartRequest = new AddCartItemRequest { ProductId = productId, Quantity = 1 };
        using var addCartHttpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/cart/items")
        {
            Content = JsonContent.Create(addCartRequest, options: JsonOptions)
        };
        // CsrfValidationMiddleware bắt buộc header X-CSRF-TOKEN khớp cookie "couppa.csrf" cho mọi
        // request thay đổi trạng thái tới /api/** không nằm trong danh sách loại trừ (POST /api/cart/items
        // không được loại trừ).
        addCartHttpRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);
        var addCartResponse = await client.SendAsync(addCartHttpRequest);
        var addCartBodyText = await addCartResponse.Content.ReadAsStringAsync();
        Assert.True(
            addCartResponse.StatusCode == HttpStatusCode.Created,
            $"AddItem kỳ vọng 201 nhưng nhận {(int)addCartResponse.StatusCode}. Body: {addCartBodyText}");

        // ---------- 5) GET /api/cart -> 200, kiểm tra đúng 1 item với đúng productId ----------
        var viewCartResponse = await client.GetAsync("/api/cart");
        var viewCartBodyText = await viewCartResponse.Content.ReadAsStringAsync();
        Assert.True(
            viewCartResponse.StatusCode == HttpStatusCode.OK,
            $"ViewCart kỳ vọng 200 nhưng nhận {(int)viewCartResponse.StatusCode}. Body: {viewCartBodyText}");

        var cartApiResponse = await JsonSerializer.DeserializeAsync<ApiResponse<CartResponse>>(
            new MemoryStream(Encoding.UTF8.GetBytes(viewCartBodyText)),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(cartApiResponse);
        Assert.True(cartApiResponse!.Success);
        Assert.NotNull(cartApiResponse.Data);
        Assert.Single(cartApiResponse.Data!.Items);
        Assert.Equal(productId, cartApiResponse.Data.Items[0].ProductId);
        Assert.Equal(1, cartApiResponse.Data.Items[0].Quantity);
        Assert.True(cartApiResponse.Data.Items[0].IsAvailable);
    }

    [Fact]
    public async Task Logout_AfterLogin_InvalidatesSession_FRAUTH003()
    {
        var client = _factory.CreateClient();

        var email = $"logout.{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequest { Email = email, Password = "Abc12345", FullName = "Logout Test" };
        await client.PostAsJsonAsync("/api/auth/register", registerRequest, JsonOptions);

        var loginRequest = new LoginRequest { Email = email, Password = "Abc12345" };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // Đã đăng nhập -> /api/users/me phải trả 200.
        var meBeforeLogout = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, meBeforeLogout.StatusCode);

        // POST /api/auth/logout yêu cầu [Authorize] và không nằm trong danh sách loại trừ CSRF
        // -> cần lấy token trước, giống bước AddCartItem ở test FullFlow phía trên.
        var csrfResponse = await client.GetAsync("/api/auth/csrf-token");
        var csrfBody = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var csrfToken = csrfBody.GetProperty("data").GetProperty("token").GetString();

        using var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Add("X-CSRF-TOKEN", csrfToken);
        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        // FR-AUTH-003: sau khi đăng xuất, Session bị hủy -> request sau đó tới endpoint yêu cầu
        // đăng nhập phải trả 401 (dùng lại đúng HttpClient/cookie container, không tạo client mới).
        var meAfterLogout = await client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogout.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_CalledByNonAdminUser_Returns403_FRAUTHZ002()
    {
        var client = _factory.CreateClient();

        // User thường (role mặc định = User khi đăng ký qua /api/auth/register).
        var email = $"nonadmin.{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequest { Email = email, Password = "Abc12345", FullName = "Non Admin" };
        await client.PostAsJsonAsync("/api/auth/register", registerRequest, JsonOptions);

        var loginRequest = new LoginRequest { Email = email, Password = "Abc12345" };
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        // FR-AUTHZ-002: User (không phải Admin) gọi thẳng 1 endpoint /api/admin/** -> phải bị 403,
        // không thực hiện thao tác. Dùng GET (không đổi trạng thái) để không bị CsrfValidationMiddleware
        // can thiệp vào kết quả - mục tiêu test này là Authorization (403), không phải CSRF.
        var adminResponse = await client.GetAsync("/api/admin/products");
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);
    }
}

/// <summary>
/// Kế thừa <see cref="WebApplicationFactory{TEntryPoint}"/> cho <c>Program</c> (Couppa.Api).
/// Thay <c>AppDbContext</c> (PostgreSQL thật, không có sẵn trong môi trường test) bằng EF Core
/// InMemory, và set Environment = "Testing" để Program.cs KHÔNG chạy nhánh
/// <c>if (app.Environment.IsDevelopment()) DbSeeder.SeedAsync(db)</c> (tránh phụ thuộc dữ liệu demo
/// ngẫu nhiên của DbSeeder — mỗi test tự seed đúng dữ liệu nó cần).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Đặt tên database InMemory cố định theo instance factory (1 factory dùng chung cho mọi test
    // trong class nhờ IClassFixture) để dữ liệu seed ở bước chuẩn bị và dữ liệu Service dùng khi xử
    // lý request là CÙNG một database InMemory.
    private readonly string _inMemoryDbName = $"couppa-integration-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" không phải "Development" -> app.Environment.IsDevelopment() = false trong
        // Program.cs, nhánh DbSeeder.SeedAsync(db) bị bỏ qua.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Gỡ đăng ký DbContextOptions<AppDbContext> gốc (UseNpgsql trỏ PostgreSQL thật) mà
            // Program.cs đã AddDbContext, thay bằng UseInMemoryDatabase — đây là cách chuẩn được
            // khuyến nghị bởi tài liệu WebApplicationFactory để override DbContext trong test.
            var dbContextOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbContextOptionsDescriptor is not null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_inMemoryDbName);
            });
        });
    }
}
