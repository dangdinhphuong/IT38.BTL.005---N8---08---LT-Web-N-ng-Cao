namespace Couppa.Api.Models.Responses;

public class ReportSummaryResponse
{
    public required IReadOnlyList<ProductsByCategoryItem> ProductsByCategory { get; init; }
    public required int InStockProducts { get; init; }
    public required int OutOfStockProducts { get; init; }
    public required int TotalUsers { get; init; }
    public required int NewUsers { get; init; }
}
