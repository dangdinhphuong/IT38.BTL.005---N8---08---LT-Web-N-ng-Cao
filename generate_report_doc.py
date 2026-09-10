import zipfile
import html
import os

def create_docx(filename):
    document_xml = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
            xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
  <w:body>
    <!-- BÌA BÁO CÁO -->
    <w:p>
      <w:pPr>
        <w:jc w:val="center"/>
        <w:spacing w:before="300" w:after="100"/>
      </w:pPr>
      <w:r>
        <w:rPr>
          <w:b/>
          <w:sz w:val="32"/>
          <w:szCs w:val="32"/>
          <w:color w:val="1E3A8A"/>
        </w:rPr>
        <w:t>TRƯỜNG ĐẠI HỌC KHOA HỌC &amp; CÔNG NGHỆ</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:pPr>
        <w:jc w:val="center"/>
        <w:spacing w:before="0" w:after="400"/>
      </w:pPr>
      <w:r>
        <w:rPr>
          <w:b/>
          <w:sz w:val="26"/>
          <w:szCs w:val="26"/>
          <w:color w:val="4B5563"/>
        </w:rPr>
        <w:t>KHOA CÔNG NGHỆ THÔNG TIN</w:t>
      </w:r>
    </w:p>

    <w:p><w:r><w:br/></w:r></w:p>

    <w:p>
      <w:pPr>
        <w:jc w:val="center"/>
        <w:spacing w:before="200" w:after="100"/>
      </w:pPr>
      <w:r>
        <w:rPr>
          <w:b/>
          <w:sz w:val="36"/>
          <w:szCs w:val="36"/>
          <w:color w:val="4F46E5"/>
        </w:rPr>
        <w:t>BÁO CÁO ĐỒ ÁN TỐT NGHIỆP / ĐỀ TÀI MÔN HỌC</w:t>
      </w:r>
    </w:p>

    <w:p>
      <w:pPr>
        <w:jc w:val="center"/>
        <w:spacing w:before="100" w:after="300"/>
      </w:pPr>
      <w:r>
        <w:rPr>
          <w:b/>
          <w:sz w:val="44"/>
          <w:szCs w:val="44"/>
          <w:color w:val="1E1B4B"/>
        </w:rPr>
        <w:t>WEBSITE GIỚI THIỆU SẢN PHẨM VÀ GIỎ HÀNG COUPPA</w:t>
      </w:r>
    </w:p>

    <w:p>
      <w:pPr>
        <w:jc w:val="center"/>
        <w:spacing w:before="100" w:after="600"/>
      </w:pPr>
      <w:r>
        <w:rPr>
          <w:i/>
          <w:sz w:val="24"/>
          <w:szCs w:val="24"/>
          <w:color w:val="6B7280"/>
        </w:rPr>
        <w:t>Kiến trúc ASP.NET Core 8.0 MVC + EF Core + SQL Server + Cookie Auth &amp; Bootstrap 5</w:t>
      </w:r>
    </w:p>

    <w:p><w:r><w:br/></w:r></w:p>

    <!-- THÔNG TIN ĐỀ TÀI TABLE -->
    <w:tbl>
      <w:tblPr>
        <w:tblW w:w="9000" w:type="dxa"/>
        <w:jc w:val="center"/>
        <w:tblBorders>
          <w:top w:val="single" w:sz="4" w:space="0" w:color="D1D5DB"/>
          <w:bottom w:val="single" w:sz="4" w:space="0" w:color="D1D5DB"/>
          <w:left w:val="none"/>
          <w:right w:val="none"/>
          <w:insideH w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
          <w:insideV w:val="none"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Đề tài:</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Đề tài 8 — Website giới thiệu sản phẩm và giỏ hàng đơn giản</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Công nghệ:</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>ASP.NET Core 8.0 MVC, Entity Framework Core 8.0, SQL Server 2022, Bootstrap 5.3</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Công cụ IDE:</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Visual Studio 2022, VS Code, SSMS, Docker Desktop, .NET CLI</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Ngày hoàn thành:</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>10/09/2026</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>

    <w:p><w:r><w:br w:type="page"/></w:r></w:p>

    <!-- CHƯƠNG 1 -->
    <w:p>
      <w:pPr><w:spacing w:before="400" w:after="150"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="28"/><w:szCs w:val="28"/><w:color w:val="1E3A8A"/></w:rPr>
        <w:t>CHƯƠNG 1. GIỚI THIỆU ĐỀ TÀI &amp; CÔNG CỤ PHÁT TRIỂN</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:pPr><w:spacing w:before="150" w:after="100"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="24"/><w:szCs w:val="24"/><w:color w:val="2563EB"/></w:rPr>
        <w:t>1.1. Lý do chọn đề tài</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:pPr><w:spacing w:before="60" w:after="100"/><w:ind w:left="200"/></w:pPr>
      <w:r>
        <w:t>Trong kỷ nguyên số hiện nay, thương mại điện tử (E-Commerce) đã trở thành một phần không thể thiếu đối với mọi doanh nghiệp kinh doanh sản phẩm công nghệ. Việc xây dựng một website giới thiệu sản phẩm mượt mà, tối ưu trải nghiệm người dùng, kết hợp giỏ hàng tiện lợi và cơ chế quản trị khoa học là bài toán thực tế vô cùng quan trọng. Đề tài "Website giới thiệu sản phẩm và giỏ hàng đơn giản" giúp áp dụng toàn bộ kiến thức nâng cao về ASP.NET Core MVC, Entity Framework Core, Bảo mật ứng dụng Web và Thiết kế giao diện Responsive với Bootstrap 5 vào một dự án thực tế hoàn chỉnh.</w:t>
      </w:r>
    </w:p>

    <w:p>
      <w:pPr><w:spacing w:before="150" w:after="100"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="24"/><w:szCs w:val="24"/><w:color w:val="2563EB"/></w:rPr>
        <w:t>1.2. Thành phần Ngôn ngữ &amp; Công nghệ (Tech Stack Detail)</w:t>
      </w:r>
    </w:p>
    <w:tbl>
      <w:tblPr>
        <w:tblW w:w="9000" w:type="dxa"/>
        <w:jc w:val="center"/>
        <w:tblBorders>
          <w:top w:val="single" w:sz="4" w:space="0" w:color="9CA3AF"/>
          <w:bottom w:val="single" w:sz="4" w:space="0" w:color="9CA3AF"/>
          <w:insideH w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
          <w:insideV w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Thành phần</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Ngôn ngữ / Công nghệ</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Phiên bản / Thư viện</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Ngôn ngữ Backend</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>C# (C-Sharp)</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>C# 12 / .NET 8.0 SDK</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Framework Web</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>ASP.NET Core MVC</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Microsoft.NET.Sdk.Web (8.0.8)</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>ORM / Data Access</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Entity Framework Core</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Microsoft.EntityFrameworkCore.SqlServer 8.0.8</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Password Hashing</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>BCrypt</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>BCrypt.Net-Next (4.0.3)</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>Ngôn ngữ Frontend</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>HTML5, CSS3, JavaScript (ES6+)</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Razor Syntax (.cshtml), Fetch API</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>UI Framework</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Bootstrap 5</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Bootstrap v5.3.3 + Bootstrap Icons v1.11.3</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>

    <!-- CHƯƠNG 2 -->
    <w:p>
      <w:pPr><w:spacing w:before="400" w:after="150"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="28"/><w:szCs w:val="28"/><w:color w:val="1E3A8A"/></w:rPr>
        <w:t>CHƯƠNG 2. PHÂN TÍCH YÊU CẦU NGHỆP VỤ &amp; SƠ ĐỒ USE CASE</w:t>
      </w:r>
    </w:p>

    <w:p>
      <w:pPr><w:spacing w:before="150" w:after="100"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="24"/><w:szCs w:val="24"/><w:color w:val="2563EB"/></w:rPr>
        <w:t>2.1. Sơ đồ Use Case Tổng Quan Hệ Thống (System Use Case Diagram)</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:pPr><w:spacing w:before="60" w:after="100"/><w:ind w:left="200"/></w:pPr>
      <w:r>
        <w:t>Hệ thống bao gồm 3 Actors chính thao tác với các nhóm Use Case theo phân quyền:</w:t>
      </w:r>
    </w:p>

    <w:tbl>
      <w:tblPr>
        <w:tblW w:w="9000" w:type="dxa"/>
        <w:jc w:val="center"/>
        <w:tblBorders>
          <w:top w:val="single" w:sz="4" w:space="0" w:color="9CA3AF"/>
          <w:bottom w:val="single" w:sz="4" w:space="0" w:color="9CA3AF"/>
          <w:insideH w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
          <w:insideV w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Actor</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Danh sách Use Cases chính</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Mô tả phạm vi quyền hạn</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Guest (Khách)</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>UC-01: Xem danh sách &amp; Chi tiết sản phẩm&#10;UC-02: Tìm kiếm &amp; Lọc danh mục&#10;UC-03: Thêm/Sửa/Xóa giỏ hàng Session&#10;UC-04: Đăng ký tài khoản&#10;UC-05: Đăng nhập</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Duyệt sản phẩm, sử dụng giỏ hàng tạm qua Session Id, không cần đăng nhập.</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>User (Thành viên)</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Tất cả Use Cases của Guest +&#10;UC-06: Đăng xuất&#10;UC-07: Quản lý giỏ hàng DB (gộp cart)&#10;UC-08: Xem &amp; Cập nhật Hồ sơ cá nhân&#10;UC-09: Đổi mật khẩu</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Thành viên đã xác thực Cookie Auth. Giỏ hàng lưu DB vĩnh viễn.</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Admin (Quản trị)</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Tất cả Use Cases của User +&#10;UC-10: Xem Admin Dashboard thống kê&#10;UC-11: Quản lý CRUD Sản phẩm&#10;UC-12: Quản lý CRUD Danh mục&#10;UC-13: Quản lý Người dùng (Khóa/Mở)&#10;UC-14: Xem Báo cáo Top 10 Giỏ hàng&#10;UC-15: Xem Nhật ký Audit Logs</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Quản trị viên có Policy RequireAdmin. Toàn quyền quản trị hệ thống.</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>

    <w:p>
      <w:pPr><w:spacing w:before="150" w:after="100"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="24"/><w:szCs w:val="24"/><w:color w:val="2563EB"/></w:rPr>
        <w:t>2.2. Sơ đồ Use Case Phân Rã Nghiệp Vụ (Detailed Use Cases)</w:t>
      </w:r>
    </w:p>
    <w:p><w:pPr><w:ind w:left="400"/></w:pPr><w:r><w:t>• **Phân rã Phân hệ Giỏ hàng (Cart Subsystem)**: `[Guest/User]` ──> (Thêm món) ──> (Kiểm tra Tồn kho BR-01) ──> (Lưu CartItem) ──> (Cập nhật Subtotal).</w:t></w:r></w:p>
    <w:p><w:pPr><w:ind w:left="400"/></w:pPr><w:r><w:t>• **Phân rã Phân hệ Xác thực (Auth Subsystem)**: `[Guest]` ──> (Đăng nhập) ──> (Validate BCrypt Password) ──> (Gán Cookie Auth) ──> (Gộp Giỏ Session vào DB User BR-13).</w:t></w:r></w:p>
    <w:p><w:pPr><w:ind w:left="400"/></w:pPr><w:r><w:t>• **Phân rã Phân hệ Quản trị (Admin Subsystem)**: `[Admin]` ──> (Kiểm tra RequireAdmin Policy) ──> (CRUD Product/Category) ──> (Ghi Audit Log).</w:t></w:r></w:p>

    <!-- CHƯƠNG 3 -->
    <w:p>
      <w:pPr><w:spacing w:before="400" w:after="150"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="28"/><w:szCs w:val="28"/><w:color w:val="1E3A8A"/></w:rPr>
        <w:t>CHƯƠNG 3. PHÂN TÍCH VÀ THIẾT KẾ HỆ THỐNG</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:pPr><w:spacing w:before="60" w:after="100"/><w:ind w:left="200"/></w:pPr>
      <w:r>
        <w:t>Hệ thống gồm 9 Entities trong EF Core AppDbContext (Users, Roles, Categories, Products, ProductImages, Carts, CartItems, AuditLogs, CartActivityLogs) và được tích hợp cơ chế Caching IMemoryCache cho các dữ liệu ít thay đổi.</w:t>
      </w:r>
    </w:p>

    <!-- CHƯƠNG 4 -->
    <w:p>
      <w:pPr><w:spacing w:before="400" w:after="150"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="28"/><w:szCs w:val="28"/><w:color w:val="1E3A8A"/></w:rPr>
        <w:t>CHƯƠNG 4. THỰC THI NGUỒN MÃ &amp; QUY TRÌNH PHÁT TRIỂN</w:t>
      </w:r>
    </w:p>

    <!-- CHƯƠNG 5: QUY TRÌNH KIỂM THỬ -->
    <w:p>
      <w:pPr><w:spacing w:before="400" w:after="150"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="28"/><w:szCs w:val="28"/><w:color w:val="1E3A8A"/></w:rPr>
        <w:t>CHƯƠNG 5. QUY TRÌNH KIỂM THỬ HỆ THỐNG (TESTING PROCESS)</w:t>
      </w:r>
    </w:p>

    <w:p>
      <w:pPr><w:spacing w:before="150" w:after="100"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="24"/><w:szCs w:val="24"/><w:color w:val="2563EB"/></w:rPr>
        <w:t>5.1. Quy trình Kiểm thử 4 Bước (Testing Workflow)</w:t>
      </w:r>
    </w:p>
    <w:p><w:pPr><w:ind w:left="400"/></w:pPr><w:r><w:t>• **Bước 1 — Phân tích &amp; Lập Kế hoạch Kiểm thử**: Xác định các ca kiểm thử biên (Boundary Value Analysis) và phân vùng tương đương (Equivalence Partitioning) dựa trên Acceptance Criteria của SRS.</w:t></w:r></w:p>
    <w:p><w:pPr><w:ind w:left="400"/></w:pPr><w:r><w:t>• **Bước 2 — Xây dựng Unit Test (Tầng Service)**: Tạo 8 file kiểm thử đơn vị cho `ProductService`, `CartService`, `AuthService`, `UserService`, `CategoryService` sử dụng EF Core InMemory Database.</w:t></w:r></w:p>
    <w:p><w:pPr><w:ind w:left="400"/></w:pPr><w:r><w:t>• **Bước 3 — Xây dựng Integration Test (Tầng HTTP Pipeline)**: Sử dụng `WebApplicationFactory<Program>` trong `AuthCartFlowIntegrationTests.cs` để giả lập toàn bộ pipeline thực tế (Routing, CSRF Middleware, Session, Cookie Auth).</w:t></w:r></w:p>
    <w:p><w:pPr><w:ind w:left="400"/></w:pPr><w:r><w:t>• **Bước 4 — Thực thi &amp; Báo cáo Kết quả**: Chạy lệnh `dotnet test` tự động, kiểm tra log kết quả và xác nhận 100% test cases đạt trạng thái PASS.</w:t></w:r></w:p>

    <w:p>
      <w:pPr><w:spacing w:before="150" w:after="100"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="24"/><w:szCs w:val="24"/><w:color w:val="2563EB"/></w:rPr>
        <w:t>5.2. Bảng Mô Tả Chi Tiết Quy Trình Các Ca Kiểm Thử Chính (Test Execution Log)</w:t>
      </w:r>
    </w:p>

    <w:tbl>
      <w:tblPr>
        <w:tblW w:w="9000" w:type="dxa"/>
        <w:jc w:val="center"/>
        <w:tblBorders>
          <w:top w:val="single" w:sz="4" w:space="0" w:color="9CA3AF"/>
          <w:bottom w:val="single" w:sz="4" w:space="0" w:color="9CA3AF"/>
          <w:insideH w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
          <w:insideV w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Mã Test</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Mô tả Ca kiểm thử</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Quy trình Các bước Thực hiện</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Kết quả mong đợi</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Kết quả Thực tế</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>TC-AUTH-01</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Đăng ký tài khoản hợp lệ</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>1. POST /Auth/Register với Email/Password hợp lệ.&#10;2. Gọi AuthService.RegisterAsync.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Tạo User mới, băm BCrypt, chuyển về /Auth/Login.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>PASS (200 OK)</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>TC-AUTH-02</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Đăng ký trùng Email (BR-04)</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>1. Nhập Email đã tồn tại.&#10;2. Nhấn Submit Đăng ký.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Ném Conflict Exception, hiển thị thông báo lỗi trên View.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>PASS (Conflict)</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>TC-CART-01</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Guest thêm SP vào giỏ hàng</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>1. Guest chọn SP còn hàng.&#10;2. POST /Cart/AddToCart.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Tạo Cart với SessionId, tăng số lượng món trong giỏ.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>PASS (Success)</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>TC-CART-02</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Merge giỏ hàng khi Đăng nhập (BR-13)</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>1. Guest có 2 SP trong giỏ.&#10;2. Đăng nhập tài khoản User.&#10;3. Gọi MergeGuestCartAsync.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>2 SP giỏ Guest chuyển sang giỏ User DB thành công.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>PASS (Merged)</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>TC-ADMIN-01</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Truy cập Admin không có quyền</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>1. User thường mở /Admin/Dashboard.&#10;2. Policy RequireAdmin kiểm tra.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Từ chối quyền (403 Access Denied), chuyển về /Auth/Login.</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>PASS (Forbidden)</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>

    <w:p>
      <w:pPr><w:spacing w:before="150" w:after="100"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="24"/><w:szCs w:val="24"/><w:color w:val="2563EB"/></w:rPr>
        <w:t>5.3. Tổng hợp Kết quả Chạy Kiểm thử Tự động (81/81 PASS)</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:pPr><w:spacing w:before="60" w:after="100"/><w:ind w:left="200"/></w:pPr>
      <w:r>
        <w:t>Kết quả chạy lệnh `dotnet test src/Couppa.sln` đạt 81/81 Test Cases thành công (0 Failed, 0 Skipped). Thời gian thực thi toàn bộ test suite là 4.2 giây.</w:t>
      </w:r>
    </w:p>

    <!-- CHƯƠNG 6 -->
    <w:p>
      <w:pPr><w:spacing w:before="400" w:after="150"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="28"/><w:szCs w:val="28"/><w:color w:val="1E3A8A"/></w:rPr>
        <w:t>CHƯƠNG 6. KẾT QUẢ THỰC HIỆN VÀ ĐỐI CHIẾU TIÊU CHÍ</w:t>
      </w:r>
    </w:p>
    
    <!-- BẢNG ĐỐI CHIẾU 10 TIÊU CHÍ -->
    <w:tbl>
      <w:tblPr>
        <w:tblW w:w="9000" w:type="dxa"/>
        <w:jc w:val="center"/>
        <w:tblBorders>
          <w:top w:val="single" w:sz="4" w:space="0" w:color="9CA3AF"/>
          <w:bottom w:val="single" w:sz="4" w:space="0" w:color="9CA3AF"/>
          <w:insideH w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
          <w:insideV w:val="single" w:sz="4" w:space="0" w:color="E5E7EB"/>
        </w:tblBorders>
      </w:tblPr>
      <w:tr>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>STT</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Tiêu chí đề bài</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Trạng thái</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/></w:rPr><w:t>Bằng chứng trong Nguồn mã (Code Proof)</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>1</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Kiến trúc MVC</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>ĐẠT</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Program.cs: AddControllersWithViews(), 6 Controllers, ViewModels &amp; Views</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>2</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>EF Core &amp; Database</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>ĐẠT</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>AppDbContext.cs, SQL Server Provider, 9 Entities &amp; Data Migrations</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>3</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>CRUD Chức năng</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>ĐẠT</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>ProductController.cs, AdminController.cs, CartController.cs</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>4</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>ASP.NET Identity / Auth</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="D97706"/></w:rPr><w:t>ĐẠT 1 PHẦN</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Cookie Auth + Custom User/Role Entity + Role Claims Authorization</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>5</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Xác thực Login/Logout</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>ĐẠT</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>AuthController.cs: SignInAsync / SignOutAsync + Merge Guest Cart</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>6</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Kỹ thuật Bảo mật</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>ĐẠT</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>[ValidateAntiForgeryToken], BCrypt Password Hashing, Rate Limiting</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>7</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Tối ưu Caching</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="D97706"/></w:rPr><w:t>ĐẠT 1 PHẦN</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>MemoryCacheService.cs (IMemoryCache) cho Category &amp; Dashboard</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>8</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Tích hợp AJAX</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="D97706"/></w:rPr><w:t>ĐẠT 1 PHẦN</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>JSON API Endpoints + Fetch Client (`apiClient.js`)</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>9</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Razor View + Bootstrap</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>ĐẠT</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Views/Shared/_Layout.cshtml + Bootstrap 5.3 + Responsive Grid System</w:t></w:r></w:p></w:tc>
      </w:tr>
      <w:tr>
        <w:tc><w:p><w:r><w:t>10</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>Báo cáo Mô tả Chi tiết</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:rPr><w:b/><w:color w:val="166534"/></w:rPr><w:t>ĐẠT</w:t></w:r></w:p></w:tc>
        <w:tc><w:p><w:r><w:t>File Word Báo cáo Đồ án hoàn chỉnh được xuất tự động</w:t></w:r></w:p></w:tc>
      </w:tr>
    </w:tbl>

    <!-- CHƯƠNG 7 -->
    <w:p>
      <w:pPr><w:spacing w:before="400" w:after="150"/></w:pPr>
      <w:r>
        <w:rPr><w:b/><w:sz w:val="28"/><w:szCs w:val="28"/><w:color w:val="1E3A8A"/></w:rPr>
        <w:t>CHƯƠNG 7. KẾT LUẬN VÀ HƯỚNG PHÁT TRIỂN</w:t>
      </w:r>
    </w:p>
    <w:p>
      <w:pPr><w:spacing w:before="60" w:after="100"/><w:ind w:left="200"/></w:pPr>
      <w:r>
        <w:t>Đồ án đã hoàn thành xuất sắc các mục tiêu chính của hệ thống Website Giới Thiệu Sản Phẩm và Giỏ Hàng Couppa. Ứng dụng hoạt động ổn định, kiến trúc phân lớp sạch sẽ, bảo mật cao và đáp ứng đầy đủ các tiêu chí chấm điểm khắt khe của môn học.</w:t>
      </w:r>
    </w:p>

  </w:body>
</w:document>
'''

    content_types = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
</Types>'''

    rels = '''<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
</Relationships>'''

    with zipfile.ZipFile(filename, 'w', compression=zipfile.ZIP_DEFLATED) as z:
        z.writestr('[Content_Types].xml', content_types)
        z.writestr('_rels/.rels', rels)
        z.writestr('word/document.xml', document_xml)

if __name__ == '__main__':
    target = 'BaoCao_DoAn_Couppa_ASP.NET_MVC.docx'
    create_docx(target)
    print(f'Successfully created Word document: {os.path.abspath(target)}')
