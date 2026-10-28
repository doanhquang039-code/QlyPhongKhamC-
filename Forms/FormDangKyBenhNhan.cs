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
        private readonly TextBox _address;

        public string RegisteredUsername => _username.Text.Trim();

        public FormDangKyBenhNhan()
        {
            Text = "Đăng ký tài khoản bệnh nhân";
            Size = new Size(500, 530);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5F);

            var title = new Label
            {
                Text = "TẠO TÀI KHOẢN BỆNH NHÂN",
                Location = new Point(24, 18),
                AutoSize = true,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 32, 54)
            };
            var note = new Label
            {
                Text = "Tài khoản đăng ký sẽ có quyền Bệnh nhân và được liên kết với hồ sơ mới.",
                Location = new Point(26, 49),
                Size = new Size(440, 34),
                ForeColor = Color.DimGray
            };

            _username = AddTextBox("Tên đăng nhập (*):", 95);
            _password = AddTextBox("Mật khẩu (*):", 135, password: true);
            _confirmPassword = AddTextBox("Nhập lại mật khẩu (*):", 175, password: true);
            _fullName = AddTextBox("Họ và tên (*):", 215);

            _dateOfBirth = new DateTimePicker
            {
                Location = new Point(175, 255),
                Size = new Size(260, 28),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                ShowCheckBox = true,
                Checked = false,
                MaxDate = DateTime.Today
            };
            AddLabel("Ngày sinh:", 255);

            _gender = new ComboBox
            {
                Location = new Point(175, 295),
                Size = new Size(160, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _gender.Items.AddRange(new object[] { "Nam", "Nữ" });
            _gender.SelectedIndex = 0;
            AddLabel("Giới tính:", 295);

            _phone = AddTextBox("Số điện thoại:", 335);
            _address = AddTextBox("Địa chỉ:", 375);

            var btnRegister = new Button
            {
                Text = "ĐĂNG KÝ",
                Location = new Point(175, 425),
                Size = new Size(125, 38),
                BackColor = Color.FromArgb(0, 150, 136),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            var btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(310, 425),
                Size = new Size(90, 38),
                DialogResult = DialogResult.Cancel
            };

            btnRegister.Click += (s, e) => Register();
            Controls.AddRange(new Control[] { title, note, _dateOfBirth, _gender, btnRegister, btnCancel });
            AcceptButton = btnRegister;
            CancelButton = btnCancel;
        }

        private TextBox AddTextBox(string label, int top, bool password = false)
        {
            AddLabel(label, top);
            var textBox = new TextBox
            {
                Location = new Point(175, top),
                Size = new Size(260, 28),
                UseSystemPasswordChar = password
            };
            Controls.Add(textBox);
            return textBox;
        }

        private void AddLabel(string text, int top)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(28, top + 4),
                AutoSize = true
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
                    _address.Text);

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
