using System.ComponentModel.DataAnnotations;

namespace Couppa.Api.Models.Requests;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }

    /// <summary>BR-06: tối thiểu 8 ký tự, có ít nhất 1 chữ hoa, 1 chữ số (kiểm tra chi tiết ở AuthService).</summary>
    [Required]
    [MinLength(8)]
    public required string Password { get; set; }

    [Required]
    public required string FullName { get; set; }
}
