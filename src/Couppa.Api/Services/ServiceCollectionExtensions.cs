namespace Couppa.Api.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>Đăng ký toàn bộ Service của ứng dụng. Mở rộng dần khi thêm Service ở các phase sau.</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        return services;
    }
}
