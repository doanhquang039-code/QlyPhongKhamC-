-- =============================================
-- HỆ THỐNG QUẢN LÝ PHÒNG KHÁM
-- SCRIPT KHỞI TẠO CSDL VÀ DỮ LIỆU MẪU
-- Chạy trong SSMS với Query > SQLCMD Mode được bật.
-- Để đổi tên database, chỉ sửa giá trị ở dòng :setvar bên dưới.
-- Yêu cầu SQL Server đã cài đặt và tài khoản có quyền tạo cơ sở dữ liệu.
-- =============================================

:setvar DatabaseName "QL_PhongKham"

USE master;
GO

IF N'$(DatabaseName)' IN (N'master', N'model', N'msdb', N'tempdb')
    THROW 50001, N'Khong duoc dung ten database he thong.', 1;
GO

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    DECLARE @CreateDatabaseSql NVARCHAR(MAX) =
        N'CREATE DATABASE ' + QUOTENAME(N'$(DatabaseName)');
    EXEC sp_executesql @CreateDatabaseSql;
END
GO

USE [$(DatabaseName)];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- 1. BẢNG NHÂN VIÊN
IF OBJECT_ID('LichHen', 'U') IS NOT NULL AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_LichHen_PhieuKhamTaiKham')
    ALTER TABLE LichHen DROP CONSTRAINT FK_LichHen_PhieuKhamTaiKham;
IF OBJECT_ID('SinhHieuPhieuKham', 'U') IS NOT NULL DROP TABLE SinhHieuPhieuKham;
IF OBJECT_ID('PhieuKham', 'U') IS NOT NULL AND EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_PhieuKham_LichHen')
    ALTER TABLE PhieuKham DROP CONSTRAINT FK_PhieuKham_LichHen;
IF OBJECT_ID('ChiTietCapPhatThuoc', 'U') IS NOT NULL DROP TABLE ChiTietCapPhatThuoc;
IF OBJECT_ID('DieuChinhKho', 'U') IS NOT NULL DROP TABLE DieuChinhKho;
IF OBJECT_ID('LoThuoc', 'U') IS NOT NULL DROP TABLE LoThuoc;
IF OBJECT_ID('PhieuNhapKho', 'U') IS NOT NULL DROP TABLE PhieuNhapKho;
IF OBJECT_ID('NhaCungCap', 'U') IS NOT NULL DROP TABLE NhaCungCap;
IF OBJECT_ID('HoSoSucKhoe', 'U') IS NOT NULL DROP TABLE HoSoSucKhoe;
IF OBJECT_ID('DongYDuLieuCaNhan', 'U') IS NOT NULL DROP TABLE DongYDuLieuCaNhan;
IF OBJECT_ID('DanhMucICD10', 'U') IS NOT NULL DROP TABLE DanhMucICD10;
IF OBJECT_ID('NhatKyHeThong', 'U') IS NOT NULL DROP TABLE NhatKyHeThong;
IF OBJECT_ID('TaiKhoan', 'U') IS NOT NULL DROP TABLE TaiKhoan;
IF OBJECT_ID('TaiKhoanBenhNhan', 'U') IS NOT NULL DROP TABLE TaiKhoanBenhNhan;
IF OBJECT_ID('ChiTietDonThuoc', 'U') IS NOT NULL DROP TABLE ChiTietDonThuoc;
IF OBJECT_ID('DonThuoc', 'U') IS NOT NULL DROP TABLE DonThuoc;
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL DROP TABLE HoaDon;
IF OBJECT_ID('ChiTietChiDinhDV', 'U') IS NOT NULL DROP TABLE ChiTietChiDinhDV;
IF OBJECT_ID('DichVu', 'U') IS NOT NULL DROP TABLE DichVu;
IF OBJECT_ID('PhieuKham', 'U') IS NOT NULL DROP TABLE PhieuKham;
IF OBJECT_ID('LichHen', 'U') IS NOT NULL DROP TABLE LichHen;
IF OBJECT_ID('BenhNhan', 'U') IS NOT NULL DROP TABLE BenhNhan;
IF OBJECT_ID('Thuoc', 'U') IS NOT NULL DROP TABLE Thuoc;
IF OBJECT_ID('PhanCongPhong', 'U') IS NOT NULL DROP TABLE PhanCongPhong;
IF OBJECT_ID('PhongKham', 'U') IS NOT NULL DROP TABLE PhongKham;
IF OBJECT_ID('Khoa', 'U') IS NOT NULL DROP TABLE Khoa;
IF OBJECT_ID('NhanVien', 'U') IS NOT NULL DROP TABLE NhanVien;
GO

