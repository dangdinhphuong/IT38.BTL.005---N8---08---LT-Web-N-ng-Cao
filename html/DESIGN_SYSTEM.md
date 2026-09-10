# TÀI LIỆU HỆ THỐNG THIẾT KẾ UI/UX VÀ KIẾN TRÚC FRONTEND (DESIGN SYSTEM & FRONTEND ARCHITECTURE)

**Dự án:** Website Giới Thiệu Sản Phẩm và Giỏ Hàng Đơn Giản (Đề tài 8)  
**Tài liệu tham chiếu:** `docs/test/SRS.md`  
**Thư mục mã nguồn UI:** `docs/test/html`  
**Phiên bản:** 1.0  
**Ngày:** 2026-09-07  

---

## 1. DESIGN SYSTEM

### 1.1 Typography System

Hệ thống sử dụng font **Inter** (Google Fonts) – chuẩn mực cho ứng dụng web thương mại điện tử hiện đại, mang lại tính đọc lọt cao và phân cấp thị giác (visual hierarchy) rõ ràng.

| Cấp bậc | Size (rem / px) | Weight | Line Height | Letter Spacing | Ứng dụng |
|---|---|---|---|---|---|
| **Heading 1 (h1)** | 2.25rem / 36px | 700 (Bold) | 1.25 | -0.02em | Hero banner, Tiêu đề trang chính |
| **Heading 2 (h2)** | 1.75rem / 28px | 700 (Bold) | 1.3 | -0.01em | Tiêu đề Section (Sản phẩm nổi bật, Danh mục) |
| **Heading 3 (h3)** | 1.25rem / 20px | 600 (SemiBold) | 1.4 | 0 | Card Header, Section nhỏ |
| **Heading 4 (h4)** | 1.00rem / 16px | 600 (SemiBold) | 1.5 | 0 | Tên sản phẩm, Tiêu đề form |
| **Body Text** | 0.9375rem / 15px | 400 (Regular) | 1.5 | 0 | Mô tả sản phẩm, Văn bản chung |
| **Caption** | 0.8125rem / 13px | 400 (Regular) | 1.4 | 0 | Ghi chú, Subtitle, Mã SKU |
| **Label** | 0.875rem / 14px | 600 (SemiBold) | 1.4 | 0 | Form Label, Table Header |
| **Button Text** | 0.875rem / 14px | 600 (SemiBold) | 1.0 | 0 | Nút bấm thao tác |

### 1.2 Color System

Bảng màu được phối hợp hài hòa giữa **Indigo (Primary)** và **Teal (Secondary)** trên nền trung tính Slate Neutral, tối ưu cho tương phản accessibility (WCAG AA).

```text
[Primary: #4F46E5] ── [Secondary: #0D9488] ── [Surface: #FFFFFF] ── [Background: #F8FAFC]
```

* **Primary (Indigo):** `#4F46E5` (Hover: `#4338CA`, Light BG: `#EEF2FF`) – Dùng cho CTA chính, giá sản phẩm, active link.
* **Secondary (Teal):** `#0D9488` (Hover: `#0F766E`, Light BG: `#CCFBF1`) – Dùng cho Badge danh mục, nút phụ.
* **Accent (Amber):** `#F59E0B` – Dùng cho đánh giá, sản phẩm nổi bật.
* **Neutral Background:** `#F8FAFC` (Slate 50) – Nền toàn trang.
* **Surface / Card:** `#FFFFFF` – Nền cho Card, Modal, Table, Header.
* **Text Main:** `#0F172A` (Slate 900) – Chữ tiêu đề chính.
* **Text Muted:** `#64748B` (Slate 500) – Chữ mô tả, caption.
* **Border:** `#E2E8F0` (Slate 200) – Đường viền card, input, table.
* **Status Colors:**
  * Success: `#10B981` (Emerald) – Còn hàng, Active.
  * Danger/Error: `#EF4444` (Red) – Hết hàng, Xóa, Lỗi API.
  * Warning: `#F59E0B` (Amber) – Tồn kho thấp, Khóa tài khoản.
  * Info: `#3B82F6` (Blue) – Role Admin, Thông tin hệ thống.

### 1.3 Spacing Scale & Grid

Áp dụng Spacing scale dựa trên bội số **4px / 8px**:
* `4px` (`--space-1`)
* `8px` (`--space-2`)
* `12px` (`--space-3`)
* `16px` (`--space-4`)
* `24px` (`--space-6`)
* `32px` (`--space-8`)
* `48px` (`--space-12`)
* `64px` (`--space-16`)

