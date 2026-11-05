using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    internal sealed class FormManHinhCho : Form
    {
        private readonly Label _currentCalls;
        private readonly DataGridView _queue;
        private readonly System.Windows.Forms.Timer _refreshTimer;

        public FormManHinhCho()
        {
            Text = "Màn hình gọi số";
            WindowState = FormWindowState.Maximized;
            BackColor = Color.FromArgb(15, 23, 42);
            KeyPreview = true;

            var title = new Label
            {
                Text = "PHÒNG KHÁM ĐA KHOA AN TÂM",
                Dock = DockStyle.Top,
                Height = 74,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 26, FontStyle.Bold),
                ForeColor = Color.White
            };
            _currentCalls = new Label
            {
                Dock = DockStyle.Top,
                Height = 150,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 30, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 212, 191)
            };
            _queue = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 18),
                ColumnHeadersHeight = 48,
                RowTemplate = { Height = 48 },
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            _queue.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(15, 23, 42),
                Font = new Font("Segoe UI", 14, FontStyle.Bold)
            };
            _queue.DefaultCellStyle = new DataGridViewCellStyle
            {
                SelectionBackColor = Color.FromArgb(204, 251, 241),
                SelectionForeColor = Color.FromArgb(15, 23, 42)
            };
            Controls.Add(_queue);
            Controls.Add(_currentCalls);
            Controls.Add(title);

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 10000 };
            _refreshTimer.Tick += (s, e) => LoadQueue();
            LoadQueue();
            _refreshTimer.Start();
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                    Close();
            };
            FormClosed += (s, e) => _refreshTimer.Stop();
        }

        private void LoadQueue()
        {
            try
            {
                DataTable queue = ClinicDAL.LayBangGoiSoTrongNgay();
                _queue.DataSource = queue;
                if (_queue.Columns.Contains("STT"))
                    _queue.Columns["STT"]!.HeaderText = "SỐ THỨ TỰ";
                if (_queue.Columns.Contains("TenPhong"))
                    _queue.Columns["TenPhong"]!.HeaderText = "PHÒNG KHÁM";
                if (_queue.Columns.Contains("TrangThai"))
                    _queue.Columns["TrangThai"]!.HeaderText = "TRẠNG THÁI";
                if (_queue.Columns.Contains("NgayKham"))
                {
                    _queue.Columns["NgayKham"]!.HeaderText = "THỜI GIAN";
                    _queue.Columns["NgayKham"]!.DefaultCellStyle.Format = "HH:mm";
                }
                _currentCalls.Text = queue.Rows.Count == 0
                    ? "HIỆN CHƯA CÓ BỆNH NHÂN TRONG HÀNG CHỜ"
                    : string.Join("       ", queue.Rows.Cast<DataRow>()
                        .Where(row => row["TrangThai"].ToString() == "Đang khám")
                        .Select(row => $"SỐ {row["STT"]}  •  {row["TenPhong"]}"));
                if (string.IsNullOrWhiteSpace(_currentCalls.Text))
                    _currentCalls.Text = "ĐANG CHỜ GỌI SỐ TIẾP THEO";
            }
            catch (Exception ex)
            {
                _currentCalls.Text = $"Không thể cập nhật hàng chờ. Vui lòng liên hệ lễ tân. ({ex.GetType().Name})";
            }
        }
    }
}
