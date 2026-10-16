-- =============================================
-- HỆ THỐNG QUẢN LÝ PHÒNG KHÁM
-- SCRIPT KHỞI TẠO CSDL VÀ DỮ LIỆU MẪU
-- Tương thích: SQL Server (sa / 123456789)
-- =============================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'QL_PhongKham')
BEGIN
    CREATE DATABASE QL_PhongKham;
END
GO

USE QL_PhongKham;
GO

-- 1. BẢNG NHÂN VIÊN
IF OBJECT_ID('TaiKhoan', 'U') IS NOT NULL DROP TABLE TaiKhoan;
IF OBJECT_ID('ChiTietDonThuoc', 'U') IS NOT NULL DROP TABLE ChiTietDonThuoc;
IF OBJECT_ID('DonThuoc', 'U') IS NOT NULL DROP TABLE DonThuoc;
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL DROP TABLE HoaDon;
IF OBJECT_ID('ChiTietChiDinhDV', 'U') IS NOT NULL DROP TABLE ChiTietChiDinhDV;
IF OBJECT_ID('DichVu', 'U') IS NOT NULL DROP TABLE DichVu;
IF OBJECT_ID('PhieuKham', 'U') IS NOT NULL DROP TABLE PhieuKham;
IF OBJECT_ID('LichHen', 'U') IS NOT NULL DROP TABLE LichHen;
IF OBJECT_ID('BenhNhan', 'U') IS NOT NULL DROP TABLE BenhNhan;
IF OBJECT_ID('Thuoc', 'U') IS NOT NULL DROP TABLE Thuoc;
IF OBJECT_ID('NhanVien', 'U') IS NOT NULL DROP TABLE NhanVien;
GO

CREATE TABLE NhanVien (
    MaNV VARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    ChucVu NVARCHAR(20) NOT NULL CONSTRAINT CK_NhanVien_ChucVu CHECK (ChucVu IN (N'Admin', N'BacSi', N'LeTan')),
    ChuyenKhoa NVARCHAR(100) NULL,
    SoDienThoai VARCHAR(15) NULL,
    Email VARCHAR(100) NULL,
    TrangThai BIT NOT NULL DEFAULT 1
);
GO

-- 2. BẢNG TÀI KHOẢN (1-1 với NhanVien)
CREATE TABLE TaiKhoan (
    TenDangNhap VARCHAR(50) PRIMARY KEY,
    MatKhau VARCHAR(255) NOT NULL,
    MaNV VARCHAR(10) NOT NULL UNIQUE,
    Quyen NVARCHAR(20) NOT NULL CONSTRAINT CK_TaiKhoan_Quyen CHECK (Quyen IN (N'Admin', N'BacSi', N'LeTan')),
    TrangThai BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_TaiKhoan_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV) ON DELETE CASCADE
);
GO

-- 3. BẢNG BỆNH NHÂN
CREATE TABLE BenhNhan (
    MaBN VARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    NgaySinh DATE NULL,
    GioiTinh NVARCHAR(5) NOT NULL CONSTRAINT CK_BenhNhan_GioiTinh CHECK (GioiTinh IN (N'Nam', N'Nữ')),
    SoDienThoai VARCHAR(15) NULL,
    DiaChi NVARCHAR(200) NULL,
    TienSuBenh NVARCHAR(MAX) NULL,
    NgayTao DATETIME DEFAULT GETDATE()
);
GO