### 1.4 Border Radius & Shadows

* **Radius Small (`6px`):** Nút nhỏ, input field, badge.
* **Radius Medium (`8px`):** Nút tiêu chuẩn, thumbnail ảnh, item list.
* **Radius Large (`12px`):** Card sản phẩm, container table.
* **Radius Extra Large (`16px`):** Modal dialog, Hero card.
* **Radius Full (`9999px`):** Avatar, Search input pill, Badge số lượng giỏ hàng.
* **Shadows:** Sử dụng shadow nhẹ (`0 1px 3px rgba(0,0,0,0.1)`), tăng nhẹ khi hover (`0 10px 15px -3px rgba(0,0,0,0.1)`).

---

## 2. UX INFORMATION ARCHITECTURE (SITEMAP)

Cấu trúc phân cấp màn hình chuẩn hóa từ `docs/test/SRS.md`:

```text
Couppa Website System
│
├── Public Site (Guest & User)
│   ├── Home Page (index.html)
│   ├── Product Listing & Search (products.html)
│   ├── Product Detail Page (product-detail.html)
│   ├── Shopping Cart Page (cart.html)
│   ├── Authentication
│   │   ├── Login (login.html)
│   │   └── Register (register.html)
│   └── User Account
│       └── Profile & Change Password (profile.html)
│
└── Admin Panel (Role = Admin)
    ├── Dashboard Overview (admin/dashboard.html)
    ├── Product Management CRUD (admin/products.html)
    ├── Category Management CRUD (admin/categories.html)
    ├── User Management (admin/users.html)
    └── Reports & Analytics (admin/reports.html)
```

---

## 3. USER FLOWS

### 3.1 Guest User Flow (Browsing & Cart Session)
```text
[Guest User] ──> Truy cập Trang Chủ / Sản Phẩm
                    │
                    ├──> Tìm kiếm / Lọc theo Danh Mục / Khoảng Giá
                    │
                    ├──> Xem Chi Tiết Sản Phẩm (Nút "Thêm Giỏ" disable nếu hết hàng)
                    │
                    └──> Nhấn "Thêm Vào Giỏ" ──> Server cấp Session Cookie ──> Lưu cart_items DB (DD-02)
                                                                  │
                                                                  └──> Xem Giỏ Hàng & Thay đổi số lượng
```

### 3.2 Login & Cart Merge Flow (Guest ──> User)
```text
[Guest có giỏ hàng] ──> Nhấn Đăng Nhập ──> Nhập Email/Pass ──> Cookie Auth (couppa.auth)
                                                                   │
                                                                   └──> Server kích hoạt Merge Guest Cart vào User Cart (BR-13)
                                                                                                 │
                                                                                                 └──> Chuyển hướng Trang Chủ / Admin (nếu Admin)
```

### 3.3 Admin Workflow (CRUD & Governance)
```text
[Admin User] ──> Đăng nhập Admin ──> Admin Dashboard (KPIs, Active/Out-of-stock count)
                                         │
                                         ├──> Quản lý Sản Phẩm (Thêm / Sửa / Soft Delete / Toggle Active / Invalidate Cache)
                                         ├──> Quản lý Danh Mục (Thêm / Sửa / Toggle Active / Kiểm tra BR-03 trước khi Xóa)
                                         ├──> Quản lý Người Dùng (Xem danh sách / Đổi Role / Khóa tài khoản - Kiểm tra BR-14)
                                         └──> Báo Cáo Thống Kê (Top sản phẩm thêm giỏ từ cart_activity_logs)
```

---

## 4. REUSABLE COMPONENT LIBRARY

