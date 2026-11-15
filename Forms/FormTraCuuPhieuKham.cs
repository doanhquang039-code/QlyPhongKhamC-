using System;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    internal sealed class FormTraCuuPhieuKham : Form
    {
        private readonly TextBox _visitId;
        private readonly TextBox _phone;
        private readonly TextBox _otp;
        private readonly Button _sendCode;
        private readonly Button _verify;
        private readonly Label _status;

        public FormTraCuuPhieuKham()
        {
            Text = "Tra cứu phiếu khám không cần tài khoản";
            ClientSize = new Size(560, 370);
            MinimumSize = new Size(560, 370);
            MaximumSize = new Size(560, 370);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 248, 252);
            Font = new Font("Segoe UI", 9.5F);

            var title = new Label
            {
                Text = "TRA CỨU HỒ SƠ KHÁM BỆNH",
                Location = new Point(24, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 42, 63)
            };
            var explanation = new Label
            {
                Text = "Nhập mã phiếu và số điện thoại đã đăng ký. OTP sẽ được gửi đến email trong hồ sơ.",
                Location = new Point(26, 56),
                Size = new Size(510, 38),
                ForeColor = Color.FromArgb(82, 99, 112)
            };

            AddFieldLabel("Mã phiếu khám", 108);
            _visitId = AddInput(132);
            _visitId.CharacterCasing = CharacterCasing.Upper;
            _visitId.MaxLength = 10;

            AddFieldLabel("Số điện thoại trong hồ sơ", 174);
            _phone = AddInput(198);
            _phone.MaxLength = 20;

            _sendCode = new Button
            {
                Text = "GỬI MÃ OTP",
                Location = new Point(26, 238),
                Size = new Size(170, 36),
                BackColor = Color.FromArgb(0, 137, 123),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _sendCode.FlatAppearance.BorderSize = 0;
            _sendCode.Click += SendCodeClicked;

            _otp = AddInput(294);
            _otp.Location = new Point(26, 294);
            _otp.Size = new Size(170, 30);
            _otp.MaxLength = 8;
            _otp.Enabled = false;
            _otp.PlaceholderText = "Mã OTP 8 chữ số";

            _verify = new Button
            {
                Text = "XÁC MINH & XEM PHIẾU",
                Location = new Point(210, 291),
                Size = new Size(220, 36),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Enabled = false
            };
            _verify.FlatAppearance.BorderSize = 0;
            _verify.Click += VerifyClicked;

            _status = new Label
            {
                Location = new Point(26, 336),
                Size = new Size(510, 28),
                ForeColor = Color.FromArgb(82, 99, 112),
                AutoEllipsis = true
            };

            Controls.AddRange(new Control[] { title, explanation, _sendCode, _otp, _verify, _status });
            AcceptButton = _sendCode;
        }

        private async void SendCodeClicked(object? sender, EventArgs e)
        {
            _sendCode.Enabled = false;
            _status.Text = "Đang xử lý yêu cầu...";
            try
            {
                bool sent = await ClinicDAL.YeuCauOtpTraCuuPhieuKhamAsync(_visitId.Text, _phone.Text);
                _otp.Clear();
                _otp.Enabled = true;
                _verify.Enabled = true;
                _status.Text = sent
                    ? "Nếu thông tin khớp hồ sơ đã hoàn tất, mã OTP đã được gửi đến email trong hồ sơ. Mã hết hạn sau 10 phút."
                    : "Không gửi được mã. Kiểm tra thông tin hoặc liên hệ phòng khám nếu hồ sơ chưa có email.";
                _status.ForeColor = sent ? Color.FromArgb(22, 101, 52) : Color.FromArgb(180, 83, 9);
                AcceptButton = _verify;
            }
            catch (Exception ex)
            {
                _status.Text = ex.Message;
                _status.ForeColor = Color.FromArgb(185, 28, 28);
            }
            finally
            {
                _sendCode.Enabled = true;
            }
        }

        private void VerifyClicked(object? sender, EventArgs e)
        {
            _verify.Enabled = false;
            try
            {
                DataSet? record = ClinicDAL.XacThucOtpVaLayPhieuKham(_visitId.Text, _otp.Text);
                if (record == null)
                {
                    _status.Text = "Mã không hợp lệ, đã hết hạn hoặc đã được sử dụng. Hãy yêu cầu mã mới.";
                    _status.ForeColor = Color.FromArgb(185, 28, 28);
                    return;
                }

                using (record)
                using (var details = new FormChiTietTraCuuPhieuKham(record))
                    details.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _status.Text = $"Không thể tải phiếu khám: {ex.Message}";
                _status.ForeColor = Color.FromArgb(185, 28, 28);
            }
            finally
            {
                _verify.Enabled = true;
            }
        }

        private void AddFieldLabel(string text, int top)
            => Controls.Add(new Label
            {
                Text = text,
                Location = new Point(26, top),
                AutoSize = true,
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

        private TextBox AddInput(int top)
        {
            var input = new TextBox
            {
                Location = new Point(26, top),
                Size = new Size(510, 30),
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(input);
            return input;
        }
    }

    internal sealed class FormChiTietTraCuuPhieuKham : Form
    {
        public FormChiTietTraCuuPhieuKham(DataSet record)
        {
            Text = "Hồ sơ phiếu khám";
            Size = new Size(980, 720);
            MinimumSize = new Size(820, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(243, 246, 248);
            Font = new Font("Segoe UI", 9.5F);

            DataRow visit = record.Tables["Visit"]!.Rows[0];
            var summary = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 190,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F),
                Text =
                    $"Bệnh nhân: {visit["TenBN"]}\r\n" +
                    $"Mã phiếu: {visit["MaPK"]}    Ngày khám: {Convert.ToDateTime(visit["NgayKham"]):dd/MM/yyyy HH:mm}\r\n" +
                    $"Bác sĩ: {visit["TenBacSi"]}    Phòng: {Value(visit, "TenPhong")}\r\n" +
                    $"Trạng thái: {visit["TrangThai"]}\r\n\r\n" +
                    $"Triệu chứng: {Value(visit, "TrieuChung")}\r\n" +
                    $"Bệnh sử: {Value(visit, "BenhSu")}\r\n" +
                    $"Khám lâm sàng: {Value(visit, "KhamLamSang")}\r\n" +
                    $"Sinh hiệu: {Value(visit, "SinhHieu")}\r\n" +
                    $"Chẩn đoán: {Value(visit, "ChanDoan")}\r\n" +
                    $"Lời dặn: {Value(visit, "LoiDan")}"
            };

            var tabs = new TabControl { Dock = DockStyle.Fill };
            var services = CreateGrid(record.Tables["Services"]!, new[]
            {
                ("TenDV", "Dịch vụ"), ("DonGiaThoiDiem", "Đơn giá"),
                ("TrangThai", "Trạng thái"), ("KetQua", "Kết quả")
            });
            FormatMoney(services, "DonGiaThoiDiem");
            var prescriptions = CreateGrid(record.Tables["Prescriptions"]!, new[]
            {
                ("TenThuoc", "Thuốc"), ("DonViTinh", "Đơn vị"), ("SoLuong", "Số lượng"),
                ("DonGiaThoiDiem", "Đơn giá"), ("ThanhTien", "Thành tiền"), ("CachDung", "Cách dùng")
            });
            FormatMoney(prescriptions, "DonGiaThoiDiem");
            FormatMoney(prescriptions, "ThanhTien");
            var invoice = CreateGrid(record.Tables["Invoice"]!, new[]
            {
                ("MaHD", "Mã hóa đơn"), ("NgayLap", "Ngày lập"), ("TrangThai", "Trạng thái"),
                ("TongTien", "Tổng tiền"), ("TienBaoHiem", "Bảo hiểm"), ("DaThu", "Đã thu"),
                ("ConNo", "Còn nợ"), ("HinhThucTT", "Hình thức")
            });
            if (invoice.Columns.Contains("NgayLap"))
                invoice.Columns["NgayLap"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            foreach (string column in new[] { "TongTien", "TienBaoHiem", "DaThu", "ConNo" })
                FormatMoney(invoice, column);
            var invoiceItems = CreateGrid(record.Tables["InvoiceItems"]!, new[]
            {
                ("Loai", "Loại"), ("TenHang", "Tên dịch vụ / thuốc"), ("SoLuong", "Số lượng"),
                ("DonGia", "Đơn giá"), ("ThanhTien", "Thành tiền")
            });
            FormatMoney(invoiceItems, "DonGia");
            FormatMoney(invoiceItems, "ThanhTien");

            tabs.TabPages.Add(new TabPage("Dịch vụ CLS") { Controls = { services } });
            tabs.TabPages.Add(new TabPage("Đơn thuốc") { Controls = { prescriptions } });
            var invoicePanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            invoice.Dock = DockStyle.Top;
            invoice.Height = 100;
            invoiceItems.Dock = DockStyle.Fill;
            invoicePanel.Controls.Add(invoiceItems);
            invoicePanel.Controls.Add(invoice);
            tabs.TabPages.Add(new TabPage("Hóa đơn") { Controls = { invoicePanel } });
            Controls.Add(tabs);
            Controls.Add(summary);
            summary.BringToFront();
        }

        private static DataGridView CreateGrid(DataTable source, (string name, string header)[] columns)
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DataSource = source
            };
            foreach (var (name, header) in columns)
            {
                if (!source.Columns.Contains(name)) continue;
                grid.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = name,
                    DataPropertyName = name,
                    HeaderText = header,
                    SortMode = DataGridViewColumnSortMode.NotSortable
                });
            }
            return grid;
        }

        private static void FormatMoney(DataGridView grid, string column)
        {
            if (grid.Columns.Contains(column))
                grid.Columns[column]!.DefaultCellStyle.Format = "N0";
        }

        private static string Value(DataRow row, string column)
            => row[column] == DBNull.Value ? "—" : row[column]?.ToString() ?? "—";
    }
}
