namespace Couppa.Api.Models.Responses;

/// <summary>Response item cho GET /Admin/Report/CartTopProducts (FR-REPORT-004).</summary>
public class CartTopProductResponse
{
    public long ProductId { get; init; }
    public required string ProductName { get; init; }
    public int AddCount { get; init; }
}