| Component Name | Trách nhiệm UI | Sub-components / Elements | Props / States |
|---|---|---|---|
| `HeaderNavbar` | Điều hướng toàn trang, tìm kiếm nhanh | Logo, NavLinks, SearchBar, CartBadge, AuthControl | `currentPath`, `cartCount`, `userAuth` |
| `ProductCard` | Hiển thị sản phẩm dạng lưới | Image, CategoryBadge, Title, Price, StockBadge, AddToCartBtn | `productObj`, `onAddToCart()` |
| `CartItemRow` | Hiển thị item giỏ hàng | Thumbnail, Name, SKU, Price, QuantityControl, Subtotal, RemoveBtn | `cartItem`, `onUpdateQty()`, `onRemove()` |
| `CartSummaryCard` | Thống kê số lượng & tổng tiền | TotalQty, Subtotal, ShippingBadge, GrandTotal, CheckoutCTA | `totalQty`, `grandTotal` |
| `DataTable` | Hiển thị bảng danh sách dữ liệu Admin | TableHeader, TableRow, ActionButtons, Pagination | `columns[]`, `data[]`, `onEdit()`, `onDelete()` |
| `StatCard` | Thẻ chỉ số KPI Dashboard | Icon, Title, Value, StatusBadge | `icon`, `title`, `value`, `color` |
| `ModalDialog` | Cửa sổ popup cho Form CRUD | Header, FormBody, ActionFooter | `modalId`, `isOpen`, `title`, `onSave()` |
| `ToastContainer` | Hiển thị thông báo phản hồi thao tác | SuccessToast, ErrorToast, InfoToast | `message`, `type ('success'\|'error'\|'info')` |
| `EmptyState` | Hiển thị khi danh sách/giỏ hàng rỗng | Icon, Title, Description, ActionButton | `title`, `desc`, `actionLabel`, `onAction()` |

---

## 5. FULL PAGE SPECIFICATIONS & FILE MAPPING

Toàn bộ 12 trang giao diện đã được thiết kế và lưu trữ đầy đủ tại `docs/test/html`:

| STT | Màn hình | Đường dẫn File HTML | Vai trò & Đặc tả UI |
|---|---|---|---|
| 1 | **Trang Chủ** | `docs/test/html/index.html` | Hero Banner, Category Cards, Featured Products Grid (dùng cache `products:featured`), Latest Products. |
| 2 | **Danh Sách Sản Phẩm** | `docs/test/html/products.html` | Header Search, Sidebar Filter (Category, Price Range), Sort Dropdown, Product Grid, Empty Search State, Pagination. |
| 3 | **Chi Tiết Sản Phẩm** | `docs/test/html/product-detail.html` | Image Gallery, Product Info, Stock Status Badge (disable "Thêm Giỏ" nếu hết hàng), Quantity Selector, Related Products. |
| 4 | **Giỏ Hàng** | `docs/test/html/cart.html` | Bảng Items (ảnh, tên, đơn giá, +/- số lượng, tạm tính), Cart Summary Card, Clear Cart, Empty Cart State. |
| 5 | **Đăng Nhập** | `docs/test/html/login.html` | Form đăng nhập, Cookie Auth simulation, Nút điền nhanh Demo Account (User/Admin). |
| 6 | **Đăng Ký** | `docs/test/html/register.html` | Form đăng ký tài khoản mới, client-side validation mật khẩu theo BR-06, kiểm tra trùng email (BR-04). |
| 7 | **Hồ Sơ Cá Nhân** | `docs/test/html/profile.html` | Xem/sửa thông tin cá nhân (họ tên, SĐT), Form đổi mật khẩu (yêu cầu mật khẩu cũ). |
| 8 | **Admin Dashboard** | `docs/test/html/admin/dashboard.html` | 4 KPI Stat Cards, Biểu đồ phân bổ sản phẩm theo danh mục, Caching Status Monitor, Bảng 5 sản phẩm mới nhất. |
| 9 | **Admin Quản Lý Sản Phẩm** | `docs/test/html/admin/products.html` | Bảng CRUD sản phẩm, Search/Filter, Modal thêm/sửa sản phẩm (SKU unique BR-05, Price ≥ 0 BR-09, Stock ≥ 0 BR-10). |
| 10 | **Admin Quản Lý Danh Mục** | `docs/test/html/admin/categories.html` | Bảng CRUD danh mục, Modal thêm/sửa, Tự tạo Slug URL, Kiểm tra BR-03 từ chối xóa khi còn sản phẩm liên kết. |
| 11 | **Admin Quản Lý Người Dùng**| `docs/test/html/admin/users.html` | Bảng người dùng, Filter theo role/trạng thái, Toggle Role Admin/User, Nút Khóa/Mở khóa (Kiểm tra BR-14 chống tự khóa). |
| 12 | **Báo Cáo Thống Kê** | `docs/test/html/admin/reports.html` | Thống kê sản phẩm theo danh mục (FR-REPORT-001), Thống kê tồn kho (FR-REPORT-002), Top sản phẩm thêm giỏ từ `cart_activity_logs` (FR-REPORT-004). |

---

## 6. RESPONSIVE RULES & BREAKPOINTS

Giao diện được xây dựng tương thích hoàn hảo trên 3 cấp độ kích thước màn hình theo yêu cầu SRS (NFR-RESP-01 / Chapter 17):

