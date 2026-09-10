# Couppa — Website Giới thiệu Sản phẩm và Giỏ hàng

Hệ thống ASP.NET Core Web API + frontend HTML/CSS/JS tĩnh, xây dựng theo [`docs/test/SRS.md`](../SRS.md), [16 task chi tiết](../tasks/README.md), và [plan triển khai 14 phase](../plans/2026-09-07-couppa-webapi-implementation.md).

## Cấu trúc project

```
docs/test/src/
├── Couppa.Api/              Backend — ASP.NET Core Web API (.NET 8)
├── Couppa.Api.Tests/        Unit test (xUnit + Moq + EF InMemory) + Integration test (WebApplicationFactory)
├── docker-compose.yml       SQL Server cho môi trường dev/demo
├── MANUAL_TEST_CHECKLIST.md Checklist kiểm thử thủ công (Permission Matrix + Acceptance Criteria)
├── TEST_COVERAGE_REPORT.md  Đối chiếu Traceability Matrix với test đã có
└── README.md                File này

docs/test/html/               Frontend — HTML/CSS/JS tĩnh, gọi API qua Fetch
```

## Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) + Docker Compose (chạy SQL Server)
- Trình duyệt hiện đại (Chrome/Edge/Firefox bản mới) — để mở frontend
- Một công cụ serve file tĩnh cho frontend, ví dụ [Live Server (VSCode extension)](https://marketplace.visualstudio.com/items?itemName=ritwickdey.LiveServer) — **bắt buộc chạy đúng cổng `5500`** (khớp cấu hình CORS mặc định, xem bên dưới)

> **[Assumption]** Hướng dẫn này giả định chạy trên máy phát triển cục bộ (localhost), đúng scope đồ án — không có hướng dẫn deploy production/cloud.

## Cách nhanh nhất — Docker Compose (build toàn bộ: DB + Backend + Frontend)

Không cần cài .NET SDK hay Live Server — chỉ cần Docker:

```bash
cd docs/test/src
docker compose up -d --build
```

Compose sẽ dựng 4 container:

| Service | Vai trò | Truy cập từ host |
|---|---|---|
| `mssql` | Microsoft SQL Server 2022 | `localhost:1433` |
| `api` | Backend ASP.NET Core (build từ [`Couppa.Api/Dockerfile`](Couppa.Api/Dockerfile)) | nội bộ, không public trực tiếp |
| `web` | Frontend tĩnh qua Nginx (build từ [`../html/Dockerfile`](../html/Dockerfile)) | nội bộ, không public trực tiếp |
| `caddy` | Reverse proxy HTTPS (chứng chỉ tự ký nội bộ) ra 2 cổng đúng như chạy thủ công | `https://localhost:5001` (API), `https://localhost:5500` (Frontend) |

Mở trình duyệt: `https://localhost:5500/index.html` (chấp nhận cảnh báo chứng chỉ tự ký lần đầu — do Caddy `tls internal` phát hành, không phải CA công khai).

Container `api` tự động:
- Generate migration `InitialCreate` khi build image (nếu `Couppa.Api/Data/Migrations` chưa có sẵn) và áp dụng (`Database.MigrateAsync()`) khi khởi động — không cần chạy `dotnet ef` thủ công.
- Seed dữ liệu mẫu (2 role, tài khoản Admin, danh mục + sản phẩm demo) vì chạy với `ASPNETCORE_ENVIRONMENT=Docker`.

Xem log / trạng thái:

```bash
docker compose logs -f api
docker compose ps
```

Dừng và xoá toàn bộ (kể cả dữ liệu SQL Server):

```bash
docker compose down -v
```

> Cấu hình riêng cho môi trường Docker nằm ở [`Couppa.Api/appsettings.Docker.json`](Couppa.Api/appsettings.Docker.json) (connection string trỏ tới service `mssql`, CORS origin `https://localhost:5500`) — không ảnh hưởng tới `appsettings.json` dùng khi chạy `dotnet run` trực tiếp.

Nếu muốn chạy thủ công từng phần (không dùng Docker cho Backend/Frontend) như trước đây, xem tiếp các bước bên dưới — vẫn cần `docker compose up -d` cho riêng `mssql`.

## Bước 1 — Khởi động SQL Server

```bash
cd docs/test/src
docker compose up -d mssql
```

Kiểm tra container đã sẵn sàng (`healthy`):

```bash
docker compose ps
```

Thông tin kết nối (khớp sẵn với `Couppa.Api/appsettings.json`):

| | |
|---|---|
| Server | `localhost,1433` |
| Database | `couppa` (tự tạo khi chạy migration lần đầu) |
| User Id | `sa` |
| Password | `Couppa_dev_password1` |

> **Lưu ý**: SQL Server không có biến môi trường tự tạo database như Postgres (`POSTGRES_DB`) — database `couppa` được EF Core tự tạo khi `Database.MigrateAsync()` chạy lần đầu (xem Bước 2).

## Bước 2 — Chạy Backend (Web API)

```bash
cd docs/test/src/Couppa.Api
dotnet restore
dotnet ef database update   # nếu đã cài dotnet-ef; xem "Ghi chú migration" bên dưới
dotnet run
```

Backend sẽ lắng nghe ở:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

(Cấu hình tại `Couppa.Api/Properties/launchSettings.json`.)

**Lần chạy đầu tiên**: nếu môi trường (`ASPNETCORE_ENVIRONMENT`) là `Development` (mặc định của `dotnet run`), hệ thống tự seed:
- 2 role (`User`, `Admin`)
- 1 tài khoản Admin: **`admin@couppa.com`** / **`Admin@12345`**
- 5 danh mục + ~25 sản phẩm demo

Xem chi tiết ở [`Couppa.Api/Data/Seed/DbSeeder.cs`](Couppa.Api/Data/Seed/DbSeeder.cs).

### Ghi chú migration

Project hiện dùng `AppDbContext` với schema khai báo qua Fluent API ([`Data/AppDbContext.cs`](Couppa.Api/Data/AppDbContext.cs)) nhưng **chưa có migration file nào được generate** (môi trường phát triển ban đầu không có .NET SDK để chạy `dotnet ef migrations add`). Trước khi chạy lần đầu, cần tự tạo migration:

```bash
cd docs/test/src/Couppa.Api
dotnet tool install --global dotnet-ef   # nếu chưa cài
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Migration sẽ tự áp dụng đúng CHECK constraint (BR-09/BR-10/BR-12), unique index, và `DeleteBehavior.Restrict` đã khai báo trong `AppDbContext`.

## Bước 3 — Chạy Frontend

Frontend là HTML/CSS/JS tĩnh ở [`docs/test/html/`](../html/), **không cần build**. Serve bằng Live Server (hoặc tương đương) ở cổng **5500**:

```bash
cd docs/test/html
# VSCode: chuột phải index.html -> "Open with Live Server"
# hoặc dùng http-server:
npx http-server -p 5500
```

Mở trình duyệt: `http://localhost:5500/index.html`

> **Quan trọng**: cổng frontend phải khớp với `Cors:AllowedOrigin` trong [`Couppa.Api/appsettings.json`](Couppa.Api/appsettings.json) (mặc định `http://localhost:5500`). Đổi cổng khác thì phải sửa cả 2 nơi.

> **HTTPS**: lần đầu chạy `dotnet run`, nếu trình duyệt cảnh báo chứng chỉ không tin cậy khi gọi `https://localhost:5001`, chạy:
> ```bash
> dotnet dev-certs https --trust
> ```

## Tài khoản test

| Vai trò | Cách tạo | Thông tin |
|---|---|---|
| **Admin** | Đã seed sẵn | `admin@couppa.com` / `Admin@12345` |
| **User** | Tự đăng ký qua `register.html` | Email/password tự chọn (password ≥8 ký tự, có hoa + số) |
| **Guest** | Không cần tài khoản | Mở trực tiếp `index.html`, chưa đăng nhập |

## Chạy Unit Test + Integration Test

```bash
cd docs/test/src
dotnet test
```

Bao gồm:
- 8 file unit test (Service layer, EF Core InMemory) — [`Couppa.Api.Tests/Services/`](Couppa.Api.Tests/Services/)
- 1 file integration test (toàn bộ pipeline HTTP thật qua `WebApplicationFactory`, EF Core InMemory) — [`Couppa.Api.Tests/Integration/AuthCartFlowIntegrationTests.cs`](Couppa.Api.Tests/Integration/AuthCartFlowIntegrationTests.cs)

Xem [`TEST_COVERAGE_REPORT.md`](TEST_COVERAGE_REPORT.md) để biết mức độ bao phủ theo từng Functional Requirement.

## Kiểm thử thủ công

Xem [`MANUAL_TEST_CHECKLIST.md`](MANUAL_TEST_CHECKLIST.md) — checklist đầy đủ Permission Matrix (15 dòng x 3 role) và 9 Acceptance Criteria, dùng để tự kiểm tra trước khi demo/bảo vệ đồ án.

**Gợi ý trình tự demo** (đủ minh họa toàn bộ NFR-AVAIL-01 — chạy liên tục ≥ 30 phút không lỗi):
1. Guest: xem trang chủ → tìm kiếm/lọc sản phẩm → xem chi tiết → thêm vào giỏ hàng
2. Guest → đăng ký tài khoản mới → đăng nhập → xác nhận giỏ hàng Guest đã merge vào tài khoản (BR-13)
3. User: sửa profile, đổi mật khẩu, tiếp tục thao tác giỏ hàng (+/-, xóa item)
4. Đăng xuất → đăng nhập bằng tài khoản Admin (`admin@couppa.com`)
5. Admin: xem Dashboard → CRUD sản phẩm/danh mục → thử xóa danh mục còn sản phẩm (409, BR-03) → khóa/mở khóa user → thử tự khóa chính mình (409, BR-14) → xem báo cáo top sản phẩm thêm giỏ

## Biến cấu hình quan trọng

Toàn bộ ở [`Couppa.Api/appsettings.json`](Couppa.Api/appsettings.json):

| Key | Ý nghĩa |
|---|---|
| `ConnectionStrings:DefaultConnection` | Chuỗi kết nối SQL Server |
| `Cors:AllowedOrigin` | Origin duy nhất được phép gọi API kèm Cookie (frontend) |
| `Cache:*TtlMinutes` | TTL của 4 cache key (`categories:active`, `products:featured`, `products:latest`, `admin:dashboard:summary`) |
| `Cart:GuestCartExpiryDays` | Số ngày giữ giỏ hàng Guest trước khi dọn dẹp |

## Giới hạn đã biết (Known Limitations)

Ghi nhận trung thực để tránh hiểu nhầm khi demo/chấm điểm:

1. **Chưa chạy `dotnet build`/`dotnet test` thật** trong quá trình phát triển (môi trường viết code không có .NET SDK) — toàn bộ code đã được rà soát thủ công kỹ lưỡng nhưng **cần chạy `dotnet build` + `dotnet test` lần đầu trước khi demo** để xác nhận không có lỗi biên dịch.
2. Chưa có migration file commit sẵn trong repo — với chạy thủ công xem "Ghi chú migration" ở Bước 2; với Docker, migration `InitialCreate` được tự generate ở build time (xem [`Couppa.Api/Dockerfile`](Couppa.Api/Dockerfile)).
3. **Chưa chạy thử `docker compose up --build` thật** (môi trường viết cấu hình Docker này không có quyền truy cập Docker daemon — user không thuộc group `docker`) — Dockerfile/compose/Caddyfile đã được rà soát thủ công kỹ (bao gồm `docker compose config` để validate cú pháp) nhưng **cần chạy build thật lần đầu trước khi demo** để xác nhận không có lỗi runtime (vd. thời gian generate migration trong SDK image, TLS nội bộ của Caddy).
3. `GroupBy` trong `ReportService.GetCartTopProductsAsync` dùng cú pháp LINQ có thể cần điều chỉnh nếu EF Core InMemory (dùng trong test) không dịch được — xem comment trong [`ReportServiceTests.cs`](Couppa.Api.Tests/Services/ReportServiceTests.cs).
4. Một số FR (FR-BROWSE search/sort chi tiết, FR-CART-006 ClearCart, FR-REPORT-001..003 nếu khác "top sản phẩm giỏ hàng") chưa có unit test riêng — xem chi tiết ở [`TEST_COVERAGE_REPORT.md`](TEST_COVERAGE_REPORT.md).
5. Rate limiting (SEC-11) chỉ áp dụng cho `POST /api/auth/login` (5 lần/phút/IP), đúng scope SRS — không áp dụng cho endpoint khác.
6. Cấu hình Docker chỉ nhắm tới môi trường dev/demo cục bộ (giống giả định ở đầu file) — dùng mật khẩu `sa` mặc định, chứng chỉ TLS tự ký của Caddy, `MSSQL_PID=Express` (giới hạn tài nguyên của SQL Server Express), không phù hợp deploy production.
