namespace Couppa.Api.Models.Requests;

/// <summary>Query cho GET /Product/Index và các action AJAX public — FR-BROWSE-002..006.</summary>
public class ProductListQuery
{
    public string? Search { get; set; }
    public long? Category { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>price_asc | price_desc | newest</summary>
    public string? Sort { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>Query cho GET /Admin/Product/Index.</summary>
public class AdminProductListQuery
{
    public string? Search { get; set; }
    public long? Category { get; set; }

    /// <summary>active | inactive — lọc theo Product.IsActive, không ảnh hưởng is_deleted (luôn bị loại).</summary>
    public string? Status { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
