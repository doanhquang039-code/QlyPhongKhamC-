using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using QlyPhongKham.Models;
using QlyPhongKham;

namespace QlyPhongKham.DAL
{
    public static class ClinicDAL
    {
        private static readonly object ClinicalSchemaLock = new();
        private static bool _clinicalRecordColumnsReady;
        private static readonly object ClinicRoomSchemaLock = new();
        private static bool _clinicRoomSchemaReady;
        private static readonly object ProfileImageSchemaLock = new();
        private static bool _profileImageSchemaReady;
        private static readonly object InvoiceSchemaLock = new();
        private static bool _invoiceSchemaReady;
        private static readonly object PharmacySchemaLock = new();
        private static bool _pharmacySchemaReady;
        private static readonly object AuditSchemaLock = new();
        private static bool _auditSchemaReady;
        private static readonly object AdvancedWorkflowSchemaLock = new();
        private static bool _advancedWorkflowSchemaReady;
        private static readonly object VisitLineageSchemaLock = new();
        private static bool _visitLineageSchemaReady;
        private static readonly object VitalSignsSchemaLock = new();
        private static bool _vitalSignsSchemaReady;

        private static void EnsureVitalSignsSchema()
        {
            if (_vitalSignsSchemaReady) return;
            lock (VitalSignsSchemaLock)
            {
                if (_vitalSignsSchemaReady) return;
                DatabaseHelper.ExecuteNonQuery(@"
                    IF OBJECT_ID(N'dbo.SinhHieuPhieuKham',N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.SinhHieuPhieuKham(
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
                            CONSTRAINT FK_SinhHieuPhieuKham_PhieuKham FOREIGN KEY(MaPK) REFERENCES dbo.PhieuKham(MaPK),
                            CONSTRAINT FK_SinhHieuPhieuKham_NhanVien FOREIGN KEY(MaNV_Do) REFERENCES dbo.NhanVien(MaNV)
                        );
                        CREATE INDEX IX_SinhHieuPhieuKham_MaPK_NgayDo ON dbo.SinhHieuPhieuKham(MaPK,NgayDo DESC);
                    END;");
                _vitalSignsSchemaReady = true;
            }
        }

        private static void LuuSinhHieu(SqlConnection connection, SqlTransaction transaction, string maPK,
            SinhHieu sinhHieu, string maNhanVien)
        {
            using var command = new SqlCommand(@"INSERT dbo.SinhHieuPhieuKham
                (MaPK,HuyetApTamThu,HuyetApTamTruong,Mach,NhietDo,Spo2,ChieuCao,CanNang,MaNV_Do)
                VALUES(@MaPK,@TamThu,@TamTruong,@Mach,@NhietDo,@Spo2,@ChieuCao,@CanNang,@MaNV)", connection, transaction);
            command.Parameters.AddWithValue("@MaPK", maPK);
            command.Parameters.AddWithValue("@TamThu", sinhHieu.HuyetApTamThu);
            command.Parameters.AddWithValue("@TamTruong", sinhHieu.HuyetApTamTruong);
            command.Parameters.AddWithValue("@Mach", sinhHieu.Mach);
            command.Parameters.AddWithValue("@NhietDo", sinhHieu.NhietDo);
            command.Parameters.AddWithValue("@Spo2", sinhHieu.Spo2);
            command.Parameters.AddWithValue("@ChieuCao", sinhHieu.ChieuCao);
            command.Parameters.AddWithValue("@CanNang", sinhHieu.CanNang);
            command.Parameters.AddWithValue("@MaNV", maNhanVien);
            command.ExecuteNonQuery();
        }

        private static void EnsureVisitLineageSchema()
        {
            if (_visitLineageSchemaReady) return;
            lock (VisitLineageSchemaLock)
            {
                if (_visitLineageSchemaReady) return;
                DatabaseHelper.ExecuteNonQuery(@"
                    IF COL_LENGTH(N'dbo.PhieuKham',N'MaPK_Goc') IS NULL
                        ALTER TABLE dbo.PhieuKham ADD MaPK_Goc VARCHAR(10) NULL;
                    IF COL_LENGTH(N'dbo.PhieuKham',N'LyDoChuyen') IS NULL
                        ALTER TABLE dbo.PhieuKham ADD LyDoChuyen NVARCHAR(500) NULL;
                    IF COL_LENGTH(N'dbo.LichHen',N'MaPK_TaiKham') IS NULL
                        ALTER TABLE dbo.LichHen ADD MaPK_TaiKham VARCHAR(10) NULL;
                    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.PhieuKham')
                        AND name=N'CK_PhieuKham_TrangThai' AND definition NOT LIKE N'%Đã chuyển phòng%')
                        ALTER TABLE dbo.PhieuKham DROP CONSTRAINT CK_PhieuKham_TrangThai;
                    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.PhieuKham')
                        AND name=N'CK_PhieuKham_TrangThai')
                        ALTER TABLE dbo.PhieuKham ADD CONSTRAINT CK_PhieuKham_TrangThai
                            CHECK(TrangThai IN(N'Chờ khám',N'Đang khám',N'Chờ kết quả CLS',N'Đã khám xong',N'Đã hủy',N'Đã chuyển phòng'));
                    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_PhieuKham_PhieuKhamGoc')
                        ALTER TABLE dbo.PhieuKham ADD CONSTRAINT FK_PhieuKham_PhieuKhamGoc
                            FOREIGN KEY(MaPK_Goc) REFERENCES dbo.PhieuKham(MaPK);
                    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_LichHen_PhieuKhamTaiKham')
                        ALTER TABLE dbo.LichHen ADD CONSTRAINT FK_LichHen_PhieuKhamTaiKham
                            FOREIGN KEY(MaPK_TaiKham) REFERENCES dbo.PhieuKham(MaPK);
                    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PhieuKham') AND name=N'IX_PhieuKham_MaPK_Goc')
                        CREATE INDEX IX_PhieuKham_MaPK_Goc ON dbo.PhieuKham(MaPK_Goc);
                    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LichHen') AND name=N'IX_LichHen_MaPK_TaiKham')
                        CREATE INDEX IX_LichHen_MaPK_TaiKham ON dbo.LichHen(MaPK_TaiKham);");
                _visitLineageSchemaReady = true;
            }
        }

        private static void EnsureAdvancedWorkflowSchema()
        {
            if (_advancedWorkflowSchemaReady) return;
            lock (AdvancedWorkflowSchemaLock)
            {
                if (_advancedWorkflowSchemaReady) return;
                EnsureInvoiceSchema();
                DatabaseHelper.ExecuteNonQuery(@"
                    IF OBJECT_ID(N'dbo.CaLamViecBacSi',N'U') IS NULL
                        CREATE TABLE dbo.CaLamViecBacSi(
                            MaCa INT IDENTITY(1,1) PRIMARY KEY,
                            MaNV VARCHAR(10) NOT NULL,
                            Thu TINYINT NOT NULL CHECK(Thu BETWEEN 1 AND 7),
                            GioBatDau TIME NOT NULL,
                            GioKetThuc TIME NOT NULL,
                            CONSTRAINT CK_CaLamViecBacSi_Gio CHECK(GioBatDau<GioKetThuc),
                            CONSTRAINT FK_CaLamViecBacSi_NhanVien FOREIGN KEY(MaNV) REFERENCES dbo.NhanVien(MaNV) ON DELETE CASCADE
                        );
                    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.CaLamViecBacSi') AND name=N'IX_CaLamViecBacSi_NhanVien_Thu')
                        CREATE INDEX IX_CaLamViecBacSi_NhanVien_Thu ON dbo.CaLamViecBacSi(MaNV,Thu,GioBatDau,GioKetThuc);
                    IF COL_LENGTH(N'dbo.ChiTietChiDinhDV',N'DaThanhToanCLS') IS NULL
                        ALTER TABLE dbo.ChiTietChiDinhDV ADD DaThanhToanCLS BIT NOT NULL
                            CONSTRAINT DF_ChiTietChiDinhDV_DaThanhToanCLS DEFAULT 1;
                    IF COL_LENGTH(N'dbo.ChiTietChiDinhDV',N'MaNV_ThucHien') IS NULL
                        ALTER TABLE dbo.ChiTietChiDinhDV ADD MaNV_ThucHien VARCHAR(10) NULL;
                    IF COL_LENGTH(N'dbo.ChiTietChiDinhDV',N'ThoiGianThucHien') IS NULL
                        ALTER TABLE dbo.ChiTietChiDinhDV ADD ThoiGianThucHien DATETIME2 NULL;
                    IF COL_LENGTH(N'dbo.ChiTietChiDinhDV',N'ThoiGianTraKetQua') IS NULL
                        ALTER TABLE dbo.ChiTietChiDinhDV ADD ThoiGianTraKetQua DATETIME2 NULL;
                    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_ChiDinh_NguoiThucHien')
                        ALTER TABLE dbo.ChiTietChiDinhDV ADD CONSTRAINT FK_ChiDinh_NguoiThucHien
                            FOREIGN KEY(MaNV_ThucHien) REFERENCES dbo.NhanVien(MaNV);
                    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.ChiTietChiDinhDV')
                        AND name=N'CK_ChiDinh_TrangThai' AND definition NOT LIKE N'%Đang thực hiện%')
                        ALTER TABLE dbo.ChiTietChiDinhDV DROP CONSTRAINT CK_ChiDinh_TrangThai;
                    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.ChiTietChiDinhDV')
                        AND name=N'CK_ChiDinh_TrangThai')
                        ALTER TABLE dbo.ChiTietChiDinhDV ADD CONSTRAINT CK_ChiDinh_TrangThai
                            CHECK(TrangThai IN(N'Chờ thực hiện',N'Đang thực hiện',N'Đã có kết quả'));
                    IF OBJECT_ID(N'dbo.TaiLieuCLS',N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.TaiLieuCLS(
                            MaTaiLieu BIGINT IDENTITY(1,1) PRIMARY KEY,
                            MaChiDinh INT NOT NULL,
                            TenTep NVARCHAR(255) NOT NULL,
                            LoaiNoiDung VARCHAR(100) NOT NULL,
                            DuLieu VARBINARY(MAX) NOT NULL,
                            KichThuoc BIGINT NOT NULL CHECK(KichThuoc>0),
                            MaNV_TaiLen VARCHAR(10) NOT NULL,
                            NgayTaiLen DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
                            CONSTRAINT FK_TaiLieuCLS_ChiDinh FOREIGN KEY(MaChiDinh) REFERENCES dbo.ChiTietChiDinhDV(MaChiDinh) ON DELETE CASCADE,
                            CONSTRAINT FK_TaiLieuCLS_NhanVien FOREIGN KEY(MaNV_TaiLen) REFERENCES dbo.NhanVien(MaNV)
                        );
                        CREATE INDEX IX_TaiLieuCLS_MaChiDinh ON dbo.TaiLieuCLS(MaChiDinh,NgayTaiLen DESC);
                    END;");
                EnsureDefaultDoctorShifts();
                DatabaseHelper.ExecuteNonQuery(@"
                    IF OBJECT_ID(N'dbo.CaThuNgan',N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.CaThuNgan(
                            MaCa BIGINT IDENTITY(1,1) PRIMARY KEY,
                            MaNV VARCHAR(10) NOT NULL,
                            ThoiGianMo DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
                            TienDauCa DECIMAL(18,2) NOT NULL CHECK(TienDauCa>=0),
                            ThoiGianDong DATETIME2 NULL,
                            TienThucTe DECIMAL(18,2) NULL,
                            GhiChuDongCa NVARCHAR(500) NULL,
                            CONSTRAINT FK_CaThuNgan_NhanVien FOREIGN KEY(MaNV) REFERENCES dbo.NhanVien(MaNV)
                        );
                        CREATE UNIQUE INDEX UX_CaThuNgan_MaNV_DangMo ON dbo.CaThuNgan(MaNV) WHERE ThoiGianDong IS NULL;
                    END;
                    IF OBJECT_ID(N'dbo.GiaoDichVienPhi',N'U') IS NULL
                        CREATE TABLE dbo.GiaoDichVienPhi(
                            MaGiaoDich BIGINT IDENTITY(1,1) PRIMARY KEY,
                            MaHD VARCHAR(10) NULL,
                            MaPK VARCHAR(10) NOT NULL,
                            MaCa BIGINT NULL,
                            MaNV VARCHAR(10) NOT NULL,
                            MaChiDinh INT NULL,
                            Loai NVARCHAR(20) NOT NULL CONSTRAINT CK_GiaoDichVienPhi_Loai CHECK(Loai IN(N'Thu',N'TamUng',N'ThuCLS',N'HoanTien')),
                            SoTien DECIMAL(18,2) NOT NULL CHECK(SoTien>0),
                            HinhThuc NVARCHAR(30) NOT NULL CHECK(HinhThuc IN(N'Tiền mặt',N'Chuyển khoản QR',N'Thẻ')),
                            ThoiGian DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
                            GhiChu NVARCHAR(500) NULL,
                            CONSTRAINT FK_GiaoDichVienPhi_HoaDon FOREIGN KEY(MaHD) REFERENCES dbo.HoaDon(MaHD),
                            CONSTRAINT FK_GiaoDichVienPhi_PhieuKham FOREIGN KEY(MaPK) REFERENCES dbo.PhieuKham(MaPK),
                            CONSTRAINT FK_GiaoDichVienPhi_ChiDinh FOREIGN KEY(MaChiDinh) REFERENCES dbo.ChiTietChiDinhDV(MaChiDinh),
                            CONSTRAINT FK_GiaoDichVienPhi_Ca FOREIGN KEY(MaCa) REFERENCES dbo.CaThuNgan(MaCa),
                            CONSTRAINT FK_GiaoDichVienPhi_NhanVien FOREIGN KEY(MaNV) REFERENCES dbo.NhanVien(MaNV)
                        );
                    IF COL_LENGTH(N'dbo.GiaoDichVienPhi',N'MaChiDinh') IS NULL
                        ALTER TABLE dbo.GiaoDichVienPhi ADD MaChiDinh INT NULL;
                    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_GiaoDichVienPhi_ChiDinh')
                        ALTER TABLE dbo.GiaoDichVienPhi ADD CONSTRAINT FK_GiaoDichVienPhi_ChiDinh
                            FOREIGN KEY(MaChiDinh) REFERENCES dbo.ChiTietChiDinhDV(MaChiDinh);
                    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.GiaoDichVienPhi')
                        AND name=N'CK_GiaoDichVienPhi_Loai' AND definition NOT LIKE N'%ThuCLS%')
                        ALTER TABLE dbo.GiaoDichVienPhi DROP CONSTRAINT CK_GiaoDichVienPhi_Loai;
                    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.GiaoDichVienPhi')
                        AND name=N'CK_GiaoDichVienPhi_Loai')
                        ALTER TABLE dbo.GiaoDichVienPhi ADD CONSTRAINT CK_GiaoDichVienPhi_Loai
                            CHECK(Loai IN(N'Thu',N'TamUng',N'ThuCLS',N'HoanTien'));
                    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.GiaoDichVienPhi') AND name=N'IX_GiaoDichVienPhi_MaHD')
                        CREATE INDEX IX_GiaoDichVienPhi_MaHD ON dbo.GiaoDichVienPhi(MaHD,ThoiGian);
                    IF OBJECT_ID(N'dbo.YeuCauHoanTien',N'U') IS NULL
                        CREATE TABLE dbo.YeuCauHoanTien(
                            MaYeuCau BIGINT IDENTITY(1,1) PRIMARY KEY,
                            MaHD VARCHAR(10) NOT NULL,
                            SoTien DECIMAL(18,2) NOT NULL CHECK(SoTien>0),
                            LyDo NVARCHAR(500) NOT NULL,
                            MaNV_DeNghi VARCHAR(10) NOT NULL,
                            NgayDeNghi DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
                            TrangThai NVARCHAR(20) NOT NULL DEFAULT N'Chờ duyệt'
                                CHECK(TrangThai IN(N'Chờ duyệt',N'Đã duyệt',N'Từ chối')),
                            MaNV_Duyet VARCHAR(10) NULL,
                            NgayDuyet DATETIME2 NULL,
                            GhiChuDuyet NVARCHAR(500) NULL,
                            CONSTRAINT FK_YeuCauHoanTien_HoaDon FOREIGN KEY(MaHD) REFERENCES dbo.HoaDon(MaHD),
                            CONSTRAINT FK_YeuCauHoanTien_NguoiDeNghi FOREIGN KEY(MaNV_DeNghi) REFERENCES dbo.NhanVien(MaNV),
                            CONSTRAINT FK_YeuCauHoanTien_NguoiDuyet FOREIGN KEY(MaNV_Duyet) REFERENCES dbo.NhanVien(MaNV)
                        );
                    INSERT INTO dbo.GiaoDichVienPhi(MaHD,MaPK,MaNV,Loai,SoTien,HinhThuc,ThoiGian,GhiChu)
                    SELECT hd.MaHD,hd.MaPK,hd.MaNV_ThuNgan,N'Thu',
                        hd.TongTien-hd.TienBaoHiem,hd.HinhThucTT,COALESCE(hd.NgayThanhToan,hd.NgayLap),N'Ghi nhận lịch sử hóa đơn đã thanh toán'
                    FROM dbo.HoaDon hd
                    WHERE hd.TrangThai=N'Đã thanh toán' AND hd.TongTien-hd.TienBaoHiem>0
                      AND NOT EXISTS(SELECT 1 FROM dbo.GiaoDichVienPhi gd WHERE gd.MaHD=hd.MaHD AND gd.Loai=N'Thu');");
                _advancedWorkflowSchemaReady = true;
            }
        }

        private static void EnsureDefaultDoctorShifts()
        {
            DatabaseHelper.ExecuteNonQuery(@"
                INSERT dbo.CaLamViecBacSi(MaNV,Thu,GioBatDau,GioKetThuc)
                SELECT nv.MaNV,days.Thu,shifts.GioBatDau,shifts.GioKetThuc
                FROM dbo.NhanVien nv
                CROSS JOIN (VALUES(1),(2),(3),(4),(5),(6)) days(Thu)
                CROSS JOIN (VALUES(CONVERT(time,'08:00'),CONVERT(time,'12:00')),
                                   (CONVERT(time,'13:00'),CONVERT(time,'17:00'))) shifts(GioBatDau,GioKetThuc)
                WHERE nv.ChucVu=N'BacSi' AND nv.TrangThai=1
                  AND NOT EXISTS(SELECT 1 FROM dbo.CaLamViecBacSi existing
                      WHERE existing.MaNV=nv.MaNV AND existing.Thu=days.Thu);");
        }

        private static bool CoQuyenTruyCapChiDinh(SqlConnection connection, SqlTransaction? transaction, int maChiDinh)
        {
            if (SessionState.IsAdmin) return true;
            if (!SessionState.IsDoctor || SessionState.CurrentUser?.MaNV is not string maNV) return false;
            using var command = new SqlCommand(@"SELECT COUNT(*) FROM ChiTietChiDinhDV c
                INNER JOIN PhieuKham pk ON pk.MaPK=c.MaPK
                WHERE c.MaChiDinh=@ID AND pk.MaNV_BacSi=@MaNV", connection, transaction);
            command.Parameters.AddWithValue("@ID", maChiDinh);
            command.Parameters.AddWithValue("@MaNV", maNV);
            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private static void EnsureBenhNhanEmailColumn()
        {
            try
            {
                DatabaseHelper.ExecuteNonQuery(@"IF COL_LENGTH('dbo.BenhNhan','Email') IS NULL
                    ALTER TABLE dbo.BenhNhan ADD Email VARCHAR(100) NULL;");
            }
            catch { }
        }
        private static void EnsureAuditSchema()
        {
EnsureBenhNhanEmailColumn();
            if (_auditSchemaReady) return;
            lock (AuditSchemaLock)
            {
                if (_auditSchemaReady) return;
                DatabaseHelper.ExecuteNonQuery(@"IF OBJECT_ID(N'dbo.NhatKyHeThong',N'U') IS NULL
                    CREATE TABLE dbo.NhatKyHeThong(MaNhatKy BIGINT IDENTITY(1,1) PRIMARY KEY,NgayGhi DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    MaNV VARCHAR(10) NULL,TenDangNhap NVARCHAR(100) NULL,HanhDong NVARCHAR(80) NOT NULL,DoiTuong NVARCHAR(120) NULL,
                    ChiTiet NVARCHAR(1000) NULL,MayTram NVARCHAR(128) NULL);");
                _auditSchemaReady = true;
            }
        }

        private static void GhiNhatKy(string action, string? objectId = null, string? details = null, string? username = null)
        {
            try
            {
                EnsureAuditSchema();
                DatabaseHelper.ExecuteNonQuery(@"INSERT NhatKyHeThong(MaNV,TenDangNhap,HanhDong,DoiTuong,ChiTiet,MayTram)
                    VALUES(@NV,@User,@Action,@Object,@Details,@Machine)",new[]{
                    new SqlParameter("@NV",(object?)SessionState.CurrentUser?.MaNV??DBNull.Value),
                    new SqlParameter("@User",(object?)username??(object?)SessionState.CurrentUser?.TenDangNhap??DBNull.Value),
                    new SqlParameter("@Action",action),new SqlParameter("@Object",(object?)objectId??DBNull.Value),
                    new SqlParameter("@Details",(object?)details??DBNull.Value),new SqlParameter("@Machine",Environment.MachineName)});
            }
            catch { /* Audit logging must not break a clinical transaction. */ }
        }

        public static DataTable LayNhatKyHeThong(DateTime tuNgay, DateTime denNgay, string keyword = "")
        {
            if (!SessionState.IsAdmin) throw new UnauthorizedAccessException("Chỉ quản trị viên được tra cứu nhật ký hệ thống.");
            EnsureAuditSchema();
            return DatabaseHelper.ExecuteQuery(@"SELECT TOP (500) NgayGhi,MaNV,TenDangNhap,HanhDong,DoiTuong,ChiTiet,MayTram
                FROM NhatKyHeThong WHERE NgayGhi>=@From AND NgayGhi<DATEADD(DAY,1,@To)
                AND (@Q=N'' OR HanhDong LIKE @Like OR DoiTuong LIKE @Like OR TenDangNhap LIKE @Like OR ChiTiet LIKE @Like)
                ORDER BY NgayGhi DESC",new[]{new SqlParameter("@From",tuNgay.Date),new SqlParameter("@To",denNgay.Date),
                new SqlParameter("@Q",keyword.Trim()),new SqlParameter("@Like","%"+keyword.Trim()+"%")});
        }

        public static void EnsurePharmacySchema()
        {
            if (_pharmacySchemaReady) return;
            lock (PharmacySchemaLock)
            {
                if (_pharmacySchemaReady) return;
                DatabaseHelper.ExecuteNonQuery(@"
                    SET XACT_ABORT ON;
                    BEGIN TRANSACTION;
                    IF OBJECT_ID(N'dbo.NhaCungCap',N'U') IS NULL
                        CREATE TABLE dbo.NhaCungCap(MaNCC INT IDENTITY(1,1) PRIMARY KEY,TenNCC NVARCHAR(150) NOT NULL UNIQUE,
                            SoDienThoai VARCHAR(20) NULL,DiaChi NVARCHAR(255) NULL,DangHoatDong BIT NOT NULL DEFAULT 1);
                    IF OBJECT_ID(N'dbo.PhieuNhapKho',N'U') IS NULL
                        CREATE TABLE dbo.PhieuNhapKho(MaPN VARCHAR(12) PRIMARY KEY,MaNCC INT NOT NULL,MaNV_Nhap VARCHAR(10) NOT NULL,
                            NgayNhap DATETIME NOT NULL DEFAULT GETDATE(),GhiChu NVARCHAR(255) NULL,
                            CONSTRAINT FK_PhieuNhapKho_NCC FOREIGN KEY(MaNCC) REFERENCES dbo.NhaCungCap(MaNCC),
                            CONSTRAINT FK_PhieuNhapKho_NV FOREIGN KEY(MaNV_Nhap) REFERENCES dbo.NhanVien(MaNV));
                    IF OBJECT_ID(N'dbo.LoThuoc',N'U') IS NULL
                        CREATE TABLE dbo.LoThuoc(MaLo INT IDENTITY(1,1) PRIMARY KEY,MaPN VARCHAR(12) NOT NULL,MaThuoc VARCHAR(10) NOT NULL,
                            SoLo NVARCHAR(60) NOT NULL,NgayHetHan DATE NOT NULL,DonGiaNhap DECIMAL(18,2) NOT NULL CHECK(DonGiaNhap>=0),
                            SoLuongNhap INT NOT NULL CHECK(SoLuongNhap>0),SoLuongCon INT NOT NULL,
                            CONSTRAINT CK_LoThuoc_SoLuongCon CHECK(SoLuongCon>=0 AND SoLuongCon<=SoLuongNhap),
                            CONSTRAINT FK_LoThuoc_PN FOREIGN KEY(MaPN) REFERENCES dbo.PhieuNhapKho(MaPN),
                            CONSTRAINT FK_LoThuoc_Thuoc FOREIGN KEY(MaThuoc) REFERENCES dbo.Thuoc(MaThuoc));
                    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LoThuoc') AND name=N'IX_LoThuoc_FEFO')
                        CREATE INDEX IX_LoThuoc_FEFO ON dbo.LoThuoc(MaThuoc,NgayHetHan,MaLo) INCLUDE(SoLuongCon);
                    IF OBJECT_ID(N'dbo.DieuChinhKho',N'U') IS NULL
                        CREATE TABLE dbo.DieuChinhKho(MaDieuChinh INT IDENTITY(1,1) PRIMARY KEY,MaLo INT NOT NULL,MaNV VARCHAR(10) NOT NULL,
                            SoLuongThayDoi INT NOT NULL CHECK(SoLuongThayDoi<>0),LyDo NVARCHAR(255) NOT NULL,NgayDieuChinh DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_DieuChinhKho_Lo FOREIGN KEY(MaLo) REFERENCES dbo.LoThuoc(MaLo),
                            CONSTRAINT FK_DieuChinhKho_NV FOREIGN KEY(MaNV) REFERENCES dbo.NhanVien(MaNV));
                    IF COL_LENGTH(N'dbo.DonThuoc',N'TrangThaiCapPhat') IS NULL
                        EXEC(N'ALTER TABLE dbo.DonThuoc ADD TrangThaiCapPhat NVARCHAR(20) NOT NULL CONSTRAINT DF_DonThuoc_TrangThaiCapPhat DEFAULT N''Chờ cấp phát'';');
                    IF COL_LENGTH(N'dbo.DonThuoc',N'NgayCapPhat') IS NULL EXEC(N'ALTER TABLE dbo.DonThuoc ADD NgayCapPhat DATETIME NULL;');
                    IF COL_LENGTH(N'dbo.DonThuoc',N'MaNV_CapPhat') IS NULL EXEC(N'ALTER TABLE dbo.DonThuoc ADD MaNV_CapPhat VARCHAR(10) NULL;');
                    IF COL_LENGTH(N'dbo.DonThuoc',N'GhiChuCapPhat') IS NULL EXEC(N'ALTER TABLE dbo.DonThuoc ADD GhiChuCapPhat NVARCHAR(255) NULL;');
                    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.DonThuoc') AND name=N'CK_DonThuoc_TrangThaiCapPhat')
                        EXEC(N'ALTER TABLE dbo.DonThuoc ADD CONSTRAINT CK_DonThuoc_TrangThaiCapPhat CHECK(TrangThaiCapPhat IN(N''Chờ cấp phát'',N''Đã cấp'',N''Không lấy'',N''Mua ngoài''));');
                    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_DonThuoc_NhanVienCapPhat')
                        EXEC(N'ALTER TABLE dbo.DonThuoc ADD CONSTRAINT FK_DonThuoc_NhanVienCapPhat FOREIGN KEY(MaNV_CapPhat) REFERENCES dbo.NhanVien(MaNV);');
                    IF OBJECT_ID(N'dbo.ChiTietCapPhatThuoc',N'U') IS NULL
                        CREATE TABLE dbo.ChiTietCapPhatThuoc(MaCapPhat INT IDENTITY(1,1) PRIMARY KEY,MaCTDT INT NOT NULL,MaLo INT NOT NULL,MaNV VARCHAR(10) NOT NULL,
                            SoLuong INT NOT NULL CHECK(SoLuong>0),NgayCapPhat DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_CapPhat_CT FOREIGN KEY(MaCTDT) REFERENCES dbo.ChiTietDonThuoc(MaCTDT),
                            CONSTRAINT FK_CapPhat_Lo FOREIGN KEY(MaLo) REFERENCES dbo.LoThuoc(MaLo),
                            CONSTRAINT FK_CapPhat_NV FOREIGN KEY(MaNV) REFERENCES dbo.NhanVien(MaNV));
                    DECLARE @ncc INT,@nv VARCHAR(10);
                    SELECT @ncc=MaNCC FROM dbo.NhaCungCap WHERE TenNCC=N'Tồn kho ban đầu';
                    IF @ncc IS NULL BEGIN INSERT dbo.NhaCungCap(TenNCC) VALUES(N'Tồn kho ban đầu'); SET @ncc=SCOPE_IDENTITY(); END;
                    SELECT TOP 1 @nv=MaNV FROM dbo.NhanVien WHERE TrangThai=1 ORDER BY CASE WHEN ChucVu=N'Admin' THEN 0 ELSE 1 END,MaNV;
                    ;WITH Missing AS(SELECT MaThuoc,SoLuongTon,ROW_NUMBER() OVER(ORDER BY MaThuoc) AS RN FROM dbo.Thuoc t
                        WHERE SoLuongTon>0 AND NOT EXISTS(SELECT 1 FROM dbo.LoThuoc l WHERE l.MaThuoc=t.MaThuoc))
                    INSERT dbo.PhieuNhapKho(MaPN,MaNCC,MaNV_Nhap,GhiChu)
                    SELECT 'LEG'+RIGHT('000000'+CONVERT(VARCHAR(6),RN),6),@ncc,@nv,N'Tồn kho chuyển đổi khi nâng cấp hệ thống' FROM Missing WHERE @nv IS NOT NULL;
                    ;WITH Missing AS(SELECT MaThuoc,SoLuongTon,ROW_NUMBER() OVER(ORDER BY MaThuoc) AS RN FROM dbo.Thuoc t
                        WHERE SoLuongTon>0 AND NOT EXISTS(SELECT 1 FROM dbo.LoThuoc l WHERE l.MaThuoc=t.MaThuoc))
                    INSERT dbo.LoThuoc(MaPN,MaThuoc,SoLo,NgayHetHan,DonGiaNhap,SoLuongNhap,SoLuongCon)
                    SELECT 'LEG'+RIGHT('000000'+CONVERT(VARCHAR(6),RN),6),MaThuoc,N'TỒN-ĐẦU',CONVERT(date,'20991231'),0,SoLuongTon,SoLuongTon
                    FROM Missing WHERE @nv IS NOT NULL;
                    COMMIT TRANSACTION;");
                _pharmacySchemaReady = true;
            }
        }

        public static void EnsureHealthRecordSchema()
        {
            DatabaseHelper.ExecuteNonQuery(@"
                IF OBJECT_ID(N'dbo.HoSoSucKhoe',N'U') IS NULL CREATE TABLE dbo.HoSoSucKhoe(
                    MaHoSo BIGINT IDENTITY(1,1) PRIMARY KEY,MaBN VARCHAR(10) NOT NULL,Loai NVARCHAR(30) NOT NULL,
                    NoiDung NVARCHAR(500) NOT NULL,NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE(),MaNV VARCHAR(10) NULL,
                    CONSTRAINT FK_HoSoSucKhoe_BN FOREIGN KEY(MaBN) REFERENCES dbo.BenhNhan(MaBN),
                    CONSTRAINT FK_HoSoSucKhoe_NV FOREIGN KEY(MaNV) REFERENCES dbo.NhanVien(MaNV));
                IF OBJECT_ID(N'dbo.DongYDuLieuCaNhan',N'U') IS NULL CREATE TABLE dbo.DongYDuLieuCaNhan(
                    MaBN VARCHAR(10) PRIMARY KEY,DongY BIT NOT NULL,NgayGhiNhan DATETIME NOT NULL DEFAULT GETDATE(),PhienBan NVARCHAR(30) NOT NULL,
                    CONSTRAINT FK_DongYDuLieu_BN FOREIGN KEY(MaBN) REFERENCES dbo.BenhNhan(MaBN));
                IF OBJECT_ID(N'dbo.DanhMucICD10',N'U') IS NULL CREATE TABLE dbo.DanhMucICD10(MaICD10 VARCHAR(10) PRIMARY KEY,TenChanDoan NVARCHAR(255) NOT NULL,ConHieuLuc BIT NOT NULL DEFAULT 1);");
        }

        public static DataTable LayHoSoSucKhoe(string maBN)
        {
            EnsureHealthRecordSchema();
            GhiNhatKy("Xem hồ sơ sức khỏe",maBN);
            return DatabaseHelper.ExecuteQuery("SELECT MaHoSo,Loai,NoiDung,NgayCapNhat,MaNV FROM HoSoSucKhoe WHERE MaBN=@BN ORDER BY Loai,NgayCapNhat DESC",new[]{new SqlParameter("@BN",maBN)});
        }

        public static void LuuMucHoSoSucKhoe(string maBN,string loai,string noiDung)
        {
            EnsureHealthRecordSchema();
            if (!new[]{"Dị ứng","Bệnh mạn tính","Thuốc đang dùng","Tiền sử phẫu thuật"}.Contains(loai) || string.IsNullOrWhiteSpace(noiDung) || noiDung.Trim().Length>500)
                throw new ArgumentException("Loại hồ sơ hoặc nội dung không hợp lệ.");
            if(!SessionState.IsAdmin && !SessionState.IsDoctor && !SessionState.IsReceptionist) throw new UnauthorizedAccessException("Không có quyền cập nhật hồ sơ sức khỏe.");
            DatabaseHelper.ExecuteNonQuery("INSERT HoSoSucKhoe(MaBN,Loai,NoiDung,MaNV) VALUES(@BN,@Type,@Text,@Staff)",new[]{
                new SqlParameter("@BN",maBN),new SqlParameter("@Type",loai),new SqlParameter("@Text",noiDung.Trim()),
                new SqlParameter("@Staff",(object?)SessionState.CurrentUser?.MaNV??DBNull.Value)});
            GhiNhatKy("Bổ sung hồ sơ sức khỏe",maBN,loai+": "+noiDung.Trim());
        }

        public static bool LuuDongYDuLieuCaNhan(string maBN,bool dongY,string phienBan)
        {
            EnsureHealthRecordSchema();
            if(string.IsNullOrWhiteSpace(phienBan)) throw new ArgumentException("Cần mã phiên bản nội dung đồng ý.");
            return DatabaseHelper.ExecuteNonQuery(@"MERGE DongYDuLieuCaNhan WITH(HOLDLOCK) AS t USING(SELECT @BN MaBN) s ON t.MaBN=s.MaBN
                WHEN MATCHED THEN UPDATE SET DongY=@Agree,NgayGhiNhan=GETDATE(),PhienBan=@Version
                WHEN NOT MATCHED THEN INSERT(MaBN,DongY,PhienBan) VALUES(@BN,@Agree,@Version);",new[]{
                new SqlParameter("@BN",maBN),new SqlParameter("@Agree",dongY),new SqlParameter("@Version",phienBan.Trim())})>0;
        }

        public static DataTable LayDanhMucICD10(string keyword="")
        {
            EnsureHealthRecordSchema();
            return DatabaseHelper.ExecuteQuery("SELECT MaICD10,TenChanDoan FROM DanhMucICD10 WHERE ConHieuLuc=1 AND (@Q=N'' OR MaICD10 LIKE @Like OR TenChanDoan LIKE @Like) ORDER BY MaICD10",new[]{
                new SqlParameter("@Q",keyword.Trim()),new SqlParameter("@Like","%"+keyword.Trim()+"%")});
        }

        private static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 210000, HashAlgorithmName.SHA256, 32);
            return $"PBKDF2$210000${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string saved, string supplied)
        {
            string[] parts = saved.Split('$');
            if (parts.Length == 4 && parts[0] == "PBKDF2" && int.TryParse(parts[1], out int iterations))
            {
                try
                {
                    byte[] expected = Convert.FromBase64String(parts[3]);
                    byte[] actual = Rfc2898DeriveBytes.Pbkdf2(supplied, Convert.FromBase64String(parts[2]), iterations, HashAlgorithmName.SHA256, expected.Length);
                    return CryptographicOperations.FixedTimeEquals(expected, actual);
                }
                catch { return false; }
            }
            return saved == supplied;
        }

        private static void UpgradePassword(string table, string username, string password)
        {
            string hash = HashPassword(password);
            DatabaseHelper.ExecuteNonQuery($"UPDATE {table} SET MatKhau=@Hash WHERE TenDangNhap=@User",
                new[] { new SqlParameter("@Hash", hash), new SqlParameter("@User", username) });
        }

        private static void EnsureInvoiceSchema()
        {
            if (_invoiceSchemaReady) return;
            lock (InvoiceSchemaLock)
            {
                if (_invoiceSchemaReady) return;
                DatabaseHelper.ExecuteNonQuery(@"
                    IF COL_LENGTH(N'dbo.HoaDon', N'PhanTramBaoHiem') IS NULL
                        ALTER TABLE dbo.HoaDon ADD PhanTramBaoHiem DECIMAL(5,2) NOT NULL CONSTRAINT DF_HoaDon_PhanTramBaoHiem DEFAULT 0;
                    IF COL_LENGTH(N'dbo.HoaDon', N'TienBaoHiem') IS NULL
                        ALTER TABLE dbo.HoaDon ADD TienBaoHiem DECIMAL(18,2) NOT NULL CONSTRAINT DF_HoaDon_TienBaoHiem DEFAULT 0;
                    IF COL_LENGTH(N'dbo.HoaDon', N'NgayThanhToan') IS NULL
                        ALTER TABLE dbo.HoaDon ADD NgayThanhToan DATETIME NULL;");
                DatabaseHelper.ExecuteNonQuery(@"
                    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.HoaDon') AND name=N'CK_HoaDon_PhanTramBaoHiem')
                        ALTER TABLE dbo.HoaDon ADD CONSTRAINT CK_HoaDon_PhanTramBaoHiem CHECK(PhanTramBaoHiem BETWEEN 0 AND 100);
                    DECLARE @uq sysname, @ddl nvarchar(1000);
                    SELECT TOP 1 @uq=i.name FROM sys.indexes i
                    INNER JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id
                    INNER JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
                    WHERE i.object_id=OBJECT_ID(N'dbo.HoaDon') AND i.is_unique=1 AND i.has_filter=0 AND c.name=N'MaPK';
                    IF @uq IS NOT NULL AND EXISTS (SELECT 1 FROM sys.key_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.HoaDon') AND name=@uq)
                    BEGIN
                        SET @ddl=N'ALTER TABLE dbo.HoaDon DROP CONSTRAINT ' + QUOTENAME(@uq);
                        EXEC sys.sp_executesql @ddl;
                    END
                    ELSE IF @uq IS NOT NULL
                    BEGIN
                        SET @ddl=N'DROP INDEX ' + QUOTENAME(@uq) + N' ON dbo.HoaDon';
                        EXEC sys.sp_executesql @ddl;
                    END
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.HoaDon') AND name=N'UX_HoaDon_MaPK_ChuaHuy')
                        CREATE UNIQUE INDEX UX_HoaDon_MaPK_ChuaHuy ON dbo.HoaDon(MaPK) WHERE TrangThai<>N'Đã hủy';");
                _invoiceSchemaReady = true;
            }
        }

        private static DataTable ExecutePagedQuery(string sql, SqlParameter[]? parameters, int? offset, int? pageSize)
        {
            if (offset.HasValue != pageSize.HasValue)
                throw new ArgumentException("Offset and page size must be provided together.");
            if (!pageSize.HasValue)
                return DatabaseHelper.ExecuteQuery(sql, parameters);
            if (!offset.HasValue)
                throw new ArgumentException("Offset and page size must be provided together.");
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");

            int selectIndex = sql.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
            if (selectIndex < 0)
                throw new ArgumentException("Paged SQL must contain a SELECT statement.", nameof(sql));

            string pagedSql = sql.Insert(selectIndex + "SELECT".Length, " COUNT_BIG(*) OVER() AS __TotalCount,");
            pagedSql += " OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";
            var queryParameters = parameters?.Select(p => (SqlParameter)((ICloneable)p).Clone()).ToList() ?? new List<SqlParameter>();
            queryParameters.Add(new SqlParameter("@PageOffset", offset.Value));
            queryParameters.Add(new SqlParameter("@PageSize", pageSize.Value));

            DataTable rows = DatabaseHelper.ExecuteQuery(pagedSql, queryParameters.ToArray());
            int totalCount = rows.Rows.Count == 0 ? 0 : checked((int)Convert.ToInt64(rows.Rows[0]["__TotalCount"]));
            rows.Columns.Remove("__TotalCount");
            rows.ExtendedProperties["TotalCount"] = totalCount;
            return rows;
        }

        private static void EnsureProfileImageSchema()
        {
            if (_profileImageSchemaReady) return;
            lock (ProfileImageSchemaLock)
            {
                if (_profileImageSchemaReady) return;

                const string sql = @"
                    SET XACT_ABORT ON;
                    BEGIN TRANSACTION;
                    IF COL_LENGTH(N'dbo.BenhNhan', N'AnhDaiDien') IS NULL
                        ALTER TABLE dbo.BenhNhan ADD AnhDaiDien VARBINARY(MAX) NULL;
                    IF COL_LENGTH(N'dbo.NhanVien', N'AnhDaiDien') IS NULL
                        ALTER TABLE dbo.NhanVien ADD AnhDaiDien VARBINARY(MAX) NULL;
                    COMMIT TRANSACTION;";
                DatabaseHelper.ExecuteNonQuery(sql);
                _profileImageSchemaReady = true;
            }
        }

        public static void EnsureClinicRoomSchema()
        {
            if (_clinicRoomSchemaReady) return;
            lock (ClinicRoomSchemaLock)
            {
                if (_clinicRoomSchemaReady) return;

                const string sql = @"
                    SET XACT_ABORT ON;
                    BEGIN TRANSACTION;

                    IF OBJECT_ID(N'dbo.Khoa', N'U') IS NULL
                        CREATE TABLE dbo.Khoa (
                            MaKhoa VARCHAR(10) NOT NULL PRIMARY KEY,
                            TenKhoa NVARCHAR(100) NOT NULL UNIQUE
                        );

                    IF OBJECT_ID(N'dbo.PhongKham', N'U') IS NULL
                        CREATE TABLE dbo.PhongKham (
                            MaPhong VARCHAR(10) NOT NULL PRIMARY KEY,
                            MaKhoa VARCHAR(10) NOT NULL,
                            TenPhong NVARCHAR(100) NOT NULL,
                            CONSTRAINT FK_PhongKham_Khoa FOREIGN KEY (MaKhoa) REFERENCES dbo.Khoa(MaKhoa)
                        );

                    IF OBJECT_ID(N'dbo.PhanCongPhong', N'U') IS NULL
                        CREATE TABLE dbo.PhanCongPhong (
                            MaNV VARCHAR(10) NOT NULL,
                            MaPhong VARCHAR(10) NOT NULL,
                            VaiTro NVARCHAR(20) NOT NULL,
                            CONSTRAINT PK_PhanCongPhong PRIMARY KEY (MaNV, MaPhong),
                            CONSTRAINT CK_PhanCongPhong_VaiTro CHECK (VaiTro IN (N'BacSi', N'LeTan')),
                            CONSTRAINT FK_PhanCongPhong_NhanVien FOREIGN KEY (MaNV) REFERENCES dbo.NhanVien(MaNV) ON DELETE CASCADE,
                            CONSTRAINT FK_PhanCongPhong_Phong FOREIGN KEY (MaPhong) REFERENCES dbo.PhongKham(MaPhong) ON DELETE CASCADE
                        );

                    IF COL_LENGTH(N'dbo.PhieuKham', N'MaPhong') IS NULL
                        EXEC(N'ALTER TABLE dbo.PhieuKham ADD MaPhong VARCHAR(10) NULL;');

                    IF COL_LENGTH(N'dbo.PhieuKham', N'MaLichHen') IS NULL
                        EXEC(N'ALTER TABLE dbo.PhieuKham ADD MaLichHen INT NULL;');
                    IF COL_LENGTH(N'dbo.PhieuKham', N'NgayKhamNgay') IS NULL
                        EXEC(N'ALTER TABLE dbo.PhieuKham ADD NgayKhamNgay AS CONVERT(DATE, NgayKham) PERSISTED;');

                    IF COL_LENGTH(N'dbo.LichHen', N'LyDoXuLy') IS NULL
                        EXEC(N'ALTER TABLE dbo.LichHen ADD LyDoXuLy NVARCHAR(255) NULL;');

                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhieuKham_Phong')
                        EXEC(N'ALTER TABLE dbo.PhieuKham ADD CONSTRAINT FK_PhieuKham_Phong FOREIGN KEY (MaPhong) REFERENCES dbo.PhongKham(MaPhong);');

                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhieuKham_LichHen')
                        EXEC(N'ALTER TABLE dbo.PhieuKham ADD CONSTRAINT FK_PhieuKham_LichHen FOREIGN KEY (MaLichHen) REFERENCES dbo.LichHen(MaLichHen);');
                    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.LichHen') AND name=N'CK_LichHen_TrangThai')
                        EXEC(N'ALTER TABLE dbo.LichHen DROP CONSTRAINT CK_LichHen_TrangThai;');
                    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.LichHen') AND name=N'CK_LichHen_TrangThai')
                        EXEC(N'ALTER TABLE dbo.LichHen ADD CONSTRAINT CK_LichHen_TrangThai CHECK (TrangThai IN (N''Chờ xác nhận'',N''Đã xác nhận'',N''Đã đến'',N''Đã hủy'',N''Bị từ chối'',N''Không đến''));');
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PhieuKham') AND name=N'UX_PhieuKham_MaLichHen')
                        EXEC(N'CREATE UNIQUE INDEX UX_PhieuKham_MaLichHen ON dbo.PhieuKham(MaLichHen) WHERE MaLichHen IS NOT NULL;');
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PhieuKham') AND name=N'UX_PhieuKham_Phong_Ngay_STT')
                        EXEC(N'CREATE UNIQUE INDEX UX_PhieuKham_Phong_Ngay_STT ON dbo.PhieuKham(MaPhong,NgayKhamNgay,STT) WHERE MaPhong IS NOT NULL;');
                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LichHen') AND name=N'UX_LichHen_BacSi_NgayHen_Active'
                        AND (filter_definition NOT LIKE N'%Bị từ chối%' OR filter_definition NOT LIKE N'%Không đến%'))
                        EXEC(N'DROP INDEX UX_LichHen_BacSi_NgayHen_Active ON dbo.LichHen;');
                    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LichHen') AND name=N'UX_LichHen_BacSi_NgayHen_Active')
                        EXEC(N'CREATE UNIQUE INDEX UX_LichHen_BacSi_NgayHen_Active ON dbo.LichHen(MaNV_BacSi,NgayHen) WHERE MaNV_BacSi IS NOT NULL AND TrangThai<>N''Đã hủy'' AND TrangThai<>N''Bị từ chối'' AND TrangThai<>N''Không đến'';');

                    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.NhanVien') AND name=N'CK_NhanVien_ChucVu')
                        ALTER TABLE dbo.NhanVien DROP CONSTRAINT CK_NhanVien_ChucVu;
                    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.TaiKhoan') AND name=N'CK_TaiKhoan_Quyen')
                        ALTER TABLE dbo.TaiKhoan DROP CONSTRAINT CK_TaiKhoan_Quyen;
                    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.NhanVien') AND name=N'CK_NhanVien_ChucVu')
                        ALTER TABLE dbo.NhanVien ADD CONSTRAINT CK_NhanVien_ChucVu CHECK(ChucVu IN(N'Admin',N'BacSi',N'LeTan',N'ThuNgan'));
                    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.TaiKhoan') AND name=N'CK_TaiKhoan_Quyen')
                        ALTER TABLE dbo.TaiKhoan ADD CONSTRAINT CK_TaiKhoan_Quyen CHECK(Quyen IN(N'Admin',N'BacSi',N'LeTan',N'ThuNgan'));

                    INSERT INTO dbo.PhanCongPhong(MaNV,MaPhong,VaiTro)
                    SELECT replacement.MaNV, oldAssignment.MaPhong, N'LeTan'
                    FROM dbo.PhanCongPhong oldAssignment
                    INNER JOIN dbo.TaiKhoan cashierAccount ON cashierAccount.MaNV=oldAssignment.MaNV
                        AND (cashierAccount.TenDangNhap=N'thungan' OR cashierAccount.Quyen=N'ThuNgan')
                    CROSS APPLY (SELECT TOP 1 candidate.MaNV
                        FROM dbo.NhanVien candidate
                        WHERE candidate.ChucVu=N'LeTan' AND candidate.TrangThai=1
                          AND candidate.MaNV<>oldAssignment.MaNV
                        ORDER BY candidate.MaNV) replacement
                    WHERE NOT EXISTS(SELECT 1 FROM dbo.PhanCongPhong existing
                        WHERE existing.MaNV=replacement.MaNV AND existing.MaPhong=oldAssignment.MaPhong);
                    DELETE assignment
                    FROM dbo.PhanCongPhong assignment
                    INNER JOIN dbo.TaiKhoan cashierAccount ON cashierAccount.MaNV=assignment.MaNV
                        AND (cashierAccount.TenDangNhap=N'thungan' OR cashierAccount.Quyen=N'ThuNgan');
                    UPDATE nv SET ChucVu=N'ThuNgan'
                    FROM dbo.NhanVien nv
                    INNER JOIN dbo.TaiKhoan tk ON tk.MaNV=nv.MaNV
                    WHERE tk.TenDangNhap=N'thungan' OR tk.Quyen=N'ThuNgan';
                    UPDATE tk SET Quyen=N'ThuNgan'
                    FROM dbo.TaiKhoan tk
                    INNER JOIN dbo.NhanVien nv ON nv.MaNV=tk.MaNV
                    WHERE nv.ChucVu=N'ThuNgan';

                    INSERT INTO dbo.Khoa (MaKhoa, TenKhoa)
                    SELECT v.MaKhoa, v.TenKhoa
                    FROM (VALUES
                        ('KNOI', N'Nội tổng quát'),
                        ('KTMH', N'Tai Mũi Họng'),
                        ('KNHI', N'Nhi khoa')
                    ) v(MaKhoa, TenKhoa)
                    WHERE NOT EXISTS (SELECT 1 FROM dbo.Khoa k WHERE k.MaKhoa = v.MaKhoa)
                      AND NOT EXISTS (SELECT 1 FROM dbo.Khoa k WHERE k.TenKhoa = v.TenKhoa);

                    INSERT INTO dbo.PhongKham (MaPhong, MaKhoa, TenPhong)
                    SELECT v.MaPhong, v.MaKhoa, v.TenPhong
                    FROM (VALUES
                        ('P101', 'KNOI', N'Nội tổng quát', N'Phòng Nội 101'),
                        ('P102', 'KNOI', N'Nội tổng quát', N'Phòng Nội 102'),
                        ('P201', 'KTMH', N'Tai Mũi Họng', N'Phòng Tai Mũi Họng 201'),
                        ('P301', 'KNHI', N'Nhi khoa', N'Phòng Nhi 301')
                    ) v(MaPhong, MaKhoa, TenKhoa, TenPhong)
                    WHERE NOT EXISTS (SELECT 1 FROM dbo.PhongKham p WHERE p.MaPhong = v.MaPhong)
                      AND EXISTS (
                          SELECT 1 FROM dbo.Khoa k
                          WHERE k.MaKhoa = v.MaKhoa AND k.TenKhoa = v.TenKhoa
                      );

                    COMMIT TRANSACTION;";
                DatabaseHelper.ExecuteNonQuery(sql);
                _clinicRoomSchemaReady = true;
            }
        }

        private static void EnsureClinicalRecordColumns()
        {
            if (_clinicalRecordColumnsReady) return;
            lock (ClinicalSchemaLock)
            {
                if (_clinicalRecordColumnsReady) return;

                const string sql = @"
                    IF COL_LENGTH('dbo.PhieuKham', 'KhamLamSang') IS NULL
                        ALTER TABLE dbo.PhieuKham ADD KhamLamSang NVARCHAR(MAX) NULL;
                    IF COL_LENGTH('dbo.PhieuKham', 'SinhHieu') IS NULL
                        ALTER TABLE dbo.PhieuKham ADD SinhHieu NVARCHAR(200) NULL;";
                DatabaseHelper.ExecuteNonQuery(sql);
                _clinicalRecordColumnsReady = true;
            }
        }

        // 1. TÀI KHOẢN & ĐĂNG NHẬP
        public static TaiKhoan? DangNhap(string username, string password)
        {
            string sql = @"
                SELECT tk.TenDangNhap, tk.MatKhau, tk.MaNV, tk.Quyen, tk.TrangThai, nv.HoTen
                FROM TaiKhoan tk
                INNER JOIN NhanVien nv ON tk.MaNV = nv.MaNV
                WHERE tk.TenDangNhap = @User AND tk.TrangThai = 1 AND nv.TrangThai = 1";

            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@User", username) });
            if (dt.Rows.Count == 0) { GhiNhatKy("Đăng nhập thất bại", username, "Tài khoản không hoạt động hoặc không tồn tại", username); return null; }

            var row = dt.Rows[0];
            string savedPass = row["MatKhau"].ToString() ?? "";
            
            // Cho phép mật khẩu plain-text hoặc so khớp trực tiếp
            if (!VerifyPassword(savedPass, password)) { GhiNhatKy("Đăng nhập thất bại", username, "Sai mật khẩu", username); return null; }
            if (!savedPass.StartsWith("PBKDF2$", StringComparison.Ordinal)) { UpgradePassword("TaiKhoan", username, password); savedPass = HashPassword(password); }

            GhiNhatKy("Đăng nhập thành công", row["MaNV"].ToString(), null, username);
            return new TaiKhoan
            {
                TenDangNhap = row["TenDangNhap"].ToString() ?? "",
                MatKhau = savedPass,
                MaNV = row["MaNV"].ToString() ?? "",
                Quyen = row["Quyen"].ToString() ?? "",
                TrangThai = Convert.ToBoolean(row["TrangThai"]),
                HoTenNhanVien = row["HoTen"].ToString()
            };
        }

        public static TaiKhoanBenhNhan? DangNhapBenhNhan(string username, string password)
        {
            const string sql = @"
                SELECT tk.TenDangNhap, tk.MaBN, bn.HoTen, bn.Email, tk.MatKhau
                FROM TaiKhoanBenhNhan tk
                INNER JOIN BenhNhan bn ON bn.MaBN = tk.MaBN
                WHERE tk.TenDangNhap = @User AND tk.TrangThai = 1";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@User", username) });
            if (dt.Rows.Count == 0 || !VerifyPassword(dt.Rows[0]["MatKhau"].ToString() ?? "", password))
                return null;
            if (!(dt.Rows[0]["MatKhau"].ToString() ?? "").StartsWith("PBKDF2$", StringComparison.Ordinal)) UpgradePassword("TaiKhoanBenhNhan", username, password);

            return new TaiKhoanBenhNhan
            {
                TenDangNhap = dt.Rows[0]["TenDangNhap"].ToString() ?? "",
                MaBN = dt.Rows[0]["MaBN"].ToString() ?? "",
                HoTen = dt.Rows[0]["HoTen"].ToString() ?? "",
                Email = dt.Rows[0]["Email"] != DBNull.Value ? dt.Rows[0]["Email"].ToString() : null
            };
        }

        public static void DangKyTaiKhoanBenhNhan(
            string username,
            string password,
            string fullName,
            DateTime? dateOfBirth,
            string gender,
            string? phone,
            string? address,
            string? email)
        {
            username = username.Trim();
            fullName = fullName.Trim();
            phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("Tên đăng nhập, mật khẩu và họ tên là bắt buộc.");
            if (username.Length > 50 || password.Length < 6 || fullName.Length > 100)
                throw new ArgumentException("Tên đăng nhập tối đa 50 ký tự, mật khẩu tối thiểu 6 ký tự và họ tên tối đa 100 ký tự.");
            if (gender is not ("Nam" or "Nữ"))
                throw new ArgumentException("Giới tính không hợp lệ.");
            if (dateOfBirth > DateTime.Today)
                throw new ArgumentException("Ngày sinh không được ở tương lai.");
            if (phone?.Length > 15 || address?.Length > 200)
                throw new ArgumentException("Số điện thoại tối đa 15 ký tự và địa chỉ tối đa 200 ký tự.");

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                using (var usernameCommand = new SqlCommand(@"
                    SELECT
                        (SELECT COUNT(*) FROM TaiKhoan WITH (UPDLOCK, HOLDLOCK) WHERE TenDangNhap = @Username) +
                        (SELECT COUNT(*) FROM TaiKhoanBenhNhan WITH (UPDLOCK, HOLDLOCK) WHERE TenDangNhap = @Username)",
                    conn, trans))
                {
                    usernameCommand.Parameters.AddWithValue("@Username", username);
                    if (Convert.ToInt32(usernameCommand.ExecuteScalar()) > 0)
                        throw new InvalidOperationException("Tên đăng nhập đã được sử dụng.");
                }

                string maBN;
                using (var codeCommand = new SqlCommand(@"
                    SELECT ISNULL(MAX(TRY_CONVERT(INT, SUBSTRING(MaBN, 3, 8))), 0) + 1
                    FROM BenhNhan WITH (UPDLOCK, HOLDLOCK)
                    WHERE MaBN LIKE 'BN[0-9]%'", conn, trans))
                {
                    int nextNumber = Convert.ToInt32(codeCommand.ExecuteScalar());
                    maBN = $"BN{nextNumber:D4}";
                    if (maBN.Length > 10)
                        throw new InvalidOperationException("Đã vượt quá số mã hồ sơ bệnh nhân có thể tạo.");
                }

                using (var patientCommand = new SqlCommand(@"
                    INSERT INTO BenhNhan (MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh, Email)
                    VALUES (@MaBN, @HoTen, @NgaySinh, @GioiTinh, @SoDienThoai, @DiaChi, NULL, @Email)",
                    conn, trans))
                {
                    patientCommand.Parameters.AddWithValue("@MaBN", maBN);
                    patientCommand.Parameters.AddWithValue("@HoTen", fullName);
                    patientCommand.Parameters.AddWithValue("@NgaySinh", (object?)dateOfBirth?.Date ?? DBNull.Value);
                    patientCommand.Parameters.AddWithValue("@GioiTinh", gender);
                    patientCommand.Parameters.AddWithValue("@SoDienThoai", (object?)phone ?? DBNull.Value);
                    patientCommand.Parameters.AddWithValue("@DiaChi", (object?)address ?? DBNull.Value);
                    patientCommand.Parameters.AddWithValue("@Email", (object?)email ?? DBNull.Value);
                    patientCommand.ExecuteNonQuery();
                }

                using (var accountCommand = new SqlCommand(@"
                    INSERT INTO TaiKhoanBenhNhan (TenDangNhap, MatKhau, MaBN, TrangThai)
                    VALUES (@Username, @Password, @MaBN, 1)", conn, trans))
                {
                    accountCommand.Parameters.AddWithValue("@Username", username);
                    accountCommand.Parameters.AddWithValue("@Password", HashPassword(password));
                    accountCommand.Parameters.AddWithValue("@MaBN", maBN);
                    accountCommand.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        // 2. BỆNH NHÂN
        public static DataTable LayDanhSachBenhNhan(string keyword = "", int? offset = null, int? pageSize = null)
        {
            string sql = @"
                SELECT MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, Email, DiaChi, TienSuBenh
                FROM BenhNhan
                WHERE @Kw = '' OR HoTen LIKE '%' + @Kw + '%' OR SoDienThoai LIKE '%' + @Kw + '%' OR MaBN LIKE '%' + @Kw + '%'
                ORDER BY NgayTao DESC, MaBN";
            return ExecutePagedQuery(sql, new[] { new SqlParameter("@Kw", keyword) }, offset, pageSize);
        }

        public static DataRow? LayBenhNhanTheoMa(string maBN)
        {
            const string sql = @"
                SELECT MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, Email, DiaChi, TienSuBenh
                FROM BenhNhan
                WHERE MaBN = @MaBN";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaBN", maBN) });
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static string SinhMaBenhNhan()
        {
            string sql = "SELECT TOP 1 MaBN FROM BenhNhan ORDER BY MaBN DESC";
            var result = DatabaseHelper.ExecuteScalar(sql);
            if (result == null || result == DBNull.Value) return "BN0001";
            string current = result.ToString()!;
            if (current.StartsWith("BN") && int.TryParse(current.Substring(2), out int num))
            {
                return $"BN{(num + 1):D4}";
            }
            return "BN0001";
        }

        public static bool LuuBenhNhan(BenhNhan bn, bool isNew)
        {
            EnsureProfileImageSchema();
            if (isNew)
            {
                string sql = @"
                    INSERT INTO BenhNhan (MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, Email, DiaChi, TienSuBenh, AnhDaiDien)
                    VALUES (@MaBN, @HoTen, @NgaySinh, @GioiTinh, @SoDienThoai, @Email, @DiaChi, @TienSuBenh, @AnhDaiDien)";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@MaBN", bn.MaBN),
                    new SqlParameter("@HoTen", bn.HoTen),
                    new SqlParameter("@NgaySinh", (object?)bn.NgaySinh ?? DBNull.Value),
                    new SqlParameter("@GioiTinh", bn.GioiTinh),
                    new SqlParameter("@SoDienThoai", (object?)bn.SoDienThoai ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)bn.Email ?? DBNull.Value),
                    new SqlParameter("@DiaChi", (object?)bn.DiaChi ?? DBNull.Value),
                    new SqlParameter("@TienSuBenh", (object?)bn.TienSuBenh ?? DBNull.Value),
                    CreateProfileImageParameter(bn.AnhDaiDien)
                }) > 0;
            }
            else
            {
                string sql = @"
                    UPDATE BenhNhan
                    SET HoTen = @HoTen, NgaySinh = COALESCE(@NgaySinh, NgaySinh), GioiTinh = @GioiTinh,
                        SoDienThoai = COALESCE(NULLIF(@SoDienThoai, ''), SoDienThoai),
                        Email = COALESCE(NULLIF(@Email, ''), Email),
                        DiaChi = COALESCE(NULLIF(@DiaChi, ''), DiaChi),
                        TienSuBenh = COALESCE(NULLIF(@TienSuBenh, ''), TienSuBenh),
                        AnhDaiDien = COALESCE(@AnhDaiDien, AnhDaiDien)
                    WHERE MaBN = @MaBN";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@MaBN", bn.MaBN),
                    new SqlParameter("@HoTen", bn.HoTen),
                    new SqlParameter("@NgaySinh", (object?)bn.NgaySinh ?? DBNull.Value),
                    new SqlParameter("@GioiTinh", bn.GioiTinh),
                    new SqlParameter("@SoDienThoai", (object?)bn.SoDienThoai ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)bn.Email ?? DBNull.Value),
                    new SqlParameter("@DiaChi", (object?)bn.DiaChi ?? DBNull.Value),
                    new SqlParameter("@TienSuBenh", (object?)bn.TienSuBenh ?? DBNull.Value),
                    CreateProfileImageParameter(bn.AnhDaiDien)
                }) > 0;
            }
        }

