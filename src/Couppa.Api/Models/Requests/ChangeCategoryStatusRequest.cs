using System.ComponentModel.DataAnnotations;

namespace Couppa.Api.Models.Requests;

public class ChangeCategoryStatusRequest
{
    [Required]
    public bool IsActive { get; set; }
}
