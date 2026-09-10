using System.Net;
using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Couppa.Api.Tests.Services;

public class UserServiceTests
{
    private const string Password = "Abc12345";

    private static ServiceProvider CreateIdentityProvider(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();
        return services.BuildServiceProvider();
    }

    private static Mock<ICurrentUserService> CurrentUser(string userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(service => service.UserId).Returns(userId);
        mock.SetupGet(service => service.IsAuthenticated).Returns(true);
        return mock;
    }

    private static Mock<IAuditLogService> AuditLog()
    {
        var mock = new Mock<IAuditLogService>();
        mock.Setup(service => service.LogAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongOldPassword_ReturnsUnauthorized()
    {
        using var provider = CreateIdentityProvider(Guid.NewGuid().ToString());
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var user = new ApplicationUser
        {
            UserName = "user@example.com",
            Email = "user@example.com",
            FullName = "Test User",
            CreatedAt = DateTimeOffset.UtcNow
        };
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        var audit = AuditLog();
        var service = new UserService(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            userManager,
            roleManager,
            CurrentUser(user.Id).Object,
            audit.Object);

        var exception = await Assert.ThrowsAsync<AppException>(() => service.ChangePasswordAsync(
            new ChangePasswordRequest { OldPassword = "Wrong123", NewPassword = "NewPass123" }));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
        Assert.Equal("USER_OLD_PASSWORD_INVALID", exception.Code);
    }

    [Fact]
    public async Task LockUserAsync_CannotLockCurrentAdmin()
    {
        using var provider = CreateIdentityProvider(Guid.NewGuid().ToString());
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole("Admin"));
        var admin = new ApplicationUser
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            FullName = "Admin",
            CreatedAt = DateTimeOffset.UtcNow
        };
        Assert.True((await userManager.CreateAsync(admin, Password)).Succeeded);
        var audit = AuditLog();
        var service = new UserService(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            userManager,
            roleManager,
            CurrentUser(admin.Id).Object,
            audit.Object);

        var exception = await Assert.ThrowsAsync<AppException>(() =>
            service.LockUserAsync(admin.Id, true));

        Assert.Equal(HttpStatusCode.Conflict, exception.StatusCode);
        Assert.Equal("CANNOT_LOCK_SELF", exception.Code);
        Assert.False((await userManager.FindByIdAsync(admin.Id))!.IsLocked);
        audit.Verify(log => log.LogAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<object?>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateMyProfileAsync_UpdatesFullNameAndPhone()
    {
        using var provider = CreateIdentityProvider(Guid.NewGuid().ToString());
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var user = new ApplicationUser
        {
            UserName = "profile@example.com",
            Email = "profile@example.com",
            FullName = "Before",
            CreatedAt = DateTimeOffset.UtcNow
        };
        Assert.True((await userManager.CreateAsync(user, Password)).Succeeded);
        var service = new UserService(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(),
            userManager,
            roleManager,
            CurrentUser(user.Id).Object,
            AuditLog().Object);

        var result = await service.UpdateMyProfileAsync(new UpdateProfileRequest
        {
            FullName = "  After  ",
            Phone = "0900000000"
        });

        Assert.Equal("After", result.FullName);
        Assert.Equal("0900000000", result.Phone);
    }
}
