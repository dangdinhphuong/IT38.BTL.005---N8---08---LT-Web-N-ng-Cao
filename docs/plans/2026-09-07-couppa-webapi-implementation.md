---
title: Xây dựng hệ thống Couppa — ASP.NET Core Web API
status: Đã lập kế hoạch
created_date: 2026-09-07
started_date:
completed_date:
cancel_reason:
owner: ai
related_spec: docs/test/SRS.md
---

# Plan triển khai — Website Giới thiệu Sản phẩm và Giỏ hàng

Kế hoạch thực thi 14 phase, dựng trên [SRS](../SRS.md) và [16 task](../tasks/README.md) đã đặc tả sẵn.

## Bối cảnh

Toàn bộ đầu vào đã có:
- **SRS đầy đủ** ([`docs/test/SRS.md`](../SRS.md)) — 29 chương + 3 phụ lục
- **16 task chi tiết** ([`docs/test/tasks/`](../tasks/README.md)) — mỗi task đã gộp đủ FR/BR/DB/API/AC
- **12 file giao diện** ([`docs/test/html/`](../html/)) — HTML/CSS/JS + Design System hoàn chỉnh

Plan này **không lặp lại** nội dung đặc tả đó, chỉ định nghĩa **thứ tự thực thi**, **cấu trúc code**, và **các điểm kỹ thuật dễ sai** khi hiện thực hóa.

## Kiến trúc & Cấu trúc thư mục

Theo SRS Chương 23 — 3-layer, không Repository pattern (EF Core `DbContext` đã đủ trừu tượng):

```text
src/
├── Couppa.Api/                   Backend — ASP.NET Core Web API (.NET 8)
│   ├── Controllers/              AuthController, ProductController, CategoryController,
│   │                             CartController, UserController,
│   │                             AdminProductController, AdminCategoryController,
│   │                             AdminUserController, DashboardController, ReportController
│   ├── Services/                 AuthService, ProductService, CategoryService,
│   │                             CartService, UserService, DashboardService, CacheService
│   ├── Data/
│   │   ├── AppDbContext.cs
│   │   ├── Entities/             Role, User, Category, Product, ProductImage,
│   │   │                         Cart, CartItem, CartActivityLog, AuditLog
│   │   ├── Migrations/
│   │   └── Seed/
│   ├── Models/                   Request DTO + Response DTO (tách khỏi Entity)
│   ├── Middleware/               ExceptionHandlingMiddleware
│   ├── appsettings.json
│   └── Program.cs
│
├── Couppa.Web/                   Frontend — tĩnh, copy từ docs/test/html
│   ├── css/styles.css
│   ├── js/                       main.js + apiClient.js (mới)
│   ├── admin/                    5 trang admin
│   └── *.html                    7 trang public
│
├── Couppa.Api.Tests/             xUnit + Moq
└── docker-compose.yml            PostgreSQL cho môi trường dev/demo
```

**Vì sao 2 project tách rời**: SRS Chương 2.1 mô tả Frontend là HTML/CSS/JS thuần giao tiếp qua Fetch API. Giữ tách rời đúng tinh thần đó và giữ nguyên được 12 file HTML đã thiết kế.

## Bảng thứ tự thực thi

