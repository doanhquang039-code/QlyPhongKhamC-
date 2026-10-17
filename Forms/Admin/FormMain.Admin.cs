using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;
using QlyPhongKham.Forms;

namespace QlyPhongKham.Forms
{
    public partial class FormMain
    {
        private static void ConfigureAdminSummaryLayout(
            Panel wrapper,
            Panel pCards,
            Panel pnlDT,
            DataGridView dgvDT,
            Panel pnlCD,
            DataGridView dgvCD,
            Panel pnlTH,
            DataGridView dgvTH)
        {
            void LayoutSummary()
            {
                int contentWidth = Math.Max(0, wrapper.ClientSize.Width - 20);
                pCards.Width = contentWidth;
                pnlTH.Width = contentWidth;
                dgvTH.Width = Math.Max(0, pnlTH.ClientSize.Width - 28);

                int cardWidth = Math.Max(0, (pCards.ClientSize.Width - 45) / 4);
                for (int i = 0; i < pCards.Controls.Count; i++)
                {
                    if (pCards.Controls[i] is not Panel card)
                        continue;

                    card.Location = new Point(i * (cardWidth + 15), 0);
                    card.Size = new Size(cardWidth, card.Height);
                    if (card.Controls.Count >= 4)
                    {
                        card.Controls[0].Width = cardWidth;
                        card.Controls[1].Width = Math.Max(0, cardWidth - 28);
                        card.Controls[3].Width = Math.Max(0, cardWidth - 28);
                    }
                }

                int columnWidth = Math.Max(0, (contentWidth - 15) / 2);
                pnlDT.Location = new Point(10, pnlDT.Top);
                pnlDT.Width = columnWidth;
                dgvDT.Width = Math.Max(0, pnlDT.ClientSize.Width - 28);

                pnlCD.Location = new Point(25 + columnWidth, pnlCD.Top);
                pnlCD.Width = columnWidth;
                dgvCD.Width = Math.Max(0, pnlCD.ClientSize.Width - 28);
            }

            wrapper.SizeChanged += (s, e) => LayoutSummary();
            LayoutSummary();
        }

        // ═══════════════════════════════════════════════════════════
        //  ADMIN: TỔNG QUAN (DASHBOARD)
        // ═══════════════════════════════════════════════════════════
        private void ShowDashboard()
        {
            SetActiveNav(btnNavTongQuan);
            SetPageHeader("Quản trị viên  ›  Tổng quan", "Tổng quan phòng khám");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            // ── KPI Cards ──
            var (kham, thu, thuocSapHet) = ClinicDAL.LayThongKeTongQuan();
            int tongBN = ClinicDAL.LayDanhSachBenhNhan().Rows.Count;

            var pCards = new Panel { Location = new Point(10, 10), Size = new Size(1080, 115) };
            pCards.Controls.Add(MakeKpiCard("LƯỢT KHÁM HÔM NAY", $"{kham} lượt", "Phiếu khám đăng ký hôm nay", C_Teal, 0, 0, 250));
            pCards.Controls.Add(MakeKpiCard("DOANH THU HÔM NAY", $"{thu:N0} đ", "Hóa đơn đã thanh toán", C_Green, 265, 0, 250));
            pCards.Controls.Add(MakeKpiCard("TỔNG SỐ BỆNH NHÂN", $"{tongBN} người", "Hồ sơ lưu trữ hệ thống", C_Blue, 530, 0, 250));
            pCards.Controls.Add(MakeKpiCard("THUỐC SẮP HẾT", $"{thuocSapHet} loại", "Tồn kho cảnh báo ≤ 30", C_Red, 795, 0, 250));

            // ── Doanh thu 7 ngày ──
            var pnlDT = new Panel { Location = new Point(10, 135), Size = new Size(525, 270), BackColor = C_Card };
            pnlDT.Paint += PaintCardBorder;
            pnlDT.Controls.Add(MakeLbl("DOANH THU 7 NGÀY GẦN NHẤT", 14, 12, bold: true));
            var dgvDT = MakeGrid(DockStyle.None);
            dgvDT.Location = new Point(14, 38);
            dgvDT.Size = new Size(497, 218);
            dgvDT.DataSource = ClinicDAL.LayDoanhThu7Ngay();
            pnlDT.Controls.Add(dgvDT);

            // ── Top 10 chẩn đoán ──
            var pnlCD = new Panel { Location = new Point(550, 135), Size = new Size(540, 270), BackColor = C_Card };
            pnlCD.Paint += PaintCardBorder;
            pnlCD.Controls.Add(MakeLbl("TOP BỆNH LÝ PHỔ BIẾN", 14, 12, bold: true));
            var dgvCD = MakeGrid(DockStyle.None);
            dgvCD.Location = new Point(14, 38);
            dgvCD.Size = new Size(512, 218);
            dgvCD.DataSource = ClinicDAL.LayTopChanDoan(10);
            pnlCD.Controls.Add(dgvCD);

            // ── Thuốc sắp hết ──
            var pnlTH = new Panel { Location = new Point(10, 415), Size = new Size(1080, 240), BackColor = C_Card };
            pnlTH.Paint += PaintCardBorder;
            pnlTH.Controls.Add(MakeLbl("⚠ CẢNH BÁO: DƯỢC PHẨM CẦN BỔ SUNG GẤP (TỒN KHO ≤ 30)", 14, 12, bold: true));
            var dgvTH = MakeGrid(DockStyle.None);
            dgvTH.Location = new Point(14, 38);
            dgvTH.Size = new Size(1052, 188);
            dgvTH.DataSource = ClinicDAL.LayThuocSapHet(30);
            pnlTH.Controls.Add(dgvTH);

            wrapper.Controls.AddRange(new Control[] { pCards, pnlDT, pnlCD, pnlTH });
            panelContent.Controls.Add(wrapper);
            ConfigureAdminSummaryLayout(wrapper, pCards, pnlDT, dgvDT, pnlCD, dgvCD, pnlTH, dgvTH);
        }

