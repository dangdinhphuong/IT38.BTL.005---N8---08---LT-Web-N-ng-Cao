# Manual Test Checklist — Couppa Web API (Phase 13)

> Nguồn: `docs/test/tasks/14-testing.md` (Permission Matrix + 9 Acceptance Criteria).
> Người test tự điền cột **"Kết quả thực tế"** (PASS/FAIL) và **"Ghi chú"** khi thực hiện thủ công.

## 1. Chuẩn bị 3 tài khoản test

| Role | Cách có tài khoản | Thông tin |
|---|---|---|
| **Guest** | Không đăng nhập (mở trình duyệt ẩn danh / xóa cookie trước khi test) | Không cần tài khoản |
| **User** | Tự đăng ký qua `register.html` (hoặc `POST /api/auth/register`) | Email/password tự chọn, ví dụ: `tester.user@example.com` / `Abc12345` (đúng BR-06: ≥ 8 ký tự, ≥ 1 chữ hoa, ≥ 1 chữ số) |
| **Admin** | Tài khoản đã được `DbSeeder.SeedAsync` seed sẵn khi chạy môi trường Development (xem `Couppa.Api/Data/Seed/DbSeeder.cs`, method `SeedAdminAsync`) | Email: `admin@couppa.com` / Password: `Admin@12345` |

Ghi chú kỹ thuật (tham chiếu `DbSeeder.cs`):
- Seed chỉ chạy khi `app.Environment.IsDevelopment()` là `true` trong `Program.cs` — tức là chạy `dotnet run` ở môi trường Development (mặc định `ASPNETCORE_ENVIRONMENT=Development`), không chạy ở Production.
- `SeedAdminAsync` chỉ seed nếu **chưa có** user nào có `RoleId == RoleIds.Admin` trong DB — nếu DB đã có Admin khác từ trước, tài khoản `admin@couppa.com` có thể không được tạo lại.

## 2. Cách chạy test thủ công

1. Khởi động backend (`dotnet run` trong `Couppa.Api/`, môi trường Development để có seed data) và frontend (theo README Phase 11/14).
2. Với mỗi dòng trong Permission Matrix (mục 3) và mỗi Acceptance Criteria (mục 4): thực hiện thao tác tương ứng với TỪNG role (Guest/User/Admin theo đúng cột ✓/✗/— của dòng đó), ghi PASS nếu hành vi thực tế khớp kỳ vọng (✓ = cho phép, ✗ = bị chặn/403, — = không áp dụng do luồng nghiệp vụ không cho phép, ví dụ Guest không đăng nhập được nên không "Đăng ký" khi đã có tài khoản).
3. Test trên tối thiểu 3 breakpoint màn hình (theo `docs/rules/UI_UX/web/responsive.md`): Mobile, Tablet, Desktop.
4. Điền PASS/FAIL vào cột "Kết quả thực tế"; nếu FAIL, mô tả rõ hành vi thực tế ở cột "Ghi chú" (kèm mã lỗi HTTP nếu có).

## 3. Permission Matrix

| Function | Guest | User | Admin | Kết quả thực tế (Guest) | Kết quả thực tế (User) | Kết quả thực tế (Admin) | Ghi chú |
|---|---|---|---|---|---|---|---|
| Xem sản phẩm / chi tiết | ✓ | ✓ | ✓ | | | | |
| Tìm kiếm / Lọc / Sắp xếp | ✓ | ✓ | ✓ | | | | |
| Thêm vào giỏ hàng | ✓ | ✓ | ✓ | | | | |
| Xem giỏ hàng / sửa số lượng / xóa item | ✓ | ✓ | ✓ | | | | |
| Đăng ký | ✓ | — | — | | | | |
| Đăng nhập / Đăng xuất | — | ✓ | ✓ | | | | |
| Xem thông tin cá nhân | ✗ | ✓ | ✓ | | | | |
| Cập nhật thông tin cá nhân | ✗ | ✓ | ✓ | | | | |
| Đổi mật khẩu | ✗ | ✓ | ✓ | | | | |
| Quản lý sản phẩm (CRUD) | ✗ | ✗ | ✓ | | | | |
| Quản lý danh mục (CRUD) | ✗ | ✗ | ✓ | | | | |
| Quản lý người dùng (xem/khóa/đổi role) | ✗ | ✗ | ✓ | | | | |
| Xem Dashboard | ✗ | ✗ | ✓ | | | | |
| Xem báo cáo thống kê | ✗ | ✗ | ✓ | | | | |
| Truy cập `/api/admin/**` | ✗ | ✗ | ✓ | | | | |

