using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;

namespace Couppa.Api.Services;

public interface ICategoryService
{
    /// <summary>Trang public (FR-CATEGORY-007) — chỉ trả category IsActive = true.</summary>
    Task<IReadOnlyList<CategoryResponse>> GetActiveListAsync();

    /// <summary>Trang admin (BR-15) — trả tất cả, kể cả Inactive, có search + phân trang.</summary>
    Task<PagedResult<CategoryResponse>> GetAllForAdminAsync(string? search, int page, int pageSize);

    Task<CategoryResponse> GetByIdAsync(long id);

    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request);

    Task<CategoryResponse> UpdateAsync(long id, UpdateCategoryRequest request);

    /// <summary>BR-03 — ném AppException.Conflict("CATEGORY_HAS_PRODUCTS", ..., new { productCount }) nếu còn sản phẩm liên kết.</summary>
    Task DeleteAsync(long id);

    Task<CategoryResponse> ChangeStatusAsync(long id, bool isActive);
}
