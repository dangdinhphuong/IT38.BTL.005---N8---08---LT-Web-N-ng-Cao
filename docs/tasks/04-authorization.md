# Task 04 — Authorization

> Trích từ `docs/test/SRS.md`, Chương 5.2 (FR-AUTHZ), 6 (BR-08), 9.3 (Authorization Model), 10 (Security), 21 (Permission Matrix), 26 (Acceptance Criteria).
> Nguồn tham chiếu: [Phụ lục B, mục 4](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

Middleware/Policy kiểm tra Role, áp dụng `[Authorize(Roles=...)]` cho toàn bộ endpoint admin. Đây là lớp bảo vệ dùng chung cho mọi module còn lại (Product, Category, User, Dashboard, Report).

## Functional Requirements

| ID | Mô tả | Actor | Priority |
|---|---|---|---|
| FR-AUTHZ-001 | Mọi endpoint `/api/admin/**` yêu cầu role = Admin, kiểm tra ở backend | System | M |
| FR-AUTHZ-002 | User không có quyền Admin bị từ chối với HTTP 403 khi gọi API quản trị | System | M |
| FR-AUTHZ-003 | Guest bị từ chối với HTTP 401 khi gọi API yêu cầu đăng nhập | System | M |
| FR-ADMIN-001 | Đăng nhập khu vực quản trị dùng chung cơ chế Cookie Auth, kiểm tra role = Admin | Admin | M |
| FR-ADMIN-002 | Truy cập trực tiếp URL/API admin khi chưa đủ quyền → 401/403, không lộ dữ liệu | System | M |

## Business Rules

| ID | Quy tắc |
|---|---|
| BR-08 | Chỉ role Admin được truy cập các API dưới `/api/admin/**` |

## Authorization Model

- Role-based, 2 role cố định: `User`, `Admin` (lưu ở bảng `roles`, tham chiếu qua `users.role_id`)
- Kiểm tra quyền bằng `[Authorize]` / `[Authorize(Roles = "Admin")]` ở tầng Controller — **backend luôn là điểm quyết định cuối cùng**, frontend chỉ ẩn/hiện UI để trải nghiệm tốt hơn, không được xem là lớp bảo mật
- Toàn bộ endpoint dưới `/api/admin/**` bắt buộc `Roles = "Admin"`
- Endpoint thao tác giỏ hàng/profile cho phép cả Guest (session) và User (đăng nhập) tùy loại

## Security Requirements áp dụng

| ID | Kỹ thuật | Mô tả áp dụng |
|---|---|---|
| SEC-03 | Authorization | Kiểm tra role ở Controller/Middleware backend cho mọi endpoint nhạy cảm |
| SEC-10 | Backend Authorization Check | Không tin bất kỳ `role`/`user_id` nào gửi từ client; luôn lấy từ Session/Claims đã xác thực |

**Phân biệt Frontend vs Backend Validation:**

| | Frontend Validation | Backend Validation |
|---|---|---|
| Mục đích | UX tức thời, giảm round-trip không cần thiết | Đảm bảo tính toàn vẹn & bảo mật dữ liệu |
| Có thể bị bỏ qua? | Có (dev tools, gọi API trực tiếp) | Không — luôn thực thi |
| Vai trò | Gợi ý, cảnh báo sớm | **Quyết định cuối cùng** về hợp lệ & quyền truy cập |

## Permission Matrix (dùng để review đủ endpoint đã gắn policy)

| Function | Guest | User | Admin |
|---|---|---|---|
| Xem sản phẩm / chi tiết | ✓ | ✓ | ✓ |
| Tìm kiếm / Lọc / Sắp xếp | ✓ | ✓ | ✓ |
| Thêm vào giỏ hàng | ✓ | ✓ | ✓ |
| Xem giỏ hàng / sửa số lượng / xóa item | ✓ | ✓ | ✓ |
| Đăng ký | ✓ | — | — |
| Đăng nhập / Đăng xuất | — | ✓ | ✓ |
| Xem thông tin cá nhân | ✗ | ✓ | ✓ |
| Cập nhật thông tin cá nhân | ✗ | ✓ | ✓ |
| Đổi mật khẩu | ✗ | ✓ | ✓ |
| Quản lý sản phẩm (CRUD) | ✗ | ✗ | ✓ |
| Quản lý danh mục (CRUD) | ✗ | ✗ | ✓ |
| Quản lý người dùng (xem/khóa/đổi role) | ✗ | ✗ | ✓ |
| Xem Dashboard | ✗ | ✗ | ✓ |
| Xem báo cáo thống kê | ✗ | ✗ | ✓ |
| Truy cập `/api/admin/**` | ✗ | ✗ | ✓ |

## Error Handling

| HTTP Status | Khi nào dùng |
|---|---|
| 401 Unauthorized | Chưa đăng nhập nhưng endpoint yêu cầu |
| 403 Forbidden | Đã đăng nhập nhưng không đủ quyền, hoặc tài khoản bị khóa |

## Acceptance Criteria

**FR-AUTHZ-002 – Unauthorized access to admin API**
> Given User đã đăng nhập (role User, không phải Admin)
> When User gọi trực tiếp `POST /api/admin/products`
> Then hệ thống trả về 403, không thực hiện thao tác.

## Việc cần làm

1. Tạo Policy `RequireAdmin` (`Roles = "Admin"`) trong `Program.cs`
2. Áp dụng `[Authorize(Policy = "RequireAdmin")]` cho toàn bộ Controller/action dưới `/api/admin/**`
3. Áp dụng `[Authorize]` (không ràng buộc role) cho các endpoint yêu cầu đăng nhập nhưng không cần Admin (vd `/api/users/me`)
4. Review lại toàn bộ Permission Matrix ở trên, đối chiếu từng endpoint đã implement (phối hợp task 05-10)
5. Viết test thử gọi trực tiếp URL/API admin bằng tài khoản User → xác nhận 403, không có dữ liệu nào bị lộ trong response lỗi

## Done khi

- [ ] Toàn bộ endpoint `/api/admin/**` đã gắn Policy Admin
- [ ] Gọi API admin khi chưa đăng nhập → 401
- [ ] Gọi API admin bằng tài khoản role User → 403
- [ ] Đối chiếu đủ 15 dòng trong Permission Matrix, không có endpoint nào thiếu kiểm tra quyền