-- 4. BẢNG LỊCH HẸN
CREATE TABLE LichHen (
    MaLichHen INT IDENTITY(1,1) PRIMARY KEY,
    MaBN VARCHAR(10) NOT NULL,
    MaNV_BacSi VARCHAR(10) NULL,
    NgayHen DATETIME NOT NULL,
    LyDoKham NVARCHAR(255) NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ xác nhận' 
        CONSTRAINT CK_LichHen_TrangThai CHECK (TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đã đến', N'Đã hủy')),
    CONSTRAINT FK_LichHen_BenhNhan FOREIGN KEY (MaBN) REFERENCES BenhNhan(MaBN) ON DELETE CASCADE,
    CONSTRAINT FK_LichHen_BacSi FOREIGN KEY (MaNV_BacSi) REFERENCES NhanVien(MaNV)
);
GO

-- 5. BẢNG PHIẾU KHÁM (Hồ sơ khám & Hàng chờ khám)
CREATE TABLE PhieuKham (
    MaPK VARCHAR(10) PRIMARY KEY,
    STT INT NOT NULL,
    MaBN VARCHAR(10) NOT NULL,
    MaNV_BacSi VARCHAR(10) NOT NULL,
    NgayKham DATETIME NOT NULL DEFAULT GETDATE(),
    TrieuChung NVARCHAR(255) NULL,
    ChanDoan NVARCHAR(255) NULL,
    LoiDan NVARCHAR(500) NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ khám'
        CONSTRAINT CK_PhieuKham_TrangThai CHECK (TrangThai IN (N'Chờ khám', N'Đang khám', N'Chờ kết quả CLS', N'Đã khám xong', N'Đã hủy')),
    CONSTRAINT FK_PhieuKham_BenhNhan FOREIGN KEY (MaBN) REFERENCES BenhNhan(MaBN),
    CONSTRAINT FK_PhieuKham_BacSi FOREIGN KEY (MaNV_BacSi) REFERENCES NhanVien(MaNV)
);
GO

-- 6. BẢNG DỊCH VỤ CẬN LÂM SÀNG
CREATE TABLE DichVu (
    MaDV VARCHAR(10) PRIMARY KEY,
    TenDV NVARCHAR(100) NOT NULL,
    DonGia DECIMAL(18,2) NOT NULL CONSTRAINT CK_DichVu_DonGia CHECK (DonGia >= 0),
    MoTa NVARCHAR(255) NULL
);
GO

-- 7. BẢNG CHI TIẾT CHỈ ĐỊNH DỊCH VỤ
CREATE TABLE ChiTietChiDinhDV (
    MaChiDinh INT IDENTITY(1,1) PRIMARY KEY,
    MaPK VARCHAR(10) NOT NULL,
    MaDV VARCHAR(10) NOT NULL,
    DonGiaThoiDiem DECIMAL(18,2) NOT NULL CONSTRAINT CK_ChiDinh_DonGia CHECK (DonGiaThoiDiem >= 0),
    KetQua NVARCHAR(MAX) NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ thực hiện'
        CONSTRAINT CK_ChiDinh_TrangThai CHECK (TrangThai IN (N'Chờ thực hiện', N'Đã có kết quả')),
    CONSTRAINT FK_ChiDinh_PhieuKham FOREIGN KEY (MaPK) REFERENCES PhieuKham(MaPK) ON DELETE CASCADE,
    CONSTRAINT FK_ChiDinh_DichVu FOREIGN KEY (MaDV) REFERENCES DichVu(MaDV)
);
GO

-- 8. BẢNG THUỐC
CREATE TABLE Thuoc (
    MaThuoc VARCHAR(10) PRIMARY KEY,
    TenThuoc NVARCHAR(100) NOT NULL,
    DonViTinh NVARCHAR(20) NOT NULL,
    DonGiaBan DECIMAL(18,2) NOT NULL CONSTRAINT CK_Thuoc_DonGia CHECK (DonGiaBan >= 0),
    SoLuongTon INT NOT NULL DEFAULT 0 CONSTRAINT CK_Thuoc_TonKho CHECK (SoLuongTon >= 0),
    CachDungMacDinh NVARCHAR(200) NULL
);
GO

-- 9. BẢNG ĐƠN THUỐC (1-1 với PhieuKham)
CREATE TABLE DonThuoc (
    MaDonThuoc VARCHAR(10) PRIMARY KEY,
    MaPK VARCHAR(10) NOT NULL UNIQUE,
    NgayKe DATETIME NOT NULL DEFAULT GETDATE(),
    GhiChu NVARCHAR(255) NULL,
    CONSTRAINT FK_DonThuoc_PhieuKham FOREIGN KEY (MaPK) REFERENCES PhieuKham(MaPK) ON DELETE CASCADE
);
GO

-- 10. BẢNG CHI TIẾT ĐƠN THUỐC
CREATE TABLE ChiTietDonThuoc (
    MaCTDT INT IDENTITY(1,1) PRIMARY KEY,
    MaDonThuoc VARCHAR(10) NOT NULL,
    MaThuoc VARCHAR(10) NOT NULL,
    SoLuong INT NOT NULL CONSTRAINT CK_CTDT_SoLuong CHECK (SoLuong > 0),
    DonGiaThoiDiem DECIMAL(18,2) NOT NULL CONSTRAINT CK_CTDT_DonGia CHECK (DonGiaThoiDiem >= 0),
    CachDung NVARCHAR(255) NOT NULL,
    CONSTRAINT FK_CTDT_DonThuoc FOREIGN KEY (MaDonThuoc) REFERENCES DonThuoc(MaDonThuoc) ON DELETE CASCADE,
    CONSTRAINT FK_CTDT_Thuoc FOREIGN KEY (MaThuoc) REFERENCES Thuoc(MaThuoc)
);
GO

-- 11. BẢNG HÓA ĐƠN (1-1 với PhieuKham)
CREATE TABLE HoaDon (
    MaHD VARCHAR(10) PRIMARY KEY,
    MaPK VARCHAR(10) NOT NULL UNIQUE,
    MaNV_ThuNgan VARCHAR(10) NOT NULL,
    NgayLap DATETIME NOT NULL DEFAULT GETDATE(),
    TienDichVu DECIMAL(18,2) NOT NULL DEFAULT 0,
    TienThuoc DECIMAL(18,2) NOT NULL DEFAULT 0,
    GiamGia DECIMAL(18,2) NOT NULL DEFAULT 0,
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
    HinhThucTT NVARCHAR(30) NOT NULL DEFAULT N'Tiền mặt'
        CONSTRAINT CK_HoaDon_HinhThucTT CHECK (HinhThucTT IN (N'Tiền mặt', N'Chuyển khoản QR', N'Thẻ')),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chưa thanh toán'
        CONSTRAINT CK_HoaDon_TrangThai CHECK (TrangThai IN (N'Chưa thanh toán', N'Đã thanh toán', N'Đã hủy')),
    CONSTRAINT FK_HoaDon_PhieuKham FOREIGN KEY (MaPK) REFERENCES PhieuKham(MaPK),
    CONSTRAINT FK_HoaDon_ThuNgan FOREIGN KEY (MaNV_ThuNgan) REFERENCES NhanVien(MaNV)
);
GO

-- =============================================
-- THÊM DỮ LIỆU MẪU (SEED DATA)
-- =============================================

-- Nhân viên
INSERT INTO NhanVien (MaNV, HoTen, ChucVu, ChuyenKhoa, SoDienThoai, Email, TrangThai) VALUES
('NV001', N'Nguyễn Quản Trị', 'Admin', NULL, '0901234567', 'admin@phongkham.com', 1),
('NV002', N'BS. Trần Văn Nam', 'BacSi', N'Nội tổng quát', '0912345678', 'nam.tv@phongkham.com', 1),
('NV003', N'BS. Lê Thị Hoa', 'BacSi', N'Tai Mũi Họng', '0923456789', 'hoa.lt@phongkham.com', 1),
('NV004', N'Phạm Thu Ngân', 'LeTan', NULL, '0934567890', 'ngan.pt@phongkham.com', 1),
('NV005', N'Hoàng Lễ Tân', 'LeTan', NULL, '0945678901', 'tan.hl@phongkham.com', 1);

-- Mật khẩu hash SHA256 cho '123' với Salt 'PhongKhamSecretKey':
-- SHA256("123" + "PhongKhamSecretKey") = "Qd5xU...": Để đơn giản cho BTL, lưu mật khẩu dạng hash hoặc plain text hỗ trợ cả hai
-- C# Helper sẽ kiểm tra plain text hoặc hash. Ở đây lưu "123456" làm mẫu cho dễ test
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, MaNV, Quyen, TrangThai) VALUES
('admin', '123456', 'NV001', 'Admin', 1),
('bacsi_nam', '123456', 'NV002', 'BacSi', 1),
('bacsi_hoa', '123456', 'NV003', 'BacSi', 1),
('thungan', '123456', 'NV004', 'LeTan', 1),
('letan', '123456', 'NV005', 'LeTan', 1);

