using Couppa.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Data.Seed;

/// <summary>Seed roles (bắt buộc), + dữ liệu demo category/product/admin cho môi trường dev/demo.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await SeedRolesAsync(db);
        await SeedAdminAsync(db);
        await SeedCategoriesAndProductsAsync(db);
    }

    private static async Task SeedRolesAsync(AppDbContext db)
    {
        if (await db.Roles.AnyAsync()) return;

        db.Roles.AddRange(
            new Role { Id = RoleIds.User, Name = "User" },
            new Role { Id = RoleIds.Admin, Name = "Admin" }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.RoleId == RoleIds.Admin)) return;

        var now = DateTimeOffset.UtcNow;
        db.Users.Add(new User
        {
            Email = "admin@couppa.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@12345"),
            FullName = "Quản trị viên",
            RoleId = RoleIds.Admin,
            IsLocked = false,
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedCategoriesAndProductsAsync(AppDbContext db)
    {
        if (await db.Categories.AnyAsync()) return;

        var now = DateTimeOffset.UtcNow;

        var categories = new[]
        {
            new Category { Name = "Điện thoại & Tablet", Slug = "dien-thoai-tablet", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Laptop & Tin học", Slug = "laptop-tin-hoc", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Phụ kiện", Slug = "phu-kien", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Âm thanh", Slug = "am-thanh", IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Category { Name = "Đồng hồ thông minh", Slug = "dong-ho-thong-minh", IsActive = true, CreatedAt = now, UpdatedAt = now },
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync();

        var random = new Random(42);
        var products = new List<Product>();
        var namesByCategory = new Dictionary<string, string[]>
        {
            ["dien-thoai-tablet"] = new[] { "Galaxy S24", "iPhone 15", "Xiaomi 14", "iPad Air", "Galaxy Tab S9" },
            ["laptop-tin-hoc"] = new[] { "MacBook Air M3", "Dell XPS 13", "ThinkPad X1", "Asus Zenbook", "HP Spectre" },
            ["phu-kien"] = new[] { "Sạc nhanh 65W", "Cáp USB-C", "Ốp lưng chống sốc", "Balo laptop", "Chuột không dây" },
            ["am-thanh"] = new[] { "Tai nghe chống ồn", "Loa Bluetooth mini", "Tai nghe true wireless", "Soundbar", "Micro thu âm" },
            ["dong-ho-thong-minh"] = new[] { "Watch Series 9", "Galaxy Watch 6", "Mi Band 8", "Garmin Venu", "Amazfit GTR" },
        };

        var skuCounter = 1;
        foreach (var category in categories)
        {
            foreach (var name in namesByCategory[category.Slug])
            {
                var stock = random.Next(0, 50);
                products.Add(new Product
                {
                    Sku = $"SKU{skuCounter++:D5}",
                    Name = name,
                    CategoryId = category.Id,
                    ShortDescription = $"{name} chính hãng, bảo hành 12 tháng.",
                    Description = $"{name} - sản phẩm công nghệ chất lượng cao thuộc danh mục {category.Name}.",
                    Price = random.Next(500, 500) == 0 ? 0 : random.Next(199_000, 49_990_000),
                    StockQuantity = stock,
                    IsActive = true,
                    IsFeatured = random.Next(0, 4) == 0,
                    IsDeleted = false,
                    CreatedAt = now.AddMinutes(-random.Next(0, 10000)),
                    UpdatedAt = now
                });
            }
        }

        db.Products.AddRange(products);
        await db.SaveChangesAsync();
    }
}
