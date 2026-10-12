using System;
using System.Data;
using Microsoft.Data.SqlClient;
using QlyPhongKham.Models;

namespace QlyPhongKham.DAL
{
    public static class ClinicDAL
    {
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
        public static int LaySTTTiepTheoTrongNgay()
        {
            string sql = "SELECT ISNULL(MAX(STT), 0) + 1 FROM PhieuKham WHERE CAST(NgayKham AS DATE) = CAST(GETDATE() AS DATE)";
            var val = DatabaseHelper.ExecuteScalar(sql);
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

        public static DataTable LayHangChoKhamTrongNgay(string? maBacSi = null)
        {
            string sql = @"
                SELECT pk.MaPK, pk.STT, pk.NgayKham, pk.TrangThai,
                       bn.MaBN, bn.HoTen AS TenBN, bn.GioiTinh, DATEDIFF(YEAR, bn.NgaySinh, GETDATE()) AS Tuoi,
                       pk.TrieuChung, nv.HoTen AS TenBacSi, pk.MaNV_BacSi
                FROM PhieuKham pk
                INNER JOIN BenhNhan bn ON pk.MaBN = bn.MaBN
                INNER JOIN NhanVien nv ON pk.MaNV_BacSi = nv.MaNV
                WHERE CAST(pk.NgayKham AS DATE) = CAST(GETDATE() AS DATE)
                  AND (@MaBS IS NULL OR pk.MaNV_BacSi = @MaBS)
                ORDER BY pk.STT ASC";
            return DatabaseHelper.ExecuteQuery(sql, new[] { new SqlParameter("@MaBS", (object?)maBacSi ?? DBNull.Value) });
        }

        public static bool TaoPhieuKham(PhieuKham pk)
        {
            string sql = @"
                INSERT INTO PhieuKham (MaPK, STT, MaBN, MaNV_BacSi, NgayKham, TrieuChung, TrangThai)
                VALUES (@MaPK, @STT, @MaBN, @MaBS, GETDATE(), @TrieuChung, N'Chờ khám')";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", pk.MaPK),
                new SqlParameter("@STT", pk.STT),
                new SqlParameter("@MaBN", pk.MaBN),
                new SqlParameter("@MaBS", pk.MaNV_BacSi),
                new SqlParameter("@TrieuChung", (object?)pk.TrieuChung ?? DBNull.Value)
            }) > 0;
        }

        public static DataTable LayDanhSachBacSi()
        {
            string sql = "SELECT MaNV, HoTen, ChuyenKhoa FROM NhanVien WHERE ChucVu = 'BacSi' AND TrangThai = 1";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        // 4. KHÁM BỆNH & CẬN LÂM SÀNG
        public static bool CapNhatKetQuaKham(string maPK, string chanDoan, string loiDan, string trangThai)
        {
            string sql = @"
                UPDATE PhieuKham
                SET ChanDoan = @ChanDoan, LoiDan = @LoiDan, TrangThai = @TrangThai
                WHERE MaPK = @MaPK";
            return DatabaseHelper.ExecuteNonQuery(sql, new[]
            {
                new SqlParameter("@MaPK", maPK),
                new SqlParameter("@ChanDoan", chanDoan),
                new SqlParameter("@LoiDan", (object?)loiDan ?? DBNull.Value),
                new SqlParameter("@TrangThai", trangThai)
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

        // 8. NHÂN VIÊN (QUẢN TRỊ ADMIN)
        public static DataTable LayDanhSachNhanVien()
        {
            string sql = @"
                SELECT nv.MaNV, nv.HoTen, nv.ChucVu, nv.ChuyenKhoa, nv.SoDienThoai, nv.Email, nv.TrangThai,
                       tk.TenDangNhap
                FROM NhanVien nv
                LEFT JOIN TaiKhoan tk ON nv.MaNV = tk.MaNV
                ORDER BY nv.MaNV";
            return DatabaseHelper.ExecuteQuery(sql);
        }

        // 9. THỐNG KÊ DOANH THU & LƯỢT KHÁM
        public static (int tongKham, decimal tongThu, int tongThuocSapHet) LayThongKeTongQuan()
        {
            string sqlKham = "SELECT COUNT(*) FROM PhieuKham WHERE CAST(NgayKham AS DATE) = CAST(GETDATE() AS DATE)";
            int kham = Convert.ToInt32(DatabaseHelper.ExecuteScalar(sqlKham));

            string sqlThu = "SELECT ISNULL(SUM(TongTien), 0) FROM HoaDon WHERE TrangThai = N'Đã thanh toán'";
            decimal thu = Convert.ToDecimal(DatabaseHelper.ExecuteScalar(sqlThu));

            string sqlThuoc = "SELECT COUNT(*) FROM Thuoc WHERE SoLuongTon <= 20";
            int thuoc = Convert.ToInt32(DatabaseHelper.ExecuteScalar(sqlThuoc));

            return (kham, thu, thuoc);
        }
    }
}
