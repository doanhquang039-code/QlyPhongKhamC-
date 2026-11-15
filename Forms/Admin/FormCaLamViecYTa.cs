using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    internal sealed class FormCaLamViecYTa : Form
    {
        private readonly ComboBox _cboNurse;
        private readonly ComboBox _cboDay;
        private readonly DateTimePicker _dtpStart;
        private readonly DateTimePicker _dtpEnd;
        private readonly DataGridView _grid;

        public FormCaLamViecYTa()
        {
            Text = UiText.Translate("Quản lý lịch trực y tá");
            Size = new Size(920, 600);
            MinimumSize = new Size(760, 480);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(243, 246, 248);

            var header = new Label
            {
                Text = UiText.Translate("QUẢN LÝ LỊCH TRỰC Y TÁ"),
                Dock = DockStyle.Top,
                Height = 52,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(17, 42, 63),
                BackColor = Color.White
            };

            var toolbar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                Padding = new Padding(12, 10, 8, 4),
                WrapContents = true,
                BackColor = Color.White
            };
            _cboNurse = new ComboBox { Width = 210, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboDay = new ComboBox { Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboDay.Items.AddRange(new object[]
            {
                UiText.Translate("Thứ 2"), UiText.Translate("Thứ 3"), UiText.Translate("Thứ 4"),
                UiText.Translate("Thứ 5"), UiText.Translate("Thứ 6"), UiText.Translate("Thứ 7"),
                UiText.Translate("Chủ nhật")
            });
            _cboDay.SelectedIndex = 0;
            _dtpStart = CreateTimePicker(8);
            _dtpEnd = CreateTimePicker(17);
            var addButton = CreateButton(UiText.Translate("＋ Thêm ca"), Color.FromArgb(0, 145, 134));
            var deleteButton = CreateButton(UiText.Translate("Xóa ca"), Color.FromArgb(220, 38, 38));
            var refreshButton = CreateButton(UiText.Translate("Làm mới"), Color.FromArgb(59, 130, 246));
            toolbar.Controls.AddRange(new Control[]
            {
                Caption(UiText.Translate("Y tá:")), _cboNurse,
                Caption(UiText.Translate("Thứ:")), _cboDay,
                Caption(UiText.Translate("Từ:")), _dtpStart,
                Caption(UiText.Translate("Đến:")), _dtpEnd,
                addButton, deleteButton, refreshButton
            });

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false
            };
            _grid.DataBindingComplete += (s, e) =>
            {
                SetHeader("MaCa", UiText.Translate("Mã ca"), false);
                SetHeader("MaNV", UiText.Translate("Mã nhân viên"), false);
                SetHeader("HoTen", UiText.Translate("Y tá"));
                SetHeader("NgayTrongTuan", UiText.Translate("Thứ"));
                SetHeader("GioBatDau", UiText.Translate("Bắt đầu"), true);
                SetHeader("GioKetThuc", UiText.Translate("Kết thúc"), true);
                DataGridViewColumn? startColumn = _grid.Columns["GioBatDau"];
                DataGridViewColumn? endColumn = _grid.Columns["GioKetThuc"];
                if (startColumn != null)
                    startColumn.DefaultCellStyle.Format = @"hh\:mm";
                if (endColumn != null)
                    endColumn.DefaultCellStyle.Format = @"hh\:mm";
            };

            Controls.Add(_grid);
            Controls.Add(toolbar);
            Controls.Add(header);
            addButton.Click += (s, e) => AddShift();
            deleteButton.Click += (s, e) => DeleteShift();
            refreshButton.Click += (s, e) => LoadShifts();
            LoadNurses();
            LoadShifts();
        }

        private void LoadNurses()
        {
            try
            {
                DataTable nurses = ClinicDAL.LayDanhSachYTaChoPhanCa();
                _cboNurse.DataSource = nurses;
                _cboNurse.DisplayMember = "HoTen";
                _cboNurse.ValueMember = "MaNV";
                _cboNurse.SelectedIndex = nurses.Rows.Count == 0 ? -1 : 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể tải danh sách y tá",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadShifts()
        {
            try
            {
                DataTable shifts = ClinicDAL.LayCaLamViecYTa();
                string[] days =
                {
                    UiText.Translate("Thứ 2"), UiText.Translate("Thứ 3"), UiText.Translate("Thứ 4"),
                    UiText.Translate("Thứ 5"), UiText.Translate("Thứ 6"), UiText.Translate("Thứ 7"),
                    UiText.Translate("Chủ nhật")
                };
                foreach (DataRow shift in shifts.Rows)
                    shift["NgayTrongTuan"] = days[Convert.ToInt32(shift["Thu"]) - 1];
                _grid.DataSource = shifts;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể tải lịch trực y tá",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddShift()
        {
            if (_cboNurse.SelectedValue == null)
            {
                MessageBox.Show(this, "Chưa có y tá đang hoạt động để phân ca.", "Thiếu dữ liệu",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ClinicDAL.LuuCaLamViecYTa(_cboNurse.SelectedValue.ToString() ?? "",
                    _cboDay.SelectedIndex + 1, _dtpStart.Value.TimeOfDay, _dtpEnd.Value.TimeOfDay);
                LoadShifts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể thêm ca trực",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteShift()
        {
            if (_grid.SelectedRows.Count == 0 ||
                !int.TryParse(_grid.SelectedRows[0].Cells["MaCa"]?.Value?.ToString(), out int shiftId))
            {
                MessageBox.Show(this, "Chọn ca trực cần xóa.", "Chưa chọn ca",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(this, "Xóa ca trực y tá đã chọn?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                ClinicDAL.XoaCaLamViecYTa(shiftId);
                LoadShifts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Không thể xóa ca trực",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetHeader(string column, string text, bool visible = true)
        {
            DataGridViewColumn? gridColumn = _grid.Columns[column];
            if (gridColumn == null) return;
            gridColumn.HeaderText = text;
            gridColumn.Visible = visible;
        }

        private static Label Caption(string text)
            => new Label { Text = text, AutoSize = true, Padding = new Padding(0, 7, 0, 0) };

        private static DateTimePicker CreateTimePicker(int hour)
            => new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Width = 76,
                Value = DateTime.Today.AddHours(hour)
            };

        private static Button CreateButton(string text, Color color)
            => new Button
            {
                Text = text,
                AutoSize = true,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0)
            };
    }
}