| Phase | Task nguồn | Nội dung | Phụ thuộc |
|---|---|---|---|
| 1 | [02](../tasks/02-backend-foundation.md) | Setup project, EF Core, exception middleware, CORS | — |
| 2 | [01](../tasks/01-database.md) | 9 bảng + constraint + index + seed | 1 |
| 3 | [03](../tasks/03-authentication.md) | Cookie Auth + Session, register/login/logout | 2 |
| 4 | [04](../tasks/04-authorization.md) | Policy Admin cho `/api/admin/**` | 3 |
| 5 | [06](../tasks/06-category.md) | Category CRUD | 4 |
| 6 | [05](../tasks/05-product.md) | Product CRUD + Public browsing | 5 |
| 7 | [07](../tasks/07-cart.md) | Cart + merge khi login | 6 |
| 8 | [09](../tasks/09-caching.md) | IMemoryCache + invalidation | 6 |
| 9 | [16](../tasks/16-user-management.md) | User Management + Profile | 4 |
| 10 | [10](../tasks/10-admin-dashboard.md), [11](../tasks/11-reports.md) | Dashboard + Reports | 7, 8 |
| 11 | [08](../tasks/08-ajax-fetch.md), [12](../tasks/12-frontend.md) | Nối 12 file HTML với API thật | 10 |
| 12 | [13](../tasks/13-security.md) | Security review toàn hệ thống | 11 |
| 13 | [14](../tasks/14-testing.md) | Unit + Integration + Manual test | 12 |
| 14 | [15](../tasks/15-deployment.md) | Docker Compose, HTTPS, README demo | 13 |

### Hai thay đổi so với Phụ lục B của SRS

**1. Category (task 06) làm TRƯỚC Product (task 05)**
`products.category_id` là FK NOT NULL. Không có Category thì không tạo được Product nào để test — Phụ lục B gốc xếp ngược thứ tự này.

**2. Gộp task 08 (AJAX/Fetch) + task 12 (Frontend) thành 1 phase**
12 file HTML đã dựng xong giao diện đầy đủ. Việc còn lại chỉ là thay dữ liệu mẫu bằng Fetch API thật. Tách 2 phase sẽ phải sửa cùng một file hai lần.

---

## Phase 1 — Setup & Backend Foundation

> Task nguồn: [02-backend-foundation.md](../tasks/02-backend-foundation.md)

### Việc cần làm

1. Tạo solution + project `Couppa.Api` (.NET 8 Web API), `Couppa.Api.Tests` (xUnit)
2. Cài package: `Npgsql.EntityFrameworkCore.PostgreSQL`, `BCrypt.Net-Next`
3. `docker-compose.yml` cho PostgreSQL (dùng luôn từ phase này, không đợi phase 14)
4. `ExceptionHandlingMiddleware` — bắt exception toàn cục, trả format lỗi chuẩn hóa
5. Cấu hình **CORS cho phép credentials** (chi tiết bên dưới)
6. Copy `docs/test/html/` → `src/Couppa.Web/`

### Điểm kỹ thuật dễ sai: CORS + Cookie

Backend (port 5001) và Frontend (port 5500) khác origin. Vì hệ thống dùng **Cookie Authentication** chứ không phải JWT, cookie sẽ **không được gửi** nếu thiếu cấu hình đúng ở CẢ HAI phía:

- Backend: `AllowCredentials()` + `WithOrigins("<origin frontend cụ thể>")` — **không được dùng `AllowAnyOrigin()`**, trình duyệt từ chối kết hợp wildcard origin với credentials
- Frontend: mọi lời gọi Fetch phải có `credentials: 'include'`
- Cookie: `SameSite=Lax` chỉ hoạt động khi cùng site. Nếu 2 port khác nhau bị coi là cross-site, cần `SameSite=None` + `Secure=true` (bắt buộc HTTPS)

Đây là lỗi phổ biến nhất khi tách frontend/backend với Cookie Auth — biểu hiện là "đăng nhập thành công nhưng request sau vẫn 401".

### Done khi

- [ ] `docker-compose up` khởi động PostgreSQL, backend kết nối được
- [ ] Gọi endpoint test ném exception → trả đúng format `{success:false, error:{code, message}}`, không lộ stack trace
- [ ] Gọi thử 1 endpoint từ frontend (port khác) → không bị CORS block, cookie được gửi kèm

---

## Phase 2 — Database

> Task nguồn: [01-database.md](../tasks/01-database.md) (chứa đầy đủ schema 9 bảng)

### Việc cần làm

