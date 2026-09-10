using Couppa.Api.Models.Responses;

namespace Couppa.Api.Services;

public interface IDashboardService
{
    /// <summary>
    /// FR-DASH-001..004 — số liệu tổng hợp cho trang quản trị.
    /// DB rỗng phải trả 0 / mảng rỗng cho mọi trường, không được throw (task 10, Exception Flow).
    /// Đọc/ghi qua cache admin:dashboard:summary, TTL 5 phút (không invalidate chủ động).
    /// </summary>
    Task<DashboardSummaryResponse> GetSummaryAsync();
}
