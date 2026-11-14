using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public sealed class FormThemSuaDichVu : Form
    {
        private readonly DichVu? _editing;

        public FormThemSuaDichVu(DichVu? dichVu)
        {
            _editing = dichVu;
            Text = dichVu == null ? "Thêm dịch vụ" : "Sửa dịch vụ";
            ClientSize = new Size(520, 350);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            var title = new Label
            {
                Text = dichVu == null ? "THÊM DỊCH VỤ MỚI" : "CẬP NHẬT DỊCH VỤ",
                Location = new Point(22, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 50, 76)
            };
            var lblCode = new Label { Text = "Mã dịch vụ", Location = new Point(24, 66), AutoSize = true };
            var txtCode = new TextBox
            {
                Location = new Point(160, 62),
                Size = new Size(330, 28),
                Text = dichVu?.MaDV ?? ClinicDAL.SinhMaDichVu(),
                ReadOnly = dichVu != null,
                BackColor = dichVu != null ? Color.FromArgb(240, 244, 247) : Color.White
            };
            var lblName = new Label { Text = "Tên dịch vụ *", Location = new Point(24, 108), AutoSize = true };
            var txtName = new TextBox
            {
                Location = new Point(160, 104),
                Size = new Size(330, 28),
                Text = dichVu?.TenDV ?? ""
            };
            var lblPrice = new Label { Text = "Đơn giá (VNĐ) *", Location = new Point(24, 150), AutoSize = true };
            var numPrice = new NumericUpDown
            {
                Location = new Point(160, 146),
                Size = new Size(200, 28),
                Maximum = 1_000_000_000,
                Minimum = 0,
                Increment = 1_000,
                ThousandsSeparator = true,
                DecimalPlaces = 0,
                Value = dichVu?.DonGia ?? 0
            };
            var lblDescription = new Label { Text = "Mô tả / lưu ý", Location = new Point(24, 192), AutoSize = true };
            var txtDescription = new TextBox
            {
                Location = new Point(160, 188),
                Size = new Size(330, 72),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = dichVu?.MoTa ?? ""
            };

            var btnSave = new Button
            {
                Text = "Lưu dịch vụ",
                Location = new Point(276, 288),
                Size = new Size(120, 36),
                BackColor = Color.FromArgb(0, 145, 134),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(406, 288),
                Size = new Size(84, 36),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat
            };

            Controls.AddRange(new Control[]
            {
                title, lblCode, txtCode, lblName, txtName, lblPrice, numPrice,
                lblDescription, txtDescription, btnSave, btnCancel
            });
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            btnSave.Click += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show(this, "Vui lòng nhập tên dịch vụ.", "Thiếu thông tin",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                if (numPrice.Value <= 0)
                {
                    MessageBox.Show(this, "Đơn giá dịch vụ phải lớn hơn 0.", "Đơn giá không hợp lệ",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numPrice.Focus();
                    return;
                }

                try
                {
                    var service = new DichVu
                    {
                        MaDV = txtCode.Text.Trim(),
                        TenDV = txtName.Text.Trim(),
                        DonGia = numPrice.Value,
                        MoTa = txtDescription.Text.Trim()
                    };
                    if (!ClinicDAL.LuuDichVu(service, _editing == null))
                        throw new InvalidOperationException("Không tìm thấy dịch vụ cần cập nhật.");

                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception exception)
                {
                    MessageBox.Show(this, exception.Message, "Không thể lưu dịch vụ",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }
    }
}
