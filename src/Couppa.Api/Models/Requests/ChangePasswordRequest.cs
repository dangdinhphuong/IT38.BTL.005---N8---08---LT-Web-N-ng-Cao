using System.ComponentModel.DataAnnotations;

namespace Couppa.Api.Models.Requests;

public class ChangePasswordRequest
{
    [Required]
    public required string OldPassword { get; set; }

    // Độ dài tối thiểu chặn thô ở đây, phần policy đầy đủ (hoa + số) do UserService validate lại (BR-06, không tin frontend).
    [Required]
    [MinLength(8)]
    public required string NewPassword { get; set; }
}
