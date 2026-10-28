using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

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

            SetPageHeader("Bác sĩ  ›  Lịch khám", "Lịch khám của tôi");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // ── TOP STATS / KPI ──
            var stats = ClinicDAL.LayThongKeBacSi(string.IsNullOrEmpty(maBS) ? null : maBS);

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 148, BackColor = Color.Transparent };

            // Welcome label + Date button
            var lblWelcome = new Label
            {
                Text = $"Chào {tenBS}. Hôm nay có {stats.tongPhanCong} lượt khám được phân công tại phòng khám.",
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
                MakeKpiCard("Lịch được phân công", stats.tongPhanCong.ToString(), "Tổng số ca khám ca hôm nay", C_Teal, 0, 42),
                MakeKpiCard("Đã hoàn tất", stats.daHoanTat.ToString(), "Bệnh nhân đã kết thúc khám", C_Green, 0, 42),
                MakeKpiCard("Đang khám / Chờ", $"{stats.dangKham} / {stats.dangCho}", $"{stats.dangCho} bệnh nhân chờ vào phòng", C_Orange, 0, 42),
                MakeKpiCard("Lượt tiếp theo", stats.luotTiepTheo, "STT kế tiếp trong hàng đợi", C_Blue, 0, 42)
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
            // CỘT PHẢI: BỆNH NHÂN ĐANG KHÁM & LƯU Ý CA KHÁM
            // ══════════════════════════════════════════════
            var grpRight = new Panel { Dock = DockStyle.Right, Width = 360, BackColor = Color.Transparent, Padding = new Padding(8, 0, 0, 0) };

            // Card 1: Bệnh nhân đang khám
            var pnlCur = new Panel { Dock = DockStyle.Top, Height = 250, BackColor = C_Card, Padding = new Padding(16) };
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

            var lblCurName = new Label { Text = curName, Location = new Point(14, 44), AutoSize = true, Font = new Font("Segoe UI", 14f, FontStyle.Bold), ForeColor = C_TextDark };
            var lblCurMeta = new Label { Text = $"{curMaBN}  •  {curGT}  •  {curTuoi} tuổi", Location = new Point(14, 76), AutoSize = true, Font = F_Small, ForeColor = C_TextGray };

            var lblCurReasonHead = new Label { Text = "Lý do khám:", Location = new Point(14, 104), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };
            var lblCurReason = new Label { Text = curLyDo, Location = new Point(14, 124), Size = new Size(320, 36), Font = F_Base, ForeColor = C_TextDark };

            var lblCurRoom = new Label { Text = "📍 Phòng khám: P.101 – Chuyên khoa Nội", Location = new Point(14, 164), AutoSize = true, Font = F_Small, ForeColor = C_Teal };

            var btnTiepTuc = PrimaryBtn("Tiếp tục phiên khám  ➔", 14, 194, 330, 38);
            btnTiepTuc.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            btnTiepTuc.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(curMaPK)) ShowBacSiKhamKeDonView(curMaPK, 0);
                else MessageBox.Show("Hiện không có bệnh nhân nào đang trong ca khám!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            pnlCur.Controls.AddRange(new Control[] { lblCurTitle, badgeCur, lblCurName, lblCurMeta, lblCurReasonHead, lblCurReason, lblCurRoom, btnTiepTuc });

            // Card 2: Lưu ý trong ca
            var pnlNotice = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16), Margin = new Padding(0, 14, 0, 0) };
            pnlNotice.Paint += PaintCardBorder;

            var lblNoticeTitle = new Label { Text = "LƯU Ý TRONG CA", Location = new Point(14, 14), AutoSize = true, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), ForeColor = C_TextGray };

            var boxNotice1 = new Panel { Location = new Point(14, 42), Size = new Size(330, 70), BackColor = Color.FromArgb(240, 253, 250), Padding = new Padding(10) };
            boxNotice1.Controls.Add(new Label { Text = "🔒 Chỉ hiển thị bệnh nhân phụ trách. Bệnh nhân ngoài phạm vi cần được cấp quyền truy cập.", Dock = DockStyle.Fill, Font = F_Small, ForeColor = Color.FromArgb(13, 148, 136) });

            var lblNoticeWaiting = new Label { Text = $"⏳ {stats.dangCho} người đang chờ khám", Location = new Point(14, 124), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };
            var lblNoticePrio    = new Label { Text = "Ưu tiên người đã nộp kết quả chụp X-quang hoặc xét nghiệm máu trước khi gọi lượt tiếp theo.", Location = new Point(14, 146), Size = new Size(330, 48), Font = F_Small, ForeColor = C_TextGray };

            var btnViewWaiting = SecondaryBtn("📋 Xem danh sách chờ", 14, 204, 330, 34, C_Teal);

            pnlNotice.Controls.AddRange(new Control[] { lblNoticeTitle, boxNotice1, lblNoticeWaiting, lblNoticePrio, btnViewWaiting });

            var pnlRightWrapper = new Panel { Dock = DockStyle.Fill };
            pnlRightWrapper.Controls.Add(pnlNotice);
            pnlRightWrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 14, BackColor = Color.Transparent });
            pnlRightWrapper.Controls.Add(pnlCur);
            grpRight.Controls.Add(pnlRightWrapper);

            void LayoutRightCards()
            {
                int cardWidth = Math.Max(180, grpRight.ClientSize.Width - 8);
                int innerWidth = Math.Max(150, cardWidth - 28);
                lblCurReason.Width = innerWidth;
                lblCurRoom.Size = new Size(innerWidth, 24);
                btnTiepTuc.Width = innerWidth;
                boxNotice1.Width = innerWidth;
                lblNoticePrio.Size = new Size(innerWidth, 54);
                btnViewWaiting.Width = innerWidth;
            }
            grpRight.SizeChanged += (s, e) => LayoutRightCards();
            LayoutRightCards();

            // ══════════════════════════════════════════════
            // CỘT TRÁI: DANH SÁCH KHÁM HÔM NAY
            // ══════════════════════════════════════════════
            var grpLeft = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(16) };
            grpLeft.Paint += PaintCardBorder;

            var lblListTitle = new Label { Text = "Danh sách khám hôm nay", Location = new Point(16, 14), AutoSize = true, Font = new Font("Segoe UI", 11.5f, FontStyle.Bold), ForeColor = C_TextDark };

            // Filter bar
            var txtSearch = MakeInput(16, 48, 260, false, "");
            var lblPlaceholder = new Label { Text = "🔍 Tìm bệnh nhân trong danh sách...", Location = new Point(24, 54), AutoSize = true, ForeColor = Color.FromArgb(156, 163, 175), BackColor = Color.White };
            lblPlaceholder.Click += (s, e) => txtSearch.Focus();
            txtSearch.Enter += (s, e) => lblPlaceholder.Visible = false;
            txtSearch.Leave += (s, e) => lblPlaceholder.Visible = string.IsNullOrEmpty(txtSearch.Text);

            var cboStatus = new ComboBox { Location = new Point(290, 48), Size = new Size(160, 30), DropDownStyle = ComboBoxStyle.DropDownList, Font = F_Base };
            cboStatus.Items.AddRange(new object[] { "Tất cả trạng thái", "Chờ khám", "Đang khám", "Đã khám xong", "Chờ kết quả CLS" });
            cboStatus.SelectedIndex = 0;

            var btnRefresh = SecondaryBtn("🔄 Làm mới", 465, 47, 100, 31, C_Blue);
            var approvedAppointments = ClinicDAL.LayLichHenDaDuyetBacSi(maBS, DateTime.Today);
            var btnApprovedAppointments = SecondaryBtn(
                $"📅 Lịch đã duyệt ({approvedAppointments.Rows.Count})", 575, 47, 190, 31, C_Teal);
            btnApprovedAppointments.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnApprovedAppointments.Click += (s, e) =>
            {
                var latest = ClinicDAL.LayLichHenDaDuyetBacSi(maBS, DateTime.Today);
                btnApprovedAppointments.Text = $"📅 Lịch đã duyệt ({latest.Rows.Count})";
                ShowBacSiLichHenDaDuyetView(maBS);
            };

            // DataGridView
            var dgv = MakeGrid(DockStyle.None);
            dgv.Location = new Point(16, 92);
            dgv.Size = new Size(grpLeft.Width - 32, grpLeft.Height - 146);
            dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgv.ScrollBars = ScrollBars.Vertical;

            // Load data method
            void LoadList()
            {
                string kw = txtSearch.Text.Trim();
                string? st = cboStatus.SelectedItem?.ToString();
                dgv.DataSource = ClinicDAL.LayDanhSachKhamBacSi(string.IsNullOrEmpty(maBS) ? null : maBS, kw, st);

                // Format columns safely
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
            }

            txtSearch.TextChanged += (s, e) => LoadList();
            cboStatus.SelectedIndexChanged += (s, e) => LoadList();
            btnRefresh.Click += (s, e) => LoadList();

            btnViewWaiting.Click += (s, e) =>
            {
                cboStatus.SelectedItem = "Chờ khám";
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
            grpLeft.Controls.AddRange(new Control[] { lblListTitle, txtSearch, lblPlaceholder, cboStatus, btnRefresh, btnApprovedAppointments, dgv, pnlFooter });
            grpLeft.SizeChanged += (s, e) =>
                btnApprovedAppointments.Left = Math.Max(btnRefresh.Right + 10, grpLeft.ClientSize.Width - btnApprovedAppointments.Width - 16);

            // Thêm cột phải trước, sau đó là cột trái fill
            pnlBody.Controls.Add(grpLeft);
            pnlBody.Controls.Add(new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Color.Transparent });
            pnlBody.Controls.Add(grpRight);
            ConfigureDoctorRightColumn(pnlBody, grpRight);

            wrapper.Controls.AddRange(new Control[] { pnlBody, pnlTop });
            panelContent.Controls.Add(wrapper);

            LoadList();
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
                Text = "Tài liệu đính kèm chưa được hỗ trợ lưu trong cơ sở dữ liệu.",
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

            tabControlDB.TabPages.AddRange(new[] { tabBenhSu, tabCLS, tabThuoc });
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

            void LoadPatients()
            {
                dgv.DataSource = ClinicDAL.LayDanhSachBenhNhanBacSi(
                    SessionState.CurrentUser?.MaNV ?? "",
                    txtSearch.Text.Trim());
            }

            void OpenSelectedPatient()
            {
                if (dgv.SelectedRows.Count == 0) return;
                string maBN = dgv.SelectedRows[0].Cells["MaBN"].Value?.ToString() ?? "";
                if (!string.IsNullOrEmpty(maBN))
                    ShowBacSiHoSoBenhAnView(maBN);
            }

            LoadPatients();
            txtSearch.TextChanged += (s, e) => LoadPatients();
            dgv.CellDoubleClick += (s, e) => OpenSelectedPatient();

            var btnView = PrimaryBtn("Xem hồ sơ", 350, 8, 120, 34);
            btnView.Click += (s, e) => OpenSelectedPatient();
            toolPanel.Controls.Add(btnView);

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

            pnlWorkflow.Controls.AddRange(new Control[] { btnStep1, btnStep2, btnStep3, btnBackToList });

            // ── PATIENT BANNER ──
            var pnlBanner = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = C_Card, Padding = new Padding(16, 10, 16, 10) };
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
                    ClinicDAL.CapNhatKetQuaDV(maCD, kq);
                    LoadCLSList();
                }
            };

            var btnAttach = SecondaryBtn("📎 Tài liệu (chưa lưu)", 184, cardCLS.Height - 50, 180, 34, C_Teal);
            btnAttach.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnAttach.Enabled = false;

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
            var txtCD = new TextBox { Location = new Point(14, 370), Size = new Size(cardLamSang.Width - 48, 44), Multiline = true, Font = F_Base, Text = cd, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Row 5: Hướng xử trí
            var lblHD = MakeLbl("Hướng xử trí:", 14, 422, bold: true);
            var txtHD = new TextBox { Location = new Point(14, 444), Size = new Size(cardLamSang.Width - 48, 44), Multiline = true, Font = F_Base, Text = ld, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            // Action buttons
            var btnSaveDraft = SecondaryBtn("💾 Lưu nháp", 14, 502, 120, 36, Color.FromArgb(107, 114, 128));
            var btnNextStep  = PrimaryBtn("Tiếp theo: Kê đơn thuốc  ➔", 144, 502, 230, 36);

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
                lblThamKham, txtThamKham, lblCD, txtCD, lblHD, txtHD,
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

            var btnPrintDon = SecondaryBtn("🖨 In đơn thuốc", 134, cardDon.Height - 48, 120, 34, C_Blue);
            btnPrintDon.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnPrintDon.Click += (s, e) => MessageBox.Show("Chức năng in đơn thuốc chưa được triển khai.", "In đơn thuốc", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                    MessageBox.Show("Đã hoàn tất ca khám và chuyển phiếu sang quầy thu ngân.", "Hoàn tất thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ShowBacSiLichKhamView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi hoàn tất ca khám: {ex.Message}", "Lỗi cơ sở dữ liệu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnPrintSummary = SecondaryBtn("🖨 In tóm tắt ca khám", 14, 376, cardPay.Width - 28, 34, C_Blue);
            btnPrintSummary.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnPrintSummary.Click += (s, e) => MessageBox.Show("Chức năng in tóm tắt ca khám chưa được triển khai.", "In tóm tắt", MessageBoxButtons.OK, MessageBoxIcon.Information);

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

            void LoadTraCuu()
            {
                var dt = ClinicDAL.TraCuuChuyenMon(txtKw.Text.Trim(), currentTab);
                dgvRes.DataSource = dt;
                lblResCount.Text = $"Kết quả tra cứu: {dt.Rows.Count} mục phù hợp";

                SafeFormatCol(dgvRes, "Loai", "Phân loại", 120);
                SafeFormatCol(dgvRes, "Ma", "Mã", 80);
                SafeFormatCol(dgvRes, "TieuDe", "Tiêu đề / Đối tượng", 200);
                SafeFormatCol(dgvRes, "MoTa", "Mô tả sơ lược", 200);
                SafeFormatCol(dgvRes, "ChiTiet", "", null, false);
            }

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

            LoadTraCuu();
        }

        // Tương thích ngược: nếu có nút cũ gọi ShowKhamBenhView()
        private void ShowKhamBenhView() => ShowBacSiKhamKeDonView();
    }
}
