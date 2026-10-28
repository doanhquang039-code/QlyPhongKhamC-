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
        private static readonly Color Muted = Color.FromArgb(105, 122, 134);

        private readonly TaiKhoanBenhNhan _patient;
        private readonly Panel _content;
        private readonly Label _pageTitle;
        private readonly Label _breadcrumb;
        private readonly Button _refreshButton;
        private readonly Button _homeButton;
        private readonly Button _appointmentsButton;
        private readonly Button _bookingButton;
        private readonly Button _recordsButton;
        private readonly Button _billsButton;
        private readonly Button _assistantButton;

        public FormBenhNhanPortal()
        {
            _patient = SessionState.CurrentPatient
                ?? throw new InvalidOperationException("Chưa có bệnh nhân đăng nhập.");

            Text = "An Tâm - Cổng bệnh nhân";
            Size = new Size(1280, 820);
            MinimumSize = new Size(1080, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = PageBackground;
            Font = new Font("Segoe UI", 9F);

            var sidebar = new Panel { Dock = DockStyle.Left, Width = 218, BackColor = Navy };
            var brand = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = Navy };
            brand.Controls.Add(new Label
            {
                Text = "+",
                Location = new Point(18, 13),
                AutoSize = true,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 196, 176)
            });
            brand.Controls.Add(new Label
            {
                Text = "an tâm",
                Location = new Point(52, 22),
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
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
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
            _appointmentsButton = CreateNavButton("▦", "Lịch hẹn", 92);
            _bookingButton = CreateNavButton("＋", "Đặt lịch khám", 138);
            _recordsButton = CreateNavButton("♡", "Hồ sơ khám bệnh", 184);
            _billsButton = CreateNavButton("▤", "Thanh toán viện phí", 230);
            _assistantButton = CreateNavButton("✦", "Trợ lý An Tâm", 276);
            _homeButton.Click += (s, e) => ShowHome();
            _appointmentsButton.Click += (s, e) => ShowAppointments();
            _bookingButton.Click += (s, e) => ShowBooking();
            _recordsButton.Click += (s, e) => ShowHealthRecords();
            _billsButton.Click += (s, e) => ShowBills();
            _assistantButton.Click += (s, e) => ShowAssistant();
            navigation.Controls.AddRange(new Control[]
            {
                _homeButton, _appointmentsButton, _bookingButton, _recordsButton, _billsButton, _assistantButton
            });

            var sidebarFooter = new Panel { Dock = DockStyle.Bottom, Height = 86, BackColor = Navy };
            var logoutButton = new Button
            {
                Text = "  Đăng xuất",
                Dock = DockStyle.Top,
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
            sidebarFooter.Controls.Add(logoutButton);

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
                Font = new Font("Segoe UI", 8.5F)
            };
            _pageTitle = new Label
            {
                Text = "Trang chủ bệnh nhân",
                Location = new Point(24, 31),
                AutoSize = true,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold)
            };
            _refreshButton = CreateButton("Làm mới", primary: false);
            _refreshButton.Size = new Size(100, 32);
            _refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _refreshButton.Location = new Point(760, 19);
            _refreshButton.Click += (s, e) => RefreshCurrentPage();
            var userLabel = new Label
            {
                Text = _patient.HoTen,
                Location = new Point(890, 25),
                Size = new Size(250, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoEllipsis = true
            };
            header.Controls.AddRange(new Control[] { _breadcrumb, _pageTitle, _refreshButton, userLabel });

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

            ShowHome();
            FormClosed += (s, e) => SessionState.CurrentPatient = null;
        }

        private void Logout(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Bạn muốn thoát tài khoản và quay lại màn hình đăng nhập?",
                    "Đăng xuất", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            SessionState.CurrentPatient = null;
            Close();
        }

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
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 74, 92);
            return button;
        }

        private void SetPage(string title, Button activeButton)
        {
            _pageTitle.Text = title;
            _breadcrumb.Text = $"Bệnh nhân   ›   {title}";
            foreach (var button in new[]
                     {
                         _homeButton, _appointmentsButton, _bookingButton, _recordsButton, _billsButton, _assistantButton
                     })
            {
                button.BackColor = button == activeButton
                    ? Color.FromArgb(0, 104, 99)
                    : Navy;
                button.ForeColor = button == activeButton ? Color.White : Color.FromArgb(207, 221, 229);
            }
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
            else if (_bookingButton.BackColor != Navy)
                ShowBooking();
            else if (_recordsButton.BackColor != Navy)
                ShowHealthRecords();
            else if (_billsButton.BackColor != Navy)
                ShowBills();
            else
                ShowAssistant();
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
                var intro = CreateCard(120);
                intro.Controls.Add(CreateLabel($"Xin chào, {_patient.HoTen}", 18, 15, 800, 32, 18, true, TextColor));
                intro.Controls.Add(CreateLabel("Chúc bạn một ngày khỏe mạnh. Theo dõi lịch hẹn và hồ sơ sức khỏe của bạn tại đây.",
                    20, 53, 800, 25, false, Muted));
                var bookButton = CreateButton("＋  Đặt lịch khám", true);
                bookButton.Location = new Point(20, 82);
                bookButton.Size = new Size(155, 32);
                bookButton.Click += (s, e) =>
                {
                    if (OpenBookingDialog())
                        ShowAppointments();
                };
                intro.Controls.Add(bookButton);
                page.Controls.Add(intro);

                var columns = CreateColumns(510);
                var left = CreateColumn();
                var right = CreateColumn();

                var appointmentCard = CreateCard(224);
                appointmentCard.Controls.Add(CreateLabel("Lịch hẹn sắp tới", 16, 14, 360, 20, 11, true, TextColor));
                if (upcoming != null)
                {
                    appointmentCard.Controls.Add(CreateTag(upcoming["TrangThai"].ToString() ?? "Chờ xác nhận", 16, 46));
                    appointmentCard.Controls.Add(CreateLabel(
                        ((DateTime)upcoming["NgayHen"]).ToString("dd/MM/yyyy  •  HH:mm"),
                        16, 82, 330, 24, 15, true, Teal));
                    appointmentCard.Controls.Add(CreateLabel($"Bác sĩ: {Value(upcoming, "TenBacSi")}", 16, 116, 360, 22, 9, true, TextColor));
                    appointmentCard.Controls.Add(CreateLabel($"Chuyên khoa: {Value(upcoming, "ChuyenKhoa")}", 16, 142, 360, 20, 9, false, Muted));
                    appointmentCard.Controls.Add(CreateLabel($"Phòng: {Value(upcoming, "TenPhong")}", 16, 165, 360, 20, 9, false, Muted));
                }
                else
                {
                    appointmentCard.Controls.Add(CreateLabel("Bạn chưa có lịch hẹn sắp tới.", 16, 64, 350, 24, 10, false, Muted));
                }
                var seeAppointments = CreateButton("Xem lịch hẹn", false);
                seeAppointments.Location = new Point(16, 184);
                seeAppointments.Size = new Size(130, 28);
                seeAppointments.Click += (s, e) => ShowAppointments();
                appointmentCard.Controls.Add(seeAppointments);
                left.Controls.Add(appointmentCard);

                var historyCard = CreateCard(240);
                historyCard.Controls.Add(CreateLabel("Hồ sơ và tài liệu mới", 16, 14, 360, 20, 11, true, TextColor));
                historyCard.Controls.Add(CreateLabel($"Số lượt khám đã ghi nhận: {history.Rows.Count}", 16, 44, 360, 20, 9, false, Muted));
                historyCard.Controls.Add(CreateLabel($"Ngày sinh: {FormatValue(profile["NgaySinh"])}   •   Giới tính: {Value(profile, "GioiTinh")}",
                    16, 70, 380, 24, 9, false, TextColor));
                historyCard.Controls.Add(CreateLabel($"Điện thoại: {FormatValue(profile["SoDienThoai"])}", 16, 100, 360, 20, 9, false, TextColor));
                var historyButton = CreateButton("Mở hồ sơ khám bệnh", false);
                historyButton.Location = new Point(16, 145);
                historyButton.Size = new Size(165, 32);
                historyButton.Click += (s, e) => ShowHealthRecords();
                historyCard.Controls.Add(historyButton);
                left.Controls.Add(historyCard);

                var billCard = CreateCard(224);
                billCard.Controls.Add(CreateLabel("Hóa đơn / viện phí", 16, 14, 300, 20, 11, true, TextColor));
                if (unpaid != null)
                {
                    billCard.Controls.Add(CreateTag("Chưa thanh toán", 16, 48, Color.FromArgb(255, 245, 219), Color.FromArgb(153, 104, 0)));
                    billCard.Controls.Add(CreateLabel($"{Convert.ToDecimal(unpaid["TongTien"]):N0} đ", 16, 87, 300, 32, 20, true, Teal));
                    billCard.Controls.Add(CreateLabel($"Hóa đơn: {Value(unpaid, "MaHD")}", 16, 125, 300, 20, 9, false, Muted));
                }
                else
                {
                    billCard.Controls.Add(CreateLabel("Bạn không có hóa đơn chưa thanh toán.", 16, 65, 300, 40, 9, false, Muted));
                }
                var billsButton = CreateButton("Xem viện phí", true);
                billsButton.Location = new Point(16, 166);
                billsButton.Size = new Size(135, 32);
                billsButton.Click += (s, e) => ShowBills();
                billCard.Controls.Add(billsButton);
                right.Controls.Add(billCard);

                var reminderCard = CreateCard(240);
                reminderCard.Controls.Add(CreateLabel("Nhắc nhở chăm sóc sức khỏe", 16, 14, 300, 38, 11, true, TextColor));
                reminderCard.Controls.Add(CreateLabel("Theo dõi lịch tái khám theo hướng dẫn của bác sĩ. Nếu triệu chứng trở nặng, hãy liên hệ cơ sở y tế.",
                    16, 60, 280, 65, 9, false, Muted));
                var supportButton = CreateButton("Mở trợ lý An Tâm", false);
                supportButton.Location = new Point(16, 145);
                supportButton.Size = new Size(165, 32);
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
                toolbar.Controls.Add(CreateLabel("Quản lý lịch hẹn khám của bạn", 16, 14, 480, 24, 12, true, TextColor));
                var bookButton = CreateButton("＋  Lịch hẹn mới", true);
                bookButton.Location = new Point(710, 15);
                bookButton.Size = new Size(150, 34);
                bookButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                bookButton.Click += (s, e) =>
                {
                    if (OpenBookingDialog())
                        ShowAppointments();
                };
                toolbar.Controls.Add(bookButton);
                page.Controls.Add(toolbar);

                var listCard = CreateCard(440);
                listCard.Controls.Add(CreateLabel("Lịch hẹn của tôi", 16, 14, 400, 22, 11, true, TextColor));
                var grid = CreateGrid(16, 48, 900, 325);
                var appointments = ClinicDAL.LayLichHenBenhNhan(_patient.MaBN);
                grid.DataSource = appointments;
                grid.Size = new Size(Math.Max(0, listCard.ClientSize.Width - 32), 325);
                HideColumns(grid, "MaBacSi", "TrangThai");
                DataGridViewColumn? statusColumn = grid.Columns["TrangThaiDuyet"];
                if (statusColumn != null)
                    statusColumn.HeaderText = "Trạng thái";
                listCard.SizeChanged += (s, e) => grid.Size = new Size(Math.Max(0, listCard.ClientSize.Width - 32), 325);
                listCard.Controls.Add(grid);
                var detailButton = CreateButton("Xem chi tiết", false);
                detailButton.Location = new Point(16, 390);
                detailButton.Size = new Size(130, 32);
                detailButton.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is DataRowView row)
                        ShowAppointmentDetail(row.Row);
                    else
                        MessageBox.Show("Chọn một lịch hẹn trước.", "Chưa chọn lịch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                var editButton = CreateButton("Sửa lịch hẹn", false);
                editButton.Location = new Point(156, 390);
                editButton.Size = new Size(130, 32);
                editButton.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is not DataRowView row)
                    {
                        MessageBox.Show("Chọn lịch hẹn cần sửa.", "Chưa chọn lịch",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (row.Row["TrangThai"].ToString() != "Chờ xác nhận" ||
                        row.Row.Field<DateTime>("NgayHen") <= DateTime.Now)
                    {
                        MessageBox.Show("Chỉ sửa được lịch tương lai đang chờ xác nhận.",
                            "Không thể sửa lịch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (OpenBookingDialog(row.Row))
                        ShowAppointments();
                };
                var cancelButton = CreateButton("Hủy lịch hẹn", false);
                cancelButton.Location = new Point(296, 390);
                cancelButton.Size = new Size(130, 32);
                cancelButton.ForeColor = Color.Firebrick;
                cancelButton.Click += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is not DataRowView row)
                    {
                        MessageBox.Show("Chọn lịch hẹn cần hủy.", "Chưa chọn lịch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (row.Row["TrangThai"].ToString() == "Đã hủy" ||
                        MessageBox.Show("Bạn chắc chắn muốn hủy lịch hẹn này?", "Xác nhận hủy",
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
                        MessageBox.Show(ex.Message, "Không thể hủy lịch", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                grid.CellDoubleClick += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is DataRowView row)
                        ShowAppointmentDetail(row.Row);
                };
                listCard.Controls.AddRange(new Control[] { detailButton, editButton, cancelButton });
                page.Controls.Add(listCard);
                _content.Controls.Add(page);
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private void ShowBookingList()
        {
            SetPage("Đặt lịch khám", _bookingButton);
            try
            {
                var page = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = PageBackground,
                    Padding = new Padding(4)
                };
                var toolbar = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 76,
                    BackColor = Color.White,
                    Padding = new Padding(12)
                };
                toolbar.Paint += (s, e) =>
                {
                    using var pen = new Pen(Color.FromArgb(229, 235, 238));
                    e.Graphics.DrawRectangle(pen, 0, 0, toolbar.Width - 1, toolbar.Height - 1);
                };
                toolbar.Controls.Add(CreateLabel("Quản lý các yêu cầu đặt lịch của bạn", 16, 10, 560, 24, 12, true, TextColor));
                toolbar.Controls.Add(CreateLabel("Bạn có thể sửa hoặc hủy lịch đang chờ xác nhận.", 16, 39, 600, 18, 8.5F, false, Muted));
                var addButton = CreateButton("＋  Đặt lịch mới", true);
                addButton.Location = new Point(toolbar.ClientSize.Width - 172, 18);
                addButton.Size = new Size(160, 36);
                addButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                addButton.Click += (s, e) =>
                {
                    if (OpenBookingDialog())
                        ShowBookingList();
                };
                toolbar.Controls.Add(addButton);
                page.Controls.Add(toolbar);

                var listCard = CreateCard(510);
                listCard.Dock = DockStyle.Fill;
                listCard.Margin = new Padding(0, 12, 0, 0);
                var grid = CreateGrid(16, 48, 900, 370);
                grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                var emptyLabel = CreateLabel("Bạn chưa có lịch hẹn. Bấm “Đặt lịch mới” để gửi yêu cầu.",
                    24, 190, 700, 28, 10, false, Muted);
                emptyLabel.Visible = false;
                void LoadAppointments()
                {
                    DataTable appointments = ClinicDAL.LayLichHenBenhNhan(_patient.MaBN);
                    grid.DataSource = appointments;
                    HideColumns(grid, "MaBacSi", "TrangThai");
                    DataGridViewColumn? statusColumn = grid.Columns["TrangThaiDuyet"];
                    if (statusColumn != null)
                        statusColumn.HeaderText = "Trạng thái";
                    emptyLabel.Visible = appointments.Rows.Count == 0;
                    grid.Visible = appointments.Rows.Count > 0;
                }
                var actions = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = Color.White };
                listCard.Controls.Add(actions);
                listCard.Controls.Add(CreateLabel("Danh sách lịch hẹn", 16, 14, 400, 22, 11, true, TextColor));
                listCard.Controls.Add(grid);
                listCard.Controls.Add(emptyLabel);

                DataRow? SelectedAppointment()
                    => grid.CurrentRow?.DataBoundItem is DataRowView view ? view.Row : null;

                var detailsButton = CreateButton("Xem chi tiết", false);
                detailsButton.Location = new Point(16, 10);
                detailsButton.Size = new Size(130, 34);
                detailsButton.Click += (s, e) =>
                {
                    DataRow? selected = SelectedAppointment();
                    if (selected == null)
                    {
                        MessageBox.Show("Chọn lịch hẹn cần xem.", "Chưa chọn lịch",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    ShowAppointmentDetail(selected);
                };

                var editButton = CreateButton("Sửa lịch hẹn", false);
                editButton.Location = new Point(156, 10);
                editButton.Size = new Size(130, 34);
                editButton.Click += (s, e) =>
                {
                    DataRow? selected = SelectedAppointment();
                    if (selected == null)
                    {
                        MessageBox.Show("Chọn lịch hẹn cần sửa.", "Chưa chọn lịch",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (selected["TrangThai"].ToString() != "Chờ xác nhận" ||
                        selected.Field<DateTime>("NgayHen") <= DateTime.Now)
                    {
                        MessageBox.Show("Chỉ sửa được lịch tương lai đang chờ xác nhận.",
                            "Không thể sửa lịch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (OpenBookingDialog(selected))
                        ShowBookingList();
                };

                var cancelButton = CreateButton("Hủy lịch hẹn", false);
                cancelButton.Location = new Point(296, 10);
                cancelButton.Size = new Size(130, 34);
                cancelButton.ForeColor = Color.Firebrick;
                cancelButton.Click += (s, e) =>
                {
                    DataRow? selected = SelectedAppointment();
                    if (selected == null)
                    {
                        MessageBox.Show("Chọn lịch hẹn cần hủy.", "Chưa chọn lịch",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (selected["TrangThai"].ToString() == "Đã hủy" ||
                        MessageBox.Show("Bạn chắc chắn muốn hủy lịch hẹn này?", "Xác nhận hủy",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                        return;
                    try
                    {
                        if (!ClinicDAL.HuyLichHenBenhNhan(
                                Convert.ToInt32(selected["MaLichHen"]), _patient.MaBN))
                            throw new InvalidOperationException("Lịch hẹn này không còn ở trạng thái có thể hủy.");
                        LoadAppointments();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Không thể hủy lịch",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                var refreshButton = CreateButton("Làm mới", false);
                refreshButton.Location = new Point(436, 10);
                refreshButton.Size = new Size(100, 34);
                refreshButton.Click += (s, e) =>
                {
                    try
                    {
                        LoadAppointments();
                    }
                    catch (Exception ex)
                    {
                        ShowLoadError(ex);
                    }
                };
                actions.Controls.AddRange(new Control[] { detailsButton, editButton, cancelButton, refreshButton });
                listCard.SizeChanged += (s, e) =>
                {
                    int gridHeight = Math.Max(120, listCard.ClientSize.Height - 120);
                    grid.Size = new Size(Math.Max(0, listCard.ClientSize.Width - 32), gridHeight);
                    emptyLabel.Location = new Point(24, 48 + Math.Max(0, (gridHeight - emptyLabel.Height) / 2));
                };
                page.Controls.Add(listCard);
                page.Controls.Add(toolbar);
                _content.Controls.Add(page);
                LoadAppointments();
            }
            catch (Exception ex)
            {
                ShowLoadError(ex);
            }
        }

        private bool OpenBookingDialog(DataRow? appointment = null)
        {
            try
            {
                DataTable doctors = ClinicDAL.LayDanhSachBacSiDatLich();
                using var dialog = new Form
                {
                    Text = appointment == null ? "Đặt lịch khám" : "Sửa lịch hẹn",
                    Size = new Size(590, 540),
                    MinimumSize = new Size(590, 540),
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
                    RowCount = 5,
                    BackColor = Color.White
                };
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
                fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

                var specialty = new ComboBox
                {
                    Dock = DockStyle.Fill,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = Font
                };
                string[] specialties = doctors.AsEnumerable()
                    .Select(row => row["ChuyenKhoa"].ToString() ?? "")
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

                var time = new ComboBox
                {
                    Dock = DockStyle.Left,
                    Width = 190,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = Font
                };
                void LoadTimeSlots()
                {
                    string? previous = time.SelectedItem?.ToString();
                    time.Items.Clear();
                    for (int hour = 8; hour <= 16; hour++)
                    {
                        foreach (string slot in new[] { $"{hour:00}:00", $"{hour:00}:30" })
                        {
                            DateTime slotTime = date.Value.Date.Add(TimeSpan.Parse(slot));
                            if (date.Value.Date > DateTime.Today || slotTime > DateTime.Now)
                                time.Items.Add(slot);
                        }
                    }
                    if (previous != null && time.Items.Contains(previous))
                        time.SelectedItem = previous;
                    else if (time.Items.Count > 0)
                        time.SelectedIndex = 0;
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
                AddField("Chuyên khoa", specialty, 0);
                AddField("Bác sĩ", doctor, 1);
                AddField("Ngày khám", date, 2);
                AddField("Lý do khám", reason, 3);
                var timeAndSave = new Panel { Dock = DockStyle.Fill };
                timeAndSave.Controls.Add(time);
                fields.Controls.Add(new Label
                {
                    Text = "Giờ khám",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = TextColor
                }, 0, 4);
                fields.Controls.Add(timeAndSave, 1, 4);

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
                }

                if (specialty.Items.Count > 0)
                {
                    string currentSpecialty = appointment?["ChuyenKhoa"]?.ToString() ?? "";
                    int currentIndex = Array.FindIndex(specialties,
                        value => string.Equals(value, currentSpecialty, StringComparison.OrdinalIgnoreCase));
                    specialty.SelectedIndex = currentIndex >= 0 ? currentIndex : 0;
                }
                specialty.SelectedIndexChanged += (s, e) => LoadDoctors();
                if (specialty.SelectedIndex >= 0)
                    LoadDoctors();
                if (appointment != null && doctor.Items.Count > 0)
                    doctor.SelectedValue = appointment["MaBacSi"];
                if (appointment != null)
                {
                    reason.Text = appointment["LyDoKham"] == DBNull.Value
                        ? ""
                        : appointment["LyDoKham"].ToString() ?? "";
                }
                LoadTimeSlots();
                if (appointment != null)
                {
                    string existingTime = appointment.Field<DateTime>("NgayHen").ToString("HH:mm");
                    if (!time.Items.Contains(existingTime))
                        time.Items.Add(existingTime);
                    time.SelectedItem = existingTime;
                }
                date.ValueChanged += (s, e) => LoadTimeSlots();

                var noDoctor = new Label
                {
                    Text = "Chưa có bác sĩ được phân công để nhận lịch hẹn. Vui lòng liên hệ lễ tân.",
                    Dock = DockStyle.Top,
                    Height = 44,
                    Padding = new Padding(18, 8, 18, 0),
                    ForeColor = Color.Firebrick,
                    Visible = doctors.Rows.Count == 0
                };
                save.Enabled = doctors.Rows.Count > 0;
                save.Click += (s, e) =>
                {
                    if (doctor.SelectedValue == null)
                    {
                        MessageBox.Show("Vui lòng chọn bác sĩ.", "Thiếu thông tin",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (time.SelectedItem == null)
                    {
                        MessageBox.Show("Ngày đã chọn không còn khung giờ khám. Vui lòng chọn ngày khác.",
                            "Không còn giờ khám", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    DateTime appointmentTime = date.Value.Date.Add(
                        TimeSpan.Parse(time.SelectedItem.ToString() ?? "08:00"));
                    try
                    {
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
                        MessageBox.Show(ex.Message, "Không thể lưu lịch hẹn",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                timeAndSave.Controls.Add(buttons);
                buttons.Dock = DockStyle.Bottom;
                dialog.Controls.Add(fields);
                dialog.Controls.Add(noDoctor);
                dialog.Controls.Add(heading);
                dialog.AcceptButton = save;
                dialog.CancelButton = cancel;
                return dialog.ShowDialog(this) == DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể mở biểu mẫu đặt lịch",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ShowBooking(DataRow? appointment = null, bool openForm = false)
        {
            if (!openForm)
            {
                ShowBookingList();
                return;
            }

            SetPage(appointment == null ? "Đặt lịch khám" : "Sửa lịch hẹn", _bookingButton);
            try
            {
                DataTable doctors = ClinicDAL.LayDanhSachBacSiDatLich();
                var page = CreatePageFlow();
                var columns = CreateColumns(590);
                var left = CreateColumn();
                var right = CreateColumn();

                var formCard = CreateCard(560);
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
                var specialties = doctors.AsEnumerable()
                    .Select(row => row["ChuyenKhoa"].ToString() ?? "")
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
                void LoadTimeSlots()
                {
                    time.Items.Clear();
                    for (int hour = 8; hour <= 16; hour++)
                    {
                        foreach (string slot in new[] { $"{hour:00}:00", $"{hour:00}:30" })
                        {
                            DateTime appointmentTime = date.Value.Date.Add(TimeSpan.Parse(slot));
                            if (date.Value.Date > DateTime.Today || appointmentTime > DateTime.Now)
                                time.Items.Add(slot);
                        }
                    }
                    if (time.Items.Count > 0)
                        time.SelectedIndex = 0;
                }
                LoadTimeSlots();
                date.ValueChanged += (s, e) => LoadTimeSlots();
                formCard.Controls.Add(time);
                formCard.Controls.Add(CreateLabel("Lý do khám", 18, 315, 160, 20, 9, true, TextColor));
                var reason = new TextBox
                {
                    Location = new Point(18, 339),
                    Size = new Size(390, 82),
                    Multiline = true,
                    MaxLength = 255,
                    ScrollBars = ScrollBars.Vertical,
                    Font = Font
                };
                formCard.Controls.Add(reason);
                formCard.Controls.Add(CreateLabel("Lịch hẹn sẽ ở trạng thái chờ xác nhận sau khi gửi yêu cầu.", 18, 434, 420, 36, 8.5F, false, Muted));

                var doctorInfo = CreateLabel("Chọn bác sĩ để xem phòng khám.", 18, 52, 310, 80, 10, false, Muted);
                var bookingCard = CreateCard(250);
                bookingCard.Controls.Add(CreateLabel("Thông tin lịch hẹn", 18, 15, 300, 24, 12, true, TextColor));
                bookingCard.Controls.Add(doctorInfo);
                var submit = CreateButton(appointment == null ? "Gửi yêu cầu đặt lịch" : "Lưu thay đổi", true);
                submit.Location = new Point(18, 172);
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
                    UpdateDoctorInfo();
                }

                void UpdateDoctorInfo()
                {
                    if (doctor.SelectedItem is DataRowView row)
                        doctorInfo.Text = $"Bác sĩ: {row["HoTen"]}\r\nChuyên khoa: {row["ChuyenKhoa"]}\r\nPhòng: {row["TenPhong"]}\r\nKhoa: {row["TenKhoa"]}";
                    else
                        doctorInfo.Text = doctors.Rows.Count == 0
                            ? "Hiện chưa có bác sĩ được phân công phòng khám."
                            : "Chọn bác sĩ để xem phòng khám.";
                }

                specialty.SelectedIndexChanged += (s, e) => LoadDoctors();
                doctor.SelectedIndexChanged += (s, e) => UpdateDoctorInfo();
                if (specialty.Items.Count > 0)
                {
                    LoadDoctors();
                    if (appointment != null)
                    {
                        doctor.SelectedValue = appointment["MaBacSi"];
                        string existingTime = appointment.Field<DateTime>("NgayHen").ToString("HH:mm");
                        if (!time.Items.Contains(existingTime))
                            time.Items.Add(existingTime);
                        time.SelectedItem = existingTime;
                        reason.Text = appointment["LyDoKham"] == DBNull.Value
                            ? ""
                            : appointment["LyDoKham"].ToString() ?? "";
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
                    if (time.SelectedItem == null)
                    {
                        MessageBox.Show("Ngày hôm nay không còn khung giờ đặt lịch. Vui lòng chọn ngày khác.",
                            "Không còn giờ khám", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (doctor.SelectedValue == null)
                    {
                        MessageBox.Show("Hiện chưa có bác sĩ phù hợp để đặt lịch.", "Chưa có bác sĩ",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    DateTime appointmentTime = date.Value.Date.Add(TimeSpan.Parse(time.SelectedItem?.ToString() ?? "08:00"));
                    try
                    {
                        ClinicDAL.DatLichHenBenhNhan(_patient.MaBN, doctor.SelectedValue.ToString() ?? "",
                            appointmentTime, reason.Text,
                            appointment == null ? null : Convert.ToInt32(appointment["MaLichHen"]));
                        MessageBox.Show(appointment == null
                                ? "Đã gửi yêu cầu đặt lịch. Vui lòng theo dõi trạng thái trong mục Lịch hẹn."
                                : "Đã cập nhật lịch hẹn của bạn.",
                            appointment == null ? "Đặt lịch thành công" : "Cập nhật thành công",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ShowAppointments();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Không thể đặt lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                var tabCard = CreateCard(510);
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
                var historyGrid = CreateGrid(10, 10, 0, 0);
                historyGrid.Dock = DockStyle.Fill;
                historyGrid.DataSource = ClinicDAL.LayLichSuKhamBenhNhan(_patient.MaBN);
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
                AddProfileField(profilePanel, "Địa chỉ", FormatValue(profile["DiaChi"]), 215);
                AddProfileField(profilePanel, "Tiền sử bệnh / dị ứng", FormatValue(profile["TienSuBenh"]), 255);
                historyTab.Controls.Add(historyGrid);
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

                var detailCard = CreateCard(390);
                detailCard.Controls.Add(CreateLabel("Chi tiết viện phí", 16, 14, 300, 22, 11, true, TextColor));
                var detailText = CreateLabel("Chọn một hóa đơn để xem thông tin.", 16, 52, 300, 85, 9, false, Muted);
                detailCard.Controls.Add(detailText);
                var total = CreateLabel("", 16, 155, 300, 34, 20, true, Teal);
                detailCard.Controls.Add(total);
                var payHint = CreateLabel("Nếu hóa đơn còn chưa thanh toán, vui lòng đến quầy thu ngân của phòng khám để được hỗ trợ. Không chia sẻ thông tin thanh toán qua kênh không chính thức.",
                    16, 205, 300, 105, 8.5F, false, Muted);
                detailCard.Controls.Add(payHint);
                grid.SelectionChanged += (s, e) =>
                {
                    if (grid.CurrentRow?.DataBoundItem is not DataRowView row)
                        return;
                    DataRow item = row.Row;
                    detailText.Text =
                        $"Mã hóa đơn: {Value(item, "MaHD")}\r\nMã phiếu khám: {Value(item, "MaPK")}\r\n" +
                        $"Ngày lập: {FormatValue(item["NgayLap"])}\r\nTrạng thái: {Value(item, "TrangThai")}\r\n" +
                        $"Hình thức: {Value(item, "HinhThucTT")}";
                    total.Text = $"{Convert.ToDecimal(item["TongTien"]):N0} đ";
                    payHint.Text = item["TrangThai"].ToString() == "Đã thanh toán"
                        ? "Hóa đơn này đã được thanh toán. Liên hệ phòng khám nếu bạn cần hỗ trợ thêm."
                        : "Hóa đơn chưa thanh toán. Vui lòng đến quầy thu ngân để được kiểm tra chi tiết và thanh toán.";
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
                Size = new Size(0, 310),
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
                Size = new Size(0, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                PlaceholderText = "Nhập câu hỏi..."
            };
            var send = CreateButton("Gửi", true);
            send.Location = new Point(0, 372);
            send.Size = new Size(80, 34);
            send.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            question.Width = 500;
            send.Left = 530;
            void PositionChatControls()
            {
                question.Width = Math.Max(150, chat.ClientSize.Width - 130);
                send.Left = chat.ClientSize.Width - send.Width - 16;
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

        private TableLayoutPanel CreateColumns(int height)
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
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
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

        private static Label CreateLabel(string text, int x, int y, int width, int height, bool bold, Color color)
            => CreateLabel(text, x, y, width, height, 9F, bold, color);

        private static Label CreateLabel(string text, int x, int y, int width, int height, float size, bool bold, Color color)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color,
                AutoEllipsis = true
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
                Font = new Font("Segoe UI", 9F, primary ? FontStyle.Bold : FontStyle.Regular),
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
                Font = new Font("Segoe UI", 8F, FontStyle.Bold)
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
                Font = new Font("Segoe UI", 8.5F),
                ColumnHeadersHeight = 36,
                EnableHeadersVisualStyles = false
            };
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 250);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            grid.DefaultCellStyle.SelectionBackColor = TealLight;
            grid.DefaultCellStyle.SelectionForeColor = TextColor;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 252);
            grid.RowTemplate.Height = 32;
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
            MessageBox.Show(exception.Message, "Lỗi tải dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
