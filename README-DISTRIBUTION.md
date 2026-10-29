# Chạy ứng dụng An Tâm trên Windows

## Máy người dùng

1. Cài SQL Server Express/Database Engine và bật SQL Server Authentication (Mixed Mode), đặt mật khẩu riêng. Tên instance có thể khác nhau giữa các máy.
2. Cài SQL Server Management Studio (SSMS) và kết nối tới instance để chạy script khởi tạo.
3. Giải nén gói này vào thư mục người dùng có quyền ghi, ví dụ `Documents\QlyPhongKham`.
4. Chạy `QlyPhongKham.exe`. Tại màn hình đăng nhập, bấm **⚙ Máy chủ**, dùng **Tìm cục bộ** để dò instance hoặc nhập đúng Server Name từ SSMS. Đặt tên database, tài khoản và mật khẩu SQL; bấm **Kiểm tra SQL Server** để xác thực server/tài khoản độc lập với database, rồi **Lưu cấu hình**. Nếu đăng nhập khi SQL chưa kết nối được, chọn **Yes** ở thông báo lỗi để mở thẳng màn cấu hình máy chủ.
5. Đăng nhập bằng tài khoản mẫu `admin` với mật khẩu `123456`, sau đó đổi mật khẩu mẫu trước khi sử dụng dữ liệu thật.

Ứng dụng đã đóng gói kèm .NET runtime; không cần cài Visual Studio, .NET SDK hoặc chạy lệnh `dotnet run`. Dữ liệu mẫu nằm trong `init_database.sql` và được nạp vào SQL Server khi chạy script; gói ZIP không chứa database `.mdf` hay dữ liệu đang chạy trên máy người đóng gói.

Mỗi máy trong hướng dẫn này có database riêng; dữ liệu không tự đồng bộ giữa các máy. Không dùng tài khoản `sa` trong môi trường có dữ liệu thật; hãy tạo tài khoản SQL riêng với quyền phù hợp.

## Lưu ý về dữ liệu

Trong SSMS, mở `init_database.sql`, bật **Query → SQLCMD Mode**, rồi chạy bằng **Execute/F5**. Tên mặc định trong script là `QL_PhongKham`; hãy đặt tên database trong cấu hình ứng dụng giống hệt tên ở dòng đầu `:setvar DatabaseName "QL_PhongKham"`. Nếu muốn dùng tên khác, chỉ sửa giá trị trong dấu ngoặc kép ở dòng `:setvar` (ví dụ `:setvar DatabaseName "QLPhongKham123"`) và nhập đúng tên đó trong ứng dụng. Script tạo database nếu chưa có và nạp dữ liệu mẫu. Script xóa và tạo lại các bảng nghiệp vụ, nên chỉ chạy trên database cài mới; chạy lại trên database đang sử dụng sẽ xóa dữ liệu hiện có.

Thông tin kết nối được lưu riêng trên máy tại `%LOCALAPPDATA%\QlyPhongKham\dbconfig.txt`. Không gửi hoặc commit file này vì nó có thể chứa mật khẩu SQL Server. Để đổi máy chủ hoặc thông tin đăng nhập, dùng nút **⚙ Máy chủ** trong ứng dụng.

## Tự đóng gói từ mã nguồn

Máy đóng gói cần Windows và .NET 9 SDK. Chạy `.\publish.ps1` từ PowerShell tại thư mục dự án. File ZIP được tạo tại `artifacts\QlyPhongKham-win-x64.zip`.
