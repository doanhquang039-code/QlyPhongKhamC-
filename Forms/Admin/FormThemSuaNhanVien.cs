using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public class FormThemSuaNhanVien : Form
    {
        private readonly NhanVien? _editing;

        public FormThemSuaNhanVien(NhanVien? nv)
        {
            _editing = nv;
            Text = nv == null ? "Thêm nhân viên mới" : "Sửa thông tin nhân viên";
            Size = new Size(500, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            var txtMa = new TextBox { Location = new Point(140, 20), Size = new Size(120, 26), ReadOnly = nv != null, BackColor = nv != null ? Color.FromArgb(245, 245, 245) : Color.White, Text = nv?.MaNV ?? ClinicDAL.SinhMaNhanVien() };
            var txtHoTen = new TextBox { Location = new Point(140, 55), Size = new Size(310, 26), Text = nv?.HoTen ?? "" };
            var cboCV = new ComboBox { Location = new Point(140, 90), Size = new Size(150, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboCV.Items.AddRange(new object[] { "Admin", "BacSi", "LeTan" });
            cboCV.SelectedItem = nv?.ChucVu ?? "LeTan";
            var txtCK = new TextBox { Location = new Point(140, 125), Size = new Size(260, 26), Text = nv?.ChuyenKhoa ?? "" };
            var txtSDT = new TextBox { Location = new Point(140, 160), Size = new Size(190, 26), Text = nv?.SoDienThoai ?? "" };
            var txtEmail = new TextBox { Location = new Point(140, 195), Size = new Size(290, 26), Text = nv?.Email ?? "" };

            Controls.AddRange(new Control[] {
                new Label { Text = "Mã nhân viên:", Location = new Point(20, 23), AutoSize = true }, txtMa,
                new Label { Text = "Họ tên (*):", Location = new Point(20, 58), AutoSize = true }, txtHoTen,
                new Label { Text = "Chức vụ:", Location = new Point(20, 93), AutoSize = true }, cboCV,
                new Label { Text = "Chuyên khoa:", Location = new Point(20, 128), AutoSize = true }, txtCK,
                new Label { Text = "Điện thoại:", Location = new Point(20, 163), AutoSize = true }, txtSDT,
                new Label { Text = "Email:", Location = new Point(20, 198), AutoSize = true }, txtEmail
            });

            var btnSave = new ModernButton { Text = "💾 Lưu", Location = new Point(140, 245), Size = new Size(130, 36), BackColor = Color.FromArgb(0, 180, 166), HoverColor = Color.FromArgb(0, 145, 133), ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(285, 245), Size = new Size(90, 36), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtHoTen.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên nhân viên!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var newNV = new NhanVien
                {
                    MaNV = txtMa.Text.Trim(),
                    HoTen = txtHoTen.Text.Trim(),
                    ChucVu = cboCV.SelectedItem?.ToString() ?? "LeTan",
                    ChuyenKhoa = txtCK.Text.Trim(),
                    SoDienThoai = txtSDT.Text.Trim(),
                    Email = txtEmail.Text.Trim()
                };
                ClinicDAL.LuuNhanVien(newNV, _editing == null);
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}
