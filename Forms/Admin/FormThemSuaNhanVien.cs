using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using QlyPhongKham.DAL;
using QlyPhongKham.Helpers;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public class FormThemSuaNhanVien : Form
    {
        private readonly NhanVien? _editing;
        private byte[]? _profileImage;

        public FormThemSuaNhanVien(NhanVien? nv)
        {
            _editing = nv;
            _profileImage = nv?.AnhDaiDien;
            Text = nv == null ? "Thêm nhân viên mới" : "Sửa thông tin nhân viên";
            ClientSize = new Size(780, 550);
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
                Text = nv == null ? "HỒ SƠ NHÂN VIÊN MỚI" : "CẬP NHẬT NHÂN VIÊN",
                Location = new Point(24, 13),
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White
            });
            header.Controls.Add(new Label
            {
                Text = "Quản lý thông tin nhân sự và ảnh đại diện",
                Location = new Point(26, 47),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(187, 211, 224)
            });

            var photoCard = CreateCard(new Point(20, 96), new Size(205, 410));
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
            photoCard.Controls.AddRange(new Control[] { picture, btnChooseImage, btnRemoveImage });
            photoCard.Controls.Add(new Label
            {
                Text = "Ảnh tự tối ưu dung lượng\nTối đa 10 MB",
                Location = new Point(18, 316),
                Size = new Size(169, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(119, 137, 150),
                Font = new Font("Segoe UI", 8.5F)
            });
            photoCard.Controls.Add(new Label
            {
                Text = nv?.MaNV ?? ClinicDAL.SinhMaNhanVien(),
                Location = new Point(12, 370),
                Size = new Size(181, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0, 119, 111),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            });

            var detailsCard = CreateCard(new Point(239, 96), new Size(521, 410));
            detailsCard.Controls.Add(new Label
            {
                Text = "THÔNG TIN NHÂN SỰ",
                Location = new Point(20, 17),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(83, 105, 121)
            });

            var txtCode = CreateInput(new Point(20, 62), new Size(210, 30));
            txtCode.Text = nv?.MaNV ?? ClinicDAL.SinhMaNhanVien();
            txtCode.ReadOnly = nv != null;
            if (nv != null)
                txtCode.BackColor = Color.FromArgb(240, 244, 247);
            AddField(detailsCard, "Mã nhân viên", txtCode, 20, 42);

            var cboRole = new ComboBox
            {
                Location = new Point(271, 62),
                Size = new Size(230, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cboRole.Items.AddRange(new object[] { "Admin", "BacSi", "LeTan" });
            cboRole.SelectedItem = nv?.ChucVu ?? "LeTan";
            AddField(detailsCard, "Chức vụ", cboRole, 271, 42);

            var txtName = CreateInput(new Point(20, 126), new Size(481, 30));
            txtName.Text = nv?.HoTen ?? "";
            AddField(detailsCard, "Họ và tên *", txtName, 20, 106);

            var departments = ClinicDAL.LayDanhSachKhoa();
            var cboSpecialty = new ComboBox
            {
                Location = new Point(20, 190),
                Size = new Size(481, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };
            cboSpecialty.Items.Add("(Chưa chọn)");
            foreach (DataRow department in departments.Rows)
            {
                string? specialtyName = department["TenKhoa"]?.ToString();
                if (!string.IsNullOrWhiteSpace(specialtyName))
                    cboSpecialty.Items.Add(specialtyName);
            }
            if (!string.IsNullOrWhiteSpace(nv?.ChuyenKhoa) &&
                !cboSpecialty.Items.Cast<string>().Any(name =>
                    string.Equals(name, nv.ChuyenKhoa, StringComparison.OrdinalIgnoreCase)))
            {
                cboSpecialty.Items.Add(nv.ChuyenKhoa);
            }
            cboSpecialty.SelectedIndex = 0;
            if (!string.IsNullOrWhiteSpace(nv?.ChuyenKhoa))
            {
                int specialtyIndex = cboSpecialty.Items.Cast<string>()
                    .ToList()
                    .FindIndex(name => string.Equals(name, nv.ChuyenKhoa, StringComparison.OrdinalIgnoreCase));
                if (specialtyIndex >= 0)
                    cboSpecialty.SelectedIndex = specialtyIndex;
            }
            cboSpecialty.Enabled = cboRole.SelectedItem?.ToString() == "BacSi";
            AddField(detailsCard, "Chuyên khoa chính", cboSpecialty, 20, 170);

            var txtPhone = CreateInput(new Point(20, 254), new Size(210, 30));
            txtPhone.Text = nv?.SoDienThoai ?? "";
            AddField(detailsCard, "Điện thoại", txtPhone, 20, 234);
            var txtEmail = CreateInput(new Point(271, 254), new Size(230, 30));
            txtEmail.Text = nv?.Email ?? "";
            AddField(detailsCard, "Email", txtEmail, 271, 234);

            string originalSpecialty = nv?.ChuyenKhoa?.Trim() ?? "";
            int originalSpecialtyIndex = cboSpecialty.SelectedIndex;
            cboRole.SelectedIndexChanged += (s, e) =>
            {
                bool isDoctor = cboRole.SelectedItem?.ToString() == "BacSi";
                cboSpecialty.Enabled = isDoctor;
                if (!isDoctor)
                    cboSpecialty.SelectedIndex = 0;
                else if (nv?.ChucVu == "BacSi" && !string.IsNullOrWhiteSpace(originalSpecialty))
                    cboSpecialty.SelectedIndex = originalSpecialtyIndex;
            };
            cboSpecialty.SelectedIndexChanged += (s, e) =>
            {
                if (cboRole.SelectedItem?.ToString() != "BacSi" ||
                    nv?.ChucVu != "BacSi" || string.IsNullOrWhiteSpace(originalSpecialty) ||
                    string.Equals(cboSpecialty.SelectedItem?.ToString(), originalSpecialty, StringComparison.OrdinalIgnoreCase))
                    return;

                MessageBox.Show(this,
                    $"Bác sĩ đã có chuyên khoa chính '{originalSpecialty}' và không thể chọn chuyên khoa khác.",
                    "Không thể đổi chuyên khoa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboSpecialty.SelectedIndex = originalSpecialtyIndex;
            };

            var btnSave = CreateButton("Lưu nhân viên", Color.FromArgb(0, 145, 134), Color.FromArgb(0, 119, 111));
            btnSave.Location = new Point(500, 520);
            btnSave.Size = new Size(150, 40);
            btnSave.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            var btnCancel = CreateButton("Hủy", Color.FromArgb(229, 236, 241), Color.FromArgb(214, 224, 231));
            btnCancel.ForeColor = Color.FromArgb(48, 69, 84);
            btnCancel.Location = new Point(665, 520);
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
                    Title = "Chọn ảnh đại diện nhân viên",
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
                    MessageBox.Show(this, "Vui lòng nhập họ tên nhân viên.", "Thiếu thông tin",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                string role = cboRole.SelectedItem?.ToString() ?? "LeTan";
                string specialty = role == "BacSi" && cboSpecialty.SelectedIndex > 0
                    ? cboSpecialty.SelectedItem?.ToString() ?? ""
                    : "";
                if (role == "BacSi" && string.IsNullOrWhiteSpace(specialty))
                {
                    MessageBox.Show(this, "Vui lòng chọn chuyên khoa chính cho bác sĩ.", "Thiếu chuyên khoa",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    cboSpecialty.Focus();
                    return;
                }

                var employee = new NhanVien
                {
                    MaNV = txtCode.Text.Trim(),
                    HoTen = txtName.Text.Trim(),
                    ChucVu = role,
                    ChuyenKhoa = specialty,
                    SoDienThoai = txtPhone.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    AnhDaiDien = _profileImage
                };
                try
                {
                    ClinicDAL.LuuNhanVien(employee, _editing == null);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex) when (ex is SqlException or InvalidOperationException or ArgumentException)
                {
                    MessageBox.Show(this, ex.Message, "Không thể lưu nhân viên", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
