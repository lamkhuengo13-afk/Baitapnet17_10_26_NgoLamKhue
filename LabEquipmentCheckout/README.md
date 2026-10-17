# Lab Equipment Checkout

Ứng dụng Windows Forms C# cho bài thực hành quản lý mượn trả thiết bị.

## Môi trường
Project được đặt mục tiêu `.NET 10 Windows Forms`, theo môi trường mà bạn cho biết đang dùng. Visual Studio cần có workload **.NET desktop development** và .NET 10 SDK/targeting pack tương ứng.

## Chạy
1. Giải nén ZIP.
2. Mở `LabEquipmentCheckout.csproj` trong Visual Studio.
3. Chọn **Build → Rebuild Solution**.
4. Nhấn F5.

## Các chức năng
- Giao diện co giãn bằng TableLayoutPanel, Dock và Anchor.
- Model `EquipmentLoan`.
- DataGridView cấu hình cột thủ công, `AutoGenerateColumns = false`, `FullRowSelect`.
- `BindingList<EquipmentLoan>` và `BindingSource`.
- Thêm, cập nhật, xóa có xác nhận.
- Tìm theo mã sinh viên hoặc tên thiết bị.
- Lọc theo Đang mượn, Quá hạn, Đã trả.
- Kiểm tra mã phiếu duy nhất, thông tin sinh viên, thiết bị, số lượng, hạn trả và ngày trả.
- Tự tính trạng thái: có ngày trả => Đã trả; chưa trả và hạn trả trước hôm nay => Quá hạn; còn lại => Đang mượn.
- Nút Đánh dấu đã trả tự ghi ngày hiện tại.
- StatusStrip đếm tổng phiếu và số phiếu quá hạn trên toàn bộ dữ liệu.
- Xuất danh sách sau lọc/tìm kiếm ra CSV UTF-8.

Dữ liệu chỉ nằm trong bộ nhớ và sẽ mất khi đóng ứng dụng; đề bài không yêu cầu cơ sở dữ liệu.
