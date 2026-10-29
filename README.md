# HỆ THỐNG QUẢN LÝ PHÒNG KHÁM TƯ NHÂN (C# WinForms - SQL Server)

## 1. Thông tin CSDL & Tài khoản truy cập
- **Tên CSDL mặc định trong ứng dụng:** `QL_PhongKham`
- **Máy chủ mặc định:** SQL Server Express cài trên máy (`.\SQLEXPRESS`)
- **Thông tin đăng nhập SQL Server:** cấu hình riêng trên từng máy tại nút **⚙ Máy chủ**; ứng dụng không đặt sẵn mật khẩu.
- **File script tạo CSDL:** `init_database.sql`

### Danh sách tài khoản đăng nhập mẫu vào phần mềm:
| Vai trò | Tên đăng nhập | Mật khẩu | Quyền hạn & Chức năng |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin` | `123456` | Toàn quyền tất cả các phân hệ, quản trị nhân sự |
| **Bác sĩ khám** | `bacsi_nam` | `123456` | Xem hàng chờ theo STT, chẩn đoán, kê đơn thuốc |
| **Lễ tân (kiêm thu ngân)** | `letan` hoặc `thungan` | `123456` | Tiếp đón, quản lý bệnh nhân, tính chi phí và xuất hóa đơn |
| **Bệnh nhân** | `benhnhan_mai` | `123456` | Xem hồ sơ, lịch sử khám, dịch vụ, đơn thuốc và hóa đơn của chính mình |

Tại màn hình đăng nhập, chọn **Đăng ký tài khoản bệnh nhân** để tạo tài khoản cổng bệnh nhân mới. Đăng ký tạo hồ sơ bệnh nhân và tài khoản role Bệnh nhân trong cùng một giao dịch; tên đăng nhập phải chưa được tài khoản nhân viên hoặc bệnh nhân khác sử dụng.

---

## 2. Hướng dẫn cấu hình kết nối từ máy Laptop khác (Mạng LAN / Wi-Fi)

Khi làm đồ án nhóm, một máy tính sẽ làm **Máy chủ CSDL (Host)** chứa SQL Server, các máy laptop của các bạn khác trong nhóm (**Client**) sẽ kết nối vào.

### Bước 1: Cấu hình trên MÁY CHỦ (Máy chạy SQL Server)
1. **Bật TCP/IP trong SQL Server Configuration Manager:**
   - Mở `SQL Server Configuration Manager`.
   - Vào `SQL Server Network Configuration` -> `Protocols for MSSQLSERVER`.
   - Nhấp đúp vào `TCP/IP` -> Chọn **Enabled: Yes**.
   - Sang tab `IP Addresses` -> Cuộn xuống mục `IPAll` -> Đặt `TCP Port` là **`1433`**.
   - Khởi động lại dịch vụ `SQL Server (MSSQLSERVER)`.
2. **Mở cổng 1433 trên Windows Firewall (Tường lửa):**
   - Mở PowerShell (Run as Administrator) và chạy lệnh:
     ```powershell
     New-NetFirewallRule -DisplayName "SQL Server Port 1433" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow
     ```
3. **Xem địa chỉ IPv4 của máy chủ:**
   - Mở CMD / PowerShell gõ `ipconfig` -> Tìm dòng `IPv4 Address` (Ví dụ: `192.168.1.15`).

### Bước 2: Kết nối từ các MÁY LAPTOP KHÁC (Client)
1. Copy thư mục `QlyPhongKham` sang laptop client.
2. Mở phần mềm -> Tại màn hình Đăng nhập, bấm nút **⚙ Máy chủ**.
3. Điền các thông tin:
   - **Máy chủ (IP / Host):** Nhập IP của máy chủ (VD: `192.168.1.15`).
   - **Tên CSDL:** `QL_PhongKham`
   - **Tài khoản / mật khẩu:** dùng thông tin SQL Server đã cấu hình trên máy chủ.
4. Bấm **"Kiểm tra kết nối"** -> Khi hiện thông báo màu xanh `✓ Kết nối thành công` -> Bấm **"Lưu cấu hình"**.
5. Đăng nhập và sử dụng bình thường!

---

## 3. Kiến trúc mã nguồn (Mô hình 3 lớp)
- **`QlyPhongKham/DAL/`**: 
  - `DatabaseHelper.cs`: Quản lý kết nối SQL Server, tự động đọc/ghi `dbconfig.txt` khi đổi IP máy chủ.
  - `ClinicDAL.cs`: Xử lý toàn bộ truy vấn SQL (CRUD bệnh nhân, cấp STT hàng chờ, trừ tồn kho thuốc bằng Transaction, tính tiền hóa đơn...).
- **`QlyPhongKham/Models/`**:
  - `ClinicModels.cs`: Định nghĩa các thực thể `BenhNhan`, `PhieuKham`, `Thuoc`, `HoaDon`, `NhanVien`, `TaiKhoan`...
- **`QlyPhongKham/Forms/`**:
  - `FormLogin.cs`: Giao diện đăng nhập, hỗ trợ chọn nhanh tài khoản mẫu và nút cấu hình máy chủ.
  - `FormConfigDb.cs`: Giao diện cấu hình IP máy chủ SQL Server động.
  - `FormMain.cs`: Dashboard quản lý tập trung với phân quyền người dùng (Role-Based Access Control).

---

## 4. Chạy từ mã nguồn
Chạy lệnh trực tiếp từ thư mục `D:\QlyPhongKhamDesktop`:
```powershell
dotnet run
```
Hoặc mở file `QlyPhongKham.csproj` bằng **Visual Studio 2022** và bấm **Start (F5)**.

## 5. Đóng gói và chạy ứng dụng trên Windows

Người dùng cuối không cần cài Visual Studio, .NET SDK hoặc chạy `dotnet run`. Tải file ZIP phát hành từ mục **Releases** trên GitHub, giải nén vào thư mục người dùng có quyền ghi, rồi chạy `QlyPhongKham.exe`.

Mỗi máy cần có SQL Server Database Engine và database riêng:

1. Cài SQL Server Express/Database Engine, bật SQL Server Authentication (Mixed Mode) và đặt mật khẩu riêng. Tên instance có thể khác nhau trên mỗi máy.
2. Trong ứng dụng, vào **⚙ Máy chủ** và bấm **Tìm cục bộ**, hoặc nhập Server Name đúng như trong SSMS. Điền tài khoản SQL và bấm **Kiểm tra SQL Server**. Bước này kiểm tra máy chủ/tài khoản, độc lập với tên database.
3. Trong SSMS mở `init_database.sql`, bật **Query → SQLCMD Mode**, rồi bấm Execute/F5. Dùng tên database `QL_PhongKham` trong cấu hình ứng dụng để khớp dòng `:setvar DatabaseName "QL_PhongKham"` trong script. Nếu muốn tên khác, chỉ sửa giá trị ở dòng `:setvar` và nhập đúng tên đó trong cấu hình ứng dụng. Script tạo database nếu chưa có và nạp dữ liệu mẫu.
4. Quay lại ứng dụng, lưu cấu hình rồi đăng nhập. Nếu đăng nhập khi SQL chưa kết nối được, chọn **Yes** ở thông báo để mở cấu hình máy chủ.
5. Cấu hình lưu riêng trên máy tại `%LOCALAPPDATA%\QlyPhongKham\dbconfig.txt`; không chia sẻ file này vì có thể chứa thông tin đăng nhập.

Để tạo gói phát hành tại máy phát triển, chạy `.\publish.ps1` bằng PowerShell (yêu cầu .NET 9 SDK). Gói ZIP được tạo trong `artifacts\QlyPhongKham-win-x64.zip`. GitHub Actions tự đóng gói khi đẩy mã lên `master`; khi tạo tag dạng `v*`, workflow tạo GitHub Release kèm file ZIP.

### Ghi chú cập nhật cơ sở dữ liệu
- Phiếu khám lưu thêm **khám lâm sàng** và **sinh hiệu**. Khi mở chức năng khám trên cơ sở dữ liệu cũ, ứng dụng tự bổ sung hai cột `KhamLamSang` và `SinhHieu` vào bảng `PhieuKham`; tài khoản SQL cần quyền `ALTER TABLE`.
- Khi mở ứng dụng, hệ thống tự bổ sung schema khoa/phòng/phân công còn thiếu và các phòng mẫu nếu chưa có. Tài khoản SQL cần quyền `CREATE TABLE` và `ALTER TABLE`; sau đó admin có thể gán nhân viên tại **Nhân sự → Phân công phòng**.
- Không chạy lại `init_database.sql` trên cơ sở dữ liệu đang sử dụng vì script khởi tạo sẽ xóa và tạo lại các bảng.
- Tên database phải bắt đầu bằng chữ cái và chỉ gồm chữ cái, chữ số hoặc dấu gạch dưới; không dùng tên database hệ thống của SQL Server.
- Để thêm khoa/phòng và phân công vào database cũ, mở [`seed_test_data.sql`](./seed_test_data.sql) trong SQL Server Management Studio, chọn đúng database ứng dụng đang kết nối rồi chạy. Script chỉ thêm dữ liệu chưa có, có thể chạy lại và không xóa dữ liệu hiện có.
- Tài khoản test nhân viên: `test_admin`, `test_bs`, `test_bs2`, `test_letan`; tài khoản bệnh nhân: `test_bn01`, `test_bn04`, `test_bn05`. Mật khẩu các tài khoản test là `123456`.
- Admin phân công bác sĩ/lễ tân vào một hoặc nhiều phòng tại **Nhân sự → Phân công phòng**. Mỗi bác sĩ chỉ có một chuyên khoa chính và chỉ được gán vào các phòng thuộc khoa đó; lễ tân có thể được gán vào nhiều phòng thuộc nhiều khoa. Lễ tân chỉ thấy các phòng được phân công và chỉ cấp phiếu cho bác sĩ cùng phòng. Bác sĩ chỉ xem hồ sơ bệnh nhân từng được phân công khám cho mình.
- Admin quản lý danh mục chuyên khoa và phòng khám tại menu **Khoa / Phòng khám**: thêm, sửa, xóa và làm mới danh sách. Không thể xóa khoa/phòng đang được sử dụng; không thể chuyển phòng đã có lịch sử khám sang chuyên khoa khác để giữ nguyên dữ liệu báo cáo.
- Các màn hình trong ứng dụng có nút **Làm mới** ở thanh trên cùng để tải lại dữ liệu của màn hình hiện tại; báo cáo/tổng quan chỉ xem và làm mới vì đây là số liệu tổng hợp. Menu được quyết định theo role tài khoản, không phải theo phòng được phân công; thay đổi role có hiệu lực từ lần đăng nhập tiếp theo.
- Admin quản lý lịch hẹn tại **Lịch hẹn hôm nay** (thêm, sửa, hủy mềm và làm mới). Lễ tân và thu ngân dùng chung role **LeTan**, với cùng các menu tiếp đón, hồ sơ bệnh nhân, kho và thanh toán. Khi mở ứng dụng, role **ThuNgan** cũ được chuẩn hóa sang **LeTan**. Phân công phòng chỉ dành cho nhân viên có role **BacSi** hoặc **LeTan**.
- Trong hộp thoại phân công phòng, có thể bỏ chọn từng phòng rồi lưu hoặc dùng **Hủy toàn bộ phân công** để gỡ hết phòng của nhân viên. Phiếu chỉ hủy được khi còn chờ khám; hóa đơn đã thanh toán và hồ sơ khám đã thực hiện được giữ nguyên để bảo toàn lịch sử.
- Tài khoản bệnh nhân đăng nhập vào cổng riêng, chỉ xem hồ sơ, lịch sử khám, chỉ định dịch vụ, đơn thuốc và hóa đơn gắn với mã bệnh nhân của tài khoản đó.
- Cổng bệnh nhân WinForms có menu riêng **Lịch hẹn**, **Đặt lịch khám**, **Hồ sơ khám bệnh**, viện phí và trợ lý hướng dẫn. Trong **Đặt lịch khám**, bệnh nhân có thể thêm yêu cầu, sửa lịch còn tương lai đang chờ xác nhận, xem chi tiết và hủy lịch; thao tác xóa là hủy mềm để lưu lịch sử. Bệnh nhân chọn chuyên khoa, bác sĩ, ngày giờ và lý do khám để gửi yêu cầu; lịch mới ở trạng thái chờ xác nhận. Hồ sơ khám bệnh hiển thị lịch sử khám, dịch vụ cận lâm sàng, đơn thuốc và thông tin cá nhân. Khung giờ đã qua trong ngày không được chọn; thanh toán hóa đơn thực hiện tại quầy thu ngân.
- Lịch hẹn bệnh nhân hiển thị cột **Trạng thái duyệt**. Lễ tân/thu ngân được phân công cùng phòng có thể duyệt lịch chờ xác nhận tại menu **Lịch khám**; lịch sau khi duyệt chuyển sang **Đã xác nhận** và xuất hiện trong mục **Lịch đã duyệt** của bác sĩ phụ trách để bác sĩ làm mới/tra cứu theo ngày.
- Không chạy lại `init_database.sql` trên cơ sở dữ liệu đang sử dụng vì script khởi tạo sẽ xóa và tạo lại các bảng. Chạy seed trên môi trường test nếu cần thêm các bản ghi kiểm thử.
