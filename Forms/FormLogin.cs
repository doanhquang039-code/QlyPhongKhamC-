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
            this.Size = new Size(460, 440);
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
                "Lễ tân tiếp đón (letan / 123456)",
                "Thu ngân viện phí (thungan / 123456)"
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

            this.Controls.AddRange(new Control[] {
                headerPanel,
                lblQuick, cboQuickPick,
                lblUser, txtUsername,
                lblPass, txtPassword,
                lblError,
                btnLogin, btnConfig
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
                if (account == null)
                {
                    lblError.Text = "Sai tên đăng nhập hoặc mật khẩu!";
                    return;
                }

                SessionState.CurrentUser = account;

                this.Hide();
                var mainForm = new FormMain();
                mainForm.FormClosed += (s, args) => this.Close();
                mainForm.Show();
            }
            catch (Exception ex)
            {
                lblError.Text = "Lỗi kết nối CSDL: " + ex.Message;
                MessageBox.Show("Không thể kết nối đến SQL Server!\nVui lòng nhấn nút 'Máy chủ' để kiểm tra lại IP hoặc mật khẩu sa.\n\nChi tiết: " + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
