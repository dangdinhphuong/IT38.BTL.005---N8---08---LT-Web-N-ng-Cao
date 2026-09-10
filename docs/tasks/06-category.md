# Task 06 — Category

> Trích từ `docs/test/SRS.md`, Chương 5.4 (FR-CATEGORY), 6 (BR-03, BR-15), 11 (Caching), 13.3 (DB), 15.3 (API), 26 (Acceptance Criteria).
> Nguồn tham chiếu: [Phụ lục B, mục 6](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

`CategoryController`, `CategoryService`, validate xóa khi còn sản phẩm liên kết (BR-03).

## Functional Requirements

| ID | Mô tả | Actor | Priority |
|---|---|---|---|
| FR-CATEGORY-001 | Admin thêm danh mục mới | Admin | M |
| FR-CATEGORY-002 | Admin sửa danh mục | Admin | M |
| FR-CATEGORY-003 | Admin xóa danh mục (chỉ khi không còn sản phẩm liên kết) | Admin | M |
| FR-CATEGORY-004 | Admin xem danh sách danh mục | Admin | M |
| FR-CATEGORY-005 | Admin bật/tắt trạng thái danh mục | Admin | M |
| FR-CATEGORY-006 | Guest/User lọc sản phẩm theo danh mục | Guest, User | M |
| FR-CATEGORY-007 | Danh mục inactive không hiển thị ở trang public | System | M |

## Business Rules

| ID | Quy tắc |
|---|---|
| BR-03 | Không cho xóa Category đang có sản phẩm liên kết (dù Active hay Inactive) — phải chuyển sản phẩm sang danh mục khác trước |
| BR-15 | Danh mục Inactive không hiển thị ở trang public nhưng sản phẩm thuộc danh mục đó vẫn tồn tại trong hệ thống (Admin vẫn thấy) |

## Database (chi tiết đầy đủ ở [Task 01](01-database.md))

- `categories` — cột chính: `name` (unique), `slug` (unique), `is_active`
- Quan hệ: **Category – Product**: 1-N, `ON DELETE RESTRICT` (không cho xóa Category còn Product ở tầng DB, khớp BR-03 — nhưng Service vẫn phải tự kiểm tra trước để trả lỗi 409 rõ ràng thay vì để DB ném exception)

## Caching liên quan đến Category

| Dữ liệu cache | Cache Key | TTL | Invalidate khi |
|---|---|---|---|
| Danh sách danh mục active | `categories:active` | 15 phút | Admin thêm/sửa/xóa/đổi trạng thái danh mục |

Chi tiết cơ chế cache dùng chung xem [Task 09 — Caching](09-caching.md).

## API Specification

#### GET /api/categories
- **Auth required**: Không | **Role**: — (chỉ trả `is_active=true`, đọc cache `categories:active`)

#### GET /api/admin/categories
- **Auth required**: Có | **Role**: Admin (trả tất cả, kể cả Inactive)

#### POST /api/admin/categories
- **Request**: `{ "name", "slug", "description" }`
- **Response 201** | **Validation**: name/slug unique | **Error**: 409 (trùng), 422
- **Side effect**: invalidate cache `categories:active`; ghi `audit_logs`

#### PUT /api/admin/categories/{id}
- **Response 200** | **Error**: 404, 409, 422
- **Side effect**: invalidate cache `categories:active`; ghi `audit_logs`

#### DELETE /api/admin/categories/{id}
- **Response 200**: `{ "success": true }`
- **Error cases**: 404, **409 nếu còn sản phẩm liên kết (BR-03)** — response kèm `{ "code": "CATEGORY_HAS_PRODUCTS", "productCount": 12 }`
- **Side effect**: invalidate cache `categories:active`; ghi `audit_logs`

#### PATCH /api/admin/categories/{id}/status
- **Request**: `{ "isActive": true }` | **Response 200** | **Error**: 404
- **Side effect**: invalidate cache `categories:active`

## Data Validation

| Trường | Frontend Validation | Backend Validation |
|---|---|---|
| Category name/slug | required | required + unique check |

## Acceptance Criteria

**FR-CATEGORY-003 – Delete Category with products**
> Given category đang có ít nhất 1 sản phẩm liên kết
> When Admin xóa category đó
> Then hệ thống trả về 409 kèm số lượng sản phẩm liên quan, không xóa category.

## Việc cần làm

1. `CategoryController` (public: `GET /api/categories`, admin: `/api/admin/categories/**`)
2. `CategoryService`: CRUD, validate name/slug unique, kiểm tra `productCount` trước khi xóa (BR-03)
3. Tích hợp cache `categories:active` + invalidation (phối hợp [Task 09](09-caching.md))
4. Ghi `audit_logs` cho mọi thao tác Create/Update/Delete/Status change

## Done khi

- [ ] Tạo category với name/slug trùng → 409
- [ ] Xóa category còn sản phẩm → 409 kèm `productCount`
- [ ] Xóa category không còn sản phẩm → thành công
- [ ] Category Inactive không xuất hiện ở `GET /api/categories` (public) nhưng vẫn xuất hiện ở `GET /api/admin/categories`
- [ ] Sau khi thêm/sửa/xóa/đổi trạng thái category, cache `categories:active` được invalidate đúng
