# Task 10 — Admin Dashboard

> Trích từ `docs/test/SRS.md`, Chương 5.8 (FR-DASH), 7.7 (UC-06), 15.6 (API), 18 (Dashboard & Reporting), 26 (Acceptance Criteria).
> Nguồn tham chiếu: [Phụ lục B, mục 10](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

`DashboardController`, `DashboardService`, tính toán số liệu tổng hợp cho trang quản trị.

## Functional Requirements

| ID | Mô tả | Actor | Priority |
|---|---|---|---|
| FR-DASH-001 | Xem tổng số sản phẩm, danh mục, người dùng | Admin | M |
| FR-DASH-002 | Xem số sản phẩm đang Active / hết hàng | Admin | M |
| FR-DASH-003 | Xem biểu đồ số lượng sản phẩm theo danh mục | Admin | S |
| FR-DASH-004 | Xem danh sách 5-10 sản phẩm mới thêm gần đây | Admin | S |

## Use Case

### UC-06: Xem Dashboard (Admin)

| | |
|---|---|
| **Actor** | Admin |
| **Goal** | Xem tổng quan hệ thống |
| **Preconditions** | Đã đăng nhập với role Admin |
| **Main Flow** | 1. Admin mở trang Dashboard<br>2. Gọi `GET /api/admin/dashboard/summary`<br>3. Backend đọc cache (nếu có) hoặc tính toán và cache lại (TTL 5 phút)<br>4. Trả về các số liệu tổng hợp |
| **Alternative Flow** | Không có |
| **Exception Flow** | Không có dữ liệu (hệ thống mới) → trả về giá trị 0, không lỗi |
| **Postconditions** | Không thay đổi dữ liệu |

## Yêu cầu hiển thị bổ sung

- Dashboard hiển thị dữ liệu **gần đúng thời gian thực** (cache TTL 5 phút là đủ chấp nhận được cho đồ án — không cần real-time tuyệt đối)
- Biểu đồ tối thiểu 1 loại (bar chart hoặc pie chart) cho "số lượng sản phẩm theo danh mục" — **[Recommendation]** dùng Chart.js (nhẹ, dễ tích hợp)
- Không xây dựng báo cáo doanh thu (ngoài phạm vi — hệ thống không thanh toán)

## Caching (chi tiết cơ chế dùng chung ở [Task 09](09-caching.md))

| Dữ liệu cache | Cache Key | TTL | Invalidate khi |
|---|---|---|---|
| Dashboard summary | `admin:dashboard:summary` | 5 phút | Không invalidate chủ động — để tự hết hạn theo TTL — **[Recommendation]** |

## API Specification

#### GET /api/admin/dashboard/summary
- **Auth required**: Có | **Role**: Admin
- **Response 200**:
```json
{
  "success": true,
  "data": {
    "totalProducts": 134,
    "totalCategories": 12,
    "totalUsers": 58,
    "activeProducts": 120,
    "outOfStockProducts": 14,
    "productsByCategory": [ { "categoryName": "Điện thoại", "count": 30 } ],
    "recentProducts": [ { "id": 99, "name": "...", "createdAt": "..." } ]
  }
}
```
(Đọc từ cache `admin:dashboard:summary`, TTL 5 phút)

## UI/UX

- **Dashboard**: các thẻ số liệu (KPI card) + biểu đồ (Chart.js hoặc tương đương) cho sản phẩm theo danh mục

## Acceptance Criteria

**FR-DASH-001 – Dashboard summary cache**
> Given cache admin:dashboard:summary còn hiệu lực (chưa hết TTL 5 phút)
> When Admin mở lại trang Dashboard trong vòng 5 phút
> Then hệ thống trả dữ liệu từ cache, không truy vấn lại DB.

## Việc cần làm

1. `DashboardController`: endpoint `GET /api/admin/dashboard/summary`, gắn Policy Admin (xem [Task 04](04-authorization.md))
2. `DashboardService`: query tổng hợp `totalProducts`, `totalCategories`, `totalUsers`, `activeProducts`, `outOfStockProducts` (stock_quantity=0), `productsByCategory` (GROUP BY category), `recentProducts` (top 5-10 theo `created_at DESC`)
3. Tích hợp cache `admin:dashboard:summary` TTL 5 phút (phối hợp [Task 09](09-caching.md))
4. Frontend: KPI card + biểu đồ Chart.js cho `productsByCategory`

## Done khi

- [ ] Gọi API trả đủ 7 trường số liệu như spec
- [ ] Hệ thống chưa có dữ liệu (DB rỗng) → trả về 0 cho mọi số liệu, không lỗi 500
- [ ] Gọi lại trong vòng 5 phút → dữ liệu lấy từ cache (không query DB lần 2)
- [ ] Biểu đồ hiển thị đúng số lượng sản phẩm theo từng danh mục
