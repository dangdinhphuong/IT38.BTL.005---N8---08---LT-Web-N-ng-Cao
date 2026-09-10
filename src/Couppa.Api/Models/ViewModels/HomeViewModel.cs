using Couppa.Api.Models.Responses;

namespace Couppa.Api.Models.ViewModels;

public class HomeViewModel
{
    public List<CategoryResponse> Categories { get; set; } = new();
    public List<ProductSummaryResponse> FeaturedProducts { get; set; } = new();
    public List<ProductSummaryResponse> LatestProducts { get; set; } = new();
}
