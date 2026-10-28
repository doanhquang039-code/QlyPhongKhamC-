using System;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using QlyPhongKham.Models;

namespace QlyPhongKham.DAL
{
    public static class ClinicDAL
    {
        private static readonly object ClinicalSchemaLock = new();
        private static bool _clinicalRecordColumnsReady;
        private static readonly object ClinicRoomSchemaLock = new();
        private static bool _clinicRoomSchemaReady;

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
                        ALTER TABLE dbo.PhieuKham ADD MaPhong VARCHAR(10) NULL;

                    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PhieuKham_Phong')
                        ALTER TABLE dbo.PhieuKham ADD CONSTRAINT FK_PhieuKham_Phong
                            FOREIGN KEY (MaPhong) REFERENCES dbo.PhongKham(MaPhong);

                    UPDATE tk
                    SET Quyen = CASE WHEN nv.ChucVu = N'ThuNgan' THEN N'LeTan' ELSE nv.ChucVu END
                    FROM dbo.TaiKhoan tk
                    INNER JOIN dbo.NhanVien nv ON nv.MaNV = tk.MaNV
                    WHERE tk.Quyen = N'ThuNgan';
                    UPDATE dbo.NhanVien SET ChucVu = N'LeTan' WHERE ChucVu = N'ThuNgan';

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
                WHERE tk.TenDangNhap = @User AND tk.TrangThai = 1";

            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@User", username) });
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            string savedPass = row["MatKhau"].ToString() ?? "";
            
            // Cho phép mật khẩu plain-text hoặc so khớp trực tiếp
            if (savedPass != password) return null;

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
                SELECT tk.TenDangNhap, tk.MaBN, bn.HoTen, tk.MatKhau
                FROM TaiKhoanBenhNhan tk
                INNER JOIN BenhNhan bn ON bn.MaBN = tk.MaBN
                WHERE tk.TenDangNhap = @User AND tk.TrangThai = 1";
            var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@User", username) });
            if (dt.Rows.Count == 0 || dt.Rows[0]["MatKhau"].ToString() != password)
                return null;

            return new TaiKhoanBenhNhan
            {
                TenDangNhap = dt.Rows[0]["TenDangNhap"].ToString() ?? "",
                MaBN = dt.Rows[0]["MaBN"].ToString() ?? "",
                HoTen = dt.Rows[0]["HoTen"].ToString() ?? ""
            };
        }

