using Couppa.Api.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Couppa.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CartActivityLog> CartActivityLogs => Set<CartActivityLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Cart activity rows intentionally keep a required ProductId while Product has a
        // soft-delete query filter. The relationship is still valid for reporting; suppress
        // EF's design-time warning so migrations can be generated without a Windows EventLog
        // provider turning the warning into an exception.
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // The PostgreSQL schema uses the snake_case column names from the SRS
        // constraints, while the SQL Server model keeps EF's conventional
        // PascalCase column names. Raw SQL used by checks/filtered indexes must
        // therefore match the active provider.
        var isPostgreSql = Database.ProviderName?.Contains(
            "Npgsql", StringComparison.OrdinalIgnoreCase) == true;
        var priceColumn = isPostgreSql ? "price" : "[Price]";
        var stockQuantityColumn = isPostgreSql ? "stock_quantity" : "[StockQuantity]";
        var quantityColumn = isPostgreSql ? "quantity" : "[Quantity]";
        var userColumn = isPostgreSql ? "user_id" : "[UserId]";
        var sessionColumn = isPostgreSql ? "session_id" : "[SessionId]";

        // ---- ApplicationUser (mở rộng AspNetUsers) ----
        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Phone).HasMaxLength(20);
            e.Property(x => x.IsLocked).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        // ---- categories ----
        modelBuilder.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Slug).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasIndex(x => x.IsActive).HasDatabaseName("idx_categories_is_active");
        });

        // ---- products ----
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products", t =>
            {
                // BR-09 / BR-10: giá và tồn kho không được âm.
                t.HasCheckConstraint("ck_products_price_non_negative", $"{priceColumn} >= 0");
                t.HasCheckConstraint("ck_products_stock_non_negative", $"{stockQuantityColumn} >= 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Sku).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Sku).IsUnique();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.ShortDescription).HasMaxLength(500);
            e.Property(x => x.Price).HasColumnType("decimal(12,2)");
            e.Property(x => x.StockQuantity).HasDefaultValue(0);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.IsFeatured).HasDefaultValue(false);
            e.Property(x => x.IsDeleted).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            // BR-03: không cho xóa Category còn Product -> RESTRICT (EF Core mặc định Cascade cho FK bắt buộc).
            e.HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => x.CategoryId).HasDatabaseName("idx_products_category_id");
            e.HasIndex(x => x.IsActive).HasDatabaseName("idx_products_is_active");
            e.HasIndex(x => x.IsDeleted).HasDatabaseName("idx_products_is_deleted");
            e.HasIndex(x => x.Name).HasDatabaseName("idx_products_name");

            // Global Query Filter (Phase 6 - Product): tự động loại is_deleted=true ở MỌI truy vấn
            // (Public lẫn Admin đều không được xem sản phẩm đã xóa - task 05), tránh sót WHERE is_deleted=false.
            e.HasQueryFilter(p => !p.IsDeleted);
        });

        // ---- product_images ----
        modelBuilder.Entity<ProductImage>(e =>
        {
            e.ToTable("product_images");
            e.HasKey(x => x.Id);
            e.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
            e.Property(x => x.IsPrimary).HasDefaultValue(false);
            e.Property(x => x.SortOrder).HasDefaultValue((short)0);

            e.HasOne(x => x.Product)
                .WithMany(p => p.Images)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.ProductId).HasDatabaseName("idx_product_images_product_id");
        });

        // ---- carts ----
        modelBuilder.Entity<Cart>(e =>
        {
            e.ToTable("carts", t =>
            {
                // BR-12: cart chỉ thuộc đúng 1 trong 2 (user hoặc session), không thể cả hai / không cả hai.
                t.HasCheckConstraint("ck_carts_owner_xor",
                    $"({userColumn} IS NOT NULL AND {sessionColumn} IS NULL) OR ({userColumn} IS NULL AND {sessionColumn} IS NOT NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasIndex(x => x.UserId).IsUnique().HasFilter($"{userColumn} IS NOT NULL");
            e.HasIndex(x => x.SessionId).IsUnique().HasFilter($"{sessionColumn} IS NOT NULL");

            e.HasOne(x => x.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ---- cart_items ----
        modelBuilder.Entity<CartItem>(e =>
        {
            e.ToTable("cart_items", t =>
            {
                t.HasCheckConstraint("ck_cart_items_quantity_positive", $"{quantityColumn} > 0");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Quantity).HasDefaultValue(1);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasOne(x => x.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            // Không cho FK tới Product cascade xóa cart_items khi Product bị xóa cứng
            // (thực tế Product chỉ soft-delete nên trường hợp này hiếm khi xảy ra - BR-11).
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
            e.HasIndex(x => x.CartId).HasDatabaseName("idx_cart_items_cart_id");
            e.HasIndex(x => x.ProductId).HasDatabaseName("idx_cart_items_product_id");
        });

        // ---- cart_activity_logs ----
        modelBuilder.Entity<CartActivityLog>(e =>
        {
            e.ToTable("cart_activity_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.Action).HasMaxLength(20).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasIndex(x => x.ProductId).HasDatabaseName("idx_cart_activity_logs_product_id");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_cart_activity_logs_created_at");
            e.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ---- audit_logs ----
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.ActorUserId).HasMaxLength(450);
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
            // PostgreSQL supports JSONB; SQL Server stores the same audit payload as
            // nvarchar(max). Keep the model provider-aware because local development
            // uses SQL Server Express while Docker uses PostgreSQL.
            e.Property(x => x.DetailJson)
                .HasColumnType(isPostgreSql ? "jsonb" : "nvarchar(max)")
                .HasColumnName("detail");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            e.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("idx_audit_logs_entity");
            e.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_audit_logs_created_at");
            e.HasOne(x => x.ActorUser)
                .WithMany()
                .HasForeignKey(x => x.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
