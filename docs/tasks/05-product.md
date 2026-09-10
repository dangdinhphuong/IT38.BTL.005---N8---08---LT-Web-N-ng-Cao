# Task 05 — Product

> Trích từ `docs/test/SRS.md`, Chương 5.3 (FR-PRODUCT), 5.5 (FR-BROWSE), 6 (BR liên quan), 7.2, 7.6 (UC-01, UC-05), 11 (Caching), 13.4-13.5 (DB), 15.2 (API), 22 (Validation), 26 (Acceptance Criteria).
> Nguồn tham chiếu: [Phụ lục B, mục 5](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

`ProductController`, `ProductService`, quản lý `product_images`, validate SKU/price/stock. Bao gồm cả 2 phía: Admin CRUD và Public browsing (xem/tìm/lọc/sắp xếp).

## Functional Requirements — Quản lý (Admin)

| ID | Mô tả | Actor | Priority |
|---|---|---|---|
| FR-PRODUCT-001 | Admin thêm sản phẩm mới (tên, mã SP, danh mục, giá, tồn kho, mô tả, hình ảnh) | Admin | M |
| FR-PRODUCT-002 | Admin xem danh sách sản phẩm có phân trang, lọc, sắp xếp | Admin | M |
| FR-PRODUCT-003 | Admin xem chi tiết 1 sản phẩm | Admin | M |
| FR-PRODUCT-004 | Admin cập nhật thông tin sản phẩm | Admin | M |
| FR-PRODUCT-005 | Admin xóa sản phẩm (soft delete) | Admin | M |
| FR-PRODUCT-006 | Admin thay đổi trạng thái sản phẩm (Active/Inactive) | Admin | M |
| FR-PRODUCT-007 | Admin quản lý nhiều hình ảnh cho 1 sản phẩm, chọn ảnh đại diện | Admin | S |
| FR-PRODUCT-008 | Mã sản phẩm (SKU) phải là duy nhất trong hệ thống | System | M |
| FR-PRODUCT-009 | Giá sản phẩm không được là số âm | System | M |
| FR-PRODUCT-010 | Số lượng tồn kho không được là số âm | System | M |

## Functional Requirements — Hiển thị (Public)

| ID | Mô tả | Actor | Priority |
|---|---|---|---|
| FR-BROWSE-001 | Xem trang chủ (sản phẩm nổi bật, mới nhất) | Guest, User | M |
| FR-BROWSE-002 | Xem danh sách sản phẩm có phân trang | Guest, User | M |
| FR-BROWSE-003 | Xem chi tiết 1 sản phẩm | Guest, User | M |
| FR-BROWSE-004 | Tìm kiếm sản phẩm theo tên (từ khóa) qua Fetch API | Guest, User | M |
| FR-BROWSE-005 | Lọc sản phẩm theo danh mục, khoảng giá | Guest, User | M |
| FR-BROWSE-006 | Sắp xếp theo giá tăng/giảm, mới nhất | Guest, User | M |
| FR-BROWSE-007 | Sản phẩm Inactive/hết hàng — **[Recommendation]** hiển thị kèm badge "Hết hàng" thay vì ẩn hoàn toàn, disable nút thêm giỏ | Guest, User | S |

## Business Rules

| ID | Quy tắc |
|---|---|
| BR-01 | Không cho thêm sản phẩm đã Inactive hoặc hết hàng vào giỏ hàng (thực thi ở [Task 07 — Cart](07-cart.md), nhưng Product Service phải expose đúng trạng thái Active/tồn kho để Cart kiểm tra) |
| BR-05 | Mã sản phẩm (SKU) phải là duy nhất trong hệ thống |
| BR-09 | Giá sản phẩm phải >= 0 |
| BR-10 | Số lượng tồn kho phải >= 0 |
| BR-11 | Khi sản phẩm bị xóa (soft delete) hoặc chuyển Inactive, các `cart_items` tham chiếu tới sản phẩm đó vẫn giữ nguyên trong DB nhưng bị đánh dấu "không khả dụng" khi hiển thị giỏ hàng — **[Recommendation]** |

## Use Case

### UC-01: Xem / Tìm kiếm / Lọc sản phẩm (Guest)

| | |
|---|---|
| **Actor** | Guest |
| **Goal** | Xem danh sách sản phẩm phù hợp với nhu cầu |
| **Preconditions** | Không cần đăng nhập |
| **Main Flow** | 1. Guest truy cập trang danh sách sản phẩm<br>2. Hệ thống gọi `GET /api/products` trả về danh sách phân trang<br>3. Guest nhập từ khóa tìm kiếm hoặc chọn bộ lọc (danh mục, giá)<br>4. Frontend gọi Fetch API `GET /api/products?search=...&category=...` (không reload trang)<br>5. Hệ thống trả kết quả phù hợp |
| **Alternative Flow** | 3a. Guest chọn sắp xếp theo giá/mới nhất → gọi lại API với `sort=` tương ứng |
| **Exception Flow** | Không tìm thấy kết quả → hiển thị danh sách rỗng kèm thông báo "Không tìm thấy sản phẩm" |
| **Postconditions** | Danh sách sản phẩm được hiển thị đúng bộ lọc |

### UC-05: Quản lý sản phẩm (Admin — CRUD)

| | |
|---|---|
| **Actor** | Admin |
| **Goal** | Thêm/sửa/xóa/xem sản phẩm |
| **Preconditions** | Đã đăng nhập với role Admin |
| **Main Flow** | 1. Admin mở trang quản lý sản phẩm<br>2. Gọi `GET /api/admin/products` (phân trang)<br>3. Admin thêm mới → `POST /api/admin/products`; sửa → `PUT /api/admin/products/{id}`; xóa → `DELETE /api/admin/products/{id}` (soft delete)<br>4. Backend validate (SKU unique, giá >= 0, tồn kho >= 0)<br>5. Backend invalidate cache liên quan (danh sách sản phẩm, sản phẩm nổi bật) |
| **Alternative Flow** | Đổi trạng thái nhanh (Active/Inactive) qua `PATCH /api/admin/products/{id}/status` |
| **Exception Flow** | SKU trùng → HTTP 409; thiếu trường bắt buộc → HTTP 422; không tìm thấy sản phẩm → HTTP 404 |
| **Postconditions** | Dữ liệu sản phẩm được cập nhật, cache liên quan bị invalidate |

## Database (bảng liên quan — chi tiết đầy đủ ở [Task 01](01-database.md))

- `products` — cột chính: `sku` (unique), `category_id` (FK), `price` (CHECK >= 0), `stock_quantity` (CHECK >= 0), `is_active`, `is_featured`, `is_deleted` (soft delete)
- `product_images` — **[Assumption A-02]**, 1-N với `products`, có `is_primary` để chọn ảnh đại diện

## Caching liên quan đến Product

| Dữ liệu cache | Cache Key | TTL | Invalidate khi |
|---|---|---|---|
| Sản phẩm nổi bật | `products:featured` | 10 phút | Admin thêm/sửa/xóa sản phẩm có cờ `is_featured`, hoặc đổi trạng thái |
| Sản phẩm mới nhất | `products:latest` | 5 phút | Admin tạo sản phẩm mới |

Danh sách sản phẩm có **filter/search/sort** (`GET /api/products?...`) **không cache** — tổ hợp tham số quá nhiều, chi phí cache > lợi ích. Chi tiết cơ chế cache dùng chung xem [Task 09 — Caching](09-caching.md).

## API Specification

#### GET /api/products
- **Auth required**: Không | **Role**: — (public)
- **Query**: `search`, `category`, `minPrice`, `maxPrice`, `sort` (`price_asc`\|`price_desc`\|`newest`), `page`, `pageSize`
- **Response 200**: danh sách phân trang (chỉ sản phẩm `is_active=true, is_deleted=false`)
- **Error cases**: 400 (tham số phân trang không hợp lệ, vd `page < 1`)

#### GET /api/products/featured
- **Response 200**: danh sách sản phẩm `is_featured=true` (đọc từ cache `products:featured`)

#### GET /api/products/latest
- **Response 200**: 10 sản phẩm mới nhất (đọc từ cache `products:latest`)

#### GET /api/products/{id}
- **Response 200**: chi tiết sản phẩm kèm `images[]`
- **Error cases**: 404 (không tồn tại, đã xóa, hoặc Inactive với Guest/User — Admin xem riêng qua `/api/admin/products/{id}`)

#### POST /api/admin/products
- **Auth required**: Có | **Role**: Admin
- **Request**: `{ "sku", "name", "categoryId", "shortDescription", "description", "price", "stockQuantity", "isFeatured", "imageUrls": [] }`
- **Response 201**: sản phẩm vừa tạo
- **Validation**: sku unique (BR-05), price ≥ 0 (BR-09), stockQuantity ≥ 0 (BR-10), categoryId phải tồn tại và active
- **Error cases**: 409 (sku trùng), 422 (thiếu/sai trường), 404 (categoryId không tồn tại)
- **Side effect**: invalidate cache `products:featured`, `products:latest`; ghi `audit_logs`

#### PUT /api/admin/products/{id}
- **Request**: giống POST (full update)
- **Response 200**: sản phẩm sau cập nhật
- **Error cases**: 404, 409 (sku trùng với sản phẩm khác), 422
- **Side effect**: invalidate cache liên quan; ghi `audit_logs`

#### DELETE /api/admin/products/{id}
- **Response 200**: `{ "success": true }` (soft delete: `is_deleted=true`)
- **Error cases**: 404
- **Side effect**: invalidate cache liên quan; ghi `audit_logs`

#### PATCH /api/admin/products/{id}/status
- **Request**: `{ "isActive": false }`
- **Response 200**: sản phẩm sau cập nhật
- **Error cases**: 404
- **Side effect**: invalidate cache liên quan; ghi `audit_logs`

#### GET /api/admin/products
- **Auth required**: Có | **Role**: Admin
- **Query**: `search`, `category`, `status`, `page`, `pageSize` (bao gồm cả Inactive, không gồm `is_deleted=true`)

## Data Validation

| Trường | Frontend Validation | Backend Validation |
|---|---|---|
| Product name | required, max length | required, max length, trim khoảng trắng thừa |
| SKU | required | required + unique check (BR-05) |
| Price | number, min=0 | `NUMERIC` + CHECK constraint ở DB (BR-09) |
| Stock quantity | number, min=0, integer | CHECK constraint ở DB (BR-10) |

## Acceptance Criteria

**FR-PRODUCT-001 – Create Product**
> Given Admin đã đăng nhập, dữ liệu hợp lệ, SKU chưa tồn tại
> When Admin submit form thêm sản phẩm
> Then hệ thống tạo sản phẩm mới, trả 201, invalidate cache products:featured/latest.

## Việc cần làm

1. `ProductController` (public: `/api/products/**`) + `ProductController` (admin: `/api/admin/products/**`)
2. `ProductService`: CRUD, validate SKU unique/price/stock, soft delete, đổi trạng thái
3. Quản lý `product_images` (thêm/xóa ảnh, chọn ảnh đại diện)
4. Tích hợp cache `products:featured`/`products:latest` + invalidation (phối hợp [Task 09](09-caching.md))
5. Ghi `audit_logs` cho mọi thao tác Create/Update/Delete/Status change

## Done khi

- [ ] Tạo sản phẩm với SKU trùng → 409
- [ ] Tạo sản phẩm giá âm hoặc tồn kho âm → 422
- [ ] Guest/User xem `GET /api/products` không thấy sản phẩm Inactive/đã xóa
- [ ] Admin xem `GET /api/admin/products` thấy cả sản phẩm Inactive
- [ ] Sau khi tạo/sửa/xóa sản phẩm, cache `products:featured`/`products:latest` được invalidate đúng
