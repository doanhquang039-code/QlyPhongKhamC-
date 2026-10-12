# HỆ THỐNG QUẢN LÝ PHÒNG KHÁM TƯ NHÂN (C# WinForms - SQL Server)

## 1. Thông tin CSDL & Tài khoản truy cập
- **Tên CSDL:** `doanhbtlc#`
- **Tài khoản SA mặc định:**
  - **User:** `sa`
  - **Password:** `123456789`
  - **Cổng kết nối:** `1433` (TCP/IP)
- **File script tạo CSDL:** `init_database.sql`

### Danh sách tài khoản đăng nhập mẫu vào phần mềm:
| Vai trò | Tên đăng nhập | Mật khẩu | Quyền hạn & Chức năng |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin` | `123456` | Toàn quyền tất cả các phân hệ, quản trị nhân sự |
| **Bác sĩ khám** | `bacsi_nam` | `123456` | Xem hàng chờ theo STT, chẩn đoán, kê đơn thuốc |
| **Lễ tân** | `letan` | `123456` | Đăng ký bệnh nhân mới, cấp STT hàng chờ trong ngày |
| **Thu ngân** | `thungan` | `123456` | Tính chi phí khám + thuốc, xuất hóa đơn viện phí |

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
2. Mở phần mềm -> Tại màn hình Đăng nhập, bấm nút **⚙ Máy chủ** (hoặc mở file `dbconfig.txt`).
3. Điền các thông tin:
   - **Máy chủ (IP / Host):** Nhập IP của máy chủ (VD: `192.168.1.15`).
   - **Tên CSDL:** `QL_PhongKham`
   - **Tài khoản:** `sa`
   - **Mật khẩu:** `123456789`
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

## 4. Cách chạy dự án
Chạy lệnh trực tiếp từ thư mục `D:\QlyPhongKhamDesktop`:
```powershell
dotnet run
```
Hoặc mở file `QlyPhongKham.csproj` bằng **Visual Studio 2022** và bấm **Start (F5)**.
