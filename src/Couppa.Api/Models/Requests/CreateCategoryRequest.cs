using System.ComponentModel.DataAnnotations;

namespace Couppa.Api.Models.Requests;

public class CreateCategoryRequest
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(120)]
    public required string Slug { get; set; }

    public string? Description { get; set; }
}
