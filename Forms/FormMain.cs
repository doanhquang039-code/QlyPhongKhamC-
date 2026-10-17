using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using QlyPhongKham.DAL;

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
        internal Panel  panelSidebar    = null!;
        internal Panel  panelContent    = null!;
        private  Label  lblPageTitle    = null!;
        private  Label  lblBreadcrumb   = null!;
        private  Panel  topBar          = null!;

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
            var lblSection = new Label { Text = "MENU CHÍNH", Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(100, 116, 139), Location = new Point(16, 156), AutoSize = true };

            // Nav buttons
            btnNavTongQuan  = CreateNavBtn("🏠", "Tổng quan",         176);
            btnNavLichHen   = CreateNavBtn("📋", "Lịch hẹn hôm nay",  222);
            btnNavBenhNhan  = CreateNavBtn("👤", "Bệnh nhân",          268);
            btnNavKhamBenh  = CreateNavBtn("🩺", "Phòng khám",         314);
            btnNavKhoThuoc  = CreateNavBtn("💊", "Kho thuốc",          360);
            btnNavThuNgan   = CreateNavBtn("💰", "Thu ngân & HĐ",      406);
            btnNavNhanVien  = CreateNavBtn("👥", "Nhân sự",             452);
            btnNavBaoCao    = CreateNavBtn("📊", "Báo cáo",             498);

            btnNavTongQuan.Click += (s, e) => ShowDashboard();
            btnNavLichHen.Click  += (s, e) =>
            {
                if (SessionState.IsDoctor) ShowBacSiLichKhamView();
                else ShowTiepDonView();
            };
            btnNavBenhNhan.Click += (s, e) =>
            {
                if (SessionState.IsDoctor) ShowBacSiHoSoBenhAnView();
                else ShowBenhNhanView();
            };
            btnNavKhamBenh.Click += (s, e) =>
            {
                if (SessionState.IsDoctor) ShowBacSiKhamKeDonView();
                else ShowKhamBenhView();
            };
            btnNavKhoThuoc.Click += (s, e) =>
            {
                if (SessionState.IsDoctor) ShowBacSiTraCuuView();
                else ShowKhoThuocView();
            };
            btnNavThuNgan.Click  += (s, e) => ShowThuNganView();
            btnNavNhanVien.Click += (s, e) => ShowNhanVienView();
            btnNavBaoCao.Click   += (s, e) => ShowBaoCaoView();

            // Logout & Config at bottom
            var btnLogout = new Button { Text = "  🚪  Đăng xuất", Size = new Size(220, 40), BackColor = Color.FromArgb(39, 22, 22), ForeColor = Color.FromArgb(252, 165, 165), FlatStyle = FlatStyle.Flat, Font = F_Bold, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };
            btnLogout.Location = new Point(10, 710);
            btnLogout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => { SessionState.CurrentUser = null; this.Close(); };

            var btnCfg = new Button { Text = "  ⚙  Cấu hình máy chủ", Size = new Size(220, 36), BackColor = Color.Transparent, ForeColor = Color.FromArgb(100, 116, 139), FlatStyle = FlatStyle.Flat, Font = F_Small, TextAlign = ContentAlignment.MiddleLeft, Cursor = Cursors.Hand };
            btnCfg.Location = new Point(10, 755);
            btnCfg.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnCfg.FlatAppearance.BorderSize = 0;
            btnCfg.Click += (s, e) => new FormConfigDb().ShowDialog();

            panelSidebar.Controls.AddRange(new Control[] { headerSidebar, lblSection, btnNavTongQuan, btnNavLichHen, btnNavBenhNhan, btnNavKhamBenh, btnNavKhoThuoc, btnNavThuNgan, btnNavNhanVien, btnNavBaoCao, btnLogout, btnCfg });
            this.Controls.Add(panelSidebar);
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
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Color.Transparent;
                _activeNavBtn.ForeColor = Color.FromArgb(203, 213, 225);
            }
            _activeNavBtn = btn;
            btn.BackColor = Color.FromArgb(0, 180, 166, 40);
            btn.ForeColor = C_Teal;
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
            var lblDate   = new Label { Text = $"📅  {DateTime.Now:dddd, dd/MM/yyyy}", Font = F_Small, ForeColor = C_TextGray, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            lblDate.Location = new Point(topBar.Width - 220, 22);
            lblDate.Anchor   = AnchorStyles.Top | AnchorStyles.Right;

            topBar.Controls.AddRange(new Control[] { lblBreadcrumb, lblPageTitle, lblDate });
            this.Controls.Add(topBar);
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

            if (panelSidebar.Controls.Count == 0)
                return;

            if (panelSidebar.Controls[0] is Panel header)
            {
                header.Width = sidebarWidth;
                if (header.Controls.Count > 0)
                    header.Controls[0].Size = new Size(sidebarWidth, 72);
                if (header.Controls.Count > 1)
                {
                    var userCard = header.Controls[1];
                    userCard.Size = new Size(sidebarWidth, 72);
                    foreach (Control control in userCard.Controls)
                    {
                        if (control is Label label && label.Location.X >= 62)
                            label.Width = sidebarWidth - label.Left - 12;
                    }
                }
            }

            foreach (Control control in panelSidebar.Controls)
            {
                if (control is not Button button)
                    continue;

                button.Width = button.Text.StartsWith("  🚪", StringComparison.Ordinal) ||
                               button.Text.StartsWith("  ⚙", StringComparison.Ordinal)
                    ? sidebarWidth - 20
                    : sidebarWidth - 16;
            }
        }

        // ════════════════════════════════════════════════════════
        //  PHÂN QUYỀN THEO ROLE
        // ════════════════════════════════════════════════════════
        private void ApplyRolePermissions()
        {
            if (SessionState.IsDoctor)
            {
                ApplyResponsiveLayout();

                if (SessionState.CurrentUser?.MaNV != null)
                {
                    ClinicDAL.DamBaoDuLieuMauBacSi(SessionState.CurrentUser.MaNV);
                }

                btnNavTongQuan.Visible = false;
                btnNavThuNgan.Visible  = false;
                btnNavNhanVien.Visible = false;
                btnNavBaoCao.Visible   = false;

                btnNavLichHen.Visible  = true;
                btnNavLichHen.Text     = "  📅  Lịch khám";
                btnNavLichHen.Location = new Point(8, 164);

                btnNavBenhNhan.Visible = true;
                btnNavBenhNhan.Text    = "  👤  Bệnh nhân";
                btnNavBenhNhan.Location = new Point(8, 210);

                btnNavKhamBenh.Visible = true;
                btnNavKhamBenh.Text    = "  🩺  Khám & Kê đơn";
                btnNavKhamBenh.Location = new Point(8, 256);

                btnNavKhoThuoc.Visible = true;
                btnNavKhoThuoc.Text    = "  🔍  Tra cứu";
                btnNavKhoThuoc.Location = new Point(8, 302);

                ShowBacSiLichKhamView();
            }
            else if (SessionState.IsReceptionist)
            {
                btnNavKhamBenh.Visible = false;
                btnNavNhanVien.Visible = false;
                btnNavBaoCao.Visible   = false;
                ShowTiepDonView();
            }
            else // Admin
            {
                btnNavTongQuan.Visible = true;
                btnNavLichHen.Visible = true;
                btnNavBenhNhan.Visible = true;
                btnNavKhamBenh.Visible = true;
                btnNavKhoThuoc.Visible = true;
                btnNavThuNgan.Visible = true;
                btnNavNhanVien.Visible = true;
                btnNavBaoCao.Visible = true;
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
            "LeTan" => "Lễ tân & Thu ngân",
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
