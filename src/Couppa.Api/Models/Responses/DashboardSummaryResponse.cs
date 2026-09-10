namespace Couppa.Api.Models.Responses;

/// <summary>Response cho GET /Admin/Dashboard/Summary (FR-DASH-001..004).</summary>
public class DashboardSummaryResponse
{
    public int TotalProducts { get; init; }
    public int TotalCategories { get; init; }
    public int TotalUsers { get; init; }
    public int ActiveProducts { get; init; }
    public int OutOfStockProducts { get; init; }
    public required IReadOnlyList<ProductsByCategoryItem> ProductsByCategory { get; init; }
    public required IReadOnlyList<RecentProductItem> RecentProducts { get; init; }
}

public class ProductsByCategoryItem
{
    public required string CategoryName { get; init; }
    public int Count { get; init; }
}

public class RecentProductItem
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