CREATE TABLE NhanVien (
    MaNV VARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    ChucVu NVARCHAR(20) NOT NULL CONSTRAINT CK_NhanVien_ChucVu CHECK (ChucVu IN (N'Admin', N'BacSi', N'LeTan', N'ThuNgan')),
    ChuyenKhoa NVARCHAR(100) NULL,
    SoDienThoai VARCHAR(15) NULL,
    Email VARCHAR(100) NULL,
    AnhDaiDien VARBINARY(MAX) NULL,
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

-- 3. KHOA, PHÒNG KHÁM VÀ PHÂN CÔNG NHIỀU-NHIỀU
CREATE TABLE Khoa (
    MaKhoa VARCHAR(10) PRIMARY KEY,
    TenKhoa NVARCHAR(100) NOT NULL UNIQUE
);
GO

CREATE TABLE PhongKham (
    MaPhong VARCHAR(10) PRIMARY KEY,
    MaKhoa VARCHAR(10) NOT NULL,
    TenPhong NVARCHAR(100) NOT NULL,
    CONSTRAINT FK_PhongKham_Khoa FOREIGN KEY (MaKhoa) REFERENCES Khoa(MaKhoa)
);
GO

CREATE TABLE PhanCongPhong (
    MaNV VARCHAR(10) NOT NULL,
    MaPhong VARCHAR(10) NOT NULL,
    VaiTro NVARCHAR(20) NOT NULL CONSTRAINT CK_PhanCongPhong_VaiTro CHECK (VaiTro IN (N'BacSi', N'LeTan')),
    CONSTRAINT PK_PhanCongPhong PRIMARY KEY (MaNV, MaPhong),
    CONSTRAINT FK_PhanCongPhong_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV) ON DELETE CASCADE,
    CONSTRAINT FK_PhanCongPhong_Phong FOREIGN KEY (MaPhong) REFERENCES PhongKham(MaPhong) ON DELETE CASCADE
);
GO

