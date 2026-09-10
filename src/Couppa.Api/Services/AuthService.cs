using System.Text.RegularExpressions;
using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

public class AuthService : IAuthService
{
    // BR-06: tối thiểu 8 ký tự, ít nhất 1 chữ hoa, ít nhất 1 chữ số. Độ dài đã được [MinLength(8)] chặn ở Request.
    private static readonly Regex PasswordPolicyRegex = new(@"^(?=.*[A-Z])(?=.*\d).+$", RegexOptions.Compiled);

    // FR-AUTH-002 Exception Flow: message chung, không tiết lộ email có tồn tại hay không.
    private const string InvalidCredentialsMessage = "Email hoặc mật khẩu không đúng";

    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IHttpContextAccessor httpContextAccessor, ILogger<AuthService> logger)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    // Application log (khác audit_logs DB) - task 13 mục Logging: đăng nhập thất bại phải log
    // Warning kèm email + IP, KHÔNG kèm password.
    private string? GetClientIp() => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // BR-04: email đăng ký phải duy nhất.
        var emailExists = await _db.Users.AnyAsync(u => u.Email == email);
        if (emailExists)
        {
            throw AppException.Conflict("AUTH_EMAIL_EXISTS", "Email đã được sử dụng");
        }

        // BR-06: password tối thiểu 8 ký tự, có ít nhất 1 chữ hoa, 1 chữ số.
        if (!PasswordPolicyRegex.IsMatch(request.Password))
        {
            throw AppException.Unprocessable("AUTH_PASSWORD_POLICY",
                "Password phải có tối thiểu 8 ký tự, ít nhất 1 chữ hoa và 1 chữ số");
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName.Trim(),
            RoleId = RoleIds.User,
            IsLocked = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return new UserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = "User"
        };
    }

    public async Task<User> ValidateCredentialsAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Sai email hoặc sai password đều trả cùng 1 lỗi/message (không lộ email tồn tại hay không).
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Warning, kèm email + IP - KHÔNG kèm password (task 13 mục Logging).
            _logger.LogWarning("Đăng nhập thất bại cho email {Email} từ IP {IP}", email, GetClientIp());
            throw AppException.Unauthorized("AUTH_INVALID_CREDENTIALS", InvalidCredentialsMessage);
        }

        // BR-07: tài khoản bị khóa không thể đăng nhập kể cả đúng password.
        if (user.IsLocked)
        {
            _logger.LogWarning("Đăng nhập thất bại: tài khoản {Email} đã bị khóa, IP {IP}", email, GetClientIp());
            throw AppException.Forbidden("AUTH_ACCOUNT_LOCKED", "Tài khoản đã bị khóa");
        }

        _logger.LogInformation("Đăng nhập thành công cho user {UserId} ({Email})", user.Id, email);
        return user;
    }
}