        public static byte[]? LayAnhBenhNhan(string maBN)
        {
            EnsureProfileImageSchema();
            object? result = DatabaseHelper.ExecuteScalar(
                "SELECT AnhDaiDien FROM BenhNhan WHERE MaBN = @MaBN",
                new[] { new SqlParameter("@MaBN", maBN) });
            return result is byte[] bytes ? bytes : null;
        }

        private static SqlParameter CreateProfileImageParameter(byte[]? imageBytes) =>
            new("@AnhDaiDien", SqlDbType.VarBinary, -1)
            {
                Value = (object?)imageBytes ?? DBNull.Value
            };

        // 3. TIẾP ĐÓN & HÀNG CHỜ KHÁM (STT trong ngày)
        public static int LaySTTTiepTheoTrongNgay(string? maPhong = null)
        {
            const string sql = @"
                SELECT ISNULL(MAX(STT), 0) + 1
                FROM PhieuKham
                WHERE CAST(NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                  AND (@MaPhong IS NULL OR MaPhong = @MaPhong)";
            var val = DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@MaPhong", (object?)maPhong ?? DBNull.Value)
            });
            return Convert.ToInt32(val);
        }

        public static string SinhMaPhieuKham()
        {
            string sql = "SELECT TOP 1 MaPK FROM PhieuKham ORDER BY MaPK DESC";
            var result = DatabaseHelper.ExecuteScalar(sql);
            if (result == null || result == DBNull.Value) return "PK0001";
            string current = result.ToString()!;
            if (current.StartsWith("PK") && int.TryParse(current.Substring(2), out int num))
            {
                return $"PK{(num + 1):D4}";
            }
            return "PK0001";
        }

