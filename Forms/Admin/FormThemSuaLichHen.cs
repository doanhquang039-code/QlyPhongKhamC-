using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public sealed class FormThemSuaLichHen : Form
    {
        private readonly int? _maLichHen;
        private readonly ComboBox _patient;
        private readonly ComboBox _doctor;
        private readonly DateTimePicker _appointmentTime;
        private readonly TextBox _reason;
        private readonly ComboBox _status;

        public FormThemSuaLichHen(int? maLichHen = null, DataRow? appointment = null)
        {
            _maLichHen = maLichHen;
            Text = maLichHen.HasValue ? "Sửa lịch hẹn" : "Thêm lịch hẹn";
            Size = new Size(520, 420);
            MinimumSize = new Size(500, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 6,
                Padding = new Padding(18),
                BackColor = Color.White
            };
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            formLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _patient = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _patient.DataSource = ClinicDAL.LayDanhSachBenhNhan();
            _patient.DisplayMember = "HoTen";
            _patient.ValueMember = "MaBN";

            _doctor = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            _doctor.DataSource = ClinicDAL.LayDanhSachBacSi();
            _doctor.DisplayMember = "HoTen";
            _doctor.ValueMember = "MaNV";

            _appointmentTime = new DateTimePicker
            {
                Dock = DockStyle.Left,
                Width = 240,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy HH:mm",
                ShowUpDown = true,
                Value = DateTime.Now.AddHours(1)
            };
            _reason = new TextBox { Dock = DockStyle.Fill, Multiline = true, MaxLength = 255, ScrollBars = ScrollBars.Vertical };
            _status = new ComboBox { Dock = DockStyle.Left, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            _status.Items.AddRange(new object[] { "Chờ xác nhận", "Đã xác nhận", "Đã đến", "Đã hủy" });
            _status.SelectedIndex = 0;

            AddField(formLayout, "Bệnh nhân", _patient, 0);
            AddField(formLayout, "Bác sĩ", _doctor, 1);
            AddField(formLayout, "Ngày giờ hẹn", _appointmentTime, 2);
            AddField(formLayout, "Lý do khám", _reason, 3);
            AddField(formLayout, "Trạng thái", _status, 4);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false
            };
            var btnSave = new Button { Text = "Lưu", Width = 90, Height = 32 };
            var btnCancel = new Button { Text = "Đóng", Width = 90, Height = 32, DialogResult = DialogResult.Cancel };
            btnSave.Click += SaveAppointment;
            buttons.Controls.AddRange(new Control[] { btnSave, btnCancel });
            formLayout.Controls.Add(buttons, 1, 5);
            Controls.Add(formLayout);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            if (appointment != null)
            {
                _patient.SelectedValue = appointment["MaBN"];
                if (appointment["MaNV_BacSi"] != DBNull.Value)
                    _doctor.SelectedValue = appointment["MaNV_BacSi"];
                if (appointment["NgayHen"] != DBNull.Value)
                    _appointmentTime.Value = Convert.ToDateTime(appointment["NgayHen"]);
                _reason.Text = appointment["LyDoKham"] == DBNull.Value ? "" : appointment["LyDoKham"].ToString() ?? "";
                _status.SelectedItem = appointment["TrangThai"].ToString();
            }
        }

        private static void AddField(TableLayoutPanel layout, string label, Control control, int row)
        {
            layout.Controls.Add(new Label
            {
                Text = label,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            }, 0, row);
            layout.Controls.Add(control, 1, row);
        }

        private void SaveAppointment(object? sender, EventArgs e)
        {
            string maBN = _patient.SelectedValue?.ToString() ?? "";
            string? maBS = _doctor.SelectedValue?.ToString();
            if (string.IsNullOrWhiteSpace(maBN))
            {
                MessageBox.Show("Chọn bệnh nhân trước khi lưu lịch hẹn.", "Thiếu thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                bool saved = ClinicDAL.LuuLichHen(
                    _maLichHen,
                    maBN,
                    string.IsNullOrWhiteSpace(maBS) ? null : maBS,
                    _appointmentTime.Value,
                    _reason.Text.Trim(),
                    _status.SelectedItem?.ToString() ?? "Chờ xác nhận");
                if (!saved)
                {
                    MessageBox.Show("Không lưu được lịch hẹn. Kiểm tra bệnh nhân, bác sĩ và trạng thái rồi thử lại.",
                        "Lỗi lưu lịch hẹn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
