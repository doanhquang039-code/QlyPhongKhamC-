using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public class FormLogin : Form
    {
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private Button btnConfig = null!;
        private ComboBox cboRole = null!;
        private Label lblError = null!;

        public FormLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng nhập - Hệ thống Quản lý Phòng Khám";
            this.ClientSize = new Size(460, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            // Header Banner Panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(10, 80, 160)
            };

            Label lblHeaderTitle = new Label
            {
                Text = "QUẢN LÝ PHÒNG KHÁM",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            Label lblSubTitle = new Label
            {
                Text = "Đăng nhập hệ thống quản lý & khám chữa bệnh",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(200, 225, 255),
                Location = new Point(22, 48),
                AutoSize = true
            };
            headerPanel.Controls.AddRange(new Control[] { lblHeaderTitle, lblSubTitle });

            Label lblRole = new Label
            {
                Text = "Vai trò đăng nhập:",
                Location = new Point(35, 105),
                AutoSize = true,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9F)
            };

            cboRole = new ComboBox
            {
                Location = new Point(35, 128),
                Size = new Size(375, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F)
            };
            cboRole.Items.AddRange(new object[] { "Quản trị viên", "Bác sĩ", "Y tá", "Bệnh nhân" });
            cboRole.SelectedIndex = -1;

            // Username
            Label lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(35, 170), AutoSize = true };
            txtUsername = new TextBox { Location = new Point(35, 195), Size = new Size(375, 30) };

            // Password
            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(35, 238), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(35, 263), Size = new Size(375, 30), UseSystemPasswordChar = true };

            lblError = new Label
            {
                Location = new Point(35, 302),
                Size = new Size(375, 34),
                ForeColor = Color.Crimson,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };

            // Buttons
            btnLogin = new Button
            {
                Text = "ĐĂNG NHẬP",
                Location = new Point(35, 348),
                Size = new Size(240, 42),
                BackColor = Color.FromArgb(10, 80, 160),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.Click += BtnLogin_Click;

            btnConfig = new Button
            {
                Text = "⚙ Máy chủ",
                Location = new Point(285, 348),
                Size = new Size(125, 42),
                BackColor = Color.FromArgb(238, 242, 246),
                ForeColor = Color.FromArgb(30, 40, 60),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfig.Click += (s, e) => {
                var dlg = new FormConfigDb();
                dlg.ShowDialog();
            };

            var btnRegister = new Button
            {
                Text = "Đăng ký tài khoản bệnh nhân",
                Location = new Point(35, 400),
                Size = new Size(375, 38),
                BackColor = Color.FromArgb(232, 246, 244),
                ForeColor = Color.FromArgb(0, 120, 110),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnRegister.FlatAppearance.BorderColor = Color.FromArgb(0, 150, 136);
            btnRegister.Click += (s, e) =>
            {
                using var dialog = new FormDangKyBenhNhan();
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    cboRole.SelectedItem = "Bệnh nhân";
                    txtUsername.Text = dialog.RegisteredUsername;
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            };

            this.Controls.AddRange(new Control[] {
                headerPanel,
                lblRole, cboRole,
                lblUser, txtUsername,
                lblPass, txtPassword,
                lblError,
                btnLogin, btnConfig, btnRegister
            });

            this.AcceptButton = btnLogin;
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text.Trim();
            string? selectedRole = cboRole.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(selectedRole) ||
                string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                lblError.Text = "Vui lòng chọn vai trò và nhập đầy đủ tên đăng nhập, mật khẩu.";
                return;
            }

            try
            {
                if (selectedRole == "Bệnh nhân")
                {
                    var patient = ClinicDAL.DangNhapBenhNhan(user, pass);
                    if (patient == null)
                    {
                        lblError.Text = "Thông tin đăng nhập không hợp lệ cho vai trò Bệnh nhân.";
                        return;
                    }

                    SessionState.CurrentUser = null;
                    SessionState.CurrentPatient = patient;
                    var patientForm = new FormBenhNhanPortal();
                    patientForm.FormClosed += (s, args) => ReturnToLogin();
                    patientForm.Show();
                    this.Hide();
                    return;
                }

                var account = ClinicDAL.DangNhap(user, pass);
                string expectedRole = selectedRole switch
                {
                    "Quản trị viên" => "Admin",
                    "Bác sĩ" => "BacSi",
                    "Y tá" => "LeTan",
                    _ => ""
                };
                if (account == null || account.Quyen != expectedRole)
                {
                    lblError.Text = $"Thông tin đăng nhập không hợp lệ cho vai trò {selectedRole}.";
                    return;
                }

                SessionState.CurrentPatient = null;
                SessionState.CurrentUser = account;
                var mainForm = new FormMain();
                mainForm.FormClosed += (s, args) => ReturnToLogin();
                mainForm.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                this.Show();
                lblError.Text = "Không kết nối được cơ sở dữ liệu. Kiểm tra cấu hình máy chủ SQL.";
                var openSettings = MessageBox.Show(
                    this,
                    $"Không kết nối được cơ sở dữ liệu.\n\n{ex.Message}\n\n" +
                    "Hãy kiểm tra SQL Server đã cài và đang chạy, tên instance đúng, " +
                    "và database đã được tạo bằng init_database.sql.\n\nMở cấu hình máy chủ ngay?",
                    "Không kết nối được SQL Server",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (openSettings == DialogResult.Yes)
                {
                    using var dialog = new FormConfigDb();
                    dialog.ShowDialog(this);
                }
            }
        }

        private void ReturnToLogin()
        {
            SessionState.CurrentUser = null;
            SessionState.CurrentPatient = null;
            txtUsername.Clear();
            txtPassword.Clear();
            lblError.Text = "";
            cboRole.SelectedIndex = -1;
            Show();
            Activate();
            txtUsername.Focus();
        }
    }
}