-- Bệnh nhân
INSERT INTO BenhNhan (MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh) VALUES
('BN0001', N'Vũ Thị Mai', '1995-04-12', N'Nữ', '0988111222', N'Hà Nội', N'Dị ứng Penicillin'),
('BN0002', N'Đặng Văn Hùng', '1988-09-25', N'Nam', '0977333444', N'Hải Phòng', N'Tiểu đường type 2'),
('BN0003', N'Ngô Bảo Châu', '2002-01-18', N'Nam', '0966555666', N'Nam Định', N'Không');

-- Dịch vụ
INSERT INTO DichVu (MaDV, TenDV, DonGia, MoTa) VALUES
('DV01', N'Khám bệnh chuyên khoa', 100000, N'Khám lâm sàng và tư vấn bác sĩ'),
('DV02', N'Nội soi Tai - Mũi - Họng', 150000, N'Nội soi phát hiện viêm xoang, amidan'),
('DV03', N'Siêu âm tổng quát', 200000, N'Siêu âm ổ bụng tổng quát'),
('DV04', N'Xét nghiệm máu tổng quát', 250000, N'Tổng phân tích tế bào máu');

-- Thuốc
INSERT INTO Thuoc (MaThuoc, TenThuoc, DonViTinh, DonGiaBan, SoLuongTon, CachDungMacDinh) VALUES
('TH01', N'Paracetamol 500mg', N'Viên', 2000, 500, N'Uống 1 viên khi sốt trên 38.5 độ, cách 4-6 tiếng'),
('TH02', N'Amoxicillin 500mg', N'Viên', 5000, 200, N'Uống 1 viên x 2 lần/ngày sau ăn'),
('TH03', N'Cetirizine 10mg', N'Viên', 3000, 300, N'Uống 1 viên vào buổi tối trước khi ngủ'),
('TH04', N'Omeprazole 20mg', N'Viên', 4000, 250, N'Uống 1 viên trước bữa ăn sáng 30 phút'),
('TH05', N'Berberin 100mg', N'Viên', 1500, 400, N'Uống 2 viên x 2 lần/ngày');
GO

PRINT N'Khởi tạo Cơ sở dữ liệu QL_PhongKham thành công!';
