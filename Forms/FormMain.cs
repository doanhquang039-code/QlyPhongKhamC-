using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using QlyPhongKham.DAL;
using QlyPhongKham.Helpers;

namespace QlyPhongKham.Forms
{
    // ═══════════════════════════════════════════════════════════
    //  CUSTOM CONTROLS  (dùng chung toàn dự án)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Panel với góc bo tròn</summary>
    public class RoundedPanel : Panel
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius { get; set; } = 12;

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = GetRoundedRect(ClientRectangle, Radius);
            using var br   = new SolidBrush(BackColor);
            e.Graphics.FillPath(br, path);
        }
        protected override void OnPaintBackground(PaintEventArgs e) { /* suppress default */ }

        private static GraphicsPath GetRoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0,   90);
            path.AddArc(r.X,         r.Bottom - d, d, d, 90,  90);
            path.CloseFigure();
            return path;
        }
    }

    /// <summary>Button với góc bo tròn + hover effect</summary>
    public class ModernButton : Button
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Radius { get; set; } = 8;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color HoverColor { get; set; }

        private bool _hovered;
        public ModernButton() { FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; Cursor = Cursors.Hand; }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color c  = _hovered && HoverColor != Color.Empty ? HoverColor : BackColor;
            using (var path = RoundedRect(rect, Radius))
            using (var br   = new SolidBrush(c))
                e.Graphics.FillPath(br, path);
            TextRenderer.DrawText(e.Graphics, Text, Font, rect, ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X,         r.Y,          d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0,   90);
            path.AddArc(r.X,         r.Bottom - d, d, d, 90,  90);
            path.CloseFigure();
            return path;
        }
    }

    // ═══════════════════════════════════════════════════════════
    //  FORM MAIN  –  Shell (sidebar, topbar, routing, helpers)
    //  Các view được tách thành partial class ở subfolder:
    //    Admin/FormMain.Admin.cs
    //    BacSi/FormMain.BacSi.cs
    //    LeTan/FormMain.LeTan.cs
    // ═══════════════════════════════════════════════════════════
    public partial class FormMain : Form
    {
        // ── Nav buttons ──
        private Button? _activeNavBtn;
        internal Button btnNavTongQuan  = null!;
        internal Button btnNavLichHen   = null!;
        internal Button btnNavBenhNhan  = null!;
        internal Button btnNavKhamBenh  = null!;
        internal Button btnNavKhoThuoc  = null!;
        internal Button btnNavThuNgan   = null!;
        internal Button btnNavNhanVien  = null!;
        internal Button btnNavBaoCao    = null!;
    internal Button btnNavNhatKy     = null!;
        internal Button btnNavKhoaPhong = null!;
        internal Button btnNavDichVu    = null!;
        internal Panel  panelSidebar    = null!;
        internal Panel  panelContent    = null!;
        private Panel _sidebarHeader = null!;
        private Panel _sidebarNavigation = null!;
        private Panel _sidebarFooter = null!;
        private Bitmap? _sidebarProfileAvatar;
        private System.Windows.Forms.Timer? _noShowTimer;
        private  Label  lblPageTitle    = null!;
        private  Label  lblBreadcrumb   = null!;
        private  Panel  topBar          = null!;
        private Button btnRefreshPage = null!;
        private Action? _refreshPageAction;

        // ── Bell notification ──
        private Button _btnBell = null!;
        private Label  _lblBellBadge = null!;
        private System.Windows.Forms.Timer? _notifTimer;

        // ── Palette "An Tâm" ──
        internal static readonly Color C_Sidebar    = Color.FromArgb(15,  32,  54);
        internal static readonly Color C_SidebarSub = Color.FromArgb(22,  42,  68);
        internal static readonly Color C_Teal       = Color.FromArgb(0,   180, 166);
        internal static readonly Color C_TealDark   = Color.FromArgb(0,   145, 133);
        internal static readonly Color C_Blue       = Color.FromArgb(59,  130, 246);
        internal static readonly Color C_Green      = Color.FromArgb(34,  197, 94);
        internal static readonly Color C_Orange     = Color.FromArgb(251, 146, 60);
        internal static readonly Color C_Red        = Color.FromArgb(239, 68,  68);
        internal static readonly Color C_Purple     = Color.FromArgb(168, 85,  247);
        internal static readonly Color C_Bg         = Color.FromArgb(244, 246, 250);
        internal static readonly Color C_Card       = Color.White;
        internal static readonly Color C_TextDark   = Color.FromArgb(15,  32,  54);
        internal static readonly Color C_TextGray   = Color.FromArgb(107, 114, 128);
        internal static readonly Color C_Border     = Color.FromArgb(229, 231, 235);
        internal static readonly Font  F_Base       = new Font("Segoe UI", 9.5f);
        internal static readonly Font  F_Bold       = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        internal static readonly Font  F_Small      = new Font("Segoe UI", 8.5f);
        internal static readonly Font  F_Title      = new Font("Segoe UI", 14f,  FontStyle.Bold);

        // ════════════════════════════════════════════════════════
        public FormMain()
        {
            InitializeComponent();
            ClinicDAL.EnsureClinicRoomSchema();
            ClinicDAL.EnsurePharmacySchema();
            Resize += (s, e) => ApplyResponsiveLayout();
            ApplyRolePermissions();
        }

        private void InitializeComponent()
        {
            this.Text          = "An Tâm – Hệ thống Quản lý Phòng Khám";
            this.Size          = new Size(1360, 820);
            this.MinimumSize   = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor     = C_Bg;
            this.Font          = F_Base;

            BuildSidebar();
            BuildTopBar();
            BuildContentArea();
        }

        // ════════════════════════════════════════════════════════
        //  SIDEBAR
        // ════════════════════════════════════════════════════════
        private void BuildSidebar()
        {
            panelSidebar = new Panel { Dock = DockStyle.Left, Width = 240, BackColor = C_Sidebar };

            // Combined Header: Logo (0..72) + User info card (72..144)
            var headerSidebar = new Panel { Dock = DockStyle.Top, Height = 144, BackColor = C_Sidebar };
            _sidebarHeader = headerSidebar;
            _sidebarNavigation = new Panel { Dock = DockStyle.Fill, BackColor = C_Sidebar, AutoScroll = true };
            _sidebarFooter = new Panel { Dock = DockStyle.Bottom, Height = 150, BackColor = C_Sidebar };

            var logoPanel = new Panel { Location = new Point(0, 0), Size = new Size(240, 72), BackColor = C_Sidebar };
            logoPanel.Controls.Add(new Label { Text = "+",      Font = new Font("Segoe UI", 22f, FontStyle.Bold), ForeColor = C_Teal,   Location = new Point(18, 16), AutoSize = true });
            logoPanel.Controls.Add(new Label { Text = "An Tâm", Font = new Font("Segoe UI", 17f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(54, 20), AutoSize = true });

            var userCard  = new Panel { Location = new Point(0, 72), Size = new Size(240, 72), BackColor = C_SidebarSub };
            var avatarLbl = new Label
            {
                Text      = (SessionState.CurrentUser?.HoTenNhanVien ?? "?").Substring(0, 1).ToUpper(),
                Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White, BackColor = C_Teal,
                Location  = new Point(14, 16), Size = new Size(40, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };
            userCard.Controls.Add(avatarLbl);
            userCard.Controls.Add(new Label { Text = SessionState.CurrentUser?.HoTenNhanVien ?? "Người dùng", Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(62, 14), Size = new Size(165, 20) });
            userCard.Controls.Add(new Label { Text = GetRoleDisplayName(SessionState.CurrentUser?.Quyen ?? ""), Font = F_Small, ForeColor = Color.FromArgb(148, 163, 184), Location = new Point(62, 36), Size = new Size(165, 18) });

            headerSidebar.Controls.AddRange(new Control[] { logoPanel, userCard });

            // Section label
            var lblSection = new Label { Text = "MENU CHÍNH", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(16, 12), AutoSize = true };

            // Nav buttons
            btnNavTongQuan  = CreateNavBtn("🏠", "Tổng quan",         32);
            btnNavLichHen   = CreateNavBtn("📋", "Lịch hẹn hôm nay",  78);
            btnNavBenhNhan  = CreateNavBtn("👤", "Bệnh nhân",         124);
            btnNavKhamBenh  = CreateNavBtn("🩺", "Phòng khám",        170);
            btnNavKhoThuoc  = CreateNavBtn("💊", "Kho thuốc",         216);
            btnNavDichVu    = CreateNavBtn("🔬", "Bảng giá dịch vụ",   262);
            btnNavThuNgan   = CreateNavBtn("💰", "Thu ngân & HĐ",     308);
            btnNavNhanVien  = CreateNavBtn("👥", "Nhân sự",            354);
            btnNavBaoCao    = CreateNavBtn("📊", "Báo cáo",            400);
            btnNavKhoaPhong = CreateNavBtn("🏥", "Khoa / Phòng khám",  446);
            btnNavNhatKy    = CreateNavBtn("📜", "Nhật ký",            492);

            btnNavTongQuan.Click += (s, e) =>
            {
                if (SessionState.IsReceptionist) ShowLeTanDashboard();
                else if (SessionState.IsAdmin) ShowDashboard();
            };
            btnNavLichHen.Click  += (s, e) =>
            {
                if (SessionState.IsDoctor) ShowBacSiLichKhamView();
                else if (SessionState.IsAdmin) ShowAdminAppointmentsView();
                else if (SessionState.IsReceptionist) ShowTiepDonView();
            };
            btnNavBenhNhan.Click += (s, e) =>
            {
                if (SessionState.IsDoctor) ShowBacSiHoSoBenhAnView();
                else if (SessionState.IsReceptionist || SessionState.IsAdmin) ShowBenhNhanView();
            };
            btnNavKhamBenh.Click += (s, e) =>
            {
                if (SessionState.IsDoctor) ShowBacSiKhamKeDonView();
                else if (SessionState.IsAdmin) ShowAdminClinicView();
                else if (SessionState.IsReceptionist) ShowLeTanLichKhamView();
            };
            btnNavKhoThuoc.Click += (s, e) =>
            {
                if (SessionState.IsDoctor) ShowBacSiThuocView();
                else if (SessionState.IsReceptionist || SessionState.IsAdmin) ShowKhoThuocView();
            };
            btnNavDichVu.Click += (s, e) =>
            {
                ShowBacSiDichVuView();
            };
            btnNavThuNgan.Click  += (s, e) =>
            {
                if (SessionState.IsCashier || SessionState.IsAdmin)
                    ShowThuNganView();
            };
            btnNavNhanVien.Click += (s, e) => ShowNhanVienView();
            btnNavBaoCao.Click   += (s, e) => ShowBaoCaoView();
            btnNavKhoaPhong.Click += (s, e) =>
            {
                if (SessionState.IsAdmin)
                    ShowKhoaPhongView();
            };
            btnNavNhatKy.Click += (s, e) =>
            {
                if (SessionState.IsAdmin)
                    ShowNhatKyView();
            };

            var profileCard = new Panel
            {
                Location = new Point(10, 6),
                Size = new Size(220, 52),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = C_SidebarSub,
                Cursor = Cursors.Hand
            };
            _sidebarProfileAvatar = ProfileImageHelper.CreateAvatar(
                ClinicDAL.LayAnhNhanVien(SessionState.CurrentUser?.MaNV ?? ""),
                SessionState.CurrentUser?.HoTenNhanVien ?? "Người dùng",
                38);
            var profileAvatar = new PictureBox
            {
                Image = _sidebarProfileAvatar,
                Location = new Point(8, 7),
                Size = new Size(38, 38),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = C_SidebarSub
            };
            profileCard.Controls.Add(profileAvatar);
            profileCard.Controls.Add(new Label
            {
                Text = "Hồ sơ cá nhân",
                Location = new Point(56, 8),
                Size = new Size(145, 19),
                ForeColor = Color.White,
                Font = F_Bold,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            });
            profileCard.Controls.Add(new Label
            {
                Text = "Xem thông tin của bạn  ›",
                Location = new Point(56, 28),
                Size = new Size(155, 16),
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = F_Small,
                Cursor = Cursors.Hand
            });
            profileCard.MouseEnter += (s, e) => profileCard.BackColor = Color.FromArgb(30, 58, 84);
            profileCard.MouseLeave += (s, e) => profileCard.BackColor = C_SidebarSub;
            foreach (Control control in profileCard.Controls)
            {
                control.MouseEnter += (s, e) => profileCard.BackColor = Color.FromArgb(30, 58, 84);
                control.MouseLeave += (s, e) => profileCard.BackColor = C_SidebarSub;
                control.Click += (s, e) => ShowEmployeeProfile();
            }
            profileCard.Click += (s, e) => ShowEmployeeProfile();

            var btnLogout = new Button { Text = "  🚪  Thoát tài khoản", Size = new Size(220, 38), Location = new Point(10, 64), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(39, 22, 22), ForeColor = Color.FromArgb(252, 165, 165), FlatStyle = FlatStyle.Flat, Font = F_Bold, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) =>
            {
                if (MessageBox.Show("Bạn muốn thoát tài khoản và quay lại màn hình đăng nhập?",
                        "Thoát tài khoản", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;

                SessionState.CurrentUser = null;
                this.Close();
            };

            var btnCfg = new Button { Text = "  ⚙  Cấu hình máy chủ", Size = new Size(220, 34), Location = new Point(10, 106), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.Transparent, ForeColor = Color.FromArgb(100, 116, 139), FlatStyle = FlatStyle.Flat, Font = F_Small, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };
            btnCfg.FlatAppearance.BorderSize = 0;
            btnCfg.Click += (s, e) => new FormConfigDb().ShowDialog();

            _sidebarNavigation.Controls.AddRange(new Control[] { lblSection, btnNavTongQuan, btnNavLichHen, btnNavBenhNhan, btnNavKhamBenh, btnNavKhoThuoc, btnNavDichVu, btnNavThuNgan, btnNavNhanVien, btnNavBaoCao, btnNavKhoaPhong, btnNavNhatKy });
            _sidebarFooter.Controls.AddRange(new Control[] { profileCard, btnLogout, btnCfg });
            panelSidebar.Controls.Add(_sidebarNavigation);
            panelSidebar.Controls.Add(_sidebarFooter);
            panelSidebar.Controls.Add(headerSidebar);
            this.Controls.Add(panelSidebar);
            FormClosed += (s, e) => _sidebarProfileAvatar?.Dispose();
        }

        private void ShowEmployeeProfile()
        {
            string maNV = SessionState.CurrentUser?.MaNV ?? "";
            if (string.IsNullOrWhiteSpace(maNV))
            {
                MessageBox.Show(this, "Không xác định được mã nhân viên đang đăng nhập.",
                    "Không thể mở hồ sơ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var dialog = new FormEmployeeProfile(maNV);
                dialog.ShowDialog(this);
            }
            catch (Exception exception) when (exception is SqlException or InvalidOperationException or ArgumentException or ExternalException)
            {
                MessageBox.Show(this, $"Không thể tải hồ sơ cá nhân.\n\n{exception.Message}",
                    "Lỗi tải hồ sơ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        internal Button CreateNavBtn(string icon, string label, int top)
        {
            var btn = new Button { Text = $"  {icon}  {label}", Location = new Point(8, top), Size = new Size(224, 42), BackColor = Color.Transparent, ForeColor = Color.FromArgb(203, 213, 225), FlatStyle = FlatStyle.Flat, Font = F_Base, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 255, 255);
            btn.MouseEnter += (s, e) => { if (btn != _activeNavBtn) btn.BackColor = Color.FromArgb(25, 255, 255, 255); };
            btn.MouseLeave += (s, e) => { if (btn != _activeNavBtn) btn.BackColor = Color.Transparent; };
            return btn;
        }

        internal void SetActiveNav(Button btn)
        {
            _noShowTimer?.Stop();
            _noShowTimer?.Dispose();
            _noShowTimer = null;
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Color.Transparent;
                _activeNavBtn.ForeColor = Color.FromArgb(203, 213, 225);
            }
            _activeNavBtn = btn;
            btn.BackColor = Color.FromArgb(0, 180, 166, 40);
            btn.ForeColor = C_Teal;
        }

        internal void StartNoShowTimer(Action refreshAppointments)
        {
            _noShowTimer?.Stop();
            _noShowTimer?.Dispose();
            _noShowTimer = new System.Windows.Forms.Timer { Interval = 60_000 };
            _noShowTimer.Tick += (sender, args) =>
            {
                try
                {
                    refreshAppointments();
                }
                catch (Exception ex)
                {
                    _noShowTimer?.Stop();
                    MessageBox.Show(this, ex.Message, "Không thể tự cập nhật lịch vắng",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            _noShowTimer.Start();
            FormClosed -= StopNoShowTimer;
            FormClosed += StopNoShowTimer;
        }

        private void StopNoShowTimer(object? sender, FormClosedEventArgs e)
        {
            _noShowTimer?.Stop();
            _noShowTimer?.Dispose();
            _noShowTimer = null;
        }

        // ════════════════════════════════════════════════════════
        //  TOP BAR
        // ════════════════════════════════════════════════════════
        private void BuildTopBar()
        {
            topBar = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = C_Card, Padding = new Padding(24, 0, 20, 0) };
            topBar.Paint += (s, e) => { using var pen = new Pen(C_Border, 1); e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1); };

            lblBreadcrumb = new Label { Text = "Quản trị viên  ›  Tổng quan", Font = F_Small, ForeColor = C_TextGray, Location = new Point(24, 12), AutoSize = true };
            lblPageTitle  = new Label { Text = "Tổng quan", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = C_TextDark, Location = new Point(24, 30), AutoSize = true };
            var lblDate   = new Label { Text = $"📅  {DateTime.Now:dddd, dd/MM/yyyy}", Font = F_Small, ForeColor = C_TextGray, AutoSize = true };

            btnRefreshPage = SecondaryBtn("↻ Làm mới", 0, 15, 100, 34, C_Blue);
            btnRefreshPage.Click += (s, e) => _refreshPageAction?.Invoke();

            // ── Bell button ──────────────────────────────────────────
            _btnBell = new Button
            {
                Text      = "🔔",
                Size      = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = C_TextDark,
                Font      = new Font("Segoe UI", 15f),
                Cursor    = Cursors.Hand
            };
            _btnBell.FlatAppearance.BorderSize = 0;
            _btnBell.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 0, 0, 0);

            _lblBellBadge = new Label
            {
                Text      = "",
                Size      = new Size(18, 18),
                BackColor = C_Red,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible   = false
            };

            _btnBell.Click += (s, e) => ShowNotificationPanel();

            void PositionTopBarRightControls()
            {
                // Sắp xếp thứ tự từ phải qua trái với khoảng cách hợp lý:
                // [lblDate]  (cách 20px)  [btnRefreshPage]  (cách 12px)  [🔔 icon]  (cách lề phải 20px)
                int rightEdge = topBar.ClientSize.Width - 20;

                _btnBell.Location      = new Point(rightEdge - _btnBell.Width, (topBar.ClientSize.Height - _btnBell.Height) / 2);
                _lblBellBadge.Location = new Point(_btnBell.Right - 14, _btnBell.Top - 2);

                btnRefreshPage.Location = new Point(_btnBell.Left - 12 - btnRefreshPage.Width, (topBar.ClientSize.Height - btnRefreshPage.Height) / 2);

                lblDate.Location = new Point(btnRefreshPage.Left - 20 - lblDate.Width, (topBar.ClientSize.Height - lblDate.Height) / 2);
            }
            topBar.SizeChanged += (s, e) => PositionTopBarRightControls();
            PositionTopBarRightControls();

            topBar.Controls.AddRange(new Control[] { lblBreadcrumb, lblPageTitle, btnRefreshPage, lblDate, _btnBell, _lblBellBadge });
            this.Controls.Add(topBar);

            // Poll thông báo mỗi 30 giây
            if (SessionState.CurrentUser != null)
            {
                Helpers.NotificationHelper.EnsureSchema();
                _notifTimer = new System.Windows.Forms.Timer { Interval = 30_000 };
                _notifTimer.Tick += (s, e) => RefreshBellBadge();
                _notifTimer.Start();
                RefreshBellBadge();
                FormClosed += (s, e) => { _notifTimer?.Stop(); _notifTimer?.Dispose(); };
            }
        }

        private void RefreshBellBadge()
        {
            try
            {
                string maNV = SessionState.CurrentUser?.MaNV ?? "";
                if (string.IsNullOrEmpty(maNV)) return;
                int count = Helpers.NotificationHelper.LaySoChuaDoc(maNV);
                if (_lblBellBadge.InvokeRequired)
                {
                    _lblBellBadge.Invoke(() => UpdateBadge(count));
                }
                else
                {
                    UpdateBadge(count);
                }
            }
            catch { }
        }

        private void UpdateBadge(int count)
        {
            _lblBellBadge.Visible = count > 0;
            _lblBellBadge.Text    = count > 99 ? "99+" : count.ToString();
        }

        private void ShowNotificationPanel()
        {
            string maNV = SessionState.CurrentUser?.MaNV ?? "";
            var dt = Helpers.NotificationHelper.LayThongBao(maNV);

            var popup = new Form
            {
                Text            = "🔔 Thông báo",
                Size            = new Size(460, 500),
                StartPosition   = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.FixedSingle,
                MaximizeBox     = false,
                MinimizeBox     = false,
                BackColor       = Color.White,
                TopMost         = true
            };
            // Hiển thị popup bên dưới bell
            var bellScreen = _btnBell.PointToScreen(Point.Empty);
            popup.Location = new Point(
                Math.Max(0, bellScreen.X + _btnBell.Width - popup.Width),
                bellScreen.Y + _btnBell.Height + 4);

            var header = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = C_Sidebar, Padding = new Padding(12, 10, 12, 0) };
            header.Controls.Add(new Label { Text = "Thông báo hệ thống", ForeColor = Color.White, Font = F_Bold, AutoSize = true, Location = new Point(12, 14) });
            var btnMarkAll = new Button
            {
                Text = "Đánh dấu tất cả đã đọc", Size = new Size(170, 26),
                FlatStyle = FlatStyle.Flat, BackColor = C_Teal, ForeColor = Color.White,
                Font = F_Small, Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnMarkAll.FlatAppearance.BorderSize = 0;
            btnMarkAll.Location = new Point(popup.ClientSize.Width - btnMarkAll.Width - 12, 11);
            header.Controls.Add(btnMarkAll);

            var listPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };

            void RenderNotifications()
            {
                listPanel.Controls.Clear();
                int y = 8;
                if (dt.Rows.Count == 0)
                {
                    listPanel.Controls.Add(new Label
                    {
                        Text = "Không có thông báo nào.", Font = F_Base, ForeColor = C_TextGray,
                        Location = new Point(12, 20), AutoSize = true
                    });
                    return;
                }
                foreach (DataRow r in dt.Rows)
                {
                    bool daDoc = Convert.ToBoolean(r["DaDoc"]);
                    var card = new Panel
                    {
                        Location  = new Point(8, y),
                        Size      = new Size(listPanel.ClientSize.Width - 28, 68),
                        BackColor = daDoc ? Color.White : Color.FromArgb(236, 253, 250),
                        Cursor    = Cursors.Hand
                    };
                    card.Paint += (s, e) =>
                    {
                        using var pen = new Pen(C_Border, 1);
                        e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                        if (!daDoc)
                        {
                            using var accent = new System.Drawing.SolidBrush(C_Teal);
                            e.Graphics.FillRectangle(accent, 0, 0, 4, card.Height);
                        }
                    };
                    card.Controls.Add(new Label
                    {
                        Text     = r["TieuDe"].ToString(),
                        Location = new Point(12, 6), Size = new Size(card.Width - 100, 20),
                        Font     = F_Bold, ForeColor = C_TextDark, AutoEllipsis = true
                    });
                    card.Controls.Add(new Label
                    {
                        Text     = r["NoiDung"].ToString(),
                        Location = new Point(12, 26), Size = new Size(card.Width - 24, 28),
                        Font     = F_Small, ForeColor = C_TextGray, AutoEllipsis = true
                    });
                    card.Controls.Add(new Label
                    {
                        Text     = Convert.ToDateTime(r["NgayTao"]).ToString("dd/MM HH:mm"),
                        Location = new Point(card.Width - 92, 6), Size = new Size(80, 18),
                        Font     = F_Small, ForeColor = C_TextGray, TextAlign = ContentAlignment.MiddleRight
                    });
                    long maThongBao = Convert.ToInt64(r["MaThongBao"]);
                    card.Click += (s, e) =>
                    {
                        Helpers.NotificationHelper.DanhDauDaDoc(maNV, maThongBao);
                        RefreshBellBadge();
                        popup.Close();
                    };
                    listPanel.Controls.Add(card);
                    y += card.Height + 6;
                }
            }
            RenderNotifications();

            btnMarkAll.Click += (s, e) =>
            {
                Helpers.NotificationHelper.DanhDauDaDoc(maNV);
                foreach (DataRow r in dt.Rows) r["DaDoc"] = true;
                RenderNotifications();
                UpdateBadge(0);
            };

            popup.Controls.Add(listPanel);
            popup.Controls.Add(header);
            popup.Show(this);
        }

        internal void SetRefreshPageAction(Action refreshAction)
        {
            _refreshPageAction = refreshAction;
            btnRefreshPage.Enabled = true;
        }

        internal void SetPageHeader(string breadcrumb, string title)
        {
            lblBreadcrumb.Text = breadcrumb;
            lblPageTitle.Text  = title;
        }

        // ════════════════════════════════════════════════════════
        //  CONTENT AREA
        // ════════════════════════════════════════════════════════
        private void BuildContentArea()
        {
            panelContent = new Panel { Dock = DockStyle.Fill, BackColor = C_Bg, Padding = new Padding(20, 16, 20, 16) };
            this.Controls.Add(panelContent);
        }

        private void ApplyResponsiveLayout()
        {
            if (panelSidebar == null || panelSidebar.IsDisposed)
                return;

            int sidebarWidth = Math.Max(240, (int)Math.Round(ClientSize.Width * 0.2));
            if (panelSidebar.Width != sidebarWidth)
                panelSidebar.Width = sidebarWidth;

            int contentTop = topBar.Bottom;
            panelContent.Dock = DockStyle.None;
            panelContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelContent.Location = new Point(sidebarWidth, contentTop);
            panelContent.Size = new Size(
                Math.Max(0, ClientSize.Width - sidebarWidth),
                Math.Max(0, ClientSize.Height - contentTop));

            if (_sidebarHeader != null)
            {
                _sidebarHeader.Width = sidebarWidth;
                if (_sidebarHeader.Controls.Count > 0)
                    _sidebarHeader.Controls[0].Size = new Size(sidebarWidth, 72);
                if (_sidebarHeader.Controls.Count > 1)
                {
                    var userCard = _sidebarHeader.Controls[1];
                    userCard.Size = new Size(sidebarWidth, 72);
                    foreach (Control control in userCard.Controls)
                    {
                        if (control is Label label && label.Location.X >= 62)
                            label.Width = sidebarWidth - label.Left - 12;
                    }
                }
            }

            foreach (Control control in _sidebarNavigation.Controls)
            {
                if (control is not Button button)
                    continue;
                button.Width = Math.Max(0, _sidebarNavigation.ClientSize.Width - 16);
            }
            foreach (Control control in _sidebarFooter.Controls)
                if (control is Button button)
                    button.Width = Math.Max(0, _sidebarFooter.ClientSize.Width - 20);
        }

        // ════════════════════════════════════════════════════════
        //  PHÂN QUYỀN THEO ROLE
        // ════════════════════════════════════════════════════════
        private void ApplyRolePermissions()
        {
            if (SessionState.IsDoctor)
            {
                ApplyResponsiveLayout();

                btnNavTongQuan.Visible = false;
                btnNavThuNgan.Visible  = false;
                btnNavNhanVien.Visible = false;
                btnNavBaoCao.Visible   = false;
                btnNavKhoaPhong.Visible = false;
                btnNavNhatKy.Visible   = false;

                btnNavLichHen.Visible  = true;
                btnNavLichHen.Text     = "  📅  Lịch khám";
                btnNavLichHen.Location = new Point(8, 32);

                btnNavBenhNhan.Visible = true;
                btnNavBenhNhan.Text    = "  👤  Bệnh nhân";
                btnNavBenhNhan.Location = new Point(8, 78);

                btnNavKhamBenh.Visible = true;
                btnNavKhamBenh.Text    = "  🩺  Khám & Kê đơn";
                btnNavKhamBenh.Location = new Point(8, 124);

                btnNavKhoThuoc.Visible = true;
                btnNavKhoThuoc.Text    = "  💊  Danh mục thuốc";
                btnNavKhoThuoc.Location = new Point(8, 170);

                btnNavDichVu.Visible   = true;
                btnNavDichVu.Text      = "  🔬  Bảng giá dịch vụ";
                btnNavDichVu.Location  = new Point(8, 216);

                ShowBacSiLichKhamView();
            }
            else if (SessionState.IsReceptionist)
            {
                btnNavTongQuan.Visible = true;
                btnNavTongQuan.Text = "  🏠  Trang chủ";
                btnNavTongQuan.Location = new Point(8, 32);
                btnNavLichHen.Visible = true;
                btnNavLichHen.Text = "  🧾  Tiếp nhận bệnh nhân";
                btnNavLichHen.Location = new Point(8, 78);
                btnNavBenhNhan.Visible = true;
                btnNavBenhNhan.Text = "  👤  Bệnh nhân";
                btnNavBenhNhan.Location = new Point(8, 124);
                btnNavKhamBenh.Visible = true;
                btnNavKhamBenh.Text = "  📅  Lịch khám";
                btnNavKhamBenh.Location = new Point(8, 170);
                btnNavKhoThuoc.Visible = true;
                btnNavKhoThuoc.Text = "  💊  Kho thuốc";
                btnNavKhoThuoc.Location = new Point(8, 216);
                btnNavDichVu.Visible   = false;
                btnNavThuNgan.Visible = false;
                btnNavNhanVien.Visible = false;
                btnNavBaoCao.Visible   = false;
                btnNavKhoaPhong.Visible = false;
                btnNavNhatKy.Visible   = false;
                ApplyResponsiveLayout();
                ShowLeTanDashboard();
            }
            else if (SessionState.IsCashier)
            {
                btnNavTongQuan.Visible = false;
                btnNavLichHen.Visible = false;
                btnNavBenhNhan.Visible = false;
                btnNavKhamBenh.Visible = false;
                btnNavKhoThuoc.Visible = false;
                btnNavDichVu.Visible   = false;
                btnNavNhanVien.Visible = false;
                btnNavBaoCao.Visible = false;
                btnNavKhoaPhong.Visible = false;
                btnNavNhatKy.Visible   = false;
                btnNavThuNgan.Visible = true;
                btnNavThuNgan.Text = "  💳  Thanh toán";
                btnNavThuNgan.Location = new Point(8, 32);
                ApplyResponsiveLayout();
                ShowThuNganView();
            }
            else // Admin
            {
                btnNavTongQuan.Visible = true;
                btnNavLichHen.Visible = true;
                btnNavBenhNhan.Visible = true;
                btnNavKhamBenh.Visible = true;
                btnNavKhamBenh.Text = "  🩺  Phiếu khám";
                btnNavKhoThuoc.Visible = true;
                btnNavDichVu.Visible   = true;
                btnNavThuNgan.Visible = true;
                btnNavNhanVien.Visible = true;
                btnNavBaoCao.Visible = true;
                btnNavKhoaPhong.Visible = true;
                btnNavNhatKy.Visible   = true;
                ApplyResponsiveLayout();
                ShowDashboard();
            }
        }

        // ════════════════════════════════════════════════════════
        //  HELPER: Role display name
        // ════════════════════════════════════════════════════════
        internal string GetRoleDisplayName(string quyen) => quyen switch
        {
            "Admin" => "Quản trị viên",
            "BacSi" => "Bác sĩ khám bệnh",
            "LeTan" => "Y tá",
            "ThuNgan" => "Thu ngân",
            _ => quyen
        };

        // ════════════════════════════════════════════════════════
        //  HELPER UI (dùng chung cho tất cả partial class view)
        // ════════════════════════════════════════════════════════
        internal static ModernButton PrimaryBtn(string text, int x, int y, int w = 140, int h = 34)
            => new ModernButton { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = C_Teal, HoverColor = C_TealDark, ForeColor = Color.White, Font = F_Bold, Radius = 7 };

        internal static ModernButton SecondaryBtn(string text, int x, int y, int w = 120, int h = 34, Color? color = null)
        {
            var c = color ?? C_Blue;
            return new ModernButton { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = c, HoverColor = ControlPaint.Dark(c, 0.1f), ForeColor = Color.White, Font = F_Base, Radius = 7 };
        }

        internal static TextBox MakeInput(int x, int y, int w = 200, bool readOnly = false, string placeholder = "")
            => new TextBox { Location = new Point(x, y), Size = new Size(w, 30), Font = F_Base, BorderStyle = BorderStyle.FixedSingle, ReadOnly = readOnly, BackColor = readOnly ? Color.FromArgb(248, 250, 252) : Color.White, Text = placeholder };

        internal static Label MakeLbl(string text, int x, int y, bool bold = false)
            => new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = bold ? F_Bold : F_Base, ForeColor = bold ? C_TextDark : C_TextGray };

        internal static DataGridView MakeGrid(DockStyle dock = DockStyle.Fill)
        {
            var g = new DataGridView
            {
                Dock = dock,
                AutoSizeColumnsMode = SessionState.CurrentUser?.Quyen is "Admin" or "BacSi"
                    ? DataGridViewAutoSizeColumnsMode.Fill
                    : DataGridViewAutoSizeColumnsMode.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                ReadOnly = true, BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
                RowHeadersVisible = false, AllowUserToAddRows = false, Font = F_Base,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersHeight = 36
            };
            g.ColumnHeadersDefaultCellStyle.BackColor         = Color.FromArgb(248, 250, 252);
            g.ColumnHeadersDefaultCellStyle.ForeColor         = C_TextGray;
            g.ColumnHeadersDefaultCellStyle.Font              = F_Bold;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);
            g.DefaultCellStyle.SelectionBackColor             = Color.FromArgb(224, 247, 250);
            g.DefaultCellStyle.SelectionForeColor             = C_TextDark;
            g.AlternatingRowsDefaultCellStyle.BackColor       = Color.FromArgb(250, 252, 255);
            g.RowTemplate.Height  = 38;
            g.EnableHeadersVisualStyles = false;
            return g;
        }

        internal static void SafeFormatCol(DataGridView g, string colName, string headerText, int? minWidth = null, bool visible = true, float fillWeight = 100f)
        {
            try
            {
                if (g.Columns.Contains(colName) && g.Columns[colName] != null)
                {
                    var c = g.Columns[colName]!;
                    c.HeaderText = headerText;
                    c.Visible = visible;
                    c.FillWeight = fillWeight;
                    if (minWidth.HasValue)
                    {
                        c.MinimumWidth = minWidth.Value;
                    }
                }
            }
            catch { }
        }

        internal static Panel MakeDoctorKpiCard(string title, string value, string subtitle, Color accentColor)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Margin = new Padding(4, 0, 4, 0) };
            var strip = new Panel { Dock = DockStyle.Top, Height = 4, BackColor = accentColor };
            var lblTitle = new Label { Text = title.ToUpper(), Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = C_TextGray, Location = new Point(14, 12), AutoSize = true };
            var lblVal = new Label { Text = value, Font = new Font("Segoe UI", 18f, FontStyle.Bold), ForeColor = accentColor, Location = new Point(12, 30), AutoSize = true };
            var lblSub = new Label { Text = subtitle, Font = new Font("Segoe UI", 8f), ForeColor = C_TextGray, Location = new Point(14, 68), AutoSize = true };
            card.Controls.AddRange(new Control[] { strip, lblTitle, lblVal, lblSub });
            card.Paint += (s, e) => { using var pen = new Pen(C_Border, 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            return card;
        }

        internal static Panel MakeKpiCard(string title, string value, string subtitle, Color accentColor, int x, int y, int w = 210)
        {
            var card = new Panel { Location = new Point(x, y), Size = new Size(w, 100), BackColor = C_Card };
            card.Controls.Add(new Panel { Location = new Point(0, 0), Size = new Size(w, 4), BackColor = accentColor });
            card.Controls.Add(new Label { Text = title.ToUpper(),  Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = C_TextGray,   Location = new Point(14, 14), Size = new Size(w - 28, 16) });
            card.Controls.Add(new Label { Text = value,            Font = new Font("Segoe UI", 19f, FontStyle.Bold),  ForeColor = accentColor,   Location = new Point(14, 34), AutoSize = true });
            card.Controls.Add(new Label { Text = subtitle,         Font = new Font("Segoe UI", 8f),                   ForeColor = C_TextGray,   Location = new Point(14, 72), AutoSize = true });
            card.Paint += (s, e) => { using var pen = new Pen(C_Border, 1); e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1); };
            return card;
        }

        internal static void PaintCardBorder(object? sender, PaintEventArgs e)
        {
            var p = (Panel)sender!;
            using var pen = new Pen(C_Border, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
        }
    }
}