1. Tạo 9 Entity class + cấu hình `AppDbContext` (Fluent API cho constraint/index)
2. Migration + apply
3. Seed: 2 `roles`, ≥ 5 `categories`, ≥ 20 `products` demo, 1 tài khoản Admin

### Điểm kỹ thuật dễ sai

**CHECK constraint của bảng `carts`** (BR-12 — cart thuộc đúng 1 trong 2: user hoặc session):
```
CHECK ((user_id IS NOT NULL AND session_id IS NULL)
    OR (user_id IS NULL AND session_id IS NOT NULL))
```
EF Core không tự sinh CHECK constraint này — phải khai báo thủ công bằng `ToTable(t => t.HasCheckConstraint(...))` hoặc viết raw SQL trong migration.

**`ON DELETE RESTRICT` cho Category → Product** (BR-03): EF Core mặc định dùng `Cascade` cho FK bắt buộc. Phải chỉ định rõ `DeleteBehavior.Restrict`, nếu không xóa Category sẽ xóa luôn toàn bộ Product.

### Done khi

- [ ] Đủ 9 bảng, đúng constraint/index theo task 01
- [ ] Insert cart với cả `user_id` và `session_id` khác NULL → bị DB chặn
- [ ] Xóa category còn sản phẩm → bị DB chặn (RESTRICT)
- [ ] Seed chạy được, có tài khoản Admin đăng nhập được ở phase 3

---

## Phase 3 — Authentication

> Task nguồn: [03-authentication.md](../tasks/03-authentication.md)

### Việc cần làm

1. Cấu hình Cookie Authentication + Session (`AddSession`, `AddDistributedMemoryCache`)
2. `AuthService`: hash password (BCrypt), validate BR-04/BR-06/BR-07
3. `AuthController`: `POST /api/auth/register`, `/login`, `/logout`
4. Rate limiting cho `/api/auth/login` (SEC-11)
5. Cấp `session_id` cho Guest (nền tảng cho cart ở phase 7)

### Điểm kỹ thuật dễ sai

**Session Fixation (SEC-04)**: phải regenerate Session ID sau khi đăng nhập thành công. Nếu giữ nguyên session cũ, kẻ tấn công biết trước session ID có thể chiếm quyền.

**Thông báo lỗi login không được tiết lộ email tồn tại hay không** (task 03): sai email và sai password đều trả cùng một message "Email hoặc mật khẩu không đúng" — nếu tách riêng thì lộ danh sách email đã đăng ký.

**Điểm hoãn lại**: bước merge cart khi login (BR-13) chưa làm được ở phase này vì `CartService` chưa tồn tại. Để lại TODO, hoàn thiện ở phase 7.

### Done khi

- [ ] Đăng ký email trùng → 409; password không đạt policy → 422
- [ ] Đăng nhập đúng → 200 + cookie, response **không chứa** `password_hash`
- [ ] Đăng nhập sai → 401 (message chung); tài khoản khóa → 403
- [ ] Đăng xuất → session hủy, request sau trả 401
- [ ] Login sai 6 lần/phút → lần 6 trả 429

---

## Phase 4 — Authorization

> Task nguồn: [04-authorization.md](../tasks/04-authorization.md)

### Việc cần làm

1. Policy `RequireAdmin` trong `Program.cs`
2. Áp `[Authorize(Policy = "RequireAdmin")]` cho mọi controller `/api/admin/**`
3. Áp `[Authorize]` cho endpoint cần đăng nhập nhưng không cần Admin

### Điểm kỹ thuật dễ sai

**Không tin `role`/`user_id` từ client** (SEC-10): luôn đọc từ `User.Claims` của request đã xác thực, không bao giờ đọc từ body/query/header do client gửi.

**Tài khoản bị khóa giữa chừng**: user đăng nhập rồi bị Admin khóa — cookie vẫn còn hiệu lực. Cần kiểm tra `is_locked` ở mỗi request (hoặc chấp nhận giới hạn này và ghi rõ là **[Assumption]**, vì SRS không đặc tả).

