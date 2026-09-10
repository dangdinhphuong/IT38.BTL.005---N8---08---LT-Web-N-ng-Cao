namespace Couppa.Api.Models.Responses;

public class ProductImageResponse
{
    public required long Id { get; init; }
    public required string ImageUrl { get; init; }
    public required bool IsPrimary { get; init; }
    public required short SortOrder { get; init; }
}
