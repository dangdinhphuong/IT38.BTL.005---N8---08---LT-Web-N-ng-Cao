# Hướng dẫn Build & Chạy dự án Couppa

Dự án gồm 3 phần:

| Thành phần | Công nghệ | Vị trí |
|---|---|---|
| Backend API | ASP.NET Core Web API (.NET 8) + Entity Framework Core | [`src/Couppa.Api/`](src/Couppa.Api/) |
| Frontend | HTML/CSS/JS tĩnh (không cần build) | [`html/`](html/) |
| Database | Microsoft SQL Server 2022 (chạy qua Docker) | container `mssql` |
| (Tùy chọn) Reverse proxy | Caddy (HTTPS tự ký) | [`src/Caddyfile`](src/Caddyfile) |

Có 3 cách chạy:
- **Cách A — Docker Compose toàn bộ**: chỉ cần cài Docker, không cần cài .NET SDK. Đơn giản nhất.
- **Cách B — Chạy thủ công, dùng Docker riêng cho SQL Server**: cài .NET SDK + Docker (chỉ dùng Docker cho SQL Server) + công cụ serve file tĩnh.
- **Cách C — Không dùng Docker chút nào**: cài SQL Server trực tiếp lên máy (native) + .NET SDK + công cụ serve file tĩnh. Dùng khi máy không cài được Docker (VD: máy công ty chặn, Windows Home không hỗ trợ WSL2/Hyper-V, v.v.).

---

## 1. Yêu cầu cài đặt theo hệ điều hành

> Mục 1.1/1.2 dưới đây liệt kê phần mềm cho **Cách A và B** (có Docker). Nếu bạn muốn chạy hoàn toàn **không cần Docker**, bỏ qua phần Docker và xem thẳng **mục 1.3 + mục 4 (Cách C)**.

### 1.1. Ubuntu (20.04/22.04/24.04)

**Bắt buộc cho cả 2 cách chạy:**

```bash
# Docker Engine + Docker Compose plugin
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Thêm user vào group docker để không cần sudo mỗi lần chạy docker
sudo usermod -aG docker $USER
newgrp docker   # hoặc logout/login lại
```

Kiểm tra:
```bash
docker --version
docker compose version
```

**Chỉ cần thêm nếu chạy Cách B (thủ công, không dùng Docker cho backend/frontend):**

```bash
# .NET 8 SDK
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0

dotnet --version   # kỳ vọng 8.0.x

# Công cụ EF Core CLI (để tạo/chạy migration)
dotnet tool install --global dotnet-ef --version 8.*
export PATH="$PATH:$HOME/.dotnet/tools"   # thêm dòng này vào ~/.bashrc để giữ vĩnh viễn

# Node.js (tùy chọn, chỉ nếu dùng http-server thay vì Live Server để serve frontend)
sudo apt-get install -y nodejs npm
```

Trình duyệt: Chrome/Edge/Firefox bản mới (thường có sẵn hoặc cài qua `apt`/Snap).

### 1.2. Windows 10/11

**Bắt buộc cho cả 2 cách chạy:**

- [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop/) — bật WSL2 backend khi cài đặt (Docker Desktop sẽ tự hướng dẫn bật WSL2 nếu chưa có).
- Sau khi cài, mở Docker Desktop và đợi trạng thái "Running".

Kiểm tra trong PowerShell:
```powershell
docker --version
docker compose version
```

**Chỉ cần thêm nếu chạy Cách B (thủ công):**

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) — tải bản "SDK x64" cho Windows, cài như phần mềm thông thường.
  ```powershell
  dotnet --version   # kỳ vọng 8.0.x
  ```
- EF Core CLI tool:
  ```powershell
  dotnet tool install --global dotnet-ef --version 8.*
  ```
