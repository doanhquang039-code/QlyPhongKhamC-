using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using QlyPhongKham.DAL;
using QlyPhongKham.Helpers;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public class FormThemSuaBN : Form
    {
        private readonly BenhNhan? _editing;
        private byte[]? _profileImage;

        public FormThemSuaBN(BenhNhan? bn)
        {
            _editing = bn;
            _profileImage = bn?.AnhDaiDien;
            string patientCode = bn?.MaBN ?? ClinicDAL.SinhMaBenhNhan();
            Text = bn == null ? "Thêm bệnh nhân mới" : "Sửa hồ sơ bệnh nhân";
            ClientSize = new Size(780, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(244, 247, 250);
            Font = new Font("Segoe UI", 9.5F);
            FormEffects.FadeIn(this);

            var header = new Panel { Location = Point.Empty, Size = new Size(780, 78), BackColor = Color.FromArgb(15, 50, 76) };
            header.Controls.Add(new Label
            {
                Text = bn == null ? "TẠO HỒ SƠ BỆNH NHÂN" : "CẬP NHẬT HỒ SƠ BỆNH NHÂN",
                Location = new Point(24, 13),
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White
            });
            header.Controls.Add(new Label
            {
                Text = "Thông tin được lưu bảo mật trong cơ sở dữ liệu phòng khám",
                Location = new Point(26, 47),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(187, 211, 224)
            });

            var photoCard = CreateCard(new Point(20, 96), new Size(205, 411));
            photoCard.Controls.Add(new Label
            {
                Text = "ẢNH ĐẠI DIỆN",
                Location = new Point(18, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(83, 105, 121)
            });
            var picture = new PictureBox
            {
                Location = new Point(28, 52),
                Size = new Size(149, 149),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(237, 243, 247),
                BorderStyle = BorderStyle.FixedSingle
            };
            var placeholder = new Label
            {
                Text = "👤",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Emoji", 38F),
                ForeColor = Color.FromArgb(155, 174, 187),
                BackColor = Color.FromArgb(237, 243, 247)
            };
            picture.Controls.Add(placeholder);

            var btnChooseImage = CreateButton("Chọn ảnh", Color.FromArgb(0, 145, 134), Color.FromArgb(0, 119, 111));
            btnChooseImage.Location = new Point(18, 220);
            btnChooseImage.Size = new Size(169, 36);
            var btnRemoveImage = CreateButton("Bỏ ảnh", Color.FromArgb(235, 241, 245), Color.FromArgb(221, 231, 237));
            btnRemoveImage.ForeColor = Color.FromArgb(58, 79, 94);
            btnRemoveImage.Location = new Point(18, 265);
            btnRemoveImage.Size = new Size(169, 34);
            var imageHint = new Label
            {
                Text = "Ảnh tự tối ưu dung lượng\nTối đa 10 MB",
                Location = new Point(18, 316),
                Size = new Size(169, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(119, 137, 150),
                Font = new Font("Segoe UI", 8.5F)
            };
            var recordCode = new Label
            {
                Text = $"MÃ HỒ SƠ   {patientCode}",
                Location = new Point(12, 370),
                Size = new Size(181, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0, 119, 111),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                AutoEllipsis = true
            };
            photoCard.Controls.AddRange(new Control[] { picture, btnChooseImage, btnRemoveImage, imageHint, recordCode });

            var detailsCard = CreateCard(new Point(239, 96), new Size(521, 411));
            detailsCard.Controls.Add(new Label
            {
                Text = "THÔNG TIN CÁ NHÂN",
                Location = new Point(20, 17),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(83, 105, 121)
            });

            var txtName = CreateInput(new Point(20, 62), new Size(481, 30));
            AddField(detailsCard, "Họ và tên *", txtName, 20, 42);
            var cboGender = new ComboBox
            {
                Location = new Point(20, 126),
                Size = new Size(210, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cboGender.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGender.SelectedItem = bn?.GioiTinh ?? "Nam";
            AddField(detailsCard, "Giới tính", cboGender, 20, 106);

            var dateOfBirth = new DateTimePicker
            {
                Location = new Point(271, 126),
                Size = new Size(230, 30),
                Format = DateTimePickerFormat.Short,
                Value = bn?.NgaySinh ?? DateTime.Today.AddYears(-25),
                Font = new Font("Segoe UI", 9.5F)
            };
            AddField(detailsCard, "Ngày sinh", dateOfBirth, 271, 106);

            var txtPhone = CreateInput(new Point(20, 190), new Size(481, 30));
            AddField(detailsCard, "Số điện thoại", txtPhone, 20, 170);
            var txtAddress = CreateInput(new Point(20, 254), new Size(481, 30));
            AddField(detailsCard, "Địa chỉ", txtAddress, 20, 234);
            var txtHistory = new TextBox
            {
                Location = new Point(20, 318),
                Size = new Size(481, 72),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5F)
            };
            AddField(detailsCard, "Tiền sử bệnh", txtHistory, 20, 298);
            txtName.Text = bn?.HoTen ?? "";
            txtPhone.Text = bn?.SoDienThoai ?? "";
            txtAddress.Text = bn?.DiaChi ?? "";
            txtHistory.Text = bn?.TienSuBenh ?? "";

            var btnSave = CreateButton("Lưu hồ sơ", Color.FromArgb(0, 145, 134), Color.FromArgb(0, 119, 111));
            btnSave.Location = new Point(516, 530);
            btnSave.Size = new Size(135, 40);
            btnSave.DialogResult = DialogResult.None;
            btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            var btnCancel = CreateButton("Hủy", Color.FromArgb(229, 236, 241), Color.FromArgb(214, 224, 231));
            btnCancel.ForeColor = Color.FromArgb(48, 69, 84);
            btnCancel.Location = new Point(665, 530);
            btnCancel.Size = new Size(95, 40);
            btnCancel.DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { header, photoCard, detailsCard, btnSave, btnCancel });
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            SetPreview();
            btnChooseImage.Click += (s, e) =>
            {
                using var dialog = new OpenFileDialog
                {
                    Title = "Chọn ảnh đại diện bệnh nhân",
                    Filter = "Tệp ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                    CheckFileExists = true
                };
                if (dialog.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    _profileImage = ProfileImageHelper.EncodeForStorage(dialog.FileName);
                    SetPreview();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException or ExternalException or InvalidOperationException)
                {
                    MessageBox.Show(this, $"Không thể dùng ảnh này.\n\n{ex.Message}", "Ảnh không hợp lệ",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            btnRemoveImage.Click += (s, e) =>
            {
                _profileImage = null;
                SetPreview();
            };
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show(this, "Vui lòng nhập họ tên bệnh nhân.", "Thiếu thông tin",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                var patient = new BenhNhan
                {
                    MaBN = patientCode,
                    HoTen = txtName.Text.Trim(),
                    GioiTinh = cboGender.SelectedItem?.ToString() ?? "Nam",
                    NgaySinh = dateOfBirth.Value.Date,
                    SoDienThoai = txtPhone.Text.Trim(),
                    DiaChi = txtAddress.Text.Trim(),
                    TienSuBenh = txtHistory.Text.Trim(),
                    AnhDaiDien = _profileImage
                };
                try
                {
                    ClinicDAL.LuuBenhNhan(patient, _editing == null);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex) when (ex is SqlException or InvalidOperationException or ArgumentException)
                {
                    MessageBox.Show(this, ex.Message, "Không thể lưu hồ sơ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            FormClosed += (s, e) => picture.Image?.Dispose();

            void SetPreview()
            {
                Image? previous = picture.Image;
                picture.Image = ProfileImageHelper.CreatePreview(_profileImage);
                previous?.Dispose();
                placeholder.Visible = picture.Image is null;
            }
        }

        private static Panel CreateCard(Point location, Size size) =>
            new()
            {
                Location = location,
                Size = size,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

        private static TextBox CreateInput(Point location, Size size) =>
            new()
            {
                Location = location,
                Size = size,
                Font = new Font("Segoe UI", 9.5F)
            };

        private static ModernButton CreateButton(string text, Color backColor, Color hoverColor) =>
            new()
            {
                Text = text,
                BackColor = backColor,
                HoverColor = hoverColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

        private static void AddField(Control parent, string caption, Control input, int x, int labelY)
        {
            parent.Controls.Add(new Label
            {
                Text = caption,
                Location = new Point(x, labelY),
                AutoSize = true,
                ForeColor = Color.FromArgb(66, 87, 102),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            });
            parent.Controls.Add(input);
        }
    }
}
