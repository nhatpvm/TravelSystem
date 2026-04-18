# Hệ thống TicketBooking

TicketBooking là hệ thống đặt vé trực tuyến đa dịch vụ, hỗ trợ đặt vé xe khách, vé tàu hỏa, vé máy bay, tour du lịch và khách sạn trên cùng một nền tảng thống nhất.

Dự án được xây dựng theo định hướng hệ thống thực tế, tập trung vào khả năng mở rộng, bảo trì, phân quyền người dùng, quản lý dữ liệu theo từng đơn vị cung cấp dịch vụ, xử lý đặt vé, thanh toán và phát hành vé điện tử.

## Tổng quan dự án

TicketBooking cung cấp đầy đủ quy trình đặt vé cho khách hàng, bao gồm tìm kiếm dịch vụ, xem chi tiết chuyến đi, chọn ghế hoặc chọn phòng, nhập thông tin hành khách, tạo đơn đặt vé, thanh toán và nhận vé điện tử kèm mã QR.

Bên cạnh chức năng dành cho khách hàng, hệ thống còn hỗ trợ các trang quản lý dành cho quản trị viên và các đơn vị cung cấp dịch vụ như nhà xe, đơn vị vận tải, hãng bay, khách sạn và công ty tour. Mỗi đơn vị có thể quản lý dữ liệu riêng thông qua kiến trúc multi-tenant.

## Chức năng chính

- Đăng ký, đăng nhập và xác thực người dùng bằng JWT
- Phân quyền theo vai trò người dùng
- Kiến trúc multi-tenant cho nhiều đơn vị cung cấp dịch vụ
- Quản lý và đặt vé xe, tàu hỏa, máy bay, tour du lịch và khách sạn
- Tìm kiếm và lọc dịch vụ theo tuyến đường, ngày đi, giá, nhà cung cấp và tình trạng còn chỗ
- Chọn ghế và giữ chỗ tạm thời trong thời gian giới hạn
- Quản lý đơn đặt vé theo mô hình snapshot-first
- Xử lý thanh toán và cập nhật trạng thái giao dịch
- Phát hành vé điện tử kèm mã QR
- Theo dõi lịch sử đặt vé của khách hàng
- Trang quản trị dành cho Admin và các đơn vị quản lý dịch vụ
- Quản lý tin tức/CMS hỗ trợ SEO
- Ghi nhận nhật ký hệ thống và lịch sử thao tác

## Công nghệ sử dụng

### Backend
- ASP.NET Core Web API (.NET 8)
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- Swagger / OpenAPI
- RESTful API có versioning

### Frontend
- Vue 3
- Vite
- TypeScript
- Tailwind CSS
- PrimeVue

### Database
- SQL Server
- EF Core Code First Migration
- Thiết kế database nhiều schema
- Dữ liệu nghiệp vụ theo tenant
- Soft delete và audit log

## Kiến trúc hệ thống

Hệ thống được thiết kế theo hướng module hóa, tách riêng từng nhóm nghiệp vụ như xác thực, phân quyền, tenant, danh mục, phương tiện, xe khách, tàu hỏa, máy bay, tour, khách sạn, booking, thanh toán, vé điện tử, CMS và vận hành hệ thống.

Trong module đặt vé, hệ thống áp dụng nguyên tắc snapshot-first để lưu lại thông tin giá, lịch trình, chính sách và hành khách tại thời điểm đặt vé. Điều này giúp đảm bảo dữ liệu booking không bị sai lệch khi thông tin gốc thay đổi sau khi khách hàng đã đặt vé.

## Mục tiêu dự án

Mục tiêu của TicketBooking là xây dựng một hệ thống đặt vé và dịch vụ du lịch trực tuyến có tính thực tế cao, mô phỏng đầy đủ các nghiệp vụ quan trọng trong quá trình tìm kiếm, đặt chỗ, thanh toán và quản lý vé.

Dự án không chỉ phục vụ mục đích học tập và đồ án tốt nghiệp, mà còn hướng đến việc xây dựng nền tảng có khả năng mở rộng, dễ bảo trì và có thể phát triển thành một hệ thống thương mại thực tế trong tương lai.
