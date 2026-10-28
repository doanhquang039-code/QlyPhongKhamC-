using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public sealed class FormPhanCongPhong : Form
    {
        private readonly string _maNV;
        private readonly CheckedListBox _rooms;
        private readonly string _role;
        private readonly string _specialty;

        public FormPhanCongPhong(string maNV, string tenNhanVien)
        {
            _maNV = maNV;
            _role = ClinicDAL.LayChucVuNhanVien(maNV);
            _specialty = ClinicDAL.LayChuyenKhoaNhanVien(maNV).Trim();
            Text = "Phân công khoa / phòng";
            Size = new Size(500, 460);
            MinimumSize = new Size(420, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            var title = new Label
            {
                Text = $"Chọn các phòng làm việc cho {tenNhanVien} ({maNV})",
                Dock = DockStyle.Top,
                Height = 48,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 10, 0),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold)
            };
            _rooms = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                IntegralHeight = false,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(14)
            };

            DataTable allRooms = ClinicDAL.LayDanhSachPhongKham();
            var assignedRooms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow row in ClinicDAL.LayPhongPhanCongNhanVien(maNV).Rows)
                assignedRooms.Add(row["MaPhong"].ToString() ?? "");

            foreach (DataRow row in allRooms.Rows)
            {
                string maPhong = row["MaPhong"].ToString() ?? "";
                string tenKhoa = row["TenKhoa"].ToString() ?? "";
                int index = _rooms.Items.Add(new RoomOption(maPhong, tenKhoa, row["TenPhong"].ToString() ?? maPhong));
                _rooms.SetItemChecked(index, assignedRooms.Contains(maPhong));
            }
            _rooms.ItemCheck += ValidateRoomSelection;

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 94,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            var btnSave = new Button { Text = "Lưu phân công", Size = new Size(130, 34), DialogResult = DialogResult.None };
            var btnClear = new Button
            {
                Text = "Hủy toàn bộ phân công",
                Size = new Size(170, 34),
                ForeColor = Color.Firebrick
            };
            var btnCancel = new Button { Text = "Hủy", Size = new Size(90, 34), DialogResult = DialogResult.Cancel };
            btnSave.Click += SaveAssignments;
            btnClear.Click += ClearAssignments;
            buttonPanel.Controls.AddRange(new Control[] { btnSave, btnClear, btnCancel });

            Controls.Add(_rooms);
            Controls.Add(buttonPanel);
            Controls.Add(title);
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void ValidateRoomSelection(object? sender, ItemCheckEventArgs e)
        {
            if (_role != "BacSi" || e.NewValue != CheckState.Checked ||
                _rooms.Items[e.Index] is not RoomOption selectedRoom)
                return;

            if (!string.IsNullOrWhiteSpace(_specialty) &&
                !string.Equals(_specialty, selectedRoom.Specialty, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    $"Bác sĩ đã có chuyên khoa chính '{_specialty}'. Không thể chọn phòng thuộc khoa '{selectedRoom.Specialty}'.",
                    "Chuyên khoa không khớp",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                e.NewValue = CheckState.Unchecked;
                return;
            }

            foreach (int checkedIndex in _rooms.CheckedIndices)
            {
                if (checkedIndex == e.Index || _rooms.Items[checkedIndex] is not RoomOption existingRoom)
                    continue;

                if (!string.Equals(existingRoom.Specialty, selectedRoom.Specialty, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "Mỗi bác sĩ chỉ có một chuyên khoa chính. Có thể chọn nhiều phòng, nhưng các phòng phải thuộc cùng một khoa.",
                        "Không thể chọn chuyên khoa khác",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    e.NewValue = CheckState.Unchecked;
                    return;
                }
            }
        }

        private void SaveAssignments(object? sender, EventArgs e)
        {
            try
            {
                var roomIds = new List<string>();
                foreach (RoomOption room in _rooms.CheckedItems)
                    roomIds.Add(room.Id);
                ClinicDAL.LuuPhanCongPhong(_maNV, roomIds);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không lưu được phân công", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearAssignments(object? sender, EventArgs e)
        {
            if (MessageBox.Show(
                    "Hủy toàn bộ phân công phòng của nhân viên này?",
                    "Xác nhận hủy phân công",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                if (!ClinicDAL.HuyPhanCongPhong(_maNV))
                {
                    MessageBox.Show("Nhân viên này hiện chưa có phân công phòng nào.", "Không có phân công",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không hủy được phân công", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private sealed class RoomOption
        {
            public string Id { get; }
            public string Specialty { get; }
            private string Label { get; }

            public RoomOption(string id, string specialty, string label)
            {
                Id = id;
                Specialty = specialty;
                Label = label;
            }

            public override string ToString() => Label;
        }
    }
}
