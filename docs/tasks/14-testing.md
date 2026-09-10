# Task 14 — Testing

> Trích từ `docs/test/SRS.md`, Chương 25 (Testing Requirements), 26 (Acceptance Criteria — đầy đủ), 27 (Traceability Matrix), 21 (Permission Matrix).
> Nguồn tham chiếu: [Phụ lục B, mục 14](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

Viết unit test (Services) + integration test (API flow chính) theo yêu cầu Chương 25, xác nhận toàn bộ Acceptance Criteria đạt.

## Testing Requirements

| Loại test | Phạm vi | Công cụ đề xuất |
|---|---|---|
| Unit Test | Business logic trong Services (đặc biệt: merge cart BR-13, validate tồn kho BR-02, tính tổng giỏ hàng) | xUnit + Moq |
| Integration Test | API endpoint quan trọng (login, CRUD product, cart flow) chạy với DB test | xUnit + `WebApplicationFactory` |
| Manual Test | Toàn bộ UI flow theo Acceptance Criteria | Kiểm thử thủ công trên 3 breakpoint |

**Phạm vi tối thiểu bắt buộc** (đủ demo/bảo vệ đồ án — không yêu cầu coverage 100%):
- Unit test cho: `CartService.AddItem`, `CartService.MergeGuestCart`, `ProductService.Create` (validate SKU/price/stock)
- Integration test cho: luồng Register → Login → Add to Cart → View Cart
- Manual test checklist cho toàn bộ Permission Matrix (xem [Task 04](04-authorization.md))

## Acceptance Criteria (Given/When/Then) — toàn bộ, dùng làm test case

**FR-CART-001 – Add Product To Cart**
> Given sản phẩm đang Active và còn tồn kho
> When User/Guest thêm sản phẩm vào giỏ
> Then hệ thống thêm sản phẩm vào cart và cập nhật cart quantity.

**FR-CART-001 (Exception)**
> Given sản phẩm Inactive hoặc hết hàng
> When User/Guest cố thêm sản phẩm vào giỏ
> Then hệ thống trả về 409 và không tạo/sửa cart_items.

**FR-CART-007 – Merge Cart On Login**
> Given Guest có giỏ hàng với 2 sản phẩm (session cart) và đăng nhập vào tài khoản User đã có sẵn 1 sản phẩm trùng trong giỏ
> When đăng nhập thành công
> Then giỏ hàng User sau merge có 3 dòng sản phẩm phân biệt, số lượng sản phẩm trùng được cộng dồn (không vượt tồn kho).

**FR-AUTH-002 – Login**
> Given tài khoản tồn tại, đúng password, không bị khóa
> When User submit form đăng nhập
> Then hệ thống tạo Session, set Cookie, trả về 200 kèm thông tin user.

**FR-AUTH-005 – Locked account**
> Given tài khoản có is_locked = true
> When User đăng nhập đúng email/password
> Then hệ thống trả về 403 và không tạo Session.

**FR-PRODUCT-001 – Create Product**
> Given Admin đã đăng nhập, dữ liệu hợp lệ, SKU chưa tồn tại
> When Admin submit form thêm sản phẩm
> Then hệ thống tạo sản phẩm mới, trả 201, invalidate cache products:featured/latest.

**FR-CATEGORY-003 – Delete Category with products**
> Given category đang có ít nhất 1 sản phẩm liên kết
> When Admin xóa category đó
> Then hệ thống trả về 409 kèm số lượng sản phẩm liên quan, không xóa category.

**FR-AUTHZ-002 – Unauthorized access to admin API**
> Given User đã đăng nhập (role User, không phải Admin)
> When User gọi trực tiếp `POST /api/admin/products`
> Then hệ thống trả về 403, không thực hiện thao tác.

**FR-DASH-001 – Dashboard summary cache**
> Given cache admin:dashboard:summary còn hiệu lực (chưa hết TTL 5 phút)
> When Admin mở lại trang Dashboard trong vòng 5 phút
> Then hệ thống trả dữ liệu từ cache, không truy vấn lại DB.

## Traceability Matrix (dùng để đảm bảo test coverage đủ theo từng module)

| Requirement ID | Feature | Use Case | API | Database | Test Case |
|---|---|---|---|---|---|
| FR-AUTH-001 | Đăng ký | UC-03 | POST /api/auth/register | users | TC-AUTH-01 |
| FR-AUTH-002 | Đăng nhập | UC-04 | POST /api/auth/login | users | TC-AUTH-02 |
| FR-AUTH-003 | Đăng xuất | UC-04 | POST /api/auth/logout | — (Session) | TC-AUTH-03 |
| FR-AUTHZ-001 | Kiểm tra quyền Admin | — | Mọi `/api/admin/**` | users, roles | TC-AUTHZ-01 |
| FR-PRODUCT-001..006 | Quản lý sản phẩm | UC-05 | `/api/admin/products/**` | products, product_images | TC-PRODUCT-01..06 |
| FR-CATEGORY-001..005 | Quản lý danh mục | UC-05 (tương tự) | `/api/admin/categories/**` | categories | TC-CATEGORY-01..05 |
| FR-BROWSE-001..006 | Xem/tìm/lọc/sắp xếp | UC-01 | GET /api/products, /featured, /latest | products, categories | TC-BROWSE-01..06 |
| FR-CART-001..006 | Giỏ hàng | UC-02 | `/api/cart/**` | carts, cart_items | TC-CART-01..06 |
| FR-CART-007 | Merge giỏ hàng khi login | UC-04 | POST /api/auth/login | carts, cart_items | TC-CART-07 |
| FR-USER-001..008 | Quản lý người dùng / profile | — | `/api/admin/users/**`, `/api/users/me/**` | users | TC-USER-01..08 |
| FR-DASH-001..004 | Dashboard | UC-06 | GET /api/admin/dashboard/summary | products, categories, users | TC-DASH-01..04 |
| FR-REPORT-001..004 | Báo cáo | — | `/api/admin/reports/**` | products, cart_activity_logs | TC-REPORT-01..04 |

## Permission Matrix (dùng cho Manual Test checklist)

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

## Việc cần làm

1. Unit test: `CartService.AddItem` (BR-01, BR-02), `CartService.MergeGuestCart` (BR-13), `ProductService.Create` (BR-05, BR-09, BR-10)
2. Integration test: luồng Register → Login → Add to Cart → View Cart (end-to-end qua `WebApplicationFactory`)
3. Chuyển toàn bộ Acceptance Criteria ở trên thành test case cụ thể (unit hoặc integration tùy loại)
4. Manual test checklist: đối chiếu từng dòng Permission Matrix bằng 3 tài khoản (Guest/User/Admin)
5. Đối chiếu Traceability Matrix, đảm bảo mỗi FR đều có ít nhất 1 test case tương ứng

## Done khi

- [ ] Unit test cho 3 method bắt buộc (`CartService.AddItem`, `CartService.MergeGuestCart`, `ProductService.Create`) pass
- [ ] Integration test luồng Register → Login → Add to Cart → View Cart pass
- [ ] Toàn bộ 9 Acceptance Criteria ở trên có test case tương ứng và pass
- [ ] Manual test Permission Matrix hoàn thành, không có dòng nào FAIL
- [ ] Traceability Matrix không có FR nào thiếu Test Case tương ứng