### Done khi

- [ ] Gọi API admin chưa đăng nhập → 401; role User → 403
- [ ] Đối chiếu đủ 15 dòng Permission Matrix (task 04), không endpoint nào thiếu kiểm tra quyền

---

## Phase 5 — Category

> Task nguồn: [06-category.md](../tasks/06-category.md)

### Việc cần làm

1. `CategoryService`: CRUD, validate name/slug unique, đếm `productCount` trước khi xóa (BR-03)
2. `CategoryController` (public `GET /api/categories`) + `AdminCategoryController`
3. Chưa tích hợp cache — để phase 8

### Điểm kỹ thuật dễ sai

**Xóa category còn sản phẩm** phải trả 409 kèm `productCount` (task 06), tức là **Service phải chủ động đếm trước**, không để DB ném `DbUpdateException` rồi bắt — cách sau không lấy được số lượng để trả về cho người dùng.

### Done khi

- [ ] Tạo category trùng name/slug → 409
- [ ] Xóa category còn sản phẩm → 409 kèm `productCount` chính xác
- [ ] Category Inactive: ẩn ở `/api/categories`, hiện ở `/api/admin/categories`

---

## Phase 6 — Product

> Task nguồn: [05-product.md](../tasks/05-product.md)

### Việc cần làm

1. `ProductService`: CRUD, validate SKU unique/price/stock, soft delete, đổi trạng thái
2. `AdminProductController`: 5 endpoint admin + quản lý `product_images`
3. `ProductController` (public): list có search/filter/sort/paging, detail, featured, latest
4. Ghi `audit_logs` cho mọi thao tác CUD
5. Chưa tích hợp cache — để phase 8

### Điểm kỹ thuật dễ sai

**Soft delete phải lọc ở mọi truy vấn**: quên `WHERE is_deleted = false` một chỗ là sản phẩm đã xóa lộ ra ngoài. Cân nhắc dùng **EF Core Global Query Filter** (`HasQueryFilter`) để tự động áp dụng, tránh sót.

**Public và Admin thấy dữ liệu khác nhau**: public chỉ thấy `is_active=true AND is_deleted=false`; admin thấy cả Inactive (nhưng vẫn không thấy deleted). Nếu dùng Global Query Filter cho `is_deleted`, admin vẫn dùng chung được — chỉ khác điều kiện `is_active`.

### Done khi

- [ ] SKU trùng → 409; giá âm / tồn kho âm → 422
- [ ] Public không thấy sản phẩm Inactive/đã xóa; Admin thấy Inactive nhưng không thấy đã xóa
- [ ] Search/filter/sort/paging trả kết quả đúng
- [ ] Mọi thao tác CUD đều có dòng trong `audit_logs`

---

## Phase 7 — Cart

> Task nguồn: [07-cart.md](../tasks/07-cart.md)

### Việc cần làm

1. `CartService`:
   - Lấy-hoặc-tạo cart theo `session_id` (Guest) / `user_id` (User)
   - Validate BR-01 (sản phẩm Active + còn hàng), BR-02 (không vượt tồn kho)
   - `MergeGuestCart(sessionId, userId)` — BR-13
   - Ghi `cart_activity_logs` mỗi lần add/remove
2. `CartController`: 5 endpoint
3. **Nối lại phase 3**: gọi `MergeGuestCart` sau khi login thành công
4. Hosted Service dọn cart Guest quá 7 ngày (FR-CART-008)

### Điểm kỹ thuật dễ sai

**Merge cart là phần phức tạp nhất của hệ thống** — 4 trường hợp phải xử lý:

