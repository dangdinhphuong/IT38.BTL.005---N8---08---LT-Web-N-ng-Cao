# Task 13 — Security

> Trích từ `docs/test/SRS.md`, Chương 10 (Security Requirements), 19 (Error Handling), 20 (Logging & Monitoring), 22 (Data Validation).
> Nguồn tham chiếu: [Phụ lục B, mục 13](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

Rà soát toàn hệ thống: CSRF token, input validation 2 lớp, rate limiting login. Đây là task tổng rà soát chéo sau khi các module chức năng (Task 03-11) đã implement — không phải viết mới từ đầu mà kiểm tra & vá các điểm còn thiếu.

## Security Requirements — Checklist đầy đủ

| ID | Kỹ thuật | Mô tả áp dụng | Module liên quan |
|---|---|---|---|
| SEC-01 | Password Hashing | BCrypt (hoặc ASP.NET Core `PasswordHasher<T>`), không tự implement thuật toán hash | [Task 03](03-authentication.md) |
| SEC-02 | Authentication | Cookie Authentication, không truyền credentials qua query string | [Task 03](03-authentication.md) |
| SEC-03 | Authorization | Kiểm tra role ở Controller/Middleware backend cho mọi endpoint nhạy cảm | [Task 04](04-authorization.md) |
| SEC-04 | Session Security | Cookie `HttpOnly=true`, `Secure=true` (production, HTTPS), `SameSite=Lax`; Session ID được regenerate sau khi đăng nhập (chống Session Fixation) | [Task 03](03-authentication.md) |
| SEC-05 | Cookie Security | Không lưu thông tin nhạy cảm (password, token) trực tiếp trong cookie; chỉ lưu Session ID | [Task 03](03-authentication.md) |
| SEC-06 | Input Validation | Validate cả frontend (UX) lẫn backend (Data Annotations / FluentValidation) — backend là bắt buộc | Toàn bộ module có input |
| SEC-07 | CSRF Protection | Dùng Anti-forgery token (`[ValidateAntiForgeryToken]` hoặc `X-CSRF-TOKEN` header) cho các request thay đổi trạng thái (POST/PUT/DELETE) khi dùng Cookie Auth | [Task 08](08-ajax-fetch.md) |
| SEC-08 | XSS Protection | Output encoding mặc định của Razor/React; sanitize mọi input hiển thị lại (tên sản phẩm, mô tả); áp dụng CSP header cơ bản | [Task 05](05-product.md), [Task 06](06-category.md) |
| SEC-09 | SQL Injection Prevention | Dùng EF Core với parameterized query, không nối chuỗi SQL thủ công | [Task 02](02-backend-foundation.md) và toàn bộ Service |
| SEC-10 | Backend Authorization Check | Không tin bất kỳ `role`/`user_id` nào gửi từ client; luôn lấy từ Session/Claims đã xác thực | [Task 04](04-authorization.md) |
| SEC-11 | Rate Limiting | Giới hạn số lần gọi `/api/auth/login` (ví dụ 5 lần/phút/IP) chống brute-force — **[Recommendation]** dùng `Microsoft.AspNetCore.RateLimiting` | [Task 03](03-authentication.md) |
| SEC-12 | Sensitive Data Exposure | Response API không bao giờ trả `password_hash`; lỗi 500 không trả stack trace cho client | Toàn bộ Controller |
| SEC-13 | Safe Error Handling | Middleware bắt exception toàn cục, trả về format lỗi chuẩn hóa, log chi tiết ở server | [Task 02](02-backend-foundation.md) |

## Phân biệt Frontend vs Backend Validation

| | Frontend Validation | Backend Validation |
|---|---|---|
| Mục đích | UX tức thời, giảm round-trip không cần thiết | Đảm bảo tính toàn vẹn & bảo mật dữ liệu |
| Có thể bị bỏ qua? | Có (dev tools, gọi API trực tiếp) | Không — luôn thực thi |
| Vai trò | Gợi ý, cảnh báo sớm | **Quyết định cuối cùng** về hợp lệ & quyền truy cập |

## Data Validation — Bảng đối chiếu toàn hệ thống

| Trường | Frontend Validation | Backend Validation |
|---|---|---|
| Email | Regex định dạng cơ bản | Regex + unique check trong DB (BR-04) |
| Password | Độ dài, has hoa/số (hiển thị gợi ý) | Bắt buộc lặp lại toàn bộ rule (BR-06) — không tin frontend |
| Product name | required, max length | required, max length, trim khoảng trắng thừa |
| SKU | required | required + unique check (BR-05) |
| Price | number, min=0 | `NUMERIC` + CHECK constraint ở DB (BR-09) |
| Stock quantity | number, min=0, integer | CHECK constraint ở DB (BR-10) |
| Category name/slug | required | required + unique check |
| Cart quantity | number, min=1 | so sánh với `stock_quantity` hiện tại tại thời điểm ghi (BR-02) |

## Logging & Monitoring (rà soát cùng đợt vì cùng nhóm cross-cutting)

### Sự kiện bắt buộc ghi log

| Sự kiện | Log Level | Ghi vào |
|---|---|---|
| Đăng nhập thành công | Information | `audit_logs` + application log |
| Đăng nhập thất bại | Warning | application log (kèm email, IP — không kèm password) |
| Đăng xuất | Information | application log |
| Tạo/Sửa/Xóa sản phẩm | Information | `audit_logs` |
| Tạo/Sửa/Xóa danh mục | Information | `audit_logs` |
| Thay đổi role user | Warning | `audit_logs` |
| Khóa/mở khóa tài khoản | Warning | `audit_logs` |
| Lỗi hệ thống (5xx) | Error | application log, kèm stack trace (server-side only) |

### Nguyên tắc

- **Không** log password (plaintext hoặc hash), token, cookie value
- `audit_logs.detail` (JSONB) chỉ chứa các trường nghiệp vụ đã thay đổi, không chứa toàn bộ payload request
- **[Recommendation]** dùng `ILogger<T>` built-in của ASP.NET Core, ghi ra Console + file (Serilog nếu cần structured logging) — không bắt buộc hệ thống log tập trung (ELK/Grafana) vì ngoài phạm vi đồ án

## Việc cần làm

1. Rà soát toàn bộ endpoint POST/PUT/PATCH/DELETE đã yêu cầu `X-CSRF-TOKEN` chưa (SEC-07)
2. Rà soát toàn bộ Controller/DTO có Data Annotations validate đầy đủ theo bảng Data Validation ở trên (SEC-06)
3. Xác nhận không endpoint nào trả `password_hash` trong response (SEC-12) — grep toàn bộ DTO response
4. Xác nhận rate limiting đã áp dụng cho `/api/auth/login` (SEC-11)
5. Xác nhận response lỗi 500 không lộ message/stack trace gốc (SEC-13)
6. Rà soát log: đối chiếu 8 sự kiện bắt buộc log ở bảng trên, xác nhận không log password/token
7. Kiểm tra XSS: thử nhập `<script>alert(1)</script>` vào tên sản phẩm/mô tả, xác nhận không thực thi khi hiển thị (SEC-08)

## Done khi

- [ ] Toàn bộ 13 mục SEC-01..SEC-13 đã được xác nhận áp dụng đúng, có test hoặc bằng chứng cụ thể
- [ ] Không có endpoint nào trả `password_hash` hoặc thông tin nhạy cảm khác
- [ ] Test CSRF: gọi POST/PUT/DELETE thiếu `X-CSRF-TOKEN` → bị từ chối
- [ ] Test rate limit: gọi login sai 6 lần liên tiếp trong 1 phút → lần thứ 6 trả 429
- [ ] Test XSS: nhập script tag vào tên sản phẩm → hiển thị dạng text thô, không thực thi
- [ ] Đối chiếu đủ 8 sự kiện log bắt buộc, xác nhận log không chứa password/token
