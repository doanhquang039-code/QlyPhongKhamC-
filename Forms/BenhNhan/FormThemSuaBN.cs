using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public class FormThemSuaBN : Form
    {
        private readonly BenhNhan? _editing;

        public FormThemSuaBN(BenhNhan? bn)
        {
            _editing = bn;
            Text = bn == null ? "Thêm bệnh nhân mới" : "Sửa hồ sơ bệnh nhân";
            Size = new Size(540, 390);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            var txtMaBN  = new TextBox { Location = new Point(140, 20), Size = new Size(130, 26), ReadOnly = bn != null, BackColor = bn != null ? Color.FromArgb(245, 245, 245) : Color.White, Text = bn?.MaBN ?? ClinicDAL.SinhMaBenhNhan() };
            var txtHoTen = new TextBox { Location = new Point(140, 55), Size = new Size(350, 26), Text = bn?.HoTen ?? "" };
            var cboGT    = new ComboBox { Location = new Point(140, 90), Size = new Size(110, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboGT.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGT.SelectedItem = bn?.GioiTinh ?? "Nam";
            var dtpNS    = new DateTimePicker { Location = new Point(140, 125), Size = new Size(150, 26), Format = DateTimePickerFormat.Short, Value = bn?.NgaySinh ?? DateTime.Today.AddYears(-25) };
            var txtSDT   = new TextBox { Location = new Point(140, 160), Size = new Size(190, 26), Text = bn?.SoDienThoai ?? "" };
            var txtDC    = new TextBox { Location = new Point(140, 195), Size = new Size(350, 26), Text = bn?.DiaChi ?? "" };
            var txtTS    = new TextBox { Location = new Point(140, 230), Size = new Size(350, 26), Text = bn?.TienSuBenh ?? "" };

            Controls.AddRange(new Control[] {
                new Label { Text = "Mã BN:", Location = new Point(20, 23), AutoSize = true }, txtMaBN,
                new Label { Text = "Họ tên (*):", Location = new Point(20, 58), AutoSize = true }, txtHoTen,
                new Label { Text = "Giới tính:", Location = new Point(20, 93), AutoSize = true }, cboGT,
                new Label { Text = "Ngày sinh:", Location = new Point(20, 128), AutoSize = true }, dtpNS,
                new Label { Text = "Điện thoại:", Location = new Point(20, 163), AutoSize = true }, txtSDT,
                new Label { Text = "Địa chỉ:", Location = new Point(20, 198), AutoSize = true }, txtDC,
                new Label { Text = "Tiền sử bệnh:", Location = new Point(20, 233), AutoSize = true }, txtTS
            });

            var btnSave   = new ModernButton { Text = "💾 Lưu", Location = new Point(160, 280), Size = new Size(130, 36), BackColor = Color.FromArgb(0, 180, 166), HoverColor = Color.FromArgb(0, 145, 133), ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(305, 280), Size = new Size(90, 36), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtHoTen.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên bệnh nhân!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var newBN = new BenhNhan
                {
                    MaBN        = txtMaBN.Text.Trim(),
                    HoTen       = txtHoTen.Text.Trim(),
                    GioiTinh    = cboGT.SelectedItem?.ToString() ?? "Nam",
                    NgaySinh    = dtpNS.Value,
                    SoDienThoai = txtSDT.Text.Trim(),
                    DiaChi      = txtDC.Text.Trim(),
                    TienSuBenh  = txtTS.Text.Trim()
                };
                ClinicDAL.LuuBenhNhan(newBN, _editing == null);
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}
