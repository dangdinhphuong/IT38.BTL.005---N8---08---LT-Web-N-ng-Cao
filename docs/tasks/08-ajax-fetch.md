# Task 08 — AJAX / Fetch

> Trích từ `docs/test/SRS.md`, Chương 12 (AJAX/Fetch API Requirements).
> Nguồn tham chiếu: [Phụ lục B, mục 8](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

Tích hợp Fetch API ở frontend cho toàn bộ thao tác tương tác (search/filter/pagination/cart actions/admin CRUD), không reload trang.

## Yêu cầu áp dụng

Các chức năng bắt buộc dùng Fetch API:

| Chức năng | Endpoint | Trigger |
|---|---|---|
| Tìm kiếm sản phẩm | `GET /api/products?search=` | Debounce 300ms khi gõ, hoặc submit form search |
| Lọc theo danh mục/giá | `GET /api/products?category=&minPrice=&maxPrice=` | Khi chọn filter |
| Sắp xếp | `GET /api/products?sort=` | Khi đổi dropdown sort |
| Phân trang | `GET /api/products?page=&pageSize=` | Khi bấm nút trang / infinite scroll |
| Thêm vào giỏ hàng | `POST /api/cart/items` | Khi bấm "Thêm vào giỏ" |
| Cập nhật số lượng giỏ hàng | `PUT /api/cart/items/{id}` | Khi bấm +/- trong giỏ hàng |
| Xóa sản phẩm khỏi giỏ | `DELETE /api/cart/items/{id}` | Khi bấm icon xóa |
| CRUD sản phẩm (Admin) | `POST/PUT/DELETE /api/admin/products` | Submit form trong modal, không reload trang danh sách |
| CRUD danh mục (Admin) | `POST/PUT/DELETE /api/admin/categories` | Tương tự |
| Đổi trạng thái sản phẩm/danh mục | `PATCH /api/admin/products/{id}/status` | Toggle switch trong bảng |
| Dashboard statistics | `GET /api/admin/dashboard/summary` | Khi mở trang Dashboard |

## Quy ước Request/Response chung

- Mọi request: `Content-Type: application/json`, `credentials: 'include'` (để gửi Cookie — bắt buộc vì hệ thống dùng Cookie Authentication, xem [Task 03](03-authentication.md))
- Request POST/PUT/PATCH/DELETE bắt buộc kèm header CSRF token: `X-CSRF-TOKEN` (xem [Task 13 — Security](13-security.md))
- Response thành công: `{ "success": true, "data": { ... } }`
- Response lỗi: theo format thống nhất (xem [Task 02 — Backend Foundation](02-backend-foundation.md), Chương 19 SRS)
- Danh sách có phân trang trả kèm metadata:
```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 134,
    "totalPages": 7
  }
}
```

## Việc cần làm

1. Viết module `apiClient.js` (hoặc tương đương) dùng chung: tự set `credentials: 'include'`, tự đính `X-CSRF-TOKEN`, tự parse response theo format `{success, data}` / `{success, error}`
2. Public site: tích hợp Fetch cho search (debounce 300ms), filter, sort, pagination — không reload trang
3. Cart: tích hợp Fetch cho add/update/remove/clear, cập nhật badge số lượng giỏ hàng ngay sau mỗi thao tác (optimistic hoặc chờ response)
4. Admin: tích hợp Fetch cho toàn bộ modal CRUD Product/Category, toggle trạng thái, Dashboard/Reports
5. Xử lý lỗi thống nhất: hiển thị `error.message` từ response, xử lý riêng 401 (redirect trang login) và 403 (thông báo không đủ quyền)

## Done khi

- [ ] Tìm kiếm/lọc/sắp xếp/phân trang sản phẩm không gây reload trang
- [ ] Thao tác giỏ hàng (add/update/remove/clear) không gây reload trang, badge số lượng cập nhật đúng
- [ ] CRUD sản phẩm/danh mục trong Admin qua modal, danh sách tự refresh sau khi submit thành công, không reload trang
- [ ] Toggle trạng thái Active/Inactive cập nhật ngay lập tức qua Fetch
- [ ] Lỗi 401/403/409/422 từ API được hiển thị đúng thông báo tương ứng trên UI
