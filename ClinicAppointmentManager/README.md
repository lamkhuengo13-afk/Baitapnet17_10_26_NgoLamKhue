# Clinic Appointment Manager

Ứng dụng Windows Forms C# (.NET 8) cho bài thực hành Quản lý lịch hẹn phòng khám.

## Cách mở và chạy
1. Cài Visual Studio 2022 có workload **.NET desktop development**.
2. Giải nén thư mục.
3. Mở `ClinicAppointmentManager.csproj`.
4. Nhấn F5 để chạy.

Nếu máy không có .NET 8 SDK, cài .NET 8 SDK hoặc đổi TargetFramework theo framework mà giảng viên yêu cầu.

## File
- `Program.cs`: điểm khởi chạy.
- `Models/Appointment.cs`: model dữ liệu.
- `MainForm.cs`: giao diện, binding, nghiệp vụ, validation, tìm kiếm/lọc, xuất CSV.

## Đã triển khai
- TableLayoutPanel và bố cục co giãn.
- DataGridView cột tự cấu hình, AutoGenerateColumns=false, FullRowSelect.
- BindingList<Appointment> và BindingSource.
- Thêm, sửa, xóa có xác nhận.
- Tìm theo tên hoặc điện thoại; lọc theo ngày và trạng thái.
- ErrorProvider; mã lịch duy nhất; số điện thoại 10 chữ số bắt đầu bằng 0; không cho lưu ngày giờ quá khứ.
- Xuất CSV danh sách đang hiển thị bằng SaveFileDialog.
- StatusStrip đếm số lịch hẹn đang hiển thị.

## Lưu ý
Dữ liệu chỉ lưu trong bộ nhớ, nên sẽ mất khi đóng ứng dụng. Đây là phạm vi phù hợp nếu đề không yêu cầu cơ sở dữ liệu.
