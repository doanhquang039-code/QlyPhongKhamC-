using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using QlyPhongKham.DAL;
using QlyPhongKham.Helpers;

namespace QlyPhongKham.Helpers
{
    /// <summary>
    /// Quản lý thông báo nội bộ hệ thống (bảng ThongBao).
    /// Mọi thao tác ghi thông báo đều fire-and-forget qua Task để không block UI.
    /// </summary>
    public static class NotificationHelper
    {
        // ── Schema migration (lazy, idempotent) ──────────────────────
        private static volatile bool _ready;
        private static readonly object _lock = new();

        public static void EnsureSchema()
        {
            if (_ready) return;
            lock (_lock)
            {
                if (_ready) return;
                DatabaseHelper.ExecuteNonQuery(@"
                    IF OBJECT_ID(N'dbo.ThongBao', N'U') IS NULL
                    BEGIN
                        CREATE TABLE dbo.ThongBao (
                            MaThongBao  BIGINT IDENTITY(1,1) PRIMARY KEY,
                            MaNV        VARCHAR(10)    NULL,   -- người nhận (NULL = tất cả admin)
                            TieuDe      NVARCHAR(200)  NOT NULL,
                            NoiDung     NVARCHAR(1000) NOT NULL,
                            LoaiThongBao NVARCHAR(50)  NOT NULL DEFAULT N'Hệ thống',
                            NgayTao     DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
                            DaDoc       BIT            NOT NULL DEFAULT 0,
                            CONSTRAINT FK_ThongBao_NhanVien FOREIGN KEY (MaNV)
                                REFERENCES dbo.NhanVien(MaNV) ON DELETE SET NULL
                        );
                        CREATE INDEX IX_ThongBao_MaNV_DaDoc
                            ON dbo.ThongBao (MaNV, DaDoc, NgayTao DESC);
                    END;");
                _ready = true;
            }
        }

        // ── Ghi thông báo ───────────────────────────────────────────
        /// <summary>Ghi thông báo cho một nhân viên cụ thể (fire-and-forget).</summary>
        public static void Push(string maNV, string tieuDe, string noiDung, string loai = "Hệ thống")
        {
            Task.Run(() =>
            {
                try
                {
                    EnsureSchema();
                    DatabaseHelper.ExecuteNonQuery(
                        @"INSERT dbo.ThongBao (MaNV, TieuDe, NoiDung, LoaiThongBao)
                          VALUES (@MaNV, @TD, @ND, @Loai)",
                        new[]
                        {
                            new SqlParameter("@MaNV", (object?)maNV ?? DBNull.Value),
                            new SqlParameter("@TD",   tieuDe),
                            new SqlParameter("@ND",   noiDung),
                            new SqlParameter("@Loai", loai)
                        });
                }
                catch { /* thông báo không được phép làm gián đoạn luồng chính */ }
            });
        }

        /// <summary>Ghi thông báo cho tất cả Admin (fire-and-forget).</summary>
        public static void PushToAdmins(string tieuDe, string noiDung, string loai = "Hệ thống")
        {
            Task.Run(() =>
            {
                try
                {
                    EnsureSchema();
                    // INSERT một dòng per admin đang hoạt động
                    DatabaseHelper.ExecuteNonQuery(
                        @"INSERT dbo.ThongBao (MaNV, TieuDe, NoiDung, LoaiThongBao)
                          SELECT MaNV, @TD, @ND, @Loai
                          FROM   dbo.NhanVien
                          WHERE  ChucVu = N'Admin' AND TrangThai = 1",
                        new[]
                        {
                            new SqlParameter("@TD",   tieuDe),
                            new SqlParameter("@ND",   noiDung),
                            new SqlParameter("@Loai", loai)
                        });
                }
                catch { }
            });
        }

        // ── Truy vấn ────────────────────────────────────────────────
        /// <summary>Số thông báo chưa đọc của nhân viên hiện tại.</summary>
        public static int LaySoChuaDoc(string maNV)
        {
            try
            {
                EnsureSchema();
                var val = DatabaseHelper.ExecuteScalar(
                    "SELECT COUNT(*) FROM dbo.ThongBao WHERE MaNV = @MaNV AND DaDoc = 0",
                    new[] { new SqlParameter("@MaNV", maNV) });
                return Convert.ToInt32(val);
            }
            catch { return 0; }
        }

        /// <summary>Lấy 50 thông báo gần nhất (đã + chưa đọc) của nhân viên.</summary>
        public static DataTable LayThongBao(string maNV, bool chuaDocOnly = false)
        {
            try
            {
                EnsureSchema();
                string filter = chuaDocOnly ? "AND DaDoc = 0" : "";
                return DatabaseHelper.ExecuteQuery(
                    $@"SELECT TOP 50 MaThongBao, TieuDe, NoiDung, LoaiThongBao, NgayTao, DaDoc
                       FROM dbo.ThongBao
                       WHERE MaNV = @MaNV {filter}
                       ORDER BY NgayTao DESC",
                    new[] { new SqlParameter("@MaNV", maNV) });
            }
            catch { return new DataTable(); }
        }

        /// <summary>Đánh dấu đã đọc một hoặc tất cả thông báo.</summary>
        public static void DanhDauDaDoc(string maNV, long? maThongBao = null)
        {
            Task.Run(() =>
            {
                try
                {
                    EnsureSchema();
                    if (maThongBao.HasValue)
                        DatabaseHelper.ExecuteNonQuery(
                            "UPDATE dbo.ThongBao SET DaDoc=1 WHERE MaThongBao=@ID AND MaNV=@MaNV",
                            new[] { new SqlParameter("@ID", maThongBao.Value), new SqlParameter("@MaNV", maNV) });
                    else
                        DatabaseHelper.ExecuteNonQuery(
                            "UPDATE dbo.ThongBao SET DaDoc=1 WHERE MaNV=@MaNV AND DaDoc=0",
                            new[] { new SqlParameter("@MaNV", maNV) });
                }
                catch { }
            });
        }
    }
}

