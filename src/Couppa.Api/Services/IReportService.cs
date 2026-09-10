using Couppa.Api.Models.Responses;

namespace Couppa.Api.Services;

public interface IReportService
{
    Task<ReportSummaryResponse> GetSummaryAsync(DateTimeOffset? from, DateTimeOffset? to);

    /// <summary>
    /// FR-REPORT-004 — Top 10 sản phẩm được thêm giỏ nhiều nhất trong khoảng [from, to],
    /// mặc định 30 ngày gần nhất nếu không truyền. DB/log rỗng phải trả mảng rỗng, không throw
    /// (task 11, Done khi).
    /// </summary>
    Task<IReadOnlyList<CartTopProductResponse>> GetCartTopProductsAsync(DateTimeOffset? from, DateTimeOffset? to);
}
