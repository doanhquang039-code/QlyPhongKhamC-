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
            DataGridView dgvTH,
            Panel pnlKhoaPhong,
            DataGridView dgvKhoaPhong)
        {
            void LayoutSummary()
            {
                int contentWidth = Math.Max(0, wrapper.ClientSize.Width - 20);
                pCards.Width = contentWidth;
                pnlTH.Width = contentWidth;
                dgvTH.Width = Math.Max(0, pnlTH.ClientSize.Width - 28);
                pnlKhoaPhong.Width = contentWidth;
                dgvKhoaPhong.Width = Math.Max(0, pnlKhoaPhong.ClientSize.Width - 28);

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
            SetRefreshPageAction(ShowDashboard);
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

            var pnlKhoaPhong = new Panel { Location = new Point(10, 665), Size = new Size(1080, 220), BackColor = C_Card };
            pnlKhoaPhong.Paint += PaintCardBorder;
            pnlKhoaPhong.Controls.Add(MakeLbl("LƯỢT KHÁM THEO KHOA / PHÒNG (7 NGÀY)", 14, 12, bold: true));
            var dgvKhoaPhong = MakeGrid(DockStyle.None);
            dgvKhoaPhong.Location = new Point(14, 38);
            dgvKhoaPhong.Size = new Size(1052, 168);
            if (ClinicDAL.CoSchemaKhoaPhong())
            {
                dgvKhoaPhong.DataSource = ClinicDAL.LayThongKeKhoaPhong(7);
                pnlKhoaPhong.Controls.Add(dgvKhoaPhong);
            }
            else
            {
                var lblSchemaWarning = MakeLbl(
                    "Database chưa có cấu trúc khoa/phòng. Hãy chạy seed_test_data.sql trên đúng database ứng dụng đang kết nối.",
                    14, 48);
                lblSchemaWarning.ForeColor = C_Red;
                lblSchemaWarning.AutoSize = false;
                lblSchemaWarning.Size = new Size(1040, 50);
                pnlKhoaPhong.Controls.Add(lblSchemaWarning);
            }

            wrapper.Controls.AddRange(new Control[] { pCards, pnlDT, pnlCD, pnlTH, pnlKhoaPhong });
            panelContent.Controls.Add(wrapper);
            ConfigureAdminSummaryLayout(wrapper, pCards, pnlDT, dgvDT, pnlCD, dgvCD, pnlTH, dgvTH, pnlKhoaPhong, dgvKhoaPhong);
        }

        // ═══════════════════════════════════════════════════════════
        //  ADMIN: DANH SÁCH PHIẾU KHÁM HÔM NAY
        // ═══════════════════════════════════════════════════════════
        private void ShowAdminClinicView()
        {
            SetRefreshPageAction(ShowAdminClinicView);
            SetActiveNav(btnNavKhamBenh);
            SetPageHeader("Quản trị viên  ›  Phiếu khám", "Danh sách phiếu khám hôm nay");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;

            var txtSearch = MakeInput(12, 10, 280);
            txtSearch.PlaceholderText = "Tìm bệnh nhân, mã phiếu...";
            var btnRefresh = SecondaryBtn("↻ Làm mới", 302, 8, 110, 34);
            var btnCancelVisit = SecondaryBtn("Hủy phiếu chờ khám", 422, 8, 155, 34, C_Red);
            toolPanel.Controls.AddRange(new Control[] { txtSearch, btnRefresh, btnCancelVisit });

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            contentPanel.Paint += PaintCardBorder;
            var dgv = MakeGrid();
            contentPanel.Controls.Add(dgv);

            void LoadAppointments()
            {
                dgv.DataSource = ClinicDAL.LayDanhSachKhamBacSi(null, txtSearch.Text.Trim());
            }

            LoadAppointments();
            txtSearch.TextChanged += (s, e) => LoadAppointments();
            btnRefresh.Click += (s, e) => LoadAppointments();
            btnCancelVisit.Click += (s, e) =>
            {
                if (dgv.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn phiếu khám cần hủy.", "Chưa chọn phiếu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string maPK = dgv.SelectedRows[0].Cells["MaPK"].Value?.ToString() ?? "";
                if (MessageBox.Show(
                        $"Hủy phiếu {maPK}? Chỉ phiếu đang chờ khám và chưa có hóa đơn mới được hủy.",
                        "Xác nhận hủy phiếu",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                if (!ClinicDAL.HuyPhieuKhamChoKham(maPK))
                {
                    MessageBox.Show("Không hủy được phiếu. Phiếu có thể đã bắt đầu khám, đã có hóa đơn hoặc trạng thái không còn phù hợp.",
                        "Không thể hủy phiếu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                LoadAppointments();
            };

            wrapper.Controls.Add(contentPanel);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        private void ShowAdminAppointmentsView()
        {
            SetRefreshPageAction(ShowAdminAppointmentsView);
            SetActiveNav(btnNavLichHen);
            SetPageHeader("Quản trị viên  ›  Lịch hẹn", "Quản lý lịch hẹn");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;

            var txtSearch = MakeInput(12, 9, 230);
            txtSearch.PlaceholderText = "Mã hẹn, bệnh nhân, bác sĩ...";
            var cboStatus = new ComboBox
            {
                Location = new Point(252, 9),
                Size = new Size(155, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            cboStatus.Items.AddRange(new object[] { "Tất cả trạng thái", "Chờ xác nhận", "Đã xác nhận", "Đã đến", "Đã hủy" });
            cboStatus.SelectedIndex = 0;

            var btnAdd = PrimaryBtn("Thêm lịch hẹn", 417, 8, 125, 34);
            var btnEdit = SecondaryBtn("Sửa", 552, 8, 75, 34, C_Orange);
            var btnCancel = SecondaryBtn("Hủy lịch", 635, 8, 95, 34, C_Red);
            var btnRefresh = SecondaryBtn("Làm mới", 738, 8, 95, 34, C_Blue);
            toolPanel.Controls.AddRange(new Control[] { txtSearch, cboStatus, btnAdd, btnEdit, btnCancel, btnRefresh });

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            contentPanel.Paint += PaintCardBorder;
            var dgv = MakeGrid();
            contentPanel.Controls.Add(dgv);

            void LoadAppointments()
            {
                string status = cboStatus.SelectedItem?.ToString() ?? "Tất cả trạng thái";
                dgv.DataSource = ClinicDAL.LayDanhSachLichHen(
                    txtSearch.Text.Trim(),
                    status == "Tất cả trạng thái" ? null : status);
            }

            DataRow? GetSelectedAppointment()
            {
                if (dgv.SelectedRows.Count == 0 ||
                    dgv.SelectedRows[0].DataBoundItem is not DataRowView rowView)
                    return null;
                return rowView.Row;
            }

            LoadAppointments();
            txtSearch.TextChanged += (s, e) => LoadAppointments();
            cboStatus.SelectedIndexChanged += (s, e) => LoadAppointments();
            btnRefresh.Click += (s, e) => LoadAppointments();

            btnAdd.Click += (s, e) =>
            {
                using var dialog = new FormThemSuaLichHen();
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    LoadAppointments();
            };

            btnEdit.Click += (s, e) =>
            {
                DataRow? selected = GetSelectedAppointment();
                if (selected == null)
                {
                    MessageBox.Show("Chọn lịch hẹn cần sửa.", "Chưa chọn lịch hẹn",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (selected["TrangThai"].ToString() == "Đã hủy")
                {
                    MessageBox.Show("Lịch đã hủy không thể sửa. Hãy tạo lịch hẹn mới nếu cần.",
                        "Lịch đã hủy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var dialog = new FormThemSuaLichHen(Convert.ToInt32(selected["MaLichHen"]), selected);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    LoadAppointments();
            };

            btnCancel.Click += (s, e) =>
            {
                DataRow? selected = GetSelectedAppointment();
                if (selected == null)
                {
                    MessageBox.Show("Chọn lịch hẹn cần hủy.", "Chưa chọn lịch hẹn",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int maLichHen = Convert.ToInt32(selected["MaLichHen"]);
                if (MessageBox.Show($"Hủy lịch hẹn #{maLichHen} của {selected["TenBN"]}?",
                        "Xác nhận hủy lịch", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                if (!ClinicDAL.HuyLichHen(maLichHen))
                {
                    MessageBox.Show("Chỉ lịch đang chờ xác nhận hoặc đã xác nhận mới được hủy.",
                        "Không thể hủy lịch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                LoadAppointments();
            };

            wrapper.Controls.Add(contentPanel);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════════
        //  ADMIN: QUẢN LÝ NHÂN SỰ
        // ═══════════════════════════════════════════════════════════
        private void ShowNhanVienView()
        {
            SetRefreshPageAction(ShowNhanVienView);
            SetActiveNav(btnNavNhanVien);
            SetPageHeader("Quản trị viên  ›  Nhân sự", "Quản lý nhân viên & Phân quyền");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;

            var btnAddNV     = PrimaryBtn("➕ Thêm nhân viên", 12, 8, 155, 34);
            var btnEditNV    = SecondaryBtn("✏ Sửa thông tin", 177, 8, 140, 34, C_Orange);
            var btnDisableNV = SecondaryBtn("🚫 Vô hiệu hóa", 327, 8, 135, 34, C_Red);
            var btnCapTK     = SecondaryBtn("🔑 Cấp / Đổi MK", 472, 8, 160, 34, C_Blue);
            var btnAssignRoom = SecondaryBtn("🏥 Phân công phòng", 638, 8, 170, 34, C_Teal);
            var btnRefreshNV = SecondaryBtn("↻ Làm mới", 482, 52, 105, 30, C_Blue);
            var txtSearchNV  = MakeInput(12, 52, 280, false, "");
            var cboRoleFilter = new ComboBox
            {
                Location = new Point(302, 52),
                Size = new Size(170, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            cboRoleFilter.Items.AddRange(new object[] { "Tất cả chức vụ", "Admin", "BacSi", "LeTan" });
            cboRoleFilter.SelectedIndex = 0;
            toolPanel.Controls.AddRange(new Control[] { btnAddNV, btnEditNV, btnDisableNV, btnCapTK, btnAssignRoom, btnRefreshNV, txtSearchNV, cboRoleFilter });

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            contentPanel.Paint += PaintCardBorder;
            var dgvNV = MakeGrid();
            contentPanel.Controls.Add(dgvNV);

            void LoadNV()
            {
                string role = cboRoleFilter.SelectedItem?.ToString() ?? "";
                if (role == "Tất cả chức vụ") role = "";
                dgvNV.DataSource = ClinicDAL.LayDanhSachNhanVien(txtSearchNV.Text.Trim(), role);
            }
            LoadNV();
            txtSearchNV.TextChanged += (s, e) => LoadNV();
            cboRoleFilter.SelectedIndexChanged += (s, e) => LoadNV();
            btnRefreshNV.Click += (s, e) => LoadNV();

            btnAssignRoom.Click += (s, e) =>
            {
                if (dgvNV.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn nhân viên cần phân công phòng khám.", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var row = dgvNV.SelectedRows[0];
                string maNV = row.Cells["MaNV"].Value?.ToString() ?? "";
                string chucVu = row.Cells["ChucVu"].Value?.ToString() ?? "";
                if (chucVu is not ("BacSi" or "LeTan"))
                {
                    MessageBox.Show("Chỉ có thể phân công phòng cho bác sĩ hoặc lễ tân.", "Không phù hợp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                using var dlg = new FormPhanCongPhong(maNV, row.Cells["HoTen"].Value?.ToString() ?? maNV);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    LoadNV();
            };

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
                    if (!ClinicDAL.XoaNhanVien(maNV))
                    {
                        MessageBox.Show("Không thể vô hiệu hóa nhân viên. Vui lòng thử lại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    LoadNV();
                    MessageBox.Show("Đã vô hiệu hóa nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void ShowKhoaPhongView()
        {
            SetRefreshPageAction(ShowKhoaPhongView);
            SetActiveNav(btnNavKhoaPhong);
            SetPageHeader("Quản trị viên  ›  Khoa / Phòng khám", "Quản lý chuyên khoa và phòng ban");
            panelContent.Controls.Clear();

            var wrapper = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(10),
                BackColor = C_Bg
            };
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            wrapper.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            wrapper.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var departmentPanel = CreateSectionPanel("DANH SÁCH CHUYÊN KHOA");
            var roomPanel = CreateSectionPanel("DANH SÁCH PHÒNG KHÁM");
            var departments = MakeGrid(DockStyle.None);
            var rooms = MakeGrid(DockStyle.None);
            departmentPanel.Controls.Add(departments);
            roomPanel.Controls.Add(rooms);
            wrapper.Controls.Add(departmentPanel, 0, 0);
            wrapper.Controls.Add(roomPanel, 1, 0);

            void LayoutGrids()
            {
                foreach (var section in new[] { departmentPanel, roomPanel })
                {
                    var grid = section == departmentPanel ? departments : rooms;
                    grid.Location = new Point(12, 88);
                    grid.Size = new Size(Math.Max(0, section.ClientSize.Width - 24), Math.Max(0, section.ClientSize.Height - 100));
                    grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                }
            }

            departmentPanel.SizeChanged += (s, e) => LayoutGrids();
            roomPanel.SizeChanged += (s, e) => LayoutGrids();

            void LoadData()
            {
                departments.DataSource = ClinicDAL.LayDanhSachKhoaQuanTri();
                rooms.DataSource = ClinicDAL.LayDanhSachPhongKhamQuanTri();
                LayoutGrids();
            }

            void RunAdminAction(Action action)
            {
                try
                {
                    action();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể cập nhật khoa / phòng",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            var departmentButtons = new FlowLayoutPanel
            {
                Location = new Point(12, 42),
                Size = new Size(510, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false
            };
            var addDepartment = PrimaryBtn("➕ Thêm", 0, 0, 105);
            var editDepartment = SecondaryBtn("✏ Sửa", 0, 0, 95, 34, C_Orange);
            var deleteDepartment = SecondaryBtn("🗑 Xóa", 0, 0, 95, 34, C_Red);
            var refreshDepartments = SecondaryBtn("↻ Làm mới", 0, 0, 110, 34, C_Blue);
            departmentButtons.Controls.AddRange(new Control[] { addDepartment, editDepartment, deleteDepartment, refreshDepartments });
            departmentPanel.Controls.Add(departmentButtons);

            var roomButtons = new FlowLayoutPanel
            {
                Location = new Point(12, 42),
                Size = new Size(510, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                WrapContents = false
            };
            var addRoom = PrimaryBtn("➕ Thêm", 0, 0, 105);
            var editRoom = SecondaryBtn("✏ Sửa", 0, 0, 95, 34, C_Orange);
            var deleteRoom = SecondaryBtn("🗑 Xóa", 0, 0, 95, 34, C_Red);
            var refreshRooms = SecondaryBtn("↻ Làm mới", 0, 0, 110, 34, C_Blue);
            roomButtons.Controls.AddRange(new Control[] { addRoom, editRoom, deleteRoom, refreshRooms });
            roomPanel.Controls.Add(roomButtons);

            DataRow? GetSelected(DataGridView grid)
                => grid.SelectedRows.Count > 0 && grid.SelectedRows[0].DataBoundItem is DataRowView rowView
                    ? rowView.Row
                    : null;

            addDepartment.Click += (s, e) =>
            {
                using var dialog = new FormThemSuaKhoaPhong(isRoom: false, isNew: true);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    RunAdminAction(() => ClinicDAL.LuuKhoa(dialog.Ma, dialog.Ten, isNew: true));
            };
            editDepartment.Click += (s, e) =>
            {
                DataRow? selected = GetSelected(departments);
                if (selected == null)
                {
                    MessageBox.Show("Chọn chuyên khoa cần sửa.", "Chưa chọn dữ liệu",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using var dialog = new FormThemSuaKhoaPhong(isRoom: false, isNew: false, selected);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    RunAdminAction(() => ClinicDAL.LuuKhoa(dialog.Ma, dialog.Ten, isNew: false));
            };
            deleteDepartment.Click += (s, e) =>
            {
                DataRow? selected = GetSelected(departments);
                if (selected == null)
                {
                    MessageBox.Show("Chọn chuyên khoa cần xóa.", "Chưa chọn dữ liệu",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string maKhoa = selected["MaKhoa"].ToString() ?? "";
                if (MessageBox.Show($"Xóa chuyên khoa '{selected["TenKhoa"]}'?", "Xác nhận xóa",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    RunAdminAction(() => ClinicDAL.XoaKhoa(maKhoa));
            };
            refreshDepartments.Click += (s, e) => LoadData();

            addRoom.Click += (s, e) =>
            {
                using var dialog = new FormThemSuaKhoaPhong(isRoom: true, isNew: true);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    RunAdminAction(() => ClinicDAL.LuuPhongKham(dialog.Ma, dialog.Ten, dialog.MaKhoa, isNew: true));
            };
            editRoom.Click += (s, e) =>
            {
                DataRow? selected = GetSelected(rooms);
                if (selected == null)
                {
                    MessageBox.Show("Chọn phòng khám cần sửa.", "Chưa chọn dữ liệu",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using var dialog = new FormThemSuaKhoaPhong(isRoom: true, isNew: false, selected);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    RunAdminAction(() => ClinicDAL.LuuPhongKham(dialog.Ma, dialog.Ten, dialog.MaKhoa, isNew: false));
            };
            deleteRoom.Click += (s, e) =>
            {
                DataRow? selected = GetSelected(rooms);
                if (selected == null)
                {
                    MessageBox.Show("Chọn phòng khám cần xóa.", "Chưa chọn dữ liệu",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string maPhong = selected["MaPhong"].ToString() ?? "";
                if (MessageBox.Show($"Xóa phòng '{selected["TenPhong"]}'?", "Xác nhận xóa",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    RunAdminAction(() => ClinicDAL.XoaPhongKham(maPhong));
            };
            refreshRooms.Click += (s, e) => LoadData();

            LoadData();
            panelContent.Controls.Add(wrapper);
        }

        private static Panel CreateSectionPanel(string title)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(6),
                BackColor = C_Card
            };
            panel.Paint += PaintCardBorder;
            panel.Controls.Add(MakeLbl(title, 14, 13, bold: true));
            return panel;
        }

        // ═══════════════════════════════════════════════════════════
        //  ADMIN: BÁO CÁO & THỐNG KÊ
        // ═══════════════════════════════════════════════════════════
        private void ShowBaoCaoView()
        {
            SetRefreshPageAction(ShowBaoCaoView);
            SetActiveNav(btnNavBaoCao);
            SetPageHeader("Quản trị viên  ›  Báo cáo", "Báo cáo thống kê hoạt động");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            var (kham, thu, thuocSapHet) = ClinicDAL.LayThongKeTongQuan();
            int tongBN = ClinicDAL.LayDanhSachBenhNhan().Rows.Count;

            var pCards = new Panel { Location = new Point(10, 10), Size = new Size(1080, 115) };
            pCards.Controls.Add(MakeKpiCard("LƯỢT KHÁM HÔM NAY", $"{kham} lượt", "Phiếu khám đăng ký hôm nay", C_Teal, 0, 0, 250));
            pCards.Controls.Add(MakeKpiCard("DOANH THU HÔM NAY", $"{thu:N0} đ", "Hóa đơn đã thanh toán hôm nay", C_Green, 265, 0, 250));
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

            var pnlKhoaPhong = new Panel { Location = new Point(10, 685), Size = new Size(1080, 220), BackColor = C_Card };
            pnlKhoaPhong.Paint += PaintCardBorder;
            pnlKhoaPhong.Controls.Add(MakeLbl("LƯỢT KHÁM THEO KHOA / PHÒNG (30 NGÀY)", 14, 12, bold: true));
            var dgvKhoaPhong = MakeGrid(DockStyle.None);
            dgvKhoaPhong.Location = new Point(14, 38);
            dgvKhoaPhong.Size = new Size(1052, 168);
            if (ClinicDAL.CoSchemaKhoaPhong())
            {
                dgvKhoaPhong.DataSource = ClinicDAL.LayThongKeKhoaPhong(30);
                pnlKhoaPhong.Controls.Add(dgvKhoaPhong);
            }
            else
            {
                var lblSchemaWarning = MakeLbl(
                    "Database chưa có cấu trúc khoa/phòng. Hãy chạy seed_test_data.sql trên đúng database ứng dụng đang kết nối.",
                    14, 48);
                lblSchemaWarning.ForeColor = C_Red;
                lblSchemaWarning.AutoSize = false;
                lblSchemaWarning.Size = new Size(1040, 50);
                pnlKhoaPhong.Controls.Add(lblSchemaWarning);
            }

            wrapper.Controls.AddRange(new Control[] { pCards, pnlDT, pnlCD, pnlTH, pnlKhoaPhong });
            panelContent.Controls.Add(wrapper);
            ConfigureAdminSummaryLayout(wrapper, pCards, pnlDT, dgvDT, pnlCD, dgvCD, pnlTH, dgvTH, pnlKhoaPhong, dgvKhoaPhong);
        }
    }
}
