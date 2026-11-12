using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public sealed class FormBenhNhanPortal : Form
    {
        private static readonly Color Navy = Color.FromArgb(17, 42, 63);
        private static readonly Color Teal = Color.FromArgb(0, 145, 134);
        private static readonly Color TealLight = Color.FromArgb(229, 246, 243);
        private static readonly Color PageBackground = Color.FromArgb(243, 246, 248);
        private static readonly Color TextColor = Color.FromArgb(30, 48, 62);
        private static readonly Color Muted = Color.FromArgb(82, 99, 112);

        private readonly TaiKhoanBenhNhan _patient;
        private readonly Panel _content;
        private readonly Label _pageTitle;
        private readonly Label _breadcrumb;
        private readonly Button _refreshButton;
        private readonly Button _homeButton;
        private readonly Button _appointmentsButton;
        private readonly Button _recordsButton;
        private readonly Button _billsButton;
        private readonly Button _assistantButton;
        private readonly Panel _profileCard;
        private readonly Button _languageViButton;
        private readonly Button _languageEnButton;

        public FormBenhNhanPortal()
        {
            _patient = SessionState.CurrentPatient
                ?? throw new InvalidOperationException("Chưa có bệnh nhân đăng nhập.");

            Text = "An Tâm - Cổng bệnh nhân";
            Size = new Size(1280, 820);
            MinimumSize = new Size(1080, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBackground;
            Font = new Font("Segoe UI", 10F);

            var sidebar = new Panel { Dock = DockStyle.Left, Width = 218, BackColor = Navy };
            var brand = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Navy };
            brand.Controls.Add(new Label
            {
                Text = "+",
                Location = new Point(24, 13),
                AutoSize = true,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 196, 176)
            });
            brand.Controls.Add(new Label
            {
                Text = "An Tâm",
                Location = new Point(60, 22),
                AutoSize = true,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.White
            });

            var patientCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Color.FromArgb(28, 58, 79),
                Padding = new Padding(12)
            };
            patientCard.Controls.Add(new Label
            {
                Text = _patient.HoTen,
                Location = new Point(12, 12),
                Size = new Size(185, 22),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true
            });
            patientCard.Controls.Add(new Label
            {
                Text = $"Mã hồ sơ: {_patient.MaBN}",
                Location = new Point(12, 37),
                Size = new Size(185, 20),
                ForeColor = Color.FromArgb(181, 202, 215),
                AutoEllipsis = true
            });

            var navigation = new Panel { Dock = DockStyle.Fill, BackColor = Navy };
            navigation.Controls.Add(new Label
            {
                Text = "CỔNG BỆNH NHÂN",
                Location = new Point(18, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(147, 169, 183)
            });

            _homeButton = CreateNavButton("⌂", "Trang chủ", 46);
            _appointmentsButton = CreateNavButton("▦", "Lịch hẹn khám", 92);
            _recordsButton = CreateNavButton("♡", "Hồ sơ khám bệnh", 138);
            _billsButton = CreateNavButton("▤", "Thanh toán viện phí", 184);
            _assistantButton = CreateNavButton("✦", "Trợ lý An Tâm", 230);
            _homeButton.Click += (s, e) => ShowHome();
            _appointmentsButton.Click += (s, e) => ShowAppointments();
            _recordsButton.Click += (s, e) => ShowHealthRecords();
            _billsButton.Click += (s, e) => ShowBills();
            _assistantButton.Click += (s, e) => ShowAssistant();
            navigation.Controls.AddRange(new Control[]
            {
                _homeButton, _appointmentsButton, _recordsButton, _billsButton, _assistantButton
            });

            var sidebarFooter = new Panel { Dock = DockStyle.Bottom, Height = 104, BackColor = Navy };
            _profileCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.FromArgb(28, 58, 79),
                Cursor = Cursors.Hand,
                Padding = new Padding(9, 7, 8, 5)
            };
            var profileIcon = new Label
            {
                Text = "👤",
                Location = new Point(9, 10),
                Size = new Size(36, 36),
                BackColor = Teal,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            var profileTitle = new Label
            {
                Text = "Hồ sơ cá nhân",
                Location = new Point(54, 7),
                Size = new Size(150, 21),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            var profileSubtitle = new Label
            {
                Text = "Xem hồ sơ của bạn",
                Location = new Point(54, 29),
                Size = new Size(150, 18),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.5F),
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            _profileCard.Controls.AddRange(new Control[] { profileIcon, profileTitle, profileSubtitle });
            _profileCard.Click += (s, e) => ShowPersonalProfile();
            foreach (Control control in _profileCard.Controls)
                control.Click += (s, e) => ShowPersonalProfile();
            _profileCard.MouseEnter += (s, e) => _profileCard.BackColor = Color.FromArgb(38, 74, 92);
            _profileCard.MouseLeave += (s, e) =>
            {
                if (_profileCard.BackColor != Color.FromArgb(0, 104, 99))
                    _profileCard.BackColor = Color.FromArgb(28, 58, 79);
            };
            var logoutButton = new Button
            {
                Text = "  Đăng xuất",
                Dock = DockStyle.Bottom,
                Height = 38,
                BackColor = Color.FromArgb(39, 68, 84),
                ForeColor = Color.FromArgb(250, 215, 215),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(10)
            };
            logoutButton.FlatAppearance.BorderSize = 0;
            logoutButton.Click += Logout;
            sidebarFooter.Controls.AddRange(new Control[] { logoutButton, _profileCard });

            sidebar.Controls.Add(navigation);
            sidebar.Controls.Add(sidebarFooter);
            sidebar.Controls.Add(patientCard);
            sidebar.Controls.Add(brand);

            var mainArea = new Panel { Dock = DockStyle.Fill, BackColor = PageBackground };
            var header = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.White };
            _breadcrumb = new Label
            {
                Text = "Bệnh nhân   ›   Trang chủ",
                Location = new Point(24, 12),
                AutoSize = true,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 9.5F)
            };
            _pageTitle = new Label
            {
                Text = "Trang chủ bệnh nhân",
                Location = new Point(24, 31),
                AutoSize = true,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold)
            };
            _refreshButton = CreateButton("Làm mới", primary: false);
            _refreshButton.Size = new Size(100, 32);
            _refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _refreshButton.Location = new Point(760, 19);
            _refreshButton.Click += (s, e) => RefreshCurrentPage();
            _languageViButton = CreateLanguageButton("VI");
            _languageEnButton = CreateLanguageButton("EN");
            _languageViButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _languageEnButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _languageViButton.Click += (s, e) => SetLanguage(false);
            _languageEnButton.Click += (s, e) => SetLanguage(true);
            var userLabel = new Label
            {
                Text = _patient.HoTen,
                Location = new Point(520, 25),
                Size = new Size(160, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoEllipsis = true
            };
            void PositionHeaderControls()
            {
                int rightEdge = header.ClientSize.Width - 16;
                _languageEnButton.Location = new Point(rightEdge - _languageEnButton.Width, 19);
                _languageViButton.Location = new Point(_languageEnButton.Left - 2 - _languageViButton.Width, 19);
                userLabel.Location = new Point(_languageViButton.Left - 8 - userLabel.Width, 25);
                _refreshButton.Location = new Point(Math.Max(_pageTitle.Right + 20, userLabel.Left - 8 - _refreshButton.Width), 19);
            }
            header.SizeChanged += (s, e) => PositionHeaderControls();
            header.Controls.AddRange(new Control[]
            {
                _breadcrumb, _pageTitle, _refreshButton, userLabel, _languageViButton, _languageEnButton
            });
            PositionHeaderControls();

            _content = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = PageBackground,
                Padding = new Padding(22, 18, 22, 18)
            };
            mainArea.Controls.Add(_content);
            mainArea.Controls.Add(header);
            Controls.Add(mainArea);
            Controls.Add(sidebar);
            UiText.Watch(_content);
            UiText.Watch(sidebar);
            UiText.Watch(header);
            UpdateLanguageButtons();

            ShowHome();
            UiText.Apply(this);
            UpdateLanguageButtons();
            FormClosed += (s, e) => SessionState.CurrentPatient = null;
        }

        private void Logout(object? sender, EventArgs e)
        {
            if (ShowMessage("Bạn muốn thoát tài khoản và quay lại màn hình đăng nhập?",
                    "Đăng xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            SessionState.CurrentPatient = null;
            Close();
        }

        private DialogResult ShowMessage(
            string message,
            string caption,
            MessageBoxButtons buttons = MessageBoxButtons.OK,
            MessageBoxIcon icon = MessageBoxIcon.Information)
            => MessageBox.Show(this, UiText.Translate(message), UiText.Translate(caption), buttons, icon);

        private Button CreateNavButton(string icon, string text, int top)
        {
            var button = new Button
            {
                Text = $"  {icon}   {text}",
                Location = new Point(9, top),
                Size = new Size(198, 40),
                BackColor = Navy,
                ForeColor = Color.FromArgb(207, 221, 229),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 74, 92);
            return button;
        }

        private static Button CreateLanguageButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Size = new Size(42, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Muted,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(220, 226, 230);
            button.FlatAppearance.BorderSize = 1;
            return button;
        }

        private void SetLanguage(bool english)
        {
            UiText.IsEnglish = english;
            UiText.Apply(this);
            UpdateLanguageButtons();
        }

        private void UpdateLanguageButtons()
        {
            _languageViButton.BackColor = UiText.IsEnglish ? Color.White : Teal;
            _languageViButton.ForeColor = UiText.IsEnglish ? Muted : Color.White;
            _languageViButton.FlatAppearance.BorderColor = UiText.IsEnglish ? Color.FromArgb(220, 226, 230) : Teal;
            _languageEnButton.BackColor = UiText.IsEnglish ? Teal : Color.White;
            _languageEnButton.ForeColor = UiText.IsEnglish ? Color.White : Muted;
            _languageEnButton.FlatAppearance.BorderColor = UiText.IsEnglish ? Teal : Color.FromArgb(220, 226, 230);
        }

        private void SetPage(string title, Control activeControl)
        {
            UiText.SetText(_pageTitle, title);
            UiText.SetText(_breadcrumb, $"Bệnh nhân   ›   {title}");
            foreach (var button in new[]
                     {
                         _homeButton, _appointmentsButton, _recordsButton, _billsButton, _assistantButton
                     })
            {
                button.BackColor = button == activeControl
                    ? Color.FromArgb(0, 104, 99)
                    : Navy;
                button.ForeColor = button == activeControl ? Color.White : Color.FromArgb(207, 221, 229);
            }
            _profileCard.BackColor = _profileCard == activeControl
                ? Color.FromArgb(0, 104, 99)
                : Color.FromArgb(28, 58, 79);
            foreach (Control control in _content.Controls)
                control.Dispose();
            _content.Controls.Clear();
        }

        private void RefreshCurrentPage()
        {
            if (_homeButton.BackColor != Navy)
                ShowHome();
            else if (_appointmentsButton.BackColor != Navy)
                ShowAppointments();
            else if (_recordsButton.BackColor != Navy)
                ShowHealthRecords();
            else if (_billsButton.BackColor != Navy)
                ShowBills();
            else if (_profileCard.BackColor == Color.FromArgb(0, 104, 99))
                ShowPersonalProfile();
            else
                ShowAssistant();
        }

        private void ShowPersonalProfile()
        {
            SetPage("Hồ sơ cá nhân", _profileCard);
            try
            {
                DataRow profile = ClinicDAL.LayBenhNhanTheoMa(_patient.MaBN)
                    ?? throw new InvalidOperationException("Không tìm thấy hồ sơ bệnh nhân.");
                var page = CreatePageFlow();
                var card = CreateCard(360);
                AddCardAccent(card);
                card.Controls.Add(CreateLabel("Thông tin cá nhân", 20, 16, 500, 28, 16, true, Navy));
                var profilePanel = new Panel
                {
                    Location = new Point(20, 58),
                    Size = new Size(card.ClientSize.Width - 40, 290),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    AutoScroll = true,
                    BackColor = Color.White
                };
                AddProfileField(profilePanel, "Họ và tên", Value(profile, "HoTen"), 15);
                AddProfileField(profilePanel, "Mã hồ sơ", Value(profile, "MaBN"), 55);
                AddProfileField(profilePanel, "Ngày sinh", FormatValue(profile["NgaySinh"]), 95);
                AddProfileField(profilePanel, "Giới tính", Value(profile, "GioiTinh"), 135);
                AddProfileField(profilePanel, "Số điện thoại", FormatValue(profile["SoDienThoai"]), 175);
                AddProfileField(profilePanel, "Email nhận thông báo", FormatValue(profile["Email"]), 215);
                AddProfileField(profilePanel, "Địa chỉ", FormatValue(profile["DiaChi"]), 255);
                AddProfileField(profilePanel, "Tiền sử bệnh / dị ứng", FormatValue(profile["TienSuBenh"]), 295);
                card.Controls.Add(profilePanel);
                page.Controls.Add(card);
                _content.Controls.Add(page);
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ShowHome()
        {
            SetPage("Trang chủ bệnh nhân", _homeButton);
            try
            {
                DataRow profile = ClinicDAL.LayBenhNhanTheoMa(_patient.MaBN)
                    ?? throw new InvalidOperationException("Không tìm thấy hồ sơ bệnh nhân.");
                DataTable appointments = ClinicDAL.LayLichHenBenhNhan(_patient.MaBN);
                DataTable bills = ClinicDAL.LayHoaDonBenhNhan(_patient.MaBN);
                DataTable history = ClinicDAL.LayLichSuKhamBenhNhan(_patient.MaBN);
                DataRow? upcoming = appointments.AsEnumerable()
                    .Where(row => row["TrangThai"].ToString() != "Đã hủy" &&
                                  row.Field<DateTime>("NgayHen") >= DateTime.Now)
                    .OrderBy(row => row.Field<DateTime>("NgayHen"))
                    .FirstOrDefault();
                DataRow? unpaid = bills.AsEnumerable()
                    .FirstOrDefault(row => row["TrangThai"].ToString() != "Đã thanh toán");

                var page = CreatePageFlow();

                // ── Banner chào mừng kèm Giờ làm việc của phòng khám ──
                var intro = CreateCard(140);
                AddCardAccent(intro);
                intro.Controls.Add(CreateLabel($"Xin chào, {_patient.HoTen}", 22, 14, 600, 32, 19, true, Navy));
                intro.Controls.Add(CreateLabel("Chúc bạn một ngày khỏe mạnh! Theo dõi lịch trực bác sĩ và lịch hẹn của bạn tại đây.",
                    22, 50, 600, 22, 10F, false, Muted));

                // Badge hiển thị giờ mở cửa phòng khám
                var pnlHoursBadge = new Panel
                {
                    Location = new Point(Math.Max(640, intro.ClientSize.Width - 340), 14),
                    Size = new Size(320, 110),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BackColor = Color.FromArgb(240, 253, 250),
                    Padding = new Padding(12)
                };
                pnlHoursBadge.Paint += (s, e) =>
                {
                    using var p = new Pen(Color.FromArgb(153, 246, 228), 1);
                    e.Graphics.DrawRectangle(p, 0, 0, pnlHoursBadge.Width - 1, pnlHoursBadge.Height - 1);
                };
                pnlHoursBadge.Controls.Add(new Label
                {
                    Text = "⏰ GIỜ HOẠT ĐỘNG PHÒNG KHÁM",
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(13, 148, 136),
                    Location = new Point(10, 8),
                    AutoSize = true
                });
                pnlHoursBadge.Controls.Add(new Label
                {
                    Text = "• Thứ 2 - Thứ 7: 07:30 - 17:00 (Khám trong ngày)\n• Ca tối / Khám hẹn: 17:30 - 20:00\n• Chủ Nhật: 08:00 - 12:00 (Theo lịch hẹn trước)",
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(55, 65, 81),
                    Location = new Point(10, 32),
                    Size = new Size(300, 70)
                });
                intro.Controls.Add(pnlHoursBadge);

                var bookButton = CreateButton("＋  Đặt lịch khám trước", true);
                bookButton.Location = new Point(22, 86);
                bookButton.Size = new Size(185, 36);
                bookButton.Click += (s, e) =>
                {
                    if (OpenBookingDialog())
                        ShowAppointments();
                };
                intro.Controls.Add(bookButton);
                page.Controls.Add(intro);

                // ── KHỐI DANH SÁCH BÁC SĨ TRỰC HÔM NAY ──
                DataTable dtDoctorsToday = ClinicDAL.LayDanhSachBacSiTrucHomNayPortal();
                var cardShiftsToday = CreateCard(235);
                AddCardAccent(cardShiftsToday);
                cardShiftsToday.Controls.Add(CreateLabel("🩺 BÁC SĨ TRỰC HÔM NAY TẠI PHÒNG KHÁM", 20, 14, 450, 24, 11.5F, true, Navy));
                cardShiftsToday.Controls.Add(CreateLabel("Nếu bác sĩ đang trong ca trực, bạn có thể đến đăng ký khám ngay trong ngày hoặc đặt lịch hẹn.",
                    20, 40, 750, 20, 9F, false, Muted));

                var pnlDoctorList = new FlowLayoutPanel
                {
                    Location = new Point(16, 68),
                    Size = new Size(cardShiftsToday.ClientSize.Width - 32, 150),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    AutoScroll = true,
                    WrapContents = false,
                    BackColor = Color.Transparent
                };

                if (dtDoctorsToday.Rows.Count == 0)
                {
                    pnlDoctorList.Controls.Add(CreateLabel("Hôm nay phòng khám chưa có ca trực của bác sĩ hoặc đang ngoài giờ tiếp nhận. Vui lòng đặt lịch hẹn trước.", 10, 10, 800, 30, 9.5F, false, Muted));
                }
                else
                {
                    foreach (DataRow drDoc in dtDoctorsToday.Rows)
                    {
                        string docName = drDoc["HoTen"]?.ToString() ?? "";
                        string docKhoa = drDoc["ChuyenKhoa"]?.ToString() ?? "";
                        string docPhong = drDoc["TenPhong"]?.ToString() ?? "";
                        string gioTruc = $"{drDoc["GioBatDau"]} - {drDoc["GioKetThuc"]}";
                        bool dangTruc = Convert.ToBoolean(drDoc["DangTrongCa"]);
                        string maDoc = drDoc["MaNV"]?.ToString() ?? "";

                        var pnlDocItem = new Panel
                        {
                            Size = new Size(250, 135),
                            Margin = new Padding(0, 0, 14, 0),
                            BackColor = dangTruc ? Color.FromArgb(240, 253, 250) : Color.FromArgb(249, 250, 251),
                            Padding = new Padding(10)
                        };
                        pnlDocItem.Paint += (s, e) =>
                        {
                            using var pen = new Pen(dangTruc ? Color.FromArgb(13, 148, 136) : Color.FromArgb(229, 231, 235), 1.2f);
                            e.Graphics.DrawRectangle(pen, 0, 0, pnlDocItem.Width - 1, pnlDocItem.Height - 1);
                        };

                        var lblStatusBadge = new Label
                        {
                            Text = dangTruc ? "● Đang có mặt" : "○ Chờ vào ca",
                            ForeColor = dangTruc ? Color.FromArgb(13, 148, 136) : Color.FromArgb(156, 163, 175),
                            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                            Location = new Point(10, 8),
                            AutoSize = true
                        };
                        var lblDocName = new Label
                        {
                            Text = docName,
                            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                            ForeColor = TextColor,
                            Location = new Point(10, 28),
                            Size = new Size(230, 20),
                            AutoEllipsis = true
                        };
                        var lblDocSpec = new Label
                        {
                            Text = $"{docKhoa} • {docPhong}",
                            Font = new Font("Segoe UI", 8.5F),
                            ForeColor = Muted,
                            Location = new Point(10, 50),
                            Size = new Size(230, 18),
                            AutoEllipsis = true
                        };
                        var lblDocHours = new Label
                        {
                            Text = $"Ca trực: {gioTruc}",
                            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                            ForeColor = Color.FromArgb(30, 64, 175),
                            Location = new Point(10, 70),
                            AutoSize = true
                        };

                        var btnQuickBook = new Button
                        {
                            Text = dangTruc ? "Khám hôm nay" : "Đặt lịch hẹn",
                            Size = new Size(228, 28),
                            Location = new Point(10, 95),
                            FlatStyle = FlatStyle.Flat,
                            BackColor = dangTruc ? Teal : Color.White,
                            ForeColor = dangTruc ? Color.White : Teal,
                            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                            Cursor = Cursors.Hand
                        };
                        btnQuickBook.FlatAppearance.BorderColor = Teal;
                        btnQuickBook.Click += (s, e) =>
                        {
                            if (OpenBookingDialog(preferredDoctorMaNV: maDoc, defaultDateToday: dangTruc))
                                ShowAppointments();
                        };

                        pnlDocItem.Controls.AddRange(new Control[] { lblStatusBadge, lblDocName, lblDocSpec, lblDocHours, btnQuickBook });
                        pnlDoctorList.Controls.Add(pnlDocItem);
                    }
                }

                cardShiftsToday.Controls.Add(pnlDoctorList);
                page.Controls.Add(cardShiftsToday);

                var columns = CreateColumns(540, 60);
                var left = CreateColumn();
                var right = CreateColumn();

                var appointmentCard = CreateCard(248);
                AddCardAccent(appointmentCard);
                appointmentCard.Controls.Add(CreateLabel("LỊCH HẸN SẮP TỚI", 20, 16, 360, 24, 12, true, Navy));
                if (upcoming != null)
                {
                    appointmentCard.Controls.Add(CreateTag(upcoming["TrangThaiDuyet"].ToString() ?? "Chờ xác nhận", 20, 48));
                    appointmentCard.Controls.Add(CreateLabel(
                        ((DateTime)upcoming["NgayHen"]).ToString("dd/MM/yyyy  •  HH:mm"),
                        20, 84, 440, 30, 17, true, Teal));
                    appointmentCard.Controls.Add(CreateLabel($"Bác sĩ  ·  {Value(upcoming, "TenBacSi")}", 20, 122, 440, 24, 10, true, TextColor));
                    appointmentCard.Controls.Add(CreateLabel($"Chuyên khoa  ·  {Value(upcoming, "ChuyenKhoa")}", 20, 151, 440, 22, 10, false, Muted));
                    appointmentCard.Controls.Add(CreateLabel($"Phòng khám  ·  {Value(upcoming, "TenPhong")}", 20, 178, 440, 22, 10, false, Muted));
                }
                else
                {
                    appointmentCard.Controls.Add(CreateLabel("Bạn chưa có lịch hẹn sắp tới.", 20, 72, 350, 24, 10, false, Muted));
                }
                var seeAppointments = CreateButton("Xem lịch hẹn", false);
                seeAppointments.Location = new Point(20, 208);
                seeAppointments.Size = new Size(145, 32);
                seeAppointments.Click += (s, e) => ShowAppointments();
                appointmentCard.Controls.Add(seeAppointments);
                left.Controls.Add(appointmentCard);

                var historyCard = CreateCard(232);
                AddCardAccent(historyCard);
                historyCard.Controls.Add(CreateLabel("HỒ SƠ VÀ TÀI LIỆU MỚI", 20, 16, 400, 24, 12, true, Navy));
                historyCard.Controls.Add(CreateLabel($"Số lượt khám đã ghi nhận  ·  {history.Rows.Count}", 20, 52, 420, 22, 10, false, Muted));
                historyCard.Controls.Add(CreateLabel($"Ngày sinh: {FormatValue(profile["NgaySinh"])}   •   Giới tính: {Value(profile, "GioiTinh")}",
                    20, 82, 460, 24, 10, false, TextColor));
                historyCard.Controls.Add(CreateLabel($"Điện thoại: {FormatValue(profile["SoDienThoai"])}", 20, 112, 400, 22, 10, false, TextColor));
                var historyButton = CreateButton("Mở hồ sơ khám bệnh", false);
                historyButton.Location = new Point(20, 158);
                historyButton.Size = new Size(180, 34);
                historyButton.Click += (s, e) => ShowHealthRecords();
                historyCard.Controls.Add(historyButton);
                left.Controls.Add(historyCard);

                var billCard = CreateCard(248);
                AddCardAccent(billCard);
                billCard.Controls.Add(CreateLabel("HÓA ĐƠN / VIỆN PHÍ", 20, 16, 360, 28, 12, true, Navy));
                if (unpaid != null)
                {
                    billCard.Controls.Add(CreateTag("Chưa thanh toán", 20, 50, Color.FromArgb(255, 245, 219), Color.FromArgb(153, 104, 0)));
                    billCard.Controls.Add(CreateLabel($"{Convert.ToDecimal(unpaid["TongTien"]):N0} đ", 20, 88, 320, 34, 21, true, Teal));
                    billCard.Controls.Add(CreateLabel($"Hóa đơn  ·  {Value(unpaid, "MaHD")}", 20, 128, 300, 22, 10, false, Muted));
                }
                else
                {
                    var noUnpaidBill = CreateWrappedLabel(
                        "Bạn không có hóa đơn chưa thanh toán.",
                        20, 68, 330, 10, false, Muted);
                    billCard.Controls.Add(noUnpaidBill);
                }
                var billsButton = CreateButton("Xem viện phí", true);
                billsButton.Location = new Point(20, 186);
                billsButton.Size = new Size(145, 34);
                billsButton.Click += (s, e) => ShowBills();
                billCard.Controls.Add(billsButton);
                right.Controls.Add(billCard);

                var reminderCard = CreateCard(252);
                AddCardAccent(reminderCard);
                reminderCard.Controls.Add(CreateLabel("NHẮC NHỞ SỨC KHỎE", 20, 16, 360, 28, 12, true, Navy));
                var reminderText = CreateWrappedLabel(
                    "Theo dõi lịch tái khám theo hướng dẫn của bác sĩ. Nếu triệu chứng trở nặng, hãy liên hệ cơ sở y tế.",
                    20, 58, 350, 10, false, Muted);
                reminderCard.Controls.Add(reminderText);
                var supportButton = CreateButton("Mở trợ lý An Tâm", false);
                supportButton.Location = new Point(20, 164);
                supportButton.Size = new Size(180, 34);
                supportButton.Click += (s, e) => ShowAssistant();
                reminderCard.Controls.Add(supportButton);
                right.Controls.Add(reminderCard);

                columns.Controls.Add(left, 0, 0);
                columns.Controls.Add(right, 1, 0);
                page.Controls.Add(columns);
                _content.Controls.Add(page);
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ShowAppointments()
        {
            SetPage("Lịch hẹn", _appointmentsButton);
            try
            {
                var page = CreatePageFlow();
                var toolbar = CreateCard(68);
                toolbar.Controls.Add(CreateLabel("Quản lý và theo dõi lịch hẹn khám bệnh của bạn", 16, 14, 480, 24, 12, true, TextColor));
                var bookButton = CreateButton("＋  Đặt lịch hẹn mới", true);
                bookButton.Location = new Point(toolbar.ClientSize.Width - 170, 15);
                bookButton.Size = new Size(155, 34);
                bookButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                bookButton.Click += (s, e) =>
                {
                    if (OpenBookingDialog())
                        ShowAppointments();
                };
                toolbar.Controls.Add(bookButton);
                page.Controls.Add(toolbar);

                // ── BẢNG DANH SÁCH LỊCH HẸN ──
                var listCard = CreateCard(270);
                listCard.Controls.Add(CreateLabel("Lịch hẹn của tôi (Click vào một dòng để xem chi tiết bên dưới)", 16, 12, 600, 22, 10.5F, true, TextColor));
                var grid = CreateGrid(16, 40, 900, 215);
                void ConfigureAppointmentColumns()
                {
                    HideColumns(grid, "MaBacSi", "TrangThai", "TenKhoa");
                    if (grid.Columns.Contains("TrangThaiDuyet"))
                    {
                        grid.Columns["TrangThaiDuyet"]!.HeaderText = "Trạng thái";
                        grid.Columns["TrangThaiDuyet"]!.DisplayIndex = grid.Columns.Count - 1;
                    }
                }
                grid.DataBindingComplete += (s, e) => ConfigureAppointmentColumns();
                var appointments = ClinicDAL.LayLichHenBenhNhan(_patient.MaBN);
                grid.DataSource = appointments;
                grid.Size = new Size(Math.Max(0, listCard.ClientSize.Width - 32), 215);
                ConfigureAppointmentColumns();
                listCard.SizeChanged += (s, e) => grid.Size = new Size(Math.Max(0, listCard.ClientSize.Width - 32), 215);
                listCard.Controls.Add(grid);
                page.Controls.Add(listCard);

                // ── THẺ CHI TIẾT LỊCH HẸN ĐƯỢC CHỌN (BÊN DƯỚI BẢNG) ──
                var detailCard = CreateCard(240);
                AddCardAccent(detailCard);
                detailCard.Controls.Add(CreateLabel("CHI TIẾT LỊCH HẸN ĐANG CHỌN", 20, 14, 400, 22, 11, true, Navy));

                var lblStatusTag = CreateTag("Chờ xác nhận", 20, 42);
                var lblTimeLarge = CreateLabel("", 20, 72, 450, 28, 15, true, Teal);
                var lblDoctorLine = CreateLabel("Chọn một lịch hẹn ở bảng trên để xem chi tiết.", 20, 104, 550, 20, 9.5F, true, TextColor);
                var lblRoomLine = CreateLabel("", 20, 126, 550, 20, 9F, false, Muted);
                var lblReasonLine = CreateLabel("", 20, 148, 550, 20, 9F, false, Color.FromArgb(75, 85, 99));

                // Dòng lưu ý quy định sửa/đổi lịch
                var lblModifyNotice = new Label
                {
                    Text = "💡 Lưu ý: Chỉ lịch ở trạng thái 'Chờ xác nhận' mới được đổi giờ hoặc sửa lý do. Lịch đã duyệt nếu cần đổi vui lòng liên hệ phòng khám.",
                    Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(107, 114, 128),
                    Location = new Point(20, 172),
                    Size = new Size(580, 40)
                };

                // Các nút thao tác bám góc phải
                var editButton = CreateButton("✏  Đổi giờ / Sửa lịch", false);
                editButton.Location = new Point(Math.Max(600, detailCard.ClientSize.Width - 280), 55);
                editButton.Size = new Size(160, 34);
                editButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

                var cancelButton = CreateButton("✖  Hủy lịch hẹn này", false);
                cancelButton.Location = new Point(Math.Max(600, detailCard.ClientSize.Width - 280), 98);
                cancelButton.Size = new Size(160, 34);
                cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                cancelButton.ForeColor = Color.Firebrick;

                detailCard.Controls.AddRange(new Control[] { lblStatusTag, lblTimeLarge, lblDoctorLine, lblRoomLine, lblReasonLine, lblModifyNotice, editButton, cancelButton });

                void UpdateAppointmentDetailSelection()
                {
                    if (grid.CurrentRow?.DataBoundItem is not DataRowView rowView)
                    {
                        lblStatusTag.Visible = false;
                        lblTimeLarge.Text = "";
                        lblDoctorLine.Text = "Chọn một lịch hẹn ở bảng trên để xem chi tiết.";
                        lblRoomLine.Text = "";
                        lblReasonLine.Text = "";
                        lblModifyNotice.Text = "";
                        editButton.Enabled = false;
                        cancelButton.Enabled = false;
                        return;
                    }

                    DataRow row = rowView.Row;
                    string rawStatus = row.Table.Columns.Contains("TrangThai") ? row["TrangThai"]?.ToString() ?? "" : "";
                    string displayStatus = row.Table.Columns.Contains("TrangThaiDuyet") ? row["TrangThaiDuyet"]?.ToString() ?? "" : rawStatus;
                    DateTime ngayHen = row.Field<DateTime>("NgayHen");
                    string doctorName = Value(row, "TenBacSi");
                    string chuyenKhoa = Value(row, "ChuyenKhoa");
                    string phong = Value(row, "TenPhong");
                    string khoa = Value(row, "TenKhoa");
                    string reason = Value(row, "LyDoKham");
                    string maLH = Value(row, "MaLichHen");

                    lblStatusTag.Visible = true;
                    lblStatusTag.Text = string.IsNullOrWhiteSpace(displayStatus) ? rawStatus : displayStatus;
                    if (rawStatus == "Đã xác nhận" || displayStatus == "Đã duyệt")
                    {
                        lblStatusTag.BackColor = Color.FromArgb(220, 252, 231);
                        lblStatusTag.ForeColor = Color.FromArgb(22, 101, 52);
                    }
                    else if (rawStatus == "Bị từ chối" || rawStatus == "Đã hủy" || displayStatus == "Bị từ chối" || displayStatus == "Đã hủy")
                    {
                        lblStatusTag.BackColor = Color.FromArgb(254, 226, 226);
                        lblStatusTag.ForeColor = Color.FromArgb(185, 28, 28);
                    }
                    else
                    {
                        lblStatusTag.BackColor = Color.FromArgb(254, 243, 199);
                        lblStatusTag.ForeColor = Color.FromArgb(180, 83, 9);
                    }

                    lblTimeLarge.Text = $"⏰ {ngayHen:dd/MM/yyyy  •  HH:mm}   (Mã lịch: #{maLH})";
                    lblDoctorLine.Text = $"👨‍⚕️ Bác sĩ: {doctorName}  •  Chuyên khoa: {chuyenKhoa}";
                    lblRoomLine.Text = $"🏥 Địa điểm khám: {phong} ({khoa})";
                    lblReasonLine.Text = string.IsNullOrWhiteSpace(reason) ? "📝 Lý do khám: Khám định kỳ" : $"📝 Lý do khám: {reason}";

                    // Kiểm tra trạng thái chờ duyệt (hỗ trợ cả giá trị thô 'Chờ xác nhận' và giá trị hiển thị 'Chờ duyệt')
                    bool isPending = (rawStatus == "Chờ xác nhận" || displayStatus == "Chờ duyệt") && ngayHen > DateTime.Now;
                    editButton.Enabled = isPending;
                    cancelButton.Enabled = rawStatus != "Đã hủy" && rawStatus != "Bị từ chối" && rawStatus != "Đã đến";

                    if (isPending)
                    {
                        lblModifyNotice.ForeColor = Color.FromArgb(13, 148, 136);
                        lblModifyNotice.Text = "✓ Đơn đang chờ phòng khám duyệt: Bạn CÓ THỂ bấm 'Đổi giờ / Sửa' ở góc phải để đổi ngày giờ hoặc cập nhật lý do khám.";
                    }
                    else if (rawStatus == "Đã xác nhận" || displayStatus == "Đã duyệt")
                    {
                        lblModifyNotice.ForeColor = Color.FromArgb(180, 83, 9);
                        lblModifyNotice.Text = "🔒 ĐƠN ĐÃ ĐƯỢC DUYỆT: Lịch hẹn và bác sĩ đã được chốt, KHÔNG THỂ thay đổi giờ hay lý do khám. Nếu cần đổi giờ, vui lòng liên hệ phòng khám hoặc hủy lịch để đặt lại.";
                    }
                    else
                    {
                        lblModifyNotice.ForeColor = Color.FromArgb(107, 114, 128);
                        lblModifyNotice.Text = "Lịch hẹn này đã qua giờ hẹn, đã khám hoặc không còn hiệu lực.";
                    }
                }

                grid.SelectionChanged += (s, e) => UpdateAppointmentDetailSelection();

                editButton.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is not DataRowView row) return;
                    string rStat = row.Row.Table.Columns.Contains("TrangThai") ? row.Row["TrangThai"]?.ToString() ?? "" : "";
                    string dStat = row.Row.Table.Columns.Contains("TrangThaiDuyet") ? row.Row["TrangThaiDuyet"]?.ToString() ?? "" : "";
                    if (rStat != "Chờ xác nhận" && dStat != "Chờ duyệt")
                    {
                        ShowMessage("Đơn khám này ĐÃ ĐƯỢC DUYỆT nên không thể thay đổi giờ hoặc lý do khám.\r\n\r\nNếu có nhu cầu thay đổi, bạn vui lòng liên hệ trực tiếp phòng khám hoặc hủy lịch hẹn này để đăng ký lịch mới.",
                            "Không thể sửa lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (OpenBookingDialog(row.Row))
                        ShowAppointments();
                };

                cancelButton.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is not DataRowView row) return;
                    if (ShowMessage("Bạn chắc chắn muốn hủy lịch hẹn này?", "Xác nhận hủy",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                    try
                    {
                        if (!ClinicDAL.HuyLichHenBenhNhan(Convert.ToInt32(row.Row["MaLichHen"]), _patient.MaBN))
                            throw new InvalidOperationException("Lịch hẹn này không còn ở trạng thái có thể hủy.");
                        ShowAppointments();
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(ex.Message, "Không thể hủy lịch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                page.Controls.Add(detailCard);
                _content.Controls.Add(page);

                if (grid.Rows.Count > 0)
                {
                    grid.Rows[0].Selected = true;
                    UpdateAppointmentDetailSelection();
                }
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ShowBookingList()
        {
            ShowAppointments();
        }

        private bool OpenBookingDialog(DataRow? appointment = null, string? preferredDoctorMaNV = null, bool defaultDateToday = false)
        {
            try
            {
                DataTable doctors = ClinicDAL.LayDanhSachBacSiDatLich();
                using var dialog = new Form
                {
                    Text = appointment == null ? "Đặt lịch khám" : "Sửa lịch hẹn",
                    Size = new Size(620, 680),
                    MinimumSize = new Size(600, 660),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White,
                    Font = new Font("Segoe UI", 9F)
                };

                var heading = new Label
                {
                    Text = appointment == null ? "ĐĂNG KÝ LỊCH KHÁM" : "CẬP NHẬT LỊCH HẸN",
                    Dock = DockStyle.Top,
                    Height = 56,
                    Padding = new Padding(18, 14, 0, 0),
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = TextColor
                };
                var fields = new TableLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(18, 8, 18, 8),
                    ColumnCount = 2,
                    RowCount = 7,
                    BackColor = Color.White
                };
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 68)); // Ca trực & phòng
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 85));
                fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var specialty = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = Font
                };
                string[] specialties = ClinicDAL.LayDanhSachKhoa().AsEnumerable()
                    .Select(row => row["TenKhoa"].ToString() ?? "")
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToArray();
                specialty.Items.AddRange(specialties.Cast<object>().ToArray());

                var doctor = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = Font
                };

                // Label hiển thị thông tin ca trực & phòng khám của bác sĩ
                var lblDoctorShiftInfo = new Label
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(13, 148, 136),
                    BackColor = Color.FromArgb(240, 253, 250),
                    Padding = new Padding(8, 6, 8, 6),
                    Text = "Chọn bác sĩ để xem lịch trực và phòng làm việc."
                };

                var date = new DateTimePicker
                {
                    Dock = DockStyle.Left,
                    Width = 190,
                    MinDate = DateTime.Today,
                    MaxDate = DateTime.Today.AddMonths(3),
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "dd/MM/yyyy"
                };
                if (appointment != null)
                {
                    DateTime existingDate = appointment.Field<DateTime>("NgayHen").Date;
                    if (existingDate >= date.MinDate && existingDate <= date.MaxDate)
                        date.Value = existingDate;
                }
                else if (defaultDateToday)
                {
                    date.Value = DateTime.Today;
                }

                var time = new ComboBox
                {
                    Dock = DockStyle.Left,
                    Width = 190,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = Font
                };
                void LoadTimeSlots(DateTime? preferred = null)
                {
                    time.Items.Clear();
                    if (doctor.SelectedValue == null)
                        return;
                    DataTable availableSlots = ClinicDAL.LayKhungGioKhaDung(
                        doctor.SelectedValue.ToString() ?? "",
                        date.Value.Date,
                        appointment == null ? null : Convert.ToInt32(appointment["MaLichHen"]));
                    foreach (DataRow slot in availableSlots.Rows)
                    {
                        time.Items.Add(Convert.ToDateTime(slot["NgayGio"]).ToString("HH:mm"));
                    }
                    string preferredText = preferred?.ToString("HH:mm") ?? "";
                    if (time.Items.Contains(preferredText))
                        time.SelectedItem = preferredText;
                    else if (time.Items.Count > 0)
                        time.SelectedIndex = 0;
                }

                void UpdateDoctorScheduleDisplay()
                {
                    if (doctor.SelectedValue is string maBS && !string.IsNullOrEmpty(maBS))
                    {
                        string lichTuan = ClinicDAL.MoTaLichTrucBacSi(maBS);
                        var (phong, khoa, caHienTai) = ClinicDAL.LayThongTinCaTrucBacSi(maBS);
                        lblDoctorShiftInfo.Text = $"📍 {phong} ({khoa})\n⏰ Ca trực hôm nay: {caHienTai}\n📅 Lịch tuần: {lichTuan}";
                    }
                    else
                    {
                        lblDoctorShiftInfo.Text = specialty.SelectedItem is string selectedSpecialty
                            && !string.IsNullOrWhiteSpace(selectedSpecialty)
                            && doctor.Items.Count == 0
                                ? "Chuyên khoa này hiện chưa có bác sĩ được phân công để đặt lịch."
                                : "Chọn bác sĩ để xem lịch trực và phòng làm việc.";
                    }
                }

                var reason = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    MaxLength = 255,
                    ScrollBars = ScrollBars.Vertical,
                    Font = Font
                };
                var buttons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.RightToLeft,
                    WrapContents = false,
                    Padding = new Padding(0, 8, 0, 0)
                };
                var save = CreateButton(appointment == null ? "Gửi yêu cầu đặt lịch" : "Lưu thay đổi", true);
                save.Size = new Size(175, 36);
                var cancel = new Button
                {
                    Text = "Đóng",
                    Size = new Size(90, 36),
                    DialogResult = DialogResult.Cancel,
                    FlatStyle = FlatStyle.Flat
                };
                buttons.Controls.AddRange(new Control[] { save, cancel });

                void AddField(string label, Control control, int row)
                {
                    fields.Controls.Add(new Label
                    {
                        Text = label,
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = TextColor
                    }, 0, row);
                    fields.Controls.Add(control, 1, row);
                }
                var dateTimePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0) };
                dateTimePanel.Controls.AddRange(new Control[]
                {
                    date,
                    new Label { Text = "  Giờ:", AutoSize = true, Padding = new Padding(6, 6, 2, 0), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = TextColor },
                    time
                });

                var txtEmail = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Font = Font,
                    PlaceholderText = "Nhập email nhận xác nhận lịch & biên lai..."
                };
                txtEmail.Text = ClinicDAL.LayEmailBenhNhanTheoMa(_patient.MaBN) ?? _patient.Email ?? "";

                AddField("Chuyên khoa", specialty, 0);
                AddField("Bác sĩ", doctor, 1);
                AddField("Lịch trực & Phòng", lblDoctorShiftInfo, 2);
                AddField("Ngày & Giờ *", dateTimePanel, 3);
                AddField("Email thông báo", txtEmail, 4);
                AddField("Lý do khám", reason, 5);
                fields.Controls.Add(buttons, 1, 6);

                void LoadDoctors()
                {
                    string selectedSpecialty = specialty.SelectedItem?.ToString() ?? "";
                    DataTable filtered = doctors.Clone();
                    foreach (DataRow row in doctors.Rows)
                        if (string.Equals(row["ChuyenKhoa"].ToString(), selectedSpecialty,
                                StringComparison.OrdinalIgnoreCase))
                            filtered.ImportRow(row);
                    filtered.Columns.Add("TenHienThi", typeof(string));
                    foreach (DataRow row in filtered.Rows)
                        row["TenHienThi"] = $"{row["HoTen"]} • {row["TenPhong"]}";
                    doctor.DataSource = filtered;
                    doctor.DisplayMember = "TenHienThi";
                    doctor.ValueMember = "MaNV";
                    doctor.Enabled = filtered.Rows.Count > 0;
                    save.Enabled = filtered.Rows.Count > 0;
                }

                // Nếu có chỉ định bác sĩ ưu tiên
                string? targetDoctorMaBS = appointment != null ? appointment["MaBacSi"]?.ToString() : preferredDoctorMaNV;
                if (!string.IsNullOrEmpty(targetDoctorMaBS))
                {
                    DataRow? foundDoc = doctors.Rows.Cast<DataRow>().FirstOrDefault(r => r["MaNV"].ToString() == targetDoctorMaBS);
                    if (foundDoc != null)
                    {
                        string targetSpec = foundDoc["ChuyenKhoa"].ToString() ?? "";
                        int specIdx = Array.FindIndex(specialties, v => string.Equals(v, targetSpec, StringComparison.OrdinalIgnoreCase));
                        if (specIdx >= 0) specialty.SelectedIndex = specIdx;
                    }
                }
                else if (specialty.Items.Count > 0)
                {
                    string currentSpecialty = appointment?["ChuyenKhoa"]?.ToString() ?? "";
                    int currentIndex = Array.FindIndex(specialties,
                        value => string.Equals(value, currentSpecialty, StringComparison.OrdinalIgnoreCase));
                    specialty.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
                }

                specialty.SelectedIndexChanged += (s, e) =>
                {
                    LoadDoctors();
                    UpdateDoctorScheduleDisplay();
                };
                if (specialty.SelectedIndex >= 0)
                    LoadDoctors();

                if (!string.IsNullOrEmpty(targetDoctorMaBS) && doctor.Items.Count > 0)
                    doctor.SelectedValue = targetDoctorMaBS;

                UpdateDoctorScheduleDisplay();

                if (appointment != null)
                {
                    reason.Text = appointment["LyDoKham"] == DBNull.Value
                        ? ""
                        : appointment["LyDoKham"].ToString() ?? "";
                }
                LoadTimeSlots(appointment == null ? null : appointment.Field<DateTime>("NgayHen"));
                if (appointment != null)
                {
                    string existingTime = appointment.Field<DateTime>("NgayHen").ToString("HH:mm");
                    if (!time.Items.Contains(existingTime))
                        time.Items.Add(existingTime);
                    time.SelectedItem = existingTime;
                }
                date.ValueChanged += (s, e) => LoadTimeSlots();
                doctor.SelectedValueChanged += (s, e) =>
                {
                    UpdateDoctorScheduleDisplay();
                    LoadTimeSlots();
                };

                var noDoctor = new Label
                {
                    Text = "Chưa có bác sĩ được phân công để nhận lịch hẹn. Vui lòng liên hệ lễ tân.",
                    Dock = DockStyle.Top,
                    Height = 44,
                    Padding = new Padding(18, 8, 18, 0),
                    ForeColor = Color.Firebrick,
                    Visible = doctors.Rows.Count == 0
                };
                save.Enabled = doctor.Items.Count > 0;
                save.Click += (s, e) =>
                {
                    if (doctor.SelectedValue == null)
                    {
                        ShowMessage("Vui lòng chọn bác sĩ.", "Thiếu thông tin",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (time.SelectedItem == null)
                    {
                        ShowMessage("Ngày đã chọn không còn khung giờ khám. Vui lòng chọn ngày khác.",
                            "Không còn giờ khám", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    DateTime appointmentTime = date.Value.Date.Add(
                        TimeSpan.Parse(time.SelectedItem.ToString() ?? "08:00"));
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(txtEmail.Text))
                        {
                            ClinicDAL.CapNhatEmailBenhNhan(_patient.MaBN, txtEmail.Text.Trim());
                            _patient.Email = txtEmail.Text.Trim();
                        }

                        ClinicDAL.DatLichHenBenhNhan(
                            _patient.MaBN,
                            doctor.SelectedValue.ToString() ?? "",
                            appointmentTime,
                            reason.Text,
                            appointment == null ? null : Convert.ToInt32(appointment["MaLichHen"]));
                        dialog.DialogResult = DialogResult.OK;
                        dialog.Close();
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(ex.Message, "Không thể lưu lịch hẹn",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                dialog.Controls.Add(fields);
                dialog.Controls.Add(noDoctor);
                dialog.Controls.Add(heading);
                dialog.AcceptButton = save;
                dialog.CancelButton = cancel;
                UiText.Watch(dialog);
                UiText.Apply(dialog);
                return dialog.ShowDialog(this) == DialogResult.OK;
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, "Không thể mở biểu mẫu đặt lịch",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ShowBooking(DataRow? appointment = null, bool openForm = false)
        {
            if (!openForm)
            {
                ShowAppointments();
                return;
            }

            SetPage(appointment == null ? "Đặt lịch khám" : "Sửa lịch hẹn", _appointmentsButton);
            try
            {
                DataTable doctors = ClinicDAL.LayDanhSachBacSiDatLich();
                var page = CreatePageFlow();
                var columns = CreateColumns(590);
                var left = CreateColumn();
                var right = CreateColumn();

                var formCard = CreateCard(630);
                formCard.Controls.Add(CreateLabel(appointment == null ? "Đặt lịch khám" : "Sửa yêu cầu đặt lịch",
                    18, 15, 420, 28, 18, true, TextColor));
                formCard.Controls.Add(CreateLabel("Chọn chuyên khoa, bác sĩ và khung giờ phù hợp.",
                    18, 48, 580, 22, 9, false, Muted));
                formCard.Controls.Add(CreateLabel("Chuyên khoa", 18, 90, 160, 20, 9, true, TextColor));
                var specialty = new ComboBox
                {
                    Location = new Point(18, 114),
                    Size = new Size(390, 32),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = Font
                };
                var specialties = ClinicDAL.LayDanhSachKhoa().AsEnumerable()
                    .Select(row => row["TenKhoa"].ToString() ?? "")
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToArray();
                specialty.Items.AddRange(specialties.Cast<object>().ToArray());
                if (specialty.Items.Count > 0)
                {
                    string currentSpecialty = appointment?["ChuyenKhoa"]?.ToString() ?? "";
                    int selectedSpecialty = Array.FindIndex(specialties,
                        value => string.Equals(value, currentSpecialty, StringComparison.OrdinalIgnoreCase));
                    specialty.SelectedIndex = selectedSpecialty >= 0 ? selectedSpecialty : 0;
                }
                else
                    formCard.Controls.Add(CreateLabel(
                        "Hiện chưa có bác sĩ được phân công chuyên khoa để đặt lịch. Vui lòng liên hệ lễ tân.",
                        18, 466, 470, 42, 9, true, Color.Firebrick));
                formCard.Controls.Add(specialty);

                formCard.Controls.Add(CreateLabel("Bác sĩ", 18, 165, 160, 20, 9, true, TextColor));
                var doctor = new ComboBox
                {
                    Location = new Point(18, 189),
                    Size = new Size(390, 32),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = Font
                };
                formCard.Controls.Add(doctor);
                formCard.Controls.Add(CreateLabel("Ngày khám", 18, 240, 160, 20, 9, true, TextColor));
                var date = new DateTimePicker
                {
                    Location = new Point(18, 264),
                    Size = new Size(190, 32),
                    MinDate = DateTime.Today,
                    MaxDate = DateTime.Today.AddMonths(3),
                    Format = DateTimePickerFormat.Custom,
                    CustomFormat = "dd/MM/yyyy"
                };
                if (appointment != null)
                {
                    DateTime existingDate = appointment.Field<DateTime>("NgayHen").Date;
                    if (existingDate >= date.MinDate && existingDate <= date.MaxDate)
                        date.Value = existingDate;
                }
                formCard.Controls.Add(date);
                formCard.Controls.Add(CreateLabel("Giờ khám", 228, 240, 160, 20, 9, true, TextColor));
                var time = new ComboBox
                {
                    Location = new Point(228, 264),
                    Size = new Size(180, 32),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = Font
                };
                void LoadTimeSlots(DateTime? preferred = null)
                {
                    time.Items.Clear();
                    if (doctor.SelectedValue == null)
                        return;
                    DataTable availableSlots = ClinicDAL.LayKhungGioKhaDung(
                        doctor.SelectedValue.ToString() ?? "",
                        date.Value.Date,
                        appointment == null ? null : Convert.ToInt32(appointment["MaLichHen"]));
                    foreach (DataRow slot in availableSlots.Rows)
                    {
                        time.Items.Add(Convert.ToDateTime(slot["NgayGio"]).ToString("HH:mm"));
                    }
                    string preferredText = preferred?.ToString("HH:mm") ?? "";
                    if (time.Items.Contains(preferredText))
                        time.SelectedItem = preferredText;
                    else if (time.Items.Count > 0)
                        time.SelectedIndex = 0;
                }
                formCard.Controls.Add(time);
                formCard.Controls.Add(CreateLabel("Email nhận thông báo", 18, 315, 300, 20, 9, true, TextColor));
                var txtPageEmail = new TextBox
                {
                    Location = new Point(18, 339),
                    Size = new Size(390, 30),
                    Font = Font,
                    PlaceholderText = "Nhập email nhận xác nhận lịch & biên lai..."
                };
                txtPageEmail.Text = ClinicDAL.LayEmailBenhNhanTheoMa(_patient.MaBN) ?? _patient.Email ?? "";
                formCard.Controls.Add(txtPageEmail);

                formCard.Controls.Add(CreateLabel("Lý do khám", 18, 380, 160, 20, 9, true, TextColor));
                var reason = new TextBox
                {
                    Location = new Point(18, 404),
                    Size = new Size(390, 78),
                    Multiline = true,
                    MaxLength = 255,
                    ScrollBars = ScrollBars.Vertical,
                    Font = Font
                };
                formCard.Controls.Add(reason);
                formCard.Controls.Add(CreateLabel("Lịch hẹn sẽ ở trạng thái chờ xác nhận sau khi gửi yêu cầu.", 18, 494, 420, 36, 8.5F, false, Muted));

                var doctorInfo = CreateLabel("Chọn bác sĩ để xem phòng khám.", 18, 52, 310, 140, 9.5F, false, Muted);
                var bookingCard = CreateCard(270);
                bookingCard.Controls.Add(CreateLabel("Thông tin bác sĩ & Lịch trực", 18, 15, 300, 24, 12, true, TextColor));
                bookingCard.Controls.Add(doctorInfo);
                var submit = CreateButton(appointment == null ? "Gửi yêu cầu đặt lịch" : "Lưu thay đổi", true);
                submit.Location = new Point(18, 205);
                submit.Size = new Size(220, 38);
                bookingCard.Controls.Add(submit);

                void LoadDoctors()
                {
                    string selectedSpecialty = specialty.SelectedItem?.ToString() ?? "";
                    DataTable filtered = doctors.Clone();
                    foreach (DataRow row in doctors.Rows)
                        if (string.Equals(row["ChuyenKhoa"].ToString(), selectedSpecialty, StringComparison.OrdinalIgnoreCase))
                            filtered.ImportRow(row);
                    if (!filtered.Columns.Contains("TenHienThi"))
                        filtered.Columns.Add("TenHienThi", typeof(string));
                    foreach (DataRow row in filtered.Rows)
                        row["TenHienThi"] = $"{row["HoTen"]}  •  {row["TenPhong"]}";
                    doctor.DataSource = filtered;
                    doctor.DisplayMember = "TenHienThi";
                    doctor.ValueMember = "MaNV";
                    doctor.Enabled = filtered.Rows.Count > 0;
                    submit.Enabled = filtered.Rows.Count > 0;
                    UpdateDoctorInfo();
                }

                void UpdateDoctorInfo()
                {
                    if (doctor.SelectedItem is DataRowView row)
                    {
                        string maBS = row["MaNV"]?.ToString() ?? "";
                        string lichTuan = ClinicDAL.MoTaLichTrucBacSi(maBS);
                        var (phong, khoa, caHienTai) = ClinicDAL.LayThongTinCaTrucBacSi(maBS);
                        doctorInfo.Text = $"👨‍⚕️ Bác sĩ: {row["HoTen"]}\r\n🏥 {phong} ({khoa})\r\n⏰ Ca trực hôm nay: {caHienTai}\r\n📅 Lịch tuần: {lichTuan}";
                    }
                    else
                    {
                        string selectedSpecialty = specialty.SelectedItem?.ToString() ?? "";
                        doctorInfo.Text = doctor.Items.Count == 0 && !string.IsNullOrWhiteSpace(selectedSpecialty)
                            ? "Chuyên khoa này hiện chưa có bác sĩ được phân công để đặt lịch."
                            : doctors.Rows.Count == 0
                                ? "Hiện chưa có bác sĩ được phân công phòng khám."
                                : "Chọn bác sĩ để xem phòng khám và ca trực.";
                    }
                }

                specialty.SelectedIndexChanged += (s, e) => LoadDoctors();
                doctor.SelectedIndexChanged += (s, e) =>
                {
                    UpdateDoctorInfo();
                    LoadTimeSlots();
                };
                date.ValueChanged += (s, e) => LoadTimeSlots();
                if (specialty.Items.Count > 0)
                {
                    LoadDoctors();
                    if (appointment != null)
                    {
                        doctor.SelectedValue = appointment["MaBacSi"];
                        reason.Text = appointment["LyDoKham"] == DBNull.Value
                            ? ""
                            : appointment["LyDoKham"].ToString() ?? "";
                    }
                    LoadTimeSlots(appointment == null ? null : appointment.Field<DateTime>("NgayHen"));
                    if (appointment != null)
                    {
                        DateTime existingAppointment = appointment.Field<DateTime>("NgayHen");
                        string existingTime = existingAppointment.ToString("HH:mm");
                        if (date.Value.Date == existingAppointment.Date)
                        {
                            if (!time.Items.Contains(existingTime))
                                time.Items.Add(existingTime);
                            time.SelectedItem = existingTime;
                        }
                    }
                }
                else
                {
                    specialty.Enabled = false;
                    doctor.Enabled = false;
                    submit.Enabled = false;
                }

                submit.Click += (s, e) =>
                {
                    if (doctor.SelectedValue == null)
                    {
                        ShowMessage("Hiện chưa có bác sĩ phù hợp để đặt lịch.", "Chưa có bác sĩ",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (time.SelectedItem == null)
                    {
                        ShowMessage("Bác sĩ không có slot 15 phút khả dụng. Vui lòng chọn ngày khác.",
                            "Không còn giờ khám", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    DateTime appointmentTime = date.Value.Date.Add(TimeSpan.Parse(time.SelectedItem?.ToString() ?? "08:00"));
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(txtPageEmail.Text))
                        {
                            ClinicDAL.CapNhatEmailBenhNhan(_patient.MaBN, txtPageEmail.Text.Trim());
                            _patient.Email = txtPageEmail.Text.Trim();
                        }

                        ClinicDAL.DatLichHenBenhNhan(_patient.MaBN, doctor.SelectedValue.ToString() ?? "",
                            appointmentTime, reason.Text,
                            appointment == null ? null : Convert.ToInt32(appointment["MaLichHen"]));
                        ShowMessage(appointment == null
                                ? "Đã gửi yêu cầu đặt lịch. Vui lòng theo dõi trạng thái trong mục Lịch hẹn."
                                : "Đã cập nhật lịch hẹn của bạn.",
                            appointment == null ? "Đặt lịch thành công" : "Cập nhật thành công",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ShowAppointments();
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(ex.Message, "Không thể đặt lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };

                left.Controls.Add(formCard);
                right.Controls.Add(bookingCard);
                var safetyCard = CreateCard(160);
                safetyCard.Controls.Add(CreateLabel("Lưu ý", 18, 15, 260, 22, 11, true, TextColor));
                safetyCard.Controls.Add(CreateLabel("Yêu cầu đặt lịch cần được phòng khám xác nhận. Nếu cần hỗ trợ khẩn cấp, vui lòng gọi cấp cứu hoặc đến cơ sở y tế gần nhất.",
                    18, 48, 290, 86, 9, false, Muted));
                right.Controls.Add(safetyCard);
                columns.Controls.Add(left, 0, 0);
                columns.Controls.Add(right, 1, 0);
                page.Controls.Add(columns);
                _content.Controls.Add(page);
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ShowAppointmentDetail(DataRow appointment)
        {
            SetPage("Chi tiết lịch hẹn", _appointmentsButton);
            var page = CreatePageFlow();
            var status = appointment["TrangThai"].ToString() ?? "Chờ xác nhận";
            var summary = CreateCard(168);
            summary.Controls.Add(CreateLabel("Hành trình chăm sóc", 18, 15, 600, 22, 12, true, TextColor));
            summary.Controls.Add(CreateTag(status, 18, 53));
            summary.Controls.Add(CreateLabel($"Lịch hẹn #{Value(appointment, "MaLichHen")}", 18, 94, 400, 24, 14, true, TextColor));
            summary.Controls.Add(CreateLabel(((DateTime)appointment["NgayHen"]).ToString("dd/MM/yyyy  •  HH:mm"),
                18, 124, 420, 20, 10, false, Muted));
            page.Controls.Add(summary);

            var detail = CreateCard(280);
            detail.Controls.Add(CreateLabel("Thông tin lịch khám", 18, 15, 400, 24, 12, true, TextColor));
            detail.Controls.Add(CreateLabel($"Bác sĩ: {Value(appointment, "TenBacSi")}", 18, 58, 560, 22, 10, true, TextColor));
            detail.Controls.Add(CreateLabel($"Chuyên khoa: {Value(appointment, "ChuyenKhoa")}", 18, 88, 560, 22, 9, false, Muted));
            detail.Controls.Add(CreateLabel($"Phòng: {Value(appointment, "TenPhong")}  •  {Value(appointment, "TenKhoa")}", 18, 116, 560, 22, 9, false, Muted));
            detail.Controls.Add(CreateLabel("Lý do khám", 18, 160, 300, 20, 9, true, TextColor));
            detail.Controls.Add(CreateLabel(Value(appointment, "LyDoKham"), 18, 188, 620, 55, 9, false, TextColor));
            var back = CreateButton("Quay lại danh sách", false);
            back.Location = new Point(18, 244);
            back.Size = new Size(160, 30);
            back.Click += (s, e) => ShowAppointments();
            detail.Controls.Add(back);
            page.Controls.Add(detail);
            _content.Controls.Add(page);
        }

        private void ShowHealthRecords()
        {
            SetPage("Hồ sơ khám bệnh", _recordsButton);
            try
            {
                var profile = ClinicDAL.LayBenhNhanTheoMa(_patient.MaBN)
                    ?? throw new InvalidOperationException("Không tìm thấy hồ sơ bệnh nhân.");
                var page = CreatePageFlow();
                var intro = CreateCard(74);
                intro.Controls.Add(CreateLabel("Hồ sơ khám bệnh của tôi", 16, 12, 500, 25, 16, true, TextColor));
                intro.Controls.Add(CreateLabel($"Cập nhật thông tin và xem lịch sử khám của {Value(profile, "HoTen")}.",
                    16, 43, 700, 18, 8.5F, false, Muted));
                page.Controls.Add(intro);

                var tabCard = CreateCard(560);
                var tabs = new TabControl
                {
                    Location = new Point(12, 12),
                    Size = new Size(0, 0),
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 9F)
                };
                var historyTab = new TabPage("Lịch sử khám");
                var servicesTab = new TabPage("Dịch vụ cận lâm sàng");
                var prescriptionTab = new TabPage("Đơn thuốc");
                var profileTab = new TabPage("Thông tin cá nhân");

                // ── Tab 1: Lịch sử khám (Grid trên + Card chi tiết phía dưới) ──
                var pnlHistoryWrapper = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
                var historyGrid = CreateGrid(8, 8, 900, 240);
                historyGrid.Dock = DockStyle.Top;
                historyGrid.Height = 240;
                historyGrid.DataSource = ClinicDAL.LayLichSuKhamBenhNhan(_patient.MaBN);

                var pnlHistoryDetail = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(249, 250, 251),
                    Padding = new Padding(14)
                };
                pnlHistoryDetail.Paint += (s, e) =>
                {
                    using var p = new Pen(Color.FromArgb(229, 231, 235), 1);
                    e.Graphics.DrawLine(p, 0, 0, pnlHistoryDetail.Width, 0);
                };

                var lblHDTitle = new Label { Text = "CHI TIẾT PHIẾU KHÁM ĐANG CHỌN", Location = new Point(12, 8), AutoSize = true, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Navy };
                var lblHDDoctor = new Label { Text = "Chọn một ca khám ở bảng trên để xem chi tiết.", Location = new Point(12, 32), Size = new Size(700, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = TextColor };
                var lblHDVitals = new Label { Text = "", Location = new Point(12, 54), Size = new Size(800, 20), Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = Color.FromArgb(13, 148, 136) };
                var lblHDDiagnosis = new Label { Text = "", Location = new Point(12, 76), Size = new Size(800, 38), Font = new Font("Segoe UI", 9F), ForeColor = Color.FromArgb(180, 83, 9) };
                var lblHDAdvice = new Label { Text = "", Location = new Point(12, 116), Size = new Size(800, 36), Font = new Font("Segoe UI", 8.5F), ForeColor = Color.FromArgb(75, 85, 99) };

                pnlHistoryDetail.Controls.AddRange(new Control[] { lblHDTitle, lblHDDoctor, lblHDVitals, lblHDDiagnosis, lblHDAdvice });

                void UpdateHistoryDetailSelection()
                {
                    if (historyGrid.CurrentRow?.DataBoundItem is not DataRowView rowView)
                    {
                        lblHDDoctor.Text = "Chọn một ca khám ở bảng trên để xem chi tiết.";
                        lblHDVitals.Text = "";
                        lblHDDiagnosis.Text = "";
                        lblHDAdvice.Text = "";
                        return;
                    }
                    DataRow row = rowView.Row;
                    string maPK = Value(row, "MaPK");
                    string doctor = Value(row, "TenBacSi");
                    string ngayKham = row["NgayKham"] != DBNull.Value ? Convert.ToDateTime(row["NgayKham"]).ToString("dd/MM/yyyy HH:mm") : "";
                    string chandoan = Value(row, "ChanDoan");
                    string sinhHieu = Value(row, "SinhHieu");
                    string loiDan = Value(row, "LoiDan");
                    string trieuChung = Value(row, "TrieuChung");

                    lblHDDoctor.Text = $"🩺 Phiếu #{maPK}  •  Ngày: {ngayKham}  •  Bác sĩ khám: {doctor}";

                    if (!string.IsNullOrWhiteSpace(sinhHieu))
                    {
                        var parts = sinhHieu.Split('|');
                        if (parts.Length >= 7)
                            lblHDVitals.Text = $"🩸 Sinh hiệu: HA: {parts[0]} mmHg | Mạch: {parts[1]} l/p | Nhiệt độ: {parts[2]}°C | SpO2: {parts[3]}% | Cân nặng: {parts[4]}kg | BMI: {parts[6]}";
                        else
                            lblHDVitals.Text = $"🩸 Sinh hiệu: {sinhHieu}";
                    }
                    else
                    {
                        lblHDVitals.Text = "🩸 Sinh hiệu: Chưa ghi nhận";
                    }

                    lblHDDiagnosis.Text = $"🔍 Chẩn đoán: {(string.IsNullOrWhiteSpace(chandoan) ? "Khám bệnh" : chandoan)}   (Triệu chứng ban đầu: {trieuChung})";
                    lblHDAdvice.Text = $"💊 Lời dặn của bác sĩ: {(string.IsNullOrWhiteSpace(loiDan) ? "Tuân thủ theo chỉ dẫn đơn thuốc" : loiDan)}";
                }

                historyGrid.SelectionChanged += (s, e) => UpdateHistoryDetailSelection();

                pnlHistoryWrapper.Controls.Add(pnlHistoryDetail);
                pnlHistoryWrapper.Controls.Add(historyGrid);
                historyTab.Controls.Add(pnlHistoryWrapper);

                if (historyGrid.Rows.Count > 0)
                {
                    historyGrid.Rows[0].Selected = true;
                    UpdateHistoryDetailSelection();
                }

                var serviceGrid = CreateGrid(10, 10, 0, 0);
                serviceGrid.Dock = DockStyle.Fill;
                serviceGrid.DataSource = ClinicDAL.LayChiDinhDVBenhNhan(_patient.MaBN);
                var prescriptionGrid = CreateGrid(10, 10, 0, 0);
                prescriptionGrid.Dock = DockStyle.Fill;
                prescriptionGrid.DataSource = ClinicDAL.LayChiTietDonThuocBenhNhan(_patient.MaBN);
                var profilePanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20), BackColor = Color.White };
                AddProfileField(profilePanel, "Họ và tên", Value(profile, "HoTen"), 15);
                AddProfileField(profilePanel, "Mã hồ sơ", Value(profile, "MaBN"), 55);
                AddProfileField(profilePanel, "Ngày sinh", FormatValue(profile["NgaySinh"]), 95);
                AddProfileField(profilePanel, "Giới tính", Value(profile, "GioiTinh"), 135);
                AddProfileField(profilePanel, "Số điện thoại", FormatValue(profile["SoDienThoai"]), 175);
                AddProfileField(profilePanel, "Email nhận thông báo", FormatValue(profile["Email"]), 215);
                AddProfileField(profilePanel, "Địa chỉ", FormatValue(profile["DiaChi"]), 255);
                AddProfileField(profilePanel, "Tiền sử bệnh / dị ứng", FormatValue(profile["TienSuBenh"]), 295);

                servicesTab.Controls.Add(serviceGrid);
                prescriptionTab.Controls.Add(prescriptionGrid);
                profileTab.Controls.Add(profilePanel);
                tabs.TabPages.AddRange(new[] { historyTab, servicesTab, prescriptionTab, profileTab });
                tabCard.Controls.Add(tabs);
                page.Controls.Add(tabCard);
                _content.Controls.Add(page);
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ShowBills()
        {
            SetPage("Thanh toán viện phí", _billsButton);
            try
            {
                DataTable bills = ClinicDAL.LayHoaDonBenhNhan(_patient.MaBN);
                var page = CreatePageFlow();
                var intro = CreateCard(86);
                intro.Controls.Add(CreateLabel("Thanh toán viện phí", 16, 14, 400, 25, 16, true, TextColor));
                intro.Controls.Add(CreateLabel("Xem chi tiết các hóa đơn và trạng thái thanh toán của bạn.", 16, 46, 720, 20, 9, false, Muted));
                page.Controls.Add(intro);

                var columns = CreateColumns(455);
                var left = CreateColumn();
                var right = CreateColumn();
                var listCard = CreateCard(440);
                listCard.Controls.Add(CreateLabel("Hóa đơn của tôi", 16, 14, 350, 22, 11, true, TextColor));
                var grid = CreateGrid(16, 48, 0, 365);
                grid.DataSource = bills;
                grid.Size = new Size(Math.Max(0, listCard.ClientSize.Width - 32), 365);
                listCard.SizeChanged += (s, e) => grid.Size = new Size(Math.Max(0, listCard.ClientSize.Width - 32), 365);
                listCard.Controls.Add(grid);
                left.Controls.Add(listCard);

                var detailCard = CreateCard(410);
                detailCard.Controls.Add(CreateLabel("Chi tiết viện phí", 16, 14, 300, 22, 11, true, TextColor));
                var detailText = CreateLabel("Chọn một hóa đơn để xem thông tin.", 16, 48, 300, 85, 9, false, Muted);
                detailCard.Controls.Add(detailText);
                var total = CreateLabel("", 16, 140, 300, 34, 18, true, Teal);
                detailCard.Controls.Add(total);
                var payHint = CreateLabel("Nếu hóa đơn còn chưa thanh toán, bạn có thể quét mã VietQR hoặc đến quầy thu ngân phòng khám để được hỗ trợ.",
                    16, 180, 300, 50, 8.5F, false, Muted);
                detailCard.Controls.Add(payHint);

                var btnViewQr = new Button
                {
                    Text = "📱 Xem mã QR thanh toán",
                    Location = new Point(16, 240),
                    Size = new Size(200, 34),
                    BackColor = Teal,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Visible = false
                };
                btnViewQr.FlatAppearance.BorderSize = 0;
                detailCard.Controls.Add(btnViewQr);

                grid.SelectionChanged += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is not DataRowView row)
                        return;
                    DataRow item = row.Row;
                    string maHD = Value(item, "MaHD");
                    string trangThai = Value(item, "TrangThai");
                    decimal tongTien = Convert.ToDecimal(item["TongTien"]);

                    detailText.Text =
                        $"Mã hóa đơn: {maHD}\r\nMã phiếu khám: {Value(item, "MaPK")}\r\n" +
                        $"Ngày lập: {FormatValue(item["NgayLap"])}\r\nTrạng thái: {trangThai}\r\n" +
                        $"Hình thức: {Value(item, "HinhThucTT")}";
                    total.Text = $"{tongTien:N0} đ";

                    if (trangThai == "Chưa thanh toán")
                    {
                        payHint.Text = "Hóa đơn chưa thanh toán. Bạn có thể bấm nút bên dưới để mở mã VietQR chuyển khoản nhanh.";
                        btnViewQr.Visible = true;
                    }
                    else
                    {
                        payHint.Text = "Hóa đơn này đã được thanh toán thành công. Cảm ơn bạn đã tin tưởng An Tâm.";
                        btnViewQr.Visible = false;
                    }
                };

                btnViewQr.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is not DataRowView row) return;
                    DataRow item = row.Row;
                    string maHD = Value(item, "MaHD");
                    decimal tongTien = Convert.ToDecimal(item["TongTien"]);
                    Helpers.VietQRHelper.ShowPaymentQrDialog(this, maHD, tongTien, $"THANH TOAN {maHD}");
                };

                if (grid.Rows.Count > 0)
                    grid.Rows[0].Selected = true;
                right.Controls.Add(detailCard);
                columns.Controls.Add(left, 0, 0);
                columns.Controls.Add(right, 1, 0);
                page.Controls.Add(columns);
                _content.Controls.Add(page);
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ShowAssistant()
        {
            SetPage("Trợ lý An Tâm", _assistantButton);
            var page = CreatePageFlow();
            var intro = CreateCard(90);
            intro.Controls.Add(CreateLabel("Trợ lý An Tâm", 16, 14, 400, 25, 16, true, TextColor));
            intro.Controls.Add(CreateLabel("Tra cứu hướng dẫn sử dụng cổng bệnh nhân. Trợ lý không thay thế tư vấn hoặc chẩn đoán của bác sĩ.",
                16, 47, 800, 25, 9, false, Muted));
            page.Controls.Add(intro);

            var columns = CreateColumns(490);
            var left = CreateColumn();
            var right = CreateColumn();
            var chat = CreateCard(460);
            chat.Controls.Add(CreateLabel("Hỏi đáp sức khỏe và hồ sơ", 16, 14, 450, 22, 12, true, TextColor));
            var transcript = new TextBox
            {
                Location = new Point(16, 48),
                Size = new Size(Math.Max(0, chat.ClientSize.Width - 32), 310),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(250, 252, 252),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F),
                Text = "Trợ lý An Tâm: Xin chào! Tôi có thể hướng dẫn bạn sử dụng các mục lịch hẹn, hồ sơ sức khỏe và viện phí.\r\n"
            };
            var question = new TextBox
            {
                Location = new Point(16, 372),
                Size = new Size(Math.Max(150, chat.ClientSize.Width - 130), 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "Nhập câu hỏi..."
            };
            var send = CreateButton("Gửi", true);
            send.Location = new Point(chat.ClientSize.Width - 96, 372);
            send.Size = new Size(80, 34);
            send.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            void PositionChatControls()
            {
                transcript.Width = Math.Max(0, chat.ClientSize.Width - 32);
                transcript.Height = Math.Max(120, chat.ClientSize.Height - 150);
                question.Top = chat.ClientSize.Height - question.Height - 16;
                question.Width = Math.Max(150, chat.ClientSize.Width - 130);
                send.Left = chat.ClientSize.Width - send.Width - 16;
                send.Top = question.Top;
            }
            chat.SizeChanged += (s, e) => PositionChatControls();
            PositionChatControls();
            void SendQuestion()
            {
                string text = question.Text.Trim();
                if (text.Length == 0)
                    return;
                string answer = text.Contains("lịch", StringComparison.OrdinalIgnoreCase)
                    ? "Bạn có thể xem hoặc gửi yêu cầu đặt lịch trong mục Lịch hẹn. Lịch mới cần phòng khám xác nhận."
                    : text.Contains("hóa đơn", StringComparison.OrdinalIgnoreCase) ||
                      text.Contains("viện phí", StringComparison.OrdinalIgnoreCase) ||
                      text.Contains("thanh toán", StringComparison.OrdinalIgnoreCase)
                        ? "Hóa đơn và trạng thái thanh toán có trong mục Thanh toán viện phí. Với hóa đơn chưa thanh toán, vui lòng liên hệ quầy thu ngân."
                        : text.Contains("hồ sơ", StringComparison.OrdinalIgnoreCase) ||
                          text.Contains("thuốc", StringComparison.OrdinalIgnoreCase) ||
                          text.Contains("kết quả", StringComparison.OrdinalIgnoreCase)
                            ? "Lịch sử khám, kết quả dịch vụ và đơn thuốc được hiển thị trong mục Hồ sơ khám bệnh."
                            : "Tôi chỉ hỗ trợ hướng dẫn sử dụng cổng bệnh nhân. Với tư vấn triệu chứng, chẩn đoán hoặc điều trị, vui lòng liên hệ bác sĩ.";
                transcript.AppendText($"\r\nBạn: {text}\r\nTrợ lý An Tâm: {answer}\r\n");
                question.Clear();
            }
            send.Click += (s, e) => SendQuestion();
            question.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SendQuestion();
                }
            };
            chat.Controls.AddRange(new Control[] { transcript, question, send });
            left.Controls.Add(chat);

            var faq = CreateCard(235);
            faq.Controls.Add(CreateLabel("Bạn có thể hỏi", 16, 14, 260, 22, 11, true, TextColor));
            string[] questions =
            {
                "Làm sao để xem lịch hẹn?",
                "Tôi xem hóa đơn ở đâu?",
                "Tôi tìm đơn thuốc ở đâu?"
            };
            int top = 52;
            foreach (string prompt in questions)
            {
                var button = CreateButton(prompt, false);
                button.Location = new Point(16, top);
                button.Size = new Size(290, 34);
                button.TextAlign = ContentAlignment.MiddleLeft;
                button.Click += (s, e) =>
                {
                    question.Text = prompt;
                    SendQuestion();
                };
                faq.Controls.Add(button);
                top += 46;
            }
            right.Controls.Add(faq);
            var caution = CreateCard(170);
            caution.Controls.Add(CreateLabel("Thông tin y tế", 16, 14, 260, 22, 11, true, TextColor));
            caution.Controls.Add(CreateLabel("Không dùng trợ lý để tự chẩn đoán hay thay đổi thuốc. Nếu có dấu hiệu khẩn cấp, hãy gọi cấp cứu hoặc đến cơ sở y tế gần nhất.",
                16, 50, 285, 90, 9, false, Muted));
            right.Controls.Add(caution);
            columns.Controls.Add(left, 0, 0);
            columns.Controls.Add(right, 1, 0);
            page.Controls.Add(columns);
            _content.Controls.Add(page);
        }

        private FlowLayoutPanel CreatePageFlow()
        {
            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Width = Math.Max(760, _content.ClientSize.Width - 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(4),
                BackColor = PageBackground
            };
            flow.ControlAdded += (s, e) => ResizePageControls(flow);
            flow.SizeChanged += (s, e) => ResizePageControls(flow);
            EventHandler resizeFlow = (s, e) =>
            {
                if (!flow.IsDisposed)
                    flow.Width = Math.Max(760, _content.ClientSize.Width - 8);
            };
            _content.SizeChanged += resizeFlow;
            flow.Disposed += (s, e) => _content.SizeChanged -= resizeFlow;
            return flow;
        }

        private static void ResizePageControls(FlowLayoutPanel flow)
        {
            int availableWidth = Math.Max(0, flow.ClientSize.Width - flow.Padding.Horizontal - 8);
            foreach (Control control in flow.Controls)
                if (control is Panel)
                    control.Width = availableWidth;
        }

        private TableLayoutPanel CreateColumns(int height, int leftColumnPercent = 66)
        {
            var layout = new TableLayoutPanel
            {
                Width = Math.Max(760, _content.ClientSize.Width - 8),
                Height = height,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSize = false,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = PageBackground
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, leftColumnPercent));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100 - leftColumnPercent));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.SizeChanged += (s, e) =>
            {
                foreach (Control control in layout.Controls)
                    if (control is FlowLayoutPanel flow)
                        ResizeColumnCards(flow);
            };
            return layout;
        }

        private FlowLayoutPanel CreateColumn()
        {
            var column = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = PageBackground
            };
            column.ControlAdded += (s, e) => ResizeColumnCards(column);
            column.SizeChanged += (s, e) => ResizeColumnCards(column);
            return column;
        }

        private static void ResizeColumnCards(FlowLayoutPanel column)
        {
            foreach (Control control in column.Controls)
                if (control is Panel card)
                    card.Width = Math.Max(0, column.ClientSize.Width - 12);
        }

        private static Panel CreateCard(int height)
        {
            var card = new Panel
            {
                Width = 1000,
                Height = height,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 12),
                Padding = new Padding(0)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(229, 235, 238));
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };
            return card;
        }

        private static void AddCardAccent(Panel card)
        {
            card.Controls.Add(new Panel
            {
                Location = Point.Empty,
                Size = new Size(card.Width, 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Teal
            });
        }

        private static Label CreateLabel(string text, int x, int y, int width, int height, bool bold, Color color)
            => CreateLabel(text, x, y, width, height, 9F, bold, color);

        private static Label CreateLabel(string text, int x, int y, int width, int height, float size, bool bold, Color color)
        {
            float readableSize = size <= 8.5F ? 9.5F : size <= 10F ? 10.5F : size <= 12F ? 12F : size;
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Segoe UI", readableSize, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color,
                AutoEllipsis = true
            };
        }

        private static Label CreateWrappedLabel(string text, int x, int y, int maxWidth, float size, bool bold, Color color)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                MaximumSize = new Size(maxWidth, 0),
                Font = new Font("Segoe UI", size <= 10F ? 10.5F : size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color
            };
        }

        private static Button CreateButton(string text, bool primary)
        {
            var button = new Button
            {
                Text = text,
                BackColor = primary ? Teal : Color.White,
                ForeColor = primary ? Color.White : TextColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, primary ? FontStyle.Bold : FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = primary ? Teal : Color.FromArgb(213, 223, 228);
            button.FlatAppearance.BorderSize = 1;
            return button;
        }

        private static Label CreateTag(string text, int x, int y, Color? back = null, Color? fore = null)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                BackColor = back ?? TealLight,
                ForeColor = fore ?? Teal,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
        }

        private static DataGridView CreateGrid(int x, int y, int width, int height)
        {
            var grid = new DataGridView
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.FromArgb(232, 237, 240),
                Font = new Font("Segoe UI", 9.5F),
                ColumnHeadersHeight = 36,
                EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 250);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor = TealLight;
            grid.DefaultCellStyle.SelectionForeColor = TextColor;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 252);
            grid.RowTemplate.Height = 36;
            grid.DataBindingComplete += (s, e) => FormMain.StandardizeGridColumns(grid);
            return grid;
        }

        private static void HideColumns(DataGridView grid, params string[] columns)
        {
            foreach (string column in columns)
                if (grid.Columns.Contains(column))
                    grid.Columns[column]!.Visible = false;
        }

        private static void AddProfileField(Control parent, string title, string value, int top)
        {
            parent.Controls.Add(CreateLabel(title, 20, top, 210, 22, 9, true, Muted));
            parent.Controls.Add(CreateLabel(value, 230, top, 540, 28, 9, false, TextColor));
        }

        private static string Value(DataRow row, string column)
            => row.Table.Columns.Contains(column) ? FormatValue(row[column]) : "Chưa cập nhật";

        private static string FormatValue(object? value)
        {
            if (value == null || value == DBNull.Value || string.IsNullOrWhiteSpace(value.ToString()))
                return "Chưa cập nhật";
            return value is DateTime date ? date.ToString("dd/MM/yyyy") : value.ToString() ?? "";
        }

        private void ShowLoadError(Exception exception)
        {
            var error = CreateCard(90);
            error.Controls.Add(CreateLabel("Không tải được dữ liệu bệnh nhân", 16, 14, 600, 24, 12, true, Color.Firebrick));
            error.Controls.Add(CreateLabel(exception.Message, 16, 46, 900, 32, 9, false, Muted));
            _content.Controls.Add(error);
            ShowMessage(exception.Message, "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