| Breakpoint | Kích thước | Quy tắc Layout UI Phụ Trách |
|---|---|---|
| **Desktop** | `≥ 1024px` | Grid sản phẩm 4 cột, Sidebar Filter cố định bên trái trang Products, Admin Sidebar 260px mở rộng đầy đủ. |
| **Tablet** | `768px – 1023px` | Grid sản phẩm 2-3 cột, Cart summary xếp chồng bên dưới bảng items, Admin sidebar thu gọn icon/drawer. |
| **Mobile** | `< 768px` | Grid sản phẩm 1-2 cột, Nav menu chuyển dạng Hamburger, Search bar thu gọn, Bảng table có scroll ngang hoặc dạng Card view. |

---

## 7. UX STATES SPECIFICATION

Mọi màn hình và chức năng quan trọng đều được quy định đầy đủ 5 trạng thái UX:

1. **Default State:** Giao diện hiển thị đầy đủ dữ liệu tiêu chuẩn.
2. **Loading / Skeleton State:** Hiển thị khối Skeleton mờ (`.skeleton`) trong lúc chờ dữ liệu API `GET /api/products` hoặc `GET /api/admin/dashboard/summary`.
3. **Empty State:** Hiển thị giao diện rỗng đẹp mắt (`.empty-state`) kèm icon và nút CTA khi:
   * Giỏ hàng trống (`cart.html`).
   * Kết quả tìm kiếm không có sản phẩm (`products.html`).
   * Danh mục chưa có sản phẩm nào.
4. **Error State:**
   * Validation lỗi trường input: hiển thị dòng chữ đỏ ngay bên dưới ô nhập (`.form-error`).
   * API lỗi (409 Conflict SKU/Category, 401 Unauthorized, 403 Forbidden): hiển thị Toast đỏ thông báo rõ nguyên nhân nghiệp vụ.
5. **Success State:** Hiển thị Toast xanh lá (`.toast-success`) khi thêm giỏ hàng thành công, lưu sản phẩm/danh mục thành công, đổi mật khẩu thành công.

---

## 8. FRONTEND ARCHITECTURE

Hệ thống mã nguồn HTML/CSS/JS tại `docs/test/html` được tổ chức theo kiến trúc Component-based modularization:

```text
docs/test/html/
├── css/
│   └── styles.css        # Core Design System, CSS Variables, Typography, Layout, Components & Breakpoints
├── js/
│   └── main.js           # Core JS Engine: App State, LocalStorage Simulation, Cart Manager, Toast & Modal Helpers
├── admin/
│   ├── dashboard.html    # Admin Dashboard Overview
│   ├── products.html     # Admin Product Management CRUD
│   ├── categories.html   # Admin Category Management CRUD
│   ├── users.html        # Admin User Management & Locking
│   └── reports.html      # Admin Reports & Cart Activity Analysis
├── index.html            # Public Homepage
├── products.html         # Public Product Listing & Search
├── product-detail.html   # Public Product Detail
├── cart.html             # Public Shopping Cart
├── login.html            # Authentication Login
├── register.html         # Authentication Register
└── profile.html          # User Profile & Password Change
```

---

## 9. API INTEGRATION MAPPING

Ma trận tích hợp giữa Frontend UI tại `docs/test/html` và Backend ASP.NET Core Web API (`docs/test/SRS.md` Chapter 15):

