# Task 11 — Reports

> Trích từ `docs/test/SRS.md`, Chương 5.9 (FR-REPORT), 13.8 (DB), 15.6 (API), 18 (Dashboard & Reporting).
> Nguồn tham chiếu: [Phụ lục B, mục 11](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

`ReportsController`, truy vấn `cart_activity_logs` cho báo cáo top sản phẩm được thêm giỏ nhiều nhất.

## Functional Requirements

| ID | Mô tả | Actor | Priority |
|---|---|---|---|
| FR-REPORT-001 | Thống kê số lượng sản phẩm theo danh mục | Admin | M |
| FR-REPORT-002 | Thống kê số lượng sản phẩm còn hàng/hết hàng | Admin | M |
| FR-REPORT-003 | Thống kê tổng số người dùng, số user mới trong kỳ | Admin | S |
| FR-REPORT-004 | Thống kê Top 10 sản phẩm được thêm vào giỏ hàng nhiều nhất (dựa trên `cart_activity_logs`) | Admin | S |

> Ghi chú: FR-REPORT-001/002 có thể tái sử dụng cùng query với Dashboard ([Task 10](10-admin-dashboard.md), `productsByCategory`/`outOfStockProducts`) — không cần viết lại logic, chỉ expose qua endpoint report riêng nếu cần filter theo khoảng thời gian.

## Database liên quan (chi tiết đầy đủ ở [Task 01](01-database.md))

### Bảng `cart_activity_logs` — **[Assumption A-03]**

Bảng bổ sung ngoài danh sách gốc của đề bài, bắt buộc để đáp ứng FR-REPORT-004. Append-only, không update/delete.

| Column | Type | Ghi chú |
|---|---|---|
| id | BIGSERIAL | PK |
| product_id | BIGINT | FK → products.id |
| user_id | BIGINT | FK → users.id, NULL nếu Guest |
| session_id | UUID | NULL nếu User |
| action | VARCHAR(20) | 'add' / 'remove' |
| quantity | INTEGER | |
| created_at | TIMESTAMPTZ | |

Index: `idx_cart_activity_logs_product_id`, `idx_cart_activity_logs_created_at`. Dữ liệu được ghi từ [Task 07 — Cart](07-cart.md) mỗi lần add/remove item.

## Yêu cầu hiển thị

- Biểu đồ tối thiểu 1 loại (bar/pie chart) cho báo cáo — **[Recommendation]** dùng Chart.js
- Không xây dựng báo cáo doanh thu (ngoài phạm vi — hệ thống không thanh toán)
- Report export CSV — **[Assumption]** không bắt buộc, đánh dấu Could-have

## API Specification

#### GET /api/admin/reports/cart-top-products
- **Auth required**: Có | **Role**: Admin
- **Query**: `from`, `to` (khoảng thời gian, mặc định 30 ngày gần nhất)
- **Response 200**: Top 10 sản phẩm theo số lần `action='add'` trong `cart_activity_logs`

```json
{
  "success": true,
  "data": [
    { "productId": 12, "productName": "...", "addCount": 87 },
    { "productId": 45, "productName": "...", "addCount": 62 }
  ]
}
```
(**[Recommendation]** cấu trúc response cụ thể — SRS gốc chỉ mô tả nội dung, không định nghĩa field name)

## Việc cần làm

1. `ReportsController`: endpoint `GET /api/admin/reports/cart-top-products`, gắn Policy Admin (xem [Task 04](04-authorization.md))
2. Query: `SELECT product_id, COUNT(*) FROM cart_activity_logs WHERE action='add' AND created_at BETWEEN @from AND @to GROUP BY product_id ORDER BY COUNT(*) DESC LIMIT 10`, join `products` để lấy tên
3. (Nếu chưa có ở Dashboard) endpoint/method cho FR-REPORT-001/002/003 — số lượng sản phẩm theo danh mục, còn hàng/hết hàng, tổng user + user mới trong kỳ
4. Frontend: bảng + biểu đồ hiển thị top sản phẩm

## Done khi

- [ ] `GET /api/admin/reports/cart-top-products` trả đúng Top 10 sản phẩm theo số lần thêm giỏ, sắp xếp giảm dần
- [ ] Filter theo khoảng thời gian `from`/`to` hoạt động đúng
- [ ] Hệ thống chưa có dữ liệu `cart_activity_logs` → trả về mảng rỗng, không lỗi 500
- [ ] Report số lượng sản phẩm theo danh mục và còn hàng/hết hàng khớp với dữ liệu thực tế trong `products`
