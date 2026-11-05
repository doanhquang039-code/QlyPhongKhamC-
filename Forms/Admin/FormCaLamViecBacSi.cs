using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    internal sealed class FormCaLamViecBacSi : Form
    {
        private readonly DataGridView _grid;
        private readonly ComboBox _doctor;
        private readonly ComboBox _day;
        private readonly DateTimePicker _start;
        private readonly DateTimePicker _end;

        public FormCaLamViecBacSi()
        {
            Text = "Ca làm việc & khung giờ bác sĩ";
            Size = new Size(860, 520);
            MinimumSize = new Size(700, 420);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;

            var title = new Label
            {
                Text = "CẤU HÌNH CA LÀM VIỆC BÁC SĨ • MỖI SLOT KHÁM 15 PHÚT",
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(14, 14, 0, 0),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            };
            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 78,
                Padding = new Padding(12, 8, 8, 8),
                WrapContents = false
            };
            _doctor = new ComboBox
            {
                Width = 210,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DataSource = ClinicDAL.LayDanhSachBacSi(),
                DisplayMember = "HoTen",
                ValueMember = "MaNV"
            };
            _day = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            _day.Items.AddRange(new object[] { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ nhật" });
            _day.SelectedIndex = 0;
            _start = TimePicker(8);
            _end = TimePicker(17);
            var add = new Button
            {
                Text = "Thêm ca",
                Width = 90,
                Height = 32,
                BackColor = Color.FromArgb(0, 180, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            add.FlatAppearance.BorderSize = 0;
            var remove = new Button
            {
                Text = "Xóa ca đã chọn",
                Width = 125,
                Height = 32,
                BackColor = Color.FromArgb(239, 68, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            remove.FlatAppearance.BorderSize = 0;
            toolbar.Controls.AddRange(new Control[]
            {
                MakeCaption("Bác sĩ"), _doctor,
                MakeCaption("Ngày"), _day,
                MakeCaption("Từ"), _start,
                MakeCaption("Đến"), _end,
                add, remove
            });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            add.Click += (sender, args) =>
            {
                if (_doctor.SelectedValue == null)
                {
                    MessageBox.Show("Chọn bác sĩ trước.", "Thiếu thông tin",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    ClinicDAL.LuuCaLamViecBacSi(
                        _doctor.SelectedValue.ToString() ?? "",
                        _day.SelectedIndex + 1,
                        _start.Value.TimeOfDay,
                        _end.Value.TimeOfDay);
                    LoadShifts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể thêm ca làm việc",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            remove.Click += (sender, args) =>
            {
                if (_grid.SelectedRows.Count == 0
                    || _grid.SelectedRows[0].DataBoundItem is not DataRowView selected)
                {
                    MessageBox.Show("Chọn ca cần xóa.", "Chưa chọn ca",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    if (!ClinicDAL.XoaCaLamViecBacSi(Convert.ToInt32(selected.Row["MaCa"])))
                        throw new InvalidOperationException("Không xóa được ca đang có lịch hẹn tương lai.");
                    LoadShifts();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể xóa ca",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            Controls.Add(_grid);
            Controls.Add(toolbar);
            Controls.Add(title);
            LoadShifts();
        }

        private void LoadShifts()
        {
            _grid.DataSource = ClinicDAL.LayCaLamViecBacSi();
            if (_grid.Columns.Contains("MaCa"))
                _grid.Columns["MaCa"]!.HeaderText = "Mã ca";
            if (_grid.Columns.Contains("MaNV"))
                _grid.Columns["MaNV"]!.HeaderText = "Mã bác sĩ";
            if (_grid.Columns.Contains("HoTen"))
                _grid.Columns["HoTen"]!.HeaderText = "Bác sĩ";
            if (_grid.Columns.Contains("NgayTrongTuan"))
                _grid.Columns["NgayTrongTuan"]!.HeaderText = "Ngày";
            if (_grid.Columns.Contains("Thu"))
                _grid.Columns["Thu"]!.Visible = false;
            foreach (string column in new[] { "GioBatDau", "GioKetThuc" })
                if (_grid.Columns.Contains(column))
                {
                    _grid.Columns[column]!.HeaderText = column == "GioBatDau" ? "Bắt đầu" : "Kết thúc";
                    _grid.Columns[column]!.DefaultCellStyle.Format = "hh\\:mm";
                }
        }

        private static DateTimePicker TimePicker(int hour)
            => new()
            {
                Width = 75,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Value = DateTime.Today.AddHours(hour)
            };

        private static Label MakeCaption(string text)
            => new()
            {
                Text = text,
                AutoSize = true,
                Padding = new Padding(0, 8, 0, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
    }
}
