# Hệ thống bán vé xe - tàu hỏa - vé máy bay - tour du lịch

Đây là hệ thống đặt vé và quản trị dịch vụ du lịch trực tuyến, hỗ trợ nhiều nhóm nghiệp vụ trên cùng một nền tảng: vé xe, vé tàu hỏa, vé máy bay, tour du lịch, khách sạn, booking, thanh toán, phát hành vé điện tử và quản trị nội dung.

Dự án được xây dựng theo định hướng đồ án tốt nghiệp có tính thực tế, tập trung vào kiến trúc phân lớp, quản lý dữ liệu theo tenant, phân quyền theo vai trò, quy trình đặt vé end-to-end và giao diện quản trị cho nhiều nhóm người dùng.

## Mục Tiêu Dự Án

- Mô phỏng một nền tảng bán vé và dịch vụ du lịch đa nghiệp vụ.
- Hỗ trợ khách hàng tìm kiếm, đặt chỗ, thanh toán và nhận vé điện tử.
- Hỗ trợ quản trị viên vận hành toàn hệ thống.
- Hỗ trợ các đơn vị cung cấp dịch vụ quản lý dữ liệu riêng theo tenant.
- Tổ chức source code rõ ràng, dễ mở rộng và phù hợp với quy trình phát triển thực tế.

## Chức Năng Chính

### Khách Hàng

- Đăng ký, đăng nhập và quản lý hồ sơ cá nhân.
- Tìm kiếm chuyến xe, tàu hỏa, chuyến bay, tour và khách sạn.
- Xem chi tiết dịch vụ, lịch trình, giá và tình trạng còn chỗ.
- Chọn ghế, giữ chỗ và nhập thông tin hành khách.
- Tạo booking, theo dõi trạng thái thanh toán và lịch sử đặt vé.
- Nhận vé điện tử kèm mã QR.

### Quản Trị Hệ Thống

- Quản lý tenant và quy trình onboarding đối tác.
- Quản lý người dùng, vai trò và phân quyền.
- Quản lý danh mục dùng chung: địa điểm, nhà cung cấp, phương tiện, sơ đồ ghế.
- Quản lý booking, thanh toán, hoàn tiền, đối soát và hỗ trợ khách hàng.
- Quản lý CMS/tin tức phục vụ SEO.
- Theo dõi audit log, outbox, notification và dữ liệu vận hành.

### Quản Lý Nhà Xe

- Quản lý nhà xe, xe khai thác và cấu hình chi tiết xe.
- Quản lý sơ đồ ghế, danh sách ghế và trạng thái ghế.
- Quản lý điểm đón/trả, tuyến đường, lịch dừng và giá chặng.
- Quản lý chuyến xe, tồn kho ghế, giữ chỗ và tình trạng vận hành.

### Các Module Nghiệp Vụ Khác

- Train: quản lý tàu, toa, ghế, hành trình và giá chặng.
- Flight: quản lý chuyến bay, cabin, sơ đồ ghế và tồn kho.
- Tour: quản lý tour, lịch khởi hành, giá và nội dung giới thiệu.
- Hotel: quản lý khách sạn, loại phòng, hình ảnh, giá và tồn kho.

## Công Nghệ Sử Dụng

### Backend

- .NET 8 Web API
- Entity Framework Core Code First
- SQL Server
- ASP.NET Core Identity
- JWT Bearer Authentication
- Role-based Authorization
- Swagger / OpenAPI
- API Versioning
- Serilog
- Hangfire

### Frontend

- React
- Vite
- TypeScript toolchain
- Tailwind CSS
- React Router
- Lucide React Icons
- Framer Motion

### Database

- SQL Server
- EF Core Migrations
- Multi-schema database design
- Soft delete
- Audit fields
- Tenant-based data isolation

## Kiến Trúc Source Code

```text
TicketBooking.V3
├── TicketBooking.Api             # ASP.NET Core Web API, controllers, middleware, Swagger
├── TicketBooking.Application     # Application layer, contracts, services, DTOs
├── TicketBooking.Domain          # Domain entities, enums, business models
├── TicketBooking.Infrastructure  # EF Core DbContext, migrations, identity, persistence
├── TicketBooking.Tests           # Automated tests
├── Travel/frontend               # Frontend React + Vite
└── TicketBooking.V3.slnx         # Solution file
```

## Kiến Trúc Backend

Backend được tổ chức theo hướng phân lớp:

- `Domain`: chứa entity, enum và các mô hình nghiệp vụ cốt lõi.
- `Application`: chứa contract, DTO và service cấp ứng dụng.
- `Infrastructure`: chứa `AppDbContext`, EF Core configuration, migration, identity và persistence.
- `Api`: chứa controller, middleware, cấu hình authentication, authorization, Swagger và dependency injection.

