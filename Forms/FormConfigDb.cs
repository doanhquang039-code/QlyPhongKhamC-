using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public class FormConfigDb : Form
    {
        private readonly ComboBox _server;
        private readonly TextBox _database;
        private readonly TextBox _username;
        private readonly TextBox _password;
        private readonly Label _status;
        private readonly Button _testButton;
        private readonly Button _saveButton;

        public FormConfigDb()
        {
            Text = "Cấu hình kết nối SQL Server";
            ClientSize = new Size(560, 390);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 9.5F);

            var title = new Label
            {
                Text = "CẤU HÌNH KẾT NỐI SQL SERVER",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 43, 73),
                Location = new Point(22, 16),
                AutoSize = true
            };
            var note = new Label
            {
                Text = "Chọn Tìm SQL cục bộ để dò instance trên máy này, hoặc nhập Server Name từ SSMS.\n" +
                       "Tài khoản/mật khẩu là của SQL Server, không phải tài khoản đăng nhập ứng dụng.",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Location = new Point(24, 45),
                Size = new Size(510, 42)
            };

            _server = new ComboBox
            {
                Location = new Point(185, 98),
                Size = new Size(250, 28),
                DropDownStyle = ComboBoxStyle.DropDown,
                Text = DatabaseHelper.ServerIp
            };
            var findServer = new Button
            {
                Text = "Tìm cục bộ",
                Location = new Point(442, 96),
                Size = new Size(96, 30)
            };
            findServer.Click += FindLocalServers;

            _database = new TextBox
            {
                Location = new Point(185, 140),
                Size = new Size(353, 27),
                Text = DatabaseHelper.DatabaseName
            };
            _username = new TextBox
            {
                Location = new Point(185, 182),
                Size = new Size(353, 27),
                Text = DatabaseHelper.Username
            };
            _password = new TextBox
            {
                Location = new Point(185, 224),
                Size = new Size(353, 27),
                Text = DatabaseHelper.Password,
                UseSystemPasswordChar = true
            };

            var serverLabel = CreateFieldLabel("SQL Server / Instance:", 101);
            var databaseLabel = CreateFieldLabel("Tên database:", 143);
            var usernameLabel = CreateFieldLabel("Tài khoản SQL:", 185);
            var passwordLabel = CreateFieldLabel("Mật khẩu SQL:", 227);

            _status = new Label
            {
                Location = new Point(25, 265),
                Size = new Size(510, 42),
                ForeColor = Color.DarkSlateBlue,
                Font = new Font("Segoe UI", 9F)
            };

            _testButton = new Button
            {
                Text = "Kiểm tra SQL Server",
                Location = new Point(25, 324),
                Size = new Size(160, 36),
                BackColor = Color.FromArgb(230, 235, 245),
                FlatStyle = FlatStyle.Flat
            };
            _testButton.Click += TestServer;

            _saveButton = new Button
            {
                Text = "Lưu cấu hình",
                Location = new Point(195, 324),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _saveButton.Click += SaveConfiguration;

            var closeButton = new Button
            {
                Text = "Đóng",
                Location = new Point(398, 324),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(220, 224, 230),
                FlatStyle = FlatStyle.Flat
            };
            closeButton.Click += (_, _) => Close();

            Controls.AddRange(new Control[]
            {
                title, note,
                serverLabel, _server, findServer,
                databaseLabel, _database,
                usernameLabel, _username,
                passwordLabel, _password,
                _status, _testButton, _saveButton, closeButton
            });
        }

        private void FindLocalServers(object? sender, EventArgs e)
        {
            try
            {
                using RegistryKey? instances = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
                string[] serverNames = instances?.GetValueNames()
                    .Select(name => string.Equals(name, "MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                        ? "."
                        : $@".\{name}")
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray() ?? Array.Empty<string>();

                _server.Items.Clear();
                _server.Items.AddRange(serverNames);
                if (serverNames.Length == 0)
                {
                    _status.Text = "Không tìm thấy instance SQL cục bộ. Cài SQL Server Database Engine, hoặc nhập Server Name từ SSMS.";
                    _status.ForeColor = Color.DarkOrange;
                    return;
                }

                if (!serverNames.Contains(_server.Text, StringComparer.OrdinalIgnoreCase))
                    _server.Text = serverNames[0];
                _status.Text = serverNames.Length == 1
                    ? $"Đã tìm thấy instance: {serverNames[0]}. Nhấn Kiểm tra SQL Server để xác thực."
                    : $"Tìm thấy {serverNames.Length} instance. Chọn instance cần dùng rồi nhấn Kiểm tra SQL Server.";
                _status.ForeColor = Color.DarkSlateBlue;
                if (serverNames.Length > 1)
                    _server.DroppedDown = true;
            }
            catch (Exception exception)
            {
                _status.Text = "Không đọc được danh sách instance SQL trên máy.";
                _status.ForeColor = Color.Red;
                MessageBox.Show(this, exception.Message, "Không dò được SQL Server",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TestServer(object? sender, EventArgs e)
        {
            if (!ValidateConfiguration())
                return;

            SetBusy(true);
            _status.Text = "Đang kiểm tra máy chủ và tài khoản SQL...";
            _status.ForeColor = Color.Blue;
            _status.Refresh();
            try
            {
                if (!DatabaseHelper.TestServerConnection(
                        _server.Text.Trim(),
                        _username.Text.Trim(),
                        _password.Text,
                        out string error))
                {
                    _status.Text = "Không kết nối được SQL Server. Kiểm tra instance và tài khoản SQL.";
                    _status.ForeColor = Color.Red;
                    MessageBox.Show(
                        this,
                        $"Không kết nối được tới SQL Server '{_server.Text.Trim()}'.\n\n{error}\n\n" +
                        "Dùng đúng Server Name trong SSMS và xác nhận dịch vụ SQL Server đang chạy. " +
                        "Phép kiểm tra này xác thực máy chủ/tài khoản; database cần được tạo riêng.",
                        "Kiểm tra SQL Server thất bại",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _status.Text = $"Kết nối SQL Server thành công. Database cần dùng: {_database.Text.Trim()}.";
                _status.ForeColor = Color.Green;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SaveConfiguration(object? sender, EventArgs e)
        {
            if (!ValidateConfiguration())
                return;

            SetBusy(true);
            try
            {
                if (!DatabaseHelper.TestServerConnection(
                        _server.Text.Trim(),
                        _username.Text.Trim(),
                        _password.Text,
                        out string error))
                {
                    MessageBox.Show(
                        this,
                        $"Chưa lưu cấu hình vì chưa kết nối được SQL Server.\n\n{error}",
                        "Kiểm tra kết nối trước",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                DatabaseHelper.SaveConfig(
                    _server.Text.Trim(),
                    _database.Text.Trim(),
                    _username.Text.Trim(),
                    _password.Text);
                MessageBox.Show(
                    this,
                    $"Đã lưu cấu hình SQL Server.\n\nNếu database '{_database.Text.Trim()}' chưa có, " +
                    "hãy mở init_database.sql trong SSMS, bật Query > SQLCMD Mode, rồi đặt giá trị " +
                    "DatabaseName trong dòng :setvar trùng với tên database này trước khi chạy script.",
                    "Lưu cấu hình thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                Close();
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    this,
                    $"Không lưu được cấu hình máy chủ.\n\n{exception.Message}",
                    "Lỗi lưu cấu hình",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private bool ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_server.Text) ||
                string.IsNullOrWhiteSpace(_database.Text) ||
                string.IsNullOrWhiteSpace(_username.Text))
            {
                MessageBox.Show(this, "Nhập Server Name, tên database và tài khoản SQL.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!Regex.IsMatch(_database.Text.Trim(), @"^[A-Za-z][A-Za-z0-9_]{0,127}$"))
            {
                MessageBox.Show(
                    this,
                    "Tên database phải bắt đầu bằng chữ cái và chỉ gồm chữ cái, chữ số hoặc dấu gạch dưới.",
                    "Tên database không hợp lệ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                _database.Focus();
                return false;
            }

            if (new[] { "master", "model", "msdb", "tempdb" }
                .Contains(_database.Text.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show(this, "Không thể dùng tên database hệ thống của SQL Server.",
                    "Tên database không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _database.Focus();
                return false;
            }

            return true;
        }

        private void SetBusy(bool busy)
        {
            _testButton.Enabled = !busy;
            _saveButton.Enabled = !busy;
            UseWaitCursor = busy;
        }

        private static Label CreateFieldLabel(string text, int top) => new()
        {
            Text = text,
            Location = new Point(25, top),
            AutoSize = true
        };
    }
}
