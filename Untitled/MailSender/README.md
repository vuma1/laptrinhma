# Mail Sender - Ứng dụng gửi email qua SMTP (WPF C#)

Dự án giao diện ứng dụng gửi email bằng C# WPF được thiết kế theo phong cách hiện đại (Modern Minimalist / Fluent UI), tái hiện toàn bộ các màn hình, trạng thái và hộp thoại trong thiết kế gốc.

## Cấu trúc thư mục dự án

- **`MainWindow.xaml`**: Khung ứng dụng chính với Custom Title Bar, thanh điều hướng bên trái (Sidebar), thông tin hồ sơ tài khoản và thanh trạng thái dưới cùng (Status Bar).
- **`Views/`**:
  - `ComposeView.xaml`: Giao diện **Soạn thư** với đầy đủ các trường (Từ, Đến với chip người nhận, Chủ đề, thanh định dạng công cụ, nội dung thư, danh sách đính kèm tệp và thanh dung lượng lưu trữ).
  - `BulkSendView.xaml`: Giao diện **Gửi hàng loạt** hỗ trợ cả 2 bước:
    - *Bước 1 / 2 · Kiểm tra*: Thông tin tệp CSV, cảnh báo kiểm tra, bảng danh sách người nhận (Hợp lệ, Trùng địa chỉ, Sai định dạng), cấu hình gửi riêng / Bcc và khung xem trước thư cá nhân hóa.
    - *Bước 2 / 2 · Đang gửi*: Thẻ tiến trình lớn (50%), bảng tiến trình từng người gửi, nút *Tạm dừng* / *Tiếp tục* và *Hủy lượt gửi*.
  - `ContactsView.xaml`: Giao diện **Danh bạ** hỗ trợ danh bạ rỗng hoặc dữ liệu được thêm thủ công/nhập từ CSV.
  - `HistoryView.xaml`: Giao diện **Lịch sử gửi** hiển thị các lần gửi và lịch hẹn phát sinh trong quá trình sử dụng.
  - `SettingsView.xaml`: Giao diện **Cài đặt SMTP** (thẻ cấu hình nhanh Gmail, Outlook, LAN nội bộ, thông tin máy chủ, cổng, bảo mật STARTTLS, tài khoản, mật khẩu ứng dụng có nút ẩn/hiện, nút test kết nối và tài liệu hướng dẫn an toàn).
- **`Views/Dialogs/`**: Các hộp thoại nổi (Modal Dialogs):
  - `ScheduleDialog.xaml`: Hộp thoại hẹn giờ gửi thư.
  - `ContactEditDialog.xaml`: Hộp thoại thêm / sửa liên hệ.
  - `ErrorDetailDialog.xaml`: Hộp thoại chi tiết lỗi SMTP (mã 550 5.7.1, giải pháp khắc phục).
  - `NotificationDialog.xaml`: 4 hộp thoại thông báo ngữ cảnh:
    1. Gửi thành công (Thư đã được gửi).
    2. Lỗi mạng (Chưa thể kết nối mạng).
    3. Sai mật khẩu (Không thể đăng nhập SMTP).
    4. Tệp vượt giới hạn (Tệp đính kèm quá lớn 25 MB).
- **`Styles/`**:
  - `Colors.xaml`: Bảng màu chuẩn.
  - `Icons.xaml`: Toàn bộ icon dạng Vector Path Geometry sắc nét.
  - `Controls.xaml`: ControlTemplate hiện đại cho Buttons, TextBoxes, RadioButtons, CheckBoxes, ProgressBars, DropShadow.
- **`ViewModels/` & `Models/`**: Kiến trúc MVVM hoàn chỉnh, khởi động không có dữ liệu dựng sẵn và các lệnh điều hướng/mở hộp thoại.

## Cách chạy dự án

Từ thư mục `Untitled\MailSender`:
```bash
dotnet run
```
Hoặc mở file `MailSender.csproj` trong Visual Studio 2022+ / JetBrains Rider và nhấn **F5**.
