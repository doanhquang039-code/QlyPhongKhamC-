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
        private readonly Label _profileSectionTitle;
        private readonly CheckBox _useActivationCode;
        private readonly Label _fullNameLabel;
        private readonly TextBox _fullName;
        private readonly Label _birthDateLabel;
        private readonly DateTimePicker _dateOfBirth;
        private readonly Label _genderLabel;
        private readonly ComboBox _gender;
        private readonly Label _phoneLabel;
        private readonly TextBox _phone;
        private readonly Label _emailLabel;
        private readonly TextBox _email;
        private readonly Label _addressLabel;
        private readonly TextBox _address;
        private readonly Label _note;
        private readonly Button _registerButton;
        private readonly Button _cancelButton;
        private readonly Panel _card;

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

            _card = new Panel
            {
                Location = new Point(20, 118),
                Size = new Size(500, 500),
                BackColor = Color.White,
                Padding = new Padding(20)
            };
            _card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(224, 232, 241));
                e.Graphics.DrawRectangle(pen, 0, 0, _card.Width - 1, _card.Height - 1);
            };

            AddSectionTitle(_card, "THÔNG TIN ĐĂNG NHẬP", 17);
            AddLabel(_card, "Tên đăng nhập *", 49);
            _username = AddTextBox(_card, 72, 450);
            AddLabel(_card, "Mật khẩu *", 117);
            _password = AddTextBox(_card, 140, 215, password: true);
            AddLabel(_card, "Nhập lại mật khẩu *", 117, 255);
            _confirmPassword = AddTextBox(_card, 140, 215, left: 255, password: true);

            _profileSectionTitle = AddSectionTitle(_card, "HỒ SƠ BỆNH NHÂN", 190);
            _useActivationCode = new CheckBox
            {
                Text = "Tôi đã có hồ sơ khám (nhập mã kích hoạt)",
                Location = new Point(240, 188),
                Size = new Size(245, 24),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            _card.Controls.Add(_useActivationCode);
            _fullNameLabel = AddLabel(_card, "Họ và tên *", 222);
            _fullName = AddTextBox(_card, 245, 450);
            _birthDateLabel = AddLabel(_card, "Ngày sinh", 290);
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
            _genderLabel = AddLabel(_card, "Giới tính", 290, 255);
            _gender = new ComboBox
            {
                Location = new Point(255, 313),
                Size = new Size(215, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Font
            };
            _gender.Items.AddRange(new object[] { "Nam", "Nữ" });
            _gender.SelectedIndex = 0;
            _phoneLabel = AddLabel(_card, "Số điện thoại", 357);
            _phone = AddTextBox(_card, 380, 215);
            _emailLabel = AddLabel(_card, "Email", 357, 255);
            _email = AddTextBox(_card, 380, 215, left: 255);
            _addressLabel = AddLabel(_card, "Địa chỉ", 425);
            _address = AddTextBox(_card, 448, 450);

            _card.Controls.AddRange(new Control[] { _dateOfBirth, _gender });
            _useActivationCode.CheckedChanged += (s, e) => UpdateActivationMode();

            _note = new Label
            {
                Text = "* Thông tin bắt buộc. Ngày sinh và thông tin liên hệ có thể bổ sung sau.",
                Location = new Point(24, 628),
                Size = new Size(492, 22),
                ForeColor = Color.FromArgb(100, 116, 139),
                Font = new Font("Segoe UI", 8.5F)
            };
            _cancelButton = new Button
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
            _cancelButton.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _registerButton = new Button
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
            _registerButton.FlatAppearance.BorderSize = 0;
            _registerButton.Click += (s, e) => Register();

            Controls.AddRange(new Control[] { header, _card, _note, _cancelButton, _registerButton });
            AcceptButton = _registerButton;
            CancelButton = _cancelButton;
            UiText.Watch(this);
            UiText.Apply(this);
        }

        private static Label AddSectionTitle(Control parent, string text, int top)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(25, top),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(10, 80, 160)
            };
            parent.Controls.Add(label);
            return label;
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

        private static Label AddLabel(Control parent, string text, int top, int left = 25)
        {
            var label = new Label
            {
                Text = text,
                Location = new Point(left, top),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            parent.Controls.Add(label);
            return label;
        }

        private void UpdateActivationMode()
        {
            bool useCode = _useActivationCode.Checked;
            _profileSectionTitle.Text = useCode ? "LIÊN KẾT HỒ SƠ CŨ" : "HỒ SƠ BỆNH NHÂN";
            foreach (Control control in new Control[]
            {
                _fullNameLabel, _fullName, _birthDateLabel, _dateOfBirth,
                _genderLabel, _gender, _phoneLabel, _phone, _addressLabel, _address
            })
                control.Visible = !useCode;

            _emailLabel.Text = useCode ? "Mã kích hoạt" : "Email";
            _emailLabel.Location = useCode ? new Point(25, 222) : new Point(255, 357);
            _email.Location = useCode ? new Point(25, 245) : new Point(255, 380);
            _email.Width = useCode ? 450 : 215;
            _email.MaxLength = useCode ? 19 : 100;
            _email.Text = "";
            _note.Text = useCode
                ? "Nhập mã do lễ tân cấp. Mã chỉ dùng một lần và có hạn 7 ngày."
                : "* Thông tin bắt buộc. Ngày sinh và thông tin liên hệ có thể bổ sung sau.";
        }

        private void Register()
        {
            if (string.IsNullOrWhiteSpace(_username.Text) ||
                string.IsNullOrWhiteSpace(_password.Text) ||
                (!_useActivationCode.Checked && string.IsNullOrWhiteSpace(_fullName.Text)))
            {
                MessageBox.Show(UiText.Translate(_useActivationCode.Checked
                        ? "Vui lòng nhập tên đăng nhập và mật khẩu."
                        : "Vui lòng nhập tên đăng nhập, mật khẩu và họ tên."),
                    UiText.Translate("Thiếu thông tin"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (_useActivationCode.Checked && string.IsNullOrWhiteSpace(_email.Text))
            {
                MessageBox.Show(UiText.Translate("Vui lòng nhập mã kích hoạt do lễ tân cấp."),
                    UiText.Translate("Thiếu mã kích hoạt"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_password.Text.Length < 6)
            {
                MessageBox.Show(UiText.Translate("Mật khẩu phải có ít nhất 6 ký tự."),
                    UiText.Translate("Mật khẩu chưa hợp lệ"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_password.Text != _confirmPassword.Text)
            {
                MessageBox.Show(UiText.Translate("Mật khẩu nhập lại không khớp."),
                    UiText.Translate("Xác nhận mật khẩu"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (_useActivationCode.Checked)
                {
                    ClinicDAL.DangKyTaiKhoanBenhNhanBangMaKichHoat(
                        _username.Text, _password.Text, _email.Text);
                }
                else
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
                }

                MessageBox.Show(UiText.Translate(_useActivationCode.Checked
                        ? "Đăng ký thành công và đã liên kết với hồ sơ khám cũ. Hãy đăng nhập bằng tài khoản vừa tạo."
                        : "Đăng ký thành công. Hãy đăng nhập bằng tài khoản bệnh nhân vừa tạo."),
                    UiText.Translate("Hoàn tất đăng ký"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(UiText.Translate(ex.Message), UiText.Translate("Không thể đăng ký"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