- Visual Studio 2022 (tùy chọn, nếu muốn mở `Couppa.sln` bằng IDE thay vì CLI) — chọn workload **ASP.NET and web development**.
- [VSCode](https://code.visualstudio.com/) + extension **Live Server** (ritwickdey.LiveServer) — để serve frontend ở cổng 5500. Hoặc dùng Node.js + `http-server` nếu không dùng VSCode.
- Trình duyệt Chrome/Edge bản mới (Windows đã có sẵn Edge).

> **Lưu ý Windows**: nên chạy toàn bộ thao tác `dotnet ef`, `dotnet run` trong PowerShell hoặc Windows Terminal. Nếu dùng WSL2, có thể làm y hệt hướng dẫn Ubuntu ở trên bên trong WSL.

### 1.3. Không dùng Docker (SQL Server cài trực tiếp lên máy)

Áp dụng cho cả Ubuntu và Windows khi muốn tránh hoàn toàn Docker. Cần 4 thứ:

**a) .NET 8 SDK** — xem lệnh cài ở mục 1.1 (Ubuntu) hoặc 1.2 (Windows) phần "Chỉ cần thêm nếu chạy Cách B".

**b) `dotnet-ef` CLI tool** — xem lệnh cài ở mục 1.1/1.2, tương tự.

**c) Microsoft SQL Server cài trực tiếp (thay cho container `mssql`)**

- **Ubuntu**: cài **SQL Server 2022 Developer/Express Edition cho Linux** (bản Ubuntu chính thức của Microsoft, hỗ trợ 20.04/22.04):
  ```bash
  # Thêm repo Microsoft SQL Server 2022
  curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | sudo gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg
  curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/mssql-server-2022.list | sudo tee /etc/apt/sources.list.d/mssql-server-2022.list
  sudo apt-get update
  sudo apt-get install -y mssql-server

  # Cấu hình lần đầu (chọn Edition, đặt mật khẩu sa)
  sudo /opt/mssql/bin/mssql-conf setup
  # -> Chọn Edition: 2) Developer  (miễn phí, đủ tính năng cho dev/demo)
  # -> Nhập và xác nhận mật khẩu SA (đặt trùng "Couppa_dev_password1" để khỏi phải sửa appsettings.json)

  systemctl status mssql-server   # kiểm tra service đã "active (running)"
  ```
  Cài công cụ dòng lệnh `sqlcmd` (tùy chọn, để kiểm tra kết nối):
  ```bash
  curl -fsSL https://packages.microsoft.com/config/ubuntu/22.04/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list
  sudo apt-get update
  sudo ACCEPT_EULA=Y apt-get install -y mssql-tools18 unixodbc-dev
  echo 'export PATH="$PATH:/opt/mssql-tools18/bin"' >> ~/.bashrc && source ~/.bashrc

  sqlcmd -S localhost -U sa -P 'Couppa_dev_password1' -C -Q "SELECT @@VERSION"
  ```

