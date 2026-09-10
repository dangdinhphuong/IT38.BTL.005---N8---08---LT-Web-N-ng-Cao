# Development Tasks

Tách từ [`docs/test/SRS.md`](../SRS.md) — Phụ lục B (Development Task Breakdown), mỗi task gộp đầy đủ nội dung nghiệp vụ liên quan (FR, Business Rules, Use Case, DB schema, API spec, Caching, Security, Acceptance Criteria...) trích từ các chương tương ứng trong SRS, để dev đọc 1 file là đủ triển khai task đó mà không cần mở lại toàn bộ SRS.

Thứ tự triển khai đề xuất — theo đúng phụ thuộc (task sau dùng lại nền tảng của task trước):

| # | Task | Ghi chú |
|---|---|---|
| [01](01-database.md) | Database | Nền tảng — 9 bảng PostgreSQL |
| [02](02-backend-foundation.md) | Backend Foundation | Khởi tạo project, EF Core, exception middleware |
| [03](03-authentication.md) | Authentication | Cookie Auth + Session (DD-01) |
| [04](04-authorization.md) | Authorization | Role-based, Permission Matrix |
| [05](05-product.md) | Product | CRUD Admin + Browsing Public |
| [06](06-category.md) | Category | CRUD + validate BR-03 |
| [07](07-cart.md) | Cart | DB-backed cart cho Guest/User (DD-02), merge khi login |
| [08](08-ajax-fetch.md) | AJAX/Fetch | Tích hợp Fetch API frontend |
| [09](09-caching.md) | Caching | IMemoryCache, key/TTL/invalidation |
| [10](10-admin-dashboard.md) | Admin Dashboard | Số liệu tổng quan |
| [11](11-reports.md) | Reports | Top sản phẩm thêm giỏ, thống kê |
| [12](12-frontend.md) | Frontend | Lắp UI Public + Admin, Responsive |
| [13](13-security.md) | Security | Rà soát CSRF/XSS/SQLi/Rate limit/Logging |
| [14](14-testing.md) | Testing | Unit/Integration/Manual test, Acceptance Criteria |
| [15](15-deployment.md) | Deployment | Docker Compose, HTTPS local, README demo |
| [16](16-user-management.md) | User Management | **[Bổ sung]** — thiếu trong Phụ lục B gốc, xem ghi chú đầu file |

## Design Decisions cần nắm trước khi làm bất kỳ task nào

- **DD-01**: Authentication dùng Cookie + Session (không JWT) — xem [Task 03](03-authentication.md)
- **DD-02**: Giỏ hàng luôn lưu DB (kể cả Guest), Session chỉ định danh — xem [Task 07](07-cart.md)

Toàn văn SRS (bao gồm Chương 1-4, 8, 17, 23-24, 28-29 không tách task riêng) vẫn ở [`../SRS.md`](../SRS.md).
