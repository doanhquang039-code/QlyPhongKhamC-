using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    internal sealed class FormThemLichHenLeTan : Form
    {
        private readonly int? _maLichHen;
        private readonly ComboBox _patient;
        private readonly ComboBox _doctor;
        private readonly DateTimePicker _appointmentDate;
        private readonly ComboBox _appointmentTime;
        private readonly TextBox _reason;
        private readonly string _appointmentStatus;
        private readonly bool _hasAssignedDoctors;

        public FormThemLichHenLeTan(string maLeTan, DataRow? appointment = null)
        {
            _maLichHen = appointment == null ? null : Convert.ToInt32(appointment["MaLichHen"]);
            _appointmentStatus = appointment?["TrangThai"]?.ToString() ?? "Chờ xác nhận";
            Text = "Đặt lịch khám";
            Size = new Size(560, 410);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(18),
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            layout.Controls.Add(new Label
            {
                Text = "ĐẶT LỊCH KHÁM",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = FormMain.C_TextDark,
                TextAlign = ContentAlignment.MiddleLeft
            }, 0, 0);
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 0)!, 2);

            var patientPanel = new Panel { Dock = DockStyle.Fill };
            _patient = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList };
            var lblEmailHint = new Label { Dock = DockStyle.Bottom, Height = 18, ForeColor = Color.FromArgb(100, 116, 139), Font = new Font("Segoe UI", 8F), Text = "✉ Đang tải email..." };
            patientPanel.Controls.Add(_patient);
            patientPanel.Controls.Add(lblEmailHint);
            _patient.SelectedValueChanged += (s, e) =>
            {
                string ma = _patient.SelectedValue?.ToString() ?? "";
                string? em = ClinicDAL.LayEmailBenhNhanTheoMa(ma);
                lblEmailHint.Text = string.IsNullOrEmpty(em) ? "⚠ Bệnh nhân chưa có email" : $"✉ Email: {em}";
                lblEmailHint.ForeColor = string.IsNullOrEmpty(em) ? Color.FromArgb(217, 119, 6) : Color.FromArgb(16, 185, 129);
            };
            _patient.DataSource = ClinicDAL.LayDanhSachBenhNhan();
            _patient.DisplayMember = "HoTen";
            _patient.ValueMember = "MaBN";
            _doctor = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            DataTable doctors = ClinicDAL.LayDanhSachBacSiTheoNhanVien(maLeTan);
            _hasAssignedDoctors = doctors.Rows.Count > 0;
            if (_hasAssignedDoctors)
            {
                _doctor.DataSource = doctors;
            }
            else
            {
                _doctor.Items.Add("Chưa có bác sĩ chung phòng được phân công");
                _doctor.SelectedIndex = 0;
            }
            _doctor.DisplayMember = "HoTen";
            _doctor.ValueMember = "MaNV";
            _appointmentDate = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(1)
            };
            _appointmentTime = new ComboBox
            {
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            var timePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            timePanel.Controls.AddRange(new Control[]
            {
                _appointmentDate,
                new Label { Text = "Giờ", AutoSize = true, Padding = new Padding(6, 8, 0, 0) },
                _appointmentTime
            });
            _reason = new TextBox { Dock = DockStyle.Fill, Multiline = true, MaxLength = 255, ScrollBars = ScrollBars.Vertical };

            AddField(layout, "Bệnh nhân *", patientPanel, 1);
            AddField(layout, "Bác sĩ *", _doctor, 2);
            AddField(layout, "Ngày giờ hẹn *", timePanel, 3);
            AddField(layout, "Lý do khám", _reason, 4);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            var save = FormMain.PrimaryBtn("Lưu lịch hẹn", 0, 0, 125, 34);
            var cancel = new Button { Text = "Hủy", Width = 85, Height = 34, DialogResult = DialogResult.Cancel };
            save.Click += Save;
            buttons.Controls.AddRange(new Control[] { save, cancel });
            layout.Controls.Add(buttons, 1, 5);
            Controls.Add(layout);
            AcceptButton = save;
            CancelButton = cancel;

            if (appointment != null)
            {
                _patient.SelectedValue = appointment["MaBN"];
                _doctor.SelectedValue = appointment["MaNV_BacSi"];
                if (appointment["NgayHen"] != DBNull.Value)
                    _appointmentDate.Value = Convert.ToDateTime(appointment["NgayHen"]).Date;
                _reason.Text = appointment["LyDoKham"] == DBNull.Value ? "" : appointment["LyDoKham"].ToString() ?? "";
            }
            _appointmentDate.ValueChanged += (sender, args) => LoadAvailableSlots();
            _doctor.SelectedValueChanged += (sender, args) => LoadAvailableSlots();
            LoadAvailableSlots(appointment == null || appointment["NgayHen"] == DBNull.Value
                ? null
                : Convert.ToDateTime(appointment["NgayHen"]));
        }

        private void LoadAvailableSlots(DateTime? preferred = null)
        {
            _appointmentTime.Items.Clear();
            if (_doctor.SelectedValue == null)
                return;
            DataTable slots = ClinicDAL.LayKhungGioKhaDung(
                _doctor.SelectedValue.ToString() ?? "",
                _appointmentDate.Value.Date,
                _maLichHen);
            foreach (DataRow row in slots.Rows)
                _appointmentTime.Items.Add(Convert.ToDateTime(row["NgayGio"]).ToString("HH:mm"));
            if (slots.Rows.Count == 0)
            {
                _appointmentTime.Items.Add("Chưa có giờ - nhờ Admin cấu hình ca");
                _appointmentTime.SelectedIndex = 0;
            }
            _appointmentTime.Enabled = true;
            string preferredText = preferred?.ToString("HH:mm") ?? "";
            if (_appointmentTime.Items.Contains(preferredText))
                _appointmentTime.SelectedItem = preferredText;
            else if (_appointmentTime.Items.Count > 0)
                _appointmentTime.SelectedIndex = 0;
        }

        private static void AddField(TableLayoutPanel layout, string label, Control control, int row)
        {
            layout.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            }, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private void Save(object? sender, EventArgs e)
        {
            if (_patient.SelectedValue == null || !_hasAssignedDoctors || _doctor.SelectedValue == null)
            {
                MessageBox.Show("Cần có hồ sơ bệnh nhân và bác sĩ được phân công phòng trước khi đặt lịch.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_appointmentTime.SelectedItem == null
                || !TimeSpan.TryParse(_appointmentTime.SelectedItem.ToString(), out _))
            {
                MessageBox.Show("Bác sĩ không có slot 15 phút khả dụng trong ngày đã chọn. Hãy chọn ngày khác hoặc nhờ quản trị viên cấu hình ca.",
                    "Không có giờ hẹn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                DateTime appointmentDateTime = _appointmentDate.Value.Date.Add(
                    TimeSpan.Parse(_appointmentTime.SelectedItem.ToString() ?? ""));
                bool saved = ClinicDAL.LuuLichHen(
                    _maLichHen,
                    _patient.SelectedValue.ToString() ?? "",
                    _doctor.SelectedValue.ToString(),
                    appointmentDateTime,
                    _reason.Text.Trim(),
                    _appointmentStatus);
                if (!saved)
                    throw new InvalidOperationException("Không lưu được lịch hẹn. Hãy kiểm tra hồ sơ và bác sĩ.");

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể lưu lịch hẹn",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
