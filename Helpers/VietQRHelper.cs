using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace QlyPhongKham.Helpers
{
    public class BankConfig
    {
        public string BankCode { get; set; } = "MBBank";
        public string BankName { get; set; } = "MBBank - Ngân hàng Quân Đội";
        public string AccountNo { get; set; } = "0392345678";
        public string AccountName { get; set; } = "PHONG KHAM DA KHOA AN TAM";
    }

    public static class VietQRHelper
    {
        private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bankconfig.txt");
        private static BankConfig? _cachedConfig;

        public static readonly (string Code, string ShortName, string FullName)[] SupportedBanks = new[]
        {
            ("MBBank", "MB", "MBBank - Ngân hàng Quân Đội"),
            ("Vietcombank", "VCB", "Vietcombank - Ngân hàng Ngoại Thương"),
            ("Techcombank", "TCB", "Techcombank - Ngân hàng Kỹ Thương"),
            ("BIDV", "BIDV", "BIDV - Đầu tư và Phát triển VN"),
            ("VietinBank", "CTG", "VietinBank - Công Thương Việt Nam"),
            ("Agribank", "VBA", "Agribank - Nông nghiệp & PTNT"),
            ("ACB", "ACB", "ACB - Ngân hàng Á Châu"),
            ("VPBank", "VPB", "VPBank - Ngân hàng VN Thịnh Vượng"),
            ("TPBank", "TPB", "TPBank - Ngân hàng Tiên Phong"),
            ("Sacombank", "STB", "Sacombank - Sài Gòn Thương Tín"),
            ("HDBank", "HDB", "HDBank - Phát triển TP.HCM"),
            ("VIB", "VIB", "VIB - Quốc tế Việt Nam"),
            ("SHB", "SHB", "SHB - Sài Gòn - Hà Nội"),
            ("MSB", "MSB", "MSB - Hàng Hải Việt Nam"),
            ("OCB", "OCB", "OCB - Phương Đông")
        };

        public static BankConfig GetConfig()
        {
            if (_cachedConfig != null) return _cachedConfig;
            var config = new BankConfig();
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var lines = File.ReadAllLines(ConfigPath);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=', 2);
                        if (parts.Length != 2) continue;
                        string key = parts[0].Trim();
                        string val = parts[1].Trim();
                        if (key.Equals("BankCode", StringComparison.OrdinalIgnoreCase)) config.BankCode = val;
                        else if (key.Equals("BankName", StringComparison.OrdinalIgnoreCase)) config.BankName = val;
                        else if (key.Equals("AccountNo", StringComparison.OrdinalIgnoreCase)) config.AccountNo = val;
                        else if (key.Equals("AccountName", StringComparison.OrdinalIgnoreCase)) config.AccountName = val;
                    }
                }
                else
                {
                    SaveConfig(config);
                }
            }
            catch { }
            _cachedConfig = config;
            return config;
        }

        public static void SaveConfig(BankConfig config)
        {
            _cachedConfig = config;
            try
            {
                var lines = new[]
                {
                    $"BankCode={config.BankCode}",
                    $"BankName={config.BankName}",
                    $"AccountNo={config.AccountNo}",
                    $"AccountName={config.AccountName}"
                };
                File.WriteAllLines(ConfigPath, lines);
            }
            catch { }
        }

        public static string BuildQrUrl(string bankCode, string accountNo, decimal amount, string description)
        {
            long cleanAmount = Math.Max(0, (long)amount);
            string cleanDes = (description ?? "").Trim();
            return $"https://qr.sepay.vn/img?bank={Uri.EscapeDataString(bankCode)}&acc={Uri.EscapeDataString(accountNo)}&template=compact&amount={cleanAmount}&des={Uri.EscapeDataString(cleanDes)}";
        }

        public static async Task<Image?> LoadQrImageAsync(string bankCode, string accountNo, decimal amount, string description)
        {
            try
            {
                string url = BuildQrUrl(bankCode, accountNo, amount, description);
                using var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync();
                    return Image.FromStream(stream);
                }
            }
            catch
            {
                // Fallback nếu api chính lỗi mạng: thử api qrserver
                try
                {
                    string fallbackData = $"2|99|{accountNo}|{bankCode}|{(long)amount}|{description}";
                    string fallbackUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=280x280&data={Uri.EscapeDataString(fallbackData)}";
                    using var res = await _httpClient.GetAsync(fallbackUrl);
                    if (res.IsSuccessStatusCode)
                    {
                        await using var stream = await res.Content.ReadAsStreamAsync();
                        return Image.FromStream(stream);
                    }
                }
                catch { }
            }
            return null;
        }

        public static Image CreateFallbackCard(string bankName, string accountNo, string accountName, decimal amount, string description)
        {
            var bmp = new Bitmap(280, 280);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(248, 250, 252));
                using var borderPen = new Pen(Color.FromArgb(13, 148, 136), 2);
                g.DrawRectangle(borderPen, 1, 1, 277, 277);

                using var brushTeal = new SolidBrush(Color.FromArgb(13, 148, 136));
                g.FillRectangle(brushTeal, 2, 2, 276, 36);

                using var fontHeader = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                using var brushWhite = new SolidBrush(Color.White);
                g.DrawString("THÔNG TIN CHUYỂN KHOẢN", fontHeader, brushWhite, 40, 10);

                using var fontBold = new Font("Segoe UI", 9f, FontStyle.Bold);
                using var fontRegular = new Font("Segoe UI", 9f, FontStyle.Regular);
                using var brushDark = new SolidBrush(Color.FromArgb(15, 32, 54));
                using var brushGray = new SolidBrush(Color.FromArgb(100, 116, 139));
                using var brushGreen = new SolidBrush(Color.FromArgb(16, 185, 129));

                int y = 50;
                g.DrawString("Ngân hàng:", fontRegular, brushGray, 12, y);
                g.DrawString(bankName, fontBold, brushDark, 12, y + 16);

                y += 42;
                g.DrawString("Số tài khoản:", fontRegular, brushGray, 12, y);
                g.DrawString(accountNo, new Font("Segoe UI", 11f, FontStyle.Bold), brushTeal, 12, y + 16);

                y += 42;
                g.DrawString("Chủ tài khoản:", fontRegular, brushGray, 12, y);
                g.DrawString(accountName, fontBold, brushDark, 12, y + 16);

                y += 42;
                g.DrawString("Số tiền:", fontRegular, brushGray, 12, y);
                g.DrawString($"{amount:N0} đ", new Font("Segoe UI", 12f, FontStyle.Bold), brushGreen, 12, y + 16);

                y += 44;
                g.DrawString("Nội dung chuyển khoản:", fontRegular, brushGray, 12, y);
                g.DrawString(description, fontBold, brushDark, 12, y + 16);
            }
            return bmp;
        }

        public static bool ShowBankConfigDialog(IWin32Window parent)
        {
            var current = GetConfig();
            using var dlg = new Form
            {
                Text = "Cấu hình tài khoản ngân hàng nhận tiền",
                Size = new Size(460, 360),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var lblTitle = new Label
            {
                Text = "🏦 TÀI KHOẢN THỤ HƯỞNG VIETQR",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(13, 148, 136),
                Location = new Point(20, 15),
                AutoSize = true
            };

            var lblBank = new Label { Text = "Ngân hàng:", Location = new Point(20, 52), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var cboBank = new ComboBox { Location = new Point(20, 72), Size = new Size(400, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5F) };
            foreach (var b in SupportedBanks)
            {
                cboBank.Items.Add(b.FullName);
            }
            int selectIdx = Array.FindIndex(SupportedBanks, x => x.Code.Equals(current.BankCode, StringComparison.OrdinalIgnoreCase));
            cboBank.SelectedIndex = selectIdx >= 0 ? selectIdx : 0;

            var lblAccNo = new Label { Text = "Số tài khoản nhận tiền:", Location = new Point(20, 110), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var txtAccNo = new TextBox { Text = current.AccountNo, Location = new Point(20, 130), Size = new Size(400, 28), Font = new Font("Segoe UI", 9.5F) };

            var lblAccName = new Label { Text = "Tên chủ tài khoản (viết hoa không dấu):", Location = new Point(20, 168), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var txtAccName = new TextBox { Text = current.AccountName, Location = new Point(20, 188), Size = new Size(400, 28), Font = new Font("Segoe UI", 9.5F) };

            var pnlBtns = new Panel { Dock = DockStyle.Bottom, Height = 56, BackColor = Color.FromArgb(248, 250, 252) };
            var btnSave = new Button
            {
                Text = "Lưu cấu hình",
                DialogResult = DialogResult.OK,
                Size = new Size(130, 34),
                Location = new Point(200, 11),
                BackColor = Color.FromArgb(13, 148, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            var btnCancel = new Button
            {
                Text = "Hủy",
                DialogResult = DialogResult.Cancel,
                Size = new Size(85, 34),
                Location = new Point(335, 11),
                BackColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F)
            };
            pnlBtns.Controls.AddRange(new Control[] { btnSave, btnCancel });

            dlg.Controls.AddRange(new Control[] { lblTitle, lblBank, cboBank, lblAccNo, txtAccNo, lblAccName, txtAccName, pnlBtns });

            if (dlg.ShowDialog(parent) == DialogResult.OK)
            {
                int chosen = cboBank.SelectedIndex;
                if (chosen >= 0 && chosen < SupportedBanks.Length)
                {
                    current.BankCode = SupportedBanks[chosen].Code;
                    current.BankName = SupportedBanks[chosen].FullName;
                }
                current.AccountNo = txtAccNo.Text.Trim();
                current.AccountName = txtAccName.Text.Trim().ToUpper();
                SaveConfig(current);
                MessageBox.Show("Đã lưu thông tin tài khoản ngân hàng thụ hưởng!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Mở hộp thoại hiển thị mã QR VietQR độc lập để bệnh nhân hoặc thu ngân quét nhanh
        /// </summary>
        public static void ShowPaymentQrDialog(IWin32Window parent, string maHD, decimal soTien, string transferContent)
        {
            var config = GetConfig();
            using var dlg = new Form
            {
                Text = $"Mã VietQR thanh toán - Hóa đơn #{maHD}",
                Size = new Size(450, 540),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(240, 253, 250), Padding = new Padding(12) };
            var lblH = new Label { Text = $"THANH TOÁN HÓA ĐƠN: {maHD}", Dock = DockStyle.Top, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 118, 110) };
            var lblT = new Label { Text = $"Số tiền cần thanh toán: {soTien:N0} đ", Dock = DockStyle.Bottom, Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(220, 38, 38) };
            pnlTop.Controls.AddRange(new Control[] { lblH, lblT });

            var picQr = new PictureBox
            {
                Size = new Size(330, 330),
                Location = new Point(52, 70),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(249, 250, 251)
            };

            var lblAcc = new Label
            {
                Text = $"Ngân hàng: {config.BankName} ({config.BankCode})\nSTK: {config.AccountNo} - {config.AccountName}\nNội dung: {transferContent}",
                Location = new Point(20, 410),
                Size = new Size(395, 45),
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(75, 85, 99),
                TextAlign = ContentAlignment.TopCenter
            };

            var btnClose = new Button
            {
                Text = "Đóng",
                Location = new Point(170, 462),
                Size = new Size(95, 30),
                BackColor = Color.FromArgb(13, 148, 136),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnClose.FlatAppearance.BorderSize = 0;

            dlg.Controls.AddRange(new Control[] { pnlTop, picQr, lblAcc, btnClose });

            dlg.Shown += async (s, e) =>
            {
                var img = await LoadQrImageAsync(config.BankCode, config.AccountNo, soTien, transferContent);
                picQr.Image = img ?? CreateFallbackCard(config.BankCode, config.AccountNo, config.AccountName, soTien, transferContent);
            };

            dlg.ShowDialog(parent);
        }
    }
}
