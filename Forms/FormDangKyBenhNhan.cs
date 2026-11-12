using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public sealed class FormDangKyBenhNhan : Form
    {
        private readonly TextBox _username;
        private readonly TextBox _password;
        private readonly TextBox _confirmPassword;
        private readonly TextBox _fullName;
        private readonly DateTimePicker _dateOfBirth;
        private readonly ComboBox _gender;
        private readonly TextBox _phone;
        private readonly TextBox _email;
        private readonly TextBox _address;

        public string RegisteredUsername => _username.Text.Trim();

        public FormDangKyBenhNhan()
        {
            Text = "Đăng ký tài khoản bệnh nhân";
            ClientSize = new Size(540, 690);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 248, 252);
            Font = new Font("Segoe UI", 9.5F);

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.FromArgb(10, 80, 160)
            };
            header.Controls.Add(new Label
            {
                Text = "TẠO TÀI KHOẢN BỆNH NHÂN",
                Location = new Point(26, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White
            });
            header.Controls.Add(new Label
            {
                Text = "Đăng ký hồ sơ để đặt lịch và theo dõi khám chữa bệnh.",
                Location = new Point(28, 57),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(218, 234, 255)
            });

            var card = new Panel
            {
                Location = new Point(20, 118),
                Size = new Size(500, 500),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(224, 232, 241));
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            AddSectionTitle(card, "THÔNG TIN ĐĂNG NHẬP", 17);
            AddLabel(card, "Tên đăng nhập *", 49);
            _username = AddTextBox(card, 72, 450);
            AddLabel(card, "Mật khẩu *", 117);
            _password = AddTextBox(card, 140, 215, password: true);
            AddLabel(card, "Nhập lại mật khẩu *", 117, 255);
            _confirmPassword = AddTextBox(card, 140, 215, left: 255, password: true);

            AddSectionTitle(card, "HỒ SƠ BỆNH NHÂN", 190);
            AddLabel(card, "Họ và tên *", 222);
            _fullName = AddTextBox(card, 245, 450);
            AddLabel(card, "Ngày sinh", 290);
            _dateOfBirth = new DateTimePicker
            {
                Location = new Point(25, 313),
                Size = new Size(215, 30),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                ShowCheckBox = true,
                Checked = false,
                MaxDate = DateTime.Today,
                Font = Font
            };
            AddLabel(card, "Giới tính", 290, 255);
            _gender = new ComboBox
            {
                Location = new Point(255, 313),
                Size = new Size(215, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Font
            };
            _gender.Items.AddRange(new object[] { "Nam", "Nữ" });
            _gender.SelectedIndex = 0;
            AddLabel(card, "Số điện thoại", 357);
            _phone = AddTextBox(card, 380, 215);
            AddLabel(card, "Email", 357, 255);
            _email = AddTextBox(card, 380, 215, left: 255);
            AddLabel(card, "Địa chỉ", 425);
            _address = AddTextBox(card, 448, 450);

            card.Controls.AddRange(new Control[] { _dateOfBirth, _gender });

            var note = new Label
            {
                Text = "* Thông tin bắt buộc. Ngày sinh và thông tin liên hệ có thể bổ sung sau.",
                Location = new Point(24, 628),
                Size = new Size(492, 22),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5F)
            };
            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(296, 652),
                Size = new Size(100, 34),
                DialogResult = DialogResult.Cancel,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            var btnRegister = new Button
            {
                Text = "TẠO TÀI KHOẢN",
                Location = new Point(404, 652),
                Size = new Size(112, 34),
                BackColor = Color.FromArgb(0, 137, 123),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += (s, e) => Register();

            Controls.AddRange(new Control[] { header, card, note, btnCancel, btnRegister });
            AcceptButton = btnRegister;
            CancelButton = btnCancel;
        }

        private static void AddSectionTitle(Control parent, string text, int top)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(25, top),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(10, 80, 160)
            });
        }

        private static TextBox AddTextBox(Control parent, int top, int width, int left = 25, bool password = false)
        {
            var textBox = new TextBox
            {
                Location = new Point(left, top),
                Size = new Size(width, 30),
                UseSystemPasswordChar = password,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10F)
            };
            parent.Controls.Add(textBox);
            return textBox;
        }

        private static void AddLabel(Control parent, string text, int top, int left = 25)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(left, top),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(51, 65, 85)
            });
        }

        private void Register()
        {
            if (string.IsNullOrWhiteSpace(_username.Text) ||
                string.IsNullOrWhiteSpace(_password.Text) ||
                string.IsNullOrWhiteSpace(_fullName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập, mật khẩu và họ tên.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_password.Text.Length < 6)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự.",
                    "Mật khẩu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_password.Text != _confirmPassword.Text)
            {
                MessageBox.Show("Mật khẩu nhập lại không khớp.",
                    "Xác nhận mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                ClinicDAL.DangKyTaiKhoanBenhNhan(
                    _username.Text,
                    _password.Text,
                    _fullName.Text,
                    _dateOfBirth.Checked ? _dateOfBirth.Value.Date : null,
                    _gender.SelectedItem?.ToString() ?? "Nam",
                    _phone.Text,
                    _address.Text,
                    _email.Text);

                MessageBox.Show("Đăng ký thành công. Hãy đăng nhập bằng tài khoản bệnh nhân vừa tạo.",
                    "Hoàn tất đăng ký", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể đăng ký",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
