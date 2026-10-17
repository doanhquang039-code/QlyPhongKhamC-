using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public class FormThemSuaThuoc : Form
    {
        private readonly Thuoc? _editing;

        public FormThemSuaThuoc(Thuoc? t)
        {
            _editing = t;
            Text = t == null ? "Thêm thuốc mới" : "Sửa thông tin thuốc";
            Size = new Size(500, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            var txtMa = new TextBox { Location = new Point(140, 20), Size = new Size(130, 26), ReadOnly = t != null, BackColor = t != null ? Color.FromArgb(245, 245, 245) : Color.White, Text = t?.MaThuoc ?? ("TH" + DateTime.Now.ToString("mmss")) };
            var txtTen = new TextBox { Location = new Point(140, 55), Size = new Size(310, 26), Text = t?.TenThuoc ?? "" };
            var cboDVT = new ComboBox { Location = new Point(140, 90), Size = new Size(130, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboDVT.Items.AddRange(new object[] { "Viên", "Vỉ", "Hộp", "Gói", "Chai", "Tuýp", "Ống" });
            cboDVT.SelectedItem = t?.DonViTinh ?? "Viên";
            var numDonGia = new NumericUpDown { Location = new Point(140, 125), Size = new Size(150, 26), Maximum = 10000000, Increment = 1000, Value = (decimal)(t?.DonGiaBan ?? 5000), ThousandsSeparator = true };
            var numTon = new NumericUpDown { Location = new Point(140, 160), Size = new Size(130, 26), Maximum = 999999, Value = t?.SoLuongTon ?? 100 };
            var txtCachDung = new TextBox { Location = new Point(140, 195), Size = new Size(310, 26), Text = t?.CachDungMacDinh ?? "" };

            Controls.AddRange(new Control[] {
                new Label { Text = "Mã thuốc:", Location = new Point(20, 23), AutoSize = true }, txtMa,
                new Label { Text = "Tên thuốc (*):", Location = new Point(20, 58), AutoSize = true }, txtTen,
                new Label { Text = "Đơn vị tính:", Location = new Point(20, 93), AutoSize = true }, cboDVT,
                new Label { Text = "Đơn giá bán:", Location = new Point(20, 128), AutoSize = true }, numDonGia,
                new Label { Text = "Số lượng tồn:", Location = new Point(20, 163), AutoSize = true }, numTon,
                new Label { Text = "Cách dùng:", Location = new Point(20, 198), AutoSize = true }, txtCachDung
            });

            var btnSave = new ModernButton { Text = "💾 Lưu thuốc", Location = new Point(140, 245), Size = new Size(140, 36), BackColor = Color.FromArgb(0, 180, 166), HoverColor = Color.FromArgb(0, 145, 133), ForeColor = Color.White, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(295, 245), Size = new Size(90, 36), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTen.Text))
                {
                    MessageBox.Show("Nhập tên thuốc!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var thuoc = new Thuoc
                {
                    MaThuoc        = txtMa.Text.Trim(),
                    TenThuoc       = txtTen.Text.Trim(),
                    DonViTinh      = cboDVT.SelectedItem?.ToString() ?? "Viên",
                    DonGiaBan      = numDonGia.Value,
                    SoLuongTon     = (int)numTon.Value,
                    CachDungMacDinh= txtCachDung.Text.Trim()
                };
                ClinicDAL.LuuThuoc(thuoc, _editing == null);
                DialogResult = DialogResult.OK;
                Close();
            };
        }
    }
}