| Màn hình UI | Thao tác UI | HTTP Method & API Endpoint | Payload / Params | API Response & Action |
|---|---|---|---|---|
| `login.html` | Bấm "Đăng Nhập" | `POST /api/auth/login` | `{ email, password }` | 200 OK + `Set-Cookie` (couppa.auth), kích hoạt merge cart |
| `register.html` | Bấm "Tạo Tài Khoản" | `POST /api/auth/register` | `{ email, password, fullName }` | 201 Created |
| `index.html` | Load trang | `GET /api/products/featured` | — | 200 OK (Cache `products:featured`) |
| `products.html` | Gõ từ khóa / Lọc | `GET /api/products` | `search, category, minPrice, maxPrice, sort, page` | 200 OK (Danh sách phân trang) |
| `product-detail.html` | Load chi tiết | `GET /api/products/{id}` | `id` | 200 OK |
| `product-detail.html` | Bấm "+ Giỏ" | `POST /api/cart/items` | `{ productId, quantity }` | 201 Created + Ghi `cart_activity_logs` |
| `cart.html` | Tăng/giảm số lượng | `PUT /api/cart/items/{id}` | `{ quantity }` | 200 OK |
| `cart.html` | Nút "Xóa item" | `DELETE /api/cart/items/{id}` | — | 200 OK |
| `cart.html` | "Xóa toàn bộ giỏ" | `DELETE /api/cart` | — | 200 OK |
| `admin/products.html` | Thêm sản phẩm | `POST /api/admin/products` | `{ sku, name, categoryId, price, stockQuantity, ... }` | 201 Created + Invalidate Cache |
| `admin/products.html` | Toggle Active | `PATCH /api/admin/products/{id}/status` | `{ isActive }` | 200 OK + Invalidate Cache |
| `admin/categories.html`| Xóa danh mục | `DELETE /api/admin/categories/{id}` | — | 200 OK hoặc 409 Conflict (nếu còn sản phẩm liên kết BR-03) |
| `admin/users.html` | Khóa tài khoản | `PATCH /api/admin/users/{id}/lock` | `{ isLocked }` | 200 OK hoặc 409 Conflict (nếu Admin tự khóa mình BR-14) |
| `admin/reports.html` | Load báo cáo Top Cart| `GET /api/admin/reports/cart-top-products` | `from, to` | 200 OK |

---

## 10. REQUIREMENT ──> UI TRACEABILITY MATRIX

Ma trận truy xuất nguồn gốc từ Yêu cầu Nghiệp vụ (FR/BR) trong SRS đến Màn hình UI thực tế:

| SRS Requirement ID | Tên Yêu Cầu Nghiệp Vụ | Màn Hình UI Đáp Ứng | File HTML Triển Khai | Trạng Thái UX |
|---|---|---|---|---|
| **FR-AUTH-001** | Đăng ký tài khoản mới | Register Page | `docs/test/html/register.html` | Form, Toast, Error Validation |
| **FR-AUTH-002** | Đăng nhập Cookie + Session | Login Page | `docs/test/html/login.html` | Form, Demo quick fills, Toast |
| **FR-AUTH-003** | Đăng xuất tài khoản | Header / Profile | `docs/test/html/profile.html` | Button, Redirect |
| **FR-BROWSE-001** | Trang chủ sản phẩm nổi bật/mới | Homepage | `docs/test/html/index.html` | Cards Grid, Badges |
| **FR-BROWSE-004** | Tìm kiếm sản phẩm theo tên | Products Page | `docs/test/html/products.html` | Search Input, Fetch API mock |
| **FR-BROWSE-005** | Lọc theo danh mục & khoảng giá | Products Page | `docs/test/html/products.html` | Sidebar Radio & Range Inputs |
| **FR-BROWSE-007** | Hiển thị badge Hết hàng | Listing & Detail | `docs/test/html/product-detail.html` | Badge "Hết hàng", Disable Button |
| **FR-CART-001** | Thêm sản phẩm vào giỏ | Product Cards / Detail | `docs/test/html/product-detail.html` | Quantity control, Toast, Cart Badge |
| **FR-CART-002..006**| Quản lý giỏ hàng (sửa/xóa/xóa hết)| Cart Page | `docs/test/html/cart.html` | Table items, Subtotal, Empty State |
| **FR-CART-007 / BR-13**| Merge giỏ hàng Guest khi login | Auth JS Engine | `docs/test/html/js/main.js` | Auto merge logic simulation |
| **FR-PRODUCT-001..006**| CRUD Sản phẩm cho Admin | Admin Products | `docs/test/html/admin/products.html` | Data Table, Form Modal, Status Toggle |
| **FR-CATEGORY-001..005**| CRUD Danh mục cho Admin | Admin Categories | `docs/test/html/admin/categories.html` | Data Table, Form Modal, Slug Auto |
| **BR-03** | Không cho xóa Category có SP | Admin Categories | `docs/test/html/admin/categories.html` | Validation Toast từ chối xóa |
| **FR-USER-004 / BR-14**| Khóa User & Chống Admin tự khóa | Admin Users | `docs/test/html/admin/users.html` | Lock/Unlock Button, Guard Toast |
| **FR-DASH-001..004** | Admin Dashboard KPIs & Charts | Admin Dashboard | `docs/test/html/admin/dashboard.html` | Stat Cards, Category Bar Chart |
| **FR-REPORT-001..004**| Báo cáo thống kê & Top Cart | Admin Reports | `docs/test/html/admin/reports.html` | Data Tables, Visual Progress Bars |