        // ═══════════════════════════════════════════════════════════
        //  ADMIN: QUẢN LÝ NHÂN SỰ
        // ═══════════════════════════════════════════════════════════
        private void ShowNhanVienView()
        {
            SetActiveNav(btnNavNhanVien);
            SetPageHeader("Quản trị viên  ›  Nhân sự", "Quản lý nhân viên & Phân quyền");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;

            var btnAddNV     = PrimaryBtn("➕ Thêm nhân viên", 12, 8, 155, 34);
            var btnEditNV    = SecondaryBtn("✏ Sửa thông tin", 177, 8, 140, 34, C_Orange);
            var btnDisableNV = SecondaryBtn("🚫 Vô hiệu hóa", 327, 8, 135, 34, C_Red);
            var btnCapTK     = SecondaryBtn("🔑 Cấp / Đổi MK", 472, 8, 160, 34, C_Blue);

            toolPanel.Controls.AddRange(new Control[] { btnAddNV, btnEditNV, btnDisableNV, btnCapTK });

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            contentPanel.Paint += PaintCardBorder;
            var dgvNV = MakeGrid();
            contentPanel.Controls.Add(dgvNV);

            void LoadNV() => dgvNV.DataSource = ClinicDAL.LayDanhSachNhanVien();
            LoadNV();

            btnAddNV.Click += (s, e) =>
            {
                var dlg = new FormThemSuaNhanVien(null);
                if (dlg.ShowDialog() == DialogResult.OK) LoadNV();
            };

            btnEditNV.Click += (s, e) =>
            {
                if (dgvNV.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn nhân viên cần sửa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var r = dgvNV.SelectedRows[0];
                var nv = new NhanVien
                {
                    MaNV = r.Cells["MaNV"].Value?.ToString() ?? "",
                    HoTen = r.Cells["HoTen"].Value?.ToString() ?? "",
                    ChucVu = r.Cells["ChucVu"].Value?.ToString() ?? "LeTan",
                    ChuyenKhoa = r.Cells["ChuyenKhoa"].Value?.ToString(),
                    SoDienThoai = r.Cells["SoDienThoai"].Value?.ToString(),
                    Email = r.Cells["Email"].Value?.ToString()
                };
                var dlg = new FormThemSuaNhanVien(nv);
                if (dlg.ShowDialog() == DialogResult.OK) LoadNV();
            };

            btnDisableNV.Click += (s, e) =>
            {
                if (dgvNV.SelectedRows.Count == 0) return;
                string maNV = dgvNV.SelectedRows[0].Cells["MaNV"].Value?.ToString() ?? "";
                if (maNV == SessionState.CurrentUser?.MaNV)
                {
                    MessageBox.Show("Không thể vô hiệu hóa tài khoản đang đăng nhập!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show("Vô hiệu hóa tài khoản nhân viên này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    ClinicDAL.XoaNhanVien(maNV);
                    LoadNV();
                }
            };

            btnCapTK.Click += (s, e) =>
            {
                if (dgvNV.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn nhân viên cần cấp tài khoản!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var r = dgvNV.SelectedRows[0];
                string maNV = r.Cells["MaNV"].Value?.ToString() ?? "";
                string tenDN = r.Cells["TenDangNhap"].Value?.ToString() ?? "";
                bool isNew = string.IsNullOrEmpty(tenDN);
                var dlg = new FormCapTaiKhoan(maNV, tenDN, r.Cells["ChucVu"].Value?.ToString() ?? "LeTan", isNew);
                if (dlg.ShowDialog() == DialogResult.OK) LoadNV();
            };

            wrapper.Controls.Add(contentPanel);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 }); // khoảng cách
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════════
        //  ADMIN: BÁO CÁO & THỐNG KÊ
        // ═══════════════════════════════════════════════════════════
        private void ShowBaoCaoView()
        {
            SetActiveNav(btnNavBaoCao);
            SetPageHeader("Quản trị viên  ›  Báo cáo", "Báo cáo thống kê hoạt động");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            var (kham, thu, thuocSapHet) = ClinicDAL.LayThongKeTongQuan();
            int tongBN = ClinicDAL.LayDanhSachBenhNhan().Rows.Count;

            var pCards = new Panel { Location = new Point(10, 10), Size = new Size(1080, 115) };
            pCards.Controls.Add(MakeKpiCard("LƯỢT KHÁM HÔM NAY", $"{kham} lượt", "Phiếu khám đăng ký hôm nay", C_Teal, 0, 0, 250));
            pCards.Controls.Add(MakeKpiCard("DOANH THU THỰC TẾ", $"{thu:N0} đ", "Tổng viện phí đã thu", C_Green, 265, 0, 250));
            pCards.Controls.Add(MakeKpiCard("TỔNG HỒ SƠ BN", $"{tongBN} hồ sơ", "Cơ sở dữ liệu bệnh nhân", C_Blue, 530, 0, 250));
            pCards.Controls.Add(MakeKpiCard("THUỐC CẦN NHẬP", $"{thuocSapHet} mục", "Số lượng tồn kho ≤ 30", C_Red, 795, 0, 250));

            var pnlDT = new Panel { Location = new Point(10, 135), Size = new Size(525, 290), BackColor = C_Card };
            pnlDT.Paint += PaintCardBorder;
            pnlDT.Controls.Add(MakeLbl("DOANH THU 7 NGÀY GẦN NHẤT", 14, 12, bold: true));
            var dgvDT = MakeGrid(DockStyle.None);
            dgvDT.Location = new Point(14, 38);
            dgvDT.Size = new Size(497, 238);
            dgvDT.DataSource = ClinicDAL.LayDoanhThu7Ngay();
            pnlDT.Controls.Add(dgvDT);

            var pnlCD = new Panel { Location = new Point(550, 135), Size = new Size(540, 290), BackColor = C_Card };
            pnlCD.Paint += PaintCardBorder;
            pnlCD.Controls.Add(MakeLbl("TOP 10 CHẨN ĐOÁN THƯỜNG GẶP", 14, 12, bold: true));
            var dgvCD = MakeGrid(DockStyle.None);
            dgvCD.Location = new Point(14, 38);
            dgvCD.Size = new Size(512, 238);
            dgvCD.DataSource = ClinicDAL.LayTopChanDoan(10);
            pnlCD.Controls.Add(dgvCD);

            var pnlTH = new Panel { Location = new Point(10, 435), Size = new Size(1080, 250), BackColor = C_Card };
            pnlTH.Paint += PaintCardBorder;
            pnlTH.Controls.Add(MakeLbl("DANH SÁCH DƯỢC PHẨM CẦN BỔ SUNG (TỒN KHO ≤ 30)", 14, 12, bold: true));
            var dgvTH = MakeGrid(DockStyle.None);
            dgvTH.Location = new Point(14, 38);
            dgvTH.Size = new Size(1052, 198);
            dgvTH.DataSource = ClinicDAL.LayThuocSapHet(30);
            pnlTH.Controls.Add(dgvTH);

            wrapper.Controls.AddRange(new Control[] { pCards, pnlDT, pnlCD, pnlTH });
            panelContent.Controls.Add(wrapper);
            ConfigureAdminSummaryLayout(wrapper, pCards, pnlDT, dgvDT, pnlCD, dgvCD, pnlTH, dgvTH);
        }
    }
}