Các nhóm API chính gồm:

- Authentication và user profile
- Tenant onboarding
- Admin management
- Bus, train, flight, tour, hotel
- Booking
- Payment
- Ticketing QR
- CMS/SEO
- Support, settlement, notification và audit

## Kiến Trúc Frontend

Frontend nằm trong `Travel/frontend`, được xây dựng bằng React và Vite. Source được chia theo module:

```text
Travel/frontend/src
├── app                # Router chính
├── modules            # Các module nghiệp vụ theo domain
├── services           # API clients
├── shared             # Layout, component dùng chung
└── assets             # Static assets
```

Các nhóm giao diện chính:

- Public site: tìm kiếm, chi tiết dịch vụ, booking flow.
- User portal: hồ sơ cá nhân, lịch sử đặt vé, thanh toán, thông báo.
- Admin portal: quản trị toàn hệ thống.
- Tenant portal: quản lý vận hành theo từng đơn vị cung cấp dịch vụ.

## Vai Trò Người Dùng

- `Admin`: quản trị toàn hệ thống.
- `QLNX`: quản lý nhà xe.
- `QLVT`: quản lý vận tải/tàu.
- `QLVMM`: quản lý vé máy bay.
- `QLKS`: quản lý khách sạn.
- `Customer`: khách hàng đặt vé.

## Cài Đặt Và Chạy Dự Án

### Yêu Cầu Môi Trường

- .NET SDK 8
- SQL Server
- Node.js
- npm

### Backend

```powershell
dotnet restore TicketBooking.V3.slnx
dotnet build TicketBooking.V3.slnx
dotnet run --project TicketBooking.Api
```

Swagger thường có thể truy cập tại:

```text
https://localhost:<port>/swagger
```

Tùy môi trường chạy, port thực tế được cấu hình bởi ASP.NET Core launch settings hoặc terminal output.

### Frontend

```powershell
cd Travel/frontend
npm install
npm run dev
```

Build production frontend:

```powershell
npm run build
```

## Database

Dự án dùng EF Core Code First với SQL Server. Connection string được cấu hình trong:

```text
TicketBooking.Api/appsettings.json
TicketBooking.Api/appsettings.Development.json
```

Khi triển khai môi trường thật, không nên commit thông tin nhạy cảm như connection string production, JWT secret, API key hoặc webhook secret.

Các lệnh EF Core thường dùng:

```powershell
dotnet ef database update --project TicketBooking.Infrastructure --startup-project TicketBooking.Api
```

Không nên tự tạo migration mới nếu chưa rà soát entity, configuration và migration hiện có.

## Build Và Kiểm Tra

Backend:

```powershell
dotnet build TicketBooking.V3.slnx --no-restore
dotnet test TicketBooking.V3.slnx --no-build
```

Frontend:

```powershell
cd Travel/frontend
npm run build
npm run lint
```

## Quy Ước Làm Việc

- Không commit `bin`, `obj`, `node_modules`, `dist`, `.vs` hoặc file log tạm.
- Không commit file chứa secret thật.
- Không tự ý thay đổi layout giao diện nếu task không yêu cầu.
- Khi bổ sung frontend, giữ đúng cấu trúc layout và phong cách UI hiện có.
- Khi thay đổi backend, cần kiểm tra tenant scope, role authorization và query filter.
- Với dữ liệu demo, nên seed hoặc tài liệu hóa rõ ràng thay vì để lẫn trong upload/runtime output.

## Trạng Thái Dự Án

Dự án đang ở giai đoạn hoàn thiện chức năng phục vụ bảo vệ đồ án. Các phần cốt lõi đã có nền tảng backend, frontend và database cho nhiều module chính. Một số khu vực vẫn cần tiếp tục rà soát, kiểm thử end-to-end và dọn dẹp repository trước khi public hoặc bàn giao.

Các nhóm nên ưu tiên trước khi nộp/bảo vệ:

- Dọn `.gitignore` và loại bỏ file build/cache khỏi Git.
- Kiểm tra lại dữ liệu demo end-to-end.
- Chạy build backend/frontend trước khi commit.
- Rà soát quyền truy cập theo role và tenant.
- Kiểm tra lại các flow booking, payment, ticketing QR và refund.

## Ghi Chú

Dự án phục vụ mục đích học tập và đồ án tốt nghiệp. Nếu triển khai thực tế, cần bổ sung kiểm thử bảo mật, kiểm thử tải, logging/monitoring production, quản lý secret, backup database và quy trình CI/CD hoàn chỉnh.
