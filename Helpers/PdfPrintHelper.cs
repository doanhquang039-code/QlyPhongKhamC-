using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

namespace QlyPhongKham.Helpers
{
    public static class PdfPrintHelper
    {
        /// <summary>
        /// In hoặc Lưu file PDF Đơn Thuốc sử dụng PrintDocument / Microsoft Print to PDF
        /// </summary>
        public static void InDonThuoc(string maPK, string tenBN, string gioiTinh, string tuoi, string chanDoan, string loiDan, string bacSi, DataTable dtThuoc)
        {
            var printDoc = new PrintDocument();
            printDoc.DocumentName = $"DonThuoc_{maPK}_{DateTime.Now:yyyyMMdd}";

            printDoc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                if (g == null) return;

                int left = 50;
                int top = 50;
                int right = ev.PageBounds.Width - 50;
                int contentWidth = right - left;

                using var fontBrand = new Font("Segoe UI", 16, FontStyle.Bold);
                using var fontSubBrand = new Font("Segoe UI", 9, FontStyle.Regular);
                using var fontDocTitle = new Font("Segoe UI", 18, FontStyle.Bold);
                using var fontSection = new Font("Segoe UI", 11, FontStyle.Bold);
                using var fontBold = new Font("Segoe UI", 10, FontStyle.Bold);
                using var fontRegular = new Font("Segoe UI", 10, FontStyle.Regular);
                using var fontItalic = new Font("Segoe UI", 9, FontStyle.Italic);
                using var brushBlack = new SolidBrush(Color.FromArgb(30, 41, 59));
                using var brushTeal = new SolidBrush(Color.FromArgb(13, 148, 136));
                using var penLine = new Pen(Color.FromArgb(203, 213, 225), 1);
                using var penHeader = new Pen(Color.FromArgb(13, 148, 136), 2);

                // Phòng khám Header
                g.DrawString("PHÒNG KHÁM ĐA KHOA AN TÂM", fontBrand, brushTeal, left, top);
                top += 28;
                g.DrawString("Địa chỉ: 123 Nguyễn Tri Phương, Quận 5, TP. Hồ Chí Minh | Hotline: 1900 6868", fontSubBrand, brushBlack, left, top);
                top += 22;
                g.DrawLine(penHeader, left, top, right, top);
                top += 15;

                // Tiêu đề đơn thuốc
                string docTitle = "ĐƠN THUỐC";
                var sizeTitle = g.MeasureString(docTitle, fontDocTitle);
                g.DrawString(docTitle, fontDocTitle, brushBlack, left + (contentWidth - sizeTitle.Width) / 2, top);
                top += 32;

                string maPhieuText = $"Mã phiếu khám: {maPK} - Ngày: {DateTime.Now:dd/MM/yyyy}";
                var sizeMa = g.MeasureString(maPhieuText, fontItalic);
                g.DrawString(maPhieuText, fontItalic, brushBlack, left + (contentWidth - sizeMa.Width) / 2, top);
                top += 25;

                // Thông tin hành chính
                g.DrawString($"Họ tên bệnh nhân: {tenBN}", fontBold, brushBlack, left, top);
                g.DrawString($"Giới tính: {gioiTinh}", fontRegular, brushBlack, left + 360, top);
                g.DrawString($"Tuổi: {tuoi}", fontRegular, brushBlack, left + 500, top);
                top += 24;

                g.DrawString($"Chẩn đoán: {chanDoan}", fontRegular, brushBlack, left, top);
                top += 28;
                g.DrawLine(penLine, left, top, right, top);
                top += 15;

                // Danh sách thuốc
                g.DrawString("DANH SÁCH THUỐC ĐIỀU TRỊ:", fontSection, brushTeal, left, top);
                top += 26;

                // Bảng thuốc Header
                g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), left, top, contentWidth, 26);
                g.DrawRectangle(penLine, left, top, contentWidth, 26);
                g.DrawString("STT", fontBold, brushBlack, left + 8, top + 5);
                g.DrawString("Tên thuốc & Hàm lượng", fontBold, brushBlack, left + 55, top + 5);
                g.DrawString("ĐVT", fontBold, brushBlack, left + 330, top + 5);
                g.DrawString("SL", fontBold, brushBlack, left + 400, top + 5);
                g.DrawString("Cách dùng / Hướng dẫn", fontBold, brushBlack, left + 460, top + 5);
                top += 26;

