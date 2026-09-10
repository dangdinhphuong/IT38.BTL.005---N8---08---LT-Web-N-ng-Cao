# Task 16 — User Management

> **[Bổ sung]** Task này KHÔNG có trong Phụ lục B gốc của SRS — Task Breakdown gốc liệt kê 15 mục nhưng bỏ sót nhóm chức năng Quản lý Người dùng/Profile (FR-USER-001..009 ở Chương 5.7, API ở Chương 15.5). Được tách thành task riêng theo yêu cầu để không bị rơi rụng khỏi phạm vi triển khai.
>
> Trích từ `docs/test/SRS.md`, Chương 5.7 (FR-USER), 6 (BR-14), 15.5 (API), 21 (Permission Matrix), 22 (Validation).

## Mục tiêu

`UserController` (`/api/users/me/**` cho self-service, `/api/admin/users/**` cho Admin), `UserService`: xem/sửa profile cá nhân, đổi mật khẩu, Admin xem/tìm kiếm/khóa-mở khóa/đổi role người dùng.

## Functional Requirements

| ID | Mô tả | Actor | Priority |
|---|---|---|---|
| FR-USER-001 | Admin xem danh sách người dùng, phân trang | Admin | M |
| FR-USER-002 | Admin xem chi tiết 1 người dùng | Admin | M |
| FR-USER-003 | Admin tìm kiếm người dùng theo email/tên | Admin | S |
| FR-USER-004 | Admin khóa/mở khóa tài khoản | Admin | M |
| FR-USER-005 | Admin thay đổi role của User | Admin | S |
| FR-USER-006 | User xem thông tin cá nhân | User | M |
| FR-USER-007 | User cập nhật thông tin cá nhân (tên, số điện thoại) | User | M |
| FR-USER-008 | User đổi mật khẩu (yêu cầu nhập mật khẩu cũ) | User | M |
| FR-USER-009 | Admin không thể tự khóa chính tài khoản mình đang dùng | System | S |

## Business Rules

| ID | Quy tắc |
|---|---|
| BR-14 | Admin không được tự khóa tài khoản đang đăng nhập của chính mình |

## Database (chi tiết đầy đủ ở [Task 01](01-database.md))

- `users` — cột chính: `email` (unique), `password_hash`, `full_name`, `phone` (nullable), `role_id` (FK → roles), `is_locked`

## API Specification

#### GET /api/users/me
- **Auth required**: Có | **Role**: User, Admin
- **Response 200**: thông tin cá nhân (không gồm `password_hash`)

#### PUT /api/users/me
- **Request**: `{ "fullName", "phone" }` | **Response 200** | **Validation**: fullName bắt buộc
- **Error cases**: 422

#### POST /api/users/me/change-password
- **Request**: `{ "oldPassword", "newPassword" }`
- **Response 200**: `{ "success": true }`
- **Validation**: oldPassword đúng; newPassword đạt policy (BR-06, xem [Task 03](03-authentication.md))
- **Error cases**: 401 (oldPassword sai), 422 (newPassword không đạt policy)

#### GET /api/admin/users
- **Auth required**: Có | **Role**: Admin
- **Query**: `search`, `role`, `status`, `page`, `pageSize`
- **Response 200**: danh sách phân trang

#### GET /api/admin/users/{id}
- **Response 200**: chi tiết user | **Error**: 404

#### PATCH /api/admin/users/{id}/lock
- **Request**: `{ "isLocked": true }`
- **Response 200** | **Error**: 404, **409 nếu Admin tự khóa chính mình (BR-14)**
- **Side effect**: ghi `audit_logs`

#### PATCH /api/admin/users/{id}/role
- **Request**: `{ "roleId": 2 }`
- **Response 200** | **Error**: 404, 422 (roleId không tồn tại)
- **Side effect**: ghi `audit_logs`

## Permission Matrix liên quan

| Function | Guest | User | Admin |
|---|---|---|---|
| Xem thông tin cá nhân | ✗ | ✓ | ✓ |
| Cập nhật thông tin cá nhân | ✗ | ✓ | ✓ |
| Đổi mật khẩu | ✗ | ✓ | ✓ |
| Quản lý người dùng (xem/khóa/đổi role) | ✗ | ✗ | ✓ |

## Data Validation

| Trường | Frontend Validation | Backend Validation |
|---|---|---|
| Password (đổi mật khẩu) | Độ dài, has hoa/số | Bắt buộc lặp lại toàn bộ rule BR-06 — không tin frontend; oldPassword phải khớp hash hiện tại |

## Việc cần làm

1. `UserController` (self-service `/api/users/me/**`): GET/PUT profile, POST change-password
2. `UserController` (admin `/api/admin/users/**`), gắn Policy Admin (xem [Task 04](04-authorization.md)): GET danh sách (phân trang + search + filter), GET chi tiết, PATCH lock, PATCH role
3. `UserService`: validate BR-14 (không cho Admin tự khóa chính mình — so sánh `id` trong request với `user_id` từ Session/Claims hiện tại), validate đổi mật khẩu (oldPassword đúng + newPassword theo BR-06)
4. Ghi `audit_logs` cho thao tác khóa/mở khóa và đổi role (đây là 2 sự kiện Warning-level theo yêu cầu Logging ở [Task 13](13-security.md))
5. Frontend: trang User Management (bảng + filter + nút khóa/mở khóa) và trang Profile (xem/sửa + đổi mật khẩu) trong Admin Panel (xem [Task 12](12-frontend.md))

## Done khi

- [ ] User xem/sửa được profile cá nhân của chính mình, không sửa được của người khác
- [ ] Đổi mật khẩu sai oldPassword → 401; newPassword không đạt policy → 422
- [ ] Admin xem danh sách user có phân trang, tìm kiếm theo email/tên hoạt động đúng
- [ ] Admin khóa tài khoản chính mình → 409, không thực hiện
- [ ] Admin khóa/mở khóa tài khoản khác → thành công, ghi `audit_logs`
- [ ] Admin đổi role user → thành công, ghi `audit_logs`
- [ ] Response không bao giờ chứa `password_hash`
