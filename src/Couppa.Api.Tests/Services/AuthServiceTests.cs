using System.Net;
using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Couppa.Api.Tests.Services;

/// <summary>
/// Unit test cho AuthService — bám theo Acceptance Criteria FR-AUTH-002/FR-AUTH-005 (task 03-authentication.md)
/// và Done-khi của Phase 3 (plan 2026-09-07-couppa-webapi-implementation.md).
/// Dùng EF Core InMemory (mỗi test 1 database riêng biệt qua Guid) để cô lập hoàn toàn giữa các test.
/// </summary>
public class AuthServiceTests
{
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task SeedRolesAsync(AppDbContext db)
    {
        db.Roles.Add(new Role { Id = RoleIds.User, Name = "User" });
        db.Roles.Add(new Role { Id = RoleIds.Admin, Name = "Admin" });
        await db.SaveChangesAsync();
    }

    // ---------- RegisterAsync ----------

    [Fact]
    public async Task RegisterAsync_ValidInput_CreatesUserWithHashedPassword()
    {
        using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var sut = new AuthService(db);

        var request = new RegisterRequest { Email = "New.User@Example.com", Password = "Abc12345", FullName = "Nguyễn Văn A" };

        var result = await sut.RegisterAsync(request);

        Assert.Equal("new.user@example.com", result.Email); // normalize lowercase
        Assert.Equal("Nguyễn Văn A", result.FullName);
        Assert.Equal("User", result.Role);

        var stored = await db.Users.SingleAsync();
        Assert.NotEqual("Abc12345", stored.PasswordHash); // SEC-01: không lưu plaintext
        Assert.True(BCrypt.Net.BCrypt.Verify("Abc12345", stored.PasswordHash));
        Assert.Equal(RoleIds.User, stored.RoleId);
        Assert.False(stored.IsLocked);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflict_BR04()
    {
        using var db = CreateDbContext();
        await SeedRolesAsync(db);
        db.Users.Add(new User
        {
            Email = "existing@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Abc12345"),
            FullName = "Existing User",
            RoleId = RoleIds.User
        });
        await db.SaveChangesAsync();

        var sut = new AuthService(db);
        var request = new RegisterRequest { Email = "existing@example.com", Password = "Abc12345", FullName = "Another" };

        var ex = await Assert.ThrowsAsync<AppException>(() => sut.RegisterAsync(request));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("AUTH_EMAIL_EXISTS", ex.Code);
    }

    [Theory]
    [InlineData("abc12345")]     // thiếu chữ hoa
    [InlineData("ABCDEFGH")]     // thiếu chữ số
    [InlineData("Ab1")]          // quá ngắn (< 8 ký tự)
    public async Task RegisterAsync_PasswordNotMeetingPolicy_ThrowsUnprocessable_BR06(string invalidPassword)
    {
        using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var sut = new AuthService(db);
        var request = new RegisterRequest { Email = "user2@example.com", Password = invalidPassword, FullName = "User Two" };

        var ex = await Assert.ThrowsAsync<AppException>(() => sut.RegisterAsync(request));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.Equal("AUTH_PASSWORD_POLICY", ex.Code);
    }

    // ---------- ValidateCredentialsAsync ----------

    [Fact]
    public async Task ValidateCredentialsAsync_CorrectCredentials_ReturnsUser_FRAUTH002()
    {
        using var db = CreateDbContext();
        await SeedRolesAsync(db);
        db.Users.Add(new User
        {
            Email = "login.ok@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Abc12345"),
            FullName = "Login Ok",
            RoleId = RoleIds.User,
            IsLocked = false
        });
        await db.SaveChangesAsync();

        var sut = new AuthService(db);
        var request = new LoginRequest { Email = "login.ok@example.com", Password = "Abc12345" };

        var user = await sut.ValidateCredentialsAsync(request);

        Assert.Equal("login.ok@example.com", user.Email);
        Assert.Equal("User", user.Role.Name);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WrongPassword_ThrowsUnauthorized_WithGenericMessage()
    {
        using var db = CreateDbContext();
        await SeedRolesAsync(db);
        db.Users.Add(new User
        {
            Email = "wrongpass@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Abc12345"),
            FullName = "Wrong Pass",
            RoleId = RoleIds.User,
            IsLocked = false
        });
        await db.SaveChangesAsync();

        var sut = new AuthService(db);
        var request = new LoginRequest { Email = "wrongpass@example.com", Password = "WrongPassword1" };

        var ex = await Assert.ThrowsAsync<AppException>(() => sut.ValidateCredentialsAsync(request));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", ex.Code);
        Assert.Equal("Email hoặc mật khẩu không đúng", ex.Message);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_EmailNotFound_ThrowsUnauthorized_SameMessageAsWrongPassword()
    {
        using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var sut = new AuthService(db);
        var request = new LoginRequest { Email = "does.not.exist@example.com", Password = "Abc12345" };

        var ex = await Assert.ThrowsAsync<AppException>(() => sut.ValidateCredentialsAsync(request));

        // Không được tiết lộ email có tồn tại hay không -> message/code phải giống hệt trường hợp sai password.
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Equal("AUTH_INVALID_CREDENTIALS", ex.Code);
        Assert.Equal("Email hoặc mật khẩu không đúng", ex.Message);
    }

    [Fact]
    public async Task ValidateCredentialsAsync_LockedAccount_ThrowsForbidden_BR07_FRAUTH005()
    {
        using var db = CreateDbContext();
        await SeedRolesAsync(db);
        db.Users.Add(new User
        {
            Email = "locked@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Abc12345"),
            FullName = "Locked User",
            RoleId = RoleIds.User,
            IsLocked = true
        });
        await db.SaveChangesAsync();

        var sut = new AuthService(db);
        var request = new LoginRequest { Email = "locked@example.com", Password = "Abc12345" };

        var ex = await Assert.ThrowsAsync<AppException>(() => sut.ValidateCredentialsAsync(request));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        Assert.Equal("AUTH_ACCOUNT_LOCKED", ex.Code);
    }
}
