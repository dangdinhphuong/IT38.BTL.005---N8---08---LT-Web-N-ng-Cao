using System.Net;
using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Couppa.Api.Tests.Services;

/// <summary>
/// Unit test cho UserService — bám theo Acceptance Criteria task 16-user-management.md và Done-khi Phase 9
/// (plan 2026-09-07-couppa-webapi-implementation.md). Dùng EF Core InMemory (mỗi test 1 DB riêng qua Guid)
/// và Mock ICurrentUserService (không dùng CurrentUserService thật vì nó cần HttpContext thật) để giả lập
/// "user hiện tại đang là ai" theo từng test case.
/// </summary>
public class UserServiceTests
{
    private const string ValidPassword = "Abc12345";

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

    private static User NewUser(string email, string fullName, short roleId = RoleIds.User, bool isLocked = false, string? password = null)
    {
        var now = DateTimeOffset.UtcNow;
        return new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password ?? ValidPassword),
            FullName = fullName,
            RoleId = roleId,
            IsLocked = isLocked,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Mock<ICurrentUserService> MockCurrentUser(long userId)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.SetupGet(c => c.UserId).Returns(userId);
        mock.SetupGet(c => c.IsAuthenticated).Returns(true);
        return mock;
    }

    private static UserService CreateService(
        AppDbContext db, long currentUserId, out Mock<IAuditLogService> auditLogMock)
    {
        auditLogMock = new Mock<IAuditLogService>();
        auditLogMock
            .Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<object?>()))
            .Returns(Task.CompletedTask);

        var currentUserMock = MockCurrentUser(currentUserId);
        return new UserService(db, currentUserMock.Object, auditLogMock.Object);
    }

    // ---------- ChangePasswordAsync ----------

    [Fact]
    public async Task ChangePasswordAsync_WrongOldPassword_ThrowsUnauthorized()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var user = NewUser("user1@example.com", "User One");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db, user.Id, out _);
        var request = new ChangePasswordRequest { OldPassword = "WrongPassword1", NewPassword = "NewPass123" };

        var ex = await Assert.ThrowsAsync<AppException>(() => service.ChangePasswordAsync(request));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Equal("USER_OLD_PASSWORD_INVALID", ex.Code);
    }

    [Theory]
    [InlineData("newpass123")] // thiếu chữ hoa
    [InlineData("NEWPASSWORD")] // thiếu chữ số
    [InlineData("New1")] // quá ngắn
    public async Task ChangePasswordAsync_NewPasswordNotMeetingPolicy_ThrowsUnprocessable_BR06(string invalidNewPassword)
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var user = NewUser("user2@example.com", "User Two");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db, user.Id, out _);
        var request = new ChangePasswordRequest { OldPassword = ValidPassword, NewPassword = invalidNewPassword };

        var ex = await Assert.ThrowsAsync<AppException>(() => service.ChangePasswordAsync(request));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.Equal("USER_PASSWORD_POLICY", ex.Code);
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidInput_UpdatesPasswordHash()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var user = NewUser("user3@example.com", "User Three");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = CreateService(db, user.Id, out _);
        const string newPassword = "NewPass123";
        var request = new ChangePasswordRequest { OldPassword = ValidPassword, NewPassword = newPassword };

        await service.ChangePasswordAsync(request);

        var stored = await db.Users.SingleAsync(u => u.Id == user.Id);
        Assert.NotEqual(ValidPassword, stored.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(newPassword, stored.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify(ValidPassword, stored.PasswordHash));
    }

    // ---------- LockUserAsync ----------

    [Fact]
    public async Task LockUserAsync_AdminLocksSelf_ThrowsConflict_BR14()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var admin = NewUser("admin@example.com", "Admin", roleId: RoleIds.Admin);
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var service = CreateService(db, admin.Id, out var auditLogMock);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.LockUserAsync(admin.Id, isLocked: true));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("CANNOT_LOCK_SELF", ex.Code);

        var reloaded = await db.Users.FindAsync(admin.Id);
        Assert.False(reloaded!.IsLocked);
        auditLogMock.Verify(
            a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<object?>()),
            Times.Never);
    }

    [Fact]
    public async Task LockUserAsync_AdminLocksAnotherUser_Succeeds_AndWritesAuditLog()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var admin = NewUser("admin2@example.com", "Admin Two", roleId: RoleIds.Admin);
        var target = NewUser("target@example.com", "Target User");
        db.Users.AddRange(admin, target);
        await db.SaveChangesAsync();

        var service = CreateService(db, admin.Id, out var auditLogMock);

        var result = await service.LockUserAsync(target.Id, isLocked: true);

        Assert.True(result.IsLocked);
        var reloaded = await db.Users.FindAsync(target.Id);
        Assert.True(reloaded!.IsLocked);

        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.UserLock, EntityType.User, target.Id, It.IsAny<object?>()),
            Times.Once);
    }

    [Fact]
    public async Task LockUserAsync_AdminUnlocksSelf_DoesNotThrow()
    {
        // Quyết định thiết kế: BR-14 chỉ cấm Admin TỰ KHÓA chính mình, không cấm tự MỞ khóa
        // (xem comment trong UserService.LockUserAsync).
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var admin = NewUser("admin3@example.com", "Admin Three", roleId: RoleIds.Admin, isLocked: true);
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        var service = CreateService(db, admin.Id, out var auditLogMock);

        var result = await service.LockUserAsync(admin.Id, isLocked: false);

        Assert.False(result.IsLocked);
        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.UserUnlock, EntityType.User, admin.Id, It.IsAny<object?>()),
            Times.Once);
    }

    // ---------- ChangeRoleAsync ----------

    [Fact]
    public async Task ChangeRoleAsync_NonExistentRoleId_ThrowsUnprocessable()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var admin = NewUser("admin4@example.com", "Admin Four", roleId: RoleIds.Admin);
        var target = NewUser("target2@example.com", "Target Two");
        db.Users.AddRange(admin, target);
        await db.SaveChangesAsync();

        var service = CreateService(db, admin.Id, out _);

        var ex = await Assert.ThrowsAsync<AppException>(() => service.ChangeRoleAsync(target.Id, roleId: 99));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, ex.StatusCode);
        Assert.Equal("USER_ROLE_INVALID", ex.Code);
    }

    [Fact]
    public async Task ChangeRoleAsync_ValidRoleId_UpdatesRole_AndWritesAuditLog()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var admin = NewUser("admin5@example.com", "Admin Five", roleId: RoleIds.Admin);
        var target = NewUser("target3@example.com", "Target Three");
        db.Users.AddRange(admin, target);
        await db.SaveChangesAsync();

        var service = CreateService(db, admin.Id, out var auditLogMock);

        var result = await service.ChangeRoleAsync(target.Id, RoleIds.Admin);

        Assert.Equal("Admin", result.Role);
        var reloaded = await db.Users.FindAsync(target.Id);
        Assert.Equal(RoleIds.Admin, reloaded!.RoleId);

        auditLogMock.Verify(
            a => a.LogAsync(AuditAction.UserRoleChange, EntityType.User, target.Id, It.IsAny<object?>()),
            Times.Once);
    }

    // ---------- GetMyProfileAsync ----------

    [Fact]
    public async Task GetMyProfileAsync_ReturnsCurrentUserProfile_WithoutPasswordHash()
    {
        await using var db = CreateDbContext();
        await SeedRolesAsync(db);
        var me = NewUser("me@example.com", "Chính Tôi");
        var other = NewUser("other@example.com", "Người Khác");
        db.Users.AddRange(me, other);
        await db.SaveChangesAsync();

        var service = CreateService(db, me.Id, out _);

        var profile = await service.GetMyProfileAsync();

        Assert.Equal(me.Id, profile.Id);
        Assert.Equal("me@example.com", profile.Email);
        Assert.Equal("Chính Tôi", profile.FullName);

        // SEC-12: DTO không có property PasswordHash nào cả -> đảm bảo bằng reflection.
        var properties = typeof(Couppa.Api.Models.Responses.UserDetailResponse).GetProperties();
        Assert.DoesNotContain(properties, p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }
}