| Guest cart | User cart | Xử lý |
|---|---|---|
| Không có | Không có | Tạo cart rỗng cho user |
| Có | Không có | Chuyển `session_id` → `user_id` trên chính cart đó (nhanh nhất) |
| Không có | Có | Giữ nguyên cart user |
| Có | Có | Merge từng item theo `product_id`, cộng dồn, **cắt theo tồn kho hiện tại**, xóa cart guest |

Trường hợp thứ 4 dễ sai nhất: cộng dồn xong có thể vượt tồn kho (guest 3 + user 4 = 7 nhưng chỉ còn 5) → phải cắt xuống 5, không được để vi phạm BR-02.

**Unique constraint `(cart_id, product_id)`**: khi merge phải UPSERT, không INSERT mù — nếu không sẽ vi phạm constraint.

**Hiển thị item của sản phẩm đã bị xóa/inactive** (BR-11): không tự xóa item, chỉ đánh dấu "không khả dụng" và loại khỏi tổng tiền.

### Done khi

- [ ] Guest chưa có cookie → thêm giỏ tự tạo session + cart
- [ ] Thêm sản phẩm Inactive/hết hàng → 409; vượt tồn kho → 409
- [ ] Test đủ 4 trường hợp merge ở bảng trên
- [ ] Merge có sản phẩm trùng, tổng vượt tồn kho → bị cắt đúng theo tồn kho
- [ ] Mỗi add/remove có dòng trong `cart_activity_logs`

---

## Phase 8 — Caching

> Task nguồn: [09-caching.md](../tasks/09-caching.md)

### Việc cần làm

1. `CacheService` bọc `IMemoryCache`: `GetOrCreateAsync(key, factory, ttl)` + `Remove(key)`
2. Tích hợp vào `CategoryService` (`categories:active`), `ProductService` (`products:featured`, `products:latest`)
3. Gắn invalidation vào đúng các thao tác CUD đã làm ở phase 5, 6

### Điểm kỹ thuật dễ sai

**Invalidation phải nằm trong cùng luồng với CUD**: gọi `Remove(key)` **sau khi** `SaveChanges()` thành công, không phải trước — nếu save lỗi mà cache đã xóa thì lần đọc sau sẽ nạp lại dữ liệu cũ, vô nghĩa nhưng không sai; ngược lại nếu xóa cache trước và save lỗi thì không sao. Tuy nhiên nếu save thành công mà quên xóa cache thì người dùng thấy dữ liệu cũ đến hết TTL — đây mới là lỗi thực sự.

**Không cache dữ liệu cá nhân hóa**: giỏ hàng và `/api/users/me` tuyệt đối không đi qua `CacheService` — cache in-process dùng chung cho mọi người dùng, cache nhầm là lộ dữ liệu người khác.

### Done khi

- [ ] 4 cache key đúng TTL theo bảng task 09
- [ ] Sau CUD category → `categories:active` bị xóa, request sau trả dữ liệu mới ngay
- [ ] Sau CUD product → cache product tương ứng bị xóa
- [ ] Giỏ hàng và profile không bao giờ đi qua cache

---

## Phase 9 — User Management

> Task nguồn: [16-user-management.md](../tasks/16-user-management.md)

### Việc cần làm

1. `UserService`: profile, đổi mật khẩu, admin list/lock/change-role, validate BR-14
2. `UserController` (`/api/users/me/**`) + `AdminUserController` (`/api/admin/users/**`)
3. Ghi `audit_logs` cho lock/unlock và đổi role (Warning level)

### Điểm kỹ thuật dễ sai

**BR-14 (Admin không tự khóa mình)**: so sánh `id` trong URL với `user_id` lấy từ Claims — **không phải** từ body request (SEC-10).

**Đổi mật khẩu phải hủy session cũ hay không?** SRS không đặc tả. Ghi rõ là **[Assumption]** nếu chọn giữ nguyên session.

### Done khi

- [ ] User chỉ sửa được profile của chính mình
- [ ] Đổi mật khẩu sai oldPassword → 401; newPassword yếu → 422
- [ ] Admin tự khóa mình → 409
- [ ] Lock/unlock và đổi role có `audit_logs`
- [ ] Response không bao giờ chứa `password_hash`

