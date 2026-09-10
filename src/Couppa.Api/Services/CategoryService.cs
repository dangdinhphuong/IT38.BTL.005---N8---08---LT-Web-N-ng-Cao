using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;
    private readonly ICacheService _cache;
    private readonly IConfiguration _configuration;

    public CategoryService(AppDbContext db, IAuditLogService auditLog, ICacheService cache, IConfiguration configuration)
    {
        _db = db;
        _auditLog = auditLog;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetActiveListAsync()
    {
        var ttlMinutes = _configuration.GetValue<double>("Cache:CategoriesActiveTtlMinutes");
        return await _cache.GetOrCreateAsync(CacheKeys.CategoriesActive, TimeSpan.FromMinutes(ttlMinutes), async () =>
            await _db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => ToResponseWithCount(c))
                .ToListAsync());
    }

    public async Task<PagedResult<CategoryResponse>> GetAllForAdminAsync(string? search, int page, int pageSize)
    {
        var query = _db.Categories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(keyword));
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var totalItems = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToResponseWithCount(c))
            .ToListAsync();

        return new PagedResult<CategoryResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    public async Task<CategoryResponse> GetByIdAsync(long id)
    {
        var category = await FindOrThrowAsync(id);
        return ToResponse(category);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        await EnsureNameAndSlugUniqueAsync(request.Name, request.Slug, excludeId: null);

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKeys.CategoriesActive);

        await _auditLog.LogAsync(AuditAction.CategoryCreate, EntityType.Category, category.Id,
            new { category.Name, category.Slug });

        return ToResponse(category);
    }

    public async Task<CategoryResponse> UpdateAsync(long id, UpdateCategoryRequest request)
    {
        var category = await FindOrThrowAsync(id);

        await EnsureNameAndSlugUniqueAsync(request.Name, request.Slug, excludeId: id);

        category.Name = request.Name.Trim();
        category.Slug = request.Slug.Trim();
        category.Description = request.Description?.Trim();
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKeys.CategoriesActive);

        await _auditLog.LogAsync(AuditAction.CategoryUpdate, EntityType.Category, category.Id,
            new { category.Name, category.Slug });

        return ToResponse(category);
    }

    public async Task DeleteAsync(long id)
    {
        var category = await FindOrThrowAsync(id);

        // Đếm chủ động trước khi xóa (không dựa vào DB RESTRICT + bắt DbUpdateException) vì response 409
        // phải trả kèm productCount chính xác cho client (task 06 API spec).
        // Soft-deleted products remain linked to the category and therefore still block deletion.
        var productCount = await _db.Products
            .IgnoreQueryFilters()
            .CountAsync(p => p.CategoryId == id);
        if (productCount > 0)
        {
            throw AppException.Conflict(
                "CATEGORY_HAS_PRODUCTS",
                "Không thể xóa danh mục vì vẫn còn sản phẩm liên kết.",
                new { productCount });
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        _cache.Remove(CacheKeys.CategoriesActive);

        await _auditLog.LogAsync(AuditAction.CategoryDelete, EntityType.Category, id,
            new { category.Name, category.Slug });
    }

    public async Task<CategoryResponse> ChangeStatusAsync(long id, bool isActive)
    {
        var category = await FindOrThrowAsync(id);

        category.IsActive = isActive;
        category.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKeys.CategoriesActive);

        await _auditLog.LogAsync(AuditAction.CategoryStatusChange, EntityType.Category, category.Id,
            new { category.IsActive });

        return ToResponse(category);
    }

    private async Task<Category> FindOrThrowAsync(long id)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == id);
        if (category is null)
        {
            throw AppException.NotFound("CATEGORY_NOT_FOUND", "Không tìm thấy danh mục.");
        }

        return category;
    }

    private async Task EnsureNameAndSlugUniqueAsync(string name, string slug, long? excludeId)
    {
        name = name.Trim();
        slug = slug.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(slug))
        {
            throw AppException.Unprocessable("CATEGORY_FIELDS_REQUIRED", "Tên và slug danh mục là bắt buộc.");
        }

        var nameExists = await _db.Categories
            .AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId));
        if (nameExists)
        {
            throw AppException.Conflict("CATEGORY_NAME_EXISTS", "Tên danh mục đã tồn tại.");
        }

        var slugExists = await _db.Categories
            .AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId));
        if (slugExists)
        {
            throw AppException.Conflict("CATEGORY_SLUG_EXISTS", "Slug danh mục đã tồn tại.");
        }
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };

    // Global Query Filter (IsDeleted) trên Product áp dụng cả khi đếm qua navigation Products.Count.
    private static CategoryResponse ToResponseWithCount(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
        Description = c.Description,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        ProductCount = c.Products.Count(p => p.IsActive)
    };
}
