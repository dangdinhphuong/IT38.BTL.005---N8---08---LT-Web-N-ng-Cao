# Task 12 — Frontend

> Trích từ `docs/test/SRS.md`, Chương 16 (UI/UX Requirements), 17 (Responsive Requirements).
> Nguồn tham chiếu: [Phụ lục B, mục 12](../SRS.md#phụ-lục-b--development-task-breakdown)

## Mục tiêu

Xây dựng UI Public + Admin, responsive theo 3 breakpoint. Task này lắp ráp giao diện gọi tới các API đã hoàn thiện ở Task 03-11, tích hợp Fetch API theo [Task 08](08-ajax-fetch.md).

## 16.1 Public Site

- **Header**: Logo, thanh Search (Fetch API), icon Giỏ hàng (badge số lượng), menu Đăng nhập/Đăng ký hoặc tên User
- **Navigation**: Danh sách danh mục (lấy từ cache `categories:active`, xem [Task 06](06-category.md))
- **Product Listing**: Grid sản phẩm, filter sidebar (danh mục, khoảng giá), dropdown sort, phân trang
- **Product Detail**: Ảnh (gallery nếu nhiều ảnh), tên, giá, mô tả, tồn kho, nút "Thêm vào giỏ" (disable nếu hết hàng/Inactive)
- **Category Page**: Danh sách sản phẩm đã lọc theo danh mục
- **Cart Page**: Bảng item (ảnh, tên, đơn giá, số lượng +/-, thành tiền, nút xóa), tổng tạm tính, nút "Xóa toàn bộ giỏ"
- **Footer**: Thông tin liên hệ, liên kết cơ bản

## 16.2 Admin Panel

- **Login**: Form riêng `/admin/login`, dùng chung cơ chế Cookie Auth với kiểm tra role Admin (xem [Task 03](03-authentication.md), [Task 04](04-authorization.md))
- **Dashboard**: Các thẻ số liệu (KPI card) + biểu đồ (Chart.js hoặc tương đương) cho sản phẩm theo danh mục (xem [Task 10](10-admin-dashboard.md))
- **Product Management**: Bảng danh sách + modal thêm/sửa (dùng Fetch API, không reload trang), upload ảnh (xem [Task 05](05-product.md))
- **Category Management**: Bảng danh sách + modal thêm/sửa (xem [Task 06](06-category.md))
- **User Management**: Bảng danh sách, filter theo role/trạng thái, nút khóa/mở khóa (xem [Task 16](16-user-management.md))
- **Reports**: Biểu đồ + bảng số liệu (sản phẩm theo danh mục, top sản phẩm thêm giỏ) (xem [Task 11](11-reports.md))
- **Profile**: Form xem/sửa thông tin cá nhân Admin, đổi mật khẩu (xem [Task 16](16-user-management.md))

## Responsive Requirements

| Breakpoint | Độ rộng | Yêu cầu hiển thị |
|---|---|---|
| Desktop | ≥ 1024px | Grid sản phẩm 4 cột, sidebar filter cố định bên trái, bảng admin đầy đủ cột |
| Tablet | 768px – 1023px | Grid sản phẩm 2-3 cột, sidebar filter thu gọn (collapsible), bảng admin scroll ngang nếu cần |
| Mobile | < 768px | Grid sản phẩm 1-2 cột, filter chuyển thành modal/drawer, menu chuyển hamburger, bảng admin chuyển dạng card |

**Nguyên tắc chung**: dùng CSS Flexbox/Grid + media query hoặc framework CSS có sẵn (Bootstrap/Tailwind — tùy chọn sinh viên); ảnh sản phẩm dùng `object-fit: cover` để không vỡ layout; test tối thiểu ở 3 kích thước: 375px (mobile), 768px (tablet), 1440px (desktop).

## NFR liên quan

| ID | Yêu cầu |
|---|---|
| NFR-USE-01 | Thao tác thêm giỏ hàng, tìm kiếm không yêu cầu reload trang (dùng Fetch API) |
| NFR-RESP-01 | Giao diện hiển thị đúng trên 3 breakpoint: Desktop (≥1024px), Tablet (768-1023px), Mobile (<768px) |
| NFR-COMPAT-01 | Hoạt động đúng trên Chrome, Edge, Firefox bản mới nhất |

## Việc cần làm

1. Dựng layout Public: Header/Navigation/Footer dùng chung, trang Home/Product Listing/Product Detail/Category/Cart
2. Dựng layout Admin: sidebar menu, trang Login/Dashboard/Product/Category/User/Reports/Profile
3. Áp dụng Fetch API cho mọi tương tác động (đã có ở [Task 08](08-ajax-fetch.md), task này chỉ lắp UI gọi vào)
4. Responsive: media query cho 3 breakpoint, test thủ công ở 375px/768px/1440px
5. Xử lý trạng thái loading/empty/error trên UI (vd "Không tìm thấy sản phẩm" khi search rỗng — theo UC-01)

## Done khi

- [ ] Toàn bộ trang Public + Admin liệt kê ở trên đã dựng xong, gọi đúng API tương ứng
- [ ] Giao diện hiển thị đúng ở 3 breakpoint: 375px, 768px, 1440px
- [ ] Nút "Thêm vào giỏ" tự động disable khi sản phẩm hết hàng/Inactive
- [ ] Admin Panel chặn truy cập khi chưa đăng nhập hoặc không phải role Admin (redirect về `/admin/login`)