---

## Phase 10 — Dashboard & Reports

> Task nguồn: [10-admin-dashboard.md](../tasks/10-admin-dashboard.md), [11-reports.md](../tasks/11-reports.md)

### Việc cần làm

1. `DashboardService`: 7 số liệu tổng hợp + cache `admin:dashboard:summary` (TTL 5 phút)
2. `DashboardController`: `GET /api/admin/dashboard/summary`
3. `ReportController`: `GET /api/admin/reports/cart-top-products` + các thống kê FR-REPORT-001..003

### Điểm kỹ thuật dễ sai

**Hệ thống chưa có dữ liệu** (DB rỗng): phải trả 0 / mảng rỗng, **không được** ném 500 hay trả null — task 10 và 11 đều nêu rõ điều này.

**Query top sản phẩm**: `GROUP BY product_id` trên `cart_activity_logs` + join `products` để lấy tên. Sản phẩm đã soft delete vẫn có trong log — quyết định hiển thị hay lọc bỏ, ghi rõ là **[Assumption]** vì SRS không nói.

### Done khi

- [ ] Dashboard trả đủ 7 trường; DB rỗng → trả 0, không lỗi
- [ ] Gọi lại trong 5 phút → lấy từ cache
- [ ] Top sản phẩm sắp xếp giảm dần đúng, filter `from`/`to` hoạt động

---

## Phase 11 — Frontend Integration

> Task nguồn: [08-ajax-fetch.md](../tasks/08-ajax-fetch.md), [12-frontend.md](../tasks/12-frontend.md)

12 file HTML đã dựng xong giao diện + Design System. Phase này thay dữ liệu mẫu trong `main.js` bằng lời gọi API thật.

### Việc cần làm

1. Viết `js/apiClient.js`: tự set `credentials:'include'`, tự đính CSRF token, parse `{success, data}` / `{success, error}`
2. Sửa `js/main.js`: bỏ mock LocalStorage, gọi API thật
3. Nối từng trang theo bảng API Integration Mapping đã có sẵn ở [DESIGN_SYSTEM.md](../html/DESIGN_SYSTEM.md) mục 9
4. Xử lý 5 trạng thái UX đã đặc tả (Default/Loading/Empty/Error/Success) — CSS class `.skeleton`, `.empty-state`, `.toast-*` đã có sẵn
5. Kiểm tra responsive 375px / 768px / 1440px

### Điểm kỹ thuật dễ sai

**Quên `credentials:'include'`** ở một lời gọi Fetch nào đó → request đó mất cookie → 401 khó truy vết. Bắt buộc mọi lời gọi phải qua `apiClient.js`, không gọi `fetch()` trực tiếp rải rác.

**Badge số lượng giỏ hàng** phải cập nhật sau mọi thao tác cart, kể cả khi thao tác ở trang khác (product-detail thêm giỏ → badge trên header đổi ngay).

### Done khi

- [ ] Search/filter/sort/paging không reload trang
- [ ] Thao tác giỏ hàng không reload trang, badge cập nhật đúng
- [ ] CRUD admin qua modal, bảng tự refresh
- [ ] Lỗi 401/403/409/422 hiển thị đúng thông báo trên UI
- [ ] Hiển thị đúng ở 3 breakpoint

---

## Phase 12 — Security Review

> Task nguồn: [13-security.md](../tasks/13-security.md) (có checklist đầy đủ SEC-01..SEC-13)

Phase rà soát chéo, không viết mới. Đi qua từng mục trong checklist 13 điểm của task 13, xác nhận bằng test cụ thể.

### Trọng tâm cần kiểm tra thực tế

