# Repair Service Tracker

Ứng dụng Windows Forms C# cho bài thực hành quản lý đơn đăng ký dịch vụ sửa chữa.

## Môi trường
Project đặt mục tiêu `.NET 10 Windows Forms`, đồng bộ với môi trường .NET 10 mà bạn đã cho biết đang sử dụng. Visual Studio cần có workload **.NET desktop development** và .NET 10 SDK/targeting pack.

## Chạy
1. Giải nén ZIP.
2. Mở `RepairServiceTracker.csproj` trong Visual Studio.
3. Chọn **Build → Rebuild Solution**.
4. Nhấn F5.

## Chức năng
- Giao diện co giãn bằng TableLayoutPanel, Dock và Anchor.
- Model `RepairOrder`.
- DataGridView cột tự định nghĩa, `AutoGenerateColumns = false`, `FullRowSelect`.
- `BindingList<RepairOrder>` và `BindingSource`.
- Thêm, cập nhật, xóa có xác nhận.
- Tìm kiếm theo mã đơn, tên khách hàng, số điện thoại.
- Lọc theo trạng thái.
- OpenFileDialog chọn ảnh; PictureBox dùng `PictureBoxSizeMode.Zoom`.
- ErrorProvider kiểm tra mã đơn duy nhất, tên, số điện thoại, loại thiết bị, mô tả lỗi và chi phí không âm.
- Chi phí hiển thị theo `N0`.
- MenuStrip xuất danh sách sau lọc/tìm kiếm ra CSV bằng SaveFileDialog và thoát ứng dụng.
- StatusStrip đếm số đơn đang hiển thị và tổng chi phí dự kiến của các đơn chưa hoàn tất (không tính đơn Hoàn tất/Đã hủy).

Dữ liệu chỉ nằm trong bộ nhớ; đường dẫn ảnh được lưu trong phiên chạy, không sao chép ảnh vào thư mục ứng dụng. Khi đóng ứng dụng, dữ liệu mẫu và các thay đổi sẽ không được lưu lâu dài vì đề bài không yêu cầu cơ sở dữ liệu.