        public static void DangKyTaiKhoanBenhNhan(
            string username,
            string password,
            string fullName,
            DateTime? dateOfBirth,
            string gender,
            string? phone,
            string? address)
        {
            username = username.Trim();
            fullName = fullName.Trim();
            phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
            address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

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
                    INSERT INTO BenhNhan (MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh)
                    VALUES (@MaBN, @HoTen, @NgaySinh, @GioiTinh, @SoDienThoai, @DiaChi, NULL)",
                    conn, trans))
                {
                    patientCommand.Parameters.AddWithValue("@MaBN", maBN);
                    patientCommand.Parameters.AddWithValue("@HoTen", fullName);
                    patientCommand.Parameters.AddWithValue("@NgaySinh", (object?)dateOfBirth?.Date ?? DBNull.Value);
                    patientCommand.Parameters.AddWithValue("@GioiTinh", gender);
                    patientCommand.Parameters.AddWithValue("@SoDienThoai", (object?)phone ?? DBNull.Value);
                    patientCommand.Parameters.AddWithValue("@DiaChi", (object?)address ?? DBNull.Value);
                    patientCommand.ExecuteNonQuery();
                }

                using (var accountCommand = new SqlCommand(@"
                    INSERT INTO TaiKhoanBenhNhan (TenDangNhap, MatKhau, MaBN, TrangThai)
                    VALUES (@Username, @Password, @MaBN, 1)", conn, trans))
                {
                    accountCommand.Parameters.AddWithValue("@Username", username);
                    accountCommand.Parameters.AddWithValue("@Password", password);
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
        public static DataTable LayDanhSachBenhNhan(string keyword = "")
        {
            string sql = @"
                SELECT MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh
                FROM BenhNhan
                WHERE @Kw = '' OR HoTen LIKE '%' + @Kw + '%' OR SoDienThoai LIKE '%' + @Kw + '%' OR MaBN LIKE '%' + @Kw + '%'
                ORDER BY NgayTao DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Kw", keyword) });
        }

        public static DataRow? LayBenhNhanTheoMa(string maBN)
        {
            const string sql = @"
                SELECT MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh
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
            if (isNew)
            {
                string sql = @"
                    INSERT INTO BenhNhan (MaBN, HoTen, NgaySinh, GioiTinh, SoDienThoai, DiaChi, TienSuBenh)
                    VALUES (@MaBN, @HoTen, @NgaySinh, @GioiTinh, @SoDienThoai, @DiaChi, @TienSuBenh)";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@MaBN", bn.MaBN),
                    new SqlParameter("@HoTen", bn.HoTen),
                    new SqlParameter("@NgaySinh", (object?)bn.NgaySinh ?? DBNull.Value),
                    new SqlParameter("@GioiTinh", bn.GioiTinh),
                    new SqlParameter("@SoDienThoai", (object?)bn.SoDienThoai ?? DBNull.Value),
                    new SqlParameter("@DiaChi", (object?)bn.DiaChi ?? DBNull.Value),
                    new SqlParameter("@TienSuBenh", (object?)bn.TienSuBenh ?? DBNull.Value)
                }) > 0;
            }
            else
            {
                string sql = @"
                    UPDATE BenhNhan
                    SET HoTen = @HoTen, NgaySinh = @NgaySinh, GioiTinh = @GioiTinh,
                        SoDienThoai = @SoDienThoai, DiaChi = @DiaChi, TienSuBenh = @TienSuBenh
                    WHERE MaBN = @MaBN";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@MaBN", bn.MaBN),
                    new SqlParameter("@HoTen", bn.HoTen),
                    new SqlParameter("@NgaySinh", (object?)bn.NgaySinh ?? DBNull.Value),
                    new SqlParameter("@GioiTinh", bn.GioiTinh),
                    new SqlParameter("@SoDienThoai", (object?)bn.SoDienThoai ?? DBNull.Value),
                    new SqlParameter("@DiaChi", (object?)bn.DiaChi ?? DBNull.Value),
                    new SqlParameter("@TienSuBenh", (object?)bn.TienSuBenh ?? DBNull.Value)
                }) > 0;
            }
        }

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

        public static DataTable LayHangChoKhamTrongNgay(string? maBacSi = null, string? maPhong = null, string? maLeTan = null)
        {
            string sql = @"
                SELECT pk.MaPK, pk.STT, pk.NgayKham, pk.TrangThai,
                       bn.MaBN, bn.HoTen AS TenBN, bn.GioiTinh, DATEDIFF(YEAR, bn.NgaySinh, GETDATE()) AS Tuoi,
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
                ORDER BY pk.STT ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaBS", (object?)maBacSi ?? DBNull.Value),
                new SqlParameter("@MaPhong", (object?)maPhong ?? DBNull.Value),
                new SqlParameter("@MaLeTan", (object?)maLeTan ?? DBNull.Value)
            });
        }

        public static bool TaoPhieuKham(PhieuKham pk, string maLeTan)
        {
            string sql = @"
                INSERT INTO PhieuKham (MaPK, STT, MaBN, MaNV_BacSi, MaPhong, NgayKham, TrieuChung, TrangThai)
                SELECT @MaPK, @STT, @MaBN, @MaBS, @MaPhong, GETDATE(), @TrieuChung, N'Chờ khám'
                WHERE EXISTS (
                    SELECT 1 FROM PhanCongPhong pc
                    INNER JOIN NhanVien nv ON nv.MaNV = pc.MaNV
                    WHERE pc.MaNV = @MaBS AND pc.MaPhong = @MaPhong AND pc.VaiTro = N'BacSi'
                      AND nv.ChucVu = N'BacSi' AND nv.TrangThai = 1
                )
                AND EXISTS (
                    SELECT 1 FROM PhanCongPhong pc
                    INNER JOIN NhanVien nv ON nv.MaNV = pc.MaNV
                    WHERE pc.MaNV = @MaLeTan AND pc.MaPhong = @MaPhong AND pc.VaiTro = N'LeTan'
                      AND nv.ChucVu = N'LeTan' AND nv.TrangThai = 1
                )";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", pk.MaPK),
                new SqlParameter("@STT", pk.STT),
                new SqlParameter("@MaBN", pk.MaBN),
                new SqlParameter("@MaBS", pk.MaNV_BacSi),
                new SqlParameter("@MaPhong", (object?)pk.MaPhong ?? DBNull.Value),
                new SqlParameter("@MaLeTan", maLeTan),
                new SqlParameter("@TrieuChung", (object?)pk.TrieuChung ?? DBNull.Value)
            }) > 0;
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

        public static DataTable LayDanhSachLichHen(string keyword = "", string? trangThai = null)
        {
            const string sql = @"
                SELECT lh.MaLichHen, lh.MaBN, bn.HoTen AS TenBN, lh.MaNV_BacSi,
                       nv.HoTen AS TenBacSi, lh.NgayHen, lh.LyDoKham, lh.TrangThai
                FROM LichHen lh
                INNER JOIN BenhNhan bn ON bn.MaBN = lh.MaBN
                LEFT JOIN NhanVien nv ON nv.MaNV = lh.MaNV_BacSi
                WHERE (@Kw = '' OR CONVERT(VARCHAR(12), lh.MaLichHen) LIKE '%' + @Kw + '%'
                    OR lh.MaBN LIKE '%' + @Kw + '%' OR bn.HoTen LIKE '%' + @Kw + '%'
                    OR nv.HoTen LIKE '%' + @Kw + '%')
                  AND (@TrangThai IS NULL OR @TrangThai = N'Tất cả trạng thái' OR lh.TrangThai = @TrangThai)
                ORDER BY lh.NgayHen DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@Kw", keyword.Trim()),
                new SqlParameter("@TrangThai", (object?)trangThai ?? DBNull.Value)
            });
        }

        public static bool DuyetLichHen(int maLichHen, string maLeTan)
        {
            const string sql = @"
                UPDATE lh
                SET TrangThai = N'Đã xác nhận'
                FROM LichHen lh
                WHERE lh.MaLichHen = @MaLichHen
                  AND lh.TrangThai = N'Chờ xác nhận'
                  AND EXISTS (
                      SELECT 1
                      FROM NhanVien nv
                      INNER JOIN PhanCongPhong pc ON pc.MaNV = nv.MaNV AND pc.VaiTro = N'LeTan'
                      INNER JOIN PhanCongPhong doctorPc ON doctorPc.MaPhong = pc.MaPhong
                          AND doctorPc.VaiTro = N'BacSi'
                      WHERE nv.MaNV = @MaLeTan
                        AND nv.ChucVu = N'LeTan'
                        AND nv.TrangThai = 1
                        AND doctorPc.MaNV = lh.MaNV_BacSi
                  )";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaLichHen", maLichHen),
                new SqlParameter("@MaLeTan", maLeTan)
            }) > 0;
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
            if (ngayHen <= DateTime.Now)
                throw new ArgumentException("Vui lòng chọn ngày giờ trong tương lai.");
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new ArgumentException("Vui lòng nhập lý do khám.");

            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
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

        public static bool LuuLichHen(int? maLichHen, string maBN, string? maBS, DateTime ngayHen, string lyDo, string trangThai)
        {
            const string sql = @"
                DECLARE @AffectedRows INT = 0;
                IF NOT EXISTS (SELECT 1 FROM BenhNhan WHERE MaBN = @MaBN)
                   OR (@MaBS IS NOT NULL AND NOT EXISTS (
                       SELECT 1 FROM NhanVien
                       WHERE MaNV = @MaBS AND ChucVu = N'BacSi' AND TrangThai = 1
                   ))
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
                      AND TrangThai <> N'Đã hủy';
                    SET @AffectedRows = @@ROWCOUNT;
                END;
                SELECT @AffectedRows;";
            return Convert.ToInt32(DatabaseHelper.ExecuteScalar(sql, new[]
            {
                new SqlParameter("@MaLichHen", (object?)maLichHen ?? DBNull.Value),
                new SqlParameter("@MaBN", maBN),
                new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value),
                new SqlParameter("@NgayHen", ngayHen),
                new SqlParameter("@LyDoKham", (object?)lyDo ?? DBNull.Value),
                new SqlParameter("@TrangThai", trangThai)
            })) > 0;
        }

        public static bool HuyLichHen(int maLichHen)
        {
            const string sql = @"
                UPDATE LichHen
                SET TrangThai = N'Đã hủy'
                WHERE MaLichHen = @MaLichHen
                  AND TrangThai IN (N'Chờ xác nhận', N'Đã xác nhận')";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaLichHen", maLichHen)
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
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public static bool HuyPhanCongPhong(string maNV)
        {
            return DatabaseHelper.ExecuteNonQuery(
                "DELETE FROM PhanCongPhong WHERE MaNV = @MaNV",
                new[] { new SqlParameter("@MaNV", maNV) }) > 0;
        }

        public static DataTable LayDanhSachBenhNhanBacSi(string maBS, string keyword = "")
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
                ORDER BY MAX(pk.NgayKham) DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaBS", maBS),
                new SqlParameter("@Kw", keyword.Trim())
            });
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
            EnsureClinicalRecordColumns();
            string sql = @"
                UPDATE PhieuKham
                SET TrieuChung = @TrieuChung, KhamLamSang = @KhamLamSang, SinhHieu = @SinhHieu,
                    ChanDoan = @ChanDoan, LoiDan = @LoiDan, TrangThai = @TrangThai
                WHERE MaPK = @MaPK AND MaNV_BacSi = @MaBS";
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
            const string sql = "UPDATE PhieuKham SET TrangThai = @TrangThai WHERE MaPK = @MaPK AND MaNV_BacSi = @MaBS";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
                new SqlParameter("@MaBS", maBS),
                new SqlParameter("@TrangThai", trangThai)
            }) > 0;
        }

        public static bool CapNhatTienSuBenhNhan(string maBN, string tienSu)
        {
            const string sql = "UPDATE BenhNhan SET TienSuBenh = @TienSu WHERE MaBN = @MaBN";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaBN", maBN),
                new SqlParameter("@TienSu", (object?)tienSu ?? DBNull.Value)
            }) > 0;
        }

        public static DataTable LayDanhSachDichVu()
        {
            return DatabaseHelper.ExecuteQuery("SELECT MaDV, TenDV, DonGia, MoTa FROM DichVu");
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

        public static bool ChiDinhDichVu(string maPK, string maDV, decimal donGia)
        {
            string sql = @"
                INSERT INTO ChiTietChiDinhDV (MaPK, MaDV, DonGiaThoiDiem, TrangThai)
                VALUES (@MaPK, @MaDV, @DonGia, N'Chờ thực hiện')";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
                new SqlParameter("@MaDV", maDV),
                new SqlParameter("@DonGia", donGia)
            }) > 0;
        }

        public static bool CapNhatKetQuaDV(int maChiDinh, string ketQua)
        {
            string sql = @"
                UPDATE ChiTietChiDinhDV
                SET KetQua = @KetQua, TrangThai = N'Đã có kết quả'
                WHERE MaChiDinh = @ID";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@ID", maChiDinh),
                new SqlParameter("@KetQua", ketQua)
            }) > 0;
        }

        // 5. THUỐC & KHO DƯỢC
        public static DataTable LayDanhSachThuoc(string keyword = "")
        {
            string sql = @"
                SELECT MaThuoc, TenThuoc, DonViTinh, DonGiaBan, SoLuongTon, CachDungMacDinh
                FROM Thuoc
                WHERE @Kw = '' OR TenThuoc LIKE '%' + @Kw + '%' OR MaThuoc LIKE '%' + @Kw + '%'
                ORDER BY TenThuoc";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Kw", keyword) });
        }

        public static bool LuuThuoc(Thuoc t, bool isNew)
        {
            if (isNew)
            {
                string sql = @"
                    INSERT INTO Thuoc (MaThuoc, TenThuoc, DonViTinh, DonGiaBan, SoLuongTon, CachDungMacDinh)
                    VALUES (@MaThuoc, @TenThuoc, @DonViTinh, @DonGiaBan, @SoLuongTon, @CachDungMacDinh)";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@MaThuoc", t.MaThuoc),
                    new SqlParameter("@TenThuoc", t.TenThuoc),
                    new SqlParameter("@DonViTinh", t.DonViTinh),
                    new SqlParameter("@DonGiaBan", t.DonGiaBan),
                    new SqlParameter("@SoLuongTon", t.SoLuongTon),
                    new SqlParameter("@CachDungMacDinh", (object?)t.CachDungMacDinh ?? DBNull.Value)
                }) > 0;
            }
            else
            {
                string sql = @"
                    UPDATE Thuoc
                    SET TenThuoc = @TenThuoc, DonViTinh = @DonViTinh, DonGiaBan = @DonGiaBan,
                        SoLuongTon = @SoLuongTon, CachDungMacDinh = @CachDungMacDinh
                    WHERE MaThuoc = @MaThuoc";
                return DatabaseHelper.ExecuteNonQuery(sql, new[]
                {
                    new SqlParameter("@MaThuoc", t.MaThuoc),
                    new SqlParameter("@TenThuoc", t.TenThuoc),
                    new SqlParameter("@DonViTinh", t.DonViTinh),
                    new SqlParameter("@DonGiaBan", t.DonGiaBan),
                    new SqlParameter("@SoLuongTon", t.SoLuongTon),
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
            const string sql = "UPDATE DonThuoc SET GhiChu = @GhiChu WHERE MaPK = @MaPK";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
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
            errorMessage = string.Empty;
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
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

                // Thêm chi tiết và trừ kho an toàn
                foreach (var item in listThuoc)
                {
                    // 1. Trừ kho an toàn (Chặn âm kho)
                    string sqlSubStock = @"
                        UPDATE Thuoc
                        SET SoLuongTon = SoLuongTon - @SL
                        WHERE MaThuoc = @MaThuoc AND SoLuongTon >= @SL";
                    using var cmdSub = new SqlCommand(sqlSubStock, conn, trans);
                    cmdSub.Parameters.AddWithValue("@SL", item.SoLuong);
                    cmdSub.Parameters.AddWithValue("@MaThuoc", item.MaThuoc);
                    int updated = cmdSub.ExecuteNonQuery();

                    if (updated == 0)
                    {
                        throw new Exception($"Thuốc '{item.TenThuoc}' không đủ tồn kho để kê đơn (Yêu cầu: {item.SoLuong})!");
                    }

                    // 2. Insert chi tiết đơn
                    string sqlInsertCT = @"
                        INSERT INTO ChiTietDonThuoc (MaDonThuoc, MaThuoc, SoLuong, DonGiaThoiDiem, CachDung)
                        VALUES (@MaDT, @MaThuoc, @SoLuong, @DonGia, @CachDung)";
                    using var cmdCT = new SqlCommand(sqlInsertCT, conn, trans);
                    cmdCT.Parameters.AddWithValue("@MaDT", maDonThuoc);
                    cmdCT.Parameters.AddWithValue("@MaThuoc", item.MaThuoc);
                    cmdCT.Parameters.AddWithValue("@SoLuong", item.SoLuong);
                    cmdCT.Parameters.AddWithValue("@DonGia", item.DonGiaThoiDiem);
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

        public static DataTable LayDanhSachHoaDon()
        {
            string sql = @"
                SELECT hd.MaHD, hd.MaPK, bn.HoTen AS TenBN, nv.HoTen AS ThuNgan,
                       hd.NgayLap, hd.TienDichVu, hd.TienThuoc, hd.GiamGia, hd.TongTien,
                       hd.HinhThucTT, hd.TrangThai
                FROM HoaDon hd
                INNER JOIN PhieuKham pk ON hd.MaPK = pk.MaPK
                INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                INNER JOIN NhanVien nv ON hd.MaNV_ThuNgan = nv.MaNV
                ORDER BY hd.NgayLap DESC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static DataTable LayChiTietHoaDon(string maHD)
        {
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

        public static bool TaoHoaDon(HoaDon hd)
        {
            string sql = @"
                INSERT INTO HoaDon (MaHD, MaPK, MaNV_ThuNgan, NgayLap, TienDichVu, TienThuoc, GiamGia, TongTien, HinhThucTT, TrangThai)
                VALUES (@MaHD, @MaPK, @MaNV, GETDATE(), @TienDV, @TienThuoc, @GiamGia, @TongTien, @HinhThucTT, @TrangThai)";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaHD", hd.MaHD),
                new SqlParameter("@MaPK", hd.MaPK),
                new SqlParameter("@MaNV", hd.MaNV_ThuNgan),
                new SqlParameter("@TienDV", hd.TienDichVu),
                new SqlParameter("@TienThuoc", hd.TienThuoc),
                new SqlParameter("@GiamGia", hd.GiamGia),
                new SqlParameter("@TongTien", hd.TongTien),
                new SqlParameter("@HinhThucTT", hd.HinhThucTT),
                new SqlParameter("@TrangThai", hd.TrangThai)
            }) > 0;
        }

        public static bool ThanhToanHoaDon(string maHD, string hinhThuc)
        {
            const string sql = @"
                UPDATE HoaDon
                SET TrangThai = N'Đã thanh toán', HinhThucTT = @HinhThuc
                WHERE MaHD = @MaHD AND TrangThai = N'Chưa thanh toán'";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaHD", maHD),
                new SqlParameter("@HinhThuc", hinhThuc)
            }) > 0;
        }

        public static bool HuyHoaDonChuaThanhToan(string maHD)
        {
            const string sql = @"
                UPDATE HoaDon
                SET TrangThai = N'Đã hủy'
                WHERE MaHD = @MaHD AND TrangThai = N'Chưa thanh toán'";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaHD", maHD)
            }) > 0;
        }

        // 8. NHÂN VIÊN (QUẢN TRỊ ADMIN)
        public static DataTable LayDanhSachNhanVien(string keyword = "", string role = "")
        {
            string sql = @"
                SELECT nv.MaNV, nv.HoTen, nv.ChucVu, nv.ChuyenKhoa, nv.SoDienThoai, nv.Email,
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
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@Kw", keyword.Trim()),
                new SqlParameter("@Role", role)
            });
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
            if (nv.ChucVu is not ("Admin" or "BacSi" or "LeTan"))
                throw new ArgumentOutOfRangeException(nameof(nv), "Chức vụ không hợp lệ. Thu ngân dùng chung vai trò Lễ tân.");

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
                string sql = @"INSERT INTO NhanVien (MaNV, HoTen, ChucVu, ChuyenKhoa, SoDienThoai, Email, TrangThai)
                    VALUES (@MaNV, @HoTen, @ChucVu, @ChuyenKhoa, @SDT, @Email, 1)";
                return DatabaseHelper.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@MaNV", nv.MaNV),
                    new SqlParameter("@HoTen", nv.HoTen),
                    new SqlParameter("@ChucVu", nv.ChucVu),
                    new SqlParameter("@ChuyenKhoa", (object?)nv.ChuyenKhoa ?? DBNull.Value),
                    new SqlParameter("@SDT", (object?)nv.SoDienThoai ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)nv.Email ?? DBNull.Value)
                }) > 0;
            }
            else
            {
                string sql = @"UPDATE NhanVien SET HoTen=@HoTen, ChucVu=@ChucVu, ChuyenKhoa=@ChuyenKhoa,
                    SoDienThoai=@SDT, Email=@Email WHERE MaNV=@MaNV";
                bool updated = DatabaseHelper.ExecuteNonQuery(sql, new[] {
                    new SqlParameter("@MaNV", nv.MaNV),
                    new SqlParameter("@HoTen", nv.HoTen),
                    new SqlParameter("@ChucVu", nv.ChucVu),
                    new SqlParameter("@ChuyenKhoa", (object?)nv.ChuyenKhoa ?? DBNull.Value),
                    new SqlParameter("@SDT", (object?)nv.SoDienThoai ?? DBNull.Value),
                    new SqlParameter("@Email", (object?)nv.Email ?? DBNull.Value)
                }) > 0;
                if (updated)
                {
                    DatabaseHelper.ExecuteNonQuery(
                        "UPDATE TaiKhoan SET Quyen = @VaiTro WHERE MaNV = @MaNV",
                        new[]
                        {
                            new SqlParameter("@MaNV", nv.MaNV),
                            new SqlParameter("@VaiTro", nv.ChucVu)
                        });

                    if (nv.ChucVu is not ("BacSi" or "LeTan"))
                    {
                        DatabaseHelper.ExecuteNonQuery(
                            "DELETE FROM PhanCongPhong WHERE MaNV = @MaNV",
                            new[] { new SqlParameter("@MaNV", nv.MaNV) });
                    }
                    else
                    {
                        DatabaseHelper.ExecuteNonQuery(
                            "UPDATE PhanCongPhong SET VaiTro = @VaiTro WHERE MaNV = @MaNV",
                            new[]
                            {
                                new SqlParameter("@MaNV", nv.MaNV),
                                new SqlParameter("@VaiTro", nv.ChucVu)
                            });
                    }
                }
                return updated;
            }
        }

        public static bool XoaNhanVien(string maNV)
        {
            // Vô hiệu hóa thay vì xóa thật (soft delete)
            return DatabaseHelper.ExecuteNonQuery(
                "UPDATE NhanVien SET TrangThai = 0 WHERE MaNV = @MaNV",
                new[] { new SqlParameter("@MaNV", maNV) }) > 0;
        }

        public static bool LuuTaiKhoan(string maNV, string tenDangNhap, string matKhau, string quyen, bool isNew)
        {
            if (quyen is not ("Admin" or "BacSi" or "LeTan"))
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
            using var trans = conn.BeginTransaction();
            try
            {
                string sql = isNew
                    ? @"INSERT INTO TaiKhoan (TenDangNhap, MatKhau, MaNV, Quyen, TrangThai)
                        VALUES (@TDN, @MK, @MaNV, @Quyen, 1)"
                    : @"UPDATE TaiKhoan SET MatKhau = @MK, Quyen = @Quyen WHERE MaNV = @MaNV";
                using var accountCommand = new SqlCommand(sql, conn, trans);
                accountCommand.Parameters.AddWithValue("@MK", matKhau);
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
        public static DataTable LayDanhSachChoThanhToan()
        {
            string sql = @"
                SELECT pk.MaPK, pk.STT, pk.NgayKham, bn.HoTen AS TenBN, bn.SoDienThoai,
                       nv.HoTen AS TenBacSi, pk.ChanDoan, pk.TrangThai
                FROM PhieuKham pk
                INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                WHERE pk.TrangThai = N'Đã khám xong'
                  AND NOT EXISTS (SELECT 1 FROM HoaDon hd WHERE hd.MaPK = pk.MaPK)
                ORDER BY pk.NgayKham ASC";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        // 10. XÓA THUỐC KHỎI ĐƠN (và hoàn trả tồn kho)
        public static bool XoaThuocKhoiDon(int maCTDT, string maThuoc, int soLuong, out string error)
        {
            error = string.Empty;
            using var conn = DatabaseHelper.GetConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var cmd1 = new SqlCommand("DELETE FROM ChiTietDonThuoc WHERE MaCTDT=@ID", conn, trans);
                cmd1.Parameters.AddWithValue("@ID", maCTDT);
                cmd1.ExecuteNonQuery();

                var cmd2 = new SqlCommand("UPDATE Thuoc SET SoLuongTon=SoLuongTon+@SL WHERE MaThuoc=@MT", conn, trans);
                cmd2.Parameters.AddWithValue("@SL", soLuong);
                cmd2.Parameters.AddWithValue("@MT", maThuoc);
                cmd2.ExecuteNonQuery();

                trans.Commit();
                return true;
            }
            catch (Exception ex) { trans.Rollback(); error = ex.Message; return false; }
        }

        // 11. NHẬP THÊM TỒN KHO
        public static bool NhapKho(string maThuoc, int soLuongNhap)
        {
            return DatabaseHelper.ExecuteNonQuery(
                "UPDATE Thuoc SET SoLuongTon=SoLuongTon+@SL WHERE MaThuoc=@MT",
                new[] { new SqlParameter("@SL", soLuongNhap), new SqlParameter("@MT", maThuoc) }) > 0;
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
                  AND CAST(NgayLap AS DATE) = CAST(GETDATE() AS DATE)";
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
                SELECT CAST(NgayLap AS DATE) AS Ngay, COUNT(*) AS SoHoaDon,
                       SUM(TongTien) AS DoanhThu
                FROM HoaDon WHERE TrangThai = N'Đã thanh toán'
                  AND NgayLap >= DATEADD(DAY, -6, CAST(GETDATE() AS DATE))
                GROUP BY CAST(NgayLap AS DATE)
                ORDER BY Ngay";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        public static DataTable LayThuocSapHet(int nguong = 30)
        {
            string sql = @"SELECT MaThuoc, TenThuoc, DonViTinh, SoLuongTon, DonGiaBan
                FROM Thuoc WHERE SoLuongTon <= @N ORDER BY SoLuongTon ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@N", nguong) });
        }

        public static DataTable LayTopChanDoan(int top = 10)
        {
            string sql = @"
                SELECT TOP (@Top) ChanDoan, COUNT(*) AS SoLuot
                FROM PhieuKham WHERE ChanDoan IS NOT NULL AND ChanDoan <> ''
                GROUP BY ChanDoan ORDER BY SoLuot DESC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@Top", top) });
        }

        public static DataTable LayLichSuKhamBenhNhan(string maBN, string? maBS = null)
        {
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
        public static (int tongPhanCong, int daHoanTat, int dangKham, int dangCho, string luotTiepTheo) LayThongKeBacSi(string? maBS)
        {
            try
            {
                string sql = @"
                    SELECT 
                        COUNT(*) AS Tong,
                        SUM(CASE WHEN TrangThai = N'Đã khám xong' THEN 1 ELSE 0 END) AS DaHoanTat,
                        SUM(CASE WHEN TrangThai = N'Đang khám' THEN 1 ELSE 0 END) AS DangKham,
                        SUM(CASE WHEN TrangThai IN (N'Chờ khám', N'Chờ kết quả CLS') THEN 1 ELSE 0 END) AS DangCho
                    FROM PhieuKham
                    WHERE CAST(NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                      AND (@MaBS IS NULL OR MaNV_BacSi = @MaBS)";
                var dt = DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value) });
                int tong = 0, daHoanTat = 0, dangKham = 0, dangCho = 0;
                if (dt.Rows.Count > 0)
                {
                    var r = dt.Rows[0];
                    tong = r["Tong"] != DBNull.Value ? Convert.ToInt32(r["Tong"]) : 0;
                    daHoanTat = r["DaHoanTat"] != DBNull.Value ? Convert.ToInt32(r["DaHoanTat"]) : 0;
                    dangKham = r["DangKham"] != DBNull.Value ? Convert.ToInt32(r["DangKham"]) : 0;
                    dangCho = r["DangCho"] != DBNull.Value ? Convert.ToInt32(r["DangCho"]) : 0;
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
                return (tong, daHoanTat, dangKham, dangCho, nextStr);
            }
            catch
            {
                return (0, 0, 0, 0, "Chưa có");
            }
        }

        public static DataTable LayDanhSachKhamBacSi(string? maBS, string keyword = "", string? trangThai = null)
        {
            string sql = @"
                SELECT pk.MaPK, pk.STT, FORMAT(pk.NgayKham, 'HH:mm') AS GioKham,
                       pk.MaBN, bn.HoTen AS TenBN, bn.GioiTinh,
                       DATEDIFF(YEAR, bn.NgaySinh, GETDATE()) AS Tuoi,
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
                  AND (@Kw = '' OR bn.HoTen LIKE '%' + @Kw + '%' OR bn.MaBN LIKE '%' + @Kw + '%' OR pk.MaPK LIKE '%' + @Kw + '%')
                  AND (@TrangThai IS NULL OR @TrangThai = '' OR @TrangThai = N'Tất cả trạng thái' OR pk.TrangThai = @TrangThai)
                ORDER BY pk.STT ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[]
            {
                new SqlParameter("@MaBS", (object?)maBS ?? DBNull.Value),
                new SqlParameter("@Kw", keyword),
                new SqlParameter("@TrangThai", (object?)trangThai ?? DBNull.Value)
            });
        }

        public static DataRow? LayChiTietPhieuKhamDayDu(string maPK, string maBS)
        {
            EnsureClinicalRecordColumns();
            string sql = @"
                SELECT pk.MaPK, pk.STT, pk.NgayKham, pk.TrieuChung, pk.KhamLamSang, pk.SinhHieu, pk.ChanDoan, pk.LoiDan, pk.TrangThai,
                       bn.MaBN, bn.HoTen AS TenBN, bn.GioiTinh, bn.NgaySinh, bn.SoDienThoai, bn.DiaChi, bn.TienSuBenh,
                       DATEDIFF(YEAR, bn.NgaySinh, GETDATE()) AS Tuoi,
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
                    WHERE (@Kw = '' OR bn.HoTen LIKE '%' + @Kw + '%' OR pk.ChanDoan LIKE '%' + @Kw + '%' OR pk.MaPK LIKE '%' + @Kw + '%')
                    ORDER BY pk.NgayKham DESC";
                var dtBA = DatabaseHelper.ExecuteQuery(sqlBA, new[] { new SqlParameter("@Kw", keyword) });
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

    }
}
