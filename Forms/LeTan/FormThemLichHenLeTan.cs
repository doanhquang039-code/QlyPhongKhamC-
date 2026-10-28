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
        private readonly DateTimePicker _appointmentTime;
        private readonly TextBox _reason;
        private readonly string _appointmentStatus;

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

            _patient = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _patient.DataSource = ClinicDAL.LayDanhSachBenhNhan();
            _patient.DisplayMember = "HoTen";
            _patient.ValueMember = "MaBN";
            _doctor = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _doctor.DataSource = ClinicDAL.LayDanhSachBacSiTheoNhanVien(maLeTan);
            _doctor.DisplayMember = "HoTen";
            _doctor.ValueMember = "MaNV";
            _appointmentTime = new DateTimePicker
            {
                Dock = DockStyle.Fill,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                ShowUpDown = true,
                Value = DateTime.Now.AddHours(1)
            };
            _reason = new TextBox { Dock = DockStyle.Fill, Multiline = true, MaxLength = 255, ScrollBars = ScrollBars.Vertical };

            AddField(layout, "Bệnh nhân *", _patient, 1);
            AddField(layout, "Bác sĩ *", _doctor, 2);
            AddField(layout, "Ngày giờ hẹn *", _appointmentTime, 3);
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
                    _appointmentTime.Value = Convert.ToDateTime(appointment["NgayHen"]);
                _reason.Text = appointment["LyDoKham"] == DBNull.Value ? "" : appointment["LyDoKham"].ToString() ?? "";
            }
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
            if (_patient.SelectedValue == null || _doctor.SelectedValue == null)
            {
                MessageBox.Show("Cần có hồ sơ bệnh nhân và bác sĩ được phân công phòng trước khi đặt lịch.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool saved = ClinicDAL.LuuLichHen(
                    _maLichHen,
                    _patient.SelectedValue.ToString() ?? "",
                    _doctor.SelectedValue.ToString(),
                    _appointmentTime.Value,
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
