using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;
using QlyPhongKham.Helpers;

namespace QlyPhongKham.Forms
{
    public partial class FormMain
    {
        // Lưu trữ mã phiếu khám & bệnh nhân đang được chọn khám trong ca
        private string? _currentDoctorMaPK;
        private string? _currentDoctorMaBN;

        private static void ConfigureDoctorRightColumn(Panel layout, Panel rightColumn, int gutter = 14)
        {
            void ResizeColumn(object? sender, EventArgs e)
            {
                int proportionalWidth = (int)Math.Round(layout.ClientSize.Width * 0.25) - gutter;
                int width = Math.Max(240, proportionalWidth);
                if (rightColumn.Width != width)
                    rightColumn.Width = width;
            }

            layout.SizeChanged += ResizeColumn;
            ResizeColumn(layout, EventArgs.Empty);
        }

        // ═══════════════════════════════════════════════════════════
        //  VIEW 1: BÁC SĨ - LỊCH KHÁM CỦA TÔI
        // ═══════════════════════════════════════════════════════════
        public void ShowBacSiLichKhamView()
        {
            SetRefreshPageAction(ShowBacSiLichKhamView);
            SetActiveNav(btnNavLichHen);
            string tenBS = SessionState.CurrentUser?.HoTenNhanVien ?? "Bác sĩ";
            string maBS  = SessionState.CurrentUser?.MaNV ?? "";

            SetPageHeader("Bác sĩ  ›  Bàn làm việc", $"Bàn làm việc Bác sĩ – {tenBS}");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // ── TOP STATS / KPI ──
            var stats = ClinicDAL.LayThongKeBacSi(string.IsNullOrEmpty(maBS) ? null : maBS);
            var (tenPhong, tenKhoa, caTruc) = ClinicDAL.LayThongTinCaTrucBacSi(maBS);
            var roomInfoBS = ClinicDAL.LayThongTinPhongNhanVien(maBS);

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 148, BackColor = Color.Transparent };

            // Welcome label (gọn hơn — chỉ hiển thị tên + ca trực)
            var lblWelcome = new Label
            {
                Text = $"Chào Bác sĩ {tenBS}  •  Ca làm việc: {caTruc}",
                Location = new Point(2, 6),
                AutoSize = true,
                Font = F_Base,
                ForeColor = C_TextGray
            };

            var btnDateToday = new Button
            {
                Text = $"📅  Hôm nay: {DateTime.Now:dd/MM/yyyy}",
                Location = new Point(panelContent.Width - 250, 4),
                Size = new Size(200, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.White,
                ForeColor = C_TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = F_Bold
            };
            btnDateToday.FlatAppearance.BorderColor = C_Border;

            pnlTop.Controls.AddRange(new Control[] { lblWelcome, btnDateToday });

            // 4 KPI Cards
            var kpiCards = new[]
            {
                MakeKpiCard("Tổng ca hôm nay", stats.tongPhanCong.ToString(), "Tổng số ca khám được phân công", C_Teal, 0, 42),
                MakeKpiCard("Đã hoàn tất", stats.daHoanTat.ToString(), "Bệnh nhân đã kết thúc khám", C_Green, 0, 42),
                MakeKpiCard("Đang chờ khám", $"{stats.dangCho}", $"{stats.dangCho} bệnh nhân chờ vào phòng", C_Orange, 0, 42),
                MakeKpiCard("Cận lâm sàng (CLS)", $"{stats.choKQCLS} ca", "Chờ / Đã có kết quả CLS", C_Purple, 0, 42)
            };
            pnlTop.Controls.AddRange(kpiCards);

            void LayoutTopCards()
            {
                int cardW = Math.Max(160, (pnlTop.ClientSize.Width - 42) / 4);
                for (int i = 0; i < kpiCards.Length; i++)
                {
                    var card = kpiCards[i];
                    card.Location = new Point(i * (cardW + 14), 42);
                    card.Size = new Size(cardW, 100);
                    if (card.Controls.Count > 3)
                    {
                        if (card.Controls[1] is Label title)
                        {
                            title.AutoSize = false;
                            title.Width = cardW - 28;
                        }
                        if (card.Controls[3] is Label subtitle)
                        {
                            subtitle.AutoSize = false;
                            subtitle.Size = new Size(cardW - 28, 28);
                        }
                    }
                }
                btnDateToday.Location = new Point(Math.Max(2, pnlTop.ClientSize.Width - btnDateToday.Width - 4), 4);
            }
            pnlTop.SizeChanged += (s, e) => LayoutTopCards();
            LayoutTopCards();

            // ── MAIN BODY LAYOUT (TWO COLUMNS) ──
            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // ══════════════════════════════════════════════
            // CỘT PHẢI: BỆNH NHÂN HIỆN TẠI, CLS & CHUYÊN MÔN
            // ══════════════════════════════════════════════
            var grpRight = new Panel { Dock = DockStyle.Right, Width = 360, BackColor = Color.Transparent, Padding = new Padding(8, 0, 0, 0), AutoScroll = true };

            // Card 1: Bệnh nhân đang khám
            var pnlCur = new Panel { Location = new Point(0, 0), Size = new Size(350, 245), BackColor = C_Card, Padding = new Padding(16) };
            pnlCur.Paint += PaintCardBorder;

            var lblCurTitle = new Label { Text = "BỆNH NHÂN ĐANG KHÁM", Location = new Point(14, 14), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = C_TextGray };
            var badgeCur = new Label
            {
                Text = "  Đang khám  ",
                Location = new Point(pnlCur.Width - 110, 12),
                Size = new Size(95, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(254, 243, 199),
                ForeColor = Color.FromArgb(180, 83, 9),
                Font = F_Small,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Lấy bệnh nhân đang khám hoặc đầu tiên
            var dtAll = ClinicDAL.LayDanhSachKhamBacSi(string.IsNullOrEmpty(maBS) ? null : maBS);
            DataRow? curRow = null;
            foreach (DataRow r in dtAll.Rows)
            {
                if (r["TrangThai"]?.ToString() == "Đang khám") { curRow = r; break; }
            }
            if (curRow == null && dtAll.Rows.Count > 0) curRow = dtAll.Rows[0];

            string curName   = curRow?["TenBN"]?.ToString() ?? "Chưa có ca khám";
            string curMaBN   = curRow?["MaBN"]?.ToString() ?? "---";
            string curGT     = curRow?["GioiTinh"]?.ToString() ?? "";
            string curTuoi   = curRow?["Tuoi"]?.ToString() ?? "";
            string curLyDo   = curRow?["LyDoKham"]?.ToString() ?? "Không có";
            string curMaPK   = curRow?["MaPK"]?.ToString() ?? "";

            var lblCurName = new Label { Text = curName, Location = new Point(14, 42), AutoSize = true, Font = new Font("Segoe UI", 13.5f, FontStyle.Bold), ForeColor = C_TextDark };
            var lblCurMeta = new Label { Text = $"{curMaBN}  •  {curGT}  •  {curTuoi} tuổi", Location = new Point(14, 72), AutoSize = true, Font = F_Small, ForeColor = C_TextGray };

            var lblCurReasonHead = new Label { Text = "Lý do khám:", Location = new Point(14, 98), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };
            var lblCurReason = new Label { Text = curLyDo, Location = new Point(14, 118), Size = new Size(320, 32), Font = F_Base, ForeColor = C_TextDark };

            var lblCurRoom = new Label { Text = $"📍 {tenPhong} ({tenKhoa})  •  Ca: {caTruc}", Location = new Point(14, 154), AutoSize = true, Font = F_Small, ForeColor = C_TealDark };

            var btnTiepTuc = PrimaryBtn("Tiếp tục phiên khám  ➔", 14, 186, 320, 38);
            btnTiepTuc.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(curMaPK)) ShowBacSiKhamKeDonView(curMaPK, 0);
                else MessageBox.Show("Hiện không có bệnh nhân nào đang trong ca khám!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            pnlCur.Controls.AddRange(new Control[] { lblCurTitle, badgeCur, lblCurName, lblCurMeta, lblCurReasonHead, lblCurReason, lblCurRoom, btnTiepTuc });

            // Card 2: Theo dõi Cận lâm sàng (CLS)
            var pnlCLS = new Panel { Location = new Point(0, 257), Size = new Size(350, 235), BackColor = C_Card, Padding = new Padding(14) };
            pnlCLS.Paint += PaintCardBorder;

            var lblCLSTitle = new Label { Text = "🔬 THEO DÕI CẬN LÂM SÀNG (CLS)", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = C_TextGray };
            var dtCLS = ClinicDAL.LayDanhSachTheoDoiCLSBacSi(maBS);

            var pnlCLSList = new Panel { Location = new Point(12, 38), Size = new Size(326, 142), AutoScroll = true, BackColor = Color.Transparent };
            if (dtCLS.Rows.Count == 0)
            {
                var lblEmptyCLS = new Label
                {
                    Text = "Chưa có chỉ định CLS nào đang chờ trong ca hôm nay.",
                    Location = new Point(4, 30),
                    Size = new Size(310, 40),
                    Font = F_Small,
                    ForeColor = C_TextGray
                };
                pnlCLSList.Controls.Add(lblEmptyCLS);
            }
            else
            {
                int itemTop = 0;
                foreach (DataRow row in dtCLS.Rows)
                {
                    string pkId = row["MaPK"]?.ToString() ?? "";
                    string tenBNItem = row["TenBN"]?.ToString() ?? "";
                    string stt = row["STT"]?.ToString() ?? "0";
                    int tongCLS = Convert.ToInt32(row["TongCLS"]);
                    int daCoKQ = Convert.ToInt32(row["DaCoKQ"]);
                    bool duKQ = (row["TrangThaiKQ"]?.ToString() == "Sẵn sàng kết luận");

                    var itemPanel = new Panel
                    {
                        Location = new Point(0, itemTop),
                        Size = new Size(305, 44),
                        BackColor = duKQ ? Color.FromArgb(240, 253, 244) : Color.FromArgb(254, 249, 195),
                        Padding = new Padding(6),
                        Cursor = Cursors.Hand
                    };
                    itemPanel.Paint += (s, e) =>
                    {
                        using var p = new Pen(duKQ ? C_Green : C_Orange, 1);
                        e.Graphics.DrawRectangle(p, 0, 0, itemPanel.Width - 1, itemPanel.Height - 1);
                    };

                    var lblItemName = new Label
                    {
                        Text = $"#{stt} {tenBNItem}",
                        Location = new Point(8, 4),
                        AutoSize = true,
                        Font = F_Bold,
                        ForeColor = C_TextDark
                    };
                    var lblItemStatus = new Label
                    {
                        Text = duKQ ? $"✓ Đã có {daCoKQ}/{tongCLS} kết quả  •  Bấm để kê đơn" : $"⏳ Đang chờ kết quả ({daCoKQ}/{tongCLS} đã có)",
                        Location = new Point(8, 22),
                        AutoSize = true,
                        Font = F_Small,
                        ForeColor = duKQ ? Color.FromArgb(22, 101, 52) : Color.FromArgb(133, 77, 14)
                    };

                    itemPanel.Controls.AddRange(new Control[] { lblItemName, lblItemStatus });
                    itemPanel.Click += (s, e) => ShowBacSiKhamKeDonView(pkId, 1);
                    lblItemName.Click += (s, e) => ShowBacSiKhamKeDonView(pkId, 1);
                    lblItemStatus.Click += (s, e) => ShowBacSiKhamKeDonView(pkId, 1);

                    pnlCLSList.Controls.Add(itemPanel);
                    itemTop += 48;
                }
            }

            var btnFilterCLS = SecondaryBtn("🔍 Lọc ca chờ CLS trên bảng", 12, 190, 326, 32, C_Teal);
            pnlCLS.Controls.AddRange(new Control[] { lblCLSTitle, pnlCLSList, btnFilterCLS });

            // Card 3: Ca trực & Chuyên môn
            var pnlChuyenMon = new Panel { Location = new Point(0, 504), Size = new Size(350, 185), BackColor = C_Card, Padding = new Padding(14) };
            pnlChuyenMon.Paint += PaintCardBorder;

            var lblCMTitle = new Label { Text = "📊 CA TRỰC & CHUYÊN MÔN", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = C_TextGray };
            var lblShiftInfo = new Label { Text = $"🏢 {tenPhong} ({tenKhoa})\n⏰ Ca trực: {caTruc}", Location = new Point(14, 36), Size = new Size(320, 36), Font = F_Small, ForeColor = C_TealDark };

            var lblTopHead = new Label { Text = "Bệnh lý thường gặp nhất (30 ngày qua):", Location = new Point(14, 76), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };
            var dtTopCD = ClinicDAL.LayTopChanDoanBacSi(maBS, 3);
            string topText = "";
            if (dtTopCD.Rows.Count == 0)
            {
                topText = "• Chưa có dữ liệu bệnh án trong 30 ngày.";
            }
            else
            {
                for (int i = 0; i < dtTopCD.Rows.Count; i++)
                {
                    var r = dtTopCD.Rows[i];
                    int sl = r.Table.Columns.Contains("SoLuot") && r["SoLuot"] != DBNull.Value
                        ? Convert.ToInt32(r["SoLuot"])
                        : (r.Table.Columns.Contains("SoLuong") && r["SoLuong"] != DBNull.Value ? Convert.ToInt32(r["SoLuong"]) : 0);
                    topText += $"• {r["ChanDoan"]}: {sl} ca\n";
                }
            }
            var lblTopList = new Label { Text = topText.TrimEnd(), Location = new Point(14, 98), Size = new Size(320, 44), Font = F_Small, ForeColor = C_TextDark };

            var btnViewAllRecord = SecondaryBtn("📂 Tra cứu phác đồ & Dược thư", 14, 144, 320, 28, C_Blue);
            btnViewAllRecord.Click += (s, e) => ShowBacSiTraCuuView();

            pnlChuyenMon.Controls.AddRange(new Control[] { lblCMTitle, lblShiftInfo, lblTopHead, lblTopList, btnViewAllRecord });

            // Spacer at bottom of right column
            var pnlRightBottomPad = new Panel { Location = new Point(0, 700), Size = new Size(350, 20) };

            grpRight.Controls.AddRange(new Control[] { pnlCur, pnlCLS, pnlChuyenMon, pnlRightBottomPad });

            void LayoutRightCards()
            {
                int cardWidth = Math.Max(260, grpRight.ClientSize.Width - 20);
                int innerWidth = Math.Max(220, cardWidth - 28);

                pnlCur.Width = cardWidth;
                lblCurReason.Width = innerWidth;
                lblCurRoom.Size = new Size(innerWidth, 24);
                btnTiepTuc.Width = innerWidth;

                pnlCLS.Location = new Point(0, pnlCur.Bottom + 12);
                pnlCLS.Width = cardWidth;
                pnlCLSList.Width = innerWidth;
                foreach (Control c in pnlCLSList.Controls) c.Width = Math.Max(180, innerWidth - 8);
                btnFilterCLS.Width = innerWidth;

                pnlChuyenMon.Location = new Point(0, pnlCLS.Bottom + 12);
                pnlChuyenMon.Width = cardWidth;
                lblShiftInfo.Width = innerWidth;
                lblTopList.Width = innerWidth;
                btnViewAllRecord.Width = innerWidth;

                pnlRightBottomPad.Location = new Point(0, pnlChuyenMon.Bottom + 10);
            }
            grpRight.SizeChanged += (s, e) => LayoutRightCards();
            LayoutRightCards();

            // ══════════════════════════════════════════════
            // CỘT TRÁI: DANH SÁCH KHÁM HÔM NAY
            // ══════════════════════════════════════════════
            var grpLeft = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16) };
            grpLeft.Paint += PaintCardBorder;

            // ── HEADER ROW 1: Tiêu đề + scope filter ──
            // lblListTitle cố định bên trái, cboScope anchor phải — tránh đè chữ
            var lblListTitle = new Label
            {
                Text = "Danh sách khám hôm nay",
                Location = new Point(16, 13),
                Size = new Size(280, 30),
                AutoSize = false,
                Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                ForeColor = C_TextDark
            };

            GridPageController? listPageController = null;
            void LoadList() => listPageController?.Reload();

            // Scope filter — anchor phải để không đè tiêu đề
            var cboScope = new ComboBox
            {
                Size = new Size(230, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            cboScope.Items.Add("👤 Bệnh nhân của tôi");
            if (!string.IsNullOrEmpty(roomInfoBS.maPhong))
                cboScope.Items.Add($"🏢 Cả phòng {roomInfoBS.tenPhong}");
            else
                cboScope.Items.Add("🏢 Tất cả bệnh nhân");
            cboScope.SelectedIndex = 0;
            cboScope.SelectedIndexChanged += (s, e) => LoadList();

            // ── HEADER ROW 2 (y=52): thanh lọc ──
            var txtSearch = MakeInput(16, 52, 260, false, "");
            var lblPlaceholder = new Label
            {
                Text = "🔍 Tìm bệnh nhân...",
                Location = new Point(24, 58),
                AutoSize = true,
                ForeColor = Color.FromArgb(156, 163, 175),
                BackColor = Color.White
            };
            lblPlaceholder.Click += (s, e) => txtSearch.Focus();
            txtSearch.Enter += (s, e) => lblPlaceholder.Visible = false;
            txtSearch.Leave += (s, e) => lblPlaceholder.Visible = string.IsNullOrEmpty(txtSearch.Text);

            var cboStatus = new ComboBox
            {
                Location = new Point(290, 52),
                Size = new Size(155, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            cboStatus.Items.AddRange(new object[] { "Tất cả trạng thái", "Chờ khám", "Đang khám", "Đã khám xong", "Chờ kết quả CLS" });
            cboStatus.SelectedIndex = 0;

            var btnRefresh = SecondaryBtn("🔄 Làm mới", 460, 51, 100, 31, C_Blue);

            var approvedAppointments = ClinicDAL.LayLichHenDaDuyetBacSi(maBS, DateTime.Today);
            var btnApprovedAppointments = SecondaryBtn(
                $"📅 Lịch đã duyệt ({approvedAppointments.Rows.Count})", 570, 51, 200, 31, C_Teal);
            btnApprovedAppointments.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnApprovedAppointments.Click += (s, e) =>
            {
                var latest = ClinicDAL.LayLichHenDaDuyetBacSi(maBS, DateTime.Today);
                btnApprovedAppointments.Text = $"📅 Lịch đã duyệt ({latest.Rows.Count})";
                ShowBacSiLichHenDaDuyetView(maBS);
            };

            // Căn vị trí cboScope về phía phải của grpLeft header
            void PositionScopeCombo()
            {
                cboScope.Location = new Point(Math.Max(310, grpLeft.ClientSize.Width - cboScope.Width - 16), 11);
                btnApprovedAppointments.Left = Math.Max(btnRefresh.Right + 10, grpLeft.ClientSize.Width - btnApprovedAppointments.Width - 16);
            }
            grpLeft.SizeChanged += (s, e) => PositionScopeCombo();
            PositionScopeCombo(); // set vị trí ngay lúc khởi tạo
            var dgv = MakeGrid(DockStyle.None);
            dgv.Location = new Point(16, 96);
            dgv.Size = new Size(grpLeft.Width - 32, grpLeft.Height - 150);
            dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgv.ScrollBars = ScrollBars.Vertical;
            dgv.DataBindingComplete += (sender, args) =>
            {
                SafeFormatCol(dgv, "MaPK", "Mã PK", 65, fillWeight: 80f);
                SafeFormatCol(dgv, "STT", "STT", 40, fillWeight: 45f);
                SafeFormatCol(dgv, "GioKham", "Giờ", 55, fillWeight: 60f);
                SafeFormatCol(dgv, "MaBN", "Mã BN", 65, fillWeight: 80f);
                SafeFormatCol(dgv, "TenBN", "Bệnh nhân", 100, fillWeight: 135f);
                SafeFormatCol(dgv, "GioiTinh", "Phái", 45, fillWeight: 60f);
                SafeFormatCol(dgv, "Tuoi", "Tuổi", 45, fillWeight: 45f);
                SafeFormatCol(dgv, "LyDoKham", "Lý do khám", 115, fillWeight: 180f);
                SafeFormatCol(dgv, "TrangThai", "Trạng thái", 100, fillWeight: 120f);
                SafeFormatCol(dgv, "TenBacSi", "Bác sĩ", null, false);
                SafeFormatCol(dgv, "SoDienThoai", "SĐT", null, false);
            };

            // Filter events

            txtSearch.TextChanged += (s, e) => LoadList();
            cboStatus.SelectedIndexChanged += (s, e) => LoadList();
            btnRefresh.Click += (s, e) => LoadList();

            btnFilterCLS.Click += (s, e) =>
            {
                cboStatus.SelectedItem = "Chờ kết quả CLS";
                LoadList();
            };

            // Paging / Action bar at bottom
            var pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = Color.Transparent };
            var lblCount = new Label { Text = "Hiển thị danh sách ca khám trong ngày", Location = new Point(16, 12), AutoSize = true, Font = F_Small, ForeColor = C_TextGray };
            var btnOpenSelected = PrimaryBtn("🩺 Mở phiên khám", grpLeft.Width - 310, 6, 140, 32);
            btnOpenSelected.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var btnViewRecord = SecondaryBtn("👤 Hồ sơ bệnh án", grpLeft.Width - 160, 6, 140, 32, C_Blue);
            btnViewRecord.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            btnOpenSelected.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn một bệnh nhân từ danh sách!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string maPK = dgv.SelectedRows[0].Cells["MaPK"].Value?.ToString() ?? "";
                ShowBacSiKhamKeDonView(maPK, 0);
            };

            btnViewRecord.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn một bệnh nhân từ danh sách!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string maBN = dgv.SelectedRows[0].Cells["MaBN"].Value?.ToString() ?? "";
                ShowBacSiHoSoBenhAnView(maBN);
            };

            dgv.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    string maPK = dgv.Rows[e.RowIndex].Cells["MaPK"].Value?.ToString() ?? "";
                    ShowBacSiKhamKeDonView(maPK, 0);
                }
            };

