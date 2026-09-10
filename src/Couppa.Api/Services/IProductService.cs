using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;

namespace Couppa.Api.Services;

public interface IProductService
{
    /// <summary>GET /Product/Index (FR-BROWSE-002..006) — chỉ trả IsActive=true (IsDeleted đã bị Global Query Filter loại).</summary>
    Task<PagedResult<ProductSummaryResponse>> GetPublicListAsync(ProductListQuery query);

    /// <summary>GET /Product/Featured — IsFeatured=true, IsActive=true.</summary>
    Task<IReadOnlyList<ProductSummaryResponse>> GetFeaturedAsync();

    /// <summary>GET /Product/Latest — 10 sản phẩm mới nhất, IsActive=true.</summary>
    Task<IReadOnlyList<ProductSummaryResponse>> GetLatestAsync();

    /// <summary>GET /Product/Detail/{id} — ném NotFound nếu không tồn tại, đã xóa, hoặc Inactive (Guest/User).</summary>
    Task<ProductResponse> GetPublicDetailAsync(long id);

    /// <summary>GET /Admin/Product/Index — trả cả Inactive, không trả đã xóa.</summary>
    Task<PagedResult<ProductSummaryResponse>> GetAdminListAsync(AdminProductListQuery query);

    /// <summary>GET /Admin/Product/Detail/{id} — trả cả khi Inactive (Admin xem riêng).</summary>
    Task<ProductResponse> GetAdminDetailAsync(long id);

    /// <summary>Dùng chung cho Cart Service (Phase 7) để lấy Product theo Id kiểm tra IsAvailableForCart/StockQuantity.</summary>
    Task<ProductResponse> GetByIdAsync(long id);

    Task<ProductResponse> CreateAsync(CreateProductRequest request);

    Task<ProductResponse> UpdateAsync(long id, UpdateProductRequest request);

    /// <summary>Soft delete — is_deleted=true, không xóa cứng khỏi DB.</summary>
    Task DeleteAsync(long id);

    Task<ProductResponse> ChangeStatusAsync(long id, bool isActive);
}