> Quy ước điền: PASS = hành vi thực tế khớp cột ✓/✗/— của Permission Matrix gốc; FAIL = không khớp (ghi rõ HTTP status/UI thực tế nhận được ở cột Ghi chú).

## 4. Acceptance Criteria — Checklist

- [ ] **FR-CART-001 – Add Product To Cart**: Given sản phẩm đang Active và còn tồn kho, When User/Guest thêm sản phẩm vào giỏ, Then hệ thống thêm sản phẩm vào cart và cập nhật cart quantity.
  - Kết quả thực tế: __________ Ghi chú: __________

- [ ] **FR-CART-001 (Exception)**: Given sản phẩm Inactive hoặc hết hàng, When User/Guest cố thêm sản phẩm vào giỏ, Then hệ thống trả về 409 và không tạo/sửa cart_items.
  - Kết quả thực tế: __________ Ghi chú: __________

- [ ] **FR-CART-007 – Merge Cart On Login**: Given Guest có giỏ hàng với 2 sản phẩm (session cart) và đăng nhập vào tài khoản User đã có sẵn 1 sản phẩm trùng trong giỏ, When đăng nhập thành công, Then giỏ hàng User sau merge có 3 dòng sản phẩm phân biệt, số lượng sản phẩm trùng được cộng dồn (không vượt tồn kho).
  - Kết quả thực tế: __________ Ghi chú: __________

- [ ] **FR-AUTH-002 – Login**: Given tài khoản tồn tại, đúng password, không bị khóa, When User submit form đăng nhập, Then hệ thống tạo Session, set Cookie, trả về 200 kèm thông tin user.
  - Kết quả thực tế: __________ Ghi chú: __________

- [ ] **FR-AUTH-005 – Locked account**: Given tài khoản có is_locked = true, When User đăng nhập đúng email/password, Then hệ thống trả về 403 và không tạo Session.
  - Kết quả thực tế: __________ Ghi chú: __________

- [ ] **FR-PRODUCT-001 – Create Product**: Given Admin đã đăng nhập, dữ liệu hợp lệ, SKU chưa tồn tại, When Admin submit form thêm sản phẩm, Then hệ thống tạo sản phẩm mới, trả 201, invalidate cache products:featured/latest.
  - Kết quả thực tế: __________ Ghi chú: __________

- [ ] **FR-CATEGORY-003 – Delete Category with products**: Given category đang có ít nhất 1 sản phẩm liên kết, When Admin xóa category đó, Then hệ thống trả về 409 kèm số lượng sản phẩm liên quan, không xóa category.
  - Kết quả thực tế: __________ Ghi chú: __________

- [ ] **FR-AUTHZ-002 – Unauthorized access to admin API**: Given User đã đăng nhập (role User, không phải Admin), When User gọi trực tiếp `POST /api/admin/products`, Then hệ thống trả về 403, không thực hiện thao tác.
  - Kết quả thực tế: __________ Ghi chú: __________

- [ ] **FR-DASH-001 – Dashboard summary cache**: Given cache admin:dashboard:summary còn hiệu lực (chưa hết TTL 5 phút), When Admin mở lại trang Dashboard trong vòng 5 phút, Then hệ thống trả dữ liệu từ cache, không truy vấn lại DB.
  - Kết quả thực tế: __________ Ghi chú: __________

## 5. Kết luận

- Tổng số dòng Permission Matrix FAIL: __________
- Tổng số Acceptance Criteria FAIL: __________
- Kết luận chung (đạt "Done khi" của task 14 hay chưa): __________