                int stt = 1;
                foreach (DataRow r in dtThuoc.Rows)
                {
                    string tenT = r["TenThuoc"]?.ToString() ?? "";
                    string dvt = dtThuoc.Columns.Contains("DonViTinh") ? r["DonViTinh"]?.ToString() ?? "" : "Viên";
                    string sl = r["SoLuong"]?.ToString() ?? "0";
                    string cd = r["CachDung"]?.ToString() ?? "";

                    g.DrawRectangle(penLine, left, top, contentWidth, 34);
                    g.DrawString(stt.ToString(), fontRegular, brushBlack, left + 14, top + 8);
                    g.DrawString(tenT, fontBold, brushBlack, left + 55, top + 8);
                    g.DrawString(dvt, fontRegular, brushBlack, left + 330, top + 8);
                    g.DrawString(sl, fontBold, brushBlack, left + 400, top + 8);
                    g.DrawString(cd, fontItalic, brushBlack, left + 460, top + 8);

                    top += 34;
                    stt++;
                }

                top += 15;
                if (!string.IsNullOrWhiteSpace(loiDan))
                {
                    g.DrawString($"Lời dặn của bác sĩ: {loiDan}", fontItalic, brushBlack, left, top);
                    top += 30;
                }

                // Chữ ký
                top += 20;
                int signLeft = right - 220;
                g.DrawString($"Ngày {DateTime.Now.Day} tháng {DateTime.Now.Month} năm {DateTime.Now.Year}", fontItalic, brushBlack, signLeft, top);
                top += 20;
                g.DrawString("Bác sĩ khám bệnh", fontBold, brushBlack, signLeft + 35, top);
                top += 65;
                g.DrawString(bacSi, fontBold, brushTeal, signLeft + 30, top);

