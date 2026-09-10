using Couppa.Api.Data;
using Couppa.Api.Data.Entities;
using Couppa.Api.Models.Responses;
using Microsoft.EntityFrameworkCore;

namespace Couppa.Api.Services;

public class ReportService : IReportService
{
    private const int DefaultRangeDays = 30;
    private const int TopProductsLimit = 10;

    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CartTopProductResponse>> GetCartTopProductsAsync(DateTimeOffset? from, DateTimeOffset? to)
    {
        var rangeTo = to ?? DateTimeOffset.UtcNow;
        var rangeFrom = from ?? rangeTo.AddDays(-DefaultRangeDays);

        // [Assumption] Task 11 không nói rõ có hiển thị sản phẩm đã soft-delete trong report hay không.
        // Chọn lọc bỏ: join với _db.Products (có Global Query Filter !IsDeleted) nên sản phẩm đã xóa
        // tự động không xuất hiện trong kết quả - nhất quán với cách Public/Admin khác đều không thấy
        // sản phẩm đã xóa ở bất kỳ đâu trong hệ thống.
        var query =
            from log in _db.CartActivityLogs
            where log.Action == CartActivityAction.Add
                  && log.CreatedAt >= rangeFrom
                  && log.CreatedAt <= rangeTo
            join product in _db.Products on log.ProductId equals product.Id
            group product by new { product.Id, product.Name } into g
            orderby g.Count() descending
            select new CartTopProductResponse
            {
                ProductId = g.Key.Id,
                ProductName = g.Key.Name,
                AddCount = g.Count()
            };

        // cart_activity_logs rỗng (hệ thống chưa có dữ liệu) -> query trả danh sách rỗng tự nhiên,
        // không throw (task 11, Done khi).
        return await query
            .Take(TopProductsLimit)
            .ToListAsync();
    }
}