- **CSRF**: gọi POST/PUT/DELETE thiếu token → phải bị từ chối
- **XSS**: nhập `<script>alert(1)</script>` vào tên sản phẩm → hiển thị dạng text, không thực thi
- **Sensitive data**: grep toàn bộ Response DTO xác nhận không có `password_hash`
- **Rate limit**: login sai 6 lần/phút → 429
- **Logging**: đối chiếu đủ 8 sự kiện bắt buộc, xác nhận log không chứa password/token

### Done khi

- [ ] Đủ 13 mục SEC-01..SEC-13 có bằng chứng đã áp dụng
- [ ] 5 test trọng tâm ở trên đều pass

---

## Phase 13 — Testing

> Task nguồn: [14-testing.md](../tasks/14-testing.md) (có đủ 9 Acceptance Criteria + Traceability Matrix)

### Việc cần làm

1. Unit test bắt buộc: `CartService.AddItem`, `CartService.MergeGuestCart`, `ProductService.Create`
2. Integration test: Register → Login → Add to Cart → View Cart
3. Chuyển 9 Acceptance Criteria (task 14) thành test case
4. Manual test toàn bộ Permission Matrix bằng 3 tài khoản Guest/User/Admin

### Ưu tiên nếu thiếu thời gian

`MergeGuestCart` là test quan trọng nhất — logic phức tạp nhất, 4 nhánh, dễ sai nhất, và là điểm khác biệt kỹ thuật nổi bật khi bảo vệ đồ án.

### Done khi

- [ ] 3 unit test bắt buộc pass
- [ ] Integration test luồng chính pass
- [ ] 9 Acceptance Criteria đều có test và pass
- [ ] Manual Permission Matrix không dòng nào FAIL

---

## Phase 14 — Deployment

> Task nguồn: [15-deployment.md](../tasks/15-deployment.md)

### Việc cần làm

1. Hoàn thiện `docker-compose.yml` (đã tạo từ phase 1)
2. HTTPS local: `dotnet dev-certs https --trust`
3. README hướng dẫn chạy demo từ đầu
4. Chạy thử liên tục ≥ 30 phút (NFR-AVAIL-01)

### Done khi

- [ ] Người khác đọc README tự chạy được demo
- [ ] Chạy 30 phút thao tác xen kẽ Public + Admin, không lỗi, không cần restart

---

## Ưu tiên nếu thiếu thời gian

Theo [Phụ lục C — MVP](../SRS.md#phụ-lục-c--mvp) của SRS:

**Không được bỏ** (Must-have): Phase 1-7, 9, 11 (phần public + cart), 12 (bảo mật cơ bản)
**Có thể rút gọn** (Should-have): Phase 8 (giữ 2 cache key thay vì 4), Phase 10 (bỏ biểu đồ, giữ số liệu), Phase 13 (giữ 3 unit test bắt buộc)
**Có thể bỏ** (Could-have): FR-REPORT-004 (top sản phẩm), export CSV, integration test đầy đủ

---

## Rủi ro đã nhận diện

| Rủi ro | Phase | Cách xử lý |
|---|---|---|
| CORS + Cookie không gửi được cross-origin | 1 | Cấu hình `AllowCredentials` + origin cụ thể ngay từ phase 1, test sớm |
| EF Core không sinh CHECK constraint của `carts` | 2 | Khai báo thủ công trong migration |
| EF Core cascade delete xóa nhầm Product khi xóa Category | 2 | Chỉ định `DeleteBehavior.Restrict` |
| Quên lọc `is_deleted` ở một truy vấn | 6 | Dùng Global Query Filter |
| Merge cart sai ở trường hợp cả 2 cart đều có sản phẩm trùng | 7 | Unit test đủ 4 trường hợp |
| Cache lộ dữ liệu cá nhân hóa | 8 | Không đưa cart/profile vào `CacheService` |
| Quên `credentials:'include'` ở một lời gọi Fetch | 11 | Bắt buộc mọi lời gọi qua `apiClient.js` |
