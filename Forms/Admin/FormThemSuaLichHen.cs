using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Helpers;

namespace QlyPhongKham.Forms
{
    public sealed class FormThemSuaLichHen : Form
    {
        private readonly int? _maLichHen;
        private readonly ComboBox _patient;
        private readonly ComboBox _doctor;
        private readonly DateTimePicker _appointmentDate;
        private readonly ComboBox _appointmentTime;
        private readonly TextBox _reason;
        private readonly ComboBox _status;

        public FormThemSuaLichHen(int? maLichHen = null, DataRow? appointment = null)
        {
            _maLichHen = maLichHen;
            Text = maLichHen.HasValue ? "Sửa lịch hẹn" : "Thêm lịch hẹn";
            Size = new Size(610, 420);
            MinimumSize = new Size(590, 400);
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
            formLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
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
            var doctorSchedule = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 8f),
                TextAlign = ContentAlignment.MiddleLeft
            };
            var doctorField = new Panel { Dock = DockStyle.Fill };
            _doctor.Dock = DockStyle.Top;
            _doctor.Height = 28;
            doctorField.Controls.Add(doctorSchedule);
            doctorField.Controls.Add(_doctor);
            void UpdateDoctorSchedule()
            {
                doctorSchedule.Text = _doctor.SelectedValue == null
                    ? "Chưa chọn bác sĩ."
                    : ClinicDAL.MoTaLichTrucBacSi(_doctor.SelectedValue.ToString() ?? "");
            }
            _doctor.SelectedValueChanged += (sender, args) => UpdateDoctorSchedule();

            _appointmentDate = new DateTimePicker
            {
                Width = 135,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(1)
            };
            _appointmentTime = new ComboBox { Width = 85, DropDownStyle = ComboBoxStyle.DropDownList };
            var appointmentSlot = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            appointmentSlot.Controls.AddRange(new Control[]
            {
                _appointmentDate,
                new Label { Text = "Giờ", AutoSize = true, Padding = new Padding(6, 8, 0, 0) },
                _appointmentTime
            });
            var configureShifts = new Button
            {
                Text = "Cấu hình ca",
                Width = 95,
                Height = 28,
                Visible = QlyPhongKham.SessionState.IsAdmin,
                Margin = new Padding(8, 2, 0, 0)
            };
            configureShifts.Click += (sender, args) =>
            {
                using var shiftForm = new FormCaLamViecBacSi();
                shiftForm.ShowDialog(this);
                LoadAvailableSlots();
            };
            appointmentSlot.Controls.Add(configureShifts);
            _reason = new TextBox { Dock = DockStyle.Fill, Multiline = true, MaxLength = 255, ScrollBars = ScrollBars.Vertical };
            _status = new ComboBox { Dock = DockStyle.Left, Width = 240, DropDownStyle = ComboBoxStyle.DropDownList };
            _status.Items.AddRange(new object[] { "Chờ xác nhận", "Đã xác nhận" });
            _status.SelectedIndex = 0;

            AddField(formLayout, "Bệnh nhân", _patient, 0);
            AddField(formLayout, "Bác sĩ", doctorField, 1);
            AddField(formLayout, "Ngày giờ hẹn", appointmentSlot, 2);
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
                    _appointmentDate.Value = Convert.ToDateTime(appointment["NgayHen"]).Date;
                _reason.Text = appointment["LyDoKham"] == DBNull.Value ? "" : appointment["LyDoKham"].ToString() ?? "";
                _status.SelectedItem = appointment["TrangThai"].ToString();
            }
            _appointmentDate.ValueChanged += (sender, args) => LoadAvailableSlots();
            _doctor.SelectedValueChanged += (sender, args) => LoadAvailableSlots();
            _appointmentTime.AccessibleDescription = "Chưa có ca làm hoặc không còn khung giờ trống. Quản trị viên có thể cấu hình ca.";
            UpdateDoctorSchedule();
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
                _appointmentTime.Items.Add("Chưa có ca - cấu hình ca bác sĩ");
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
            if (_appointmentTime.SelectedItem == null
                || !TimeSpan.TryParse(_appointmentTime.SelectedItem.ToString(), out _))
            {
                MessageBox.Show("Bác sĩ không có slot 15 phút khả dụng trong ngày đã chọn. Hãy chọn ngày khác hoặc cấu hình ca làm việc.",
                    "Không có giờ hẹn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                DateTime appointmentDateTime = _appointmentDate.Value.Date.Add(
                    TimeSpan.Parse(_appointmentTime.SelectedItem.ToString() ?? ""));
                string trangThai = _status.SelectedItem?.ToString() ?? "Chờ xác nhận";
                bool saved = ClinicDAL.LuuLichHen(
                    _maLichHen,
                    maBN,
                    string.IsNullOrWhiteSpace(maBS) ? null : maBS,
                    appointmentDateTime,
                    _reason.Text.Trim(),
                    trangThai);
                if (!saved)
                {
                    MessageBox.Show("Không lưu được lịch hẹn. Kiểm tra bệnh nhân, bác sĩ và trạng thái rồi thử lại.",
                        "Lỗi lưu lịch hẹn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Gửi email và thông báo nếu lịch hẹn được xác nhận
                if (trangThai == "Đã xác nhận")
                {
                    try
                    {
                        string tenBN = _patient.Text;
                        string tenBS = _doctor.Text;
                        string? emailBN = ClinicDAL.LayEmailBenhNhanTheoMa(maBN);
                        if (!string.IsNullOrEmpty(emailBN))
                        {
                            string body = EmailHelper.BuildXacNhanLichHenBody(tenBN, tenBS, appointmentDateTime, _reason.Text.Trim());
                            EmailHelper.SendAsync(emailBN, $"[An Tâm Clinic] Xác nhận lịch hẹn khám bệnh - {appointmentDateTime:dd/MM/yyyy HH:mm}", body);
                        }

                        if (!string.IsNullOrEmpty(maBS))
                        {
                            NotificationHelper.Push(maBS, "Lịch hẹn mới đã xác nhận",
                                $"Bệnh nhân {tenBN} có lịch hẹn lúc {appointmentDateTime:dd/MM/yyyy HH:mm}. Lý do: {_reason.Text.Trim()}",
                                "LichHen");
                        }

                        NotificationHelper.PushToAdmins("Lịch hẹn đã xác nhận",
                            $"Lịch hẹn khám của bệnh nhân {tenBN} (Bác sĩ: {tenBS}) lúc {appointmentDateTime:dd/MM/yyyy HH:mm} đã được xác nhận.",
                            "LichHen");
                    }
                    catch { }
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