            pnlFooter.Controls.AddRange(new Control[] { lblCount, btnOpenSelected, btnViewRecord });
            grpLeft.Controls.AddRange(new Control[] { lblListTitle, cboScope, txtSearch, lblPlaceholder, cboStatus, btnRefresh, btnApprovedAppointments, dgv, pnlFooter });
            // grpLeft.SizeChanged handled by PositionScopeCombo above

            // Thêm cột phải trước, sau đó là cột trái fill
            pnlBody.Controls.Add(grpLeft);
            pnlBody.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlBody.Controls.Add(grpRight);
            ConfigureDoctorRightColumn(pnlBody, grpRight);

            // ── INFO BAR: Phòng khám & Khoa (giữa KPI và body) ──
            var pnlRoomInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 38,
                BackColor = Color.FromArgb(13, 148, 136),   // teal-600
                Padding = new Padding(14, 0, 14, 0)
            };
            string roomSummary = !string.IsNullOrEmpty(roomInfoBS.maPhong)
                ? $"🏢  {roomInfoBS.tenPhong}  ({roomInfoBS.tenKhoa})     •     Tổ phụ trách: {roomInfoBS.soBacSi} Bác sĩ  &  {roomInfoBS.soYTa} Điều dưỡng / Y tá"
                : "🏢  Chưa được phân công phòng khám";
            var lblRoomInfo = new Label
            {
                Text = roomSummary,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            pnlRoomInfo.Controls.Add(lblRoomInfo);

            wrapper.Controls.AddRange(new Control[] { pnlBody, pnlRoomInfo, pnlTop });
            panelContent.Controls.Add(wrapper);

            listPageController = GridPageController.Attach(dgv, (offset, pageSize) =>
            {
                string? targetBS = cboScope.SelectedIndex == 0 ? maBS : null;
                string? targetPhong = cboScope.SelectedIndex == 1 ? roomInfoBS.maPhong : null;
                return ClinicDAL.LayDanhSachKhamBacSi(
                    string.IsNullOrEmpty(targetBS) ? null : targetBS,
                    txtSearch.Text.Trim(),
                    cboStatus.SelectedItem?.ToString(),
                    offset,
                    pageSize,
                    targetPhong);
            });
        }

