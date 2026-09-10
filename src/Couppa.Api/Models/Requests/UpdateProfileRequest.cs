using System.ComponentModel.DataAnnotations;

namespace Couppa.Api.Models.Requests;

public class UpdateProfileRequest
{
    [Required]
    [MaxLength(150)]
    public required string FullName { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }
}