- **Windows**: tải **SQL Server 2022 Express/Developer Edition** từ [microsoft.com/sql-server/sql-server-downloads](https://www.microsoft.com/sql-server/sql-server-downloads), chạy installer:
  - Chọn kiểu cài **Basic** (Express) hoặc **Custom** (Developer, đủ tính năng hơn, vẫn miễn phí).
  - Trong bước cấu hình instance, chọn **Mixed Mode Authentication**, đặt mật khẩu `sa` trùng `Couppa_dev_password1` (hoặc mật khẩu khác — nếu khác thì nhớ sửa `appsettings.json` ở bước cấu hình phía dưới).
  - Ghi nhớ tên instance (mặc định thường là `SQLEXPRESS` hoặc `MSSQLSERVER`).
  - Cài thêm **SQL Server Management Studio (SSMS)** (tùy chọn, để xem/quản lý database bằng giao diện): [aka.ms/ssmsfullsetup](https://aka.ms/ssmsfullsetup).
  - Mở **SQL Server Configuration Manager** → đảm bảo TCP/IP đã **Enabled** cho instance, cổng **1433** (mặc định instance `MSSQLSERVER` dùng cổng 1433; nếu dùng named instance `SQLEXPRESS`, cần bật SQL Server Browser service hoặc cấu hình cổng tĩnh 1433 thủ công).

**d) Công cụ serve frontend tĩnh** — VSCode + Live Server, hoặc Node.js + `http-server` (xem mục 1.1/1.2).

---

## 2. Cách A — Docker Compose (khuyến nghị, nhanh nhất)

Không cần cài .NET SDK hay Live Server — chỉ cần Docker đã cài ở mục 1.

```bash
cd src
docker compose up -d --build
```

Compose dựng 4 container:

| Service | Vai trò | Truy cập từ host |
|---|---|---|
| `mssql` | SQL Server 2022 | `localhost:1433` |
| `api` | Backend ASP.NET Core | nội bộ (qua Caddy) |
| `web` | Frontend Nginx | nội bộ (qua Caddy) |
| `caddy` | Reverse proxy HTTPS tự ký | `https://localhost:5001` (API), `https://localhost:5500` (Frontend) |

Mở trình duyệt: `https://localhost:5500/index.html` — chấp nhận cảnh báo chứng chỉ tự ký lần đầu (do Caddy `tls internal` phát hành nội bộ, không phải CA công khai).

Container `api` tự động:
- Generate migration `InitialCreate` khi build image (nếu chưa có sẵn) và áp dụng khi khởi động.
- Seed dữ liệu mẫu (role, tài khoản Admin, danh mục + sản phẩm demo) vì chạy với `ASPNETCORE_ENVIRONMENT=Docker`.

Xem log / trạng thái:
```bash
docker compose logs -f api
docker compose ps
```

Dừng và xoá toàn bộ (kể cả dữ liệu SQL Server):
```bash
docker compose down -v
```

---

## 3. Cách B — Chạy thủ công từng phần

Vẫn cần Docker chỉ để chạy riêng SQL Server.

### Bước 1 — Khởi động SQL Server

```bash
cd src
docker compose up -d mssql
docker compose ps   # đợi tới khi trạng thái "healthy"
```

Thông tin kết nối (khớp `Couppa.Api/appsettings.json`):

| | |
|---|---|
| Server | `localhost,1433` |
| Database | `couppa` (tự tạo khi migration chạy lần đầu) |
| User Id | `sa` |
| Password | `Couppa_dev_password1` |

### Bước 2 — Chạy Backend (Web API)

```bash
cd src/Couppa.Api
dotnet restore
dotnet ef migrations add InitialCreate   # chỉ chạy lần đầu, nếu thư mục Data/Migrations chưa có gì
dotnet ef database update
dotnet run
```

Backend lắng nghe ở:
- HTTPS: `https://localhost:5001`
- HTTP: `http://localhost:5000`

Lần chạy đầu (môi trường `Development` mặc định) tự seed:
- 2 role (`User`, `Admin`)
- 1 tài khoản Admin: `admin@couppa.com` / `Admin@12345`
- 5 danh mục + ~25 sản phẩm demo

**Windows**: thay `dotnet ef migrations add ...` / `dotnet run` chạy y hệt trong PowerShell (cùng thư mục `src\Couppa.Api`).

**Chứng chỉ HTTPS local**: nếu trình duyệt cảnh báo không tin cậy khi gọi `https://localhost:5001`:
```bash
dotnet dev-certs https --trust
```
(Windows: chạy lệnh này trong PowerShell, sẽ có popup xác nhận cài chứng chỉ.)

### Bước 3 — Chạy Frontend

Frontend ở [`html/`](html/), không cần build. Serve ở cổng **5500** (bắt buộc khớp cổng để CORS hoạt động):

```bash
cd html
# Cách 1 — VSCode: chuột phải index.html -> "Open with Live Server" (cấu hình cổng 5500 trong settings.json nếu cần)
# Cách 2 — dùng http-server (cần Node.js):
npx http-server -p 5500
```

Mở trình duyệt: `http://localhost:5500/index.html`

> **Quan trọng**: cổng frontend phải khớp `Cors:AllowedOrigin` trong [`src/Couppa.Api/appsettings.json`](src/Couppa.Api/appsettings.json) (mặc định `http://localhost:5500`). Đổi cổng khác thì phải sửa cả 2 nơi.

---

## 4. Cách C — Không dùng Docker chút nào

Dùng khi máy không cài/chạy được Docker. Yêu cầu đã cài SQL Server native + .NET SDK + dotnet-ef theo mục 1.3.

### Bước 1 — Khởi động SQL Server

- **Ubuntu**: service `mssql-server` tự chạy nền sau khi cài (`systemctl status mssql-server` để kiểm tra; `sudo systemctl start mssql-server` nếu chưa chạy).
- **Windows**: service **SQL Server (MSSQLSERVER)** hoặc **SQL Server (SQLEXPRESS)** tự chạy nền sau khi cài. Kiểm tra qua **Services** (services.msc) hoặc **SQL Server Configuration Manager** — trạng thái phải là "Running". Nếu dùng named instance (`SQLEXPRESS`), cũng bật service **SQL Server Browser**.

### Bước 2 — Cấu hình connection string

Vì không có container `mssql` (tên host `mssql` không tồn tại), cần trỏ connection string về `localhost` với đúng instance/cổng đã cài ở mục 1.3.

Mở [`src/Couppa.Api/appsettings.json`](src/Couppa.Api/appsettings.json):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=couppa;User Id=sa;Password=Couppa_dev_password1;TrustServerCertificate=True"
}
```

- Nếu đặt mật khẩu `sa` khác lúc cài SQL Server, sửa `Password=...` cho khớp.
- Nếu Windows dùng named instance `SQLEXPRESS` (không có cổng tĩnh 1433), đổi `Server` thành `Server=localhost\SQLEXPRESS;...` (bỏ `,1433`).
- File này (`appsettings.json`, không phải `appsettings.Docker.json`) được dùng khi chạy `dotnet run` với `ASPNETCORE_ENVIRONMENT=Development` (mặc định) — đúng cho cả Ubuntu lẫn Windows ở Cách C.

### Bước 3 — Chạy Backend (Web API)

```bash
cd src/Couppa.Api
dotnet restore
dotnet ef migrations add InitialCreate   # chỉ chạy lần đầu, nếu Data/Migrations chưa có gì
dotnet ef database update                # tạo database "couppa" + toàn bộ bảng trên SQL Server vừa cài
dotnet run
```

(Windows: chạy y hệt trong PowerShell tại `src\Couppa.Api`.)

Backend lắng nghe ở `https://localhost:5001` và `http://localhost:5000`, tự seed dữ liệu mẫu như mô tả ở mục 3 Bước 2.

