using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public class FormConfigDb : Form
    {
        private TextBox txtServer = null!;
        private TextBox txtDatabase = null!;
        private TextBox txtUser = null!;
        private TextBox txtPassword = null!;
        private Button btnTest = null!;
        private Button btnSave = null!;
        private Button btnClose = null!;
        private Label lblStatus = null!;

        public FormConfigDb()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Cấu hình kết nối SQL Server (Hỗ trợ đa máy laptop)";
            this.Size = new Size(500, 360);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

            Label lblTitle = new Label
            {
                Text = "CẤU HÌNH KẾT NỐI MÁY CHỦ CSDL",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 43, 73),
                Location = new Point(20, 15),
                AutoSize = true
            };

            Label lblNote = new Label
            {
                Text = "Để kết nối từ laptop khác trong mạng LAN/Wi-Fi: Nhập IP máy chủ (VD: 192.168.1.15)",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location = new Point(20, 42),
                Size = new Size(440, 35)
            };

            int startY = 85;
            int stepY = 40;

            // Server
            Label lblSrv = new Label { Text = "Máy chủ (IP / Host):", Location = new Point(25, startY), AutoSize = true };
            txtServer = new TextBox { Location = new Point(180, startY - 3), Size = new Size(270, 26), Text = DatabaseHelper.ServerIp };

            // Database
            Label lblDb = new Label { Text = "Tên CSDL:", Location = new Point(25, startY + stepY), AutoSize = true };
            txtDatabase = new TextBox { Location = new Point(180, startY + stepY - 3), Size = new Size(270, 26), Text = DatabaseHelper.DatabaseName };

            // Username
            Label lblUsr = new Label { Text = "Tài khoản (sa):", Location = new Point(25, startY + stepY * 2), AutoSize = true };
            txtUser = new TextBox { Location = new Point(180, startY + stepY * 2 - 3), Size = new Size(270, 26), Text = DatabaseHelper.Username };

            // Password
            Label lblPwd = new Label { Text = "Mật khẩu:", Location = new Point(25, startY + stepY * 3), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(180, startY + stepY * 3 - 3), Size = new Size(270, 26), Text = DatabaseHelper.Password, UseSystemPasswordChar = true };

            lblStatus = new Label
            {
                Location = new Point(25, startY + stepY * 4),
                Size = new Size(430, 25),
                ForeColor = Color.DarkSlateBlue,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            // Buttons
            btnTest = new Button
            {
                Text = "Kiểm tra kết nối",
                Location = new Point(25, 270),
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(230, 235, 245),
                FlatStyle = FlatStyle.Flat
            };
            btnTest.Click += BtnTest_Click;

            btnSave = new Button
            {
                Text = "Lưu cấu hình",
                Location = new Point(170, 270),
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;

            btnClose = new Button
            {
                Text = "Đóng",
                Location = new Point(320, 270),
                Size = new Size(130, 34),
                BackColor = Color.FromArgb(220, 224, 230),
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] {
                lblTitle, lblNote,
                lblSrv, txtServer,
                lblDb, txtDatabase,
                lblUsr, txtUser,
                lblPwd, txtPassword,
                lblStatus,
                btnTest, btnSave, btnClose
            });
        }

        private void BtnTest_Click(object? sender, EventArgs e)
        {
            lblStatus.Text = "Đang kiểm tra kết nối...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            DatabaseHelper.ServerIp = txtServer.Text.Trim();
            DatabaseHelper.DatabaseName = txtDatabase.Text.Trim();
            DatabaseHelper.Username = txtUser.Text.Trim();
            DatabaseHelper.Password = txtPassword.Text.Trim();
            DatabaseHelper.LoadConfig();

            if (DatabaseHelper.TestConnection(out string error))
            {
                lblStatus.Text = "✓ Kết nối thành công đến SQL Server!";
                lblStatus.ForeColor = Color.Green;
            }
            else
            {
                lblStatus.Text = "✗ Kết nối thất bại: " + error;
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            DatabaseHelper.SaveConfig(txtServer.Text.Trim(), txtDatabase.Text.Trim(), txtUser.Text.Trim(), txtPassword.Text.Trim());
            MessageBox.Show("Đã lưu cấu hình kết nối thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.Close();
        }
    }
}
