using Couppa.Api.Models.Responses;

namespace Couppa.Api.Models.ViewModels;

public class ProductListViewModel
{
    public List<ProductSummaryResponse> Products { get; set; } = new();
    public List<CategoryResponse> Categories { get; set; } = new();
    public long? SelectedCategoryId { get; set; }
    public string? SearchKeyword { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalItems { get; set; } = 0;
}

public class ProductDetailViewModel
{
    public ProductResponse Product { get; set; } = null!;
    public List<ProductSummaryResponse> RelatedProducts { get; set; } = new();
}
