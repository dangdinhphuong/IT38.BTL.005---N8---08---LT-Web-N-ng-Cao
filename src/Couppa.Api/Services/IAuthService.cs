using Couppa.Api.Data.Entities;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;

namespace Couppa.Api.Services;

public interface IAuthService
{
    /// <summary>
    /// UC-03: tạo user mới với role User. Ném AppException.Conflict nếu email đã tồn tại (BR-04),
    /// AppException.Unprocessable nếu password không đạt policy (BR-06).
    /// </summary>
    Task<UserResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// UC-04: kiểm tra email tồn tại, so khớp password hash, kiểm tra is_locked (BR-07).
    /// Trả về User entity (kèm Role) để Controller tự tạo ClaimsPrincipal/Session — KHÔNG set Cookie ở Service.
    /// Sai email hoặc sai password đều ném cùng 1 AppException.Unauthorized với message chung
    /// (không tiết lộ email có tồn tại hay không). Tài khoản is_locked=true ném AppException.Forbidden.
    /// </summary>
    Task<User> ValidateCredentialsAsync(LoginRequest request);
}
