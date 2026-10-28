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
        private ComboBox cboQuickPick = null!;
        private Label lblError = null!;

        public FormLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng nhập - Hệ thống Quản lý Phòng Khám";
            this.Size = new Size(460, 480);
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

            // Quick Pick
            Label lblQuick = new Label
            {
                Text = "Chọn nhanh tài khoản mẫu:",
                Location = new Point(35, 105),
                AutoSize = true,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 9F)
            };

            cboQuickPick = new ComboBox
            {
                Location = new Point(35, 128),
                Size = new Size(375, 28),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboQuickPick.Items.AddRange(new object[] {
                "-- Tự nhập tài khoản --",
                "Quản trị viên (admin / 123456)",
                "Bác sĩ khám (bacsi_nam / 123456)",
                "Lễ tân & Thu ngân (letan / 123456)",
                "Lễ tân & Thu ngân (thungan / 123456)",
                "TEST Admin (test_admin / 123456)",
                "TEST Bác sĩ Nội (test_bs / 123456)",
                "TEST Bác sĩ Tai Mũi Họng (test_bs2 / 123456)",
                "TEST Lễ tân & Thu ngân (test_letan / 123456)",
                "TEST Bệnh nhân (test_bn01 / 123456)"
            });
            cboQuickPick.SelectedIndex = 1;
            cboQuickPick.SelectedIndexChanged += CboQuickPick_SelectedIndexChanged;

            // Username
            Label lblUser = new Label { Text = "Tên đăng nhập:", Location = new Point(35, 170), AutoSize = true };
            txtUsername = new TextBox { Location = new Point(35, 195), Size = new Size(375, 28), Text = "admin" };

            // Password
            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(35, 235), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(35, 260), Size = new Size(375, 28), Text = "123456", UseSystemPasswordChar = true };

            lblError = new Label
            {
                Location = new Point(35, 295),
                Size = new Size(375, 22),
                ForeColor = Color.Crimson,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };

            // Buttons
            btnLogin = new Button
            {
                Text = "ĐĂNG NHẬP",
                Location = new Point(35, 325),
                Size = new Size(240, 40),
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
                Location = new Point(285, 325),
                Size = new Size(125, 40),
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
                Location = new Point(35, 373),
                Size = new Size(375, 34),
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
                    cboQuickPick.SelectedIndex = 0;
                    txtUsername.Text = dialog.RegisteredUsername;
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            };

            this.Controls.AddRange(new Control[] {
                headerPanel,
                lblQuick, cboQuickPick,
                lblUser, txtUsername,
                lblPass, txtPassword,
                lblError,
                btnLogin, btnConfig, btnRegister
            });

            this.AcceptButton = btnLogin;
        }

        private void CboQuickPick_SelectedIndexChanged(object? sender, EventArgs e)
        {
            switch (cboQuickPick.SelectedIndex)
            {
                case 1:
                    txtUsername.Text = "admin";
                    txtPassword.Text = "123456";
                    break;
                case 2:
                    txtUsername.Text = "bacsi_nam";
                    txtPassword.Text = "123456";
                    break;
                case 3:
                    txtUsername.Text = "letan";
                    txtPassword.Text = "123456";
                    break;
                case 4:
                    txtUsername.Text = "thungan";
                    txtPassword.Text = "123456";
                    break;
                case 5:
                    txtUsername.Text = "test_admin";
                    txtPassword.Text = "123456";
                    break;
                case 6:
                    txtUsername.Text = "test_bs";
                    txtPassword.Text = "123456";
                    break;
                case 7:
                    txtUsername.Text = "test_bs2";
                    txtPassword.Text = "123456";
                    break;
                case 8:
                    txtUsername.Text = "test_letan";
                    txtPassword.Text = "123456";
                    break;
                case 9:
                    txtUsername.Text = "test_bn01";
                    txtPassword.Text = "123456";
                    break;
            }
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";
            string user = txtUsername.Text.Trim();
            string pass = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                lblError.Text = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!";
                return;
            }

            try
            {
                var account = ClinicDAL.DangNhap(user, pass);
                if (account != null)
                {
                    SessionState.CurrentPatient = null;
                    SessionState.CurrentUser = account;
                    var mainForm = new FormMain();
                    mainForm.FormClosed += (s, args) => ReturnToLogin();
                    mainForm.Show();
                    this.Hide();
                    return;
                }

                var patient = ClinicDAL.DangNhapBenhNhan(user, pass);
                if (patient == null)
                {
                    lblError.Text = "Sai tên đăng nhập hoặc mật khẩu!";
                    return;
                }

                SessionState.CurrentUser = null;
                SessionState.CurrentPatient = patient;
                var patientForm = new FormBenhNhanPortal();
                patientForm.FormClosed += (s, args) => ReturnToLogin();
                patientForm.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                this.Show();
                lblError.Text = "Lỗi: " + ex.Message;
                MessageBox.Show("Lỗi khi mở giao diện:\n" + ex.Message + "\n\nChi tiết:\n" + ex.StackTrace, "Lỗi đăng nhập", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReturnToLogin()
        {
            SessionState.CurrentUser = null;
            SessionState.CurrentPatient = null;
            txtUsername.Clear();
            txtPassword.Clear();
            lblError.Text = "";
            cboQuickPick.SelectedIndex = 0;
            Show();
            Activate();
            txtUsername.Focus();
        }
    }
}
