using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Helpers;

namespace QlyPhongKham.Forms
{
    public sealed class FormEmployeeProfile : Form
    {
        private readonly Bitmap _avatar;

        public FormEmployeeProfile(string maNV)
        {
            DataRow profile = ClinicDAL.LayHoSoNhanVien(maNV)
                ?? throw new InvalidOperationException("Không tìm thấy hồ sơ nhân viên đang đăng nhập.");
            string fullName = Value(profile, "HoTen", "Nhân viên");
            _avatar = ProfileImageHelper.CreateAvatar(
                profile["AnhDaiDien"] as byte[],
                fullName,
                104);

            Text = "Hồ sơ cá nhân";
            ClientSize = new Size(520, 540);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(244, 247, 250);
            Font = new Font("Segoe UI", 9.5F);
            FormEffects.FadeIn(this);

            var header = new Panel
            {
                Location = Point.Empty,
                Size = new Size(520, 84),
                BackColor = Color.FromArgb(15, 50, 76)
            };
            header.Controls.Add(new Label
            {
                Text = "HỒ SƠ CÁ NHÂN",
                Location = new Point(24, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White
            });
            header.Controls.Add(new Label
            {
                Text = "Thông tin tài khoản và nhân sự",
                Location = new Point(26, 49),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(187, 211, 224)
            });

            var card = new Panel
            {
                Location = new Point(24, 102),
                Size = new Size(472, 354),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            var avatar = new PictureBox
            {
                Image = _avatar,
                Location = new Point(182, 20),
                Size = new Size(104, 104),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(237, 243, 247)
            };
            card.Controls.Add(avatar);

            var name = new Label
            {
                Text = fullName,
                Location = new Point(20, 134),
                Size = new Size(430, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 50, 76)
            };
            card.Controls.Add(name);

            string specialty = Value(profile, "ChuyenKhoa", "Chưa cập nhật");
            string role = Value(profile, "ChucVu", "");
            if (string.IsNullOrWhiteSpace(specialty))
                specialty = "Chưa cập nhật";
            bool hasRoomAssignments = role is "LeTan" or "BacSi";
            int cardHeight = hasRoomAssignments ? 430 : 354;
            int formHeight = hasRoomAssignments ? 616 : 540;
            ClientSize = new Size(520, formHeight);
            card.Size = new Size(472, cardHeight);

            var roleLabel = new Label
            {
                Text = string.IsNullOrWhiteSpace(specialty) || specialty == "Chưa cập nhật"
                    ? RoleDisplayName(role)
                    : $"{RoleDisplayName(role)}  ·  {specialty}",
                Location = new Point(20, 164),
                Size = new Size(430, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 145, 134),
                AutoEllipsis = true
            };
            card.Controls.Add(roleLabel);

            AddField(card, "Mã nhân viên", Value(profile, "MaNV"), 20, 207);
            AddField(card, "Tên đăng nhập", Value(profile, "TenDangNhap", "Chưa cấp"), 20, 245);
            AddField(card, "Điện thoại", Value(profile, "SoDienThoai", "Chưa cập nhật"), 20, 283);
            AddField(card, "Email", Value(profile, "Email", "Chưa cập nhật"), 20, 321);

            if (hasRoomAssignments)
            {
                var assignments = ClinicDAL.LayPhanCongPhongNhanVien(maNV, role);
                var rooms = new List<string>();
                var departments = new List<string>();
                foreach (DataRow assignment in assignments.Rows)
                {
                    string room = Value(assignment, "TenPhong", "");
                    string department = Value(assignment, "TenKhoa", "");
                    if (!string.IsNullOrWhiteSpace(room) && !rooms.Contains(room))
                        rooms.Add(room);
                    if (!string.IsNullOrWhiteSpace(department) && !departments.Contains(department))
                        departments.Add(department);
                }

                string roomValue = rooms.Count > 0 ? string.Join(", ", rooms) : "Chưa được phân công";
                string departmentValue = departments.Count > 0 ? string.Join(", ", departments) : "Chưa được phân công";
                AddField(card, "Phòng khám", roomValue, 20, 359);
                AddField(card, "Khoa", departmentValue, 20, 397);
            }

            var close = new ModernButton
            {
                Text = "Đóng",
                Location = new Point(205, hasRoomAssignments ? 553 : 477),
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(0, 145, 134),
                HoverColor = Color.FromArgb(0, 119, 111),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };

            Controls.AddRange(new Control[] { header, card, close });
            AcceptButton = close;
            FormClosed += (s, e) => avatar.Image?.Dispose();
        }

        private static void AddField(Control parent, string title, string value, int x, int top)
        {
            parent.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(x, top),
                Size = new Size(130, 23),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(83, 105, 121)
            });
            parent.Controls.Add(new Label
            {
                Text = value,
                Location = new Point(x + 135, top),
                Size = new Size(285, 23),
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(30, 48, 62)
            });
        }

        private static string Value(DataRow row, string column, string fallback = "")
        {
            object value = row[column];
            return value == DBNull.Value ? fallback : value.ToString() ?? fallback;
        }

        private static string RoleDisplayName(string role) => role switch
        {
            "Admin" => "Quản trị viên",
            "BacSi" => "Bác sĩ khám bệnh",
            "LeTan" => "Lễ tân",
            "ThuNgan" => "Thu ngân",
            _ => role
        };
    }
}
