using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Middleware;
using Couppa.Api.Models.Requests;
using Couppa.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLog;
    private readonly ICategoryService _categoryService;
    private readonly ICacheService _cache;
    private readonly IConfiguration _configuration;

    public ProductService(
        AppDbContext db,
        IAuditLogService auditLog,
        ICategoryService categoryService,
        ICacheService cache,
        IConfiguration configuration)
    {
        _db = db;
        _auditLog = auditLog;
        _categoryService = categoryService;
        _cache = cache;
        _configuration = configuration;
    }

    public async Task<PagedResult<ProductSummaryResponse>> GetPublicListAsync(ProductListQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize < 1 ? 20 : query.PageSize, 1, 100);

        // is_deleted đã bị Global Query Filter loại (AppDbContext.cs) - chỉ cần tự lọc is_active ở đây.
        var products = _db.Products.Where(p => p.IsActive && p.Category.IsActive).AsQueryable();
        products = ApplyCommonFilters(products, query.Search, query.Category, query.MinPrice, query.MaxPrice);
        products = ApplySort(products, query.Sort);

        return await ToPagedSummaryAsync(products, page, pageSize);
    }

    public async Task<IReadOnlyList<ProductSummaryResponse>> GetFeaturedAsync()
    {
        var ttlMinutes = _configuration.GetValue<double>("Cache:ProductsFeaturedTtlMinutes");
        return await _cache.GetOrCreateAsync(CacheKeys.ProductsFeatured, TimeSpan.FromMinutes(ttlMinutes), async () =>
            await _db.Products
                .Where(p => p.IsActive && p.Category.IsActive && p.IsFeatured)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Select(p => ToSummary(p))
                .ToListAsync());
    }

    public async Task<IReadOnlyList<ProductSummaryResponse>> GetLatestAsync()
    {
        var ttlMinutes = _configuration.GetValue<double>("Cache:ProductsLatestTtlMinutes");
        return await _cache.GetOrCreateAsync(CacheKeys.ProductsLatest, TimeSpan.FromMinutes(ttlMinutes), async () =>
            await _db.Products
                .Where(p => p.IsActive && p.Category.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Select(p => ToSummary(p))
                .ToListAsync());
    }

    public async Task<ProductResponse> GetPublicDetailAsync(long id)
    {
        // Public/Guest: 404 nếu không tồn tại, đã xóa (Global Query Filter), hoặc Inactive - task 05 API spec.
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && p.Category.IsActive);

        if (product is null)
        {
            throw AppException.NotFound("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");
        }

        return ToResponse(product);
    }

    public async Task<PagedResult<ProductSummaryResponse>> GetAdminListAsync(AdminProductListQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = Math.Clamp(query.PageSize < 1 ? 20 : query.PageSize, 1, 100);

        // Admin: trả cả Inactive, chỉ is_deleted bị loại (Global Query Filter).
        var products = _db.Products.AsQueryable();
        products = ApplyCommonFilters(products, query.Search, query.Category, minPrice: null, maxPrice: null);

        if (string.Equals(query.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            products = products.Where(p => p.IsActive);
        }
        else if (string.Equals(query.Status, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            products = products.Where(p => !p.IsActive);
        }

        products = products.OrderByDescending(p => p.CreatedAt);

        return await ToPagedSummaryAsync(products, page, pageSize);
    }

    public async Task<ProductResponse> GetAdminDetailAsync(long id)
    {
        var product = await FindOrThrowAsync(id);
        return ToResponse(product);
    }

    public async Task<ProductResponse> GetByIdAsync(long id)
    {
        var product = await FindOrThrowAsync(id);
        return ToResponse(product);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
    {
        ValidateRequiredText(request.Sku, "PRODUCT_SKU_REQUIRED", "Mã sản phẩm (SKU) là bắt buộc.");
        ValidateRequiredText(request.Name, "PRODUCT_NAME_REQUIRED", "Tên sản phẩm là bắt buộc.");
        ValidatePriceAndStock(request.Price, request.StockQuantity);
        await EnsureSkuUniqueAsync(request.Sku, excludeId: null);
        await EnsureCategoryActiveAsync(request.CategoryId);

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            ShortDescription = request.ShortDescription?.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            IsFeatured = request.IsFeatured,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        product.Images = BuildImages(request.ImageUrls, product);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        InvalidateProductCaches();

        await _auditLog.LogAsync(AuditAction.ProductCreate, EntityType.Product, product.Id,
            new { product.Sku, product.Name, product.Price, product.StockQuantity });

        return ToResponse(product);
    }

    public async Task<ProductResponse> UpdateAsync(long id, UpdateProductRequest request)
    {
        var product = await FindOrThrowAsync(id, includeImages: true);

        ValidateRequiredText(request.Sku, "PRODUCT_SKU_REQUIRED", "Mã sản phẩm (SKU) là bắt buộc.");
        ValidateRequiredText(request.Name, "PRODUCT_NAME_REQUIRED", "Tên sản phẩm là bắt buộc.");
        ValidatePriceAndStock(request.Price, request.StockQuantity);
        // Loại trừ chính record đang sửa khỏi check unique SKU.
        await EnsureSkuUniqueAsync(request.Sku, excludeId: id);
        await EnsureCategoryActiveAsync(request.CategoryId);

        product.Sku = request.Sku.Trim();
        product.Name = request.Name.Trim();
        product.CategoryId = request.CategoryId;
        product.ShortDescription = request.ShortDescription?.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.StockQuantity = request.StockQuantity;
        product.IsFeatured = request.IsFeatured;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        // Full update ảnh: thay toàn bộ danh sách theo imageUrls gửi lên (đơn giản, đúng tinh thần PUT).
        _db.ProductImages.RemoveRange(product.Images);
        product.Images = BuildImages(request.ImageUrls, product);

        await _db.SaveChangesAsync();
        InvalidateProductCaches();

        await _auditLog.LogAsync(AuditAction.ProductUpdate, EntityType.Product, product.Id,
            new { product.Sku, product.Name, product.Price, product.StockQuantity });

        return ToResponse(product);
    }

    public async Task DeleteAsync(long id)
    {
        var product = await FindOrThrowAsync(id);

        product.IsDeleted = true;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        InvalidateProductCaches();

        await _auditLog.LogAsync(AuditAction.ProductDelete, EntityType.Product, product.Id,
            new { product.Sku, product.Name });
    }

    public async Task<ProductResponse> ChangeStatusAsync(long id, bool isActive)
    {
        var product = await FindOrThrowAsync(id);

        product.IsActive = isActive;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        InvalidateProductCaches();

        await _auditLog.LogAsync(AuditAction.ProductStatusChange, EntityType.Product, product.Id,
            new { product.IsActive });

        return ToResponse(product);
    }

    // Task 09 quy định chỉ xóa products:featured khi sản phẩm liên quan cờ is_featured, nhưng CUD nào
    // cũng có thể làm thay đổi cờ đó (vd Update đổi is_featured false -> true) hoặc thứ tự "mới nhất"
    // (products:latest). Xóa cả 2 key ở mọi thao tác CUD để tránh bug tinh vi khi chỉ xóa có điều kiện,
    // đúng tinh thần "không thiết kế caching quá phức tạp" của SRS — chấp nhận cache miss thêm vài lần.
    private void InvalidateProductCaches()
    {
        _cache.Remove(CacheKeys.ProductsFeatured);
        _cache.Remove(CacheKeys.ProductsLatest);
    }

    // ---- helpers ----

    private async Task<Product> FindOrThrowAsync(long id, bool includeImages = true)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();
        if (includeImages)
        {
            query = query.Include(p => p.Images);
        }

        // Global Query Filter đã loại is_deleted=true; Admin cũng không được xem sản phẩm đã xóa (task 05).
        var product = await query.FirstOrDefaultAsync(p => p.Id == id);
        if (product is null)
        {
            throw AppException.NotFound("PRODUCT_NOT_FOUND", "Không tìm thấy sản phẩm.");
        }

        return product;
    }

    private static void ValidatePriceAndStock(decimal price, int stockQuantity)
    {
        // DB có CHECK constraint (BR-09/BR-10) nhưng EF InMemory (dùng trong test) không enforce -
        // Service phải tự validate bằng code, không được ỷ lại vào DB.
        if (price < 0)
        {
            throw AppException.Unprocessable("PRODUCT_PRICE_NEGATIVE", "Giá sản phẩm không được là số âm.");
        }

        if (stockQuantity < 0)
        {
            throw AppException.Unprocessable("PRODUCT_STOCK_NEGATIVE", "Số lượng tồn kho không được là số âm.");
        }
    }

    private static void ValidateRequiredText(string? value, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw AppException.Unprocessable(code, message);
        }
    }

    private async Task EnsureSkuUniqueAsync(string sku, long? excludeId)
    {
        var trimmedSku = sku.Trim();
        var exists = await _db.Products
            .AnyAsync(p => p.Sku == trimmedSku && (excludeId == null || p.Id != excludeId));

        if (exists)
        {
            throw AppException.Conflict("PRODUCT_SKU_EXISTS", "Mã sản phẩm (SKU) đã tồn tại.");
        }
    }

    private async Task EnsureCategoryActiveAsync(long categoryId)
    {
        // GetByIdAsync tự ném NotFound("CATEGORY_NOT_FOUND") nếu category không tồn tại.
        var category = await _categoryService.GetByIdAsync(categoryId);
        if (!category.IsActive)
        {
            throw AppException.Unprocessable("CATEGORY_INACTIVE", "Danh mục sản phẩm hiện không hoạt động.");
        }
    }

    private static List<ProductImage> BuildImages(string[]? imageUrls, Product product)
    {
        if (imageUrls is null || imageUrls.Length == 0)
        {
            return new List<ProductImage>();
        }

        var images = new List<ProductImage>();
        short order = 0;
        foreach (var url in imageUrls)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            images.Add(new ProductImage
            {
                Product = product,
                ImageUrl = url,
                // Ảnh đầu tiên mặc định là ảnh đại diện (FR-PRODUCT-007) - Admin có thể đổi sau qua API riêng nếu cần.
                IsPrimary = order == 0,
                SortOrder = order
            });
            order++;
        }

        return images;
    }

    private static IQueryable<Product> ApplyCommonFilters(
        IQueryable<Product> query, string? search, long? categoryId, decimal? minPrice, decimal? maxPrice)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        return query;
    }

    private static IQueryable<Product> ApplySort(IQueryable<Product> query, string? sort) => sort switch
    {
        "price_asc" => query.OrderBy(p => p.Price),
        "price_desc" => query.OrderByDescending(p => p.Price),
        "newest" => query.OrderByDescending(p => p.CreatedAt),
        _ => query.OrderByDescending(p => p.CreatedAt)
    };

    private async Task<PagedResult<ProductSummaryResponse>> ToPagedSummaryAsync(
        IQueryable<Product> query, int page, int pageSize)
    {
        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Select(p => ToSummary(p))
            .ToListAsync();

        return new PagedResult<ProductSummaryResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
    }

    private static ProductSummaryResponse ToSummary(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        CategoryId = p.CategoryId,
        CategoryName = p.Category != null ? p.Category.Name : null,
        Price = p.Price,
        StockQuantity = p.StockQuantity,
        IsActive = p.IsActive,
        IsFeatured = p.IsFeatured,
        IsAvailableForCart = p.IsAvailableForCart,
        CreatedAt = p.CreatedAt,
        PrimaryImageUrl = p.Images
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => i.ImageUrl)
            .FirstOrDefault()
    };

    private static ProductResponse ToResponse(Product p) => new()
    {
        Id = p.Id,
        Sku = p.Sku,
        Name = p.Name,
        CategoryId = p.CategoryId,
        CategoryName = p.Category != null ? p.Category.Name : null,
        ShortDescription = p.ShortDescription,
        Description = p.Description,
        Price = p.Price,
        StockQuantity = p.StockQuantity,
        IsActive = p.IsActive,
        IsFeatured = p.IsFeatured,
        IsAvailableForCart = p.IsAvailableForCart,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Images = p.Images
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new ProductImageResponse
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            })
            .ToList()
    };
}