Nếu trình duyệt cảnh báo chứng chỉ HTTPS không tin cậy: `dotnet dev-certs https --trust`.

### Bước 4 — Chạy Frontend

Giống hệt mục 3 Bước 3: serve thư mục [`html/`](html/) ở cổng **5500** bằng Live Server hoặc `npx http-server -p 5500`.

Mở trình duyệt: `http://localhost:5500/index.html`

---

## 5. Chạy Unit Test + Integration Test

```bash
cd src
dotnet test
```

Bao gồm:
- Unit test (Service layer, EF Core InMemory) — [`src/Couppa.Api.Tests/Services/`](src/Couppa.Api.Tests/Services/)
- Integration test (HTTP pipeline thật qua `WebApplicationFactory`) — [`src/Couppa.Api.Tests/Integration/AuthCartFlowIntegrationTests.cs`](src/Couppa.Api.Tests/Integration/AuthCartFlowIntegrationTests.cs)

Không cần SQL Server chạy trước — test dùng EF Core InMemory.

---

## 6. Tài khoản test

| Vai trò | Cách tạo | Thông tin |
|---|---|---|
| Admin | Đã seed sẵn | `admin@couppa.com` / `Admin@12345` |
| User | Tự đăng ký qua `register.html` | Email/password tự chọn (password ≥8 ký tự, có hoa + số) |
| Guest | Không cần tài khoản | Mở trực tiếp `index.html` |

