# Couppa — Website giới thiệu sản phẩm và giỏ hàng

Couppa là ứng dụng ASP.NET Core MVC được triển khai theo yêu cầu trong
[`docs/SRS.md`](docs/SRS.md). Ứng dụng sử dụng Razor View, Bootstrap,
ASP.NET Core Identity, Entity Framework Core, giỏ hàng lưu trong database cho
cả Guest và User, cùng cơ chế cache cho các dữ liệu được yêu cầu trong SRS.

## Công nghệ

- .NET 8 / ASP.NET Core MVC
- Razor Views và Bootstrap
- ASP.NET Core Identity Cookie Authentication
- Entity Framework Core
- SQL Server Express cho môi trường local
- PostgreSQL cho môi trường Docker
- ASP.NET Core Session cho định danh giỏ hàng Guest
- IMemoryCache
- xUnit, Moq và ASP.NET Core integration testing

## Cấu trúc project

```text
.
├── docs/SRS.md                 # Tài liệu yêu cầu chính
├── src/
│   ├── Couppa.Api/             # Ứng dụng MVC
│   │   ├── Controllers/
│   │   ├── Data/
│   │   ├── Services/
│   │   ├── Views/
│   │   └── wwwroot/
│   ├── Couppa.Api.Tests/       # Unit test và integration test
│   └── docker-compose.yml      # PostgreSQL + ứng dụng
└── html/                       # Giao diện tĩnh cũ, không dùng bởi MVC
```

`docs/SRS.md` là nguồn yêu cầu chính. Khi code hoặc tài liệu khác có điểm
khác với SRS, ưu tiên nội dung của SRS mới nhất.

## Yêu cầu môi trường

- .NET 8 SDK
- SQL Server Express (khi chạy local), hoặc Docker Desktop (khi chạy Docker)
- Git

Kiểm tra SDK:

```powershell
dotnet --version
```

## Chạy local với SQL Server Express

Cấu hình mặc định tại
[`src/Couppa.Api/appsettings.json`](src/Couppa.Api/appsettings.json):

```json
{
  "Database": {
    "Provider": "SqlServer"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=IT38;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

Nếu SQL Server của bạn dùng server hoặc database khác, cập nhật
`Database:Provider` và `ConnectionStrings:DefaultConnection` trước khi chạy.

Khởi động ứng dụng từ thư mục gốc:

```powershell
dotnet restore
dotnet build src/Couppa.Api/Couppa.Api.csproj
dotnet run --project src/Couppa.Api/Couppa.Api.csproj
```

Ở chế độ `SqlServer`, ứng dụng dùng `EnsureCreatedAsync()` để tạo các bảng còn
thiếu trong database đã cấu hình. Không cần chạy migration PostgreSQL cho chế
độ này.

Các địa chỉ local:

- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

## Chạy bằng Docker với PostgreSQL

Docker sử dụng cấu hình
[`src/Couppa.Api/appsettings.Docker.json`](src/Couppa.Api/appsettings.Docker.json)
và database PostgreSQL trong `src/docker-compose.yml`.

```powershell
cd src
docker compose up --build
```

Sau khi container khởi động:

- Ứng dụng: `http://localhost:5001`
- PostgreSQL: `localhost:5432`
- Database: `couppa`
- User: `couppa`

Dừng và xóa container:

```powershell
cd src
docker compose down
```

Muốn xóa cả dữ liệu PostgreSQL được lưu trong volume, dùng lệnh sau và chỉ
thực hiện khi đã xác nhận không cần dữ liệu:

```powershell
docker compose down -v
```

## Tài khoản seed

Trong môi trường Development/Docker, ứng dụng seed role và tài khoản Admin:

```text
Email:    admin@couppa.com
Password: Admin@12345
```

User thông thường có thể đăng ký tại `/Account/Register`.

## Chức năng chính

Các nhóm chức năng hiện có gồm:

- Xem, tìm kiếm, lọc và sắp xếp sản phẩm.
- Quản lý sản phẩm và danh mục dành cho Admin.
- Đăng ký, đăng nhập, đăng xuất và phân quyền `User`/`Admin`.
- Giỏ hàng cho Guest và User: thêm, cập nhật, xóa item, xóa toàn bộ.
- Merge giỏ hàng Guest vào giỏ hàng User sau khi đăng nhập.
- Dashboard và báo cáo sản phẩm được thêm vào giỏ nhiều nhất.
- Cache danh mục, sản phẩm nổi bật, sản phẩm mới nhất và dashboard.
- Anti-forgery token, kiểm tra quyền ở backend và xử lý lỗi thống nhất.

Một số endpoint chính:

| Nhóm | Endpoint |
|---|---|
| Sản phẩm | `/Product/Index`, `/Product/Detail`, `/Product/Featured`, `/Product/Latest` |
| Giỏ hàng | `/Cart/Index`, `/Cart/AddItem`, `/Cart/UpdateItem`, `/Cart/RemoveItem`, `/Cart/Clear` |
| Tài khoản | `/Account/Register`, `/Account/Login`, `/Account/Logout` |
| Admin sản phẩm | `/Admin/Product/Index` |
| Admin danh mục | `/Admin/Category/Index` |
| Admin người dùng | `/Admin/User/Index` |
| Dashboard | `/Admin/Dashboard/Index` |
| Báo cáo | `/Admin/Report/CartTopProducts` |

## Chạy test

Từ thư mục gốc:

```powershell
dotnet test src/Couppa.Api.Tests/Couppa.Api.Tests.csproj
```

Test bao gồm các business rule của service và luồng MVC/integration chính.

## Cấu hình quan trọng

- `src/Couppa.Api/appsettings.json`: cấu hình chạy local, mặc định dùng SQL Server Express và database `IT38`.
- `src/Couppa.Api/appsettings.Docker.json`: cấu hình PostgreSQL trong Docker.
- `src/Couppa.Api/Program.cs`: đăng ký provider database, Identity, Session, CORS, rate limiting và khởi tạo database.
- `src/Couppa.Api/Data/AppDbContext.cs`: mapping entity, khóa ngoại, index và các business constraint.
- `src/Couppa.Api/Data/Migrations/`: migration dành cho provider PostgreSQL.

Không commit mật khẩu hoặc connection string chứa thông tin nhạy cảm lên
repository dùng chung. Với môi trường triển khai, nên ghi đè bằng biến môi
trường hoặc Secret Manager.