        private void ShowBacSiLichHenDaDuyetView(string maBS)
        {
            using var dialog = new Form
            {
                Text = "Lịch hẹn đã được lễ tân duyệt",
                Size = new Size(900, 560),
                MinimumSize = new Size(760, 440),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(12) };
            toolbar.Controls.Add(new Label
            {
                Text = "Lịch hẹn đã được xác nhận",
                Location = new Point(12, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = C_TextDark
            });
            var datePicker = new DateTimePicker
            {
                Location = new Point(300, 12),
                Width = 145,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            var refresh = SecondaryBtn("Làm mới", 460, 10, 100, 30, C_Blue);
            var grid = MakeGrid();
            grid.Dock = DockStyle.Fill;
            void LoadAppointments()
            {
                DataTable appointments = ClinicDAL.LayLichHenDaDuyetBacSi(maBS, datePicker.Value.Date);
                grid.DataSource = appointments;
                SafeFormatCol(grid, "MaLichHen", "Mã lịch", 70);
                SafeFormatCol(grid, "NgayHen", "Ngày giờ hẹn", 140);
                SafeFormatCol(grid, "MaBN", "Mã BN", 80);
                SafeFormatCol(grid, "TenBN", "Bệnh nhân", 180);
                SafeFormatCol(grid, "SoDienThoai", "Điện thoại", 120);
                SafeFormatCol(grid, "LyDoKham", "Lý do khám", 200);
                SafeFormatCol(grid, "TenPhong", "Phòng", 150);
            }
            datePicker.ValueChanged += (s, e) => LoadAppointments();
            refresh.Click += (s, e) => LoadAppointments();
            toolbar.Controls.AddRange(new Control[] { datePicker, refresh });
            dialog.Controls.Add(grid);
            dialog.Controls.Add(toolbar);
            LoadAppointments();
            dialog.ShowDialog(this);
        }

        // ═══════════════════════════════════════════════════════════
        //  VIEW 2: BÁC SĨ - HỒ SƠ BỆNH ÁN
        // ═══════════════════════════════════════════════════════════
        public void ShowBacSiHoSoBenhAnView(string? selectedMaBN = null)
        {
            SetRefreshPageAction(() => ShowBacSiHoSoBenhAnView(selectedMaBN));
            SetActiveNav(btnNavBenhNhan);
            if (string.IsNullOrEmpty(selectedMaBN))
            {
                ShowBacSiDanhSachBenhNhanView();
                return;
            }

            SetPageHeader("Bác sĩ  ›  Bệnh nhân", "Hồ sơ bệnh án chi tiết");
            panelContent.Controls.Clear();

            string maBS = SessionState.CurrentUser?.MaNV ?? "";
            if (!ClinicDAL.BacSiDuocPhanCongBenhNhan(maBS, selectedMaBN))
            {
                MessageBox.Show("Bệnh nhân chưa từng được phân công cho bác sĩ đang đăng nhập.", "Không có quyền truy cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowBacSiDanhSachBenhNhanView();
                return;
            }

            DataRow? rBN = ClinicDAL.LayBenhNhanTheoMa(selectedMaBN);
            if (rBN == null)
            {
                MessageBox.Show("Không tìm thấy hồ sơ bệnh nhân trong cơ sở dữ liệu.", "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowBacSiLichKhamView();
                return;
            }

            string maBN = rBN["MaBN"].ToString() ?? "";
            string tenBN = rBN["HoTen"].ToString() ?? "";
            string gioi = rBN["GioiTinh"].ToString() ?? "";
            string sdt = rBN["SoDienThoai"] == DBNull.Value ? "Chưa cập nhật" : rBN["SoDienThoai"].ToString() ?? "Chưa cập nhật";
            string diaChi = rBN["DiaChi"] == DBNull.Value ? "Chưa cập nhật" : rBN["DiaChi"].ToString() ?? "Chưa cập nhật";
            string tienSu = rBN["TienSuBenh"] == DBNull.Value ? "" : rBN["TienSuBenh"].ToString() ?? "";
            DataTable healthRecords = ClinicDAL.LayHoSoSucKhoe(maBN);
            foreach (DataRow item in healthRecords.Rows)
            {
                string entry = $"{item["Loai"]}: {item["NoiDung"]}";
                tienSu = string.IsNullOrWhiteSpace(tienSu) ? entry : tienSu + " | " + entry;
            }
            string nsStr = rBN["NgaySinh"] != DBNull.Value ? Convert.ToDateTime(rBN["NgaySinh"]).ToString("dd/MM/yyyy") : "Chưa cập nhật";
            var dtLichSu = ClinicDAL.LayLichSuKhamBenhNhan(maBN, maBS);
            var dtChiDinh = ClinicDAL.LayChiDinhDVBenhNhan(maBN, maBS);
            var dtDonThuoc = ClinicDAL.LayChiTietDonThuocBenhNhan(maBN, maBS);
            string lastVisit = dtLichSu.Rows.Count > 0
                ? Convert.ToDateTime(dtLichSu.Rows[0]["NgayKham"]).ToString("dd/MM/yyyy HH:mm")
                : "Chưa có lần khám";
            string lastDoctor = dtLichSu.Rows.Count > 0 ? dtLichSu.Rows[0]["TenBacSi"].ToString() ?? "Chưa có" : "Chưa có";

            var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // ── TOP BACK BUTTON & HEADER ──
            var pnlTopNav = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            var btnBack = new LinkLabel
            {
                Text = "← Danh sách bệnh nhân được phân công",
                Location = new Point(2, 8),
                AutoSize = true,
                Font = F_Bold,
                LinkColor = C_Teal,
                ActiveLinkColor = C_TealDark
            };
            btnBack.LinkClicked += (s, e) => ShowBacSiDanhSachBenhNhanView();

            var btnOpenExam = PrimaryBtn("🩺 Mở phiên khám", panelContent.Width - 190, 4, 150, 32);
            btnOpenExam.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenExam.Click += (s, e) =>
            {
                var dtToday = ClinicDAL.LayDanhSachKhamBacSi(maBS, maBN);
                DataRow? activeAppointment = dtToday.Rows.Cast<DataRow>()
                    .FirstOrDefault(row => row["TrangThai"].ToString() is "Chờ khám" or "Đang khám" or "Chờ kết quả CLS");
                if (activeAppointment == null)
                {
                    MessageBox.Show("Bệnh nhân không có phiếu khám đang chờ xử lý hôm nay.", "Không có ca khám", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                ShowBacSiKhamKeDonView(activeAppointment["MaPK"].ToString(), 0);
            };

            pnlTopNav.Controls.AddRange(new Control[] { btnBack, btnOpenExam });

            // ── PATIENT BANNER CARD ──
            var pnlBanner = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = C_Card, Padding = new Padding(16, 12, 16, 12) };
            pnlBanner.Paint += PaintCardBorder;

            var lblName = new Label { Text = tenBN, Location = new Point(16, 12), AutoSize = true, Font = new Font("Segoe UI", 15f, FontStyle.Bold), ForeColor = C_TextDark };
            var badgeBN = new Label { Text = $"{maBN}  •  {gioi}  •  Sinh: {nsStr}", Location = new Point(16, 44), AutoSize = true, Font = F_Small, ForeColor = C_TextGray };

            var lblInfo1 = new Label { Text = $"📅 Lần khám gần nhất: {lastVisit}", Location = new Point(360, 16), AutoSize = true, Font = F_Small, ForeColor = C_TextDark };
            var lblInfo2 = new Label { Text = $"👨‍⚕️ Bác sĩ gần nhất: {lastDoctor}", Location = new Point(360, 42), AutoSize = true, Font = F_Small, ForeColor = C_TextDark };

            var lblInfo3 = new Label
            {
                Text = $"☎ Điện thoại: {sdt}",
                Location = new Point(620, 16),
                AutoSize = true,
                Font = F_Small,
                ForeColor = C_TextDark
            };
            var lblInfo4 = new Label
            {
                Text = $"📍 Địa chỉ: {diaChi}",
                Location = new Point(620, 42),
                AutoSize = true,
                Font = F_Small,
                ForeColor = C_TextDark
            };

            pnlBanner.Controls.AddRange(new Control[] { lblName, badgeBN, lblInfo1, lblInfo2, lblInfo3, lblInfo4 });

            // ── ALERT NOTICE ──
            var pnlAlert = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.FromArgb(243, 244, 246), Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 8, 0, 8) };
            pnlAlert.Controls.Add(new Label { Text = "🔒 Cơ sở: Ghi nhận thông tin cho giai đoạn điều trị. Đảm bảo bảo mật thông tin hồ sơ y tế theo quy định.", Dock = DockStyle.Fill, Font = F_Small, ForeColor = Color.FromArgb(75, 85, 99) });

            // ── MAIN BODY (TWO COLUMNS) ──
            var pnlMainBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // CỘT PHẢI: THÔNG TIN AN TOÀN & TÀI LIỆU CHIA SẺ
            var pnlRightCol = new Panel { Dock = DockStyle.Right, Width = 360, BackColor = Color.Transparent, Padding = new Padding(8, 8, 0, 0) };

            // Card Thông tin an toàn
            var cardSafe = new Panel { Dock = DockStyle.Top, Height = 280, BackColor = C_Card, Padding = new Padding(16) };
            cardSafe.Paint += PaintCardBorder;

            var lblSafeTitle = new Label { Text = "Thông tin an toàn người bệnh", Location = new Point(14, 14), AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = C_TextDark };

            var boxDiUng = new Panel { Location = new Point(14, 44), Size = new Size(cardSafe.Width - 28, 54), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(254, 242, 242), Padding = new Padding(8) };
            boxDiUng.Controls.Add(new Label { Text = "⚠️ Dị ứng / Chống chỉ định:", Location = new Point(8, 6), AutoSize = true, Font = F_Bold, ForeColor = C_Red });
            boxDiUng.Controls.Add(new Label { Text = string.IsNullOrWhiteSpace(tienSu) ? "Chưa khai báo tiền sử bệnh / dị ứng." : tienSu, Location = new Point(8, 26), AutoSize = true, Font = F_Small, ForeColor = C_Red });

            var boxBenhNen = new Panel { Location = new Point(14, 106), Size = new Size(cardSafe.Width - 28, 48), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(8) };
            boxBenhNen.Controls.Add(new Label { Text = "ℹ️ Bệnh nền:", Location = new Point(8, 6), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark });
            boxBenhNen.Controls.Add(new Label { Text = string.IsNullOrWhiteSpace(tienSu) ? "Chưa khai báo." : tienSu, Location = new Point(8, 26), AutoSize = true, Font = F_Small, ForeColor = C_TextGray });

            var boxThuocDung = new Panel { Location = new Point(14, 162), Size = new Size(cardSafe.Width - 28, 48), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(8) };
            boxThuocDung.Controls.Add(new Label { Text = "💊 Thuốc đang sử dụng:", Location = new Point(8, 6), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark });
            boxThuocDung.Controls.Add(new Label { Text = "Chưa có mục ghi nhận thuốc đang sử dụng trong hồ sơ.", Location = new Point(8, 26), AutoSize = true, Font = F_Small, ForeColor = C_TextGray });

            var btnUpdateSafety = SecondaryBtn("✏ Cập nhật thông tin an toàn", 14, 224, cardSafe.Width - 28, 34, C_Teal);
            btnUpdateSafety.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnUpdateSafety.Click += (s, e) =>
            {
                string newTS = Microsoft.VisualBasic.Interaction.InputBox("Cập nhật tiền sử bệnh / dị ứng cho bệnh nhân:", "Cập nhật an toàn người bệnh", tienSu);
                if (!string.IsNullOrWhiteSpace(newTS))
                {
                    try
                    {
                        if (!ClinicDAL.CapNhatTienSuBenhNhan(maBN, newTS))
                        {
                            MessageBox.Show("Không thể cập nhật tiền sử bệnh nhân.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        MessageBox.Show("Đã cập nhật tiền sử bệnh nhân.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        ShowBacSiHoSoBenhAnView(maBN);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi cập nhật tiền sử: {ex.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            cardSafe.Controls.AddRange(new Control[] { lblSafeTitle, boxDiUng, boxBenhNen, boxThuocDung, btnUpdateSafety });

            var cardContact = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16), Margin = new Padding(0, 14, 0, 0) };
            cardContact.Paint += PaintCardBorder;
            cardContact.Controls.Add(new Label
            {
                Text = "Thông tin liên hệ",
                Location = new Point(14, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = C_TextDark
            });
            cardContact.Controls.Add(new Label
            {
                Text = $"Điện thoại: {sdt}\nĐịa chỉ: {diaChi}",
                Location = new Point(14, 46),
                Size = new Size(cardContact.Width - 28, 64),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = F_Base,
                ForeColor = C_TextDark
            });
            cardContact.Controls.Add(new Label
            {
                Text = "Tệp kết quả được lưu trực tiếp theo từng chỉ định CLS.",
                Location = new Point(14, 122),
                Size = new Size(cardContact.Width - 28, 48),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = F_Small,
                ForeColor = C_TextGray
            });

            var pnlRightWrapper = new Panel { Dock = DockStyle.Fill };
            pnlRightWrapper.Controls.Add(cardContact);
            pnlRightWrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent });
            pnlRightWrapper.Controls.Add(cardSafe);
            pnlRightCol.Controls.Add(pnlRightWrapper);

            // CỘT TRÁI: DIỄN BIẾN LÂM SÀNG & KẾT QUẢ XN GẦN NHẤT
            var pnlLeftCol = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 8, 8, 0) };

            // Card Diễn biến lâm sàng
            var cardDienBien = new Panel { Dock = DockStyle.Top, Height = 340, BackColor = C_Card, Padding = new Padding(16) };
            cardDienBien.Paint += PaintCardBorder;

            var lblDBTitle = new Label { Text = "Diễn biến lâm sàng", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = C_TextDark };

            var tabControlDB = new TabControl { Location = new Point(14, 46), Size = new Size(cardDienBien.Width - 28, cardDienBien.Height - 60), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, Font = F_Base };

            // Tab 1: Bệnh sử & khám bệnh
            var tabBenhSu = new TabPage("  📋 Bệnh sử & Khám bệnh  ");
            var dgvLichSu = MakeGrid();
            dgvLichSu.BorderStyle = BorderStyle.FixedSingle;
            dgvLichSu.CellBorderStyle = DataGridViewCellBorderStyle.Single;
            dgvLichSu.GridColor = C_Border;
            dgvLichSu.DataSource = dtLichSu;
            SafeFormatCol(dgvLichSu, "MaPK", "Mã phiếu", 90);
            SafeFormatCol(dgvLichSu, "NgayKham", "Ngày khám", 110);
            SafeFormatCol(dgvLichSu, "TrieuChung", "Triệu chứng", 180);
            SafeFormatCol(dgvLichSu, "KhamLamSang", "Khám lâm sàng", 180);
            SafeFormatCol(dgvLichSu, "SinhHieu", "Sinh hiệu", 150);
            SafeFormatCol(dgvLichSu, "ChanDoan", "Chẩn đoán", 180);
            SafeFormatCol(dgvLichSu, "LoiDan", "Lời dặn", 160);
            SafeFormatCol(dgvLichSu, "TenBacSi", "Bác sĩ", 140);
            SafeFormatCol(dgvLichSu, "TrangThai", "Trạng thái", 100);
            tabBenhSu.Controls.Add(dgvLichSu);

            // Tab 2: Cận lâm sàng
            var tabCLS = new TabPage("  🔬 Cận lâm sàng  ");
            var dgvCLSHistory = MakeGrid();
            dgvCLSHistory.DataSource = dtChiDinh.Copy();
            SafeFormatCol(dgvCLSHistory, "NgayKham", "Ngày khám", 110);
            SafeFormatCol(dgvCLSHistory, "MaPK", "Mã phiếu", 90);
            SafeFormatCol(dgvCLSHistory, "TenDV", "Dịch vụ", 180);
            SafeFormatCol(dgvCLSHistory, "DonGiaThoiDiem", "Đơn giá", 100);
            SafeFormatCol(dgvCLSHistory, "TrangThai", "Trạng thái", 110);
            SafeFormatCol(dgvCLSHistory, "KetQua", "Kết quả", 180);
            tabCLS.Controls.Add(dgvCLSHistory);

            // Tab 3: Đơn thuốc
            var tabThuoc = new TabPage("  💊 Đơn thuốc đã kê  ");
            var dgvDonThuocHistory = MakeGrid();
            dgvDonThuocHistory.DataSource = dtDonThuoc.Copy();
            SafeFormatCol(dgvDonThuocHistory, "NgayKham", "Ngày khám", 110);
            SafeFormatCol(dgvDonThuocHistory, "MaDonThuoc", "Mã đơn", 90);
            SafeFormatCol(dgvDonThuocHistory, "TenThuoc", "Thuốc", 160);
            SafeFormatCol(dgvDonThuocHistory, "DonViTinh", "Đơn vị", 80);
            SafeFormatCol(dgvDonThuocHistory, "SoLuong", "Số lượng", 75);
            SafeFormatCol(dgvDonThuocHistory, "DonGiaThoiDiem", "Đơn giá", 100);
            SafeFormatCol(dgvDonThuocHistory, "ThanhTien", "Thành tiền", 110);
            SafeFormatCol(dgvDonThuocHistory, "CachDung", "Cách dùng", 220);
            tabThuoc.Controls.Add(dgvDonThuocHistory);

            var tabHealth = new TabPage("  🛡 Hồ sơ sức khỏe  ");
            var dgvHealth = MakeGrid();
            dgvHealth.Dock = DockStyle.Fill;
            dgvHealth.DataSource = healthRecords.Copy();
            SafeFormatCol(dgvHealth, "Loai", "Nhóm thông tin", 140);
            SafeFormatCol(dgvHealth, "NoiDung", "Nội dung", 360);
            SafeFormatCol(dgvHealth, "NgayCapNhat", "Cập nhật", 130);
            SafeFormatCol(dgvHealth, "MaNV", "Mã nhân viên", 100);
            var btnAddHealth = SecondaryBtn("＋ Thêm mục sức khỏe", 8, 8, 190, 32, C_Teal);
            btnAddHealth.Click += (s, e) =>
            {
                using var editor = new Form
                {
                    Text = "Bổ sung hồ sơ sức khỏe",
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    ClientSize = new Size(430, 210),
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                var category = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(18, 42),
                    Size = new Size(394, 30)
                };
                category.Items.AddRange(new object[] { "Dị ứng", "Bệnh mạn tính", "Thuốc đang dùng", "Tiền sử phẫu thuật" });
                category.SelectedIndex = 0;
                var content = new TextBox
                {
                    Location = new Point(18, 96),
                    Size = new Size(394, 54),
                    Multiline = true,
                    MaxLength = 500
                };
                var save = new Button { Text = "Lưu thông tin", Location = new Point(280, 164), Size = new Size(132, 32), DialogResult = DialogResult.OK };
                editor.Controls.AddRange(new Control[]
                {
                    new Label { Text = "Loại thông tin", Location = new Point(18, 18), AutoSize = true },
                    category,
                    new Label { Text = "Nội dung (tối đa 500 ký tự)", Location = new Point(18, 76), AutoSize = true },
                    content,
                    save
                });
                editor.AcceptButton = save;
                if (editor.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(content.Text))
                    return;
                try
                {
                    ClinicDAL.LuuMucHoSoSucKhoe(maBN, category.SelectedItem?.ToString() ?? "", content.Text);
                    ShowBacSiHoSoBenhAnView(maBN);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể cập nhật hồ sơ sức khỏe: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            tabHealth.Controls.Add(dgvHealth);
            tabHealth.Controls.Add(btnAddHealth);
            btnAddHealth.BringToFront();

            tabControlDB.TabPages.AddRange(new[] { tabBenhSu, tabCLS, tabThuoc, tabHealth });
            cardDienBien.Controls.AddRange(new Control[] { lblDBTitle, tabControlDB });

            // Card dịch vụ cận lâm sàng gần đây
            var cardKQ = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16), Margin = new Padding(0, 14, 0, 0) };
            cardKQ.Paint += PaintCardBorder;

            var lblKQTitle = new Label { Text = "Dịch vụ cận lâm sàng & kết quả đã ghi nhận", Location = new Point(14, 14), AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold), ForeColor = C_TextDark };
            var dgvLatestCLS = MakeGrid(DockStyle.None);
            dgvLatestCLS.Location = new Point(14, 46);
            dgvLatestCLS.Size = new Size(cardKQ.Width - 28, cardKQ.Height - 60);
            dgvLatestCLS.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvLatestCLS.DataSource = dtChiDinh.Copy();
            SafeFormatCol(dgvLatestCLS, "NgayKham", "Ngày khám", 110);
            SafeFormatCol(dgvLatestCLS, "MaPK", "Mã phiếu", 90);
            SafeFormatCol(dgvLatestCLS, "TenDV", "Dịch vụ", 180);
            SafeFormatCol(dgvLatestCLS, "TrangThai", "Trạng thái", 110);
            SafeFormatCol(dgvLatestCLS, "KetQua", "Kết quả", 180);
            cardKQ.Controls.AddRange(new Control[] { lblKQTitle, dgvLatestCLS });

            var pnlLeftWrapper = new Panel { Dock = DockStyle.Fill };
            pnlLeftWrapper.Controls.Add(cardKQ);
            pnlLeftWrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent });
            pnlLeftWrapper.Controls.Add(cardDienBien);
            pnlLeftCol.Controls.Add(pnlLeftWrapper);

            pnlMainBody.Controls.Add(pnlLeftCol);
            pnlMainBody.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlMainBody.Controls.Add(pnlRightCol);
            ConfigureDoctorRightColumn(pnlMainBody, pnlRightCol);

            wrapper.Controls.AddRange(new Control[] { pnlMainBody, pnlAlert, pnlBanner, pnlTopNav });
            panelContent.Controls.Add(wrapper);
        }

        private void ShowBacSiDanhSachBenhNhanView()
        {
            SetActiveNav(btnNavBenhNhan);
            SetPageHeader("Bác sĩ  ›  Bệnh nhân", "Danh sách bệnh nhân được phân công");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;
            var txtSearch = MakeInput(12, 10, 320);
            txtSearch.PlaceholderText = "Tìm theo mã, tên hoặc số điện thoại...";
            toolPanel.Controls.Add(txtSearch);

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            contentPanel.Paint += PaintCardBorder;
            var dgv = MakeGrid();
            contentPanel.Controls.Add(dgv);

            GridPageController? patientPageController = null;
            void LoadPatients() => patientPageController?.Reload();

            void OpenSelectedPatient()
            {
                if (dgv.SelectedRows.Count == 0) return;
                string maBN = dgv.SelectedRows[0].Cells["MaBN"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(maBN))
                    ShowBacSiHoSoBenhAnView(maBN);
            }

            txtSearch.TextChanged += (s, e) => LoadPatients();
            dgv.CellDoubleClick += (s, e) => OpenSelectedPatient();

            var btnView = PrimaryBtn("Xem hồ sơ", 350, 8, 120, 34);
            btnView.Click += (s, e) => OpenSelectedPatient();
            toolPanel.Controls.Add(btnView);

            patientPageController = GridPageController.Attach(dgv, (offset, pageSize) =>
                ClinicDAL.LayDanhSachBenhNhanBacSi(
                    SessionState.CurrentUser?.MaNV ?? "",
                    txtSearch.Text.Trim(),
                    offset,
                    pageSize));

            wrapper.Controls.Add(contentPanel);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════════
        //  VIEW 3: BÁC SĨ - KHÁM & KÊ ĐƠN (3 BƯỚC LIÊN TỤC THEO FIGMA)
        // ═══════════════════════════════════════════════════════════
        public void ShowBacSiKhamKeDonView(string? targetMaPK = null, int activeStep = 0)
        {
            SetRefreshPageAction(() => ShowBacSiKhamKeDonView(targetMaPK, activeStep));
            SetActiveNav(btnNavKhamBenh);
            string maBS = SessionState.CurrentUser?.MaNV ?? "";

            // Nếu chưa có mã phiếu, lấy phiếu đầu tiên đang khám hoặc chờ khám
            if (string.IsNullOrEmpty(targetMaPK))
            {
                var dtAll = ClinicDAL.LayDanhSachKhamBacSi(string.IsNullOrEmpty(maBS) ? null : maBS);
                if (dtAll.Rows.Count > 0)
                {
                    // Ưu tiên ca đang khám
                    foreach (DataRow r in dtAll.Rows)
                    {
                        if (r["TrangThai"]?.ToString() == "Đang khám") { targetMaPK = r["MaPK"]?.ToString(); break; }
                    }
                    if (string.IsNullOrEmpty(targetMaPK)) targetMaPK = dtAll.Rows[0]["MaPK"]?.ToString();
                }
            }

            _currentDoctorMaPK = targetMaPK;
            DataRow? drPK = !string.IsNullOrEmpty(targetMaPK) ? ClinicDAL.LayChiTietPhieuKhamDayDu(targetMaPK, maBS) : null;
            if (drPK == null || drPK["MaBacSi"].ToString() != maBS)
            {
                MessageBox.Show("Không tìm thấy phiếu khám hoặc phiếu không thuộc bác sĩ đang đăng nhập.", "Không thể mở ca khám", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowBacSiLichKhamView();
                return;
            }
            _currentDoctorMaBN = drPK["MaBN"].ToString();

            string tenBN = drPK["TenBN"].ToString() ?? "";
            string maBN = drPK["MaBN"].ToString() ?? "";
            string gioi = drPK["GioiTinh"].ToString() ?? "";
            string tuoi = drPK["Tuoi"] == DBNull.Value ? "Chưa cập nhật" : drPK["Tuoi"].ToString() ?? "Chưa cập nhật";
            string sdt = drPK["SoDienThoai"] == DBNull.Value ? "Chưa cập nhật" : drPK["SoDienThoai"].ToString() ?? "Chưa cập nhật";
            string tienSu = drPK["TienSuBenh"] == DBNull.Value ? "" : drPK["TienSuBenh"].ToString() ?? "";
            string stt = drPK["STT"].ToString() ?? "";
            string tc = drPK["TrieuChung"] == DBNull.Value ? "" : drPK["TrieuChung"].ToString() ?? "";
            string cd = drPK["ChanDoan"] == DBNull.Value ? "" : drPK["ChanDoan"].ToString() ?? "";
            string ld = drPK["LoiDan"] == DBNull.Value ? "" : drPK["LoiDan"].ToString() ?? "";

            string stepTitle = activeStep switch
            {
                0 => "Lập phiếu khám",
                1 => "Kê đơn thuốc",
                2 => "Kết quả & hoàn tất khám",
                _ => "Khám & Kê đơn"
            };

            SetPageHeader($"Bác sĩ  ›  Khám & Kê đơn  ›  {stepTitle}", $"{stepTitle}  –  {tenBN} ({maBN})");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // ── TOP WORKFLOW STEPS BAR ──
            var pnlWorkflow = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Color.White };
            pnlWorkflow.Paint += (s, e) => { using var pen = new Pen(C_Border, 1); e.Graphics.DrawLine(pen, 0, pnlWorkflow.Height - 1, pnlWorkflow.Width, pnlWorkflow.Height - 1); };

            var btnStep1 = new Button { Text = "  1. 🩺 Lập phiếu khám  ", Location = new Point(14, 8), Size = new Size(180, 32), FlatStyle = FlatStyle.Flat, Font = F_Bold, Cursor = Cursors.Hand };
            var btnStep2 = new Button { Text = "  2. 💊 Kê đơn thuốc  ", Location = new Point(200, 8), Size = new Size(180, 32), FlatStyle = FlatStyle.Flat, Font = F_Bold, Cursor = Cursors.Hand };
            var btnStep3 = new Button { Text = "  3. 📑 Kết quả & Hoàn tất  ", Location = new Point(386, 8), Size = new Size(200, 32), FlatStyle = FlatStyle.Flat, Font = F_Bold, Cursor = Cursors.Hand };

            void StyleStepBtn(Button b, bool isActive)
            {
                b.FlatAppearance.BorderSize = isActive ? 2 : 0;
                b.FlatAppearance.BorderColor = C_Teal;
                b.BackColor = isActive ? Color.FromArgb(240, 253, 250) : Color.Transparent;
                b.ForeColor = isActive ? C_Teal : C_TextGray;
            }
            StyleStepBtn(btnStep1, activeStep == 0);
            StyleStepBtn(btnStep2, activeStep == 1);
            StyleStepBtn(btnStep3, activeStep == 2);

            btnStep1.Click += (s, e) => ShowBacSiKhamKeDonView(_currentDoctorMaPK, 0);
            btnStep2.Click += (s, e) => ShowBacSiKhamKeDonView(_currentDoctorMaPK, 1);
            btnStep3.Click += (s, e) => ShowBacSiKhamKeDonView(_currentDoctorMaPK, 2);

            var btnBackToList = SecondaryBtn("📋 Lịch khám", panelContent.Width - 140, 8, 110, 32, Color.FromArgb(107, 114, 128));
            btnBackToList.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnBackToList.Click += (s, e) => ShowBacSiLichKhamView();

            var btnTransfer = SecondaryBtn("Chuyển khoa / phòng", 598, 8, 160, 32, C_Blue);
            btnTransfer.Visible = activeStep == 0;
            btnTransfer.Click += (s, e) =>
            {
                DataTable rooms = ClinicDAL.LayDanhSachPhongKham();
                if (rooms.Rows.Count == 0)
                {
                    MessageBox.Show("Chưa có phòng khám để chuyển đến.", "Không có phòng", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using var transferForm = new Form
                {
                    Text = "Chuyển bệnh nhân sang khoa / phòng",
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    ClientSize = new Size(460, 275),
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                var room = new ComboBox
                {
                    Location = new Point(18, 42),
                    Size = new Size(424, 30),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DisplayMember = "TenPhong",
                    ValueMember = "MaPhong",
                    DataSource = rooms
                };
                var targetDoctor = new ComboBox
                {
                    Location = new Point(18, 104),
                    Size = new Size(424, 30),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DisplayMember = "TenHienThi",
                    ValueMember = "MaNV"
                };
                void LoadTargetDoctors()
                {
                    if (room.SelectedValue == null) return;
                    DataTable doctors = ClinicDAL.LayDanhSachBacSiTheoPhong(room.SelectedValue.ToString() ?? "");
                    if (!doctors.Columns.Contains("TenHienThi"))
                        doctors.Columns.Add("TenHienThi", typeof(string));
                    foreach (DataRow doctorRow in doctors.Rows)
                        doctorRow["TenHienThi"] = $"{doctorRow["HoTen"]} - {doctorRow["ChuyenKhoa"]}";
                    targetDoctor.DataSource = doctors;
                }
                room.SelectedValueChanged += (sender, args) => LoadTargetDoctors();
                LoadTargetDoctors();
                var reason = new TextBox
                {
                    Location = new Point(18, 166),
                    Size = new Size(424, 48),
                    Multiline = true,
                    MaxLength = 500
                };
                var confirmTransfer = new Button
                {
                    Text = "Tạo phiếu chuyển",
                    Location = new Point(292, 226),
                    Size = new Size(150, 32),
                    DialogResult = DialogResult.OK
                };
                transferForm.Controls.AddRange(new Control[]
                {
                    new Label { Text = "Khoa / phòng tiếp nhận", Location = new Point(18, 18), AutoSize = true },
                    room,
                    new Label { Text = "Bác sĩ tiếp nhận", Location = new Point(18, 80), AutoSize = true },
                    targetDoctor,
                    new Label { Text = "Lý do chuyển", Location = new Point(18, 142), AutoSize = true },
                    reason,
                    confirmTransfer
                });
                transferForm.AcceptButton = confirmTransfer;
                if (transferForm.ShowDialog(this) != DialogResult.OK) return;
                if (targetDoctor.SelectedValue == null || string.IsNullOrWhiteSpace(reason.Text))
                {
                    MessageBox.Show("Chọn bác sĩ tiếp nhận và nhập lý do chuyển.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    var childVisit = ClinicDAL.ChuyenPhongKham(
                        _currentDoctorMaPK ?? "",
                        targetDoctor.SelectedValue.ToString() ?? "",
                        room.SelectedValue?.ToString() ?? "",
                        reason.Text);
                    MessageBox.Show($"Đã tạo phiếu khám liên kết {childVisit.maPK} (STT {childVisit.stt}).",
                        "Chuyển phòng thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ShowBacSiLichKhamView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Không thể chuyển phòng: {ex.Message}", "Chuyển phòng thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            pnlWorkflow.Controls.AddRange(new Control[] { btnStep1, btnStep2, btnStep3, btnTransfer, btnBackToList });

            // ── PATIENT BANNER ──
            string maPKGoc = drPK["MaPK_Goc"] == DBNull.Value ? "" : drPK["MaPK_Goc"].ToString() ?? "";
            string lyDoChuyen = drPK["LyDoChuyen"] == DBNull.Value ? "" : drPK["LyDoChuyen"].ToString() ?? "";
            var pnlBanner = new Panel { Dock = DockStyle.Top, Height = string.IsNullOrEmpty(maPKGoc) ? 74 : 96, BackColor = C_Card, Padding = new Padding(16, 10, 16, 10) };
            pnlBanner.Paint += PaintCardBorder;

            var lblPName = new Label { Text = $"{tenBN}  ({maBN})", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 13f, FontStyle.Bold), ForeColor = C_TextDark };
            var lblPMeta = new Label { Text = $"STT #{stt}  •  {gioi}  •  {tuoi} tuổi  •  SĐT: {sdt}", Location = new Point(14, 40), AutoSize = true, Font = F_Small, ForeColor = C_TextGray };

            var badgeAllergy = new Label
            {
                Text = string.IsNullOrWhiteSpace(tienSu)
                    ? "  ⚠️ Chưa khai báo tiền sử bệnh / dị ứng  "
                    : $"  ⚠️ Cảnh báo an toàn: {tienSu}  ",
                Location = new Point(panelContent.Width - 380, 20),
                Size = new Size(340, 28),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(254, 242, 242),
                ForeColor = C_Red,
                Font = F_Bold,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlBanner.Controls.AddRange(new Control[] { lblPName, lblPMeta, badgeAllergy });
            if (!string.IsNullOrEmpty(maPKGoc))
            {
                var transferInfo = new Label
                {
                    Text = $"Phiếu chuyển từ {maPKGoc}: {lyDoChuyen}",
                    Location = new Point(14, 64),
                    Size = new Size(panelContent.Width - 40, 20),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Font = F_Small,
                    ForeColor = C_Blue
                };
                pnlBanner.Controls.Add(transferInfo);
            }

            // ── BODY CONTENT THEO STEP ──
            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0) };

            if (activeStep == 0)
            {
                BuildStep1LapPhieuKham(pnlBody, drPK, tc, cd, ld, tienSu);
            }
            else if (activeStep == 1)
            {
                BuildStep2KeDonThuoc(pnlBody, drPK, tienSu);
            }
            else
            {
                BuildStep3KetQuaHoanTat(pnlBody, drPK);
            }

            wrapper.Controls.AddRange(new Control[] { pnlBody, pnlBanner, pnlWorkflow });
            panelContent.Controls.Add(wrapper);
        }

        // ───────────────────────────────────────────────────────────
        // BƯỚC 1: LẬP PHIẾU KHÁM LÂM SÀNG & CHỈ ĐỊNH CLS
        // ───────────────────────────────────────────────────────────
        private void BuildStep1LapPhieuKham(Panel container, DataRow? drPK, string tc, string cd, string ld, string tienSu)
        {
            var pnlContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // ── CỘT PHẢI: CHỈ ĐỊNH DỊCH VỤ CLS ──
            var pnlRightCol = new Panel { Dock = DockStyle.Right, Width = 400, BackColor = Color.Transparent, Padding = new Padding(8, 0, 0, 0) };

            var cardCLS = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16) };
            cardCLS.Paint += PaintCardBorder;

            var lblCLSTitle = new Label { Text = "Chỉ định dịch vụ / xét nghiệm", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };

            var cboDichVu = new ComboBox { Location = new Point(14, 46), Size = new Size(cardCLS.Width - 160, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = F_Base, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var dtDV = ClinicDAL.LayDanhSachDichVu();
            cboDichVu.DataSource = dtDV;
            cboDichVu.DisplayMember = "TenDV";
            cboDichVu.ValueMember   = "MaDV";

            var btnAddCLS = PrimaryBtn("➕ Thêm chỉ định", cardCLS.Width - 140, 44, 125, 32);
            btnAddCLS.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var dgvCLS = MakeGrid(DockStyle.None);
            dgvCLS.Location = new Point(14, 86);
            dgvCLS.Size = new Size(cardCLS.Width - 28, cardCLS.Height - 190);
            dgvCLS.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            void LoadCLSList()
            {
                if (!string.IsNullOrEmpty(_currentDoctorMaPK))
                {
                    dgvCLS.DataSource = ClinicDAL.LayChiDinhDVTheoPhieuKham(_currentDoctorMaPK);
                    SafeFormatCol(dgvCLS, "MaChiDinh", "", null, false);
                    SafeFormatCol(dgvCLS, "MaPK", "", null, false);
                    SafeFormatCol(dgvCLS, "MaDV", "", null, false);
                    SafeFormatCol(dgvCLS, "TenDV", "Dịch vụ CLS", 180);
                    SafeFormatCol(dgvCLS, "DonGiaThoiDiem", "Đơn giá", 100);
                    var priceColumn = dgvCLS.Columns["DonGiaThoiDiem"];
                    if (priceColumn != null)
                        priceColumn.DefaultCellStyle.Format = "N0";
                    SafeFormatCol(dgvCLS, "KetQua", "Kết quả", 150);
                    SafeFormatCol(dgvCLS, "TrangThai", "Trạng thái", 110);
                }
            }
            LoadCLSList();

            btnAddCLS.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(_currentDoctorMaPK)) return;
                if (cboDichVu.SelectedItem is DataRowView drv)
                {
                    decimal donGia = Convert.ToDecimal(drv["DonGia"]);
                    string maDV = drv["MaDV"].ToString()!;
                    ClinicDAL.ChiDinhDichVu(_currentDoctorMaPK, maDV, donGia);
                    LoadCLSList();
                    MessageBox.Show($"✅ Đã chỉ định dịch vụ: {drv["TenDV"]}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnNhapKQ = SecondaryBtn("📝 Nhập kết quả CLS", 14, cardCLS.Height - 50, 160, 34, C_Orange);
            btnNhapKQ.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnNhapKQ.Click += (s, e) =>
            {
                if (dgvCLS.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Vui lòng chọn một dịch vụ CLS trong bảng!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int maCD = Convert.ToInt32(dgvCLS.SelectedRows[0].Cells["MaChiDinh"].Value);
                string tenD = dgvCLS.SelectedRows[0].Cells["TenDV"].Value?.ToString() ?? "";
                string kq = Microsoft.VisualBasic.Interaction.InputBox($"Nhập kết quả xét nghiệm / CLS cho:\n{tenD}", "Kết quả cận lâm sàng", "Chỉ số bình thường, không ghi nhận bất thường");
                if (!string.IsNullOrEmpty(kq))
                {
                    if (!ClinicDAL.CapNhatKetQuaDV(maCD, kq))
                    {
                        MessageBox.Show("Chỉ định CLS cần được thu phí trước khi cập nhật kết quả.",
                            "Chưa thanh toán CLS", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    LoadCLSList();
                }
            };

            var btnAttach = SecondaryBtn("📎 Tài liệu (chưa lưu)", 184, cardCLS.Height - 50, 180, 34, C_Teal);
            btnAttach.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAttach.Text = "📎 Tệp kết quả";
            btnAttach.Click += (s, e) =>
            {
                if (dgvCLS.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn một chỉ định CLS trước.", "Chưa chọn chỉ định",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                int maCD = Convert.ToInt32(dgvCLS.SelectedRows[0].Cells["MaChiDinh"].Value);
                string tenDV = dgvCLS.SelectedRows[0].Cells["TenDV"].Value?.ToString() ?? "CLS";
                OpenClsAttachments(maCD, tenDV);
            };

            cardCLS.Controls.AddRange(new Control[] { lblCLSTitle, cboDichVu, btnAddCLS, dgvCLS, btnNhapKQ, btnAttach });
            pnlRightCol.Controls.Add(cardCLS);

            // ── CỘT TRÁI: THÔNG TIN KHÁM LÂM SÀNG ──
            var cardLamSang = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16), AutoScroll = true };
            cardLamSang.Paint += PaintCardBorder;

            var lblTitle = new Label { Text = "Thông tin khám lâm sàng", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };

            // Row 1: Triệu chứng & Tiền sử
            var lblTC = MakeLbl("Triệu chứng / Lý do khám (*):", 14, 46, bold: true);
            var txtTC = new TextBox { Location = new Point(14, 68), Size = new Size(cardLamSang.Width - 48, 44), Multiline = true, Font = F_Base, Text = tc, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            var lblTS = MakeLbl("Tiền sử / Bệnh sử:", 14, 120, bold: true);
            var txtTS = new TextBox
            {
                Location = new Point(14, 142),
                Size = new Size(cardLamSang.Width - 48, 36),
                Multiline = true,
                Font = F_Base,
                Text = tienSu,
                ReadOnly = true,
                BackColor = Color.FromArgb(248, 250, 252),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Row 2: Vitals Grid (Sinh hiệu)
            var pnlVitals = new Panel { Location = new Point(14, 188), Size = new Size(cardLamSang.Width - 48, 76), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(8) };
            pnlVitals.Paint += PaintCardBorder;

            var txtHA = MakeInput(8, 26, 90);
            var txtMach = MakeInput(106, 26, 75);
            var txtNhiet = MakeInput(189, 26, 75);
            var txtSpO2 = MakeInput(272, 26, 65);
            var txtNang = MakeInput(345, 26, 70);
            var txtCao = MakeInput(423, 26, 70);
            var txtBMI = MakeInput(501, 26, 70, true);
            var savedVitals = drPK?["SinhHieu"] == DBNull.Value ? "" : drPK?["SinhHieu"]?.ToString() ?? "";
            var vitalValues = savedVitals.Split('|');
            if (vitalValues.Length == 7)
            {
                txtHA.Text = vitalValues[0];
                txtMach.Text = vitalValues[1];
                txtNhiet.Text = vitalValues[2];
                txtSpO2.Text = vitalValues[3];
                txtNang.Text = vitalValues[4];
                txtCao.Text = vitalValues[5];
                txtBMI.Text = vitalValues[6];
            }

            pnlVitals.Controls.AddRange(new Control[] {
                MakeLbl("Huyết áp", 8, 8, bold: true), txtHA,
                MakeLbl("Mạch (l/p)", 106, 8, bold: true), txtMach,
                MakeLbl("Nhiệt độ (°C)", 189, 8, bold: true), txtNhiet,
                MakeLbl("SpO2 (%)", 272, 8, bold: true), txtSpO2,
                MakeLbl("Cân nặng (kg)", 345, 8, bold: true), txtNang,
                MakeLbl("Chiều cao (cm)", 423, 8, bold: true), txtCao,
                MakeLbl("BMI", 501, 8, bold: true), txtBMI
            });

            void RecalcBMI()
            {
                if (double.TryParse(txtNang.Text, out double w) && double.TryParse(txtCao.Text, out double h) && h > 0)
                {
                    double hm = h / 100.0;
                    txtBMI.Text = (w / (hm * hm)).ToString("F1");
                }
            }
            txtNang.TextChanged += (s, e) => RecalcBMI();
            txtCao.TextChanged += (s, e) => RecalcBMI();

            // Row 3: Thăm khám lâm sàng
            var lblThamKham = MakeLbl("Thăm khám lâm sàng:", 14, 274, bold: true);
            var txtThamKham = new TextBox
            {
                Location = new Point(14, 296),
                Size = new Size(cardLamSang.Width - 48, 44),
                Multiline = true,
                Font = F_Base,
                Text = drPK?["KhamLamSang"] == DBNull.Value ? "" : drPK?["KhamLamSang"]?.ToString() ?? "",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            // Row 4: Chẩn đoán sơ bộ
            var lblCD = MakeLbl("Chẩn đoán sơ bộ (*):", 14, 348, bold: true);
            var icd = new ComboBox
            {
                Location = new Point(14, 370),
                Size = new Size(cardLamSang.Width - 48, 30),
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = F_Base
            };
            DataTable icdCodes = ClinicDAL.LayDanhMucICD10();
            if (!icdCodes.Columns.Contains("TenHienThi"))
                icdCodes.Columns.Add("TenHienThi", typeof(string));
            foreach (DataRow row in icdCodes.Rows)
                row["TenHienThi"] = $"{row["MaICD10"]} - {row["TenChanDoan"]}";
            icd.DataSource = icdCodes;
            icd.DisplayMember = "TenHienThi";
            icd.ValueMember = "MaICD10";
            if (icdCodes.Rows.Count == 0)
                icd.Text = "Danh mục ICD-10 chưa được nạp; có thể nhập chẩn đoán bên dưới";
            var txtCD = new TextBox { Location = new Point(14, 406), Size = new Size(cardLamSang.Width - 48, 44), Multiline = true, Font = F_Base, Text = cd, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            icd.SelectedIndexChanged += (s, e) =>
            {
                if (icd.SelectedItem is DataRowView selected)
                    txtCD.Text = $"{selected["MaICD10"]} - {selected["TenChanDoan"]}";
            };

            // Row 5: Hướng xử trí
            var lblHD = MakeLbl("Hướng xử trí:", 14, 458, bold: true);
            var txtHD = new TextBox { Location = new Point(14, 480), Size = new Size(cardLamSang.Width - 48, 44), Multiline = true, Font = F_Base, Text = ld, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Action buttons
            var btnSaveDraft = SecondaryBtn("💾 Lưu nháp", 14, 538, 120, 36, Color.FromArgb(107, 114, 128));
            var btnNextStep  = PrimaryBtn("Tiếp theo: Kê đơn thuốc  ➔", 144, 538, 230, 36);

            string GetSinhHieu()
                => string.Join("|", txtHA.Text.Trim(), txtMach.Text.Trim(), txtNhiet.Text.Trim(),
                    txtSpO2.Text.Trim(), txtNang.Text.Trim(), txtCao.Text.Trim(), txtBMI.Text.Trim());

            btnSaveDraft.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(_currentDoctorMaPK)) return;
                try
                {
                    if (ClinicDAL.CapNhatKetQuaKham(_currentDoctorMaPK, SessionState.CurrentUser?.MaNV ?? "", txtTC.Text.Trim(), txtThamKham.Text.Trim(), GetSinhHieu(), txtCD.Text.Trim(), txtHD.Text.Trim(), "Đang khám"))
                        MessageBox.Show("Đã lưu nháp kết quả khám lâm sàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    else
                        MessageBox.Show("Không tìm thấy phiếu khám để lưu.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu kết quả khám: {ex.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnNextStep.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(_currentDoctorMaPK)) return;
                if (string.IsNullOrWhiteSpace(txtCD.Text))
                {
                    MessageBox.Show("Vui lòng nhập chẩn đoán trước khi chuyển sang kê đơn.", "Thiếu chẩn đoán", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCD.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(txtTC.Text))
                {
                    MessageBox.Show("Vui lòng nhập triệu chứng / lý do khám.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtTC.Focus();
                    return;
                }
                try
                {
                    if (!ClinicDAL.CapNhatKetQuaKham(_currentDoctorMaPK, SessionState.CurrentUser?.MaNV ?? "", txtTC.Text.Trim(), txtThamKham.Text.Trim(), GetSinhHieu(), txtCD.Text.Trim(), txtHD.Text.Trim(), "Đang khám"))
                    {
                        MessageBox.Show("Không tìm thấy phiếu khám để lưu. Chưa thể chuyển bước.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu kết quả khám: {ex.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                ShowBacSiKhamKeDonView(_currentDoctorMaPK, 1);
            };

            cardLamSang.Controls.AddRange(new Control[] {
                lblTitle, lblTC, txtTC, lblTS, txtTS, pnlVitals,
                lblThamKham, txtThamKham, lblCD, icd, txtCD, lblHD, txtHD,
                btnSaveDraft, btnNextStep
            });

            pnlContainer.Controls.Add(cardLamSang);
            pnlContainer.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlContainer.Controls.Add(pnlRightCol);
            ConfigureDoctorRightColumn(pnlContainer, pnlRightCol);
            container.Controls.Add(pnlContainer);
        }

        // ───────────────────────────────────────────────────────────
        // BƯỚC 2: KÊ ĐƠN THUỐC ĐIỆN TỬ
        // ───────────────────────────────────────────────────────────
        private void OpenClsAttachments(int maChiDinh, string tenDichVu)
        {
            using var dialog = new Form
            {
                Text = $"Tài liệu CLS - {tenDichVu}",
                Size = new Size(680, 420),
                MinimumSize = new Size(560, 340),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.Sizable,
                BackColor = Color.White
            };
            var title = new Label
            {
                Text = $"Tệp kết quả cho {tenDichVu}  •  Chấp nhận PDF/JPEG/PNG, tối đa 10 MB mỗi tệp",
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(14, 12, 12, 0),
                Font = F_Bold
            };
            var grid = MakeGrid();
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 54,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };
            var close = new Button { Text = "Đóng", Width = 90, Height = 32, DialogResult = DialogResult.Cancel };
            var download = SecondaryBtn("Tải tệp đã chọn", 0, 0, 135, 32, C_Blue);
            var upload = PrimaryBtn("Tải tệp lên SQL", 0, 0, 130, 32);
            actions.Controls.AddRange(new Control[] { close, download, upload });

            void LoadAttachments()
            {
                grid.DataSource = ClinicDAL.LayTaiLieuCLS(maChiDinh);
                SafeFormatCol(grid, "MaTaiLieu", "Mã", 65);
                SafeFormatCol(grid, "TenTep", "Tên tệp", 250);
                SafeFormatCol(grid, "LoaiNoiDung", "Định dạng", 130);
                SafeFormatCol(grid, "KichThuoc", "Dung lượng (byte)", 120);
                SafeFormatCol(grid, "NgayTaiLen", "Ngày tải", 145);
                if (grid.Columns.Contains("NgayTaiLen"))
                    grid.Columns["NgayTaiLen"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }

            DataRow? SelectedAttachment()
                => grid.SelectedRows.Count > 0 && grid.SelectedRows[0].DataBoundItem is DataRowView view
                    ? view.Row
                    : null;

            upload.Click += (sender, args) =>
            {
                using var picker = new OpenFileDialog
                {
                    Title = "Chọn kết quả CLS",
                    Filter = "Tệp kết quả (PDF, JPEG, PNG)|*.pdf;*.jpg;*.jpeg;*.png",
                    CheckFileExists = true,
                    Multiselect = false
                };
                if (picker.ShowDialog(dialog) != DialogResult.OK)
                    return;
                try
                {
                    byte[] content = File.ReadAllBytes(picker.FileName);
                    string contentType = Path.GetExtension(picker.FileName).ToLowerInvariant() switch
                    {
                        ".pdf" => "application/pdf",
                        ".png" => "image/png",
                        ".jpg" or ".jpeg" => "image/jpeg",
                        _ => ""
                    };
                    ClinicDAL.LuuTaiLieuCLS(maChiDinh, Path.GetFileName(picker.FileName), contentType, content);
                    LoadAttachments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể tải tệp CLS",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            download.Click += (sender, args) =>
            {
                DataRow? row = SelectedAttachment();
                if (row == null)
                {
                    MessageBox.Show("Chọn tệp cần tải xuống.", "Chưa chọn tệp",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    var attachment = ClinicDAL.TaiTaiLieuCLS(Convert.ToInt64(row["MaTaiLieu"]));
                    using var picker = new SaveFileDialog
                    {
                        FileName = attachment.tenTep,
                        Filter = "Tệp kết quả (PDF, JPEG, PNG)|*.pdf;*.jpg;*.jpeg;*.png",
                        OverwritePrompt = true
                    };
                    if (picker.ShowDialog(dialog) == DialogResult.OK)
                        File.WriteAllBytes(picker.FileName, attachment.duLieu);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể tải tệp xuống",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            dialog.Controls.Add(grid);
            dialog.Controls.Add(actions);
            dialog.Controls.Add(title);
            dialog.CancelButton = close;
            LoadAttachments();
            dialog.ShowDialog(this);
        }

        private void BuildStep2KeDonThuoc(Panel container, DataRow? drPK, string tienSu)
        {
            var pnlContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // ── CỘT PHẢI: TÌM & THÊM THUỐC ──
            var pnlRightCol = new Panel { Dock = DockStyle.Right, Width = 400, BackColor = Color.Transparent, Padding = new Padding(8, 0, 0, 0) };

            var cardAdd = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16) };
            cardAdd.Paint += PaintCardBorder;

            var lblAddTitle = new Label { Text = "Tìm và thêm thuốc vào đơn", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };

            var txtSearchThuoc = MakeInput(14, 46, cardAdd.Width - 28, false, "");
            txtSearchThuoc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            var lblSearchPlace = new Label { Text = "🔍 Nhập tên thuốc để tìm kiếm...", Location = new Point(22, 52), AutoSize = true, ForeColor = Color.FromArgb(156, 163, 175), BackColor = Color.White };
            lblSearchPlace.Click += (s, e) => txtSearchThuoc.Focus();
            txtSearchThuoc.Enter += (s, e) => lblSearchPlace.Visible = false;
            txtSearchThuoc.Leave += (s, e) => lblSearchPlace.Visible = string.IsNullOrEmpty(txtSearchThuoc.Text);

            var lstThuoc = new ListBox { Location = new Point(14, 82), Size = new Size(cardAdd.Width - 28, 120), Font = F_Base, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var dtAllThuoc = ClinicDAL.LayDanhSachThuoc();

            void FilterThuoc(string kw)
            {
                lstThuoc.Items.Clear();
                foreach (DataRow r in dtAllThuoc.Rows)
                {
                    string ten = r["TenThuoc"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(kw) || ten.ToLower().Contains(kw.ToLower()))
                    {
                        lstThuoc.Items.Add($"{r["MaThuoc"]} | {ten} ({Convert.ToDecimal(r["DonGiaBan"]):N0} đ, còn {r["SoLuongTon"]})");
                    }
                }
                if (lstThuoc.Items.Count > 0) lstThuoc.SelectedIndex = 0;
            }
            FilterThuoc("");
            txtSearchThuoc.TextChanged += (s, e) => FilterThuoc(txtSearchThuoc.Text.Trim());

            // Prescription detail controls
            var lblLieu = MakeLbl("Liều dùng mỗi lần:", 14, 212, bold: true);
            var numLieu = new NumericUpDown { Location = new Point(14, 234), Size = new Size(80, 26), Minimum = 1, Maximum = 10, Value = 1, Font = F_Base };
            var lblLan = MakeLbl("Số lần / ngày:", 110, 212, bold: true);
            var numLan = new NumericUpDown { Location = new Point(110, 234), Size = new Size(80, 26), Minimum = 1, Maximum = 6, Value = 2, Font = F_Base };
            var lblNgay = MakeLbl("Số ngày:", 206, 212, bold: true);
            var numNgay = new NumericUpDown { Location = new Point(206, 234), Size = new Size(80, 26), Minimum = 1, Maximum = 90, Value = 5, Font = F_Base };

            var lblTongSL = new Label { Text = "Tổng: 10 viên", Location = new Point(14, 272), AutoSize = true, Font = F_Bold, ForeColor = C_Teal };
            void UpdateTongSL()
            {
                int total = (int)(numLieu.Value * numLan.Value * numNgay.Value);
                lblTongSL.Text = $"Tổng số lượng kê: {total} đơn vị";
            }
            numLieu.ValueChanged += (s, e) => UpdateTongSL();
            numLan.ValueChanged += (s, e) => UpdateTongSL();
            numNgay.ValueChanged += (s, e) => UpdateTongSL();

            var lblCachDung = MakeLbl("Cách dùng & thời điểm:", 14, 302, bold: true);
            var cboCachDung = new ComboBox { Location = new Point(14, 324), Size = new Size(cardAdd.Width - 28, 28), Font = F_Base, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            cboCachDung.Items.AddRange(new object[] {
                "Uống sau khi ăn sáng và tối",
                "Uống trước bữa ăn 30 phút",
                "Uống 1 viên khi sốt trên 38.5°C, cách 4-6 giờ",
                "Uống vào buổi tối trước khi đi ngủ",
                "Ngậm dưới lưỡi khi đau rát họng"
            });
            cboCachDung.SelectedIndex = 0;

            var btnThemThuoc = PrimaryBtn("➕ THÊM VÀO ĐƠN THUỐC", 14, 370, cardAdd.Width - 28, 40);
            btnThemThuoc.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // ── CỘT TRÁI: DANH SÁCH THUỐC TRONG ĐƠN ──
            var cardDon = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16) };
            cardDon.Paint += PaintCardBorder;

            var lblDonTitle = new Label { Text = "Danh sách thuốc trong đơn", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };

            var dgvDon = MakeGrid(DockStyle.None);
            dgvDon.Location = new Point(14, 46);
            dgvDon.Size = new Size(cardDon.Width - 28, cardDon.Height - 210);
            dgvDon.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            var lblTongTien = new Label { Text = "Tổng tiền thuốc: 0 đ", Location = new Point(14, cardDon.Height - 150), AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left, Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = C_Teal };

            void LoadDonThuoc()
            {
                if (!string.IsNullOrEmpty(_currentDoctorMaPK))
                {
                    var dt = ClinicDAL.LayChiTietDonThuocTheoPK(_currentDoctorMaPK);
                    dgvDon.DataSource = dt;
                    SafeFormatCol(dgvDon, "MaCTDT", "", null, false);
                    SafeFormatCol(dgvDon, "MaDonThuoc", "", null, false);
                    SafeFormatCol(dgvDon, "MaThuoc", "", null, false);
                    SafeFormatCol(dgvDon, "TenThuoc", "Thuốc / Hàm lượng", 180);
                    SafeFormatCol(dgvDon, "DonViTinh", "ĐVT", 60);
                    SafeFormatCol(dgvDon, "SoLuong", "Số lượng", 70);
                    SafeFormatCol(dgvDon, "DonGiaThoiDiem", "Đơn giá", 90);
                    var priceColumn = dgvDon.Columns["DonGiaThoiDiem"];
                    if (priceColumn != null)
                        priceColumn.DefaultCellStyle.Format = "N0";
                    SafeFormatCol(dgvDon, "ThanhTien", "Thành tiền", 100);
                    var totalColumn = dgvDon.Columns["ThanhTien"];
                    if (totalColumn != null)
                        totalColumn.DefaultCellStyle.Format = "N0";
                    SafeFormatCol(dgvDon, "CachDung", "Chỉ định / Liều dùng", 200);

                    decimal tong = 0;
                    foreach (DataRow r in dt.Rows)
                    {
                        tong += Convert.ToDecimal(r["ThanhTien"]);
                    }
                    lblTongTien.Text = $"Tổng tiền thuốc: {tong:N0} đ";
                }
            }
            LoadDonThuoc();

            btnThemThuoc.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(_currentDoctorMaPK)) return;
                if (lstThuoc.SelectedItem == null)
                {
                    MessageBox.Show("Vui lòng chọn một loại thuốc từ danh sách!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string line = lstThuoc.SelectedItem.ToString()!;
                string maThuoc = line.Split('|')[0].Trim();

                DataRow? rThuoc = null;
                foreach (DataRow r in dtAllThuoc.Rows)
                {
                    if (r["MaThuoc"]?.ToString() == maThuoc) { rThuoc = r; break; }
                }
                if (rThuoc == null) return;
                if (!string.IsNullOrWhiteSpace(tienSu) &&
                    MessageBox.Show(
                        $"Bệnh nhân có tiền sử / dị ứng đã ghi nhận:\r\n{tienSu}\r\n\r\nDanh mục thuốc hiện chưa có dữ liệu hoạt chất để tự xác định tương tác. Bác sĩ cần kiểm tra an toàn trước khi kê. Tiếp tục với thuốc {rThuoc["TenThuoc"]}?",
                        "Cảnh báo an toàn thuốc",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                int sl = (int)(numLieu.Value * numLan.Value * numNgay.Value);
                decimal donGia = Convert.ToDecimal(rThuoc["DonGiaBan"]);
                string cdung = $"Uống {numLieu.Value}v x {numLan.Value} lần/ngày ({numNgay.Value} ngày). {cboCachDung.Text.Trim()}";

                var listItems = new List<ChiTietDonThuoc>
                {
                    new ChiTietDonThuoc
                    {
                        MaThuoc        = maThuoc,
                        TenThuoc       = rThuoc["TenThuoc"]?.ToString() ?? "",
                        SoLuong        = sl,
                        DonGiaThoiDiem = donGia,
                        CachDung       = cdung
                    }
                };

                if (ClinicDAL.KeDonThuoc(_currentDoctorMaPK, "", listItems, out string err))
                {
                    MessageBox.Show($"✅ Đã thêm thuốc: {rThuoc["TenThuoc"]} (SL: {sl}) vào đơn!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadDonThuoc();
                }
                else
                {
                    MessageBox.Show("❌ " + err, "Không thể kê thuốc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            // Lời dặn dùng thuốc
            var lblLoiDan = MakeLbl("Lời dặn / Ghi chú dùng thuốc:", 14, cardDon.Height - 120, bold: true);
            lblLoiDan.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            var txtLoiDan = new TextBox { Location = new Point(14, cardDon.Height - 98), Size = new Size(cardDon.Width - 28, 40), Multiline = true, Font = F_Base, Text = ClinicDAL.LayGhiChuDonThuoc(_currentDoctorMaPK ?? ""), Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            // Buttons
            var btnDeleteThuoc = SecondaryBtn("🗑 Xóa thuốc", 14, cardDon.Height - 48, 110, 34, C_Red);
            btnDeleteThuoc.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnDeleteThuoc.Click += (s, e) =>
            {
                if (dgvDon.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn thuốc cần xóa khỏi đơn!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                int maCTDT = Convert.ToInt32(dgvDon.SelectedRows[0].Cells["MaCTDT"].Value);
                string maT = dgvDon.SelectedRows[0].Cells["MaThuoc"].Value?.ToString() ?? "";
                int sl     = Convert.ToInt32(dgvDon.SelectedRows[0].Cells["SoLuong"].Value);
                if (MessageBox.Show("Xóa thuốc này khỏi đơn và hoàn trả tồn kho?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (ClinicDAL.XoaThuocKhoiDon(maCTDT, maT, sl, out string err))
                    {
                        LoadDonThuoc();
                    }
                    else MessageBox.Show("Lỗi: " + err, "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnPrintDon = SecondaryBtn("🖨 In / Lưu PDF Đơn thuốc", 134, cardDon.Height - 48, 190, 34, C_Blue);
            btnPrintDon.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnPrintDon.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(_currentDoctorMaPK))
                {
                    MessageBox.Show("Chưa chọn phiếu khám nào!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var dtThuoc = ClinicDAL.LayChiTietDonThuocTheoPK(_currentDoctorMaPK);
                if (dtThuoc.Rows.Count == 0)
                {
                    MessageBox.Show("Đơn thuốc hiện đang trống, vui lòng thêm thuốc trước khi in!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string tenBN = drPK?["TenBN"]?.ToString() ?? "Bệnh nhân";
                string gioiTinh = drPK?["GioiTinh"]?.ToString() ?? "";
                string tuoi = drPK?["Tuoi"]?.ToString() ?? "";
                string chanDoan = drPK?["ChanDoan"]?.ToString() ?? "Khám bệnh";
                string loiDan = txtLoiDan.Text.Trim();
                string tenBS = SessionState.CurrentUser?.HoTenNhanVien ?? "Bác sĩ điều trị";

                PdfPrintHelper.InDonThuoc(_currentDoctorMaPK, tenBN, gioiTinh, tuoi, chanDoan, loiDan, tenBS, dtThuoc);
            };

            var btnNextStep = PrimaryBtn("Tiếp theo: Kết quả & Hoàn tất  ➔", cardDon.Width - 270, cardDon.Height - 48, 250, 34);
            btnNextStep.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnNextStep.Click += (s, e) =>
            {
                try
                {
                    if (!string.IsNullOrEmpty(_currentDoctorMaPK) &&
                        ClinicDAL.LayChiTietDonThuocTheoPK(_currentDoctorMaPK).Rows.Count > 0 &&
                        !ClinicDAL.CapNhatGhiChuDonThuoc(_currentDoctorMaPK, txtLoiDan.Text.Trim()))
                    {
                        MessageBox.Show("Không thể lưu lời dặn của đơn thuốc.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    ShowBacSiKhamKeDonView(_currentDoctorMaPK, 2);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi lưu lời dặn đơn thuốc: {ex.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            cardDon.Controls.AddRange(new Control[] { lblDonTitle, dgvDon, lblTongTien, lblLoiDan, txtLoiDan, btnDeleteThuoc, btnPrintDon, btnNextStep });

            cardAdd.Controls.AddRange(new Control[] {
                lblAddTitle, txtSearchThuoc, lblSearchPlace, lstThuoc,
                lblLieu, numLieu, lblLan, numLan, lblNgay, numNgay, lblTongSL,
                lblCachDung, cboCachDung, btnThemThuoc
            });
            pnlRightCol.Controls.Add(cardAdd);

            pnlContainer.Controls.Add(cardDon);
            pnlContainer.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlContainer.Controls.Add(pnlRightCol);
            ConfigureDoctorRightColumn(pnlContainer, pnlRightCol);
            container.Controls.Add(pnlContainer);
        }

        // ───────────────────────────────────────────────────────────
        // BƯỚC 3: KẾT QUẢ & HOÀN TẤT KHÁM, CHUYỂN THANH TOÁN
        // ───────────────────────────────────────────────────────────
        private void BuildStep3KetQuaHoanTat(Panel container, DataRow? drPK)
        {
            var pnlContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            // ── CỘT PHẢI: BẢNG KÊ CHI PHÍ & CHUYỂN THANH TOÁN ──
            var pnlRightCol = new Panel { Dock = DockStyle.Right, Width = 400, BackColor = Color.Transparent, Padding = new Padding(8, 0, 0, 0) };

            var cardPay = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16) };
            cardPay.Paint += PaintCardBorder;

            var lblPayTitle = new Label { Text = "Bảng kê chi phí chuyển quầy thu ngân", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };

            var pnlItems = new Panel { Location = new Point(14, 46), Size = new Size(cardPay.Width - 28, 160), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(248, 250, 252), Padding = new Padding(12) };
            pnlItems.Paint += PaintCardBorder;

            var (tienDV, tienThuoc) = !string.IsNullOrEmpty(_currentDoctorMaPK) ? ClinicDAL.TinhChiPhiPhieuKham(_currentDoctorMaPK) : (0, 0);
            decimal tongTien = tienDV + tienThuoc;

            var lblI1 = new Label { Text = $"Dịch vụ cận lâm sàng:  {tienDV:N0} đ", Location = new Point(10, 18), AutoSize = true, Font = F_Base, ForeColor = C_TextDark };
            var lblI2 = new Label { Text = $"Thuốc theo đơn:  {tienThuoc:N0} đ", Location = new Point(10, 52), AutoSize = true, Font = F_Base, ForeColor = C_TextDark };
            var lblI3 = new Label { Text = $"TỔNG CHI PHÍ ĐÃ GHI NHẬN:  {tongTien:N0} đ", Location = new Point(10, 102), AutoSize = true, Font = new Font("Segoe UI", 12.5f, FontStyle.Bold), ForeColor = C_Teal };

            pnlItems.Controls.AddRange(new Control[] { lblI1, lblI2, lblI3 });

            var pnlCheck = new Panel { Location = new Point(14, 218), Size = new Size(cardPay.Width - 28, 90), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(240, 253, 250), Padding = new Padding(10) };
            pnlCheck.Controls.Add(new Label { Text = "Kiểm tra hoàn tất:\n☑ Phiếu khám lâm sàng đã ghi nhận chẩn đoán\n☑ Đã chỉ định CLS & cập nhật kết quả\n☑ Đã hoàn tất kê đơn thuốc và lời dặn", Dock = DockStyle.Fill, Font = F_Small, ForeColor = Color.FromArgb(15, 118, 110) });

            // Action: Gửi bảng kê cho thu ngân
            var btnSendCashier = PrimaryBtn("🚀 GỬI BẢNG KÊ CHO THU NGÂN", 14, 324, cardPay.Width - 28, 42);
            btnSendCashier.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            btnSendCashier.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(_currentDoctorMaPK)) return;
                if (string.IsNullOrWhiteSpace(drPK?["ChanDoan"]?.ToString()))
                {
                    MessageBox.Show("Cần lưu chẩn đoán trước khi hoàn tất ca khám.", "Thiếu chẩn đoán", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    if (!ClinicDAL.CapNhatTrangThaiPhieuKham(_currentDoctorMaPK, SessionState.CurrentUser?.MaNV ?? "", "Đã khám xong"))
                    {
                        MessageBox.Show("Không tìm thấy phiếu khám để hoàn tất.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    if (MessageBox.Show("Đã hoàn tất ca khám. Bạn có muốn tạo lịch tái khám nháp cho bệnh nhân không?",
                        "Tạo lịch tái khám", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        using var followUpForm = new Form
                        {
                            Text = "Chọn thời gian tái khám",
                            StartPosition = FormStartPosition.CenterParent,
                            FormBorderStyle = FormBorderStyle.FixedDialog,
                            ClientSize = new Size(340, 125),
                            MaximizeBox = false,
                            MinimizeBox = false
                        };
                        var days = new ComboBox
                        {
                            DropDownStyle = ComboBoxStyle.DropDownList,
                            Location = new Point(18, 30),
                            Size = new Size(290, 30)
                        };
                        days.Items.AddRange(new object[] { "Sau 7 ngày", "Sau 14 ngày", "Sau 30 ngày" });
                        days.SelectedIndex = 0;
                        var confirm = new Button
                        {
                            Text = "Tạo lịch",
                            DialogResult = DialogResult.OK,
                            Location = new Point(208, 76),
                            Size = new Size(100, 30)
                        };
                        followUpForm.Controls.AddRange(new Control[] { days, confirm });
                        followUpForm.AcceptButton = confirm;
                        if (followUpForm.ShowDialog(this) == DialogResult.OK)
                        {
                            try
                            {
                                int interval = days.SelectedIndex switch { 1 => 14, 2 => 30, _ => 7 };
                                DateTime followUp = ClinicDAL.TaoLichTaiKham(_currentDoctorMaPK, interval);
                                MessageBox.Show($"Đã tạo lịch nháp {followUp:dd/MM/yyyy HH:mm}. Lễ tân cần xác nhận lịch.",
                                    "Đã tạo lịch tái khám", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Ca khám đã hoàn tất nhưng chưa tạo được lịch tái khám: {ex.Message}",
                                    "Không tạo được lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Đã hoàn tất ca khám và chuyển phiếu sang quầy thu ngân.", "Hoàn tất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    ShowBacSiLichKhamView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi hoàn tất ca khám: {ex.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnPrintSummary = SecondaryBtn("🖨 In tóm tắt ca khám", 14, 376, cardPay.Width - 28, 34, C_Blue);
            btnPrintSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnPrintSummary.Click += (s, e) =>
            {
                if (drPK == null) return;
                using var documentForm = new Form
                {
                    Text = "In giấy tờ y tế",
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    ClientSize = new Size(480, 300),
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                var documentType = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(18, 34),
                    Size = new Size(438, 30)
                };
                documentType.Items.AddRange(new object[]
                {
                    "Tóm tắt ca khám",
                    "Giấy khám sức khỏe",
                    "Giấy chuyển tuyến",
                    "Giấy nghỉ ốm"
                });
                documentType.SelectedIndex = 0;
                var content = new TextBox
                {
                    Location = new Point(18, 92),
                    Size = new Size(438, 142),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };
                content.Text = documentType.SelectedIndex == 0
                    ? $"Phiếu khám: {_currentDoctorMaPK}\r\nNgày khám: {Convert.ToDateTime(drPK["NgayKham"]):dd/MM/yyyy HH:mm}\r\nTriệu chứng: {drPK["TrieuChung"]}\r\nKhám lâm sàng: {drPK["KhamLamSang"]}\r\nSinh hiệu: {drPK["SinhHieu"]}\r\nLời dặn: {drPK["LoiDan"]}"
                    : "";
                documentType.SelectedIndexChanged += (sender, args) =>
                {
                    content.Text = documentType.SelectedIndex == 0
                        ? $"Phiếu khám: {_currentDoctorMaPK}\r\nNgày khám: {Convert.ToDateTime(drPK["NgayKham"]):dd/MM/yyyy HH:mm}\r\nTriệu chứng: {drPK["TrieuChung"]}\r\nKhám lâm sàng: {drPK["KhamLamSang"]}\r\nSinh hiệu: {drPK["SinhHieu"]}\r\nLời dặn: {drPK["LoiDan"]}"
                        : "";
                };
                var print = new Button
                {
                    Text = "Xem trước / In / Lưu PDF",
                    Location = new Point(286, 250),
                    Size = new Size(170, 32),
                    DialogResult = DialogResult.OK
                };
                documentForm.Controls.AddRange(new Control[]
                {
                    new Label { Text = "Loại giấy tờ", Location = new Point(18, 12), AutoSize = true },
                    documentType,
                    new Label { Text = "Nội dung bổ sung / xác nhận", Location = new Point(18, 70), AutoSize = true },
                    content,
                    print
                });
                documentForm.AcceptButton = print;
                if (documentForm.ShowDialog(this) != DialogResult.OK) return;
                if (string.IsNullOrWhiteSpace(content.Text))
                {
                    MessageBox.Show("Vui lòng nhập nội dung giấy tờ trước khi in.", "Thiếu nội dung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string docTitle = documentType.SelectedItem?.ToString() ?? "Giấy y tế";
                string dob = drPK["NgaySinh"] == DBNull.Value
                    ? "Chưa cập nhật"
                    : Convert.ToDateTime(drPK["NgaySinh"]).ToString("dd/MM/yyyy");
                PdfPrintHelper.InGiayYTe(
                    docTitle,
                    drPK["MaBN"].ToString() ?? "",
                    drPK["TenBN"].ToString() ?? "",
                    dob,
                    drPK["ChanDoan"].ToString() ?? "",
                    content.Text.Trim(),
                    SessionState.CurrentUser?.HoTenNhanVien ?? "Bác sĩ điều trị");
            };

            cardPay.Controls.AddRange(new Control[] { lblPayTitle, pnlItems, pnlCheck, btnSendCashier, btnPrintSummary });
            pnlRightCol.Controls.Add(cardPay);

            // ── CỘT TRÁI: KẾT QUẢ XÉT NGHIỆM & TÓM TẮT ĐIỀU TRỊ ──
            var cardKQ = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16), AutoScroll = true };
            cardKQ.Paint += PaintCardBorder;

            var lblKQTitle = new Label { Text = "Dịch vụ cận lâm sàng đã chỉ định cho ca khám", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };
            var dgvCLS = MakeGrid(DockStyle.None);
            dgvCLS.Location = new Point(14, 46);
            dgvCLS.Size = new Size(cardKQ.Width - 28, 140);
            dgvCLS.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvCLS.DataSource = string.IsNullOrEmpty(_currentDoctorMaPK)
                ? new DataTable()
                : ClinicDAL.LayChiDinhDVTheoPhieuKham(_currentDoctorMaPK);
            SafeFormatCol(dgvCLS, "TenDV", "Dịch vụ", 180);
            SafeFormatCol(dgvCLS, "TrangThai", "Trạng thái", 110);
            SafeFormatCol(dgvCLS, "KetQua", "Kết quả", 180);

            var lblSummaryTitle = new Label { Text = "Tóm tắt khám & Hướng dẫn theo dõi", Location = new Point(14, 200), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };

            var pnlSumBox = new Panel { Location = new Point(14, 230), Size = new Size(cardKQ.Width - 28, 160), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.FromArgb(240, 253, 250), Padding = new Padding(12) };
            pnlSumBox.Paint += PaintCardBorder;

            var lblS1 = new Label { Text = "Thông tin được lấy từ phiếu khám hiện tại.", Location = new Point(12, 10), AutoSize = true, Font = F_Bold, ForeColor = C_TealDark };
            var diagnosis = drPK?["ChanDoan"] == DBNull.Value ? "Chưa ghi nhận" : drPK?["ChanDoan"]?.ToString() ?? "Chưa ghi nhận";
            var advice = drPK?["LoiDan"] == DBNull.Value ? "Chưa ghi nhận" : drPK?["LoiDan"]?.ToString() ?? "Chưa ghi nhận";
            var lblS2 = new Label { Text = $"Chẩn đoán: {diagnosis}\nLời dặn: {advice}", Location = new Point(12, 36), Size = new Size(pnlSumBox.Width - 24, 72), Font = F_Base, ForeColor = C_TextDark };

            pnlSumBox.Controls.AddRange(new Control[] { lblS1, lblS2 });

            cardKQ.Controls.AddRange(new Control[] { lblKQTitle, dgvCLS, lblSummaryTitle, pnlSumBox });

            pnlContainer.Controls.Add(cardKQ);
            pnlContainer.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlContainer.Controls.Add(pnlRightCol);
            ConfigureDoctorRightColumn(pnlContainer, pnlRightCol);
            container.Controls.Add(pnlContainer);
        }

        // ═══════════════════════════════════════════════════════════
        //  VIEW 4: BÁC SĨ - TRA CỨU CHUYÊN MÔN
        // ═══════════════════════════════════════════════════════════
        public void ShowBacSiTraCuuView()
        {
            SetRefreshPageAction(ShowBacSiTraCuuView);
            SetActiveNav(btnNavKhoThuoc);
            SetPageHeader("Bác sĩ  ›  Tra cứu", "Tra cứu chuyên môn (Thuốc, Dịch vụ, Bệnh án & Phác đồ)");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            // ── TOP SEARCH BAR ──
            var pnlSearch = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = C_Card, Padding = new Padding(16, 12, 16, 12) };
            pnlSearch.Paint += PaintCardBorder;

            var txtKw = MakeInput(14, 12, 420, false, "");
            var lblKwPlace = new Label { Text = "🔍 Nhập từ khóa tìm kiếm (tên thuốc, dịch vụ, bệnh nhân, phác đồ)...", Location = new Point(22, 18), AutoSize = true, ForeColor = Color.FromArgb(156, 163, 175), BackColor = Color.White };
            lblKwPlace.Click += (s, e) => txtKw.Focus();
            txtKw.Enter += (s, e) => lblKwPlace.Visible = false;
            txtKw.Leave += (s, e) => lblKwPlace.Visible = string.IsNullOrEmpty(txtKw.Text);

            var cboSource = new ComboBox { Location = new Point(445, 12), Size = new Size(150, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = F_Base };
            cboSource.Items.AddRange(new object[] { "Tất cả nguồn", "15 ngày gần đây", "Nội tổng quát", "Dược lâm sàng" });
            cboSource.SelectedIndex = 0;

            var btnSearch = PrimaryBtn("🔍 Tìm kiếm", 605, 11, 110, 31);

            // Tab bar
            string currentTab = "TatCa";
            var btnTabAll     = SecondaryBtn("Tất cả", 14, 52, 90, 32, C_Teal);
            var btnTabThuoc   = SecondaryBtn("Thuốc", 110, 52, 90, 32, Color.FromArgb(107, 114, 128));
            var btnTabDV      = SecondaryBtn("Dịch vụ CLS", 206, 52, 110, 32, Color.FromArgb(107, 114, 128));
            var btnTabBA      = SecondaryBtn("Bệnh án", 322, 52, 100, 32, Color.FromArgb(107, 114, 128));
            var btnTabDoc     = SecondaryBtn("Tài liệu & Phác đồ", 428, 52, 150, 32, Color.FromArgb(107, 114, 128));

            pnlSearch.Controls.AddRange(new Control[] {
                txtKw, lblKwPlace, cboSource, btnSearch,
                btnTabAll, btnTabThuoc, btnTabDV, btnTabBA, btnTabDoc
            });

            // ── MAIN BODY (TWO COLUMNS) ──
            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), BackColor = Color.Transparent };

            // Right: Preview card
            var pnlRight = new Panel { Dock = DockStyle.Right, Width = 400, BackColor = C_Card, Padding = new Padding(16) };
            pnlRight.Paint += PaintCardBorder;

            var lblPreTitle = new Label { Text = "Xem trước tài liệu / Chi tiết thông tin", Location = new Point(14, 12), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };
            var badgePerm = new Label { Text = "  Tài liệu được phép truy cập  ", Location = new Point(pnlRight.Width - 190, 10), Size = new Size(175, 24), Anchor = AnchorStyles.Top | AnchorStyles.Right, BackColor = Color.FromArgb(220, 252, 231), ForeColor = Color.FromArgb(22, 101, 52), Font = F_Small, TextAlign = ContentAlignment.MiddleCenter };

            var txtPreDetails = new TextBox { Location = new Point(14, 46), Size = new Size(pnlRight.Width - 28, pnlRight.Height - 110), Multiline = true, ReadOnly = true, Font = F_Base, BackColor = Color.FromArgb(248, 250, 252), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            var btnPreAction = PrimaryBtn("📄 Tải xuống / Mở tài liệu", 14, pnlRight.Height - 48, pnlRight.Width - 28, 34);
            btnPreAction.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnPreAction.Click += (s, e) => MessageBox.Show("Đã mở tài liệu chi tiết trong trình xem y khoa an toàn!", "Mở tài liệu", MessageBoxButtons.OK, MessageBoxIcon.Information);

            pnlRight.Controls.AddRange(new Control[] { lblPreTitle, badgePerm, txtPreDetails, btnPreAction });

            // Left: Grid kết quả
            var pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16) };
            pnlLeft.Paint += PaintCardBorder;

            var lblResCount = new Label { Text = "Kết quả tra cứu:", Location = new Point(14, 12), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };

            var dgvRes = MakeGrid(DockStyle.None);
            dgvRes.Location = new Point(14, 40);
            dgvRes.Size = new Size(pnlLeft.Width - 28, pnlLeft.Height - 54);
            dgvRes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvRes.DataBindingComplete += (sender, args) =>
            {
                SafeFormatCol(dgvRes, "Loai", "Phân loại", 120);
                SafeFormatCol(dgvRes, "Ma", "Mã", 80);
                SafeFormatCol(dgvRes, "TieuDe", "Tiêu đề / Đối tượng", 200);
                SafeFormatCol(dgvRes, "MoTa", "Mô tả sơ lược", 200);
                SafeFormatCol(dgvRes, "ChiTiet", "", null, false);
            };

            GridPageController? lookupPageController = null;
            DataTable LoadLookupPage(int offset, int pageSize)
            {
                DataTable results = ClinicDAL.TraCuuChuyenMonPaged(
                    txtKw.Text.Trim(),
                    currentTab,
                    offset,
                    pageSize);
                int count = results.ExtendedProperties["TotalCount"] is int totalCount
                    ? totalCount
                    : results.Rows.Count;
                lblResCount.Text = $"Kết quả tra cứu: {count} mục phù hợp";
                return results;
            }
            void LoadTraCuu() => lookupPageController?.Reload();

            void SetActiveTabBtn(Button activeBtn, string tabKey)
            {
                currentTab = tabKey;
                foreach (var b in new[] { btnTabAll, btnTabThuoc, btnTabDV, btnTabBA, btnTabDoc })
                {
                    b.BackColor = Color.FromArgb(107, 114, 128);
                }
                activeBtn.BackColor = C_Teal;
                LoadTraCuu();
            }

            btnTabAll.Click   += (s, e) => SetActiveTabBtn(btnTabAll, "TatCa");
            btnTabThuoc.Click += (s, e) => SetActiveTabBtn(btnTabThuoc, "Thuoc");
            btnTabDV.Click    += (s, e) => SetActiveTabBtn(btnTabDV, "DichVu");
            btnTabBA.Click    += (s, e) => SetActiveTabBtn(btnTabBA, "BenhAn");
            btnTabDoc.Click   += (s, e) => SetActiveTabBtn(btnTabDoc, "TaiLieu");

            txtKw.TextChanged += (s, e) => LoadTraCuu();
            btnSearch.Click   += (s, e) => LoadTraCuu();

            dgvRes.SelectionChanged += (s, e) =>
            {
                if (dgvRes.SelectedRows.Count == 0) return;
                var r = dgvRes.SelectedRows[0];
                string loai   = r.Cells["Loai"].Value?.ToString() ?? "";
                string ma     = r.Cells["Ma"].Value?.ToString() ?? "";
                string tieuDe = r.Cells["TieuDe"].Value?.ToString() ?? "";
                string moTa   = r.Cells["MoTa"].Value?.ToString() ?? "";
                string chiTiet= r.Cells["ChiTiet"].Value?.ToString() ?? "";

                txtPreDetails.Text = $"=== CHI TIẾT ĐỐI TƯỢNG TRA CỨU ===\r\n\r\n" +
                                     $"Phân loại: {loai}\r\n" +
                                     $"Mã tham chiếu: {ma}\r\n" +
                                     $"Tiêu đề: {tieuDe}\r\n\r\n" +
                                     $"Mô tả: {moTa}\r\n\r\n" +
                                     $"Chi tiết chuyên môn:\r\n{chiTiet}\r\n\r\n" +
                                     $"Thời gian truy xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\r\n" +
                                     $"Người tra cứu: {SessionState.CurrentUser?.HoTenNhanVien ?? "Bác sĩ"}";
            };

            pnlLeft.Controls.AddRange(new Control[] { lblResCount, dgvRes });

            pnlBody.Controls.Add(pnlLeft);
            pnlBody.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlBody.Controls.Add(pnlRight);
            ConfigureDoctorRightColumn(pnlBody, pnlRight);

            wrapper.Controls.AddRange(new Control[] { pnlBody, pnlSearch });
            panelContent.Controls.Add(wrapper);

            lookupPageController = GridPageController.Attach(dgvRes, LoadLookupPage);
        }

        // Tương thích ngược: nếu có nút cũ gọi ShowKhamBenhView()
        private void ShowKhamBenhView() => ShowBacSiKhamKeDonView();

        // ═══════════════════════════════════════════════════════════
        //  VIEW 5: BÁC SĨ - DANH MỤC THUỐC & DƯỢC THƯ (READ-ONLY)
        // ═══════════════════════════════════════════════════════════
        public void ShowBacSiThuocView()
        {
            SetRefreshPageAction(ShowBacSiThuocView);
            SetActiveNav(btnNavKhoThuoc);
            SetPageHeader("Bác sĩ  ›  Danh mục thuốc", "Tra cứu dược thư, hoạt chất, quy cách & tồn kho thực tế phục vụ kê đơn");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            // ── TOP TOOLBAR ──
            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = C_Card, Padding = new Padding(14, 10, 14, 10) };
            toolPanel.Paint += PaintCardBorder;

            var lblSearch = MakeLbl("Tìm thuốc:", 14, 17);
            var txtSearch = MakeInput(88, 12, 230);
            txtSearch.PlaceholderText = "Tên biệt dược, mã thuốc...";

            var lblStatus = MakeLbl("Tồn kho:", 330, 17);
            var cboStatus = new ComboBox
            {
                Location = new Point(395, 12),
                Size = new Size(155, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            cboStatus.Items.AddRange(new object[] { "Tất cả trạng thái", "Còn hàng (> 30)", "Sắp hết (1 - 30)", "Hết hàng (= 0)" });
            cboStatus.SelectedIndex = 0;

            var btnSearch = PrimaryBtn("🔍 Tìm kiếm", 562, 11, 100, 33);
            var btnRefresh = SecondaryBtn("↻ Làm mới", 670, 11, 95, 33, C_Blue);

            // KPI badges
            var pnlKpiBadges = new Panel { Location = new Point(780, 10), Size = new Size(Math.Max(200, toolPanel.ClientSize.Width - 790), 36), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlKpiBadges.Paint += (s, e) =>
            {
                var k = ClinicDAL.LayThongKeKhoThuoc();
                using var f = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var brushText = new SolidBrush(Color.FromArgb(55, 65, 81));
                using var brushGreen = new SolidBrush(Color.FromArgb(16, 185, 129));
                using var brushOrange = new SolidBrush(Color.FromArgb(245, 158, 11));
                using var brushRed = new SolidBrush(Color.FromArgb(239, 68, 68));

                int x = 8;
                e.Graphics.DrawString($"Tổng: {k.tongLoaiThuoc} loại", f, brushText, x, 8);
                x += 105;
                e.Graphics.DrawString($"🟢 Còn: {k.anToanCount}", f, brushGreen, x, 8);
                x += 95;
                e.Graphics.DrawString($"🟡 Sắp hết: {k.thuocSapHet}", f, brushOrange, x, 8);
                x += 105;
                e.Graphics.DrawString($"🔴 Hết: {k.hetHangCount}", f, brushRed, x, 8);
            };

            toolPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch, lblStatus, cboStatus, btnSearch, btnRefresh, pnlKpiBadges });

            // ── BODY ──
            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), BackColor = Color.Transparent };

            // Right detail card
            var pnlRight = new Panel { Dock = DockStyle.Right, Width = 360, BackColor = C_Card, Padding = new Padding(14) };
            pnlRight.Paint += PaintCardBorder;

            var lblDetailHeader = MakeLbl("💊 THÔNG TIN BIỆT DƯỢC", 14, 12, bold: true);
            lblDetailHeader.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);

            var lblDrugName = new Label { Location = new Point(14, 42), Size = new Size(pnlRight.Width - 28, 48), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = C_Teal, Text = "Chọn thuốc để xem chi tiết", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var lblStockBadge = new Label { Location = new Point(14, 94), Size = new Size(160, 26), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Text = "Tồn kho: --", BackColor = Color.FromArgb(243, 244, 246), TextAlign = ContentAlignment.MiddleCenter };
            var lblPriceBadge = new Label { Location = new Point(184, 94), Size = new Size(160, 26), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), Text = "Đơn giá: --", BackColor = Color.FromArgb(243, 244, 246), TextAlign = ContentAlignment.MiddleCenter };

            var lblInstructTitle = MakeLbl("Hướng dẫn & Liều dùng khuyến nghị:", 14, 134, bold: true);
            var txtInstruct = new TextBox { Location = new Point(14, 160), Size = new Size(pnlRight.Width - 28, pnlRight.Height - 175), Multiline = true, ReadOnly = true, Font = F_Base, BackColor = Color.FromArgb(248, 250, 252), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            pnlRight.Controls.AddRange(new Control[] { lblDetailHeader, lblDrugName, lblStockBadge, lblPriceBadge, lblInstructTitle, txtInstruct });

            // Left grid
            var pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            pnlLeft.Paint += PaintCardBorder;
            var dgvThuoc = MakeGrid();
            dgvThuoc.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvThuoc.DataBindingComplete += (s, e) =>
            {
                if (dgvThuoc.Columns.Contains("MaThuoc"))
                {
                    dgvThuoc.Columns["MaThuoc"]!.HeaderText = "Mã";
                    dgvThuoc.Columns["MaThuoc"]!.FillWeight = 0.8f;
                    dgvThuoc.Columns["MaThuoc"]!.MinimumWidth = 65;
                }
                if (dgvThuoc.Columns.Contains("TenThuoc"))
                {
                    dgvThuoc.Columns["TenThuoc"]!.HeaderText = "Tên thuốc / Biệt dược";
                    dgvThuoc.Columns["TenThuoc"]!.FillWeight = 2.2f;
                    dgvThuoc.Columns["TenThuoc"]!.MinimumWidth = 140;
                }
                if (dgvThuoc.Columns.Contains("DonViTinh"))
                {
                    dgvThuoc.Columns["DonViTinh"]!.HeaderText = "ĐVT";
                    dgvThuoc.Columns["DonViTinh"]!.FillWeight = 0.8f;
                    dgvThuoc.Columns["DonViTinh"]!.MinimumWidth = 60;
                }
                if (dgvThuoc.Columns.Contains("DonGiaBan"))
                {
                    dgvThuoc.Columns["DonGiaBan"]!.HeaderText = "Đơn giá kê đơn";
                    dgvThuoc.Columns["DonGiaBan"]!.FillWeight = 1.2f;
                    dgvThuoc.Columns["DonGiaBan"]!.MinimumWidth = 95;
                    dgvThuoc.Columns["DonGiaBan"]!.DefaultCellStyle.Format = "N0 đ";
                    dgvThuoc.Columns["DonGiaBan"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (dgvThuoc.Columns.Contains("SoLuongTon"))
                {
                    dgvThuoc.Columns["SoLuongTon"]!.HeaderText = "Tồn kho";
                    dgvThuoc.Columns["SoLuongTon"]!.FillWeight = 1.0f;
                    dgvThuoc.Columns["SoLuongTon"]!.MinimumWidth = 70;
                    dgvThuoc.Columns["SoLuongTon"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
                if (dgvThuoc.Columns.Contains("CachDungMacDinh"))
                {
                    dgvThuoc.Columns["CachDungMacDinh"]!.HeaderText = "Cách dùng mặc định";
                    dgvThuoc.Columns["CachDungMacDinh"]!.FillWeight = 2.4f;
                    dgvThuoc.Columns["CachDungMacDinh"]!.MinimumWidth = 160;
                }
            };

            dgvThuoc.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && dgvThuoc.Columns[e.ColumnIndex].Name == "SoLuongTon" && e.Value != null)
                {
                    if (int.TryParse(e.Value.ToString(), out int sl))
                    {
                        if (sl == 0)
                        {
                            e.CellStyle.ForeColor = Color.FromArgb(220, 38, 38);
                            e.CellStyle.Font = new Font(F_Base, FontStyle.Bold);
                        }
                        else if (sl <= 30)
                        {
                            e.CellStyle.ForeColor = Color.FromArgb(217, 119, 6);
                            e.CellStyle.Font = new Font(F_Base, FontStyle.Bold);
                        }
                        else
                        {
                            e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                        }
                    }
                }
            };

            pnlLeft.Controls.Add(dgvThuoc);

            pnlBody.Controls.Add(pnlLeft);
            pnlBody.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlBody.Controls.Add(pnlRight);
            ConfigureDoctorRightColumn(pnlBody, pnlRight);

            void UpdateDetail(DataRowView? drv)
            {
                if (drv == null)
                {
                    lblDrugName.Text = "Chọn thuốc để xem chi tiết";
                    lblStockBadge.Text = "Tồn kho: --";
                    lblPriceBadge.Text = "Đơn giá: --";
                    txtInstruct.Text = "";
                    return;
                }
                var r = drv.Row;
                string ma = r["MaThuoc"]?.ToString() ?? "";
                string ten = r["TenThuoc"]?.ToString() ?? "";
                string dvt = r["DonViTinh"]?.ToString() ?? "";
                decimal gia = r.Table.Columns.Contains("DonGiaBan") && r["DonGiaBan"] != DBNull.Value ? Convert.ToDecimal(r["DonGiaBan"]) : 0;
                int ton = r.Table.Columns.Contains("SoLuongTon") && r["SoLuongTon"] != DBNull.Value ? Convert.ToInt32(r["SoLuongTon"]) : 0;
                string cachDung = r.Table.Columns.Contains("CachDungMacDinh") ? r["CachDungMacDinh"]?.ToString() ?? "" : "";

                lblDrugName.Text = $"{ten} [{ma}]";
                lblPriceBadge.Text = $"Đơn giá: {gia:N0} đ / {dvt}";

                if (ton == 0)
                {
                    lblStockBadge.Text = $"🔴 HẾT HÀNG (0 {dvt})";
                    lblStockBadge.BackColor = Color.FromArgb(254, 226, 226);
                    lblStockBadge.ForeColor = Color.FromArgb(185, 28, 28);
                }
                else if (ton <= 30)
                {
                    lblStockBadge.Text = $"🟡 SẮP HẾT ({ton} {dvt})";
                    lblStockBadge.BackColor = Color.FromArgb(254, 243, 199);
                    lblStockBadge.ForeColor = Color.FromArgb(180, 83, 9);
                }
                else
                {
                    lblStockBadge.Text = $"🟢 KHẢ DỤNG ({ton:N0} {dvt})";
                    lblStockBadge.BackColor = Color.FromArgb(220, 252, 231);
                    lblStockBadge.ForeColor = Color.FromArgb(21, 128, 61);
                }

                txtInstruct.Text = string.IsNullOrWhiteSpace(cachDung)
                    ? "Chưa có ghi chú liều dùng mặc định cho biệt dược này.\nBác sĩ kê đơn theo chỉ định lâm sàng phù hợp với người bệnh."
                    : $"Liều lượng & Đường dùng khuyến nghị:\n{cachDung}\n\nLưu ý kê đơn:\n- Kiểm tra tiền sử dị ứng thuốc của người bệnh trước khi kê toa.\n- Tư vấn bệnh nhân tuân thủ đúng liều lượng và thời điểm uống thuốc.";
            }

            dgvThuoc.SelectionChanged += (s, e) =>
            {
                if (dgvThuoc.SelectedRows.Count > 0 && dgvThuoc.SelectedRows[0].DataBoundItem is DataRowView drv)
                    UpdateDetail(drv);
            };

            string GetSelectedTon()
            {
                int idx = cboStatus.SelectedIndex;
                return idx switch
                {
                    1 => "ConHang",
                    2 => "SapHet",
                    3 => "HetHang",
                    _ => ""
                };
            }

            GridPageController? pageController = null;
            void LoadData()
            {
                pageController?.Reload();
                pnlKpiBadges.Invalidate();
            }

            pageController = GridPageController.Attach(dgvThuoc, (offset, pageSize) =>
                ClinicDAL.LayDanhSachThuoc(txtSearch.Text.Trim(), offset, pageSize, GetSelectedTon()));

            btnSearch.Click += (s, e) => LoadData();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadData(); };
            cboStatus.SelectedIndexChanged += (s, e) => LoadData();
            btnRefresh.Click += (s, e) =>
            {
                txtSearch.Text = "";
                cboStatus.SelectedIndex = 0;
                LoadData();
            };

            wrapper.Controls.AddRange(new Control[] { pnlBody, toolPanel });
            panelContent.Controls.Add(wrapper);
            LoadData();
        }

        // ═══════════════════════════════════════════════════════════
        //  VIEW 6: BÁC SĨ - BẢNG GIÁ DỊCH VỤ KỸ THUẬT & CLS (READ-ONLY)
        // ═══════════════════════════════════════════════════════════
        public void ShowBacSiDichVuView()
        {
            SetRefreshPageAction(ShowBacSiDichVuView);
            SetActiveNav(btnNavDichVu);
            SetPageHeader("Bác sĩ  ›  Bảng giá dịch vụ", "Danh mục dịch vụ kỹ thuật, khám chuyên khoa, xét nghiệm & chẩn đoán hình ảnh (CLS)");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            // ── TOP TOOLBAR ──
            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = C_Card, Padding = new Padding(14, 10, 14, 10) };
            toolPanel.Paint += PaintCardBorder;

            var lblSearch = MakeLbl("Tìm dịch vụ:", 14, 17);
            var txtSearch = MakeInput(100, 12, 280);
            txtSearch.PlaceholderText = "Tên kỹ thuật, mã DV, từ khóa...";

            var btnSearch = PrimaryBtn("🔍 Tìm kiếm", 390, 11, 100, 33);
            var btnRefresh = SecondaryBtn("↻ Làm mới", 500, 11, 95, 33, C_Blue);

            var lblCountBadge = new Label
            {
                Location = new Point(toolPanel.ClientSize.Width - 260, 14),
                Size = new Size(245, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = C_Teal,
                TextAlign = ContentAlignment.MiddleRight,
                Text = "Dịch vụ kỹ thuật phòng khám"
            };

            toolPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnSearch, btnRefresh, lblCountBadge });

            // ── BODY ──
            var pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 0), BackColor = Color.Transparent };

            // Right detail card
            var pnlRight = new Panel { Dock = DockStyle.Right, Width = 360, BackColor = C_Card, Padding = new Padding(14) };
            pnlRight.Paint += PaintCardBorder;

            var lblDetailHeader = MakeLbl("🔬 CHI TIẾT DỊCH VỤ KỸ THUẬT", 14, 12, bold: true);
            lblDetailHeader.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);

            var lblServiceName = new Label { Location = new Point(14, 42), Size = new Size(pnlRight.Width - 28, 48), Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = C_Teal, Text = "Chọn dịch vụ để xem chi tiết", Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var lblPriceBadge = new Label { Location = new Point(14, 94), Size = new Size(200, 28), Font = new Font("Segoe UI", 10f, FontStyle.Bold), Text = "Đơn giá: --", BackColor = Color.FromArgb(220, 252, 231), ForeColor = Color.FromArgb(22, 101, 52), TextAlign = ContentAlignment.MiddleCenter };

            var lblNotesTitle = MakeLbl("Hướng dẫn & Lưu ý chuẩn bị cho bệnh nhân:", 14, 136, bold: true);
            var txtNotes = new TextBox { Location = new Point(14, 162), Size = new Size(pnlRight.Width - 28, pnlRight.Height - 177), Multiline = true, ReadOnly = true, Font = F_Base, BackColor = Color.FromArgb(248, 250, 252), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };

            pnlRight.Controls.AddRange(new Control[] { lblDetailHeader, lblServiceName, lblPriceBadge, lblNotesTitle, txtNotes });

            // Left grid
            var pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            pnlLeft.Paint += PaintCardBorder;
            var dgvDichVu = MakeGrid();
            dgvDichVu.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvDichVu.DataBindingComplete += (s, e) =>
            {
                if (dgvDichVu.Columns.Contains("MaDV"))
                {
                    dgvDichVu.Columns["MaDV"]!.HeaderText = "Mã DV";
                    dgvDichVu.Columns["MaDV"]!.FillWeight = 0.8f;
                    dgvDichVu.Columns["MaDV"]!.MinimumWidth = 70;
                }
                if (dgvDichVu.Columns.Contains("TenDV"))
                {
                    dgvDichVu.Columns["TenDV"]!.HeaderText = "Tên dịch vụ kỹ thuật / Cận lâm sàng";
                    dgvDichVu.Columns["TenDV"]!.FillWeight = 2.5f;
                    dgvDichVu.Columns["TenDV"]!.MinimumWidth = 160;
                }
                if (dgvDichVu.Columns.Contains("DonGia"))
                {
                    dgvDichVu.Columns["DonGia"]!.HeaderText = "Đơn giá viện phí";
                    dgvDichVu.Columns["DonGia"]!.FillWeight = 1.2f;
                    dgvDichVu.Columns["DonGia"]!.MinimumWidth = 105;
                    dgvDichVu.Columns["DonGia"]!.DefaultCellStyle.Format = "N0 đ";
                    dgvDichVu.Columns["DonGia"]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (dgvDichVu.Columns.Contains("MoTa"))
                {
                    dgvDichVu.Columns["MoTa"]!.HeaderText = "Mô tả / Lưu ý chuẩn bị";
                    dgvDichVu.Columns["MoTa"]!.FillWeight = 2.5f;
                    dgvDichVu.Columns["MoTa"]!.MinimumWidth = 160;
                }
            };

            pnlLeft.Controls.Add(dgvDichVu);

            pnlBody.Controls.Add(pnlLeft);
            pnlBody.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlBody.Controls.Add(pnlRight);
            ConfigureDoctorRightColumn(pnlBody, pnlRight);

            void UpdateDetail(DataRowView? drv)
            {
                if (drv == null)
                {
                    lblServiceName.Text = "Chọn dịch vụ để xem chi tiết";
                    lblPriceBadge.Text = "Đơn giá: --";
                    txtNotes.Text = "";
                    return;
                }
                var r = drv.Row;
                string ma = r["MaDV"]?.ToString() ?? "";
                string ten = r["TenDV"]?.ToString() ?? "";
                decimal gia = r.Table.Columns.Contains("DonGia") && r["DonGia"] != DBNull.Value ? Convert.ToDecimal(r["DonGia"]) : 0;
                string moTa = r.Table.Columns.Contains("MoTa") ? r["MoTa"]?.ToString() ?? "" : "";

                lblServiceName.Text = $"{ten} [{ma}]";
                lblPriceBadge.Text = $"Đơn giá: {gia:N0} đ";

                txtNotes.Text = string.IsNullOrWhiteSpace(moTa)
                    ? "Dịch vụ kỹ thuật chuẩn của phòng khám.\nKhông yêu cầu chuẩn bị đặc biệt trước khi thực hiện."
                    : $"Quy trình & Hướng dẫn chuẩn bị:\n{moTa}\n\nLưu ý bác sĩ khi chỉ định:\n- Tư vấn mức phí và giải thích lý do cần thực hiện cho người bệnh.\n- Đảm bảo bệnh nhân đáp ứng các điều kiện an toàn cận lâm sàng.";
            }

            dgvDichVu.SelectionChanged += (s, e) =>
            {
                if (dgvDichVu.SelectedRows.Count > 0 && dgvDichVu.SelectedRows[0].DataBoundItem is DataRowView drv)
                    UpdateDetail(drv);
            };

            GridPageController? pageController = null;
            void LoadData()
            {
                pageController?.Reload();
            }

            pageController = GridPageController.Attach(dgvDichVu, (offset, pageSize) =>
            {
                var dt = ClinicDAL.LayDanhSachDichVu(txtSearch.Text.Trim(), offset, pageSize);
                int total = dt.Rows.Count > 0 && dt.Columns.Contains("_TotalRows") && dt.Rows[0]["_TotalRows"] != DBNull.Value
                    ? Convert.ToInt32(dt.Rows[0]["_TotalRows"]) : dt.Rows.Count;
                lblCountBadge.Text = $"Tổng số: {total} dịch vụ";
                return dt;
            });

            btnSearch.Click += (s, e) => LoadData();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadData(); };
            btnRefresh.Click += (s, e) =>
            {
                txtSearch.Text = "";
                LoadData();
            };

            wrapper.Controls.AddRange(new Control[] { pnlBody, toolPanel });
            panelContent.Controls.Add(wrapper);
            LoadData();
        }
    }
}
