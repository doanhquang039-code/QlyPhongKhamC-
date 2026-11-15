using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    internal sealed class FormCaLamViecBacSi : Form
    {
        // ── Palette ──────────────────────────────────────────────
        private static readonly Color C_Teal       = Color.FromArgb(0, 145, 134);
        private static readonly Color C_TealLight  = Color.FromArgb(204, 251, 241);
        private static readonly Color C_Navy       = Color.FromArgb(17, 42, 63);
        private static readonly Color C_Bg         = Color.FromArgb(243, 246, 248);
        private static readonly Color C_Card       = Color.White;
        private static readonly Color C_Border     = Color.FromArgb(229, 231, 235);
        private static readonly Color C_Muted      = Color.FromArgb(100, 116, 139);
        private static readonly Color C_Red        = Color.FromArgb(220, 38, 38);
        private static readonly Color C_Blue       = Color.FromArgb(59, 130, 246);
        private static readonly Color C_Orange     = Color.FromArgb(217, 119, 6);

        // ── State ─────────────────────────────────────────────────
        private readonly ComboBox _cboKhoa;
        private readonly ComboBox _cboPhong;
        private readonly ComboBox _cboFilterDoc;
        private readonly ComboBox _cboAddDoc;
        private readonly ComboBox _cboDay;
        private readonly DateTimePicker _dtpStart;
        private readonly DateTimePicker _dtpEnd;
        private readonly DataGridView _grid;
        private readonly TabControl _tabs;
        private Panel _weekCanvas = null!;
        private DataTable _currentShifts = new DataTable();
        private bool _suppressFilter;

        public FormCaLamViecBacSi()
        {
            Text            = "Quản lý lịch khám – Ca làm việc bác sĩ";
            Size            = new Size(1140, 720);
            MinimumSize     = new Size(960, 600);
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = C_Bg;

            // ════════════════════════════════════════════════════
            //  HEADER
            // ════════════════════════════════════════════════════
            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = C_Card };
            pnlHeader.Paint += PaintBottom;
            pnlHeader.Controls.Add(new Label
            {
                Text     = "QUẢN LÝ LỊCH KHÁM – CA LÀM VIỆC BÁC SĨ",
                Font     = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = C_Navy,
                Location = new Point(18, 14),
                AutoSize = true
            });
            pnlHeader.Controls.Add(new Label
            {
                Text     = "Lọc theo khoa / phòng, thêm/xóa ca, xem lịch tuần",
                Font     = new Font("Segoe UI", 9F),
                ForeColor = C_Muted,
                Location = new Point(430, 19),
                AutoSize = true
            });

            // ════════════════════════════════════════════════════
            //  FILTER ROW
            // ════════════════════════════════════════════════════
            var pnlFilter = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C_Card, Padding = new Padding(12, 0, 12, 0) };
            pnlFilter.Paint += PaintBottom;
            var filterFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 9, 0, 0) };

            _cboKhoa       = MakeCbo(190);
            _cboPhong      = MakeCbo(210);
            _cboFilterDoc  = MakeCbo(210);

            var btnRefresh = MakeBtn("↻ Làm mới", C_Teal, 100);
            btnRefresh.Margin = new Padding(6, 0, 0, 0);
            btnRefresh.Click += (s, e) => LoadShifts();

            filterFlow.Controls.AddRange(new Control[]
            {
                MakeCaption("Khoa:"), _cboKhoa,
                MakeCaption("  Phòng:"), _cboPhong,
                MakeCaption("  Bác sĩ:"), _cboFilterDoc,
                btnRefresh
            });
            pnlFilter.Controls.Add(filterFlow);

            // ════════════════════════════════════════════════════
            //  TOOLBAR – THÊM CA
            // ════════════════════════════════════════════════════
            var pnlTools = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(12, 0, 12, 0) };
            pnlTools.Paint += PaintBottom;
            var toolsFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Padding = new Padding(0, 9, 0, 0) };

            _cboAddDoc = MakeCbo(205);
            _cboDay    = MakeCbo(100);
            _cboDay.Items.AddRange(new object[] { "Thứ 2","Thứ 3","Thứ 4","Thứ 5","Thứ 6","Thứ 7","Chủ nhật" });
            _cboDay.SelectedIndex = 0;
            _dtpStart = TimePicker(8);
            _dtpEnd   = TimePicker(17);

            var btnAdd    = MakeBtn("＋ Thêm ca", C_Teal, 100);
            var btnDelete = MakeBtn("🗑 Xóa ca",  C_Red,   95);
            var btnCopy   = MakeBtn("📋 Sao chép", C_Orange, 100);

            toolsFlow.Controls.AddRange(new Control[]
            {
                MakeCaption("Bác sĩ:"),  _cboAddDoc,
                MakeCaption("  Thứ:"),   _cboDay,
                MakeCaption("  Từ:"),    _dtpStart,
                MakeCaption("  Đến:"),   _dtpEnd,
                btnAdd, btnDelete, btnCopy
            });
            pnlTools.Controls.Add(toolsFlow);

            // ════════════════════════════════════════════════════
            //  TAB CONTROL
            // ════════════════════════════════════════════════════
            _tabs = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F), Padding = new Point(10, 5) };
            var tabList = new TabPage("📋  Danh sách ca");
            var tabWeek = new TabPage("📅  Lịch tuần theo khoa");

            // Grid
            _grid = BuildGrid();
            tabList.Controls.Add(_grid);

            // Week panel (sẽ được vẽ lại mỗi lần chuyển tab)
            var pnlWeekOuter = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = C_Card };
            var pnlWeekInfo  = new Label
            {
                Text      = "Chọn tab này để xem lịch tuần dạng bảng (thứ 2 → chủ nhật).",
                Font      = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = C_Muted,
                Location  = new Point(16, 16),
                AutoSize  = true
            };
            pnlWeekOuter.Controls.Add(pnlWeekInfo);
            _weekCanvas = pnlWeekOuter;
            tabWeek.Controls.Add(_weekCanvas);

            _tabs.TabPages.AddRange(new[] { tabList, tabWeek });
            _tabs.SelectedIndexChanged += (s, e) =>
            {
                if (_tabs.SelectedTab == tabWeek)
                    DrawWeekView();
            };

            // Assemble
            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 8), BackColor = C_Bg };
            pnlBody.Controls.Add(_tabs);
            Controls.Add(pnlBody);
            Controls.Add(pnlTools);
            Controls.Add(pnlFilter);
            Controls.Add(pnlHeader);

            // ── Init data ──
            LoadFilterData();

            // ── Events ────────────────────────────────────────
            _cboKhoa.SelectedIndexChanged      += (s, e) => { if (!_suppressFilter) { RefreshPhongFilter(); RefreshDocFilter(); LoadShifts(); } };
            _cboPhong.SelectedIndexChanged     += (s, e) => { if (!_suppressFilter) { RefreshDocFilter(); LoadShifts(); } };
            _cboFilterDoc.SelectedIndexChanged += (s, e) => { if (!_suppressFilter) LoadShifts(); };

            btnAdd.Click    += BtnAdd_Click;
            btnDelete.Click += BtnDelete_Click;
            btnCopy.Click   += BtnCopy_Click;

            LoadShifts();
        }

        // ════════════════════════════════════════════════════════
        //  FILTER DATA
        // ════════════════════════════════════════════════════════
        private void LoadFilterData()
        {
            _suppressFilter = true;
            try
            {
                // Khoa
                var khoaDt = ClinicDAL.LayDanhSachKhoa();
                var khoaFull = new DataTable();
                khoaFull.Columns.Add("MaKhoa", typeof(string));
                khoaFull.Columns.Add("TenKhoa", typeof(string));
                khoaFull.Rows.Add("", "— Tất cả khoa —");
                foreach (DataRow r in khoaDt.Rows)
                    khoaFull.Rows.Add(r["MaKhoa"].ToString(), r["TenKhoa"].ToString());
                _cboKhoa.DataSource    = khoaFull;
                _cboKhoa.DisplayMember = "TenKhoa";
                _cboKhoa.ValueMember   = "MaKhoa";
                _cboKhoa.SelectedIndex = 0;

                RefreshPhongFilter();
                RefreshDocFilter();

                // Bác sĩ cho ô thêm ca (không có "Tất cả")
                RefreshAddDoctorCombo();
            }
            finally { _suppressFilter = false; }
        }

        private void RefreshPhongFilter()
        {
            _suppressFilter = true;
            try
            {
                string maKhoa = _cboKhoa.SelectedValue?.ToString() ?? "";
                var dt = ClinicDAL.LayDanhSachPhongTheoKhoa(string.IsNullOrEmpty(maKhoa) ? null : maKhoa);
                var full = new DataTable();
                full.Columns.Add("MaPhong", typeof(string));
                full.Columns.Add("TenPhong", typeof(string));
                full.Rows.Add("", "— Tất cả phòng —");
                foreach (DataRow r in dt.Rows)
                    full.Rows.Add(r["MaPhong"].ToString(), r["TenPhong"].ToString());
                _cboPhong.DataSource    = full;
                _cboPhong.DisplayMember = "TenPhong";
                _cboPhong.ValueMember   = "MaPhong";
                _cboPhong.SelectedIndex = 0;
            }
            finally { _suppressFilter = false; }
        }

        private void RefreshDocFilter()
        {
            _suppressFilter = true;
            try
            {
                string maKhoa = _cboKhoa.SelectedValue?.ToString()  ?? "";
                string maPhong = _cboPhong.SelectedValue?.ToString() ?? "";
                var dt = ClinicDAL.LayBacSiTheoKhoaPhong(
                    string.IsNullOrEmpty(maKhoa)  ? null : maKhoa,
                    string.IsNullOrEmpty(maPhong) ? null : maPhong);
                var full = new DataTable();
                full.Columns.Add("MaNV",  typeof(string));
                full.Columns.Add("HoTen", typeof(string));
                full.Rows.Add("", "— Tất cả bác sĩ —");
                foreach (DataRow r in dt.Rows)
                    full.Rows.Add(r["MaNV"].ToString(), r["HoTen"].ToString());
                _cboFilterDoc.DataSource    = full;
                _cboFilterDoc.DisplayMember = "HoTen";
                _cboFilterDoc.ValueMember   = "MaNV";
                _cboFilterDoc.SelectedIndex = 0;

                RefreshAddDoctorCombo(dt);
            }
            finally { _suppressFilter = false; }
        }

        private void RefreshAddDoctorCombo(DataTable? dt = null)
        {
            dt ??= ClinicDAL.LayBacSiTheoKhoaPhong();
            if (dt.Rows.Count == 0)
                dt = ClinicDAL.LayDanhSachBacSi();
            _cboAddDoc.DataSource    = dt;
            _cboAddDoc.DisplayMember = "HoTen";
            _cboAddDoc.ValueMember   = "MaNV";
        }

        // ════════════════════════════════════════════════════════
        //  LOAD / DISPLAY SHIFTS
        // ════════════════════════════════════════════════════════
        private void LoadShifts()
        {
            string maKhoa  = _cboKhoa.SelectedValue?.ToString()     ?? "";
            string maPhong = _cboPhong.SelectedValue?.ToString()     ?? "";
            string maNV    = _cboFilterDoc.SelectedValue?.ToString() ?? "";

            DataTable dt = ClinicDAL.LayCaLamViecBacSiTheoKhoaPhong(
                string.IsNullOrEmpty(maKhoa)  ? null : maKhoa,
                string.IsNullOrEmpty(maPhong) ? null : maPhong);

            // Filter by specific doctor if selected
            if (!string.IsNullOrEmpty(maNV))
            {
                var rows = dt.AsEnumerable().Where(r => r["MaNV"]?.ToString() == maNV);
                dt = rows.Any() ? rows.CopyToDataTable() : dt.Clone();
            }

            _currentShifts   = dt;
            _grid.DataSource  = _currentShifts;

            // Column headers
            void ColHeader(string col, string header, bool visible = true)
            {
                if (!_grid.Columns.Contains(col)) return;
                _grid.Columns[col]!.HeaderText = header;
                _grid.Columns[col]!.Visible    = visible;
            }
            ColHeader("MaCa",        "Mã ca");
            ColHeader("MaNV",        "Mã NV",   false);
            ColHeader("HoTen",       "Bác sĩ");
            ColHeader("TenKhoa",     "Khoa");
            ColHeader("TenPhong",    "Phòng khám");
            ColHeader("NgayTrongTuan","Thứ");
            ColHeader("Thu",         "Thu",     false);
            if (_grid.Columns.Contains("GioBatDau"))
            {
                _grid.Columns["GioBatDau"]!.HeaderText              = "Giờ bắt đầu";
                _grid.Columns["GioBatDau"]!.DefaultCellStyle.Format = @"hh\:mm";
            }
            if (_grid.Columns.Contains("GioKetThuc"))
            {
                _grid.Columns["GioKetThuc"]!.HeaderText              = "Giờ kết thúc";
                _grid.Columns["GioKetThuc"]!.DefaultCellStyle.Format = @"hh\:mm";
            }

            if (_tabs.SelectedTab?.Text.Contains("Lịch tuần") == true)
                DrawWeekView();
        }

        // ════════════════════════════════════════════════════════
        //  WEEK VIEW (custom-painted calendar)
        // ════════════════════════════════════════════════════════
        private void DrawWeekView()
        {
            // Remove old canvas (keep scroll panel)
            var outer = _weekCanvas;
            foreach (Control c in outer.Controls.OfType<Panel>().ToList())
                c.Dispose();
            outer.Controls.Clear();

            if (_currentShifts == null || _currentShifts.Rows.Count == 0)
            {
                outer.Controls.Add(new Label
                {
                    Text     = "Không có ca làm việc nào trong bộ lọc hiện tại.",
                    Location = new Point(20, 20),
                    AutoSize = true,
                    Font     = new Font("Segoe UI", 10F, FontStyle.Italic),
                    ForeColor = C_Muted
                });
                return;
            }

            string[] dayNames = { "Thứ 2","Thứ 3","Thứ 4","Thứ 5","Thứ 6","Thứ 7","Chủ nhật" };

            var doctors = _currentShifts.AsEnumerable()
                .GroupBy(r => r["MaNV"]?.ToString() ?? "")
                .Select(g => new
                {
                    MaNV  = g.Key,
                    HoTen = g.First()["HoTen"]?.ToString() ?? "",
                    Khoa  = g.First()["TenKhoa"]?.ToString() ?? "",
                    Phong = g.First()["TenPhong"]?.ToString() ?? ""
                })
                .OrderBy(d => d.Khoa).ThenBy(d => d.HoTen)
                .ToList();

            // Current day highlight
            int todayDow = DateTime.Today.DayOfWeek == DayOfWeek.Sunday
                ? 7 : (int)DateTime.Today.DayOfWeek;
            int todayIdx = todayDow - 1; // 0=Mon..5=Sat,6=Sun

            const int ColW    = 115;
            const int RowH    = 64;
            const int LeftW   = 190;
            const int HeaderH = 38;
            int totalW = LeftW + ColW * 7 + 2;
            int totalH = HeaderH + RowH * doctors.Count + 2;

            var canvas = new Panel
            {
                Location  = new Point(8, 8),
                Size      = new Size(totalW, totalH),
                BackColor = C_Card
            };

            canvas.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using var fBold    = new Font("Segoe UI", 9F,   FontStyle.Bold);
                using var fNormal  = new Font("Segoe UI", 8.5F, FontStyle.Regular);
                using var fSmall   = new Font("Segoe UI", 7.5F, FontStyle.Regular);
                using var fShift   = new Font("Segoe UI", 8F,   FontStyle.Bold);
                using var penGrid  = new Pen(C_Border, 1);
                using var brHeader = new SolidBrush(Color.FromArgb(241, 245, 249));
                using var brToday  = new SolidBrush(Color.FromArgb(224, 253, 247));
                using var brText   = new SolidBrush(C_Navy);
                using var brMuted  = new SolidBrush(C_Muted);
                using var brShift  = new SolidBrush(C_TealLight);
                using var brShiftT = new SolidBrush(Color.FromArgb(0, 100, 94));
                using var penShift = new Pen(C_Teal, 1.5f);
                using var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                using var sfLeft   = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };

                // ── COLUMN HEADERS ──
                g.FillRectangle(brHeader, 0, 0, totalW, HeaderH);
                g.DrawString("Bác sĩ / Phòng", fBold, brMuted,
                    new RectangleF(8, 0, LeftW - 12, HeaderH), sfLeft);

                for (int d = 0; d < 7; d++)
                {
                    int x = LeftW + d * ColW;
                    if (d == todayIdx)
                    {
                        using var brTodayCol = new SolidBrush(Color.FromArgb(204, 251, 241));
                        g.FillRectangle(brTodayCol, x, 0, ColW, HeaderH);
                    }
                    g.DrawString(dayNames[d], fBold, d == todayIdx ? brShiftT : brMuted,
                        new RectangleF(x, 0, ColW, HeaderH), sfCenter);
                    g.DrawLine(penGrid, x, 0, x, totalH);
                }
                g.DrawLine(penGrid, LeftW, 0, LeftW, totalH);

                // ── ROWS ──
                for (int i = 0; i < doctors.Count; i++)
                {
                    int y   = HeaderH + i * RowH;
                    var doc = doctors[i];

                    // Alternating bg
                    if (i % 2 == 1)
                    {
                        using var brAlt = new SolidBrush(Color.FromArgb(250, 252, 252));
                        g.FillRectangle(brAlt, 0, y, totalW, RowH);
                    }

                    // Today column tint
                    if (todayIdx >= 0 && todayIdx < 7)
                    {
                        int tx = LeftW + todayIdx * ColW;
                        using var brTodayTint = new SolidBrush(Color.FromArgb(20, 0, 145, 134));
                        g.FillRectangle(brTodayTint, tx, y, ColW, RowH);
                    }

                    g.DrawLine(penGrid, 0, y + RowH, totalW, y + RowH);

                    // Doctor name + room
                    g.DrawString(doc.HoTen, fBold,   brText,  new RectangleF(8, y + 6,  LeftW - 14, 22), sfLeft);
                    g.DrawString(doc.Phong, fSmall, brMuted, new RectangleF(8, y + 30, LeftW - 14, 18), sfLeft);

                    // Shifts
                    var docShifts = _currentShifts.AsEnumerable()
                        .Where(r => r["MaNV"]?.ToString() == doc.MaNV)
                        .ToList();

                    foreach (var shift in docShifts)
                    {
                        int thu    = Convert.ToInt32(shift["Thu"]);
                        int dayIdx = thu == 7 ? 6 : thu - 1;
                        int cx     = LeftW + dayIdx * ColW + 4;
                        int cy     = y + 4;
                        int cw     = ColW - 8;
                        int ch     = RowH - 8;

                        // Fill shift cell
                        using var roundPath = RoundRect(cx, cy, cw, ch, 4);
                        g.FillPath(brShift, roundPath);
                        g.DrawPath(penShift, roundPath);

                        string s1 = shift["GioBatDau"]  is TimeSpan t1 ? t1.ToString(@"hh\:mm") : shift["GioBatDau"]?.ToString()  ?? "";
                        string s2 = shift["GioKetThuc"] is TimeSpan t2 ? t2.ToString(@"hh\:mm") : shift["GioKetThuc"]?.ToString() ?? "";
                        g.DrawString($"{s1}–{s2}", fShift, brShiftT,
                            new RectangleF(cx + 2, cy + 2, cw - 4, ch - 4), sfCenter);
                    }
                }

                // Outer border
                g.DrawRectangle(penGrid, 0, 0, totalW - 1, totalH - 1);
            };

            outer.Controls.Add(canvas);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundRect(int x, int y, int w, int h, int r)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ════════════════════════════════════════════════════════
        //  BUTTON HANDLERS
        // ════════════════════════════════════════════════════════
        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            if (_cboAddDoc.SelectedValue == null || string.IsNullOrEmpty(_cboAddDoc.SelectedValue.ToString()))
            {
                MessageBox.Show("Vui lòng chọn bác sĩ cần thêm ca.", "Thiếu thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                ClinicDAL.LuuCaLamViecBacSi(
                    _cboAddDoc.SelectedValue.ToString() ?? "",
                    _cboDay.SelectedIndex + 1,
                    _dtpStart.Value.TimeOfDay,
                    _dtpEnd.Value.TimeOfDay);
                LoadShifts();
                MessageBox.Show("Đã thêm ca làm việc.", "Thành công",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể thêm ca làm việc",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_grid.SelectedRows.Count == 0 ||
                _grid.SelectedRows[0].DataBoundItem is not DataRowView sel)
            {
                MessageBox.Show("Chọn ca làm việc cần xóa.", "Chưa chọn",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string docName = sel.Row["HoTen"]?.ToString() ?? "";
            string dayName = sel.Row["NgayTrongTuan"]?.ToString() ?? "";
            if (MessageBox.Show($"Xóa ca {dayName} của {docName}?", "Xác nhận xóa",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            try
            {
                if (!ClinicDAL.XoaCaLamViecBacSi(Convert.ToInt32(sel.Row["MaCa"])))
                    throw new InvalidOperationException("Không xóa được – ca đang có lịch hẹn tương lai được đặt trên ca này.");
                LoadShifts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể xóa ca",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCopy_Click(object? sender, EventArgs e)
        {
            DataTable allDocs;
            try { allDocs = ClinicDAL.LayBacSiTheoKhoaPhong(); }
            catch { allDocs = ClinicDAL.LayDanhSachBacSi(); }

            if (allDocs.Rows.Count < 2)
            {
                MessageBox.Show("Cần ít nhất 2 bác sĩ trong hệ thống để sao chép ca.", "Không đủ dữ liệu",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dlg = new Form
            {
                Text = "Sao chép ca làm việc", Size = new Size(420, 210),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, BackColor = Color.White
            };

            var cboSrc = new ComboBox { Location = new Point(130, 18), Width = 255,
                DropDownStyle = ComboBoxStyle.DropDownList, DataSource = allDocs.Copy(),
                DisplayMember = "HoTen", ValueMember = "MaNV" };
            var cboDst = new ComboBox { Location = new Point(130, 60), Width = 255,
                DropDownStyle = ComboBoxStyle.DropDownList, DataSource = allDocs,
                DisplayMember = "HoTen", ValueMember = "MaNV" };

            dlg.Controls.Add(new Label { Text = "Nguồn (copy từ):", Location = new Point(14, 22), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            dlg.Controls.Add(new Label { Text = "Đích (copy sang):", Location = new Point(14, 64), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) });
            dlg.Controls.Add(new Label { Text = "⚠ Ca trùng lịch sẽ bị bỏ qua tự động.", Location = new Point(14, 106), AutoSize = true, Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = C_Muted });
            dlg.Controls.Add(cboSrc);
            dlg.Controls.Add(cboDst);

            var btnOk = new Button
            {
                Text = "Sao chép", Location = new Point(130, 135), Size = new Size(110, 34),
                BackColor = C_Teal, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            btnOk.FlatAppearance.BorderSize = 0;
            var btnCancel = new Button
            {
                Text = "Hủy", Location = new Point(252, 135), Size = new Size(80, 34),
                FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel
            };
            dlg.Controls.AddRange(new Control[] { btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            string srcMaNV = cboSrc.SelectedValue?.ToString() ?? "";
            string dstMaNV = cboDst.SelectedValue?.ToString() ?? "";
            if (srcMaNV == dstMaNV)
            {
                MessageBox.Show("Nguồn và đích không được là cùng một bác sĩ.", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                int count = ClinicDAL.SaoCepCaLamViec(srcMaNV, dstMaNV);
                MessageBox.Show(count > 0
                    ? $"Đã sao chép {count} ca làm việc thành công."
                    : "Không có ca nào được sao chép (tất cả đã bị trùng lịch hoặc nguồn không có ca).",
                    "Kết quả", MessageBoxButtons.OK,
                    count > 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                LoadShifts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi sao chép", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════
        private static DataGridView BuildGrid()
        {
            var dg = new DataGridView
            {
                Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false, BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None, GridColor = Color.FromArgb(229, 231, 235),
                Font = new Font("Segoe UI", 9.5F), ColumnHeadersHeight = 36,
                EnableHeadersVisualStyles = false
            };
            dg.ColumnHeadersDefaultCellStyle.BackColor  = Color.FromArgb(247, 249, 250);
            dg.ColumnHeadersDefaultCellStyle.Font       = new Font("Segoe UI", 9F, FontStyle.Bold);
            dg.ColumnHeadersDefaultCellStyle.ForeColor  = Color.FromArgb(100, 116, 139);
            dg.DefaultCellStyle.SelectionBackColor      = Color.FromArgb(204, 251, 241);
            dg.DefaultCellStyle.SelectionForeColor      = Color.FromArgb(17, 42, 63);
            dg.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 252);
            dg.RowTemplate.Height = 36;
            return dg;
        }

        private static DateTimePicker TimePicker(int hour) => new()
        {
            Width = 72, Format = DateTimePickerFormat.Custom,
            CustomFormat = "HH:mm", ShowUpDown = true,
            Value = DateTime.Today.AddHours(hour),
            Margin = new Padding(0, 0, 0, 0)
        };

        private static Label MakeCaption(string text) => new()
        {
            Text = text, AutoSize = true,
            Padding = new Padding(2, 9, 2, 0),
            Font = new Font("Segoe UI", 9F, FontStyle.Bold)
        };

        private static Button MakeBtn(string text, Color back, int width = 100)
        {
            var b = new Button
            {
                Text = text, Width = width, Height = 32,
                BackColor = back, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(4, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private static ComboBox MakeCbo(int width) => new()
        {
            Width = width, DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 9.5F),
            Margin = new Padding(2, 0, 0, 0)
        };

        private static void PaintBottom(object? sender, System.Windows.Forms.PaintEventArgs e)
        {
            if (sender is not Control c) return;
            using var pen = new Pen(Color.FromArgb(229, 231, 235), 1);
            e.Graphics.DrawLine(pen, 0, c.Height - 1, c.Width, c.Height - 1);
        }
    }
}