                ev.HasMorePages = false;
            };

            ExecutePrint(printDoc);
        }

        /// <summary>
        /// In hoặc Lưu file PDF Hóa Đơn Viện Phí
        /// </summary>
        public static void InHoaDon(string maHD, string maPK, string tenBN, string chanDoan, string hinhThucTT, decimal tienDV, decimal tienThuoc, decimal giamGia, decimal tongTien, string nguoiLap)
        {
            var printDoc = new PrintDocument();
            printDoc.DocumentName = $"HoaDon_{maHD}_{DateTime.Now:yyyyMMdd}";

            printDoc.PrintPage += (s, ev) =>
            {
                var g = ev.Graphics;
                if (g == null) return;

                int left = 50;
                int top = 50;
                int right = ev.PageBounds.Width - 50;
                int contentWidth = right - left;

                using var fontBrand = new Font("Segoe UI", 16, FontStyle.Bold);
                using var fontSubBrand = new Font("Segoe UI", 9, FontStyle.Regular);
                using var fontDocTitle = new Font("Segoe UI", 18, FontStyle.Bold);
                using var fontSection = new Font("Segoe UI", 11, FontStyle.Bold);
                using var fontBold = new Font("Segoe UI", 10, FontStyle.Bold);
                using var fontRegular = new Font("Segoe UI", 10, FontStyle.Regular);
                using var fontItalic = new Font("Segoe UI", 9, FontStyle.Italic);
                using var fontBigNumber = new Font("Segoe UI", 14, FontStyle.Bold);
                using var brushBlack = new SolidBrush(Color.FromArgb(30, 41, 59));
                using var brushBlue = new SolidBrush(Color.FromArgb(10, 80, 160));
                using var brushTeal = new SolidBrush(Color.FromArgb(13, 148, 136));
                using var penLine = new Pen(Color.FromArgb(203, 213, 225), 1);
                using var penHeader = new Pen(Color.FromArgb(10, 80, 160), 2);

                // Header
                g.DrawString("PHÒNG KHÁM ĐA KHOA AN TÂM", fontBrand, brushBlue, left, top);
                top += 28;
                g.DrawString("Mã số thuế: 0312345678 | Hotline hỗ trợ: 1900 6868", fontSubBrand, brushBlack, left, top);
                top += 22;
                g.DrawLine(penHeader, left, top, right, top);
                top += 15;

                // Title
                string docTitle = "HÓA ĐƠN THANH TOÁN VIỆN PHÍ";
                var sizeTitle = g.MeasureString(docTitle, fontDocTitle);
                g.DrawString(docTitle, fontDocTitle, brushBlack, left + (contentWidth - sizeTitle.Width) / 2, top);
                top += 32;

                string maHDText = $"Mã số: {maHD} - Ngày lập: {DateTime.Now:dd/MM/yyyy HH:mm}";
                var sizeMa = g.MeasureString(maHDText, fontItalic);
                g.DrawString(maHDText, fontItalic, brushBlack, left + (contentWidth - sizeMa.Width) / 2, top);
                top += 25;

                // Thông tin phiếu
                g.DrawString($"Họ tên bệnh nhân: {tenBN}", fontBold, brushBlack, left, top);
                g.DrawString($"Mã phiếu khám: {maPK}", fontRegular, brushBlack, left + 400, top);
                top += 24;
                g.DrawString($"Chẩn đoán bệnh: {chanDoan}", fontRegular, brushBlack, left, top);
                g.DrawString($"Hình thức TT: {hinhThucTT}", fontBold, brushBlue, left + 400, top);
                top += 28;
                g.DrawLine(penLine, left, top, right, top);
                top += 15;

                // Bảng chi phí
                g.DrawString("CHI TIẾT THANH TOÁN", fontSection, brushBlue, left, top);
                top += 25;

                g.FillRectangle(new SolidBrush(Color.FromArgb(241, 245, 249)), left, top, contentWidth, 26);
                g.DrawRectangle(penLine, left, top, contentWidth, 26);
                g.DrawString("STT", fontBold, brushBlack, left + 10, top + 5);
                g.DrawString("Khoản mục viện phí", fontBold, brushBlack, left + 60, top + 5);
                g.DrawString("Thành tiền (VNĐ)", fontBold, brushBlack, right - 160, top + 5);
                top += 26;

                // Item 1: Dịch vụ
                g.DrawRectangle(penLine, left, top, contentWidth, 28);
                g.DrawString("1", fontRegular, brushBlack, left + 15, top + 6);
                g.DrawString("Chi phí Khám & Dịch vụ Cận lâm sàng", fontRegular, brushBlack, left + 60, top + 6);
                g.DrawString($"{tienDV:N0} đ", fontBold, brushBlack, right - 160, top + 6);
                top += 28;

                // Item 2: Thuốc
                g.DrawRectangle(penLine, left, top, contentWidth, 28);
                g.DrawString("2", fontRegular, brushBlack, left + 15, top + 6);
                g.DrawString("Chi phí Dược phẩm theo toa", fontRegular, brushBlack, left + 60, top + 6);
                g.DrawString($"{tienThuoc:N0} đ", fontBold, brushBlack, right - 160, top + 6);
                top += 28;

                // Giảm giá
                if (giamGia > 0)
                {
                    g.DrawRectangle(penLine, left, top, contentWidth, 28);
                    g.DrawString("3", fontRegular, brushBlack, left + 15, top + 6);
                    g.DrawString("Ưu đãi / Giảm giá viện phí", fontRegular, brushBlack, left + 60, top + 6);
                    g.DrawString($"-{giamGia:N0} đ", fontBold, new SolidBrush(Color.Red), right - 160, top + 6);
                    top += 28;
                }

                // Tổng cộng Box
                top += 10;
                g.FillRectangle(new SolidBrush(Color.FromArgb(238, 242, 255)), left, top, contentWidth, 42);
                g.DrawRectangle(new Pen(Color.FromArgb(99, 102, 241), 1.5f), left, top, contentWidth, 42);
                g.DrawString("TỔNG TIỀN PHẢI THANH TOÁN:", fontBigNumber, brushBlue, left + 15, top + 10);
                g.DrawString($"{tongTien:N0} VNĐ", fontBigNumber, new SolidBrush(Color.FromArgb(220, 38, 38)), right - 220, top + 10);
                top += 55;

                // Chữ ký
                top += 20;
                int signLeftBN = left + 40;
                g.DrawString("Người nộp tiền", fontBold, brushBlack, signLeftBN + 20, top);
                g.DrawString("(Ký, ghi rõ họ tên)", fontItalic, brushBlack, signLeftBN + 15, top + 18);

                int signLeftTN = right - 220;
                g.DrawString("Thu ngân / Người lập", fontBold, brushBlack, signLeftTN + 25, top);
                g.DrawString("(Ký, đóng dấu)", fontItalic, brushBlack, signLeftTN + 40, top + 18);
                top += 70;
                g.DrawString(nguoiLap, fontBold, brushBlue, signLeftTN + 30, top);

                ev.HasMorePages = false;
            };

            ExecutePrint(printDoc);
        }

        private static void ExecutePrint(PrintDocument printDoc)
        {
            using var previewDlg = new PrintPreviewDialog
            {
                Document = printDoc,
                Width = 850,
                Height = 700,
                StartPosition = FormStartPosition.CenterParent,
                Text = "Xem trước bản in / Lưu PDF (Chọn máy in 'Microsoft Print to PDF')"
            };
            previewDlg.ShowDialog();
        }
    }
}

