using System.ComponentModel.DataAnnotations;

namespace Couppa.Api.Models.Requests;

public class ChangeProductStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}