        public static DataTable LayHangChoKhamTrongNgay(
            string? maBacSi = null,
            string? maPhong = null,
            string? maLeTan = null,
            int? offset = null,
            int? pageSize = null)
        {
            string sql = @"
                SELECT pk.MaPK, pk.STT, pk.NgayKham, pk.TrangThai,
                       bn.MaBN, bn.HoTen AS TenBN, bn.GioiTinh, CASE WHEN bn.NgaySinh IS NULL THEN NULL ELSE DATEDIFF(YEAR, bn.NgaySinh, GETDATE()) - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, bn.NgaySinh, GETDATE()), bn.NgaySinh) > CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END END AS Tuoi,
                       pk.TrieuChung, nv.HoTen AS TenBacSi, pk.MaNV_BacSi, pk.MaPhong,
                       p.TenPhong, k.TenKhoa
                FROM PhieuKham pk
                INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                LEFT JOIN PhongKham p ON p.MaPhong = pk.MaPhong
                LEFT JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                WHERE CAST(pk.NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                  AND (@MaBS IS NULL OR pk.MaNV_BacSi = @MaBS)
                  AND (@MaPhong IS NULL OR pk.MaPhong = @MaPhong)
                  AND (@MaLeTan IS NULL OR EXISTS (
                      SELECT 1 FROM PhanCongPhong pc
                      INNER JOIN NhanVien nv ON nv.MaNV = pc.MaNV
                      WHERE pc.MaNV = @MaLeTan AND pc.MaPhong = pk.MaPhong AND pc.VaiTro = N'LeTan'
                        AND nv.ChucVu = N'LeTan' AND nv.TrangThai = 1
                  ))
                ORDER BY pk.STT ASC, pk.MaPK";
            return ExecutePagedQuery(sql, new[]
            {
                new SqlParameter("@MaBS", (object?)maBacSi ?? DBNull.Value),
                new SqlParameter("@MaPhong", (object?)maPhong ?? DBNull.Value),
                new SqlParameter("@MaLeTan", (object?)maLeTan ?? DBNull.Value)
            }, offset, pageSize);
        }

        public static DataTable LayBangGoiSoTrongNgay()
        {
            return DatabaseHelper.ExecuteQuery(@"
                SELECT pk.STT,p.TenPhong,pk.TrangThai,pk.NgayKham
                FROM dbo.PhieuKham pk
                LEFT JOIN dbo.PhongKham p ON p.MaPhong=pk.MaPhong
                WHERE CAST(pk.NgayKham AS date)=CAST(GETDATE() AS date)
                  AND pk.TrangThai IN(N'Chờ khám',N'Đang khám',N'Chờ kết quả CLS')
                ORDER BY CASE pk.TrangThai WHEN N'Đang khám' THEN 0 WHEN N'Chờ kết quả CLS' THEN 1 ELSE 2 END,
                         pk.STT,pk.MaPK;");
        }

        public static bool TaoPhieuKham(PhieuKham pk, string maLeTan, SinhHieu? sinhHieu = null)
        {
            if (!SessionState.IsReceptionist || SessionState.CurrentUser?.MaNV != maLeTan)
                throw new UnauthorizedAccessException("Chỉ lễ tân đang đăng nhập được cấp phiếu khám.");
            EnsureClinicalRecordColumns();
            EnsureVitalSignsSchema();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                using (var lockRows = new SqlCommand("SELECT COUNT(*) FROM PhieuKham WITH (UPDLOCK,HOLDLOCK)", conn, tx))
                    lockRows.ExecuteScalar();
                int nextCode;
                using (var code = new SqlCommand("SELECT ISNULL(MAX(TRY_CONVERT(int,SUBSTRING(MaPK,3,8))),0)+1 FROM PhieuKham", conn, tx))
                    nextCode = Convert.ToInt32(code.ExecuteScalar());
                int nextNumber;
                using (var number = new SqlCommand("SELECT ISNULL(MAX(STT),0)+1 FROM PhieuKham WHERE CAST(NgayKham AS date)=CAST(GETDATE() AS date) AND MaPhong=@MaPhong", conn, tx))
                {
                    number.Parameters.AddWithValue("@MaPhong", (object?)pk.MaPhong ?? DBNull.Value);
                    nextNumber = Convert.ToInt32(number.ExecuteScalar());
                }
                pk.MaPK = $"PK{nextCode:D4}";
                pk.STT = nextNumber;
                const string sql = @"
                    INSERT INTO PhieuKham (MaPK, STT, MaBN, MaNV_BacSi, MaPhong, NgayKham, TrieuChung, SinhHieu, TrangThai)
                    SELECT @MaPK, @STT, @MaBN, @MaBS, @MaPhong, GETDATE(), @TrieuChung, @SinhHieu, N'Chờ khám'
                    WHERE EXISTS (SELECT 1 FROM PhanCongPhong pc INNER JOIN NhanVien nv ON nv.MaNV=pc.MaNV
                      WHERE pc.MaNV=@MaBS AND pc.MaPhong=@MaPhong AND pc.VaiTro=N'BacSi' AND nv.ChucVu=N'BacSi' AND nv.TrangThai=1)
                      AND EXISTS (SELECT 1 FROM PhanCongPhong pc INNER JOIN NhanVien nv ON nv.MaNV=pc.MaNV
                      WHERE pc.MaNV=@MaLeTan AND pc.MaPhong=@MaPhong AND pc.VaiTro=N'LeTan' AND nv.ChucVu=N'LeTan' AND nv.TrangThai=1)";
                using var insert = new SqlCommand(sql, conn, tx);
                insert.Parameters.AddWithValue("@MaPK", pk.MaPK); insert.Parameters.AddWithValue("@STT", pk.STT);
                insert.Parameters.AddWithValue("@MaBN", pk.MaBN); insert.Parameters.AddWithValue("@MaBS", pk.MaNV_BacSi);
                insert.Parameters.AddWithValue("@MaPhong", (object?)pk.MaPhong ?? DBNull.Value); insert.Parameters.AddWithValue("@MaLeTan", maLeTan);
                insert.Parameters.AddWithValue("@TrieuChung", (object?)pk.TrieuChung ?? DBNull.Value);
                insert.Parameters.AddWithValue("@SinhHieu", sinhHieu is null ? DBNull.Value : sinhHieu.ToSummary());
                if (insert.ExecuteNonQuery() != 1) { tx.Rollback(); return false; }
                if (sinhHieu is not null) LuuSinhHieu(conn, tx, pk.MaPK, sinhHieu, maLeTan);
                tx.Commit(); return true;
            }
            catch { tx.Rollback(); throw; }
        }

        public static bool HuyPhieuKhamChoKham(string maPK)
        {
            const string sql = @"
                UPDATE PhieuKham
                SET TrangThai = N'Đã hủy'
                WHERE MaPK = @MaPK
                  AND TrangThai = N'Chờ khám'
                  AND NOT EXISTS (SELECT 1 FROM HoaDon WHERE HoaDon.MaPK = PhieuKham.MaPK)";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK)
            }) > 0;
        }

        public static DataTable LayDanhSachBacSi()
        {
            string sql = "SELECT MaNV, HoTen, ChuyenKhoa FROM NhanVien WHERE ChucVu = 'BacSi' AND TrangThai = 1";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static DataTable LayDanhSachLichHen(string keyword = "", string? trangThai = null, int? offset = null, int? pageSize = null)
        {
            const string sql = @"
                SELECT lh.MaLichHen, lh.MaBN, bn.HoTen AS TenBN, lh.MaNV_BacSi,
                       nv.HoTen AS TenBacSi, lh.NgayHen, lh.LyDoKham, lh.LyDoXuLy, lh.TrangThai
                FROM LichHen lh
                INNER JOIN BenhNhan bn ON bn.MaBN = lh.MaBN
                LEFT JOIN NhanVien nv ON nv.MaNV = lh.MaNV_BacSi
                WHERE (@Kw = '' OR CONVERT(VARCHAR(12), lh.MaLichHen) LIKE '%' + @Kw + '%'
                    OR lh.MaBN LIKE '%' + @Kw + '%' OR bn.HoTen LIKE '%' + @Kw + '%'
                    OR nv.HoTen LIKE '%' + @Kw + '%')
                  AND (@TrangThai IS NULL OR @TrangThai = N'Tất cả trạng thái' OR lh.TrangThai = @TrangThai)
                ORDER BY lh.NgayHen DESC, lh.MaLichHen DESC";
            return ExecutePagedQuery(sql, new[]
            {
                new SqlParameter("@Kw", keyword.Trim()),
                new SqlParameter("@TrangThai", (object?)trangThai ?? DBNull.Value)
            }, offset, pageSize);
        }

        public static DataTable LayDanhSachLichHenLeTan(
            string maLeTan,
            DateTime? ngay,
            string keyword,
            string? trangThai,
            int offset,
            int pageSize,
            string? maBacSi = null)
        {
            const string sql = @"
                SELECT lh.MaLichHen, lh.MaBN, bn.HoTen AS TenBN, lh.MaNV_BacSi,
                       nv.HoTen AS TenBacSi, lh.NgayHen, lh.LyDoKham, lh.LyDoXuLy, lh.TrangThai,
                       (SELECT COUNT(*) FROM LichHen old WHERE old.MaBN=lh.MaBN AND old.TrangThai=N'Không đến') AS SoLanVangMat,
                       CASE lh.TrangThai
                           WHEN N'Chờ xác nhận' THEN N'Chờ duyệt'
                           WHEN N'Đã xác nhận' THEN N'Đã duyệt'
                           ELSE lh.TrangThai
                       END AS TrangThaiDuyet
                FROM LichHen lh
                INNER JOIN BenhNhan bn ON bn.MaBN = lh.MaBN
                LEFT JOIN NhanVien nv ON nv.MaNV = lh.MaNV_BacSi
                WHERE (@Ngay IS NULL OR (lh.NgayHen >= @Ngay AND lh.NgayHen < DATEADD(DAY, 1, @Ngay)))
                  AND (@Kw = '' OR CONVERT(VARCHAR(12), lh.MaLichHen) LIKE '%' + @Kw + '%'
                      OR lh.MaBN LIKE '%' + @Kw + '%' OR bn.HoTen LIKE '%' + @Kw + '%'
                      OR nv.HoTen LIKE '%' + @Kw + '%')
                  AND (@TrangThai IS NULL OR @TrangThai = N'Tất cả trạng thái' OR lh.TrangThai = @TrangThai)
                  AND (@MaBS IS NULL OR @MaBS = '' OR lh.MaNV_BacSi = @MaBS)
                  AND (
                      EXISTS (
                          SELECT 1
                          FROM PhanCongPhong pc
                          INNER JOIN PhanCongPhong doctorPc ON doctorPc.MaPhong = pc.MaPhong
                              AND doctorPc.VaiTro = N'BacSi'
                          INNER JOIN NhanVien receptionist ON receptionist.MaNV = pc.MaNV
                              AND receptionist.ChucVu = N'LeTan' AND receptionist.TrangThai = 1
                          INNER JOIN NhanVien doctor ON doctor.MaNV = doctorPc.MaNV
                              AND doctor.ChucVu = N'BacSi' AND doctor.TrangThai = 1
                          WHERE pc.MaNV = @MaLeTan AND pc.VaiTro = N'LeTan'
                            AND doctorPc.MaNV = lh.MaNV_BacSi
                      )
                      OR NOT EXISTS (SELECT 1 FROM PhanCongPhong WHERE MaNV = @MaLeTan AND VaiTro = N'LeTan')
                  )
                ORDER BY lh.NgayHen, lh.MaLichHen";
            return ExecutePagedQuery(sql, new[]
            {
                new SqlParameter("@MaLeTan", maLeTan),
                new SqlParameter("@Ngay", (object?)ngay?.Date ?? DBNull.Value),
                new SqlParameter("@Kw", keyword.Trim()),
                new SqlParameter("@TrangThai", (object?)trangThai ?? DBNull.Value),
                new SqlParameter("@MaBS", (object?)maBacSi ?? DBNull.Value)
            }, offset, pageSize);
        }

        public static int DanhDauLichHenKhongDenQuaHan(string maLeTan, int thoiGianChoPhut = 60)
        {
            if (!SessionState.IsAdmin && (!SessionState.IsReceptionist || SessionState.CurrentUser?.MaNV != maLeTan))
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật trạng thái lịch hẹn.");
            if (thoiGianChoPhut < 1 || thoiGianChoPhut > 1440)
                throw new ArgumentOutOfRangeException(nameof(thoiGianChoPhut));

            const string sql = @"
                UPDATE lh
                SET TrangThai=N'Không đến',
                    LyDoXuLy=COALESCE(NULLIF(LTRIM(RTRIM(LyDoXuLy)),N''),N'Tự động đánh dấu sau thời gian chờ 60 phút.')
                FROM LichHen lh
                WHERE lh.TrangThai=N'Đã xác nhận'
                  AND DATEADD(MINUTE,@GraceMinutes,lh.NgayHen)<=GETDATE()
                  AND (@MaLeTan IS NULL OR EXISTS(
                      SELECT 1 FROM PhanCongPhong rp
                      INNER JOIN PhanCongPhong dp ON dp.MaPhong=rp.MaPhong AND dp.VaiTro=N'BacSi'
                      WHERE rp.MaNV=@MaLeTan AND rp.VaiTro=N'LeTan' AND dp.MaNV=lh.MaNV_BacSi
                  ))";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@GraceMinutes", thoiGianChoPhut),
                new SqlParameter("@MaLeTan", SessionState.IsAdmin ? DBNull.Value : maLeTan)
            });
        }

        public static int LaySoLichHenChoDuyetLeTan(string maLeTan, DateTime? ngay = null, string? maBacSi = null)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM LichHen lh
                WHERE (@Ngay IS NULL OR (lh.NgayHen >= @Ngay AND lh.NgayHen < DATEADD(DAY, 1, @Ngay)))
                  AND lh.TrangThai = N'Chờ xác nhận'
                  AND (@MaBS IS NULL OR @MaBS = '' OR lh.MaNV_BacSi = @MaBS)
                  AND (
                      EXISTS (
                          SELECT 1
                          FROM PhanCongPhong pc
                          INNER JOIN PhanCongPhong doctorPc ON doctorPc.MaPhong = pc.MaPhong
                              AND doctorPc.VaiTro = N'BacSi'
                          INNER JOIN NhanVien receptionist ON receptionist.MaNV = pc.MaNV
                              AND receptionist.ChucVu = N'LeTan' AND receptionist.TrangThai = 1
                          INNER JOIN NhanVien doctor ON doctor.MaNV = doctorPc.MaNV
                              AND doctor.ChucVu = N'BacSi' AND doctor.TrangThai = 1
                          WHERE pc.MaNV = @MaLeTan AND pc.VaiTro = N'LeTan'
                            AND doctorPc.MaNV = lh.MaNV_BacSi
                      )
                      OR NOT EXISTS (SELECT 1 FROM PhanCongPhong WHERE MaNV = @MaLeTan AND VaiTro = N'LeTan')
                  )";
            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@MaLeTan", maLeTan),
                new SqlParameter("@Ngay", (object?)ngay?.Date ?? DBNull.Value),
                new SqlParameter("@MaBS", (object?)maBacSi ?? DBNull.Value)
            }));
        }

        public static bool DuyetLichHen(int maLichHen, string maLeTan)
        {
            if (!SessionState.IsReceptionist || SessionState.CurrentUser?.MaNV != maLeTan)
                throw new UnauthorizedAccessException("Chỉ lễ tân đang đăng nhập được duyệt lịch.");
            const string sql = @"
                UPDATE lh
                SET TrangThai = N'Đã xác nhận'
                FROM LichHen lh
                WHERE lh.MaLichHen = @MaLichHen
                  AND lh.TrangThai = N'Chờ xác nhận'
                  AND (
                      EXISTS (
                          SELECT 1
                          FROM NhanVien nv
                          INNER JOIN PhanCongPhong pc ON pc.MaNV = nv.MaNV AND pc.VaiTro = N'LeTan'
                          INNER JOIN PhanCongPhong doctorPc ON doctorPc.MaPhong = pc.MaPhong
                              AND doctorPc.VaiTro = N'BacSi'
                          WHERE nv.MaNV = @MaLeTan
                            AND nv.ChucVu = N'LeTan'
                            AND nv.TrangThai = 1
                            AND doctorPc.MaNV = lh.MaNV_BacSi
                      )
                      OR NOT EXISTS (SELECT 1 FROM PhanCongPhong WHERE MaNV = @MaLeTan AND VaiTro = N'LeTan')
                  )";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaLichHen", maLichHen),
                new SqlParameter("@MaLeTan", maLeTan)
            }) > 0;
        }

        public static (string maPK, int stt) CheckInLichHen(int maLichHen, string maLeTan, SinhHieu? sinhHieu = null)
        {
            if (!SessionState.IsReceptionist || SessionState.CurrentUser?.MaNV != maLeTan)
                throw new UnauthorizedAccessException("Chỉ lễ tân đang đăng nhập được check-in lịch.");
            EnsureClinicRoomSchema();
            EnsureClinicalRecordColumns();
            EnsureVitalSignsSchema();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                string maBN, maBS, maPhong, lyDo;
                using (var appointment = new SqlCommand(@"SELECT TOP 1 lh.MaBN,lh.MaNV_BacSi,lh.LyDoKham,pc.MaPhong
                    FROM LichHen lh WITH (UPDLOCK,HOLDLOCK)
                    INNER JOIN PhanCongPhong pc ON pc.MaNV=@MaLeTan AND pc.VaiTro=N'LeTan' AND pc.MaPhong IN
                        (SELECT MaPhong FROM PhanCongPhong WHERE MaNV=lh.MaNV_BacSi AND VaiTro=N'BacSi')
                    INNER JOIN NhanVien le ON le.MaNV=pc.MaNV AND le.ChucVu=N'LeTan' AND le.TrangThai=1
                    INNER JOIN NhanVien bs ON bs.MaNV=lh.MaNV_BacSi AND bs.ChucVu=N'BacSi' AND bs.TrangThai=1
                    WHERE lh.MaLichHen=@MaLichHen AND lh.TrangThai=N'Đã xác nhận' AND lh.NgayHen<=GETDATE()
                    ORDER BY pc.MaPhong", conn, tx))
                {
                    appointment.Parameters.AddWithValue("@MaLichHen", maLichHen);
                    appointment.Parameters.AddWithValue("@MaLeTan", maLeTan);
                    using var reader = appointment.ExecuteReader();
                    if (!reader.Read()) throw new InvalidOperationException("Lịch không còn ở trạng thái đã xác nhận hoặc không thuộc phòng được phân công.");
                    maBN = reader.GetString(0); maBS = reader.GetString(1); lyDo = reader.IsDBNull(2) ? "" : reader.GetString(2); maPhong = reader.GetString(3);
                }
                using (var locks = new SqlCommand("SELECT COUNT(*) FROM PhieuKham WITH (UPDLOCK,HOLDLOCK)", conn, tx)) locks.ExecuteScalar();
                int code;
                using (var cmd = new SqlCommand("SELECT ISNULL(MAX(TRY_CONVERT(int,SUBSTRING(MaPK,3,8))),0)+1 FROM PhieuKham", conn, tx)) code = Convert.ToInt32(cmd.ExecuteScalar());
                int stt;
                using (var cmd = new SqlCommand("SELECT ISNULL(MAX(STT),0)+1 FROM PhieuKham WHERE CAST(NgayKham AS date)=CAST(GETDATE() AS date) AND MaPhong=@MaPhong", conn, tx))
                { cmd.Parameters.AddWithValue("@MaPhong", maPhong); stt = Convert.ToInt32(cmd.ExecuteScalar()); }
                string maPK = $"PK{code:D4}";
                using (var insert = new SqlCommand(@"INSERT INTO PhieuKham(MaPK,STT,MaBN,MaNV_BacSi,MaPhong,MaLichHen,NgayKham,TrieuChung,SinhHieu,TrangThai)
                    VALUES(@MaPK,@STT,@MaBN,@MaBS,@MaPhong,@MaLichHen,GETDATE(),@LyDo,@SinhHieu,N'Chờ khám')", conn, tx))
                {
                    insert.Parameters.AddWithValue("@MaPK", maPK); insert.Parameters.AddWithValue("@STT", stt);
                    insert.Parameters.AddWithValue("@MaBN", maBN); insert.Parameters.AddWithValue("@MaBS", maBS);
                    insert.Parameters.AddWithValue("@MaPhong", maPhong); insert.Parameters.AddWithValue("@MaLichHen", maLichHen);
                    insert.Parameters.AddWithValue("@LyDo", (object?)lyDo ?? DBNull.Value);
                    insert.Parameters.AddWithValue("@SinhHieu", sinhHieu is null ? DBNull.Value : sinhHieu.ToSummary());
                    insert.ExecuteNonQuery();
                }
                if (sinhHieu is not null) LuuSinhHieu(conn, tx, maPK, sinhHieu, maLeTan);
                using (var update = new SqlCommand("UPDATE LichHen SET TrangThai=N'Đã đến' WHERE MaLichHen=@MaLichHen AND TrangThai=N'Đã xác nhận'", conn, tx))
                { update.Parameters.AddWithValue("@MaLichHen", maLichHen); if (update.ExecuteNonQuery()!=1) throw new InvalidOperationException("Không cập nhật được trạng thái check-in."); }
                tx.Commit(); return (maPK, stt);
            }
            catch { tx.Rollback(); throw; }
        }

        public static DataTable LayLichHenDaDuyetBacSi(string maBacSi, DateTime ngay)
        {
            const string sql = @"
                SELECT lh.MaLichHen, lh.NgayHen, lh.MaBN, bn.HoTen AS TenBN,
                       bn.SoDienThoai, lh.LyDoKham, lh.TrangThai,
                       room.TenPhong, room.TenKhoa
                FROM LichHen lh
                INNER JOIN BenhNhan bn ON bn.MaBN = lh.MaBN
                OUTER APPLY (
                    SELECT TOP 1 p.TenPhong, k.TenKhoa
                    FROM PhanCongPhong pc
                    INNER JOIN PhongKham p ON p.MaPhong = pc.MaPhong
                    INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                    WHERE pc.MaNV = lh.MaNV_BacSi AND pc.VaiTro = N'BacSi'
                    ORDER BY p.MaPhong
                ) room
                WHERE lh.MaNV_BacSi = @MaBacSi
                  AND lh.TrangThai = N'Đã xác nhận'
                  AND CAST(lh.NgayHen AS DATE) = @Ngay
                ORDER BY lh.NgayHen";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaBacSi", maBacSi),
                new SqlParameter("@Ngay", ngay.Date)
            });
        }

        public static DataTable LayLichHenBenhNhan(string maBN)
        {
            const string sql = @"
                SELECT lh.MaLichHen, lh.NgayHen, lh.LyDoKham, lh.TrangThai,
                       CASE
                           WHEN lh.TrangThai = N'Chờ xác nhận' THEN N'Chờ duyệt'
                           WHEN lh.TrangThai = N'Đã xác nhận' THEN N'Đã duyệt'
                           ELSE lh.TrangThai
                       END AS TrangThaiDuyet,
                       nv.MaNV AS MaBacSi, nv.HoTen AS TenBacSi, nv.ChuyenKhoa,
                       room.TenPhong, room.TenKhoa
                FROM LichHen lh
                LEFT JOIN NhanVien nv ON nv.MaNV = lh.MaNV_BacSi
                OUTER APPLY (
                    SELECT TOP 1 p.TenPhong, k.TenKhoa
                    FROM PhanCongPhong pc
                    INNER JOIN PhongKham p ON p.MaPhong = pc.MaPhong
                    INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                    WHERE pc.MaNV = nv.MaNV AND pc.VaiTro = N'BacSi'
                    ORDER BY p.MaPhong
                ) room
                WHERE lh.MaBN = @MaBN
                ORDER BY lh.NgayHen DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaBN", maBN)
            });
        }

        public static DataTable LayDanhSachBacSiDatLich()
        {
            const string sql = @"
                WITH DoctorRooms AS (
                    SELECT nv.MaNV, nv.HoTen, nv.ChuyenKhoa, p.MaPhong, p.TenPhong, k.TenKhoa,
                           ROW_NUMBER() OVER (PARTITION BY nv.MaNV ORDER BY p.MaPhong) AS RowNumber
                    FROM NhanVien nv
                    INNER JOIN PhanCongPhong pc ON pc.MaNV = nv.MaNV AND pc.VaiTro = N'BacSi'
                    INNER JOIN PhongKham p ON p.MaPhong = pc.MaPhong
                    INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                    WHERE nv.ChucVu = N'BacSi' AND nv.TrangThai = 1
                      AND nv.ChuyenKhoa = k.TenKhoa
                )
                SELECT MaNV, HoTen, ChuyenKhoa, MaPhong, TenPhong, TenKhoa
                FROM DoctorRooms
                WHERE RowNumber = 1
                ORDER BY TenKhoa, HoTen";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static bool DatLichHenBenhNhan(string maBN, string maBS, DateTime ngayHen, string lyDo, int? maLichHen = null)
        {
            ngayHen = new DateTime(ngayHen.Ticks - (ngayHen.Ticks % TimeSpan.TicksPerMinute), ngayHen.Kind);
            if (ngayHen <= DateTime.Now)
                throw new ArgumentException("Vui lòng chọn ngày giờ trong tương lai.");
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new ArgumentException("Vui lòng nhập lý do khám.");

            EnsureAdvancedWorkflowSchema();
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                using var doctorCommand = new SqlCommand(@"
                    SELECT COUNT(*)
                    FROM NhanVien nv
                    INNER JOIN PhanCongPhong pc ON pc.MaNV = nv.MaNV AND pc.VaiTro = N'BacSi'
                    INNER JOIN PhongKham p ON p.MaPhong = pc.MaPhong
                    INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                    WHERE nv.MaNV = @MaBS AND nv.ChucVu = N'BacSi'
                      AND nv.TrangThai = 1 AND nv.ChuyenKhoa = k.TenKhoa",
                    conn, trans);
                doctorCommand.Parameters.AddWithValue("@MaBS", maBS);
                if (Convert.ToInt32(doctorCommand.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("Bác sĩ hiện chưa có phòng khám được phân công.");

                using var shiftCommand = new SqlCommand(@"SELECT COUNT(*) FROM dbo.CaLamViecBacSi WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@MaBS AND Thu=@Thu AND @Gio>=GioBatDau
                      AND DATEADD(MINUTE,15,@Gio)<=GioKetThuc
                      AND DATEDIFF(MINUTE,GioBatDau,@Gio)%15=0", conn, trans);
                shiftCommand.Parameters.AddWithValue("@MaBS", maBS);
                shiftCommand.Parameters.AddWithValue("@Thu", ngayHen.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)ngayHen.DayOfWeek);
                shiftCommand.Parameters.Add("@Gio", System.Data.SqlDbType.Time).Value = ngayHen.TimeOfDay;
                if (Convert.ToInt32(shiftCommand.ExecuteScalar()) == 0)
                    throw new InvalidOperationException("Giờ hẹn không nằm trong ca làm việc hoặc không khớp slot 15 phút của bác sĩ.");

                using var duplicateCommand = new SqlCommand(@"
                    SELECT COUNT(*) FROM LichHen
                    WHERE MaNV_BacSi = @MaBS
                      AND NgayHen = @NgayHen
                      AND (@MaLichHen IS NULL OR MaLichHen <> @MaLichHen)
                      AND TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận', N'Đã đến')",
                    conn, trans);
                duplicateCommand.Parameters.AddWithValue("@MaBS", maBS);
                duplicateCommand.Parameters.AddWithValue("@NgayHen", ngayHen);
                duplicateCommand.Parameters.AddWithValue("@MaLichHen", (object?)maLichHen ?? DBNull.Value);
                if (Convert.ToInt32(duplicateCommand.ExecuteScalar()) > 0)
                    throw new InvalidOperationException("Khung giờ này đã có lịch hẹn. Vui lòng chọn giờ khác.");

                if (maLichHen.HasValue)
                {
                    using var updateCommand = new SqlCommand(@"
                        UPDATE LichHen
                        SET MaNV_BacSi = @MaBS, NgayHen = @NgayHen, LyDoKham = @LyDo
                        WHERE MaLichHen = @MaLichHen AND MaBN = @MaBN
                          AND TrangThai = N'Chờ xác nhận'",
                        conn, trans);
                    updateCommand.Parameters.AddWithValue("@MaLichHen", maLichHen.Value);
                    updateCommand.Parameters.AddWithValue("@MaBN", maBN);
                    updateCommand.Parameters.AddWithValue("@MaBS", maBS);
                    updateCommand.Parameters.AddWithValue("@NgayHen", ngayHen);
                    updateCommand.Parameters.AddWithValue("@LyDo", lyDo.Trim());
                    if (updateCommand.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Chỉ có thể sửa lịch hẹn đang chờ xác nhận của tài khoản này.");
                }
                else
                {
                    using var insertCommand = new SqlCommand(@"
                        INSERT INTO LichHen (MaBN, MaNV_BacSi, NgayHen, LyDoKham, TrangThai)
                        SELECT @MaBN, @MaBS, @NgayHen, @LyDo, N'Chờ xác nhận'
                        WHERE EXISTS (SELECT 1 FROM BenhNhan WHERE MaBN = @MaBN)",
                        conn, trans);
                    insertCommand.Parameters.AddWithValue("@MaBN", maBN);
                    insertCommand.Parameters.AddWithValue("@MaBS", maBS);
                    insertCommand.Parameters.AddWithValue("@NgayHen", ngayHen);
                    insertCommand.Parameters.AddWithValue("@LyDo", lyDo.Trim());
                    if (insertCommand.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Không tìm thấy hồ sơ bệnh nhân.");
                }

                trans.Commit();
                return true;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public static DataTable LayCaLamViecBacSi()
        {
            if (!SessionState.IsAdmin)
                throw new UnauthorizedAccessException("Chỉ quản trị viên được quản lý ca bác sĩ.");
            EnsureAdvancedWorkflowSchema();
            return DatabaseHelper.ExecuteQuery(@"SELECT c.MaCa,c.MaNV,nv.HoTen,
                    CASE c.Thu WHEN 1 THEN N'Thứ 2' WHEN 2 THEN N'Thứ 3' WHEN 3 THEN N'Thứ 4'
                        WHEN 4 THEN N'Thứ 5' WHEN 5 THEN N'Thứ 6' WHEN 6 THEN N'Thứ 7' ELSE N'Chủ nhật' END AS NgayTrongTuan,
                    c.Thu,c.GioBatDau,c.GioKetThuc
                FROM dbo.CaLamViecBacSi c
                INNER JOIN dbo.NhanVien nv ON nv.MaNV=c.MaNV
                ORDER BY nv.HoTen,c.Thu,c.GioBatDau");
        }

        public static bool LuuCaLamViecBacSi(string maBacSi, int thu, TimeSpan gioBatDau, TimeSpan gioKetThuc)
        {
            if (!SessionState.IsAdmin)
                throw new UnauthorizedAccessException("Chỉ quản trị viên được quản lý ca bác sĩ.");
            if (thu is < 1 or > 7 || gioBatDau < TimeSpan.Zero || gioKetThuc >= TimeSpan.FromDays(1)
                || gioKetThuc <= gioBatDau || gioBatDau.Ticks % TimeSpan.TicksPerMinute != 0
                || gioKetThuc.Ticks % TimeSpan.TicksPerMinute != 0
                || (int)gioBatDau.TotalMinutes % 15 != 0 || (int)gioKetThuc.TotalMinutes % 15 != 0
                || (gioKetThuc - gioBatDau).TotalMinutes < 15)
                throw new ArgumentException("Ca phải nằm trong một ngày, có thời lượng ít nhất 15 phút và khớp lưới 15 phút.");
            EnsureAdvancedWorkflowSchema();
            const string sql = @"
                IF NOT EXISTS(SELECT 1 FROM dbo.NhanVien nv WHERE nv.MaNV=@MaNV AND nv.ChucVu=N'BacSi' AND nv.TrangThai=1)
                    THROW 50001,N'Bác sĩ không hợp lệ hoặc đã nghỉ.',1;
                IF EXISTS(SELECT 1 FROM dbo.CaLamViecBacSi
                    WHERE MaNV=@MaNV AND Thu=@Thu AND @GioBatDau<GioKetThuc AND @GioKetThuc>GioBatDau)
                    THROW 50002,N'Ca làm việc bị trùng với ca đã cấu hình.',1;
                INSERT INTO dbo.CaLamViecBacSi(MaNV,Thu,GioBatDau,GioKetThuc)
                    VALUES(@MaNV,@Thu,@GioBatDau,@GioKetThuc);
                SELECT 1";
            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@MaNV", maBacSi),
                new SqlParameter("@Thu", thu),
                new SqlParameter("@GioBatDau", System.Data.SqlDbType.Time) { Value = gioBatDau },
                new SqlParameter("@GioKetThuc", System.Data.SqlDbType.Time) { Value = gioKetThuc }
            })) == 1;
        }

        public static bool XoaCaLamViecBacSi(int maCa)
        {
            if (!SessionState.IsAdmin)
                throw new UnauthorizedAccessException("Chỉ quản trị viên được quản lý ca bác sĩ.");
            EnsureAdvancedWorkflowSchema();
            const string sql = @"DELETE c FROM dbo.CaLamViecBacSi c
                WHERE c.MaCa=@ID
                  AND NOT EXISTS(SELECT 1 FROM dbo.LichHen lh
                    WHERE lh.MaNV_BacSi=c.MaNV AND lh.NgayHen>=GETDATE()
                      AND ((DATEPART(WEEKDAY,lh.NgayHen)+@@DATEFIRST-2)%7)+1=c.Thu
                      AND CONVERT(time,lh.NgayHen)>=c.GioBatDau AND CONVERT(time,lh.NgayHen)<c.GioKetThuc
                      AND lh.TrangThai IN(N'Chờ xác nhận',N'Đã xác nhận',N'Đã đến'))";
            return DatabaseHelper.ExecuteNonQuery(sql, new[] { new SqlParameter("@ID", maCa) }) > 0;
        }

        public static DataTable LayKhungGioKhaDung(string maBacSi, DateTime ngay, int? maLichHen = null)
        {
            EnsureAdvancedWorkflowSchema();
            EnsureDefaultDoctorShifts();
            int thu = ngay.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)ngay.DayOfWeek;
            DataTable shifts = DatabaseHelper.ExecuteQuery(@"SELECT GioBatDau,GioKetThuc
                FROM dbo.CaLamViecBacSi WHERE MaNV=@MaNV AND Thu=@Thu ORDER BY GioBatDau",
                new[] { new SqlParameter("@MaNV", maBacSi), new SqlParameter("@Thu", thu) });
            DataTable occupied = DatabaseHelper.ExecuteQuery(@"SELECT NgayHen FROM dbo.LichHen
                WHERE MaNV_BacSi=@MaNV AND CONVERT(date,NgayHen)=@Ngay
                  AND (@Exclude IS NULL OR MaLichHen<>@Exclude)
                  AND TrangThai IN(N'Chờ xác nhận',N'Đã xác nhận',N'Đã đến')",
                new[]
                {
                    new SqlParameter("@MaNV", maBacSi),
                    new SqlParameter("@Ngay", ngay.Date),
                    new SqlParameter("@Exclude", (object?)maLichHen ?? DBNull.Value)
                });
            var occupiedTimes = occupied.AsEnumerable()
                .Select(row => row.Field<DateTime>("NgayHen"))
                .ToHashSet();
            var slots = new DataTable();
            slots.Columns.Add("NgayGio", typeof(DateTime));
            DateTime now = DateTime.Now;
            foreach (DataRow shift in shifts.Rows)
            {
                TimeSpan start = (TimeSpan)shift["GioBatDau"];
                TimeSpan end = (TimeSpan)shift["GioKetThuc"];
                for (TimeSpan slot = start; slot.Add(TimeSpan.FromMinutes(15)) <= end; slot = slot.Add(TimeSpan.FromMinutes(15)))
                {
                    DateTime dateTime = ngay.Date.Add(slot);
                    if (dateTime <= now || occupiedTimes.Contains(dateTime))
                        continue;
                    slots.Rows.Add(dateTime);
                }
            }
            return slots;
        }

        public static DataTable LayLichTrucBacSi(string maBacSi)
        {
            EnsureAdvancedWorkflowSchema();
            EnsureDefaultDoctorShifts();
            return DatabaseHelper.ExecuteQuery(@"SELECT Thu,GioBatDau,GioKetThuc
                FROM dbo.CaLamViecBacSi
                WHERE MaNV=@MaNV
                ORDER BY Thu,GioBatDau",
                new[] { new SqlParameter("@MaNV", maBacSi) });
        }

        public static string MoTaLichTrucBacSi(string maBacSi)
        {
            DataTable shifts = LayLichTrucBacSi(maBacSi);
            if (shifts.Rows.Count == 0)
                return "Chưa có lịch trực.";
            string[] dayNames = { "", "T2", "T3", "T4", "T5", "T6", "T7", "CN" };
            var groupedDays = shifts.AsEnumerable()
                .GroupBy(row => row.Field<byte>("Thu"))
                .OrderBy(group => group.Key)
                .Select(group => new
                {
                    Day = group.Key,
                    Hours = string.Join(", ", group.Select(row =>
                        $"{((TimeSpan)row["GioBatDau"]).ToString(@"hh\:mm")}-{((TimeSpan)row["GioKetThuc"]).ToString(@"hh\:mm")}"))
                })
                .ToList();
            if (groupedDays.Count == 6
                && groupedDays.Select(group => group.Day).SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6 })
                && groupedDays.Select(group => group.Hours).Distinct().Count() == 1)
                return $"Lịch trực: T2-T7, {groupedDays[0].Hours}";
            return "Lịch trực: " + string.Join(" | ", groupedDays.Select(group => $"{dayNames[group.Day]} {group.Hours}"));
        }

        public static bool LuuLichHen(int? maLichHen, string maBN, string? maBS, DateTime ngayHen, string lyDo, string trangThai)
        {
            ngayHen = new DateTime(ngayHen.Ticks - (ngayHen.Ticks % TimeSpan.TicksPerMinute), ngayHen.Kind);
            if (!SessionState.IsAdmin && !SessionState.IsReceptionist)
                throw new UnauthorizedAccessException("Bạn không có quyền quản lý lịch hẹn.");
            EnsureAdvancedWorkflowSchema();
            const string sql = @"
                SET XACT_ABORT ON;
                SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
                BEGIN TRANSACTION;
                DECLARE @AffectedRows INT = 0;
                IF @TrangThai NOT IN (N'Chờ xác nhận', N'Đã xác nhận')
                   OR (@MaLeTan IS NOT NULL AND @TrangThai<>N'Chờ xác nhận')
                   OR NOT EXISTS (SELECT 1 FROM BenhNhan WHERE MaBN = @MaBN)
                   OR @MaBS IS NULL OR NOT EXISTS (
                       SELECT 1 FROM NhanVien nv INNER JOIN PhanCongPhong pc ON pc.MaNV=nv.MaNV AND pc.VaiTro=N'BacSi'
                       WHERE nv.MaNV=@MaBS AND nv.ChucVu=N'BacSi' AND nv.TrangThai=1
                   )
                   OR NOT EXISTS (
                       SELECT 1 FROM dbo.CaLamViecBacSi c
                       WHERE c.MaNV=@MaBS AND c.Thu=@Thu
                         AND @Gio>=c.GioBatDau AND DATEADD(MINUTE,15,@Gio)<=c.GioKetThuc
                         AND DATEDIFF(MINUTE,c.GioBatDau,@Gio)%15=0
                   )
                   OR (@MaLeTan IS NOT NULL AND NOT EXISTS (
                       SELECT 1 FROM PhanCongPhong receptionRoom
                       INNER JOIN PhanCongPhong doctorRoom ON doctorRoom.MaPhong=receptionRoom.MaPhong AND doctorRoom.VaiTro=N'BacSi'
                       WHERE receptionRoom.MaNV=@MaLeTan AND receptionRoom.VaiTro=N'LeTan' AND doctorRoom.MaNV=@MaBS
                   ))
                    SET @AffectedRows = 0;
                ELSE IF @MaBS IS NOT NULL AND EXISTS (
                    SELECT 1 FROM LichHen WITH (UPDLOCK,HOLDLOCK)
                    WHERE MaNV_BacSi=@MaBS AND NgayHen=@NgayHen
                      AND (@MaLichHen IS NULL OR MaLichHen<>@MaLichHen)
                      AND TrangThai IN (N'Chờ xác nhận',N'Đã xác nhận',N'Đã đến'))
                    SET @AffectedRows = 0;
                ELSE IF @MaLichHen IS NULL
                BEGIN
                    INSERT INTO LichHen (MaBN, MaNV_BacSi, NgayHen, LyDoKham, TrangThai)
                    VALUES (@MaBN, @MaBS, @NgayHen, @LyDoKham, @TrangThai);
                    SET @AffectedRows = @@ROWCOUNT;
                END
                ELSE
                BEGIN
                    UPDATE LichHen
                    SET MaBN = @MaBN, MaNV_BacSi = @MaBS, NgayHen = @NgayHen,
                        LyDoKham = @LyDoKham, TrangThai = @TrangThai
                    WHERE MaLichHen = @MaLichHen
                      AND TrangThai <> N'Đã hủy'
                      AND (@MaLeTan IS NULL OR TrangThai=N'Chờ xác nhận');
                    SET @AffectedRows = @@ROWCOUNT;
                END;
                SELECT @AffectedRows;
                COMMIT TRANSACTION;";
            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@MaLichHen", (object?)maLichHen ?? DBNull.Value),
                new SqlParameter("@MaLeTan", SessionState.IsReceptionist ? SessionState.CurrentUser!.MaNV : DBNull.Value),
                new SqlParameter("@MaBN", maBN),
                new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value),
                new SqlParameter("@NgayHen", ngayHen),
                new SqlParameter("@Thu", ngayHen.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)ngayHen.DayOfWeek),
                new SqlParameter("@Gio", System.Data.SqlDbType.Time) { Value = ngayHen.TimeOfDay },
                new SqlParameter("@LyDoKham", (object?)lyDo ?? DBNull.Value),
                new SqlParameter("@TrangThai", trangThai)
            })) > 0;
        }

        public static DateTime TaoLichTaiKham(string maPK, int soNgay)
        {
            if (!SessionState.IsDoctor)
                throw new UnauthorizedAccessException("Chỉ bác sĩ được tạo lịch tái khám.");
            if (soNgay is not (7 or 14 or 30))
                throw new ArgumentOutOfRangeException(nameof(soNgay), "Số ngày tái khám chỉ hỗ trợ 7, 14 hoặc 30 ngày.");

            EnsureVisitLineageSchema();
            string maBS = SessionState.CurrentUser!.MaNV;
            DataTable visit = DatabaseHelper.ExecuteQuery(
                @"SELECT MaBN,MaNV_BacSi,NgayKham,TrangThai FROM dbo.PhieuKham WHERE MaPK=@MaPK",
                new[] { new SqlParameter("@MaPK", maPK) });
            if (visit.Rows.Count != 1 || visit.Rows[0]["MaNV_BacSi"].ToString() != maBS)
                throw new InvalidOperationException("Không tìm thấy phiếu khám thuộc bác sĩ đang đăng nhập.");
            if (visit.Rows[0]["TrangThai"].ToString() != "Đã khám xong")
                throw new InvalidOperationException("Chỉ có thể tạo lịch tái khám từ phiếu đã hoàn tất.");

            DateTime followUpDate = DateTime.Today.AddDays(soNgay);
            DataTable slots = LayKhungGioKhaDung(maBS, followUpDate);
            if (slots.Rows.Count == 0)
                throw new InvalidOperationException("Bác sĩ chưa có slot 15 phút khả dụng vào ngày tái khám. Hãy cấu hình ca làm việc hoặc chọn lại sau.");
            DateTime dateTime = Convert.ToDateTime(slots.Rows[0]["NgayGio"]);
            string reason = $"Tái khám sau {soNgay} ngày (phiếu {maPK})";

            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                using var lockAppointment = new SqlCommand(
                    "SELECT COUNT(*) FROM dbo.LichHen WITH(UPDLOCK,HOLDLOCK) WHERE MaNV_BacSi=@MaBS AND NgayHen=@NgayHen AND TrangThai IN(N'Chờ xác nhận',N'Đã xác nhận',N'Đã đến')",
                    connection, transaction);
                lockAppointment.Parameters.AddWithValue("@MaBS", maBS);
                lockAppointment.Parameters.AddWithValue("@NgayHen", dateTime);
                if (Convert.ToInt32(lockAppointment.ExecuteScalar()) > 0)
                    throw new InvalidOperationException("Slot vừa được đặt bởi lịch hẹn khác. Vui lòng thử lại.");

                using var insert = new SqlCommand(@"
                    INSERT dbo.LichHen(MaBN,MaNV_BacSi,NgayHen,LyDoKham,TrangThai,MaPK_TaiKham)
                    VALUES(@MaBN,@MaBS,@NgayHen,@LyDo,N'Chờ xác nhận',@MaPK);", connection, transaction);
                insert.Parameters.AddWithValue("@MaBN", visit.Rows[0]["MaBN"].ToString() ?? "");
                insert.Parameters.AddWithValue("@MaBS", maBS);
                insert.Parameters.AddWithValue("@NgayHen", dateTime);
                insert.Parameters.AddWithValue("@LyDo", reason);
                insert.Parameters.AddWithValue("@MaPK", maPK);
                insert.ExecuteNonQuery();
                transaction.Commit();
            }
            catch
            {
                if (transaction.Connection != null)
                    transaction.Rollback();
                throw;
            }
            GhiNhatKy("Tạo lịch tái khám", maPK, dateTime.ToString("yyyy-MM-dd HH:mm"));
            return dateTime;
        }

        public static (string maPK, int stt) ChuyenPhongKham(string maPKGoc, string maBacSiMoi, string maPhongMoi, string lyDo)
        {
            if (!SessionState.IsDoctor)
                throw new UnauthorizedAccessException("Chỉ bác sĩ được chuyển phòng khám.");
            if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length > 500)
                throw new ArgumentException("Cần nhập lý do chuyển phòng (tối đa 500 ký tự).", nameof(lyDo));
            EnsureClinicRoomSchema();
            EnsureClinicalRecordColumns();
            EnsureVisitLineageSchema();

            string maBS = SessionState.CurrentUser!.MaNV;
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            string childId = "";
            int nextNumber = 0;
            try
            {
                string maBN;
                string? trieuChung;
                string? sinhHieu;
                string trangThai;
                using (var visit = new SqlCommand(@"
                    SELECT MaBN,TrieuChung,SinhHieu,TrangThai
                    FROM dbo.PhieuKham WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaPK=@MaPK AND MaNV_BacSi=@MaBS;", connection, transaction))
                {
                    visit.Parameters.AddWithValue("@MaPK", maPKGoc);
                    visit.Parameters.AddWithValue("@MaBS", maBS);
                    using var reader = visit.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException("Không tìm thấy phiếu khám thuộc bác sĩ đang đăng nhập.");
                    maBN = reader.GetString(0);
                    trieuChung = reader.IsDBNull(1) ? null : reader.GetString(1);
                    sinhHieu = reader.IsDBNull(2) ? null : reader.GetString(2);
                    trangThai = reader.GetString(3);
                }
                if (trangThai is not ("Chờ khám" or "Đang khám"))
                    throw new InvalidOperationException("Chỉ có thể chuyển phiếu đang chờ khám hoặc đang khám.");
                if (maBS == maBacSiMoi)
                    throw new InvalidOperationException("Hãy chọn bác sĩ khác để chuyển ca.");

                using (var assignment = new SqlCommand(@"
                    SELECT COUNT(*) FROM dbo.PhanCongPhong pc
                    INNER JOIN dbo.NhanVien nv ON nv.MaNV=pc.MaNV
                    WHERE pc.MaNV=@MaBS AND pc.MaPhong=@MaPhong AND pc.VaiTro=N'BacSi'
                      AND nv.ChucVu=N'BacSi' AND nv.TrangThai=1;", connection, transaction))
                {
                    assignment.Parameters.AddWithValue("@MaBS", maBacSiMoi);
                    assignment.Parameters.AddWithValue("@MaPhong", maPhongMoi);
                    if (Convert.ToInt32(assignment.ExecuteScalar()) != 1)
                        throw new InvalidOperationException("Bác sĩ không được phân công tại phòng đã chọn.");
                }

                int nextCode;
                using (var code = new SqlCommand("SELECT ISNULL(MAX(TRY_CONVERT(int,SUBSTRING(MaPK,3,8))),0)+1 FROM dbo.PhieuKham WITH(UPDLOCK,HOLDLOCK)", connection, transaction))
                    nextCode = Convert.ToInt32(code.ExecuteScalar());
                using (var number = new SqlCommand(@"
                    SELECT ISNULL(MAX(STT),0)+1 FROM dbo.PhieuKham WITH(UPDLOCK,HOLDLOCK)
                    WHERE CAST(NgayKham AS date)=CAST(GETDATE() AS date) AND MaPhong=@MaPhong;", connection, transaction))
                {
                    number.Parameters.AddWithValue("@MaPhong", maPhongMoi);
                    nextNumber = Convert.ToInt32(number.ExecuteScalar());
                }
                childId = $"PK{nextCode:D4}";
                using (var insert = new SqlCommand(@"
                    INSERT dbo.PhieuKham(MaPK,STT,MaBN,MaNV_BacSi,MaPhong,MaPK_Goc,LyDoChuyen,NgayKham,TrieuChung,SinhHieu,TrangThai)
                    VALUES(@MaPK,@STT,@MaBN,@MaBS,@MaPhong,@MaPKGoc,@LyDo,GETDATE(),@TrieuChung,@SinhHieu,N'Chờ khám');", connection, transaction))
                {
                    insert.Parameters.AddWithValue("@MaPK", childId);
                    insert.Parameters.AddWithValue("@STT", nextNumber);
                    insert.Parameters.AddWithValue("@MaBN", maBN);
                    insert.Parameters.AddWithValue("@MaBS", maBacSiMoi);
                    insert.Parameters.AddWithValue("@MaPhong", maPhongMoi);
                    insert.Parameters.AddWithValue("@MaPKGoc", maPKGoc);
                    insert.Parameters.AddWithValue("@LyDo", lyDo.Trim());
                    insert.Parameters.AddWithValue("@TrieuChung", (object?)trieuChung ?? DBNull.Value);
                    insert.Parameters.AddWithValue("@SinhHieu", (object?)sinhHieu ?? DBNull.Value);
                    insert.ExecuteNonQuery();
                }
                using (var update = new SqlCommand(@"
                    UPDATE dbo.PhieuKham SET TrangThai=N'Đã chuyển phòng'
                    WHERE MaPK=@MaPK AND MaNV_BacSi=@MaBS AND TrangThai IN(N'Chờ khám',N'Đang khám');", connection, transaction))
                {
                    update.Parameters.AddWithValue("@MaPK", maPKGoc);
                    update.Parameters.AddWithValue("@MaBS", maBS);
                    if (update.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Phiếu gốc đã thay đổi trạng thái; không thể chuyển phòng.");
                }
                transaction.Commit();
            }
            catch
            {
                if (transaction.Connection != null)
                    transaction.Rollback();
                throw;
            }
            GhiNhatKy("Chuyển phòng khám", maPKGoc, $"Phiếu mới {childId}; lý do: {lyDo.Trim()}");
            return (childId, nextNumber);
        }

        public static bool HuyLichHen(int maLichHen)
        {
            if (!SessionState.IsAdmin && !SessionState.IsReceptionist)
                throw new UnauthorizedAccessException("Bạn không có quyền hủy lịch hẹn.");
            const string sql = @"
                UPDATE LichHen
                SET TrangThai = N'Đã hủy'
                WHERE MaLichHen = @MaLichHen
                  AND TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận')
                  AND (@MaLeTan IS NULL OR EXISTS (SELECT 1 FROM PhanCongPhong rp INNER JOIN PhanCongPhong dp ON dp.MaPhong=rp.MaPhong AND dp.VaiTro=N'BacSi'
                      WHERE rp.MaNV=@MaLeTan AND rp.VaiTro=N'LeTan' AND dp.MaNV=LichHen.MaNV_BacSi))";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaLichHen", maLichHen),
                new SqlParameter("@MaLeTan", SessionState.IsReceptionist ? SessionState.CurrentUser!.MaNV : DBNull.Value)
            }) > 0;
        }

        public static bool KetThucLichHen(int maLichHen, string trangThai, string lyDo)
        {
            if (!SessionState.IsAdmin && !SessionState.IsReceptionist)
                throw new UnauthorizedAccessException("Bạn không có quyền xử lý lịch hẹn.");
            if (trangThai is not ("Bị từ chối" or "Không đến"))
                throw new ArgumentOutOfRangeException(nameof(trangThai));
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new ArgumentException("Cần nhập lý do xử lý lịch hẹn.", nameof(lyDo));
            if (lyDo.Trim().Length > 255)
                throw new ArgumentException("Lý do xử lý tối đa 255 ký tự.", nameof(lyDo));
            const string sql = @"UPDATE LichHen SET TrangThai=@TrangThai, LyDoXuLy=@LyDo
                WHERE MaLichHen=@MaLichHen AND TrangThai IN (N'Chờ xác nhận',N'Đã xác nhận')
                  AND (@TrangThai<>N'Không đến' OR NgayHen<GETDATE())
                  AND (@MaLeTan IS NULL OR EXISTS (SELECT 1 FROM PhanCongPhong rp INNER JOIN PhanCongPhong dp ON dp.MaPhong=rp.MaPhong AND dp.VaiTro=N'BacSi'
                      WHERE rp.MaNV=@MaLeTan AND rp.VaiTro=N'LeTan' AND dp.MaNV=LichHen.MaNV_BacSi))";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaLichHen", maLichHen), new SqlParameter("@TrangThai", trangThai), new SqlParameter("@LyDo", lyDo.Trim()),
                new SqlParameter("@MaLeTan", SessionState.IsReceptionist ? SessionState.CurrentUser!.MaNV : DBNull.Value)
            }) > 0;
        }

        public static DataTable LayDanhSachPhongTheoNhanVien(string maNV)
        {
            const string sql = @"
                SELECT p.MaPhong, p.TenPhong + N' - ' + k.TenKhoa AS TenPhong
                FROM PhanCongPhong pc
                INNER JOIN PhongKham p ON p.MaPhong = pc.MaPhong
                INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                INNER JOIN NhanVien nv ON nv.MaNV = pc.MaNV
                WHERE pc.MaNV = @MaNV
                  AND pc.VaiTro = nv.ChucVu
                  AND nv.TrangThai = 1
                ORDER BY k.TenKhoa, p.TenPhong";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaNV", maNV) });
        }

        public static DataTable LayDanhSachBacSiTheoPhong(string maPhong)
        {
            const string sql = @"
                SELECT nv.MaNV, nv.HoTen, nv.ChuyenKhoa
                FROM PhanCongPhong pc
                INNER JOIN NhanVien nv ON nv.MaNV = pc.MaNV
                WHERE pc.MaPhong = @MaPhong AND pc.VaiTro = N'BacSi'
                  AND nv.ChucVu = N'BacSi' AND nv.TrangThai = 1
                ORDER BY nv.HoTen";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaPhong", maPhong) });
        }

        public static DataTable LayDanhSachBacSiTheoNhanVien(string maNV)
        {
            const string sql = @"
                SELECT DISTINCT nv.MaNV, nv.HoTen, nv.ChuyenKhoa
                FROM PhanCongPhong pcLeTan
                INNER JOIN PhanCongPhong pcBacSi ON pcBacSi.MaPhong = pcLeTan.MaPhong
                    AND pcBacSi.VaiTro = N'BacSi'
                INNER JOIN NhanVien nv ON nv.MaNV = pcBacSi.MaNV
                WHERE pcLeTan.MaNV = @MaNV AND pcLeTan.VaiTro = N'LeTan'
                  AND nv.ChucVu = N'BacSi' AND nv.TrangThai = 1
                ORDER BY nv.HoTen";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaNV", maNV)
            });
        }

        public static (string maPhong, string tenPhong, string tenKhoa, int soBacSi, int soYTa) LayThongTinPhongNhanVien(string maNV)
        {
            const string sql = @"
                SELECT TOP 1
                    ISNULL(p.MaPhong, '') AS MaPhong,
                    ISNULL(p.TenPhong, N'Khu vực tiếp đón chung') AS TenPhong,
                    ISNULL(k.TenKhoa, N'Phòng khám đa khoa') AS TenKhoa,
                    (SELECT COUNT(DISTINCT pcBS.MaNV) FROM PhanCongPhong pcBS WHERE pcBS.MaPhong = pc.MaPhong AND pcBS.VaiTro = N'BacSi') AS SoBacSi,
                    (SELECT COUNT(DISTINCT pcYT.MaNV) FROM PhanCongPhong pcYT WHERE pcYT.MaPhong = pc.MaPhong AND pcYT.VaiTro = N'LeTan') AS SoYTa
                FROM dbo.PhanCongPhong pc
                INNER JOIN dbo.PhongKham p ON p.MaPhong = pc.MaPhong
                LEFT JOIN dbo.Khoa k ON k.MaKhoa = p.MaKhoa
                WHERE pc.MaNV = @MaNV
                ORDER BY pc.MaPhong";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaNV", maNV) });
            if (dt.Rows.Count > 0)
            {
                var r = dt.Rows[0];
                return (
                    r["MaPhong"].ToString() ?? "",
                    r["TenPhong"].ToString() ?? "",
                    r["TenKhoa"].ToString() ?? "",
                    Convert.ToInt32(r["SoBacSi"]),
                    Convert.ToInt32(r["SoYTa"])
                );
            }
            return ("", "Khu vực tiếp đón chung", "Phòng khám đa khoa", 0, 0);
        }

        public static DataTable LayDanhSachNhanVienCungPhong(string maNV)
        {
            const string sql = @"
                SELECT DISTINCT nv.MaNV, nv.HoTen, nv.ChucVu, nv.ChuyenKhoa, pc.VaiTro, p.TenPhong, k.TenKhoa
                FROM dbo.PhanCongPhong pcMy
                INNER JOIN dbo.PhanCongPhong pc ON pc.MaPhong = pcMy.MaPhong
                INNER JOIN dbo.NhanVien nv ON nv.MaNV = pc.MaNV
                INNER JOIN dbo.PhongKham p ON p.MaPhong = pc.MaPhong
                LEFT JOIN dbo.Khoa k ON k.MaKhoa = p.MaKhoa
                WHERE pcMy.MaNV = @MaNV AND nv.TrangThai = 1
                ORDER BY pc.VaiTro, nv.HoTen";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaNV", maNV) });
        }

        public static DataTable LayDanhSachPhongKham()
        {
            const string sql = @"
                SELECT p.MaPhong, p.MaKhoa, k.TenKhoa, p.TenPhong,
                       p.TenPhong + N' - ' + k.TenKhoa AS TenPhong
                FROM PhongKham p
                INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                ORDER BY k.TenKhoa, p.TenPhong";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static DataTable LayDanhSachKhoa()
        {
            return DatabaseHelper.ExecuteQuery("SELECT MaKhoa, TenKhoa FROM Khoa ORDER BY TenKhoa");
        }

        public static DataTable LayDanhSachKhoaQuanTri()
        {
            const string sql = @"
                SELECT k.MaKhoa, k.TenKhoa,
                       (SELECT COUNT(*) FROM PhongKham p WHERE p.MaKhoa = k.MaKhoa) AS SoPhong,
                       (SELECT COUNT(*) FROM NhanVien nv
                        WHERE nv.ChucVu = N'BacSi' AND nv.ChuyenKhoa = k.TenKhoa AND nv.TrangThai = 1) AS SoBacSi
                FROM Khoa k
                ORDER BY k.TenKhoa";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static DataTable LayDanhSachPhongKhamQuanTri()
        {
            const string sql = @"
                SELECT p.MaPhong, p.TenPhong, p.MaKhoa, k.TenKhoa,
                       (SELECT COUNT(*) FROM PhanCongPhong pc WHERE pc.MaPhong = p.MaPhong) AS SoNhanVien,
                       (SELECT COUNT(*) FROM PhieuKham pk WHERE pk.MaPhong = p.MaPhong) AS SoLuotKham
                FROM PhongKham p
                INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                ORDER BY k.TenKhoa, p.TenPhong";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static void LuuKhoa(string maKhoa, string tenKhoa, bool isNew)
        {
            if (string.IsNullOrWhiteSpace(maKhoa) || string.IsNullOrWhiteSpace(tenKhoa))
                throw new ArgumentException("Mã khoa và tên khoa là bắt buộc.");

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                string oldName = "";
                if (!isNew)
                {
                    using var readCommand = new SqlCommand(
                        "SELECT TenKhoa FROM Khoa WHERE MaKhoa = @MaKhoa", conn, trans);
                    readCommand.Parameters.AddWithValue("@MaKhoa", maKhoa.Trim());
                    oldName = readCommand.ExecuteScalar()?.ToString()
                        ?? throw new InvalidOperationException("Không tìm thấy khoa cần sửa.");
                }

                using var command = new SqlCommand(isNew
                    ? "INSERT INTO Khoa (MaKhoa, TenKhoa) VALUES (@MaKhoa, @TenKhoa)"
                    : "UPDATE Khoa SET TenKhoa = @TenKhoa WHERE MaKhoa = @MaKhoa", conn, trans);
                command.Parameters.AddWithValue("@MaKhoa", maKhoa.Trim());
                command.Parameters.AddWithValue("@TenKhoa", tenKhoa.Trim());
                if (command.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không lưu được thông tin khoa.");

                if (!isNew && !string.Equals(oldName, tenKhoa.Trim(), StringComparison.Ordinal))
                {
                    using var updateDoctors = new SqlCommand(
                        "UPDATE NhanVien SET ChuyenKhoa = @TenKhoa WHERE ChucVu = N'BacSi' AND ChuyenKhoa = @TenKhoaCu",
                        conn, trans);
                    updateDoctors.Parameters.AddWithValue("@TenKhoa", tenKhoa.Trim());
                    updateDoctors.Parameters.AddWithValue("@TenKhoaCu", oldName);
                    updateDoctors.ExecuteNonQuery();
                }

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public static void XoaKhoa(string maKhoa)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                using var usageCommand = new SqlCommand(@"
                    SELECT COUNT(*)
                    FROM Khoa k
                    WHERE k.MaKhoa = @MaKhoa
                      AND (EXISTS (SELECT 1 FROM PhongKham p WHERE p.MaKhoa = k.MaKhoa)
                           OR EXISTS (SELECT 1 FROM NhanVien nv
                                      WHERE nv.ChucVu = N'BacSi' AND nv.ChuyenKhoa = k.TenKhoa))",
                    conn, trans);
                usageCommand.Parameters.AddWithValue("@MaKhoa", maKhoa);
                if (Convert.ToInt32(usageCommand.ExecuteScalar()) > 0)
                    throw new InvalidOperationException("Không thể xóa khoa đang có phòng khám hoặc bác sĩ chuyên khoa. Hãy chuyển các dữ liệu liên quan trước.");

                using var deleteCommand = new SqlCommand(
                    "DELETE FROM Khoa WHERE MaKhoa = @MaKhoa", conn, trans);
                deleteCommand.Parameters.AddWithValue("@MaKhoa", maKhoa);
                if (deleteCommand.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không tìm thấy khoa cần xóa.");
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public static void LuuPhongKham(string maPhong, string tenPhong, string maKhoa, bool isNew)
        {
            if (string.IsNullOrWhiteSpace(maPhong) || string.IsNullOrWhiteSpace(tenPhong) || string.IsNullOrWhiteSpace(maKhoa))
                throw new ArgumentException("Mã phòng, tên phòng và chuyên khoa là bắt buộc.");

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                using var departmentCommand = new SqlCommand(
                    "SELECT TenKhoa FROM Khoa WHERE MaKhoa = @MaKhoa", conn, trans);
                departmentCommand.Parameters.AddWithValue("@MaKhoa", maKhoa.Trim());
                string departmentName = departmentCommand.ExecuteScalar()?.ToString()
                    ?? throw new InvalidOperationException("Chuyên khoa đã chọn không tồn tại.");

                if (!isNew)
                {
                    using var currentDepartmentCommand = new SqlCommand(
                        "SELECT MaKhoa FROM PhongKham WHERE MaPhong = @MaPhong", conn, trans);
                    currentDepartmentCommand.Parameters.AddWithValue("@MaPhong", maPhong.Trim());
                    string currentDepartmentId = currentDepartmentCommand.ExecuteScalar()?.ToString()
                        ?? throw new InvalidOperationException("Không tìm thấy phòng khám cần sửa.");

                    if (!string.Equals(currentDepartmentId, maKhoa.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        using var historyCommand = new SqlCommand(@"
                            SELECT COUNT(*) FROM PhieuKham
                            WHERE MaPhong = @MaPhong", conn, trans);
                        historyCommand.Parameters.AddWithValue("@MaPhong", maPhong.Trim());
                        if (Convert.ToInt32(historyCommand.ExecuteScalar()) > 0)
                            throw new InvalidOperationException("Không thể đổi chuyên khoa của phòng đã có lịch sử khám. Hãy tạo phòng mới để giữ nguyên báo cáo lịch sử.");

                        using var doctorCommand = new SqlCommand(@"
                            SELECT COUNT(*)
                            FROM PhanCongPhong pc
                            INNER JOIN NhanVien nv ON nv.MaNV = pc.MaNV
                            WHERE pc.MaPhong = @MaPhong AND pc.VaiTro = N'BacSi'
                              AND (nv.ChuyenKhoa IS NULL OR nv.ChuyenKhoa <> @TenKhoa)", conn, trans);
                        doctorCommand.Parameters.AddWithValue("@MaPhong", maPhong.Trim());
                        doctorCommand.Parameters.AddWithValue("@TenKhoa", departmentName);
                        if (Convert.ToInt32(doctorCommand.ExecuteScalar()) > 0)
                            throw new InvalidOperationException("Không thể chuyển phòng sang khoa khác vì đang có bác sĩ được phân công không thuộc chuyên khoa đó.");
                    }
                }

                using var command = new SqlCommand(isNew
                    ? "INSERT INTO PhongKham (MaPhong, TenPhong, MaKhoa) VALUES (@MaPhong, @TenPhong, @MaKhoa)"
                    : "UPDATE PhongKham SET TenPhong = @TenPhong, MaKhoa = @MaKhoa WHERE MaPhong = @MaPhong",
                    conn, trans);
                command.Parameters.AddWithValue("@MaPhong", maPhong.Trim());
                command.Parameters.AddWithValue("@TenPhong", tenPhong.Trim());
                command.Parameters.AddWithValue("@MaKhoa", maKhoa.Trim());
                if (command.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không lưu được thông tin phòng khám.");
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public static void XoaPhongKham(string maPhong)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                using var usageCommand = new SqlCommand(@"
                    SELECT
                        (SELECT COUNT(*) FROM PhanCongPhong WHERE MaPhong = @MaPhong) +
                        (SELECT COUNT(*) FROM PhieuKham WHERE MaPhong = @MaPhong)", conn, trans);
                usageCommand.Parameters.AddWithValue("@MaPhong", maPhong);
                if (Convert.ToInt32(usageCommand.ExecuteScalar()) > 0)
                    throw new InvalidOperationException("Không thể xóa phòng đang được phân công hoặc đã có phiếu khám. Hãy gỡ phân công trước; dữ liệu lịch sử được giữ lại.");

                using var deleteCommand = new SqlCommand(
                    "DELETE FROM PhongKham WHERE MaPhong = @MaPhong", conn, trans);
                deleteCommand.Parameters.AddWithValue("@MaPhong", maPhong);
                if (deleteCommand.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không tìm thấy phòng cần xóa.");
                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public static string LayChuyenKhoaNhanVien(string maNV)
        {
            const string sql = "SELECT ChuyenKhoa FROM NhanVien WHERE MaNV = @MaNV";
            return DatabaseHelper.ExecuteScalar(sql, new[] { new SqlParameter("@MaNV", maNV) })?.ToString() ?? "";
        }

        public static string LayChucVuNhanVien(string maNV)
        {
            const string sql = "SELECT ChucVu FROM NhanVien WHERE MaNV = @MaNV";
            return DatabaseHelper.ExecuteScalar(sql, new[] { new SqlParameter("@MaNV", maNV) })?.ToString() ?? "";
        }

        public static DataTable LayPhongPhanCongNhanVien(string maNV)
        {
            const string sql = @"
                SELECT pc.MaPhong
                FROM PhanCongPhong pc
                INNER JOIN NhanVien nv ON nv.MaNV = pc.MaNV
                WHERE pc.MaNV = @MaNV
                  AND pc.VaiTro = nv.ChucVu
                  AND nv.TrangThai = 1";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaNV", maNV) });
        }

        public static void LuuPhanCongPhong(string maNV, System.Collections.Generic.IEnumerable<string> maPhongs)
        {
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                string? role;
                string specialty;
                using (var roleCommand = new SqlCommand(
                    "SELECT ChucVu, ISNULL(ChuyenKhoa, N'') FROM NhanVien WHERE MaNV = @MaNV AND TrangThai = 1", conn, trans))
                {
                    roleCommand.Parameters.AddWithValue("@MaNV", maNV);
                    using var reader = roleCommand.ExecuteReader();
                    role = reader.Read() ? reader.GetString(0) : null;
                    specialty = role == null || reader.IsDBNull(1) ? "" : reader.GetString(1);
                }

                if (role is not ("BacSi" or "LeTan"))
                    throw new InvalidOperationException("Chỉ bác sĩ và lễ tân mới được phân công phòng khám.");

                using (var deleteCommand = new SqlCommand(
                    "DELETE FROM PhanCongPhong WHERE MaNV = @MaNV", conn, trans))
                {
                    deleteCommand.Parameters.AddWithValue("@MaNV", maNV);
                    deleteCommand.ExecuteNonQuery();
                }

                var distinctRooms = maPhongs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (distinctRooms.Count == 0)
                    throw new InvalidOperationException("Hãy phân công ít nhất một phòng cho bác sĩ hoặc lễ tân.");

                string? selectedSpecialty = null;
                foreach (string maPhong in distinctRooms)
                {
                    using (var specialtyCommand = new SqlCommand(@"
                        SELECT k.TenKhoa
                        FROM PhongKham p
                        INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                        WHERE p.MaPhong = @MaPhong", conn, trans))
                    {
                        specialtyCommand.Parameters.AddWithValue("@MaPhong", maPhong);
                        string? roomSpecialty = specialtyCommand.ExecuteScalar()?.ToString();
                        if (roomSpecialty == null)
                            throw new InvalidOperationException($"Không tìm thấy phòng khám {maPhong}.");

                        if (role == "BacSi")
                        {
                            if (selectedSpecialty != null &&
                                !string.Equals(selectedSpecialty, roomSpecialty, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("Mỗi bác sĩ chỉ được phân công vào một chuyên khoa chính. Hãy chọn các phòng thuộc cùng một khoa.");

                            selectedSpecialty = roomSpecialty;
                            if (!string.IsNullOrWhiteSpace(specialty) &&
                                !string.Equals(specialty.Trim(), roomSpecialty, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException($"Bác sĩ đã có chuyên khoa chính '{specialty}'. Không thể phân công sang khoa '{roomSpecialty}'.");
                        }
                    }

                    using var insertCommand = new SqlCommand(@"
                        INSERT INTO PhanCongPhong (MaNV, MaPhong, VaiTro)
                        SELECT @MaNV, p.MaPhong, @VaiTro
                        FROM PhongKham p
                        WHERE p.MaPhong = @MaPhong", conn, trans);
                    insertCommand.Parameters.AddWithValue("@MaNV", maNV);
                    insertCommand.Parameters.AddWithValue("@MaPhong", maPhong);
                    insertCommand.Parameters.AddWithValue("@VaiTro", role);
                    if (insertCommand.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException($"Không tìm thấy phòng khám {maPhong}.");
                }

                if (role == "BacSi" && string.IsNullOrWhiteSpace(specialty) && selectedSpecialty != null)
                {
                    using var updateSpecialty = new SqlCommand(
                    "UPDATE NhanVien SET ChuyenKhoa = @ChuyenKhoa WHERE MaNV = @MaNV", conn, trans);
                    updateSpecialty.Parameters.AddWithValue("@ChuyenKhoa", selectedSpecialty);
                    updateSpecialty.Parameters.AddWithValue("@MaNV", maNV);
                    updateSpecialty.ExecuteNonQuery();
                }

                trans.Commit();
                GhiNhatKy("Phân công phòng", maNV, string.Join(",", distinctRooms));
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public static bool HuyPhanCongPhong(string maNV)
        {
            var r = DatabaseHelper.ExecuteNonQuery(
                "DELETE FROM PhanCongPhong WHERE MaNV = @MaNV",
                new[] { new SqlParameter("@MaNV", maNV) }) > 0;
            if (r) GhiNhatKy("Hủy phân công phòng", maNV);
            return r;
        }

        public static DataTable LayDanhSachBenhNhanBacSi(string maBS, string keyword = "", int? offset = null, int? pageSize = null)
        {
            const string sql = @"
                SELECT bn.MaBN, bn.HoTen, bn.NgaySinh, bn.GioiTinh, bn.SoDienThoai,
                       MAX(pk.NgayKham) AS LanKhamGanNhat, COUNT(*) AS SoLanKham
                FROM PhieuKham pk
                INNER JOIN BenhNhan bn ON bn.MaBN = pk.MaBN
                WHERE pk.MaNV_BacSi = @MaBS
                  AND (@Kw = '' OR bn.MaBN LIKE '%' + @Kw + '%'
                       OR bn.HoTen LIKE '%' + @Kw + '%'
                       OR bn.SoDienThoai LIKE '%' + @Kw + '%')
                GROUP BY bn.MaBN, bn.HoTen, bn.NgaySinh, bn.GioiTinh, bn.SoDienThoai
                ORDER BY MAX(pk.NgayKham) DESC, bn.MaBN";
            return ExecutePagedQuery(sql, new[]
            {
                new SqlParameter("@MaBS", maBS),
                new SqlParameter("@Kw", keyword.Trim())
            }, offset, pageSize);
        }

        public static bool HuyLichHenBenhNhan(int maLichHen, string maBN)
        {
            const string sql = @"
                UPDATE LichHen
                SET TrangThai = N'Đã hủy'
                WHERE MaLichHen = @MaLichHen AND MaBN = @MaBN
                  AND TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận')";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaLichHen", maLichHen),
                new SqlParameter("@MaBN", maBN)
            }) > 0;
        }

        public static bool BacSiDuocPhanCongBenhNhan(string maBS, string maBN)
        {
            const string sql = @"
                SELECT COUNT(*)
                FROM PhieuKham
                WHERE MaNV_BacSi = @MaBS AND MaBN = @MaBN";
            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@MaBS", maBS),
                new SqlParameter("@MaBN", maBN)
            })) > 0;
        }

        // 4. KHÁM BỆNH & CẬN LÂM SÀNG
        public static bool CapNhatKetQuaKham(string maPK, string maBS, string trieuChung, string khamLamSang, string sinhHieu, string chanDoan, string loiDan, string trangThai)
        {
            if (!SessionState.IsDoctor || SessionState.CurrentUser?.MaNV != maBS)
                throw new UnauthorizedAccessException("Bác sĩ chỉ được cập nhật phiếu khám được phân công cho mình.");
            EnsureClinicalRecordColumns();
            string sql = @"
                UPDATE PhieuKham
                SET TrieuChung = @TrieuChung, KhamLamSang = @KhamLamSang, SinhHieu = @SinhHieu,
                    ChanDoan = @ChanDoan, LoiDan = @LoiDan, TrangThai = N'Đang khám'
                WHERE MaPK = @MaPK AND MaNV_BacSi = @MaBS AND TrangThai IN (N'Chờ khám', N'Đang khám')
                  AND NOT EXISTS (SELECT 1 FROM HoaDon WHERE HoaDon.MaPK=PhieuKham.MaPK AND TrangThai<>N'Đã hủy')";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
                new SqlParameter("@MaBS", maBS),
                new SqlParameter("@TrieuChung", trieuChung),
                new SqlParameter("@KhamLamSang", (object?)khamLamSang ?? DBNull.Value),
                new SqlParameter("@SinhHieu", (object?)sinhHieu ?? DBNull.Value),
                new SqlParameter("@ChanDoan", chanDoan),
                new SqlParameter("@LoiDan", (object?)loiDan ?? DBNull.Value),
                new SqlParameter("@TrangThai", trangThai)
            }) > 0;
        }

        public static bool CapNhatTrangThaiPhieuKham(string maPK, string maBS, string trangThai)
        {
            if (!SessionState.IsDoctor || SessionState.CurrentUser?.MaNV != maBS)
                throw new UnauthorizedAccessException("Bác sĩ chỉ được cập nhật phiếu khám được phân công cho mình.");
            const string sql = @"UPDATE PhieuKham SET TrangThai = @TrangThai WHERE MaPK = @MaPK AND MaNV_BacSi = @MaBS
                AND TrangThai IN (N'Chờ khám', N'Đang khám') AND NOT EXISTS (SELECT 1 FROM HoaDon WHERE HoaDon.MaPK=PhieuKham.MaPK AND TrangThai<>N'Đã hủy')
                AND (@TrangThai<>N'Đã khám xong' OR NOT EXISTS (SELECT 1 FROM ChiTietChiDinhDV c WHERE c.MaPK=PhieuKham.MaPK AND c.TrangThai<>N'Đã có kết quả'))";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
                new SqlParameter("@MaBS", maBS),
                new SqlParameter("@TrangThai", trangThai)
            }) > 0;
        }

        public static bool CapNhatTienSuBenhNhan(string maBN, string tienSu)
        {
            if (!SessionState.IsDoctor)
                throw new UnauthorizedAccessException("Chỉ bác sĩ được cập nhật tiền sử bệnh.");
            const string sql = @"UPDATE BenhNhan SET TienSuBenh = @TienSu WHERE MaBN = @MaBN
                AND EXISTS (SELECT 1 FROM PhieuKham WHERE MaBN=@MaBN AND MaNV_BacSi=@MaBS)";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaBN", maBN),
                new SqlParameter("@MaBS", SessionState.CurrentUser!.MaNV),
                new SqlParameter("@TienSu", (object?)tienSu ?? DBNull.Value)
            }) > 0;
        }

        public static DataTable LayDanhSachDichVu(string keyword = "", int? offset = null, int? pageSize = null)
        {
            string sql = @"
                SELECT MaDV, TenDV, DonGia, MoTa
                FROM DichVu
                WHERE @Kw = '' OR TenDV LIKE '%' + @Kw + '%' OR MaDV LIKE '%' + @Kw + '%' OR MoTa LIKE '%' + @Kw + '%'
                ORDER BY MaDV";
            var parameters = new[] { new SqlParameter("@Kw", keyword.Trim()) };
            if (offset.HasValue && pageSize.HasValue)
                return ExecutePagedQuery(sql, parameters, offset.Value, pageSize.Value);
            return DatabaseHelper.ExecuteQuery(sql, parameters);
        }

        public static DataTable LayChiDinhDVTheoPhieuKham(string maPK)
        {
            string sql = @"
                SELECT c.MaChiDinh, c.MaPK, c.MaDV, d.TenDV, c.DonGiaThoiDiem, c.KetQua, c.TrangThai
                FROM ChiTietChiDinhDV c
                INNER JOIN DichVu d ON c.MaDV = d.MaDV
                WHERE c.MaPK = @MaPK";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaPK", maPK) });
        }

        public static DataTable LayTaiLieuCLS(int maChiDinh)
        {
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            if (!CoQuyenTruyCapChiDinh(connection, null, maChiDinh))
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập tài liệu của chỉ định này.");
            using var command = new SqlCommand(@"SELECT MaTaiLieu,TenTep,LoaiNoiDung,KichThuoc,NgayTaiLen
                FROM dbo.TaiLieuCLS WHERE MaChiDinh=@ID ORDER BY NgayTaiLen DESC", connection);
            command.Parameters.AddWithValue("@ID", maChiDinh);
            var table = new DataTable();
            using var adapter = new SqlDataAdapter(command);
            adapter.Fill(table);
            return table;
        }

        public static long LuuTaiLieuCLS(int maChiDinh, string tenTep, string loaiNoiDung, byte[] duLieu)
        {
            if (!SessionState.IsAdmin && !SessionState.IsDoctor)
                throw new UnauthorizedAccessException("Chỉ bác sĩ hoặc quản trị viên được lưu tài liệu CLS.");
            if (string.IsNullOrWhiteSpace(tenTep) || tenTep.Trim().Length > 255)
                throw new ArgumentException("Tên tệp không hợp lệ.", nameof(tenTep));
            if (duLieu.Length == 0 || duLieu.Length > 10 * 1024 * 1024)
                throw new ArgumentException("Tệp phải có dung lượng từ 1 byte đến 10 MB.", nameof(duLieu));

            string extension = System.IO.Path.GetExtension(tenTep).ToLowerInvariant();
            bool isPdf = extension == ".pdf" && duLieu.Length >= 5
                && duLieu[0] == 0x25 && duLieu[1] == 0x50 && duLieu[2] == 0x44 && duLieu[3] == 0x46 && duLieu[4] == 0x2D;
            bool isPng = extension == ".png" && duLieu.Length >= 8
                && duLieu[0] == 0x89 && duLieu[1] == 0x50 && duLieu[2] == 0x4E && duLieu[3] == 0x47
                && duLieu[4] == 0x0D && duLieu[5] == 0x0A && duLieu[6] == 0x1A && duLieu[7] == 0x0A;
            bool isJpeg = (extension is ".jpg" or ".jpeg") && duLieu.Length >= 3
                && duLieu[0] == 0xFF && duLieu[1] == 0xD8 && duLieu[2] == 0xFF;
            string normalizedContentType = isPdf ? "application/pdf"
                : isPng ? "image/png"
                : isJpeg ? "image/jpeg"
                : throw new ArgumentException("Chỉ chấp nhận PDF, PNG hoặc JPEG hợp lệ.", nameof(duLieu));
            if (!string.Equals(loaiNoiDung, normalizedContentType, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Định dạng nội dung không khớp với tệp.", nameof(loaiNoiDung));

            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            if (!CoQuyenTruyCapChiDinh(connection, null, maChiDinh))
                throw new UnauthorizedAccessException("Bạn không có quyền lưu tài liệu cho chỉ định này.");
            using var command = new SqlCommand(@"INSERT INTO dbo.TaiLieuCLS(MaChiDinh,TenTep,LoaiNoiDung,DuLieu,KichThuoc,MaNV_TaiLen)
                OUTPUT INSERTED.MaTaiLieu
                VALUES(@ID,@TenTep,@LoaiNoiDung,@DuLieu,@KichThuoc,@MaNV)", connection);
            command.Parameters.AddWithValue("@ID", maChiDinh);
            command.Parameters.AddWithValue("@TenTep", System.IO.Path.GetFileName(tenTep.Trim()));
            command.Parameters.AddWithValue("@LoaiNoiDung", normalizedContentType);
            command.Parameters.Add("@DuLieu", System.Data.SqlDbType.VarBinary, -1).Value = duLieu;
            command.Parameters.AddWithValue("@KichThuoc", duLieu.LongLength);
            command.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
            return Convert.ToInt64(command.ExecuteScalar());
        }

        public static (string tenTep, string loaiNoiDung, byte[] duLieu) TaiTaiLieuCLS(long maTaiLieu)
        {
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var command = new SqlCommand(@"SELECT MaChiDinh,TenTep,LoaiNoiDung,DuLieu
                FROM dbo.TaiLieuCLS WHERE MaTaiLieu=@ID", connection);
            command.Parameters.AddWithValue("@ID", maTaiLieu);
            int maChiDinh;
            string tenTep;
            string loaiNoiDung;
            byte[] duLieu;
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                    throw new InvalidOperationException("Không tìm thấy tài liệu CLS.");
                maChiDinh = reader.GetInt32(0);
                tenTep = reader.GetString(1);
                loaiNoiDung = reader.GetString(2);
                duLieu = (byte[])reader["DuLieu"];
            }
            if (!CoQuyenTruyCapChiDinh(connection, null, maChiDinh))
                throw new UnauthorizedAccessException("Bạn không có quyền tải tài liệu của chỉ định này.");
            return (tenTep, loaiNoiDung, duLieu);
        }

        public static bool ChiDinhDichVu(string maPK, string maDV, decimal donGia)
        {
            if (!SessionState.IsDoctor) throw new UnauthorizedAccessException("Chỉ bác sĩ được chỉ định dịch vụ.");
            EnsureAdvancedWorkflowSchema();
            string sql = @"
                INSERT INTO ChiTietChiDinhDV (MaPK, MaDV, DonGiaThoiDiem, TrangThai,DaThanhToanCLS)
                SELECT @MaPK, @MaDV, dv.DonGia, N'Chờ thực hiện',0 FROM DichVu dv WHERE dv.MaDV=@MaDV
                  AND EXISTS (SELECT 1 FROM PhieuKham pk WHERE pk.MaPK=@MaPK AND pk.MaNV_BacSi=@MaBS AND pk.TrangThai IN (N'Chờ khám', N'Đang khám')
                    AND NOT EXISTS (SELECT 1 FROM HoaDon hd WHERE hd.MaPK=pk.MaPK AND hd.TrangThai<>N'Đã hủy'));
                IF @@ROWCOUNT > 0 UPDATE PhieuKham SET TrangThai=N'Chờ kết quả CLS' WHERE MaPK=@MaPK";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
                new SqlParameter("@MaBS", SessionState.CurrentUser!.MaNV),
                new SqlParameter("@MaDV", maDV),
                new SqlParameter("@DonGia", donGia)
            }) > 0;
        }

        public static bool CapNhatKetQuaDV(int maChiDinh, string ketQua)
        {
            if (!SessionState.IsDoctor) throw new UnauthorizedAccessException("Chỉ bác sĩ được cập nhật kết quả CLS.");
            EnsureAdvancedWorkflowSchema();
            string sql = @"
                UPDATE ChiTietChiDinhDV
                SET KetQua = @KetQua, TrangThai = N'Đã có kết quả'
                WHERE MaChiDinh = @ID AND DaThanhToanCLS=1
                  AND EXISTS (SELECT 1 FROM PhieuKham pk WHERE pk.MaPK=ChiTietChiDinhDV.MaPK AND pk.MaNV_BacSi=@MaBS AND pk.TrangThai=N'Chờ kết quả CLS')
                  AND NOT EXISTS (SELECT 1 FROM HoaDon hd WHERE hd.MaPK=ChiTietChiDinhDV.MaPK AND hd.TrangThai<>N'Đã hủy');
                DECLARE @Updated INT=@@ROWCOUNT;
                IF @Updated>0 UPDATE pk SET TrangThai=N'Đang khám' FROM PhieuKham pk
                    WHERE pk.TrangThai=N'Chờ kết quả CLS' AND pk.MaNV_BacSi=@MaBS AND pk.MaPK=(SELECT MaPK FROM ChiTietChiDinhDV WHERE MaChiDinh=@ID)
                      AND NOT EXISTS (SELECT 1 FROM ChiTietChiDinhDV c WHERE c.MaPK=pk.MaPK AND c.TrangThai<>N'Đã có kết quả');
                SELECT @Updated";
            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@ID", maChiDinh),
                new SqlParameter("@MaBS", SessionState.CurrentUser!.MaNV),
                new SqlParameter("@KetQua", ketQua)
            })) > 0;
        }

        // 5. THUỐC & KHO DƯỢC
        public static DataTable LayDanhSachThuoc(string keyword = "", int? offset = null, int? pageSize = null, string trangThaiTon = "")
        {
            string conditionTon = trangThaiTon switch
            {
                "ConHang" => " AND SoLuongTon > 30",
                "SapHet" => " AND SoLuongTon > 0 AND SoLuongTon <= 30",
                "HetHang" => " AND SoLuongTon = 0",
                _ => ""
            };

            string sql = $@"
                SELECT MaThuoc, TenThuoc, DonViTinh, DonGiaBan, SoLuongTon, CachDungMacDinh
                FROM Thuoc
                WHERE (@Kw = '' OR TenThuoc LIKE '%' + @Kw + '%' OR MaThuoc LIKE '%' + @Kw + '%'){conditionTon}
                ORDER BY TenThuoc, MaThuoc";
            return ExecutePagedQuery(sql, new[] { new SqlParameter("@Kw", keyword) }, offset, pageSize);
        }

        public static bool LuuThuoc(Thuoc t, bool isNew)
        {
            EnsurePharmacySchema();
            if (isNew)
            {
                string sql = @"
                    INSERT INTO Thuoc (MaThuoc, TenThuoc, DonViTinh, DonGiaBan, SoLuongTon, CachDungMacDinh)
                    VALUES (@MaThuoc, @TenThuoc, @DonViTinh, @DonGiaBan, 0, @CachDungMacDinh)";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@MaThuoc", t.MaThuoc),
                    new SqlParameter("@TenThuoc", t.TenThuoc),
                    new SqlParameter("@DonViTinh", t.DonViTinh),
                    new SqlParameter("@DonGiaBan", t.DonGiaBan),
                    new SqlParameter("@CachDungMacDinh", (object?)t.CachDungMacDinh ?? DBNull.Value)
                }) > 0;
            }
            else
            {
                string sql = @"
                    UPDATE Thuoc
                    SET TenThuoc = @TenThuoc, DonViTinh = @DonViTinh, DonGiaBan = @DonGiaBan,
                        CachDungMacDinh = @CachDungMacDinh
                    WHERE MaThuoc = @MaThuoc";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@MaThuoc", t.MaThuoc),
                    new SqlParameter("@TenThuoc", t.TenThuoc),
                    new SqlParameter("@DonViTinh", t.DonViTinh),
                    new SqlParameter("@DonGiaBan", t.DonGiaBan),
                    new SqlParameter("@CachDungMacDinh", (object?)t.CachDungMacDinh ?? DBNull.Value)
                }) > 0;
            }
        }

        // 6. ĐƠN THUỐC (Với Transaction trừ tồn kho)
        public static string SinhMaDonThuoc()
        {
            string sql = "SELECT TOP 1 MaDonThuoc FROM DonThuoc ORDER BY MaDonThuoc DESC";
            var result = DatabaseHelper.ExecuteScalar(sql);
            if (result == null || result == DBNull.Value) return "DT0001";
            string current = result.ToString()!;
            if (current.StartsWith("DT") && int.TryParse(current.Substring(2), out int num))
            {
                return $"DT{(num + 1):D4}";
            }
            return "DT0001";
        }

        public static DataTable LayChiTietDonThuocTheoPK(string maPK)
        {
            string sql = @"
                SELECT ct.MaCTDT, ct.MaDonThuoc, ct.MaThuoc, t.TenThuoc, t.DonViTinh,
                       ct.SoLuong, ct.DonGiaThoiDiem, (ct.SoLuong * ct.DonGiaThoiDiem) AS ThanhTien, ct.CachDung
                FROM ChiTietDonThuoc ct
                INNER JOIN DonThuoc dt ON ct.MaDonThuoc = dt.MaDonThuoc
                INNER JOIN Thuoc t ON ct.MaThuoc = t.MaThuoc
                WHERE dt.MaPK = @MaPK";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaPK", maPK) });
        }

        public static string LayGhiChuDonThuoc(string maPK)
        {
            const string sql = "SELECT GhiChu FROM DonThuoc WHERE MaPK = @MaPK";
            return DatabaseHelper.ExecuteScalar(sql, new[] { new SqlParameter("@MaPK", maPK) })?.ToString() ?? "";
        }

        public static bool CapNhatGhiChuDonThuoc(string maPK, string ghiChu)
        {
            if (!SessionState.IsDoctor) throw new UnauthorizedAccessException("Chỉ bác sĩ được cập nhật lời dặn.");
            const string sql = @"UPDATE DonThuoc SET GhiChu = @GhiChu WHERE MaPK = @MaPK
                AND EXISTS (SELECT 1 FROM PhieuKham WHERE PhieuKham.MaPK=DonThuoc.MaPK AND MaNV_BacSi=@MaBS AND TrangThai IN (N'Chờ khám',N'Đang khám'))
                AND NOT EXISTS (SELECT 1 FROM HoaDon WHERE HoaDon.MaPK=DonThuoc.MaPK AND TrangThai<>N'Đã hủy')";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
                new SqlParameter("@MaBS", SessionState.CurrentUser!.MaNV),
                new SqlParameter("@GhiChu", (object?)ghiChu ?? DBNull.Value)
            }) > 0;
        }

        public static DataTable LayChiTietDonThuocBenhNhan(string maBN, string? maBS = null)
        {
            const string sql = @"
                SELECT pk.NgayKham, dt.MaDonThuoc, t.TenThuoc, t.DonViTinh,
                       ct.SoLuong, ct.DonGiaThoiDiem,
                       ct.SoLuong * ct.DonGiaThoiDiem AS ThanhTien, ct.CachDung
                FROM PhieuKham pk
                INNER JOIN DonThuoc dt ON dt.MaPK = pk.MaPK
                INNER JOIN ChiTietDonThuoc ct ON ct.MaDonThuoc = dt.MaDonThuoc
                INNER JOIN Thuoc t ON t.MaThuoc = ct.MaThuoc
                WHERE pk.MaBN = @MaBN
                  AND (@MaBS IS NULL OR pk.MaNV_BacSi = @MaBS)
                ORDER BY pk.NgayKham DESC, dt.MaDonThuoc DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaBN", maBN),
                new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value)
            });
        }

        public static DataTable LayChiDinhDVBenhNhan(string maBN, string? maBS = null)
        {
            const string sql = @"
                SELECT pk.NgayKham, pk.MaPK, dv.TenDV, ct.DonGiaThoiDiem,
                       ct.TrangThai, ct.KetQua
                FROM PhieuKham pk
                INNER JOIN ChiTietChiDinhDV ct ON ct.MaPK = pk.MaPK
                INNER JOIN DichVu dv ON dv.MaDV = ct.MaDV
                WHERE pk.MaBN = @MaBN
                  AND (@MaBS IS NULL OR pk.MaNV_BacSi = @MaBS)
                ORDER BY pk.NgayKham DESC, ct.MaChiDinh DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaBN", maBN),
                new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value)
            });
        }

        public static bool KeDonThuoc(string maPK, string ghiChu, System.Collections.Generic.List<ChiTietDonThuoc> listThuoc, out string errorMessage)
        {
            if (!SessionState.IsDoctor) throw new UnauthorizedAccessException("Chỉ bác sĩ được kê đơn thuốc.");
            EnsurePharmacySchema();
            errorMessage = string.Empty;
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                using (var editable = new SqlCommand(@"SELECT COUNT(*) FROM PhieuKham pk WITH (UPDLOCK, HOLDLOCK)
                    WHERE pk.MaPK=@MaPK AND pk.MaNV_BacSi=@MaBS AND pk.TrangThai IN (N'Chờ khám', N'Đang khám')
                    AND NOT EXISTS (SELECT 1 FROM HoaDon hd WHERE hd.MaPK=pk.MaPK AND hd.TrangThai<>N'Đã hủy')", conn, trans))
                {
                    editable.Parameters.AddWithValue("@MaPK", maPK);
                    editable.Parameters.AddWithValue("@MaBS", SessionState.CurrentUser!.MaNV);
                    if (Convert.ToInt32(editable.ExecuteScalar()) != 1) throw new InvalidOperationException("Phiếu khám đã hoàn tất hoặc đã có hóa đơn.");
                }
                // Kiểm tra xem đã có đơn thuốc cho phiếu khám này chưa
                string sqlCheck = "SELECT MaDonThuoc FROM DonThuoc WHERE MaPK = @MaPK";
                using var cmdCheck = new SqlCommand(sqlCheck, conn, trans);
                cmdCheck.Parameters.AddWithValue("@MaPK", maPK);
                var existingId = cmdCheck.ExecuteScalar()?.ToString();

                string maDonThuoc = existingId ?? SinhMaDonThuoc();

                if (string.IsNullOrEmpty(existingId))
                {
                    string sqlInsertDT = @"
                        INSERT INTO DonThuoc (MaDonThuoc, MaPK, NgayKe, GhiChu)
                        VALUES (@MaDT, @MaPK, GETDATE(), @GhiChu)";
                    using var cmdDT = new SqlCommand(sqlInsertDT, conn, trans);
                    cmdDT.Parameters.AddWithValue("@MaDT", maDonThuoc);
                    cmdDT.Parameters.AddWithValue("@MaPK", maPK);
                    cmdDT.Parameters.AddWithValue("@GhiChu", (object?)ghiChu ?? DBNull.Value);
                    cmdDT.ExecuteNonQuery();
                }

                // Kê đơn chỉ ghi nghĩa vụ thuốc; tồn kho giảm khi dược sĩ/lễ tân cấp phát.
                foreach (var item in listThuoc)
                {
                    if (item.SoLuong <= 0) throw new InvalidOperationException("Số lượng kê phải lớn hơn 0.");

                    // 2. Insert chi tiết đơn
                    string sqlInsertCT = @"
                        INSERT INTO ChiTietDonThuoc (MaDonThuoc, MaThuoc, SoLuong, DonGiaThoiDiem, CachDung)
                        SELECT @MaDT, @MaThuoc, @SoLuong, DonGiaBan, @CachDung FROM Thuoc WHERE MaThuoc=@MaThuoc";
                    using var cmdCT = new SqlCommand(sqlInsertCT, conn, trans);
                    cmdCT.Parameters.AddWithValue("@MaDT", maDonThuoc);
                    cmdCT.Parameters.AddWithValue("@MaThuoc", item.MaThuoc);
                    cmdCT.Parameters.AddWithValue("@SoLuong", item.SoLuong);
                    cmdCT.Parameters.AddWithValue("@CachDung", item.CachDung);
                    cmdCT.ExecuteNonQuery();
                }

                trans.Commit();
                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                errorMessage = ex.Message;
                return false;
            }
        }

        // 7. THANH TOÁN & HÓA ĐƠN
        public static string SinhMaHoaDon()
        {
            string sql = "SELECT TOP 1 MaHD FROM HoaDon ORDER BY MaHD DESC";
            var result = DatabaseHelper.ExecuteScalar(sql);
            if (result == null || result == DBNull.Value) return "HD0001";
            string current = result.ToString()!;
            if (current.StartsWith("HD") && int.TryParse(current.Substring(2), out int num))
            {
                return $"HD{(num + 1):D4}";
            }
            return "HD0001";
        }

        public static DataTable LayDanhSachHoaDon(string keyword = "", string? trangThai = null, int? offset = null, int? pageSize = null)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được tra cứu hóa đơn.");
            string sql = @"
                SELECT hd.MaHD, hd.MaPK, bn.HoTen AS TenBN, nv.HoTen AS ThuNgan,
                       hd.NgayLap, hd.TienDichVu, hd.TienThuoc, hd.GiamGia, hd.TongTien,
                       hd.PhanTramBaoHiem,hd.TienBaoHiem,
                       ISNULL(receipts.DaThu,0) AS DaThu,
                       hd.TongTien-hd.TienBaoHiem-ISNULL(receipts.DaThu,0) AS ConNo,
                       hd.HinhThucTT, hd.TrangThai
                FROM HoaDon hd
                INNER JOIN PhieuKham pk ON hd.MaPK = pk.MaPK
                INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                INNER JOIN NhanVien nv ON hd.MaNV_ThuNgan = nv.MaNV
                OUTER APPLY(SELECT SUM(CASE WHEN g.Loai=N'HoanTien' THEN -g.SoTien ELSE g.SoTien END) DaThu
                    FROM dbo.GiaoDichVienPhi g WHERE g.MaHD=hd.MaHD) receipts
                WHERE (@Kw = '' OR hd.MaHD LIKE '%' + @Kw + '%'
                    OR hd.MaPK LIKE '%' + @Kw + '%' OR bn.HoTen LIKE '%' + @Kw + '%')
                  AND (@TrangThai IS NULL OR @TrangThai = N'Tất cả trạng thái' OR hd.TrangThai = @TrangThai)
                ORDER BY hd.NgayLap DESC, hd.MaHD DESC";
            EnsureAdvancedWorkflowSchema();
            return ExecutePagedQuery(sql, new[]
            {
                new SqlParameter("@Kw", keyword.Trim()),
                new SqlParameter("@TrangThai", (object?)trangThai ?? DBNull.Value)
            }, offset, pageSize);
        }

        public static DataTable LayChiTietHoaDon(string maHD)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được xem chi tiết hóa đơn.");
            const string sql = @"
                SELECT N'Dịch vụ' AS Loai, dv.TenDV AS TenHang, 1 AS SoLuong,
                       ct.DonGiaThoiDiem AS DonGia, ct.DonGiaThoiDiem AS ThanhTien
                FROM HoaDon hd
                INNER JOIN ChiTietChiDinhDV ct ON ct.MaPK = hd.MaPK
                INNER JOIN DichVu dv ON dv.MaDV = ct.MaDV
                WHERE hd.MaHD = @MaHD
                UNION ALL
                SELECT N'Thuốc' AS Loai, t.TenThuoc AS TenHang, ct.SoLuong,
                       ct.DonGiaThoiDiem AS DonGia, ct.SoLuong * ct.DonGiaThoiDiem AS ThanhTien
                FROM HoaDon hd
                INNER JOIN DonThuoc dt ON dt.MaPK = hd.MaPK
                INNER JOIN ChiTietDonThuoc ct ON ct.MaDonThuoc = dt.MaDonThuoc
                INNER JOIN Thuoc t ON t.MaThuoc = ct.MaThuoc
                WHERE hd.MaHD = @MaHD";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaHD", maHD)
            });
        }

        public static (decimal tienDV, decimal tienThuoc) TinhChiPhiPhieuKham(string maPK)
        {
            string sqlDV = "SELECT ISNULL(SUM(DonGiaThoiDiem), 0) FROM ChiTietChiDinhDV WHERE MaPK = @MaPK";
            decimal tienDV = Convert.ToDecimal(DatabaseHelper.ExecuteScalar(sqlDV, new[] { new SqlParameter("@MaPK", maPK) }));

            string sqlThuoc = @"
                SELECT ISNULL(SUM(ct.SoLuong * ct.DonGiaThoiDiem), 0)
                FROM ChiTietDonThuoc ct
                INNER JOIN DonThuoc dt ON ct.MaDonThuoc = dt.MaDonThuoc
                WHERE dt.MaPK = @MaPK";
            decimal tienThuoc = Convert.ToDecimal(DatabaseHelper.ExecuteScalar(sqlThuoc, new[] { new SqlParameter("@MaPK", maPK) }));

            return (tienDV, tienThuoc);
        }

        public static long MoCaThuNgan(decimal tienDauCa)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được mở ca thu ngân.");
            if (tienDauCa < 0)
                throw new ArgumentOutOfRangeException(nameof(tienDauCa), "Tiền đầu ca không được âm.");
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                using var command = new SqlCommand(@"INSERT INTO dbo.CaThuNgan(MaNV,TienDauCa)
                    OUTPUT INSERTED.MaCa VALUES(@MaNV,@TienDauCa)", connection, transaction);
                command.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                command.Parameters.AddWithValue("@TienDauCa", tienDauCa);
                long maCa = Convert.ToInt64(command.ExecuteScalar());
                transaction.Commit();
                GhiNhatKy("Mở ca thu ngân", maCa.ToString(), $"Tiền đầu ca {tienDauCa:N0}");
                return maCa;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static DataTable LayThongTinCaThuNgan()
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được xem ca thu ngân.");
            EnsureAdvancedWorkflowSchema();
            return DatabaseHelper.ExecuteQuery(@"SELECT TOP (1) c.MaCa,c.ThoiGianMo,c.TienDauCa,
                    c.TienDauCa+ISNULL(SUM(CASE WHEN g.Loai IN(N'Thu',N'TamUng',N'ThuCLS') THEN g.SoTien ELSE -g.SoTien END),0) AS TienDuKien
                FROM dbo.CaThuNgan c LEFT JOIN dbo.GiaoDichVienPhi g ON g.MaCa=c.MaCa
                WHERE c.MaNV=@MaNV AND c.ThoiGianDong IS NULL
                GROUP BY c.MaCa,c.ThoiGianMo,c.TienDauCa ORDER BY c.ThoiGianMo DESC",
                new[] { new SqlParameter("@MaNV", SessionState.CurrentUser!.MaNV) });
        }

        public static bool DongCaThuNgan(decimal tienThucTe, string ghiChu = "")
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được đóng ca thu ngân.");
            if (tienThucTe < 0)
                throw new ArgumentOutOfRangeException(nameof(tienThucTe), "Tiền kiểm đếm không được âm.");
            if (ghiChu.Trim().Length > 500)
                throw new ArgumentException("Ghi chú đóng ca tối đa 500 ký tự.", nameof(ghiChu));
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                long maCa;
                using (var read = new SqlCommand(@"SELECT MaCa FROM dbo.CaThuNgan WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@MaNV AND ThoiGianDong IS NULL", connection, transaction))
                {
                    read.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                    object? value = read.ExecuteScalar();
                    if (value is null || value == DBNull.Value)
                        throw new InvalidOperationException("Không có ca thu ngân đang mở.");
                    maCa = Convert.ToInt64(value);
                }
                using var update = new SqlCommand(@"UPDATE dbo.CaThuNgan
                    SET ThoiGianDong=SYSDATETIME(),TienThucTe=@TienThucTe,GhiChuDongCa=@GhiChu
                    WHERE MaCa=@MaCa AND ThoiGianDong IS NULL", connection, transaction);
                update.Parameters.AddWithValue("@MaCa", maCa);
                update.Parameters.AddWithValue("@TienThucTe", tienThucTe);
                update.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? DBNull.Value : ghiChu.Trim());
                if (update.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Ca đã được đóng ở một phiên khác.");
                using var expected = new SqlCommand(@"SELECT TienDauCa+ISNULL(SUM(CASE WHEN Loai IN(N'Thu',N'TamUng',N'ThuCLS')
                    THEN SoTien ELSE -SoTien END),0) FROM dbo.CaThuNgan c
                    LEFT JOIN dbo.GiaoDichVienPhi g ON g.MaCa=c.MaCa WHERE c.MaCa=@MaCa GROUP BY TienDauCa", connection, transaction);
                expected.Parameters.AddWithValue("@MaCa", maCa);
                decimal expectedAmount = Convert.ToDecimal(expected.ExecuteScalar());
                transaction.Commit();
                GhiNhatKy("Đóng ca thu ngân", maCa.ToString(),
                    $"Dự kiến {expectedAmount:N0}; thực tế {tienThucTe:N0}; chênh lệch {tienThucTe - expectedAmount:N0}");
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static bool TaoHoaDon(HoaDon hd, decimal phanTramBaoHiem = 0)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được lập hóa đơn.");
            if (phanTramBaoHiem is < 0 or > 100)
                throw new ArgumentOutOfRangeException(nameof(phanTramBaoHiem), "Tỷ lệ bảo hiểm phải từ 0 đến 100.");
            EnsureInvoiceSchema();
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                using var command = new SqlCommand(@"
                    INSERT INTO HoaDon(MaHD,MaPK,MaNV_ThuNgan,NgayLap,NgayThanhToan,TienDichVu,TienThuoc,GiamGia,TongTien,
                        PhanTramBaoHiem,TienBaoHiem,HinhThucTT,TrangThai)
                    SELECT @MaHD,pk.MaPK,@MaNV,GETDATE(),NULL,cost.TienDV,cost.TienThuoc,@GiamGia,
                        cost.TienDV+cost.TienThuoc-@GiamGia,@Rate,
                        ROUND((cost.TienDV+cost.TienThuoc-@GiamGia)*@Rate/100,0),@HinhThuc,N'Chưa thanh toán'
                    FROM PhieuKham pk
                    CROSS APPLY(SELECT ISNULL((SELECT SUM(DonGiaThoiDiem) FROM ChiTietChiDinhDV WHERE MaPK=pk.MaPK),0) TienDV,
                        ISNULL((SELECT SUM(ct.SoLuong*ct.DonGiaThoiDiem) FROM DonThuoc dt
                            INNER JOIN ChiTietDonThuoc ct ON ct.MaDonThuoc=dt.MaDonThuoc WHERE dt.MaPK=pk.MaPK),0) TienThuoc) cost
                    WHERE pk.MaPK=@MaPK AND pk.TrangThai=N'Đã khám xong'
                      AND NOT EXISTS(SELECT 1 FROM HoaDon old WHERE old.MaPK=pk.MaPK AND old.TrangThai<>N'Đã hủy')
                      AND NOT EXISTS(SELECT 1 FROM ChiTietChiDinhDV c WHERE c.MaPK=pk.MaPK AND c.TrangThai<>N'Đã có kết quả')
                      AND @GiamGia>=0 AND @GiamGia<=cost.TienDV+cost.TienThuoc
                      AND @Rate BETWEEN 0 AND 100
                      AND @HinhThuc IN(N'Tiền mặt',N'Chuyển khoản QR',N'Thẻ')
                      AND ISNULL((SELECT SUM(g.SoTien) FROM dbo.GiaoDichVienPhi g
                          WHERE g.MaPK=pk.MaPK AND g.MaHD IS NULL AND g.Loai IN(N'TamUng',N'ThuCLS')),0)
                          <= cost.TienDV+cost.TienThuoc-@GiamGia-ROUND((cost.TienDV+cost.TienThuoc-@GiamGia)*@Rate/100,0)", connection, transaction);
                command.Parameters.AddWithValue("@MaHD", hd.MaHD);
                command.Parameters.AddWithValue("@MaPK", hd.MaPK);
                command.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                command.Parameters.AddWithValue("@GiamGia", hd.GiamGia);
                command.Parameters.AddWithValue("@Rate", phanTramBaoHiem);
                command.Parameters.AddWithValue("@HinhThuc", hd.HinhThucTT);
                if (command.ExecuteNonQuery() != 1)
                {
                    transaction.Commit();
                    return false;
                }
                using (var assign = new SqlCommand(@"UPDATE dbo.GiaoDichVienPhi SET MaHD=@MaHD
                    WHERE MaPK=@MaPK AND MaHD IS NULL AND Loai IN(N'TamUng',N'ThuCLS')", connection, transaction))
                {
                    assign.Parameters.AddWithValue("@MaHD", hd.MaHD);
                    assign.Parameters.AddWithValue("@MaPK", hd.MaPK);
                    assign.ExecuteNonQuery();
                }
                using (var settle = new SqlCommand(@"UPDATE dbo.HoaDon SET TrangThai=N'Đã thanh toán',NgayThanhToan=GETDATE()
                    WHERE MaHD=@MaHD AND TongTien-TienBaoHiem<=ISNULL((SELECT SUM(CASE WHEN Loai=N'HoanTien' THEN -SoTien ELSE SoTien END)
                        FROM dbo.GiaoDichVienPhi WHERE MaHD=@MaHD),0)", connection, transaction))
                {
                    settle.Parameters.AddWithValue("@MaHD", hd.MaHD);
                    settle.ExecuteNonQuery();
                }
                transaction.Commit();
                GhiNhatKy("Lập hóa đơn", hd.MaHD,
                    $"Phiếu khám {hd.MaPK}; bảo hiểm {phanTramBaoHiem:0.##}%");
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static bool ThanhToanHoaDon(string maHD, string hinhThuc, decimal? soTien = null)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được thanh toán hóa đơn.");
            if (hinhThuc is not ("Tiền mặt" or "Chuyển khoản QR" or "Thẻ"))
                throw new ArgumentOutOfRangeException(nameof(hinhThuc), "Hình thức thanh toán không hợp lệ.");
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                string maPK;
                decimal balance;
                using (var invoice = new SqlCommand(@"SELECT MaPK,TongTien-TienBaoHiem FROM dbo.HoaDon WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaHD=@MaHD AND TrangThai=N'Chưa thanh toán'", connection, transaction))
                {
                    invoice.Parameters.AddWithValue("@MaHD", maHD);
                    using var reader = invoice.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException("Hóa đơn không còn ở trạng thái chờ thanh toán.");
                    maPK = reader.GetString(0);
                    balance = reader.GetDecimal(1);
                }
                using (var paid = new SqlCommand(@"SELECT ISNULL(SUM(CASE WHEN Loai=N'HoanTien' THEN -SoTien ELSE SoTien END),0)
                    FROM dbo.GiaoDichVienPhi WHERE MaHD=@MaHD", connection, transaction))
                {
                    paid.Parameters.AddWithValue("@MaHD", maHD);
                    balance -= Convert.ToDecimal(paid.ExecuteScalar());
                }
                decimal amount = soTien ?? balance;
                if (amount <= 0 || amount > balance)
                    throw new ArgumentOutOfRangeException(nameof(soTien), $"Số tiền thu phải lớn hơn 0 và không vượt quá công nợ {balance:N0}.");
                long maCa;
                using (var shift = new SqlCommand(@"SELECT MaCa FROM dbo.CaThuNgan WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@MaNV AND ThoiGianDong IS NULL", connection, transaction))
                {
                    shift.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                    object? value = shift.ExecuteScalar();
                    if (value is null || value == DBNull.Value)
                        throw new InvalidOperationException("Mở ca thu ngân trước khi ghi nhận giao dịch.");
                    maCa = Convert.ToInt64(value);
                }
                using (var insert = new SqlCommand(@"INSERT INTO dbo.GiaoDichVienPhi(MaHD,MaPK,MaCa,MaNV,Loai,SoTien,HinhThuc)
                    VALUES(@MaHD,@MaPK,@MaCa,@MaNV,N'Thu',@SoTien,@HinhThuc)", connection, transaction))
                {
                    insert.Parameters.AddWithValue("@MaHD", maHD);
                    insert.Parameters.AddWithValue("@MaPK", maPK);
                    insert.Parameters.AddWithValue("@MaCa", maCa);
                    insert.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                    insert.Parameters.AddWithValue("@SoTien", amount);
                    insert.Parameters.AddWithValue("@HinhThuc", hinhThuc);
                    insert.ExecuteNonQuery();
                }
                using (var settle = new SqlCommand(@"UPDATE dbo.HoaDon
                    SET TrangThai=CASE WHEN TongTien-TienBaoHiem<=ISNULL((SELECT SUM(CASE WHEN Loai=N'HoanTien' THEN -SoTien ELSE SoTien END)
                        FROM dbo.GiaoDichVienPhi WHERE MaHD=@MaHD),0) THEN N'Đã thanh toán' ELSE N'Chưa thanh toán' END,
                        HinhThucTT=@HinhThuc,
                        NgayThanhToan=CASE WHEN TongTien-TienBaoHiem<=ISNULL((SELECT SUM(CASE WHEN Loai=N'HoanTien' THEN -SoTien ELSE SoTien END)
                        FROM dbo.GiaoDichVienPhi WHERE MaHD=@MaHD),0) THEN GETDATE() ELSE NULL END
                    WHERE MaHD=@MaHD", connection, transaction))
                {
                    settle.Parameters.AddWithValue("@MaHD", maHD);
                    settle.Parameters.AddWithValue("@HinhThuc", hinhThuc);
                    settle.ExecuteNonQuery();
                }
                transaction.Commit();
                GhiNhatKy("Thanh toán hóa đơn", maHD, $"Thu {amount:N0}; hình thức {hinhThuc}; ca {maCa}");
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static bool TamUngPhieuKham(string maPK, decimal soTien, string hinhThuc, string? ghiChu = null)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được ghi nhận tạm ứng.");
            if (soTien <= 0)
                throw new ArgumentOutOfRangeException(nameof(soTien), "Số tiền tạm ứng phải lớn hơn 0.");
            if (hinhThuc is not ("Tiền mặt" or "Chuyển khoản QR" or "Thẻ"))
                throw new ArgumentOutOfRangeException(nameof(hinhThuc), "Hình thức thanh toán không hợp lệ.");
            if (ghiChu?.Trim().Length > 500)
                throw new ArgumentException("Ghi chú tối đa 500 ký tự.", nameof(ghiChu));
            EnsureAdvancedWorkflowSchema();

            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                decimal total;
                using (var ticket = new SqlCommand(@"SELECT ISNULL((SELECT SUM(DonGiaThoiDiem) FROM ChiTietChiDinhDV WHERE MaPK=pk.MaPK),0)
                    +ISNULL((SELECT SUM(ct.SoLuong*ct.DonGiaThoiDiem) FROM DonThuoc dt
                        INNER JOIN ChiTietDonThuoc ct ON ct.MaDonThuoc=dt.MaDonThuoc WHERE dt.MaPK=pk.MaPK),0)
                    FROM PhieuKham pk WITH(UPDLOCK,HOLDLOCK)
                    WHERE pk.MaPK=@MaPK AND pk.TrangThai=N'Đã khám xong'
                      AND NOT EXISTS(SELECT 1 FROM HoaDon hd WHERE hd.MaPK=pk.MaPK AND hd.TrangThai<>N'Đã hủy')
                      AND NOT EXISTS(SELECT 1 FROM ChiTietChiDinhDV c WHERE c.MaPK=pk.MaPK AND c.TrangThai<>N'Đã có kết quả')",
                    connection, transaction))
                {
                    ticket.Parameters.AddWithValue("@MaPK", maPK);
                    object? value = ticket.ExecuteScalar();
                    if (value is null || value == DBNull.Value)
                        throw new InvalidOperationException("Chỉ có thể tạm ứng cho phiếu khám đã hoàn tất và chưa lập hóa đơn.");
                    total = Convert.ToDecimal(value);
                }
                decimal advance;
                using (var existing = new SqlCommand(@"SELECT ISNULL(SUM(SoTien),0) FROM dbo.GiaoDichVienPhi
                    WHERE MaPK=@MaPK AND MaHD IS NULL AND Loai IN(N'TamUng',N'ThuCLS')", connection, transaction))
                {
                    existing.Parameters.AddWithValue("@MaPK", maPK);
                    advance = Convert.ToDecimal(existing.ExecuteScalar());
                }
                if (soTien + advance > total)
                    throw new InvalidOperationException($"Tổng tạm ứng không được vượt quá chi phí tạm tính {total:N0}.");
                long maCa;
                using (var shift = new SqlCommand(@"SELECT MaCa FROM dbo.CaThuNgan WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@MaNV AND ThoiGianDong IS NULL", connection, transaction))
                {
                    shift.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                    object? value = shift.ExecuteScalar();
                    if (value is null || value == DBNull.Value)
                        throw new InvalidOperationException("Mở ca thu ngân trước khi ghi nhận giao dịch.");
                    maCa = Convert.ToInt64(value);
                }
                using var insert = new SqlCommand(@"INSERT INTO dbo.GiaoDichVienPhi(MaPK,MaCa,MaNV,Loai,SoTien,HinhThuc,GhiChu)
                    VALUES(@MaPK,@MaCa,@MaNV,N'TamUng',@SoTien,@HinhThuc,@GhiChu)", connection, transaction);
                insert.Parameters.AddWithValue("@MaPK", maPK);
                insert.Parameters.AddWithValue("@MaCa", maCa);
                insert.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                insert.Parameters.AddWithValue("@SoTien", soTien);
                insert.Parameters.AddWithValue("@HinhThuc", hinhThuc);
                insert.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? DBNull.Value : ghiChu.Trim());
                insert.ExecuteNonQuery();
                transaction.Commit();
                GhiNhatKy("Thu tạm ứng", maPK, $"Thu {soTien:N0}; hình thức {hinhThuc}; ca {maCa}");
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static DataTable LayChiDinhChoThanhToanCLS()
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được xem chỉ định CLS chờ thu.");
            EnsureAdvancedWorkflowSchema();
            return DatabaseHelper.ExecuteQuery(@"SELECT c.MaChiDinh,c.MaPK,bn.HoTen AS TenBN,dv.TenDV,
                    c.DonGiaThoiDiem,c.TrangThai,pk.NgayKham
                FROM dbo.ChiTietChiDinhDV c
                INNER JOIN dbo.PhieuKham pk ON pk.MaPK=c.MaPK
                INNER JOIN dbo.BenhNhan bn ON bn.MaBN=pk.MaBN
                INNER JOIN dbo.DichVu dv ON dv.MaDV=c.MaDV
                WHERE c.TrangThai=N'Chờ thực hiện' AND c.DaThanhToanCLS=0
                  AND NOT EXISTS(SELECT 1 FROM dbo.HoaDon hd WHERE hd.MaPK=pk.MaPK AND hd.TrangThai<>N'Đã hủy')
                ORDER BY pk.NgayKham,c.MaChiDinh");
        }

        public static bool ThanhToanChiDinhCLS(int maChiDinh, string hinhThuc)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được thu phí CLS.");
            if (hinhThuc is not ("Tiền mặt" or "Chuyển khoản QR" or "Thẻ"))
                throw new ArgumentOutOfRangeException(nameof(hinhThuc), "Hình thức thanh toán không hợp lệ.");
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                string maPK;
                decimal amount;
                using (var order = new SqlCommand(@"SELECT c.MaPK,c.DonGiaThoiDiem
                    FROM dbo.ChiTietChiDinhDV c WITH(UPDLOCK,HOLDLOCK)
                    INNER JOIN dbo.PhieuKham pk ON pk.MaPK=c.MaPK
                    WHERE c.MaChiDinh=@ID AND c.TrangThai=N'Chờ thực hiện' AND c.DaThanhToanCLS=0
                      AND NOT EXISTS(SELECT 1 FROM dbo.HoaDon hd WHERE hd.MaPK=pk.MaPK AND hd.TrangThai<>N'Đã hủy')",
                    connection, transaction))
                {
                    order.Parameters.AddWithValue("@ID", maChiDinh);
                    using var reader = order.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException("Chỉ định CLS đã thanh toán hoặc không còn chờ thực hiện.");
                    maPK = reader.GetString(0);
                    amount = reader.GetDecimal(1);
                }
                long maCa;
                using (var shift = new SqlCommand(@"SELECT MaCa FROM dbo.CaThuNgan WITH(UPDLOCK,HOLDLOCK)
                    WHERE MaNV=@MaNV AND ThoiGianDong IS NULL", connection, transaction))
                {
                    shift.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                    object? value = shift.ExecuteScalar();
                    if (value is null || value == DBNull.Value)
                        throw new InvalidOperationException("Mở ca thu ngân trước khi thu phí CLS.");
                    maCa = Convert.ToInt64(value);
                }
                using (var insert = new SqlCommand(@"INSERT INTO dbo.GiaoDichVienPhi(MaPK,MaChiDinh,MaCa,MaNV,Loai,SoTien,HinhThuc)
                    VALUES(@MaPK,@MaChiDinh,@MaCa,@MaNV,N'ThuCLS',@SoTien,@HinhThuc)", connection, transaction))
                {
                    insert.Parameters.AddWithValue("@MaPK", maPK);
                    insert.Parameters.AddWithValue("@MaChiDinh", maChiDinh);
                    insert.Parameters.AddWithValue("@MaCa", maCa);
                    insert.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                    insert.Parameters.AddWithValue("@SoTien", amount);
                    insert.Parameters.AddWithValue("@HinhThuc", hinhThuc);
                    insert.ExecuteNonQuery();
                }
                using (var paid = new SqlCommand(@"UPDATE dbo.ChiTietChiDinhDV SET DaThanhToanCLS=1
                    WHERE MaChiDinh=@ID AND DaThanhToanCLS=0", connection, transaction))
                {
                    paid.Parameters.AddWithValue("@ID", maChiDinh);
                    if (paid.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("Chỉ định CLS đã được thu ở một phiên khác.");
                }
                transaction.Commit();
                GhiNhatKy("Thu phí cận lâm sàng", maChiDinh.ToString(),
                    $"Phiếu {maPK}; số tiền {amount:N0}; ca {maCa}");
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static DataTable LayGiaoDichVienPhi(string maHD)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được tra cứu giao dịch.");
            EnsureAdvancedWorkflowSchema();
            return DatabaseHelper.ExecuteQuery(@"SELECT g.MaGiaoDich,g.Loai,g.SoTien,g.HinhThuc,g.ThoiGian,g.GhiChu,
                    nv.HoTen AS NguoiThu,g.MaCa
                FROM dbo.GiaoDichVienPhi g INNER JOIN dbo.NhanVien nv ON nv.MaNV=g.MaNV
                WHERE g.MaHD=@MaHD ORDER BY g.ThoiGian,g.MaGiaoDich",
                new[] { new SqlParameter("@MaHD", maHD) });
        }

        public static DataTable LayYeuCauHoanTien(string? trangThai = null)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được tra cứu yêu cầu hoàn tiền.");
            EnsureAdvancedWorkflowSchema();
            return DatabaseHelper.ExecuteQuery(@"SELECT y.MaYeuCau,y.MaHD,y.SoTien,y.LyDo,y.TrangThai,y.NgayDeNghi,
                    requester.HoTen AS NguoiDeNghi,approver.HoTen AS NguoiDuyet,y.NgayDuyet,y.GhiChuDuyet
                FROM dbo.YeuCauHoanTien y
                INNER JOIN dbo.NhanVien requester ON requester.MaNV=y.MaNV_DeNghi
                LEFT JOIN dbo.NhanVien approver ON approver.MaNV=y.MaNV_Duyet
                WHERE @Status IS NULL OR y.TrangThai=@Status
                ORDER BY y.NgayDeNghi DESC",
                new[] { new SqlParameter("@Status", (object?)trangThai ?? DBNull.Value) });
        }

        public static bool TaoYeuCauHoanTien(string maHD, decimal soTien, string lyDo)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được đề nghị hoàn tiền.");
            if (soTien <= 0)
                throw new ArgumentOutOfRangeException(nameof(soTien), "Số tiền hoàn phải lớn hơn 0.");
            if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length > 500)
                throw new ArgumentException("Cần ghi lý do hoàn tiền (tối đa 500 ký tự).", nameof(lyDo));
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                using var command = new SqlCommand(@"INSERT INTO dbo.YeuCauHoanTien(MaHD,SoTien,LyDo,MaNV_DeNghi)
                    SELECT hd.MaHD,@SoTien,@LyDo,@MaNV
                    FROM dbo.HoaDon hd WITH(UPDLOCK,HOLDLOCK)
                    WHERE hd.MaHD=@MaHD AND hd.TrangThai IN(N'Đã thanh toán',N'Chưa thanh toán')
                      AND @SoTien<=ISNULL((SELECT SUM(CASE WHEN g.Loai=N'HoanTien' THEN -g.SoTien ELSE g.SoTien END)
                          FROM dbo.GiaoDichVienPhi g WHERE g.MaHD=hd.MaHD),0)
                          -ISNULL((SELECT SUM(y.SoTien) FROM dbo.YeuCauHoanTien y
                              WHERE y.MaHD=hd.MaHD AND y.TrangThai=N'Chờ duyệt'),0)", connection, transaction);
                command.Parameters.AddWithValue("@MaHD", maHD);
                command.Parameters.AddWithValue("@SoTien", soTien);
                command.Parameters.AddWithValue("@LyDo", lyDo.Trim());
                command.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                bool created = command.ExecuteNonQuery() == 1;
                if (!created)
                    throw new InvalidOperationException("Số tiền yêu cầu vượt quá khoản đã thu còn có thể hoàn, hoặc hóa đơn chưa thanh toán.");
                transaction.Commit();
                GhiNhatKy("Đề nghị hoàn tiền", maHD, $"Số tiền {soTien:N0}; lý do {lyDo.Trim()}");
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static bool XuLyYeuCauHoanTien(long maYeuCau, bool chapNhan, string? ghiChu = null)
        {
            if (!SessionState.IsAdmin)
                throw new UnauthorizedAccessException("Chỉ quản trị viên được duyệt hoặc từ chối hoàn tiền.");
            if (ghiChu?.Trim().Length > 500)
                throw new ArgumentException("Ghi chú duyệt tối đa 500 ký tự.", nameof(ghiChu));
            EnsureAdvancedWorkflowSchema();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                string maHD;
                string maPK;
                decimal amount;
                string method;
                using (var request = new SqlCommand(@"SELECT y.MaHD,hd.MaPK,y.SoTien,hd.HinhThucTT
                    FROM dbo.YeuCauHoanTien y WITH(UPDLOCK,HOLDLOCK)
                    INNER JOIN dbo.HoaDon hd WITH(UPDLOCK,HOLDLOCK) ON hd.MaHD=y.MaHD
                    WHERE y.MaYeuCau=@ID AND y.TrangThai=N'Chờ duyệt'", connection, transaction))
                {
                    request.Parameters.AddWithValue("@ID", maYeuCau);
                    using var reader = request.ExecuteReader();
                    if (!reader.Read())
                        throw new InvalidOperationException("Yêu cầu không còn ở trạng thái chờ duyệt.");
                    maHD = reader.GetString(0);
                    maPK = reader.GetString(1);
                    amount = reader.GetDecimal(2);
                    method = reader.GetString(3);
                }
                if (chapNhan)
                {
                    using var insert = new SqlCommand(@"INSERT INTO dbo.GiaoDichVienPhi(MaHD,MaPK,MaNV,Loai,SoTien,HinhThuc,GhiChu)
                        VALUES(@MaHD,@MaPK,@MaNV,N'HoanTien',@SoTien,@HinhThuc,@GhiChu)", connection, transaction);
                    insert.Parameters.AddWithValue("@MaHD", maHD);
                    insert.Parameters.AddWithValue("@MaPK", maPK);
                    insert.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                    insert.Parameters.AddWithValue("@SoTien", amount);
                    insert.Parameters.AddWithValue("@HinhThuc", method);
                    insert.Parameters.AddWithValue("@GhiChu", string.IsNullOrWhiteSpace(ghiChu) ? DBNull.Value : ghiChu.Trim());
                    insert.ExecuteNonQuery();
                }
                using var update = new SqlCommand(@"UPDATE dbo.YeuCauHoanTien
                    SET TrangThai=@Status,MaNV_Duyet=@MaNV,NgayDuyet=SYSDATETIME(),GhiChuDuyet=@Note
                    WHERE MaYeuCau=@ID AND TrangThai=N'Chờ duyệt'", connection, transaction);
                update.Parameters.AddWithValue("@Status", chapNhan ? "Đã duyệt" : "Từ chối");
                update.Parameters.AddWithValue("@MaNV", SessionState.CurrentUser!.MaNV);
                update.Parameters.AddWithValue("@Note", string.IsNullOrWhiteSpace(ghiChu) ? DBNull.Value : ghiChu.Trim());
                if (update.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Yêu cầu đã được xử lý ở một phiên khác.");
                transaction.Commit();
                GhiNhatKy(chapNhan ? "Duyệt hoàn tiền" : "Từ chối hoàn tiền", maHD,
                    $"Yêu cầu {maYeuCau}; số tiền {amount:N0}");
                return true;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public static bool HuyHoaDonChuaThanhToan(string maHD)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được hủy hóa đơn.");
            EnsureAdvancedWorkflowSchema();
            const string sql = @"
                UPDATE HoaDon
                SET TrangThai = N'Đã hủy'
                WHERE MaHD = @MaHD AND TrangThai = N'Chưa thanh toán'
                  AND NOT EXISTS(SELECT 1 FROM dbo.GiaoDichVienPhi WHERE MaHD=@MaHD)";
            bool canceled = DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaHD", maHD)
            }) > 0;
            if (canceled) GhiNhatKy("Hủy hóa đơn", maHD);
            return canceled;
        }

        // 8. NHÂN VIÊN (QUẢN TRỊ ADMIN)
        public static DataTable LayDanhSachNhanVien(string keyword = "", string role = "", int? offset = null, int? pageSize = null)
        {
            EnsureProfileImageSchema();
            string sql = @"
                SELECT nv.MaNV, nv.HoTen, nv.ChucVu, nv.ChuyenKhoa, nv.SoDienThoai, nv.Email, nv.AnhDaiDien,
                       CASE WHEN nv.TrangThai = 1 THEN N'Đang làm' ELSE N'Đã nghỉ' END AS TrangThai,
                       tk.TenDangNhap, ISNULL(pc.PhongCongTac, N'Chưa phân công') AS PhongCongTac
                FROM NhanVien nv
                LEFT JOIN TaiKhoan tk ON nv.MaNV = tk.MaNV
                OUTER APPLY (
                    SELECT STUFF((
                        SELECT N', ' + p.TenPhong + N' (' + k.TenKhoa + N')'
                        FROM PhanCongPhong pc2
                        INNER JOIN PhongKham p ON p.MaPhong = pc2.MaPhong
                        INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                        WHERE pc2.MaNV = nv.MaNV AND pc2.VaiTro = nv.ChucVu
                        ORDER BY k.TenKhoa, p.TenPhong
                        FOR XML PATH(''), TYPE
                    ).value('.', 'NVARCHAR(MAX)'), 1, 2, N'') AS PhongCongTac
                ) pc
                WHERE (@Kw = '' OR nv.MaNV LIKE '%' + @Kw + '%'
                    OR nv.HoTen LIKE '%' + @Kw + '%'
                    OR nv.ChucVu LIKE '%' + @Kw + '%'
                    OR nv.ChuyenKhoa LIKE '%' + @Kw + '%'
                    OR nv.SoDienThoai LIKE '%' + @Kw + '%'
                    OR nv.Email LIKE '%' + @Kw + '%'
                    OR tk.TenDangNhap LIKE '%' + @Kw + '%'
                    OR pc.PhongCongTac LIKE '%' + @Kw + '%')
                AND (@Role = '' OR nv.ChucVu = @Role)
                ORDER BY nv.MaNV";
            return ExecutePagedQuery(sql, new[]
            {
                new SqlParameter("@Kw", keyword.Trim()),
                new SqlParameter("@Role", role)
            }, offset, pageSize);
        }

        public static DataRow? LayHoSoNhanVien(string maNV)
        {
            EnsureProfileImageSchema();
            const string sql = @"
                SELECT nv.MaNV, nv.HoTen, nv.ChucVu, nv.ChuyenKhoa, nv.SoDienThoai,
                       nv.Email, nv.AnhDaiDien, nv.TrangThai, tk.TenDangNhap
                FROM NhanVien nv
                LEFT JOIN TaiKhoan tk ON tk.MaNV = nv.MaNV
                WHERE nv.MaNV = @MaNV";
            var result = DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaNV", maNV)
            });
            return result.Rows.Count > 0 ? result.Rows[0] : null;
        }

        public static string SinhMaNhanVien()
        {
            string sql = "SELECT TOP 1 MaNV FROM NhanVien ORDER BY MaNV DESC";
            var result = DatabaseHelper.ExecuteScalar(sql);
            if (result == null || result == DBNull.Value) return "NV001";
            string current = result.ToString()!;
            if (current.StartsWith("NV") && int.TryParse(current.Substring(2), out int num))
                return $"NV{(num + 1):D3}";
            return "NV001";
        }

        public static bool LuuNhanVien(NhanVien nv, bool isNew)
        {
            if (!SessionState.IsAdmin) throw new UnauthorizedAccessException("Chỉ quản trị viên được quản lý hồ sơ nhân viên.");
            EnsureProfileImageSchema();
            if (nv.ChucVu is not ("Admin" or "BacSi" or "LeTan" or "ThuNgan"))
                throw new ArgumentOutOfRangeException(nameof(nv), "Chức vụ không hợp lệ.");

            if (nv.ChucVu == "BacSi")
            {
                if (string.IsNullOrWhiteSpace(nv.ChuyenKhoa))
                    throw new InvalidOperationException("Bác sĩ phải có một chuyên khoa chính.");

                int specialtyExists = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM Khoa WHERE TenKhoa = @ChuyenKhoa",
                    new[] { new SqlParameter("@ChuyenKhoa", nv.ChuyenKhoa.Trim()) }));
                if (specialtyExists == 0)
                    throw new InvalidOperationException("Chuyên khoa đã chọn không tồn tại trong danh mục khoa.");

                if (!isNew)
                {
                    string currentRole = LayChucVuNhanVien(nv.MaNV);
                    string currentSpecialty = LayChuyenKhoaNhanVien(nv.MaNV).Trim();
                    if (currentRole == "BacSi" && !string.IsNullOrWhiteSpace(currentSpecialty) &&
                        !string.Equals(currentSpecialty, nv.ChuyenKhoa.Trim(), StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException($"Bác sĩ đã có chuyên khoa chính '{currentSpecialty}' và không thể đổi sang chuyên khoa khác.");
                }

                const string assignedSpecialtySql = @"
                    SELECT COUNT(*)
                    FROM PhanCongPhong pc
                    INNER JOIN PhongKham p ON p.MaPhong = pc.MaPhong
                    INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                    WHERE pc.MaNV = @MaNV
                      AND k.TenKhoa <> @ChuyenKhoa";
                int conflictingRooms = Convert.ToInt32(DatabaseHelper.ExecuteScalar(assignedSpecialtySql, new[]
                {
                    new SqlParameter("@MaNV", nv.MaNV),
                    new SqlParameter("@ChuyenKhoa", nv.ChuyenKhoa.Trim())
                }));
                if (conflictingRooms > 0)
                    throw new InvalidOperationException("Không thể lưu vai trò bác sĩ: nhân viên đang được phân công vào phòng thuộc chuyên khoa khác.");
            }

            if (isNew)
            {
                string sql = @"INSERT INTO NhanVien (MaNV, HoTen, ChucVu, ChuyenKhoa, SoDienThoai, Email, AnhDaiDien, TrangThai)
                    VALUES (@MaNV, @HoTen, @ChucVu, @ChuyenKhoa, @SDT, @Email, @AnhDaiDien, 1)";
                return DatabaseHelper.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@MaNV", nv.MaNV),
                    new SqlParameter("@HoTen", nv.HoTen),
                    new SqlParameter("@ChucVu", nv.ChucVu),
                    new SqlParameter("@ChuyenKhoa", (object?)nv.ChuyenKhoa ?? DBNull.Value),
                    new SqlParameter("@SDT", (object?)nv.SoDienThoai ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)nv.Email ?? DBNull.Value),
                    CreateProfileImageParameter(nv.AnhDaiDien)
                }) > 0;
            }
            else
            {
                using var conn = DatabaseHelper.GetConnection();
                conn.Open(); using var tx = conn.BeginTransaction();
                try
                {
                    using (var employee = new SqlCommand(@"UPDATE NhanVien SET HoTen=@HoTen, ChucVu=@ChucVu, ChuyenKhoa=@ChuyenKhoa,
                        SoDienThoai=@SDT, Email=@Email, AnhDaiDien=COALESCE(@AnhDaiDien,AnhDaiDien) WHERE MaNV=@MaNV", conn, tx))
                    {
                        employee.Parameters.AddRange(new[] {
                            new SqlParameter("@MaNV", nv.MaNV), new SqlParameter("@HoTen", nv.HoTen),
                            new SqlParameter("@ChucVu", nv.ChucVu), new SqlParameter("@ChuyenKhoa", (object?)nv.ChuyenKhoa ?? DBNull.Value),
                            new SqlParameter("@SDT", (object?)nv.SoDienThoai ?? DBNull.Value), new SqlParameter("@Email", (object?)nv.Email ?? DBNull.Value),
                            CreateProfileImageParameter(nv.AnhDaiDien)
                        });
                        if (employee.ExecuteNonQuery() != 1) { tx.Rollback(); return false; }
                    }
                    using (var account = new SqlCommand("UPDATE TaiKhoan SET Quyen=@VaiTro WHERE MaNV=@MaNV", conn, tx))
                    {
                        account.Parameters.AddWithValue("@MaNV", nv.MaNV); account.Parameters.AddWithValue("@VaiTro", nv.ChucVu); account.ExecuteNonQuery();
                    }
                    string assignmentSql = nv.ChucVu is not ("BacSi" or "LeTan")
                        ? "DELETE FROM PhanCongPhong WHERE MaNV=@MaNV"
                        : "UPDATE PhanCongPhong SET VaiTro=@VaiTro WHERE MaNV=@MaNV";
                    using (var assignment = new SqlCommand(assignmentSql, conn, tx))
                    {
                        assignment.Parameters.AddWithValue("@MaNV", nv.MaNV);
                        if (nv.ChucVu is "BacSi" or "LeTan") assignment.Parameters.AddWithValue("@VaiTro", nv.ChucVu);
                        assignment.ExecuteNonQuery();
                    }
                    tx.Commit(); return true;
                }
                catch { tx.Rollback(); throw; }
            }
        }

        public static byte[]? LayAnhNhanVien(string maNV)
        {
            EnsureProfileImageSchema();
            object? result = DatabaseHelper.ExecuteScalar(
                "SELECT AnhDaiDien FROM NhanVien WHERE MaNV = @MaNV",
                new[] { new SqlParameter("@MaNV", maNV) });
            return result is byte[] bytes ? bytes : null;
        }

        public static bool XoaNhanVien(string maNV)
        {
            if (!SessionState.IsAdmin) throw new UnauthorizedAccessException("Chỉ quản trị viên được vô hiệu hóa nhân viên.");
            // Vô hiệu hóa thay vì xóa thật (soft delete)
            using var conn = DatabaseHelper.GetConnection();
            conn.Open(); using var tx = conn.BeginTransaction();
            try
            {
                using var cmd = new SqlCommand(@"UPDATE NhanVien SET TrangThai=0 WHERE MaNV=@MaNV;
                    UPDATE TaiKhoan SET TrangThai=0 WHERE MaNV=@MaNV;
                    DELETE FROM PhanCongPhong WHERE MaNV=@MaNV;", conn, tx);
                cmd.Parameters.AddWithValue("@MaNV", maNV);
                int affected = cmd.ExecuteNonQuery(); tx.Commit(); return affected > 0;
            }
            catch { tx.Rollback(); throw; }
        }

        public static bool LuuTaiKhoan(string maNV, string tenDangNhap, string matKhau, string quyen, bool isNew)
        {
            if (!SessionState.IsAdmin) throw new UnauthorizedAccessException("Chỉ quản trị viên được quản lý tài khoản nhân viên.");
            if (quyen is not ("Admin" or "BacSi" or "LeTan" or "ThuNgan"))
                throw new ArgumentOutOfRangeException(nameof(quyen), "Vai trò tài khoản không hợp lệ.");

            if (quyen == "BacSi")
            {
                string specialty = LayChuyenKhoaNhanVien(maNV).Trim();
                if (string.IsNullOrWhiteSpace(specialty) ||
                    Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                        "SELECT COUNT(*) FROM Khoa WHERE TenKhoa = @ChuyenKhoa",
                        new[] { new SqlParameter("@ChuyenKhoa", specialty) })) == 0)
                    throw new InvalidOperationException("Hãy gán chuyên khoa chính hợp lệ cho nhân viên trước khi cấp quyền bác sĩ.");

                int conflictingRooms = Convert.ToInt32(DatabaseHelper.ExecuteScalar(@"
                    SELECT COUNT(*)
                    FROM PhanCongPhong pc
                    INNER JOIN PhongKham p ON p.MaPhong = pc.MaPhong
                    INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                    WHERE pc.MaNV = @MaNV AND k.TenKhoa <> @ChuyenKhoa",
                    new[]
                    {
                        new SqlParameter("@MaNV", maNV),
                        new SqlParameter("@ChuyenKhoa", specialty)
                    }));
                if (conflictingRooms > 0)
                    throw new InvalidOperationException("Nhân viên đang được phân công ở nhiều chuyên khoa; hãy gỡ hoặc sửa phân công trước khi cấp quyền bác sĩ.");
            }

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                if (isNew)
                {
                    using var usernameCheck = new SqlCommand(@"SELECT
                        (SELECT COUNT(*) FROM TaiKhoan WITH (UPDLOCK,HOLDLOCK) WHERE TenDangNhap=@TDN) +
                        (SELECT COUNT(*) FROM TaiKhoanBenhNhan WITH (UPDLOCK,HOLDLOCK) WHERE TenDangNhap=@TDN)", conn, trans);
                    usernameCheck.Parameters.AddWithValue("@TDN", tenDangNhap.Trim());
                    if (Convert.ToInt32(usernameCheck.ExecuteScalar()) > 0)
                        throw new InvalidOperationException("Tên đăng nhập đã được dùng bởi tài khoản nhân viên hoặc bệnh nhân.");
                }
                string sql = isNew
                    ? @"INSERT INTO TaiKhoan (TenDangNhap, MatKhau, MaNV, Quyen, TrangThai)
                        VALUES (@TDN, @MK, @MaNV, @Quyen, 1)"
                    : @"UPDATE TaiKhoan SET MatKhau = @MK, Quyen = @Quyen WHERE MaNV = @MaNV";
                using var accountCommand = new SqlCommand(sql, conn, trans);
                accountCommand.Parameters.AddWithValue("@MK", HashPassword(matKhau));
                accountCommand.Parameters.AddWithValue("@MaNV", maNV);
                accountCommand.Parameters.AddWithValue("@Quyen", quyen);
                if (isNew)
                    accountCommand.Parameters.AddWithValue("@TDN", tenDangNhap);
                if (accountCommand.ExecuteNonQuery() != 1)
                {
                    trans.Rollback();
                    return false;
                }

                using var employeeCommand = new SqlCommand(
                    "UPDATE NhanVien SET ChucVu = @Quyen WHERE MaNV = @MaNV", conn, trans);
                employeeCommand.Parameters.AddWithValue("@Quyen", quyen);
                employeeCommand.Parameters.AddWithValue("@MaNV", maNV);
                if (employeeCommand.ExecuteNonQuery() != 1)
                    throw new InvalidOperationException("Không tìm thấy nhân viên để đồng bộ vai trò.");

                string assignmentsSql = quyen is not ("BacSi" or "LeTan")
                    ? "DELETE FROM PhanCongPhong WHERE MaNV = @MaNV"
                    : "UPDATE PhanCongPhong SET VaiTro = @Quyen WHERE MaNV = @MaNV";
                using var assignmentsCommand = new SqlCommand(assignmentsSql, conn, trans);
                assignmentsCommand.Parameters.AddWithValue("@MaNV", maNV);
                if (quyen is "BacSi" or "LeTan")
                    assignmentsCommand.Parameters.AddWithValue("@Quyen", quyen);
                assignmentsCommand.ExecuteNonQuery();

                trans.Commit();
                return true;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        // 9. DANH SÁCH CHỜ THANH TOÁN (đã khám xong, chưa có hóa đơn)
        public static DataTable LayDanhSachChoThanhToan(int? offset = null, int? pageSize = null, string? maLeTan = null)
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
                throw new UnauthorizedAccessException("Chỉ thu ngân hoặc quản trị viên được xem hồ sơ chờ lập hóa đơn.");
            string sql = @"
                SELECT pk.MaPK, pk.STT, pk.NgayKham, bn.HoTen AS TenBN, bn.SoDienThoai,
                       nv.HoTen AS TenBacSi, pk.ChanDoan, pk.TrangThai
                FROM PhieuKham pk
                INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                WHERE pk.TrangThai = N'Đã khám xong'
                  AND NOT EXISTS (SELECT 1 FROM HoaDon hd WHERE hd.MaPK = pk.MaPK AND hd.TrangThai <> N'Đã hủy')
                  AND (@MaLeTan IS NULL OR EXISTS (SELECT 1 FROM PhanCongPhong pc WHERE pc.MaNV=@MaLeTan AND pc.MaPhong=pk.MaPhong AND pc.VaiTro=N'LeTan'))
                ORDER BY pk.NgayKham ASC, pk.MaPK";
            return ExecutePagedQuery(sql, new[] { new SqlParameter("@MaLeTan", (object?)maLeTan ?? DBNull.Value) }, offset, pageSize);
        }

        // 10. XÓA THUỐC KHỎI ĐƠN (và hoàn trả tồn kho)
        public static bool XoaThuocKhoiDon(int maCTDT, string maThuoc, int soLuong, out string error)
        {
            if (!SessionState.IsDoctor) throw new UnauthorizedAccessException("Chỉ bác sĩ được sửa đơn thuốc.");
            error = string.Empty;
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                string actualDrug; int actualQuantity; string ticket;
                using (var read = new SqlCommand(@"SELECT ct.MaThuoc, ct.SoLuong, dt.MaPK, pk.MaNV_BacSi
                    FROM ChiTietDonThuoc ct WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN DonThuoc dt ON dt.MaDonThuoc=ct.MaDonThuoc INNER JOIN PhieuKham pk ON pk.MaPK=dt.MaPK WHERE ct.MaCTDT=@ID", conn, trans))
                {
                    read.Parameters.AddWithValue("@ID", maCTDT);
                    using var reader = read.ExecuteReader();
                    if (!reader.Read()) throw new InvalidOperationException("Dòng thuốc không còn tồn tại.");
                    actualDrug = reader.GetString(0); actualQuantity = reader.GetInt32(1); ticket = reader.GetString(2);
                    if (reader.GetString(3) != SessionState.CurrentUser!.MaNV) throw new UnauthorizedAccessException("Không được sửa đơn thuốc của bác sĩ khác.");
                }
                using (var guard = new SqlCommand("SELECT COUNT(*) FROM HoaDon WITH (UPDLOCK, HOLDLOCK) WHERE MaPK=@PK AND TrangThai<>N'Đã hủy'", conn, trans))
                {
                    guard.Parameters.AddWithValue("@PK", ticket);
                    if (Convert.ToInt32(guard.ExecuteScalar()) > 0) throw new InvalidOperationException("Không thể sửa đơn thuốc của phiếu đã có hóa đơn.");
                }
                using (var del = new SqlCommand("DELETE FROM ChiTietDonThuoc WHERE MaCTDT=@ID", conn, trans))
                { del.Parameters.AddWithValue("@ID", maCTDT); if (del.ExecuteNonQuery() != 1) throw new InvalidOperationException("Không xóa được dòng thuốc."); }
                using (var stock = new SqlCommand("UPDATE Thuoc SET SoLuongTon=SoLuongTon+@SL WHERE MaThuoc=@MT", conn, trans))
                { stock.Parameters.AddWithValue("@SL", actualQuantity); stock.Parameters.AddWithValue("@MT", actualDrug); if (stock.ExecuteNonQuery() != 1) throw new InvalidOperationException("Không tìm thấy thuốc để hoàn kho."); }

                trans.Commit();
                return true;
            }
            catch (Exception ex) { trans.Rollback(); error = ex.Message; return false; }
        }

        // 11. NHẬP THÊM TỒN KHO
        public static bool NhapKho(string maThuoc, int soLuongNhap)
        {
            throw new InvalidOperationException("Nhập kho cần phiếu nhập, nhà cung cấp, số lô, hạn dùng và giá nhập. Dùng chức năng Nhập lô thuốc.");
        }

        public static string NhapLoThuoc(string maThuoc, string nhaCungCap, string soLo, DateTime hanDung,
            int soLuong, decimal giaNhap, string? ghiChu = null)
        {
            EnsurePharmacySchema();
            if (!SessionState.IsAdmin && !SessionState.IsReceptionist) throw new UnauthorizedAccessException("Bạn không có quyền nhập kho.");
            if (soLuong <= 0 || giaNhap < 0 || hanDung.Date <= DateTime.Today || string.IsNullOrWhiteSpace(nhaCungCap) || string.IsNullOrWhiteSpace(soLo))
                throw new ArgumentException("Nhà cung cấp, số lô, hạn dùng trong tương lai, số lượng dương và giá nhập hợp lệ là bắt buộc.");
            using var conn = DatabaseHelper.GetConnection(); conn.Open();
            using var tx = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                int ncc;
                using (var find = new SqlCommand("SELECT MaNCC FROM NhaCungCap WITH (UPDLOCK,HOLDLOCK) WHERE TenNCC=@Ten", conn, tx))
                {
                    find.Parameters.AddWithValue("@Ten", nhaCungCap.Trim());
                    object? value = find.ExecuteScalar();
                    if (value == null || value == DBNull.Value)
                    {
                        using var add = new SqlCommand("INSERT NhaCungCap(TenNCC) VALUES(@Ten); SELECT CONVERT(int,SCOPE_IDENTITY())", conn, tx);
                        add.Parameters.AddWithValue("@Ten", nhaCungCap.Trim()); ncc = Convert.ToInt32(add.ExecuteScalar());
                    }
                    else ncc = Convert.ToInt32(value);
                }
                string maPN = "PN" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
                string actor = SessionState.CurrentUser!.MaNV;
                using (var receipt = new SqlCommand("INSERT PhieuNhapKho(MaPN,MaNCC,MaNV_Nhap,GhiChu) VALUES(@PN,@NCC,@NV,@Note)", conn, tx))
                {
                    receipt.Parameters.AddWithValue("@PN", maPN); receipt.Parameters.AddWithValue("@NCC", ncc);
                    receipt.Parameters.AddWithValue("@NV", actor); receipt.Parameters.AddWithValue("@Note", (object?)ghiChu ?? DBNull.Value); receipt.ExecuteNonQuery();
                }
                using (var lot = new SqlCommand(@"INSERT LoThuoc(MaPN,MaThuoc,SoLo,NgayHetHan,DonGiaNhap,SoLuongNhap,SoLuongCon)
                    SELECT @PN,MaThuoc,@SoLo,@Expiry,@Cost,@Qty,@Qty FROM Thuoc WHERE MaThuoc=@Drug", conn, tx))
                {
                    lot.Parameters.AddWithValue("@PN", maPN); lot.Parameters.AddWithValue("@Drug", maThuoc);
                    lot.Parameters.AddWithValue("@SoLo", soLo.Trim()); lot.Parameters.AddWithValue("@Expiry", hanDung.Date);
                    lot.Parameters.AddWithValue("@Cost", giaNhap); lot.Parameters.AddWithValue("@Qty", soLuong);
                    if (lot.ExecuteNonQuery()!=1) throw new InvalidOperationException("Không tìm thấy thuốc cần nhập.");
                }
                using (var stock = new SqlCommand("UPDATE Thuoc SET SoLuongTon=SoLuongTon+@Qty WHERE MaThuoc=@Drug", conn, tx))
                { stock.Parameters.AddWithValue("@Qty", soLuong); stock.Parameters.AddWithValue("@Drug", maThuoc); stock.ExecuteNonQuery(); }
                tx.Commit(); return maPN;
            }
            catch { tx.Rollback(); throw; }
        }

        public static bool DieuChinhLoThuoc(int maLo, int soLuongThayDoi, string lyDo)
        {
            EnsurePharmacySchema();
            if (!SessionState.IsAdmin && !SessionState.IsReceptionist) throw new UnauthorizedAccessException("Bạn không có quyền kiểm kê kho.");
            if (soLuongThayDoi == 0 || string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length > 255)
                throw new ArgumentException("Số lượng điều chỉnh khác 0 và lý do tối đa 255 ký tự là bắt buộc.");
            using var conn = DatabaseHelper.GetConnection(); conn.Open(); using var tx = conn.BeginTransaction();
            try
            {
                string drug;
                using (var read = new SqlCommand("SELECT MaThuoc FROM LoThuoc WITH (UPDLOCK,HOLDLOCK) WHERE MaLo=@Lot AND SoLuongCon+@Delta>=0", conn, tx))
                { read.Parameters.AddWithValue("@Lot", maLo); read.Parameters.AddWithValue("@Delta", soLuongThayDoi); drug = read.ExecuteScalar()?.ToString() ?? throw new InvalidOperationException("Lô không tồn tại hoặc điều chỉnh làm tồn lô âm."); }
                using (var update = new SqlCommand("UPDATE LoThuoc SET SoLuongCon=SoLuongCon+@Delta WHERE MaLo=@Lot", conn, tx))
                { update.Parameters.AddWithValue("@Delta", soLuongThayDoi); update.Parameters.AddWithValue("@Lot", maLo); update.ExecuteNonQuery(); }
                using (var stock = new SqlCommand("UPDATE Thuoc SET SoLuongTon=SoLuongTon+@Delta WHERE MaThuoc=@Drug AND SoLuongTon+@Delta>=0", conn, tx))
                { stock.Parameters.AddWithValue("@Delta", soLuongThayDoi); stock.Parameters.AddWithValue("@Drug", drug); if (stock.ExecuteNonQuery()!=1) throw new InvalidOperationException("Điều chỉnh làm tồn tổng âm."); }
                using (var log = new SqlCommand("INSERT DieuChinhKho(MaLo,MaNV,SoLuongThayDoi,LyDo) VALUES(@Lot,@NV,@Delta,@Reason)", conn, tx))
                { log.Parameters.AddWithValue("@Lot", maLo); log.Parameters.AddWithValue("@NV", SessionState.CurrentUser!.MaNV); log.Parameters.AddWithValue("@Delta", soLuongThayDoi); log.Parameters.AddWithValue("@Reason", lyDo.Trim()); log.ExecuteNonQuery(); }
                tx.Commit(); return true;
            }
            catch { tx.Rollback(); throw; }
        }

        public static DataTable LayDanhSachDonChoCapPhat()
        {
            EnsurePharmacySchema();
            string leTan = SessionState.IsReceptionist ? SessionState.CurrentUser?.MaNV ?? "" : "";
            const string sql = @"SELECT dt.MaDonThuoc,pk.MaPK,pk.MaPhong,bn.HoTen AS TenBN,dt.NgayKe,
                    COUNT(ct.MaCTDT) AS SoLoaiThuoc,SUM(ct.SoLuong) AS TongSoLuong
                FROM DonThuoc dt INNER JOIN PhieuKham pk ON pk.MaPK=dt.MaPK
                INNER JOIN BenhNhan bn ON bn.MaBN=pk.MaBN INNER JOIN ChiTietDonThuoc ct ON ct.MaDonThuoc=dt.MaDonThuoc
                INNER JOIN HoaDon hd ON hd.MaPK=pk.MaPK AND hd.TrangThai=N'Đã thanh toán'
                WHERE dt.TrangThaiCapPhat=N'Chờ cấp phát'
                  AND (@LeTan='' OR EXISTS(SELECT 1 FROM PhanCongPhong pc WHERE pc.MaNV=@LeTan AND pc.MaPhong=pk.MaPhong AND pc.VaiTro=N'LeTan'))
                GROUP BY dt.MaDonThuoc,pk.MaPK,pk.MaPhong,bn.HoTen,dt.NgayKe ORDER BY dt.NgayKe,dt.MaDonThuoc";
            return DatabaseHelper.ExecuteQuery(sql,new[]{new SqlParameter("@LeTan",leTan)});
        }

        public static bool CapPhatDonThuoc(string maPK)
        {
            EnsurePharmacySchema();
            if (!SessionState.IsAdmin && !SessionState.IsReceptionist) throw new UnauthorizedAccessException("Chỉ dược sĩ/lễ tân được cấp phát thuốc.");
            using var conn=DatabaseHelper.GetConnection(); conn.Open(); using var tx=conn.BeginTransaction(System.Data.IsolationLevel.Serializable);
            try
            {
                string maDon;
                using(var prescription=new SqlCommand(@"SELECT dt.MaDonThuoc FROM DonThuoc dt WITH(UPDLOCK,HOLDLOCK)
                    INNER JOIN PhieuKham pk ON pk.MaPK=dt.MaPK INNER JOIN HoaDon hd ON hd.MaPK=pk.MaPK
                    WHERE dt.MaPK=@PK AND dt.TrangThaiCapPhat=N'Chờ cấp phát' AND hd.TrangThai=N'Đã thanh toán'",conn,tx))
                { prescription.Parameters.AddWithValue("@PK",maPK); maDon=prescription.ExecuteScalar()?.ToString() ?? throw new InvalidOperationException("Đơn chưa kê, chưa thanh toán hoặc đã được xử lý."); }
                var items=new List<(int id,string drug,int qty)>();
                using(var read=new SqlCommand("SELECT MaCTDT,MaThuoc,SoLuong FROM ChiTietDonThuoc WHERE MaDonThuoc=@Don",conn,tx))
                { read.Parameters.AddWithValue("@Don",maDon); using var r=read.ExecuteReader(); while(r.Read()) items.Add((r.GetInt32(0),r.GetString(1),r.GetInt32(2))); }
                if(items.Count==0) throw new InvalidOperationException("Đơn thuốc không có chi tiết.");
                string actor=SessionState.CurrentUser!.MaNV;
                foreach(var item in items)
                {
                    int remaining=item.qty;
                    var lots=new List<(int id,int qty)>();
                    using(var find=new SqlCommand(@"SELECT MaLo,SoLuongCon FROM LoThuoc WITH(UPDLOCK,ROWLOCK)
                        WHERE MaThuoc=@Drug AND SoLuongCon>0 AND NgayHetHan>=CONVERT(date,GETDATE())
                        ORDER BY NgayHetHan,MaLo",conn,tx))
                    { find.Parameters.AddWithValue("@Drug",item.drug); using var r=find.ExecuteReader(); while(r.Read()) lots.Add((r.GetInt32(0),r.GetInt32(1))); }
                    int available=lots.Sum(x=>x.qty);
                    if(available<remaining) throw new InvalidOperationException($"Không đủ hàng còn hạn cho thuốc {item.drug}: cần {remaining}, còn {available}.");
                    foreach(var lot in lots)
                    {
                        if(remaining==0) break;
                        int take=Math.Min(remaining,lot.qty);
                        using(var update=new SqlCommand("UPDATE LoThuoc SET SoLuongCon=SoLuongCon-@Qty WHERE MaLo=@Lot AND SoLuongCon>=@Qty",conn,tx))
                        { update.Parameters.AddWithValue("@Qty",take); update.Parameters.AddWithValue("@Lot",lot.id); if(update.ExecuteNonQuery()!=1) throw new InvalidOperationException("Lô đã được cấp phát đồng thời; thử lại."); }
                        using(var add=new SqlCommand("INSERT ChiTietCapPhatThuoc(MaCTDT,MaLo,MaNV,SoLuong) VALUES(@Detail,@Lot,@NV,@Qty)",conn,tx))
                        { add.Parameters.AddWithValue("@Detail",item.id); add.Parameters.AddWithValue("@Lot",lot.id); add.Parameters.AddWithValue("@NV",actor); add.Parameters.AddWithValue("@Qty",take); add.ExecuteNonQuery(); }
                        remaining-=take;
                    }
                    using(var stock=new SqlCommand("UPDATE Thuoc SET SoLuongTon=SoLuongTon-@Qty WHERE MaThuoc=@Drug AND SoLuongTon>=@Qty",conn,tx))
                    { stock.Parameters.AddWithValue("@Qty",item.qty); stock.Parameters.AddWithValue("@Drug",item.drug); if(stock.ExecuteNonQuery()!=1) throw new InvalidOperationException("Tồn tổng và tồn lô không khớp."); }
                }
                using(var finish=new SqlCommand("UPDATE DonThuoc SET TrangThaiCapPhat=N'Đã cấp',NgayCapPhat=GETDATE(),MaNV_CapPhat=@NV WHERE MaDonThuoc=@Don AND TrangThaiCapPhat=N'Chờ cấp phát'",conn,tx))
                { finish.Parameters.AddWithValue("@NV",actor); finish.Parameters.AddWithValue("@Don",maDon); if(finish.ExecuteNonQuery()!=1) throw new InvalidOperationException("Đơn thuốc đã được xử lý."); }
                tx.Commit(); return true;
            }
            catch { tx.Rollback(); throw; }
        }

        public static bool DanhDauDonThuocKhongCap(string maPK,string trangThai,string lyDo)
        {
            EnsurePharmacySchema();
            if(!SessionState.IsAdmin && !SessionState.IsReceptionist) throw new UnauthorizedAccessException("Chỉ dược sĩ/lễ tân được xử lý đơn thuốc.");
            if(trangThai is not ("Không lấy" or "Mua ngoài") || string.IsNullOrWhiteSpace(lyDo)) throw new ArgumentException("Chọn trạng thái và nhập lý do.");
            return DatabaseHelper.ExecuteNonQuery(@"UPDATE dt SET TrangThaiCapPhat=@Status,NgayCapPhat=GETDATE(),MaNV_CapPhat=@NV,GhiChuCapPhat=@Reason
                FROM DonThuoc dt INNER JOIN PhieuKham pk ON pk.MaPK=dt.MaPK INNER JOIN HoaDon hd ON hd.MaPK=pk.MaPK
                WHERE dt.MaPK=@PK AND dt.TrangThaiCapPhat=N'Chờ cấp phát' AND hd.TrangThai=N'Đã thanh toán'",new[]{
                new SqlParameter("@Status",trangThai),new SqlParameter("@NV",SessionState.CurrentUser!.MaNV),new SqlParameter("@PK",maPK),new SqlParameter("@Reason",lyDo.Trim())})>0;
        }

        public static bool XoaThuoc(string maThuoc, out string error)
        {
            error = string.Empty;
            try
            {
                int inUse = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM ChiTietDonThuoc WHERE MaThuoc=@MT",
                    new[] { new SqlParameter("@MT", maThuoc) }));
                if (inUse > 0) { error = "Thuốc đã được sử dụng trong đơn, không thể xóa!"; return false; }
                DatabaseHelper.ExecuteNonQuery("DELETE FROM Thuoc WHERE MaThuoc=@MT",
                    new[] { new SqlParameter("@MT", maThuoc) });
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        // 12. THỐNG KÊ CHI TIẾT
        public static (int tongKham, decimal tongThu, int tongThuocSapHet) LayThongKeTongQuan()
        {
            string sqlKham = "SELECT COUNT(*) FROM PhieuKham WHERE CAST(NgayKham AS DATE) = CAST(GETDATE() AS DATE)";
            int kham = Convert.ToInt32(DatabaseHelper.ExecuteScalar(sqlKham));
            string sqlThu = @"
                SELECT ISNULL(SUM(TongTien), 0)
                FROM HoaDon
                WHERE TrangThai = N'Đã thanh toán'
                  AND CAST(COALESCE(NgayThanhToan, NgayLap) AS DATE) = CAST(GETDATE() AS DATE)";
            decimal thu = Convert.ToDecimal(DatabaseHelper.ExecuteScalar(sqlThu));
            string sqlThuoc = "SELECT COUNT(*) FROM Thuoc WHERE SoLuongTon <= 30";
            int thuoc = Convert.ToInt32(DatabaseHelper.ExecuteScalar(sqlThuoc));
            return (kham, thu, thuoc);
        }

        public static DataTable LayThongKeKhoaPhong(int soNgay = 30)
        {
            const string sql = @"
                SELECT k.TenKhoa, p.TenPhong,
                       COUNT(pk.MaPK) AS SoLuotKham,
                       COUNT(DISTINCT pk.MaBN) AS SoBenhNhan,
                       SUM(CASE WHEN pk.TrangThai = N'Đã khám xong' THEN 1 ELSE 0 END) AS SoCaHoanTat
                FROM PhongKham p
                INNER JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                LEFT JOIN PhieuKham pk ON pk.MaPhong = p.MaPhong
                    AND pk.NgayKham >= DATEADD(DAY, -@SoNgay, CONVERT(date, GETDATE()))
                    AND pk.NgayKham < DATEADD(DAY, 1, CONVERT(date, GETDATE()))
                GROUP BY k.TenKhoa, p.TenPhong
                ORDER BY k.TenKhoa, p.TenPhong";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@SoNgay", soNgay)
            });
        }

        public static bool CoSchemaKhoaPhong()
        {
            const string sql = @"
                SELECT CASE WHEN
                    OBJECT_ID(N'dbo.Khoa', N'U') IS NOT NULL
                    AND OBJECT_ID(N'dbo.PhongKham', N'U') IS NOT NULL
                    AND COL_LENGTH(N'dbo.Khoa', N'MaKhoa') IS NOT NULL
                    AND COL_LENGTH(N'dbo.Khoa', N'TenKhoa') IS NOT NULL
                    AND COL_LENGTH(N'dbo.PhongKham', N'MaPhong') IS NOT NULL
                    AND COL_LENGTH(N'dbo.PhongKham', N'MaKhoa') IS NOT NULL
                    AND COL_LENGTH(N'dbo.PhongKham', N'TenPhong') IS NOT NULL
                    AND COL_LENGTH(N'dbo.PhieuKham', N'MaPhong') IS NOT NULL
                    THEN 1 ELSE 0 END";
            return Convert.ToBoolean(DatabaseHelper.ExecuteScalar(sql));
        }

        public static DataTable LayDoanhThu7Ngay()
        {
            string sql = @"
                SELECT CAST(COALESCE(NgayThanhToan, NgayLap) AS DATE) AS Ngay, COUNT(*) AS SoHoaDon,
                       SUM(TongTien) AS DoanhThu
                FROM HoaDon WHERE TrangThai = N'Đã thanh toán'
                  AND COALESCE(NgayThanhToan, NgayLap) >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                GROUP BY CAST(COALESCE(NgayThanhToan, NgayLap) AS DATE)
                ORDER BY Ngay";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static DataTable LayThuocSapHet(int nguong = 30)
        {
            EnsurePharmacySchema();
            string sql = @"SELECT t.MaThuoc,t.TenThuoc,t.DonViTinh,t.SoLuongTon,t.DonGiaBan,
                    l.SoLo,l.NgayHetHan,DATEDIFF(DAY,CONVERT(date,GETDATE()),l.NgayHetHan) AS SoNgayConLai,
                    CASE WHEN t.SoLuongTon<=@N THEN N'Tồn thấp' ELSE N'Sắp hết hạn' END AS CanhBao
                FROM Thuoc t LEFT JOIN LoThuoc l ON l.MaThuoc=t.MaThuoc AND l.SoLuongCon>0 AND l.NgayHetHan<=DATEADD(DAY,90,CONVERT(date,GETDATE()))
                WHERE t.SoLuongTon<=@N OR l.MaLo IS NOT NULL ORDER BY CASE WHEN l.MaLo IS NULL THEN 1 ELSE 0 END,l.NgayHetHan,t.SoLuongTon";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@N", nguong) });
        }

        public static DataTable LayDanhSachLoThuoc(string maThuoc)
        {
            EnsurePharmacySchema();
            return DatabaseHelper.ExecuteQuery(@"SELECT l.MaLo,l.SoLo,l.NgayHetHan,l.SoLuongCon,l.DonGiaNhap,pn.MaPN,n.TenNCC
                FROM LoThuoc l JOIN PhieuNhapKho pn ON pn.MaPN=l.MaPN JOIN NhaCungCap n ON n.MaNCC=pn.MaNCC
                WHERE l.MaThuoc=@Drug ORDER BY l.NgayHetHan,l.MaLo",new[]{new SqlParameter("@Drug",maThuoc)});
        }

        public static DataTable LayTopChanDoan(int top = 10)
        {
            string sql = @"
                SELECT TOP (@Top) ChanDoan, COUNT(*) AS SoLuot, COUNT(*) AS SoLuong
                FROM PhieuKham WHERE ChanDoan IS NOT NULL AND ChanDoan <> ''
                GROUP BY ChanDoan ORDER BY SoLuot DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Top", top) });
        }

        public static DataTable LayLichSuKhamBenhNhan(string maBN, string? maBS = null)
        {
            GhiNhatKy("Xem bệnh án", maBN, maBS == null ? "Lịch sử hồ sơ" : $"Bác sĩ {maBS} xem hồ sơ");
            EnsureClinicalRecordColumns();
            string sql = @"
                SELECT pk.MaPK, pk.NgayKham, pk.TrieuChung, pk.KhamLamSang, pk.SinhHieu, pk.ChanDoan, pk.LoiDan,
                       nv.HoTen AS TenBacSi, pk.TrangThai
                FROM PhieuKham pk
                INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                WHERE pk.MaBN = @MaBN
                  AND (@MaBS IS NULL OR pk.MaNV_BacSi = @MaBS)
                ORDER BY pk.NgayKham DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaBN", maBN),
                new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value)
            });
        }

        public static DataTable LayHoaDonBenhNhan(string maBN)
        {
            const string sql = @"
                SELECT hd.MaHD, hd.MaPK, hd.NgayLap, hd.TienDichVu, hd.TienThuoc,
                       hd.GiamGia, hd.TongTien, hd.HinhThucTT, hd.TrangThai
                FROM HoaDon hd
                INNER JOIN PhieuKham pk ON pk.MaPK = hd.MaPK
                WHERE pk.MaBN = @MaBN
                ORDER BY hd.NgayLap DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaBN", maBN) });
        }

        public static bool XoaBenhNhan(string maBN, out string error)
        {
            error = string.Empty;
            try
            {
                int inUse = Convert.ToInt32(DatabaseHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM PhieuKham WHERE MaBN=@MB",
                    new[] { new SqlParameter("@MB", maBN) }));
                if (inUse > 0) { error = "Bệnh nhân đã có phiếu khám, không thể xóa!"; return false; }
                DatabaseHelper.ExecuteNonQuery("DELETE FROM BenhNhan WHERE MaBN=@MB",
                    new[] { new SqlParameter("@MB", maBN) });
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        public static string SinhMaDichVu()
        {
            string sql = "SELECT TOP 1 MaDV FROM DichVu ORDER BY MaDV DESC";
            var result = DatabaseHelper.ExecuteScalar(sql);
            if (result == null || result == DBNull.Value) return "DV01";
            string current = result.ToString()!;
            if (current.StartsWith("DV") && int.TryParse(current.Substring(2), out int num))
                return $"DV{(num + 1):D2}";
            return "DV01";
        }

        public static bool LuuDichVu(DichVu dv, bool isNew)
        {
            if (isNew)
            {
                string sql = "INSERT INTO DichVu (MaDV, TenDV, DonGia, MoTa) VALUES (@MaDV,@TenDV,@DonGia,@MoTa)";
                return DatabaseHelper.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@MaDV", dv.MaDV), new SqlParameter("@TenDV", dv.TenDV),
                    new SqlParameter("@DonGia", dv.DonGia), new SqlParameter("@MoTa", (object?)dv.MoTa ?? DBNull.Value)
                }) > 0;
            }
            else
            {
                string sql = "UPDATE DichVu SET TenDV=@TenDV, DonGia=@DonGia, MoTa=@MoTa WHERE MaDV=@MaDV";
                return DatabaseHelper.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@MaDV", dv.MaDV), new SqlParameter("@TenDV", dv.TenDV),
                    new SqlParameter("@DonGia", dv.DonGia), new SqlParameter("@MoTa", (object?)dv.MoTa ?? DBNull.Value)
                }) > 0;
            }
        }

        // 13. DÀNH CHO ROLE BÁC SĨ (LỊCH KHÁM, HỒ SƠ BỆNH ÁN, TRA CỨU CHUYÊN MÔN)
        public static (int tongPhanCong, int daHoanTat, int dangKham, int dangCho, int choKQCLS, string luotTiepTheo) LayThongKeBacSi(string? maBS)
        {
            try
            {
                string sql = @"
                    SELECT 
                        COUNT(*) AS Tong,
                        SUM(CASE WHEN TrangThai = N'Đã khám xong' THEN 1 ELSE 0 END) AS DaHoanTat,
                        SUM(CASE WHEN TrangThai = N'Đang khám' THEN 1 ELSE 0 END) AS DangKham,
                        SUM(CASE WHEN TrangThai = N'Chờ khám' THEN 1 ELSE 0 END) AS DangCho,
                        SUM(CASE WHEN TrangThai = N'Chờ kết quả CLS' THEN 1 ELSE 0 END) AS ChoKQCLS
                    FROM PhieuKham
                    WHERE CAST(NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                      AND (@MaBS IS NULL OR MaNV_BacSi = @MaBS)
                      AND TrangThai <> N'Đã hủy'";
                var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value) });
                int tong = 0, daHoanTat = 0, dangKham = 0, dangCho = 0, choKQCLS = 0;
                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    tong = r["Tong"] != DBNull.Value ? Convert.ToInt32(r["Tong"]) : 0;
                    daHoanTat = r["DaHoanTat"] != DBNull.Value ? Convert.ToInt32(r["DaHoanTat"]) : 0;
                    dangKham = r["DangKham"] != DBNull.Value ? Convert.ToInt32(r["DangKham"]) : 0;
                    dangCho = r["DangCho"] != DBNull.Value ? Convert.ToInt32(r["DangCho"]) : 0;
                    choKQCLS = r["ChoKQCLS"] != DBNull.Value ? Convert.ToInt32(r["ChoKQCLS"]) : 0;
                }

                string sqlNext = @"
                    SELECT TOP 1 FORMAT(pk.NgayKham, 'HH:mm') AS Gio, bn.HoTen, pk.MaBN
                    FROM PhieuKham pk
                    INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                    WHERE CAST(pk.NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                      AND pk.TrangThai IN (N'Chờ khám', N'Đang khám')
                      AND (@MaBS IS NULL OR pk.MaNV_BacSi = @MaBS)
                    ORDER BY pk.STT ASC";
                var dtNext = DatabaseHelper.ExecuteQuery(sqlNext, new[] { new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value) });
                string nextStr = "Chưa có";
                if (dtNext.Rows.Count > 0)
                {
                    nextStr = $"{dtNext.Rows[0]["Gio"]} - {dtNext.Rows[0]["HoTen"]}";
                }
                return (tong, daHoanTat, dangKham, dangCho, choKQCLS, nextStr);
            }
            catch
            {
                return (0, 0, 0, 0, 0, "Chưa có");
            }
        }

        public static (string tenPhong, string tenKhoa, string caTruc) LayThongTinCaTrucBacSi(string maBS)
        {
            EnsureAdvancedWorkflowSchema();
            try
            {
                string sql = @"
                    SELECT TOP 1
                        ISNULL(p.TenPhong, N'Phòng khám chuyên khoa') AS TenPhong,
                        ISNULL(k.TenKhoa, N'Khoa Khám bệnh') AS TenKhoa,
                        ISNULL(CONVERT(VARCHAR(5), clv.GioBatDau, 108) + ' - ' + CONVERT(VARCHAR(5), clv.GioKetThuc, 108), N'Ca hành chính (07:30 - 17:00)') AS CaTruc
                    FROM dbo.NhanVien nv
                    LEFT JOIN dbo.PhanCongPhong pc ON pc.MaNV = nv.MaNV AND pc.VaiTro = N'BacSi'
                    LEFT JOIN dbo.PhongKham p ON p.MaPhong = pc.MaPhong
                    LEFT JOIN dbo.Khoa k ON k.MaKhoa = p.MaKhoa
                    LEFT JOIN dbo.CaLamViecBacSi clv ON clv.MaNV = nv.MaNV AND clv.Thu = DATEPART(dw, GETDATE())
                    WHERE nv.MaNV = @MaBS";
                var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaBS", maBS) });
                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    return (
                        r["TenPhong"].ToString() ?? "Phòng khám chuyên khoa",
                        r["TenKhoa"].ToString() ?? "Khoa Khám bệnh",
                        r["CaTruc"].ToString() ?? "Ca hành chính (07:30 - 17:00)"
                    );
                }
            }
            catch { }
            return ("Phòng khám chuyên khoa", "Khoa Khám bệnh", "Ca hành chính (07:30 - 17:00)");
        }

        public static DataTable LayDanhSachTheoDoiCLSBacSi(string maBS)
        {
            EnsureAdvancedWorkflowSchema();
            try
            {
                string sql = @"
                    SELECT 
                        pk.MaPK,
                        pk.STT,
                        bn.MaBN,
                        bn.HoTen AS TenBN,
                        pk.TrangThai AS TrangThaiPK,
                        COUNT(ct.MaChiDinh) AS TongCLS,
                        SUM(CASE WHEN ct.TrangThai = N'Đã có kết quả' THEN 1 ELSE 0 END) AS DaCoKQ,
                        CASE 
                            WHEN COUNT(ct.MaChiDinh) > 0 AND COUNT(ct.MaChiDinh) = SUM(CASE WHEN ct.TrangThai = N'Đã có kết quả' THEN 1 ELSE 0 END)
                            THEN N'Sẵn sàng kết luận'
                            ELSE N'Đang chờ kết quả'
                        END AS TrangThaiKQ
                    FROM dbo.PhieuKham pk
                    INNER JOIN dbo.BenhNhan bn ON bn.MaBN = pk.MaBN
                    INNER JOIN dbo.ChiTietChiDinhDV ct ON ct.MaPK = pk.MaPK
                    WHERE pk.MaNV_BacSi = @MaBS
                      AND CAST(pk.NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                      AND pk.TrangThai IN (N'Chờ kết quả CLS', N'Đang khám')
                    GROUP BY pk.MaPK, pk.STT, bn.MaBN, bn.HoTen, pk.TrangThai
                    ORDER BY 
                        CASE WHEN COUNT(ct.MaChiDinh) = SUM(CASE WHEN ct.TrangThai = N'Đã có kết quả' THEN 1 ELSE 0 END) THEN 0 ELSE 1 END,
                        pk.STT ASC";
                return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaBS", maBS) });
            }
            catch
            {
                return new DataTable();
            }
        }

        public static DataTable LayTopChanDoanBacSi(string maBS, int top = 3)
        {
            try
            {
                string sql = @"
                    SELECT TOP (@Top) ISNULL(ChanDoan, N'Khác') AS ChanDoan, COUNT(*) AS SoLuot, COUNT(*) AS SoLuong
                    FROM dbo.PhieuKham
                    WHERE MaNV_BacSi = @MaBS
                      AND ChanDoan IS NOT NULL AND RTRIM(LTRIM(ChanDoan)) <> ''
                      AND NgayKham >= DATEADD(day, -30, GETDATE())
                    GROUP BY ChanDoan
                    ORDER BY SoLuot DESC";
                return DatabaseHelper.ExecuteQuery(sql, new[]
                {
                    new SqlParameter("@MaBS", maBS),
                    new SqlParameter("@Top", top)
                });
            }
            catch
            {
                return new DataTable();
            }
        }

        public static DataTable LayDanhSachKhamBacSi(
            string? maBS,
            string keyword = "",
            string? trangThai = null,
            int? offset = null,
            int? pageSize = null,
            string? maPhong = null)
        {
            string sql = @"
                SELECT pk.MaPK, pk.STT, FORMAT(pk.NgayKham, 'HH:mm') AS GioKham,
                       pk.MaBN, bn.HoTen AS TenBN, bn.GioiTinh,
                       CASE WHEN bn.NgaySinh IS NULL THEN NULL ELSE DATEDIFF(YEAR, bn.NgaySinh, GETDATE()) - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, bn.NgaySinh, GETDATE()), bn.NgaySinh) > CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END END AS Tuoi,
                       ISNULL(pk.TrieuChung, N'Khám sức khỏe tổng quát') AS LyDoKham,
                       pk.TrangThai, nv.HoTen AS TenBacSi, bn.SoDienThoai,
                       k.TenKhoa, p.TenPhong
                FROM PhieuKham pk
                INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                LEFT JOIN PhongKham p ON p.MaPhong = pk.MaPhong
                LEFT JOIN Khoa k ON k.MaKhoa = p.MaKhoa
                WHERE CAST(pk.NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                  AND (@MaBS IS NULL OR pk.MaNV_BacSi = @MaBS)
                  AND (@MaPhong IS NULL OR pk.MaPhong = @MaPhong)
                  AND (@Kw = '' OR bn.HoTen LIKE '%' + @Kw + '%' OR bn.MaBN LIKE '%' + @Kw + '%' OR pk.MaPK LIKE '%' + @Kw + '%')
                  AND (@TrangThai IS NULL OR @TrangThai = '' OR @TrangThai = N'Tất cả trạng thái' OR pk.TrangThai = @TrangThai)
                ORDER BY pk.STT ASC, pk.MaPK";
            return ExecutePagedQuery(sql, new[]
            {
                new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value),
                new SqlParameter("@MaPhong", (object?)maPhong ?? DBNull.Value),
                new SqlParameter("@Kw", keyword),
                new SqlParameter("@TrangThai", (object?)trangThai ?? DBNull.Value)
            }, offset, pageSize);
        }

        public static DataRow? LayChiTietPhieuKhamDayDu(string maPK, string maBS)
        {
            EnsureClinicalRecordColumns();
            EnsureVisitLineageSchema();
            string sql = @"
                SELECT pk.MaPK, pk.MaPK_Goc, pk.LyDoChuyen, pk.STT, pk.NgayKham, pk.TrieuChung, pk.KhamLamSang, pk.SinhHieu, pk.ChanDoan, pk.LoiDan, pk.TrangThai,
                       bn.MaBN, bn.HoTen AS TenBN, bn.GioiTinh, bn.NgaySinh, bn.SoDienThoai, bn.DiaChi, bn.TienSuBenh,
                       CASE WHEN bn.NgaySinh IS NULL THEN NULL ELSE DATEDIFF(YEAR, bn.NgaySinh, GETDATE()) - CASE WHEN DATEADD(YEAR, DATEDIFF(YEAR, bn.NgaySinh, GETDATE()), bn.NgaySinh) > CAST(GETDATE() AS DATE) THEN 1 ELSE 0 END END AS Tuoi,
                       nv.MaNV AS MaBacSi, nv.HoTen AS TenBacSi, nv.ChuyenKhoa
                FROM PhieuKham pk
                INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                WHERE pk.MaPK = @MaPK AND pk.MaNV_BacSi = @MaBS";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
                new SqlParameter("@MaBS", maBS)
            });
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public static DataTable TraCuuChuyenMon(string keyword, string tab = "TatCa")
        {
            var dtResult = new DataTable();
            dtResult.Columns.Add("Loai", typeof(string));
            dtResult.Columns.Add("Ma", typeof(string));
            dtResult.Columns.Add("TieuDe", typeof(string));
            dtResult.Columns.Add("MoTa", typeof(string));
            dtResult.Columns.Add("ChiTiet", typeof(string));

            keyword = keyword.Trim().ToLower();

            // 1. Thuốc
            if (tab == "TatCa" || tab == "Thuoc")
            {
                var dtT = LayDanhSachThuoc(keyword);
                foreach (DataRow r in dtT.Rows)
                {
                    dtResult.Rows.Add(
                        "Thuốc tân dược",
                        r["MaThuoc"].ToString(),
                        $"{r["TenThuoc"]} ({r["DonViTinh"]})",
                        $"Giá: {Convert.ToDecimal(r["DonGiaBan"]):N0} đ | Tồn kho: {r["SoLuongTon"]} {r["DonViTinh"]}",
                        r["CachDungMacDinh"].ToString()
                    );
                }
            }

            // 2. Dịch vụ
            if (tab == "TatCa" || tab == "DichVu")
            {
                string sqlDV = "SELECT MaDV, TenDV, DonGia, MoTa FROM DichVu WHERE @Kw = '' OR TenDV LIKE '%' + @Kw + '%' OR MaDV LIKE '%' + @Kw + '%'";
                var dtDV = DatabaseHelper.ExecuteQuery(sqlDV, new[] { new SqlParameter("@Kw", keyword) });
                foreach (DataRow r in dtDV.Rows)
                {
                    dtResult.Rows.Add(
                        "Dịch vụ CLS",
                        r["MaDV"].ToString(),
                        r["TenDV"].ToString(),
                        $"Đơn giá: {Convert.ToDecimal(r["DonGia"]):N0} đ",
                        r["MoTa"].ToString()
                    );
                }
            }

            // 3. Bệnh án / Phiếu khám
            if (tab == "TatCa" || tab == "BenhAn")
            {
                string sqlBA = @"
                    SELECT TOP 20 pk.MaPK, pk.NgayKham, bn.HoTen AS TenBN, bn.MaBN, pk.ChanDoan, pk.TrieuChung, nv.HoTen AS TenBS
                    FROM PhieuKham pk
                    INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                    INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                    WHERE (@MaBS IS NULL OR pk.MaNV_BacSi=@MaBS) AND (@Kw = '' OR bn.HoTen LIKE '%' + @Kw + '%' OR pk.ChanDoan LIKE '%' + @Kw + '%' OR pk.MaPK LIKE '%' + @Kw + '%')
                    ORDER BY pk.NgayKham DESC";
                var dtBA = DatabaseHelper.ExecuteQuery(sqlBA, new[] { new SqlParameter("@Kw", keyword), new SqlParameter("@MaBS", SessionState.IsDoctor ? SessionState.CurrentUser!.MaNV : DBNull.Value) });
                foreach (DataRow r in dtBA.Rows)
                {
                    dtResult.Rows.Add(
                        "Bệnh án / Phiếu khám",
                        r["MaPK"].ToString(),
                        $"Phiếu {r["MaPK"]} - {r["TenBN"]} ({r["MaBN"]})",
                        $"Ngày: {Convert.ToDateTime(r["NgayKham"]):dd/MM/yyyy} | BS: {r["TenBS"]}",
                        $"Chẩn đoán: {r["ChanDoan"]} | Triệu chứng: {r["TrieuChung"]}"
                    );
                }
            }

            // 4. Tài liệu & Phác đồ mẫu
            if (tab == "TatCa" || tab == "TaiLieu")
            {
                var sampleDocs = new[]
                {
                    ("Hướng dẫn chẩn đoán và điều trị sốt xuất huyết Dengue", "Phác đồ Bộ Y Tế", "Theo dõi sát công thức máu và lượng tiểu cầu hằng ngày."),
                    ("Phác đồ kiểm soát huyết áp cho người cao tuổi", "Khuyến cáo tim mạch", "Khởi đầu liều thấp, kiểm tra điện giải và chức năng thận."),
                    ("Quy trình an toàn sử dụng kháng sinh dòng Beta-lactam", "Dược lâm sàng", "Luôn khai thác tiền sử dị ứng và test lẩy da khi cần thiết."),
                    ("Quy chuẩn đọc kết quả công thức máu 18 thông số", "Xét nghiệm y khoa", "Đối chiếu Hemoglobin, Bạch cầu, Tiểu cầu và công thức bạch cầu.")
                };

                foreach (var doc in sampleDocs)
                {
                    if (string.IsNullOrEmpty(keyword) || doc.Item1.ToLower().Contains(keyword) || doc.Item2.ToLower().Contains(keyword))
                    {
                        dtResult.Rows.Add("Tài liệu & Phác đồ", "DOC-REF", doc.Item1, doc.Item2, doc.Item3);
                    }
                }
            }

            return dtResult;
        }

        public static DataTable TraCuuChuyenMonPaged(string keyword, string tab, int offset, int pageSize)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");

            var result = new DataTable();
            result.Columns.Add("Loai", typeof(string));
            result.Columns.Add("Ma", typeof(string));
            result.Columns.Add("TieuDe", typeof(string));
            result.Columns.Add("MoTa", typeof(string));
            result.Columns.Add("ChiTiet", typeof(string));

            keyword = keyword.Trim().ToLowerInvariant();
            int totalCount = 0;
            int requestedEnd = checked(offset + pageSize);

            void AppendSegment(
                int segmentCount,
                Func<int, int, DataTable> loadSegment,
                Action<DataRow> appendRow)
            {
                int segmentStart = totalCount;
                totalCount = checked(totalCount + segmentCount);
                int localStart = Math.Max(offset, segmentStart);
                int localEnd = Math.Min(requestedEnd, totalCount);
                if (localStart >= localEnd)
                    return;

                DataTable page = loadSegment(localStart - segmentStart, localEnd - localStart);
                foreach (DataRow row in page.Rows)
                    appendRow(row);
            }

            int GetTotalCount(DataTable page) =>
                page.ExtendedProperties["TotalCount"] is int count
                    ? count
                    : throw new InvalidOperationException("The paged lookup query did not return its total count.");

            if (tab is "TatCa" or "Thuoc")
            {
                DataTable probe = LayDanhSachThuoc(keyword, 0, 1);
                int count = GetTotalCount(probe);
                AppendSegment(count,
                    (pageOffset, countPerPage) => LayDanhSachThuoc(keyword, pageOffset, countPerPage),
                    row => result.Rows.Add(
                        "Thuốc tân dược",
                        row["MaThuoc"].ToString(),
                        $"{row["TenThuoc"]} ({row["DonViTinh"]})",
                        $"Giá: {Convert.ToDecimal(row["DonGiaBan"]):N0} đ | Tồn kho: {row["SoLuongTon"]} {row["DonViTinh"]}",
                        row["CachDungMacDinh"].ToString()));
            }

            if (tab is "TatCa" or "DichVu")
            {
                const string serviceSql = @"
                    SELECT MaDV, TenDV, DonGia, MoTa
                    FROM DichVu
                    WHERE @Kw = '' OR TenDV LIKE '%' + @Kw + '%' OR MaDV LIKE '%' + @Kw + '%'
                    ORDER BY TenDV, MaDV";
                var serviceParameters = new[] { new SqlParameter("@Kw", keyword) };
                int count = GetTotalCount(ExecutePagedQuery(serviceSql, serviceParameters, 0, 1));
                AppendSegment(count,
                    (pageOffset, countPerPage) => ExecutePagedQuery(serviceSql, serviceParameters, pageOffset, countPerPage),
                    row => result.Rows.Add(
                        "Dịch vụ CLS",
                        row["MaDV"].ToString(),
                        row["TenDV"].ToString(),
                        $"Đơn giá: {Convert.ToDecimal(row["DonGia"]):N0} đ",
                        row["MoTa"].ToString()));
            }

            if (tab is "TatCa" or "BenhAn")
            {
                const string recordSql = @"
                    SELECT pk.MaPK, pk.NgayKham, bn.HoTen AS TenBN, bn.MaBN,
                           pk.ChanDoan, pk.TrieuChung, nv.HoTen AS TenBS
                    FROM PhieuKham pk
                    INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                    INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                    WHERE (@MaBS IS NULL OR pk.MaNV_BacSi=@MaBS) AND (@Kw = '' OR bn.HoTen LIKE '%' + @Kw + '%'
                        OR pk.ChanDoan LIKE '%' + @Kw + '%' OR pk.MaPK LIKE '%' + @Kw + '%')
                    ORDER BY pk.NgayKham DESC, pk.MaPK DESC";
                var recordParameters = new[] { new SqlParameter("@Kw", keyword), new SqlParameter("@MaBS", SessionState.IsDoctor ? SessionState.CurrentUser!.MaNV : DBNull.Value) };
                int count = GetTotalCount(ExecutePagedQuery(recordSql, recordParameters, 0, 1));
                AppendSegment(count,
                    (pageOffset, countPerPage) => ExecutePagedQuery(recordSql, recordParameters, pageOffset, countPerPage),
                    row => result.Rows.Add(
                        "Bệnh án / Phiếu khám",
                        row["MaPK"].ToString(),
                        $"Phiếu {row["MaPK"]} - {row["TenBN"]} ({row["MaBN"]})",
                        $"Ngày: {Convert.ToDateTime(row["NgayKham"]):dd/MM/yyyy} | BS: {row["TenBS"]}",
                        $"Chẩn đoán: {row["ChanDoan"]} | Triệu chứng: {row["TrieuChung"]}"));
            }

            if (tab is "TatCa" or "TaiLieu")
            {
                var sampleDocs = new[]
                {
                    ("Hướng dẫn chẩn đoán và điều trị sốt xuất huyết Dengue", "Phác đồ Bộ Y Tế", "Theo dõi sát công thức máu và lượng tiểu cầu hằng ngày."),
                    ("Phác đồ kiểm soát huyết áp cho người cao tuổi", "Khuyến cáo tim mạch", "Khởi đầu liều thấp, kiểm tra điện giải và chức năng thận."),
                    ("Quy trình an toàn sử dụng kháng sinh dòng Beta-lactam", "Dược lâm sàng", "Luôn khai thác tiền sử dị ứng và test lẩy da khi cần thiết."),
                    ("Quy chuẩn đọc kết quả công thức máu 18 thông số", "Xét nghiệm y khoa", "Đối chiếu Hemoglobin, Bạch cầu, Tiểu cầu và công thức bạch cầu.")
                }
                .Where(document => string.IsNullOrEmpty(keyword)
                    || document.Item1.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                    || document.Item2.Contains(keyword, StringComparison.CurrentCultureIgnoreCase))
                .ToArray();

                AppendSegment(sampleDocs.Length,
                    (pageOffset, countPerPage) =>
                    {
                        var page = new DataTable();
                        page.Columns.Add("Loai", typeof(string));
                        page.Columns.Add("Ma", typeof(string));
                        page.Columns.Add("TieuDe", typeof(string));
                        page.Columns.Add("MoTa", typeof(string));
                        page.Columns.Add("ChiTiet", typeof(string));
                        foreach (var document in sampleDocs.Skip(pageOffset).Take(countPerPage))
                            page.Rows.Add("Tài liệu & Phác đồ", "DOC-REF", document.Item1, document.Item2, document.Item3);
                        return page;
                    },
                    row => result.Rows.Add(row.ItemArray));
            }

            result.ExtendedProperties["TotalCount"] = totalCount;
            return result;
        }

        // ══════════════════════════════════════════════════════════════
        //  EMAIL BN: Lookup email bệnh nhân theo MaBN hoặc MaLichHen
        // ══════════════════════════════════════════════════════════════
        public static string? LayEmailBenhNhanTheoMa(string maBN)
        {
            try
            {
                var val = DatabaseHelper.ExecuteScalar(
                    "SELECT Email FROM dbo.BenhNhan WHERE MaBN = @MaBN",
                    new[] { new SqlParameter("@MaBN", maBN) });
                return val == null || val == DBNull.Value ? null : val.ToString();
            }
            catch { return null; }
        }

        public static bool CapNhatEmailBenhNhan(string maBN, string email)
        {
            EnsureBenhNhanEmailColumn();
            try
            {
                string sql = "UPDATE BenhNhan SET Email = @Email WHERE MaBN = @MaBN";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@Email", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email.Trim()),
                    new SqlParameter("@MaBN", maBN)
                }) > 0;
            }
            catch { return false; }
        }

        public static (string maBN, string tenBN, string? email, string tenBacSi, DateTime ngayHen, string lyDoKham)
            LayThongTinLichHenDeDuyet(int maLichHen)
        {
            const string sql = @"
                SELECT lh.MaBN, bn.HoTen, bn.Email, nv.HoTen AS TenBS, lh.NgayHen, lh.LyDoKham
                FROM   dbo.LichHen lh
                INNER JOIN dbo.BenhNhan bn ON bn.MaBN = lh.MaBN
                LEFT  JOIN dbo.NhanVien nv ON nv.MaNV = lh.MaNV_BacSi
                WHERE  lh.MaLichHen = @ID";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@ID", maLichHen) });
            if (dt.Rows.Count == 0) return ("", "", null, "", DateTime.Now, "");
            var r = dt.Rows[0];
            return (
                r["MaBN"].ToString() ?? "",
                r["HoTen"].ToString() ?? "",
                r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                r["TenBS"] == DBNull.Value ? "" : r["TenBS"].ToString() ?? "",
                Convert.ToDateTime(r["NgayHen"]),
                r["LyDoKham"] == DBNull.Value ? "" : r["LyDoKham"].ToString() ?? ""
            );
        }

        public static (string maBN, string tenBN, string? email, string maHD, decimal tongTien, string hinhThuc)
            LayThongTinHoaDonDaThanhToan(string maHD)
        {
            const string sql = @"
                SELECT hd.MaHD, bn.MaBN, bn.HoTen, bn.Email, hd.TongTien, hd.HinhThucTT
                FROM   dbo.HoaDon hd
                INNER JOIN dbo.PhieuKham pk ON pk.MaPK = hd.MaPK
                INNER JOIN dbo.BenhNhan  bn ON bn.MaBN = pk.MaBN
                WHERE  hd.MaHD = @MaHD";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaHD", maHD) });
            if (dt.Rows.Count == 0) return ("", "", null, maHD, 0, "");
            var r = dt.Rows[0];
            return (
                r["MaBN"].ToString() ?? "",
                r["HoTen"].ToString() ?? "",
                r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                r["MaHD"].ToString() ?? maHD,
                Convert.ToDecimal(r["TongTien"]),
                r["HinhThucTT"].ToString() ?? "Tiền mặt"
            );
        }

        // ══════════════════════════════════════════════════════════════
        //  ADMIN DASHBOARD: Nhân viên trực, giờ làm việc, trạng thái khám
        // ══════════════════════════════════════════════════════════════
        public static DataTable LayNhanVienDangTruc()
        {
            EnsureAdvancedWorkflowSchema();
            try
            {
                // Trả về bác sĩ và lễ tân có ca làm việc hôm nay (theo CaLamViecBacSi)
                return DatabaseHelper.ExecuteQuery(@"
                    SELECT  nv.MaNV,
                            nv.HoTen,
                            nv.ChucVu,
                            nv.ChuyenKhoa,
                            CONVERT(VARCHAR(5), clv.GioBatDau, 108) AS GioBatDau,
                            CONVERT(VARCHAR(5), clv.GioKetThuc, 108) AS GioKetThuc,
                            ISNULL(p.TenPhong, N'Chưa xếp') AS TenPhong,
                            ISNULL(k.TenKhoa, N'Chưa xếp') AS TenKhoa,
                            CAST(CASE WHEN CONVERT(time, GETDATE()) BETWEEN clv.GioBatDau AND clv.GioKetThuc
                                 THEN 1 ELSE 0 END AS BIT) AS DangTrongCa
                    FROM   dbo.CaLamViecBacSi clv
                    INNER JOIN dbo.NhanVien nv ON nv.MaNV = clv.MaNV AND nv.TrangThai = 1
                    LEFT JOIN dbo.PhanCongPhong pc ON pc.MaNV = nv.MaNV AND pc.VaiTro = N'BacSi'
                    LEFT JOIN dbo.PhongKham p ON p.MaPhong = pc.MaPhong
                    LEFT JOIN dbo.Khoa k ON k.MaKhoa = p.MaKhoa
                    WHERE  clv.Thu = DATEPART(dw, GETDATE())
                    ORDER  BY DangTrongCa DESC, clv.GioBatDau, nv.HoTen");
            }
            catch
            {
                return new DataTable();
            }
        }

        public static DataTable LayTrangThaiKhamHienTai()
        {
            try
            {
                // Phiếu khám đang xử lý hôm nay, nhóm theo phòng
                return DatabaseHelper.ExecuteQuery(@"
                    SELECT  ISNULL(p.TenPhong, N'Chưa phân phòng') AS TenPhong,
                            ISNULL(k.TenKhoa, N'-') AS TenKhoa,
                            ISNULL(nv.HoTen, N'-')   AS TenBacSi,
                            COUNT(CASE WHEN pk.TrangThai = N'Chờ khám'          THEN 1 END) AS ChoKham,
                            COUNT(CASE WHEN pk.TrangThai = N'Đang khám'         THEN 1 END) AS DangKham,
                            COUNT(CASE WHEN pk.TrangThai = N'Chờ kết quả CLS'   THEN 1 END) AS ChoKetQua,
                            COUNT(CASE WHEN pk.TrangThai = N'Đã khám xong'      THEN 1 END) AS DaKhamXong,
                            COUNT(*) AS TongPhieu
                    FROM   dbo.PhieuKham pk
                    LEFT JOIN dbo.NhanVien  nv ON nv.MaNV  = pk.MaNV_BacSi
                    LEFT JOIN dbo.PhongKham  p ON  p.MaPhong = pk.MaPhong
                    LEFT JOIN dbo.Khoa       k ON  k.MaKhoa  = p.MaKhoa
                    WHERE  CAST(pk.NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                      AND  pk.TrangThai <> N'Đã hủy'
                    GROUP BY p.TenPhong, k.TenKhoa, nv.HoTen
                    ORDER BY TongPhieu DESC");
            }
            catch
            {
                return new DataTable();
            }
        }

        public static DataTable LayLichHenHomNayAdmin()
        {
            try
            {
                return DatabaseHelper.ExecuteQuery(@"
                    SELECT  lh.MaLichHen,
                            bn.HoTen   AS TenBN,
                            ISNULL(nv.HoTen, N'Chưa chỉ định') AS TenBacSi,
                            CONVERT(VARCHAR(5), lh.NgayHen, 108) AS GioHen,
                            ISNULL(lh.LyDoKham, N'-') AS LyDoKham,
                            lh.TrangThai
                    FROM   dbo.LichHen lh
                    INNER JOIN dbo.BenhNhan bn ON bn.MaBN = lh.MaBN
                    LEFT  JOIN dbo.NhanVien nv ON nv.MaNV = lh.MaNV_BacSi
                    WHERE  CAST(lh.NgayHen AS DATE) = CAST(GETDATE() AS DATE)
                      AND  lh.TrangThai NOT IN (N'Đã hủy', N'Bị từ chối', N'Không đến')
                    ORDER  BY lh.NgayHen");
            }
            catch
            {
                return new DataTable();
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  THỐNG KÊ KHO THUỐC & THU NGÂN
        // ══════════════════════════════════════════════════════════════
        public static (int tongLoaiThuoc, int tongSoLuongTon, int anToanCount, int thuocSapHet, int hetHangCount, int loCanHan, decimal tongGiaTriKho) LayThongKeKhoThuoc()
        {
            EnsurePharmacySchema();
            try
            {
                string sql = @"
                    SELECT 
                        COUNT(*) AS TongLoai,
                        ISNULL(SUM(SoLuongTon), 0) AS TongTon,
                        ISNULL(SUM(CASE WHEN SoLuongTon > 30 THEN 1 ELSE 0 END), 0) AS AnToan,
                        ISNULL(SUM(CASE WHEN SoLuongTon > 0 AND SoLuongTon <= 30 THEN 1 ELSE 0 END), 0) AS SapHet,
                        ISNULL(SUM(CASE WHEN SoLuongTon = 0 THEN 1 ELSE 0 END), 0) AS HetHang,
                        ISNULL(SUM(CAST(SoLuongTon AS DECIMAL(18,2)) * CAST(DonGiaBan AS DECIMAL(18,2))), 0) AS TongGiaTri
                    FROM Thuoc";
                var dt = DatabaseHelper.ExecuteQuery(sql);

                int tongLoai = 0, tongTon = 0, anToan = 0, sapHet = 0, hetHang = 0;
                decimal giaTri = 0;
                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    tongLoai = r["TongLoai"] != DBNull.Value ? Convert.ToInt32(r["TongLoai"]) : 0;
                    tongTon  = r["TongTon"] != DBNull.Value ? Convert.ToInt32(r["TongTon"]) : 0;
                    anToan   = r["AnToan"] != DBNull.Value ? Convert.ToInt32(r["AnToan"]) : 0;
                    sapHet   = r["SapHet"] != DBNull.Value ? Convert.ToInt32(r["SapHet"]) : 0;
                    hetHang  = r["HetHang"] != DBNull.Value ? Convert.ToInt32(r["HetHang"]) : 0;
                    giaTri   = r["TongGiaTri"] != DBNull.Value ? Convert.ToDecimal(r["TongGiaTri"]) : 0;
                }

                string sqlLo = @"
                    SELECT COUNT(*) 
                    FROM LoThuoc 
                    WHERE SoLuongCon > 0 AND NgayHetHan <= DATEADD(DAY, 90, CAST(GETDATE() AS DATE))";
                int loCanHan = 0;
                try
                {
                    var valLo = DatabaseHelper.ExecuteScalar(sqlLo);
                    if (valLo != null && valLo != DBNull.Value) loCanHan = Convert.ToInt32(valLo);
                }
                catch { }

                return (tongLoai, tongTon, anToan, sapHet, hetHang, loCanHan, giaTri);
            }
            catch
            {
                return (0, 0, 0, 0, 0, 0, 0);
            }
        }

        public static (decimal doanhThuHomNay, int daThuCount, int chuaThuCount, decimal tienChuaThu, int daHuyCount, int tienMatCount, int chuyenKhoanCount) LayThongKeThuNganHomNay()
        {
            try
            {
                string sql = @"
                    SELECT 
                        ISNULL(SUM(CASE WHEN TrangThai = N'Đã thanh toán' THEN TongTien ELSE 0 END), 0) AS DoanhThu,
                        ISNULL(SUM(CASE WHEN TrangThai = N'Đã thanh toán' THEN 1 ELSE 0 END), 0) AS DaThuCount,
                        ISNULL(SUM(CASE WHEN TrangThai = N'Chưa thanh toán' THEN 1 ELSE 0 END), 0) AS ChuaThuCount,
                        ISNULL(SUM(CASE WHEN TrangThai = N'Chưa thanh toán' THEN TongTien ELSE 0 END), 0) AS TienChuaThu,
                        ISNULL(SUM(CASE WHEN TrangThai = N'Đã hủy' THEN 1 ELSE 0 END), 0) AS DaHuyCount,
                        ISNULL(SUM(CASE WHEN TrangThai = N'Đã thanh toán' AND HinhThucTT = N'Tiền mặt' THEN 1 ELSE 0 END), 0) AS TienMat,
                        ISNULL(SUM(CASE WHEN TrangThai = N'Đã thanh toán' AND HinhThucTT <> N'Tiền mặt' THEN 1 ELSE 0 END), 0) AS ChuyenKhoan
                    FROM HoaDon
                    WHERE CAST(COALESCE(NgayThanhToan, NgayLap) AS DATE) = CAST(GETDATE() AS DATE)";
                var dt = DatabaseHelper.ExecuteQuery(sql);
                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    return (
                        r["DoanhThu"] != DBNull.Value ? Convert.ToDecimal(r["DoanhThu"]) : 0,
                        r["DaThuCount"] != DBNull.Value ? Convert.ToInt32(r["DaThuCount"]) : 0,
                        r["ChuaThuCount"] != DBNull.Value ? Convert.ToInt32(r["ChuaThuCount"]) : 0,
                        r["TienChuaThu"] != DBNull.Value ? Convert.ToDecimal(r["TienChuaThu"]) : 0,
                        r["DaHuyCount"] != DBNull.Value ? Convert.ToInt32(r["DaHuyCount"]) : 0,
                        r["TienMat"] != DBNull.Value ? Convert.ToInt32(r["TienMat"]) : 0,
                        r["ChuyenKhoan"] != DBNull.Value ? Convert.ToInt32(r["ChuyenKhoan"]) : 0
                    );
                }
            }
            catch { }
            return (0, 0, 0, 0, 0, 0, 0);
        }
    }
}
