using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    internal sealed class FormThemSuaKhoaPhong : Form
    {
        private readonly TextBox _txtMa;
        private readonly TextBox _txtTen;
        private readonly ComboBox? _cboKhoa;

        public string Ma => _txtMa.Text.Trim();
        public string Ten => _txtTen.Text.Trim();
        public string MaKhoa => _cboKhoa?.SelectedValue?.ToString() ?? "";

        public FormThemSuaKhoaPhong(bool isRoom, bool isNew, DataRow? row = null)
        {
            Text = isRoom
                ? (isNew ? "Thêm phòng khám" : "Sửa phòng khám")
                : (isNew ? "Thêm chuyên khoa" : "Sửa chuyên khoa");
            Size = new Size(440, isRoom ? 250 : 210);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            _txtMa = new TextBox
            {
                Location = new Point(145, 24),
                Size = new Size(250, 28),
                Text = row?[isRoom ? "MaPhong" : "MaKhoa"].ToString() ?? "",
                ReadOnly = !isNew,
                BackColor = isNew ? Color.White : Color.FromArgb(245, 245, 245)
            };
            _txtTen = new TextBox
            {
                Location = new Point(145, 64),
                Size = new Size(250, 28),
                Text = row?[isRoom ? "TenPhong" : "TenKhoa"].ToString() ?? ""
            };
            Controls.AddRange(new Control[]
            {
                new Label { Text = isRoom ? "Mã phòng:" : "Mã khoa:", Location = new Point(25, 28), AutoSize = true },
                _txtMa,
                new Label { Text = isRoom ? "Tên phòng:" : "Tên chuyên khoa:", Location = new Point(25, 68), AutoSize = true },
                _txtTen
            });

            if (isRoom)
            {
                _cboKhoa = new ComboBox
                {
                    Location = new Point(145, 104),
                    Size = new Size(250, 28),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DataSource = ClinicDAL.LayDanhSachKhoa(),
                    DisplayMember = "TenKhoa",
                    ValueMember = "MaKhoa"
                };
                if (row != null)
                    _cboKhoa.SelectedValue = row["MaKhoa"].ToString() ?? "";
                Controls.AddRange(new Control[]
                {
                    new Label { Text = "Chuyên khoa:", Location = new Point(25, 108), AutoSize = true },
                    _cboKhoa
                });
            }

            var btnSave = new Button
            {
                Text = "Lưu",
                Location = new Point(215, isRoom ? 155 : 115),
                Size = new Size(85, 34),
                DialogResult = DialogResult.None
            };
            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(310, isRoom ? 155 : 115),
                Size = new Size(85, 34),
                DialogResult = DialogResult.Cancel
            };
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(Ma) || string.IsNullOrWhiteSpace(Ten) ||
                    (isRoom && string.IsNullOrWhiteSpace(MaKhoa)))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin.", "Thiếu thông tin",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.AddRange(new Control[] { btnSave, btnCancel });
            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }
    }
}
