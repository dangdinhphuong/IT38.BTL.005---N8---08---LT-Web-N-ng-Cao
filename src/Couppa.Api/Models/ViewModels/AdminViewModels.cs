using Couppa.Api.Models.Responses;

namespace Couppa.Api.Models.ViewModels;

public class AdminDashboardViewModel
{
    public DashboardSummaryResponse Summary { get; set; } = null!;
}

public class AdminCategoriesViewModel
{
    public List<CategoryResponse> Categories { get; set; } = new();
}

public class AdminProductsViewModel
{
    public List<ProductSummaryResponse> Products { get; set; } = new();
    public List<CategoryResponse> Categories { get; set; } = new();
}

public class AdminUsersViewModel
{
    public List<UserDetailResponse> Users { get; set; } = new();
}

public class AdminReportsViewModel
{
    public ReportSummaryResponse Summary { get; set; } = null!;
    public IReadOnlyList<CartTopProductResponse> TopProducts { get; set; } = new List<CartTopProductResponse>();
}
