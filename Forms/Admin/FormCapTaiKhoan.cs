using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public class FormCapTaiKhoan : Form
    {
        public FormCapTaiKhoan(string maNV, string tenDangNhapCu, string chucVu, bool isNew)
        {
            Text = isNew ? "Cấp tài khoản mới" : "Đặt lại mật khẩu";
            Size = new Size(430, 280);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            var txtMaNV = new TextBox { Location = new Point(150, 20), Size = new Size(220, 26), ReadOnly = true, BackColor = Color.FromArgb(245, 245, 245), Text = maNV };
            var txtTDN  = new TextBox { Location = new Point(150, 55), Size = new Size(220, 26), ReadOnly = !isNew, BackColor = isNew ? Color.White : Color.FromArgb(245, 245, 245), Text = tenDangNhapCu };
            var txtMK   = new TextBox { Location = new Point(150, 90), Size = new Size(220, 26), UseSystemPasswordChar = true };
            var txtMK2  = new TextBox { Location = new Point(150, 125), Size = new Size(220, 26), UseSystemPasswordChar = true };
            var cboQ    = new ComboBox { Location = new Point(150, 160), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboQ.Items.AddRange(new object[] { "Admin", "BacSi", "LeTan" });
            cboQ.SelectedItem = chucVu;

            Controls.AddRange(new Control[] {
                new Label { Text = "Mã nhân viên:", Location = new Point(20, 23), AutoSize = true }, txtMaNV,
                new Label { Text = "Tên đăng nhập:", Location = new Point(20, 58), AutoSize = true }, txtTDN,
                new Label { Text = "Mật khẩu mới:", Location = new Point(20, 93), AutoSize = true }, txtMK,
                new Label { Text = "Xác nhận MK:", Location = new Point(20, 128), AutoSize = true }, txtMK2,
                new Label { Text = "Phân quyền:", Location = new Point(20, 163), AutoSize = true }, cboQ
            });

            var btnSave = new ModernButton { Text = "💾 Xác nhận", Location = new Point(120, 202), Size = new Size(130, 36), BackColor = Color.FromArgb(0, 180, 166), HoverColor = Color.FromArgb(0, 145, 133), ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(265, 202), Size = new Size(90, 36), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTDN.Text))
                {
                    MessageBox.Show("Nhập tên đăng nhập!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (txtMK.Text != txtMK2.Text)
                {
                    MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (txtMK.Text.Length < 4)
                {
                    MessageBox.Show("Mật khẩu phải ít nhất 4 ký tự!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ClinicDAL.LuuTaiKhoan(maNV, txtTDN.Text.Trim(), txtMK.Text, cboQ.SelectedItem?.ToString() ?? "LeTan", isNew);
                MessageBox.Show("✅ Đã lưu tài khoản thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}
