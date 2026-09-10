using System.ComponentModel.DataAnnotations;
using Couppa.Api.Models.Responses;

namespace Couppa.Api.Models.ViewModels;

public class UserProfileViewModel
{
    public UserDetailResponse User { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Họ tên")]
    public string FullName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    [DataType(DataType.Password)]
    public string? OldPassword { get; set; }

    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự")]
    public string? NewPassword { get; set; }
}
