# Task 09 — Caching

> Trích từ `docs/test/SRS.md`, Chương 11 (Caching Requirements).
> Nguồn tham chiếu: [Phụ lục B, mục 9](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

Áp dụng `IMemoryCache` cho categories/featured/latest/dashboard, xây dựng cơ chế invalidation nhất quán, dùng chung cho [Task 05 — Product](05-product.md), [Task 06 — Category](06-category.md), [Task 10 — Admin Dashboard](10-admin-dashboard.md).

## Cơ chế

**ASP.NET Core `IMemoryCache`** (in-process). Redis được ghi nhận là Future Enhancement (Chương 29 SRS) khi cần scale nhiều instance — không triển khai trong scope đồ án.

## Bảng cache đầy đủ

| Dữ liệu cache | Cache Key | TTL | Tạo khi | Đọc khi | Invalidate khi |
|---|---|---|---|---|---|
| Danh sách danh mục active | `categories:active` | 15 phút | Lần đầu có request lấy danh mục sau khi cache miss/hết hạn | Mọi request `GET /api/categories`, dropdown lọc sản phẩm | Admin thêm/sửa/xóa/đổi trạng thái danh mục |
| Sản phẩm nổi bật | `products:featured` | 10 phút | Request đầu tiên tới trang chủ sau khi cache miss | `GET /api/products/featured` | Admin thêm/sửa/xóa sản phẩm có cờ `is_featured`, hoặc đổi trạng thái |
| Sản phẩm mới nhất | `products:latest` | 5 phút | Request đầu tiên tới trang chủ sau khi cache miss | `GET /api/products/latest` | Admin tạo sản phẩm mới |
| Dashboard summary | `admin:dashboard:summary` | 5 phút | Admin mở Dashboard lần đầu sau khi cache miss | `GET /api/admin/dashboard/summary` | Không invalidate chủ động — để tự hết hạn theo TTL — **[Recommendation]** |

## Quy tắc invalidation cụ thể

- **Thêm/Sửa/Xóa sản phẩm** → xóa key `products:featured`, `products:latest` (nếu sản phẩm liên quan cờ đó); không cần xóa cache danh mục
- **Thêm/Sửa/Xóa/Đổi trạng thái danh mục** → xóa key `categories:active`
- Danh sách sản phẩm có **filter/search/sort** (`GET /api/products?...`) **không cache** — vì tổ hợp tham số quá nhiều, chi phí cache > lợi ích (đúng tinh thần "không thiết kế caching quá phức tạp")
- Giỏ hàng và thông tin cá nhân **không bao giờ cache** — dữ liệu riêng tư theo từng người dùng

## NFR liên quan

| ID | Yêu cầu |
|---|---|
| NFR-PERF-02 | Trang chủ/danh mục dùng cache để giảm tải truy vấn DB lặp lại |
| NFR-CACHE-01 | Cache áp dụng cho danh mục, sản phẩm nổi bật/mới nhất, số liệu dashboard — không cache dữ liệu cá nhân hóa (giỏ hàng, thông tin user) |

## Acceptance Criteria

**FR-DASH-001 – Dashboard summary cache**
> Given cache admin:dashboard:summary còn hiệu lực (chưa hết TTL 5 phút)
> When Admin mở lại trang Dashboard trong vòng 5 phút
> Then hệ thống trả dữ liệu từ cache, không truy vấn lại DB.

## Việc cần làm

1. Đăng ký `IMemoryCache` trong DI container (`Program.cs`)
2. Tạo 1 service/helper dùng chung (vd `ICacheService`) bọc `IMemoryCache` với 2 method chính: `GetOrCreateAsync(key, factory, ttl)` và `Remove(key)` — tránh lặp code ở nhiều Service
3. Áp dụng vào `CategoryService`, `ProductService`, `DashboardService` theo đúng bảng cache key/TTL ở trên (phối hợp Task 05/06/10)
4. Viết test xác nhận: gọi 2 lần liên tiếp `GET /api/categories` trong TTL → lần 2 không query DB (có thể verify qua log hoặc mock)
5. Viết test xác nhận: sau khi Admin sửa 1 category, `GET /api/categories` trả dữ liệu mới ngay (không đợi hết TTL)

## Done khi

- [ ] `categories:active`, `products:featured`, `products:latest`, `admin:dashboard:summary` đều được cache đúng TTL
- [ ] Sau khi CUD category → `categories:active` bị xóa ngay, request tiếp theo trả dữ liệu mới
- [ ] Sau khi CUD product liên quan `is_featured`/mới tạo → cache tương ứng bị xóa ngay
- [ ] Giỏ hàng và `/api/users/me` không bao giờ đọc/ghi qua `IMemoryCache`