-- 3. BẢNG BỆNH NHÂN
CREATE TABLE BenhNhan (
    MaBN VARCHAR(10) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    NgaySinh DATE NULL,
    GioiTinh NVARCHAR(5) NOT NULL CONSTRAINT CK_BenhNhan_GioiTinh CHECK (GioiTinh IN (N'Nam', N'Nữ')),
    SoDienThoai VARCHAR(15) NULL,
    Email VARCHAR(100) NULL,
    DiaChi NVARCHAR(200) NULL,
    TienSuBenh NVARCHAR(MAX) NULL,
    AnhDaiDien VARBINARY(MAX) NULL,
    NgayTao DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE HoSoSucKhoe (
    MaHoSo BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaBN VARCHAR(10) NOT NULL,
    Loai NVARCHAR(30) NOT NULL CHECK (Loai IN (N'Dị ứng',N'Bệnh mạn tính',N'Thuốc đang dùng',N'Tiền sử phẫu thuật')),
    NoiDung NVARCHAR(500) NOT NULL,
    NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE(),
    MaNV VARCHAR(10) NULL,
    CONSTRAINT FK_HoSoSucKhoe_BN FOREIGN KEY(MaBN) REFERENCES BenhNhan(MaBN),
    CONSTRAINT FK_HoSoSucKhoe_NV FOREIGN KEY(MaNV) REFERENCES NhanVien(MaNV)
);
GO
CREATE TABLE DongYDuLieuCaNhan (
    MaBN VARCHAR(10) PRIMARY KEY,
    DongY BIT NOT NULL,
    NgayGhiNhan DATETIME NOT NULL DEFAULT GETDATE(),
    PhienBan NVARCHAR(30) NOT NULL,
    CONSTRAINT FK_DongYDuLieu_BN FOREIGN KEY(MaBN) REFERENCES BenhNhan(MaBN)
);
GO
CREATE TABLE DanhMucICD10 (MaICD10 VARCHAR(10) PRIMARY KEY,TenChanDoan NVARCHAR(255) NOT NULL,ConHieuLuc BIT NOT NULL DEFAULT 1);
GO

CREATE TABLE NhatKyHeThong (
    MaNhatKy BIGINT IDENTITY(1,1) PRIMARY KEY, NgayGhi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    MaNV VARCHAR(10) NULL, TenDangNhap NVARCHAR(100) NULL, HanhDong NVARCHAR(80) NOT NULL,
    DoiTuong NVARCHAR(120) NULL, ChiTiet NVARCHAR(1000) NULL, MayTram NVARCHAR(128) NULL
);
GO

CREATE TABLE TaiKhoanBenhNhan (
    TenDangNhap VARCHAR(50) PRIMARY KEY,
    MatKhau VARCHAR(255) NOT NULL,
    MaBN VARCHAR(10) NOT NULL UNIQUE,
    TrangThai BIT NOT NULL DEFAULT 1,
    CONSTRAINT FK_TaiKhoanBenhNhan_BenhNhan FOREIGN KEY (MaBN) REFERENCES BenhNhan(MaBN) ON DELETE CASCADE
);
GO

-- 4. BẢNG LỊCH HẸN
CREATE TABLE LichHen (
    MaLichHen INT IDENTITY(1,1) PRIMARY KEY,
    MaBN VARCHAR(10) NOT NULL,
    MaNV_BacSi VARCHAR(10) NULL,
    NgayHen DATETIME NOT NULL,
    LyDoKham NVARCHAR(255) NULL,
    LyDoXuLy NVARCHAR(255) NULL,
    MaPK_TaiKham VARCHAR(10) NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ xác nhận' 
        CONSTRAINT CK_LichHen_TrangThai CHECK (TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đã đến', N'Đã hủy', N'Bị từ chối', N'Không đến')),
    CONSTRAINT FK_LichHen_BenhNhan FOREIGN KEY (MaBN) REFERENCES BenhNhan(MaBN) ON DELETE CASCADE,
    CONSTRAINT FK_LichHen_BacSi FOREIGN KEY (MaNV_BacSi) REFERENCES NhanVien(MaNV)
);
GO

CREATE UNIQUE INDEX UX_LichHen_BacSi_NgayHen_Active ON LichHen(MaNV_BacSi, NgayHen)
    WHERE MaNV_BacSi IS NOT NULL AND TrangThai <> N'Đã hủy' AND TrangThai <> N'Bị từ chối' AND TrangThai <> N'Không đến';
GO

-- 5. BẢNG PHIẾU KHÁM (Hồ sơ khám & Hàng chờ khám)
CREATE TABLE PhieuKham (
    MaPK VARCHAR(10) PRIMARY KEY,
    STT INT NOT NULL,
    MaBN VARCHAR(10) NOT NULL,
    MaNV_BacSi VARCHAR(10) NOT NULL,
    MaPhong VARCHAR(10) NULL,
    MaPK_Goc VARCHAR(10) NULL,
    LyDoChuyen NVARCHAR(500) NULL,
    NgayKham DATETIME NOT NULL DEFAULT GETDATE(),
    NgayKhamNgay AS CONVERT(DATE, NgayKham) PERSISTED,
    TrieuChung NVARCHAR(255) NULL,
    KhamLamSang NVARCHAR(MAX) NULL,
    SinhHieu NVARCHAR(200) NULL,
    ChanDoan NVARCHAR(255) NULL,
    LoiDan NVARCHAR(500) NULL,
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chờ khám'
        CONSTRAINT CK_PhieuKham_TrangThai CHECK (TrangThai IN (N'Chờ khám', N'Đang khám', N'Chờ kết quả CLS', N'Đã khám xong', N'Đã hủy', N'Đã chuyển phòng')),
    CONSTRAINT FK_PhieuKham_BenhNhan FOREIGN KEY (MaBN) REFERENCES BenhNhan(MaBN),
    CONSTRAINT FK_PhieuKham_BacSi FOREIGN KEY (MaNV_BacSi) REFERENCES NhanVien(MaNV),
    CONSTRAINT FK_PhieuKham_Phong FOREIGN KEY (MaPhong) REFERENCES PhongKham(MaPhong),
    CONSTRAINT FK_PhieuKham_PhieuKhamGoc FOREIGN KEY (MaPK_Goc) REFERENCES PhieuKham(MaPK)
);
GO

ALTER TABLE PhieuKham ADD MaLichHen INT NULL;
ALTER TABLE PhieuKham ADD CONSTRAINT FK_PhieuKham_LichHen FOREIGN KEY (MaLichHen) REFERENCES LichHen(MaLichHen);
ALTER TABLE LichHen ADD CONSTRAINT FK_LichHen_PhieuKhamTaiKham FOREIGN KEY (MaPK_TaiKham) REFERENCES PhieuKham(MaPK);
GO
CREATE UNIQUE INDEX UX_PhieuKham_MaLichHen ON PhieuKham(MaLichHen) WHERE MaLichHen IS NOT NULL;
CREATE UNIQUE INDEX UX_PhieuKham_Phong_Ngay_STT ON PhieuKham(MaPhong, NgayKhamNgay, STT) WHERE MaPhong IS NOT NULL;
GO

CREATE TABLE SinhHieuPhieuKham (
    MaSinhHieu BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaPK VARCHAR(10) NOT NULL,
    HuyetApTamThu DECIMAL(5,1) NOT NULL CHECK(HuyetApTamThu BETWEEN 40 AND 300),
    HuyetApTamTruong DECIMAL(5,1) NOT NULL CHECK(HuyetApTamTruong BETWEEN 20 AND 200),
    Mach DECIMAL(5,1) NOT NULL CHECK(Mach BETWEEN 20 AND 250),
    NhietDo DECIMAL(4,1) NOT NULL CHECK(NhietDo BETWEEN 25 AND 45),
    Spo2 DECIMAL(5,1) NOT NULL CHECK(Spo2 BETWEEN 50 AND 100),
    ChieuCao DECIMAL(5,1) NULL CHECK(ChieuCao BETWEEN 30 AND 250),
    CanNang DECIMAL(6,2) NULL CHECK(CanNang BETWEEN 1 AND 400),
    MaNV_Do VARCHAR(10) NOT NULL,
    NgayDo DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_SinhHieu_PhieuKham FOREIGN KEY(MaPK) REFERENCES PhieuKham(MaPK),
    CONSTRAINT FK_SinhHieu_NhanVien FOREIGN KEY(MaNV_Do) REFERENCES NhanVien(MaNV)
);
GO
CREATE INDEX IX_SinhHieuPhieuKham_MaPK_NgayDo ON SinhHieuPhieuKham(MaPK, NgayDo DESC);
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

CREATE TABLE NhaCungCap (
    MaNCC INT IDENTITY(1,1) PRIMARY KEY,
    TenNCC NVARCHAR(150) NOT NULL UNIQUE,
    SoDienThoai VARCHAR(20) NULL,
    DiaChi NVARCHAR(255) NULL,
    DangHoatDong BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE PhieuNhapKho (
    MaPN VARCHAR(12) PRIMARY KEY,
    MaNCC INT NOT NULL,
    MaNV_Nhap VARCHAR(10) NOT NULL,
    NgayNhap DATETIME NOT NULL DEFAULT GETDATE(),
    GhiChu NVARCHAR(255) NULL,
    CONSTRAINT FK_PhieuNhapKho_NCC FOREIGN KEY (MaNCC) REFERENCES NhaCungCap(MaNCC),
    CONSTRAINT FK_PhieuNhapKho_NhanVien FOREIGN KEY (MaNV_Nhap) REFERENCES NhanVien(MaNV)
);
GO

CREATE TABLE LoThuoc (
    MaLo INT IDENTITY(1,1) PRIMARY KEY,
    MaPN VARCHAR(12) NOT NULL,
    MaThuoc VARCHAR(10) NOT NULL,
    SoLo NVARCHAR(60) NOT NULL,
    NgayHetHan DATE NOT NULL,
    DonGiaNhap DECIMAL(18,2) NOT NULL CHECK (DonGiaNhap >= 0),
    SoLuongNhap INT NOT NULL CHECK (SoLuongNhap > 0),
    SoLuongCon INT NOT NULL,
    CONSTRAINT CK_LoThuoc_SoLuongCon CHECK (SoLuongCon >= 0 AND SoLuongCon <= SoLuongNhap),
    CONSTRAINT FK_LoThuoc_PhieuNhap FOREIGN KEY (MaPN) REFERENCES PhieuNhapKho(MaPN),
    CONSTRAINT FK_LoThuoc_Thuoc FOREIGN KEY (MaThuoc) REFERENCES Thuoc(MaThuoc)
);
GO
CREATE INDEX IX_LoThuoc_FEFO ON LoThuoc(MaThuoc, NgayHetHan, MaLo) INCLUDE (SoLuongCon);
GO

CREATE TABLE DieuChinhKho (
    MaDieuChinh INT IDENTITY(1,1) PRIMARY KEY,
    MaLo INT NOT NULL,
    MaNV VARCHAR(10) NOT NULL,
    SoLuongThayDoi INT NOT NULL CHECK (SoLuongThayDoi <> 0),
    LyDo NVARCHAR(255) NOT NULL,
    NgayDieuChinh DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_DieuChinhKho_Lo FOREIGN KEY (MaLo) REFERENCES LoThuoc(MaLo),
    CONSTRAINT FK_DieuChinhKho_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV)
);
GO

-- 9. BẢNG ĐƠN THUỐC (1-1 với PhieuKham)
CREATE TABLE DonThuoc (
    MaDonThuoc VARCHAR(10) PRIMARY KEY,
    MaPK VARCHAR(10) NOT NULL,
    NgayKe DATETIME NOT NULL DEFAULT GETDATE(),
    GhiChu NVARCHAR(255) NULL,
    TrangThaiCapPhat NVARCHAR(20) NOT NULL DEFAULT N'Chờ cấp phát'
        CONSTRAINT CK_DonThuoc_TrangThaiCapPhat CHECK (TrangThaiCapPhat IN (N'Chờ cấp phát',N'Đã cấp',N'Không lấy',N'Mua ngoài')),
    NgayCapPhat DATETIME NULL,
    MaNV_CapPhat VARCHAR(10) NULL,
    GhiChuCapPhat NVARCHAR(255) NULL,
    CONSTRAINT FK_DonThuoc_PhieuKham FOREIGN KEY (MaPK) REFERENCES PhieuKham(MaPK) ON DELETE CASCADE
);
GO

ALTER TABLE DonThuoc ADD CONSTRAINT FK_DonThuoc_NhanVienCapPhat FOREIGN KEY (MaNV_CapPhat) REFERENCES NhanVien(MaNV);

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

CREATE TABLE ChiTietCapPhatThuoc (
    MaCapPhat INT IDENTITY(1,1) PRIMARY KEY,
    MaCTDT INT NOT NULL,
    MaLo INT NOT NULL,
    MaNV VARCHAR(10) NOT NULL,
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    NgayCapPhat DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_CapPhat_ChiTietDon FOREIGN KEY (MaCTDT) REFERENCES ChiTietDonThuoc(MaCTDT),
    CONSTRAINT FK_CapPhat_Lo FOREIGN KEY (MaLo) REFERENCES LoThuoc(MaLo),
    CONSTRAINT FK_CapPhat_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV)
);
GO

-- 11. BẢNG HÓA ĐƠN (1-1 với PhieuKham)
CREATE TABLE HoaDon (
    MaHD VARCHAR(10) PRIMARY KEY,
    MaPK VARCHAR(10) NOT NULL,
    MaNV_ThuNgan VARCHAR(10) NOT NULL,
    NgayLap DATETIME NOT NULL DEFAULT GETDATE(),
    NgayThanhToan DATETIME NULL,
    TienDichVu DECIMAL(18,2) NOT NULL DEFAULT 0,
    TienThuoc DECIMAL(18,2) NOT NULL DEFAULT 0,
    GiamGia DECIMAL(18,2) NOT NULL DEFAULT 0,
    TongTien DECIMAL(18,2) NOT NULL DEFAULT 0,
    HinhThucTT NVARCHAR(30) NOT NULL DEFAULT N'Tiền mặt'
        CONSTRAINT CK_HoaDon_HinhThucTT CHECK (HinhThucTT IN (N'Chưa chọn', N'Tiền mặt', N'Chuyển khoản QR', N'Thẻ')),
    TrangThai NVARCHAR(30) NOT NULL DEFAULT N'Chưa thanh toán'
        CONSTRAINT CK_HoaDon_TrangThai CHECK (TrangThai IN (N'Chưa thanh toán', N'Đã thanh toán', N'Đã hủy')),
    CONSTRAINT FK_HoaDon_PhieuKham FOREIGN KEY (MaPK) REFERENCES PhieuKham(MaPK),
    CONSTRAINT FK_HoaDon_ThuNgan FOREIGN KEY (MaNV_ThuNgan) REFERENCES NhanVien(MaNV)
);
GO

CREATE UNIQUE INDEX UX_HoaDon_MaPK_ChuaHuy ON HoaDon(MaPK) WHERE TrangThai <> N'Đã hủy';
GO

-- =============================================
-- THÊM DỮ LIỆU MẪU (SEED DATA)
-- =============================================

-- Nhân viên
INSERT INTO NhanVien (MaNV, HoTen, ChucVu, ChuyenKhoa, SoDienThoai, Email, TrangThai) VALUES
('NV001', N'Nguyễn Quản Trị', 'Admin', NULL, '0901234567', 'admin@phongkham.com', 1),
('NV002', N'BS. Trần Văn Nam', 'BacSi', N'Nội tổng quát', '0912345678', 'nam.tv@phongkham.com', 1),
('NV003', N'BS. Lê Thị Hoa', 'BacSi', N'Tai Mũi Họng', '0923456789', 'hoa.lt@phongkham.com', 1),
('NV004', N'Phạm Thu Ngân', 'ThuNgan', NULL, '0934567890', 'ngan.pt@phongkham.com', 1),
('NV005', N'Hoàng Lễ Tân', 'LeTan', NULL, '0945678901', 'tan.hl@phongkham.com', 1);

-- Mật khẩu mẫu '123456' được băm PBKDF2-SHA256 (210000 vòng).
INSERT INTO TaiKhoan (TenDangNhap, MatKhau, MaNV, Quyen, TrangThai) VALUES
('admin', 'PBKDF2$210000$Q2xpbmljRGVtb1NhbHQwMQ==$GjPrYMc9dhjD7oZPbijVodSow+Dwaf7ufWyQe/P3wYw=', 'NV001', 'Admin', 1),
('bacsi_nam', 'PBKDF2$210000$Q2xpbmljRGVtb1NhbHQwMQ==$GjPrYMc9dhjD7oZPbijVodSow+Dwaf7ufWyQe/P3wYw=', 'NV002', 'BacSi', 1),
('bacsi_hoa', 'PBKDF2$210000$Q2xpbmljRGVtb1NhbHQwMQ==$GjPrYMc9dhjD7oZPbijVodSow+Dwaf7ufWyQe/P3wYw=', 'NV003', 'BacSi', 1),
('thungan', 'PBKDF2$210000$Q2xpbmljRGVtb1NhbHQwMQ==$GjPrYMc9dhjD7oZPbijVodSow+Dwaf7ufWyQe/P3wYw=', 'NV004', 'ThuNgan', 1),
('letan', 'PBKDF2$210000$Q2xpbmljRGVtb1NhbHQwMQ==$GjPrYMc9dhjD7oZPbijVodSow+Dwaf7ufWyQe/P3wYw=', 'NV005', 'LeTan', 1);

INSERT INTO Khoa (MaKhoa, TenKhoa) VALUES
('KNOI', N'Nội tổng quát'),
('KTMH', N'Tai Mũi Họng'),
('KNHI', N'Nhi khoa');

INSERT INTO PhongKham (MaPhong, MaKhoa, TenPhong) VALUES
('P101', 'KNOI', N'Phòng Nội 101'),
('P102', 'KNOI', N'Phòng Nội 102'),
('P201', 'KTMH', N'Phòng Tai Mũi Họng 201'),
('P301', 'KNHI', N'Phòng Nhi 301');

INSERT INTO PhanCongPhong (MaNV, MaPhong, VaiTro) VALUES
('NV002', 'P101', N'BacSi'),
('NV003', 'P201', N'BacSi'),
('NV005', 'P101', N'LeTan');

-- Bệnh nhân
INSERT INTO BenhNhan (MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh) VALUES
('BN0001', N'Vũ Thị Mai', '1995-04-12', N'Nữ', '0988111222', N'Hà Nội', N'Dị ứng Penicillin'),
('BN0002', N'Đặng Văn Hùng', '1988-09-25', N'Nam', '0977333444', N'Hải Phòng', N'Tiểu đường type 2'),
('BN0003', N'Ngô Bảo Châu', '2002-01-18', N'Nam', '0966555666', N'Nam Định', N'Không');

INSERT INTO TaiKhoanBenhNhan (TenDangNhap, MatKhau, MaBN, TrangThai) VALUES
('benhnhan_mai', 'PBKDF2$210000$Q2xpbmljRGVtb1NhbHQwMQ==$GjPrYMc9dhjD7oZPbijVodSow+Dwaf7ufWyQe/P3wYw=', 'BN0001', 1),
('benhnhan_hung', 'PBKDF2$210000$Q2xpbmljRGVtb1NhbHQwMQ==$GjPrYMc9dhjD7oZPbijVodSow+Dwaf7ufWyQe/P3wYw=', 'BN0002', 1),
('benhnhan_chau', 'PBKDF2$210000$Q2xpbmljRGVtb1NhbHQwMQ==$GjPrYMc9dhjD7oZPbijVodSow+Dwaf7ufWyQe/P3wYw=', 'BN0003', 1);

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

PRINT N'Khởi tạo cơ sở dữ liệu $(DatabaseName) thành công!';