---

## 7. Bảng tổng hợp cổng (port)

| Cổng | Dùng cho |
|---|---|
| 1433 | SQL Server |
| 5000 | Backend HTTP (chỉ khi chạy `dotnet run` thủ công) |
| 5001 | Backend HTTPS (chạy thủ công) hoặc Caddy → API (Docker Compose) |
| 5500 | Frontend (Live Server/http-server thủ công, hoặc Caddy → Web trong Docker Compose) |

---

## 8. Sự cố thường gặp

| Vấn đề | Nguyên nhân / cách xử lý |
|---|---|
| `docker: permission denied` (Ubuntu) | User chưa thuộc group `docker` → `sudo usermod -aG docker $USER` rồi logout/login lại |
| Trình duyệt cảnh báo "Not Secure" khi vào `https://localhost:5001` hoặc `:5500` | Chứng chỉ tự ký (dev cert hoặc Caddy `tls internal`) — chấp nhận cảnh báo, hoặc chạy `dotnet dev-certs https --trust` (chạy thủ công) |
| Lỗi CORS khi frontend gọi API | Cổng frontend không khớp `Cors:AllowedOrigin` trong `appsettings.json` — sửa lại cho khớp 5500 |
| `dotnet ef` báo "command not found" | Chưa cài EF Core CLI tool: `dotnet tool install --global dotnet-ef --version 8.*`, và đảm bảo `$HOME/.dotnet/tools` (Linux) hoặc `%USERPROFILE%\.dotnet\tools` (Windows) có trong PATH |
| Container `mssql` không lên `healthy` | Thiếu RAM (SQL Server cần tối thiểu ~2GB) — tăng RAM cấp cho Docker Desktop (Windows) hoặc kiểm tra `docker compose logs mssql` |
| Port 1433/5000/5001/5500 đã bị chiếm | Dừng tiến trình đang dùng cổng đó, hoặc đổi cổng trong `docker-compose.yml`/`launchSettings.json`/`appsettings.json` (nhớ đổi đồng bộ) |
| (Cách C - Ubuntu) `sqlcmd`/`dotnet ef database update` báo "Login failed for user 'sa'" | Sai mật khẩu đã đặt lúc `mssql-conf setup`, hoặc mật khẩu không đạt độ phức tạp (SQL Server yêu cầu ≥8 ký tự, có hoa/thường/số/ký tự đặc biệt) — chạy lại `sudo /opt/mssql/bin/mssql-conf setup` để đặt lại |
| (Cách C - Ubuntu) service `mssql-server` không start | Kiểm tra `sudo journalctl -u mssql-server --no-pager \| tail -50`; thường do thiếu RAM (tối thiểu 2GB) hoặc chưa chạy `mssql-conf setup` |
| (Cách C - Windows) Không kết nối được `localhost,1433` | Nếu cài instance mặc định `MSSQLSERVER` nhưng TCP/IP chưa Enable trong SQL Server Configuration Manager → bật lên rồi restart service. Nếu dùng `SQLEXPRESS`, đổi connection string sang `localhost\SQLEXPRESS` (bỏ cổng) và đảm bảo service **SQL Server Browser** đang chạy |
| (Cách C) `dotnet ef database update` báo "Cannot open database couppa" lần đầu | Bình thường — EF Core sẽ tự tạo database `couppa` trong quá trình chạy migration, không cần tạo tay trước |
