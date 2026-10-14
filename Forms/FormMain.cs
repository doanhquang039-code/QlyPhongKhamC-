using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public class FormMain : Form
    {
        private Panel panelSidebar = null!;
        private Panel panelContent = null!;
        private Button btnMenuTiepDon = null!;
        private Button btnMenuKhamBenh = null!;
        private Button btnMenuKhoThuoc = null!;
        private Button btnMenuThuNgan = null!;
        private Button btnMenuNhanVien = null!;
        private Button btnMenuThongKe = null!;
        private Button btnMenuBenhNhan = null!;
        private Button btnLogout = null!;

        public FormMain()
        {
            InitializeComponent();
            ApplyRolePermissions();
        }

        private void InitializeComponent()
        {
            this.Text = "HỆ THỐNG QUẢN LÝ PHÒNG KHÁM - doanhbtlc#";
            this.Size = new Size(1280, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1100, 680);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // ── SIDEBAR ─────────────────────────────────────
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 230,
                BackColor = Color.FromArgb(15, 32, 54)
            };

            // Logo header
            Panel logoPanel = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.FromArgb(0, 100, 200) };
            var lblLogo = new Label
            {
                Text = "🏥 PHÒNG KHÁM",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 10),
                AutoSize = true
            };
            var lblSub = new Label
            {
                Text = "Quản lý toàn diện",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(200, 225, 255),
                Location = new Point(17, 40),
                AutoSize = true
            };
            logoPanel.Controls.AddRange(new Control[] { lblLogo, lblSub });
            panelSidebar.Controls.Add(logoPanel);

            // Thông tin người dùng
            Panel userPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(22, 45, 75),
                Padding = new Padding(10, 8, 10, 0)
            };
            var lblUser = new Label
            {
                Text = $"👤 {SessionState.CurrentUser?.HoTenNhanVien}",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 10),
                AutoSize = true
            };
            var lblRole = new Label
            {
                Text = $"Vai trò: {GetRoleDisplayName(SessionState.CurrentUser?.Quyen ?? "")}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(150, 200, 255),
                Location = new Point(12, 32),
                AutoSize = true
            };
            userPanel.Controls.AddRange(new Control[] { lblUser, lblRole });
            panelSidebar.Controls.Add(userPanel);

            // Menu buttons
            int btnY = 135;
            int step = 46;

            btnMenuBenhNhan = CreateSidebarButton("👤 Hồ sơ Bệnh nhân", btnY);
            btnMenuTiepDon = CreateSidebarButton("📋 Tiếp đón & Đăng ký", btnY + step);
            btnMenuKhamBenh = CreateSidebarButton("🩺 Phòng khám Bác sĩ", btnY + step * 2);
            btnMenuKhoThuoc = CreateSidebarButton("💊 Kho Dược phẩm", btnY + step * 3);
            btnMenuThuNgan = CreateSidebarButton("💰 Thu ngân & Hóa đơn", btnY + step * 4);
            btnMenuNhanVien = CreateSidebarButton("👥 Quản lý Nhân sự", btnY + step * 5);
            btnMenuThongKe = CreateSidebarButton("📊 Báo cáo Thống kê", btnY + step * 6);

            btnMenuBenhNhan.Click += (s, e) => ShowBenhNhanView();
            btnMenuTiepDon.Click += (s, e) => ShowTiepDonView();
            btnMenuKhamBenh.Click += (s, e) => ShowKhamBenhView();
            btnMenuKhoThuoc.Click += (s, e) => ShowKhoThuocView();
            btnMenuThuNgan.Click += (s, e) => ShowThuNganView();
            btnMenuNhanVien.Click += (s, e) => ShowNhanVienView();
            btnMenuThongKe.Click += (s, e) => ShowThongKeView();

            // Nút cấu hình DB & Đăng xuất
            var btnCfg = new Button
            {
                Text = "⚙ Cấu hình Máy chủ",
                Location = new Point(10, btnY + step * 7 + 10),
                Size = new Size(210, 36),
                BackColor = Color.FromArgb(30, 55, 90),
                ForeColor = Color.FromArgb(180, 210, 255),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnCfg.FlatAppearance.BorderSize = 0;
            btnCfg.Click += (s, e) => new FormConfigDb().ShowDialog();

            btnLogout = new Button
            {
                Text = "🚪 Đăng xuất",
                Location = new Point(10, btnY + step * 7 + 56),
                Size = new Size(210, 38),
                BackColor = Color.FromArgb(180, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogout.Click += (s, e) => { SessionState.CurrentUser = null; this.Close(); };

            panelSidebar.Controls.AddRange(new Control[] {
                btnMenuBenhNhan, btnMenuTiepDon, btnMenuKhamBenh,
                btnMenuKhoThuoc, btnMenuThuNgan, btnMenuNhanVien, btnMenuThongKe,
                btnCfg, btnLogout
            });

            // ── CONTENT ─────────────────────────────────────
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10)
            };

            this.Controls.Add(panelContent);
            this.Controls.Add(panelSidebar);
        }

        private string GetRoleDisplayName(string quyen) => quyen switch
        {
            "Admin" => "Quản trị viên",
            "BacSi" => "Bác sĩ khám bệnh",
            "ThuNgan" => "Thu ngân viên",
            "LeTan" => "Lễ tân tiếp đón",
            _ => quyen
        };

        private Button CreateSidebarButton(string text, int top)
        {
            var btn = new Button
            {
                Text = "  " + text,
                Location = new Point(10, top),
                Size = new Size(210, 40),
                BackColor = Color.FromArgb(22, 45, 75),
                ForeColor = Color.FromArgb(210, 225, 245),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => { if (btn.BackColor != Color.FromArgb(0, 100, 200)) btn.BackColor = Color.FromArgb(35, 70, 110); };
            btn.MouseLeave += (s, e) => { if (btn.BackColor != Color.FromArgb(0, 100, 200)) btn.BackColor = Color.FromArgb(22, 45, 75); };
            return btn;
        }

        private void HighlightMenu(Button activeBtn)
        {
            Button[] menuBtns = { btnMenuBenhNhan, btnMenuTiepDon, btnMenuKhamBenh, btnMenuKhoThuoc, btnMenuThuNgan, btnMenuNhanVien, btnMenuThongKe };
            foreach (var b in menuBtns) b.BackColor = Color.FromArgb(22, 45, 75);
            activeBtn.BackColor = Color.FromArgb(0, 100, 200);
        }

        private void ApplyRolePermissions()
        {
            if (SessionState.IsDoctor)
            {
                btnMenuBenhNhan.Enabled = false;
                btnMenuTiepDon.Enabled = false;
                btnMenuThuNgan.Enabled = false;
                btnMenuNhanVien.Enabled = false;
                btnMenuThongKe.Enabled = false;
                ShowKhamBenhView();
            }
            else if (SessionState.IsReceptionist)
            {
                btnMenuKhamBenh.Enabled = false;
                btnMenuThuNgan.Enabled = false;
                btnMenuNhanVien.Enabled = false;
                btnMenuThongKe.Enabled = false;
                ShowTiepDonView();
            }
            else if (SessionState.IsCashier)
            {
                btnMenuBenhNhan.Enabled = false;
                btnMenuTiepDon.Enabled = false;
                btnMenuKhamBenh.Enabled = false;
                btnMenuNhanVien.Enabled = false;
                ShowThuNganView();
            }
            else // Admin
            {
                ShowThongKeView();
            }
        }

        // ──────────────────────────────────────────────
        // HELPER UI
        // ──────────────────────────────────────────────
        private static Label MakeLabel(string text, int x, int y, bool bold = false)
            => new Label { Text = text, Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 9F, bold ? FontStyle.Bold : FontStyle.Regular) };

        private static TextBox MakeTextBox(int x, int y, int w = 160, bool readOnly = false, string text = "")
            => new TextBox { Location = new Point(x, y), Size = new Size(w, 26), ReadOnly = readOnly, BackColor = readOnly ? Color.FromArgb(245, 245, 245) : Color.White, Text = text };

        private static Button MakeButton(string text, int x, int y, int w, int h, Color backColor, Color? foreColor = null)
        {
            var btn = new Button
            {
                Text = text, Location = new Point(x, y), Size = new Size(w, h),
                BackColor = backColor, ForeColor = foreColor ?? Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Panel MakePageHeader(string title, string subtitle = "")
        {
            var p = new Panel { Dock = DockStyle.Top, Height = 56, BackColor = Color.White };
            p.Controls.Add(new Label { Text = title, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 35, 65), Location = new Point(16, 10), AutoSize = true });
            if (!string.IsNullOrEmpty(subtitle))
                p.Controls.Add(new Label { Text = subtitle, Font = new Font("Segoe UI", 8.5F), ForeColor = Color.Gray, Location = new Point(18, 34), AutoSize = true });
            var sep = new Panel { Dock = DockStyle.Bottom, Height = 2, BackColor = Color.FromArgb(0, 100, 200) };
            p.Controls.Add(sep);
            return p;
        }

        private static DataGridView MakeGrid(DockStyle dock = DockStyle.Fill)
        {
            var dgv = new DataGridView
            {
                Dock = dock,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                Font = new Font("Segoe UI", 9F)
            };
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 228, 255);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 35, 65);
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 252, 255);
            return dgv;
        }

        // ──────────────────────────────────────────────────────────────
        // VIEW 0: HỒ SƠ BỆNH NHÂN (Lễ tân + Admin)
        // ──────────────────────────────────────────────────────────────
        private void ShowBenhNhanView()
        {
            HighlightMenu(btnMenuBenhNhan);
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            wrapper.Controls.Add(MakePageHeader("👤 Hồ sơ Bệnh nhân", "Tìm kiếm, thêm mới, sửa và xem lịch sử khám"));

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.White, Padding = new Padding(10, 7, 10, 0) };
            var txtSearch = new TextBox { Location = new Point(10, 8), Size = new Size(280, 26), Text = "" };
            toolPanel.Controls.Add(new Label { Text = "🔍 Tìm kiếm:", Location = new Point(10, 11), AutoSize = true, Font = new Font("Segoe UI", 9F) });
            txtSearch.Location = new Point(90, 8);
            var btnSearch = MakeButton("Tìm", 385, 7, 60, 28, Color.FromArgb(0, 122, 255));
            var btnAdd = MakeButton("➕ Thêm BN mới", 460, 7, 140, 28, Color.FromArgb(40, 167, 69));
            var btnEdit = MakeButton("✏ Sửa", 610, 7, 80, 28, Color.FromArgb(255, 140, 0));
            var btnDelete = MakeButton("🗑 Xóa", 700, 7, 80, 28, Color.FromArgb(220, 53, 69));
            var btnHistory = MakeButton("📋 Lịch sử khám", 790, 7, 150, 28, Color.FromArgb(60, 60, 160));
            toolPanel.Controls.AddRange(new Control[] { txtSearch, btnSearch, btnAdd, btnEdit, btnDelete, btnHistory });

            var dgvBN = MakeGrid();
            var contentPanel = new Panel { Dock = DockStyle.Fill };
            contentPanel.Controls.Add(dgvBN);

            void LoadBN() => dgvBN.DataSource = ClinicDAL.LayDanhSachBenhNhan(txtSearch.Text.Trim());
            LoadBN();
            btnSearch.Click += (s, e) => LoadBN();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadBN(); };

            btnAdd.Click += (s, e) =>
            {
                var dlg = new FormThemSuaBN(null);
                if (dlg.ShowDialog() == DialogResult.OK) LoadBN();
            };

            btnEdit.Click += (s, e) =>
            {
                if (dgvBN.SelectedRows.Count == 0) { MessageBox.Show("Chọn một bệnh nhân để sửa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var row = dgvBN.SelectedRows[0];
                var bn = new BenhNhan
                {
                    MaBN = row.Cells["MaBN"].Value?.ToString() ?? "",
                    HoTen = row.Cells["HoTen"].Value?.ToString() ?? "",
                    GioiTinh = row.Cells["GioiTinh"].Value?.ToString() ?? "Nam",
                    SoDienThoai = row.Cells["SoDienThoai"].Value?.ToString(),
                    DiaChi = row.Cells["DiaChi"].Value?.ToString(),
                    TienSuBenh = row.Cells["TienSuBenh"].Value?.ToString(),
                };
                if (DateTime.TryParse(row.Cells["NgaySinh"].Value?.ToString(), out DateTime ns)) bn.NgaySinh = ns;
                var dlg = new FormThemSuaBN(bn);
                if (dlg.ShowDialog() == DialogResult.OK) LoadBN();
            };

            btnDelete.Click += (s, e) =>
            {
                if (dgvBN.SelectedRows.Count == 0) return;
                string maBN = dgvBN.SelectedRows[0].Cells["MaBN"].Value?.ToString() ?? "";
                string tenBN = dgvBN.SelectedRows[0].Cells["HoTen"].Value?.ToString() ?? "";
                if (MessageBox.Show($"Xóa bệnh nhân '{tenBN}'?\n(Chỉ xóa được nếu chưa có phiếu khám)", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (ClinicDAL.XoaBenhNhan(maBN, out string err))
                    { MessageBox.Show("Đã xóa bệnh nhân!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information); LoadBN(); }
                    else MessageBox.Show("Không thể xóa: " + err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnHistory.Click += (s, e) =>
            {
                if (dgvBN.SelectedRows.Count == 0) { MessageBox.Show("Chọn một bệnh nhân!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                string maBN = dgvBN.SelectedRows[0].Cells["MaBN"].Value?.ToString() ?? "";
                string tenBN = dgvBN.SelectedRows[0].Cells["HoTen"].Value?.ToString() ?? "";
                var dlg = new FormLichSuKham(maBN, tenBN);
                dlg.ShowDialog();
            };

            wrapper.Controls.Add(contentPanel);
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ──────────────────────────────────────────────────────────────
        // VIEW 1: TIẾP ĐÓN & HÀNG CHỜ (Lễ tân)
        // ──────────────────────────────────────────────────────────────
        private void ShowTiepDonView()
        {
            HighlightMenu(btnMenuTiepDon);
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            wrapper.Controls.Add(MakePageHeader("📋 Tiếp đón & Cấp số thứ tự", "Đăng ký khám mới và quản lý hàng chờ trong ngày"));

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 200 };

            // ─ Panel đăng ký ─
            var grp = new GroupBox { Text = "ĐĂNG KÝ KHÁM & CẤP SỐ THỨ TỰ", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var pnlForm = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            // Row 1
            var lblMaBN = MakeLabel("Mã BN:", 10, 15); var txtMaBN = MakeTextBox(65, 12, 90, true);
            var lblHoTen = MakeLabel("Họ tên (*):", 170, 15); var txtHoTen = MakeTextBox(250, 12, 200);
            var lblGT = MakeLabel("Giới tính:", 465, 15);
            var cboGT = new ComboBox { Location = new Point(535, 12), Size = new Size(75, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboGT.Items.AddRange(new object[] { "Nam", "Nữ" }); cboGT.SelectedIndex = 0;
            var lblNS = MakeLabel("Ngày sinh:", 625, 15);
            var dtpNS = new DateTimePicker { Location = new Point(700, 12), Size = new Size(130, 26), Format = DateTimePickerFormat.Short };
            var lblSDT = MakeLabel("Điện thoại:", 845, 15); var txtSDT = MakeTextBox(920, 12, 130);

            // Row 2
            var lblDC = MakeLabel("Địa chỉ:", 10, 52); var txtDC = MakeTextBox(65, 49, 400);
            var lblTS = MakeLabel("Tiền sử bệnh:", 480, 52); var txtTS = MakeTextBox(570, 49, 340);

            // Row 3
            var lblBS = MakeLabel("Bác sĩ khám (*):", 10, 90);
            var cboBacSi = new ComboBox { Location = new Point(115, 87), Size = new Size(230, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            var dtBS = ClinicDAL.LayDanhSachBacSi();
            cboBacSi.DataSource = dtBS; cboBacSi.DisplayMember = "HoTen"; cboBacSi.ValueMember = "MaNV";
            var lblTC = MakeLabel("Triệu chứng ban đầu:", 360, 90); var txtTC = MakeTextBox(500, 87, 360);

            var lblSTT = new Label
            {
                Text = $"🎫 STT hôm nay: #{ClinicDAL.LaySTTTiepTheoTrongNgay()}",
                Location = new Point(10, 128), AutoSize = true,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.FromArgb(0, 100, 200)
            };
            var btnCap = MakeButton("🎟 CẤP PHIẾU KHÁM", 300, 123, 200, 36, Color.FromArgb(0, 122, 255));
            var btnTaiKham = MakeButton("🔁 Tái khám (BN cũ)", 515, 123, 180, 36, Color.FromArgb(40, 167, 69));
            var btnReset = MakeButton("🔄 Làm mới", 710, 123, 110, 36, Color.FromArgb(108, 117, 125));

            txtMaBN.Text = ClinicDAL.SinhMaBenhNhan();

            btnReset.Click += (s, e) =>
            {
                txtMaBN.Text = ClinicDAL.SinhMaBenhNhan();
                txtHoTen.Text = txtSDT.Text = txtDC.Text = txtTS.Text = txtTC.Text = "";
                dtpNS.Value = DateTime.Today.AddYears(-30);
                cboGT.SelectedIndex = 0;
                lblSTT.Text = $"🎫 STT hôm nay: #{ClinicDAL.LaySTTTiepTheoTrongNgay()}";
            };

            // Tái khám: tìm BN cũ theo SĐT
            btnTaiKham.Click += (s, e) =>
            {
                string sdt = Microsoft.VisualBasic.Interaction.InputBox("Nhập số điện thoại bệnh nhân cũ để tìm kiếm:", "Tái khám - Tìm BN cũ", "");
                if (string.IsNullOrWhiteSpace(sdt)) return;
                var dt = ClinicDAL.LayDanhSachBenhNhan(sdt);
                if (dt.Rows.Count == 0) { MessageBox.Show("Không tìm thấy bệnh nhân với SĐT này!", "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var row = dt.Rows[0];
                txtMaBN.Text = row["MaBN"].ToString();
                txtHoTen.Text = row["HoTen"].ToString();
                cboGT.SelectedItem = row["GioiTinh"].ToString();
                if (DateTime.TryParse(row["NgaySinh"].ToString(), out DateTime ns)) dtpNS.Value = ns;
                txtSDT.Text = row["SoDienThoai"].ToString();
                txtDC.Text = row["DiaChi"].ToString();
                txtTS.Text = row["TienSuBenh"].ToString();
                MessageBox.Show($"Đã tải thông tin: {row["HoTen"]}\nBấm 'CẤP PHIẾU KHÁM' để tạo phiếu tái khám.", "Tìm thấy bệnh nhân", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            DataGridView dgvHangCho = MakeGrid();
            void LoadHangCho() => dgvHangCho.DataSource = ClinicDAL.LayHangChoKhamTrongNgay();

            btnCap.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtHoTen.Text)) { MessageBox.Show("Vui lòng nhập họ tên bệnh nhân!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (cboBacSi.DataSource == null || cboBacSi.SelectedValue == null) { MessageBox.Show("Vui lòng chọn bác sĩ!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                bool isNewBN = !txtMaBN.Text.StartsWith("BN") ||
                    !ClinicDAL.LayDanhSachBenhNhan(txtMaBN.Text).Rows.Cast<DataRow>().Any(r => r["MaBN"].ToString() == txtMaBN.Text);

                var bn = new BenhNhan
                {
                    MaBN = txtMaBN.Text.Trim(),
                    HoTen = txtHoTen.Text.Trim(),
                    GioiTinh = cboGT.SelectedItem?.ToString() ?? "Nam",
                    NgaySinh = dtpNS.Value,
                    SoDienThoai = txtSDT.Text.Trim(),
                    DiaChi = txtDC.Text.Trim(),
                    TienSuBenh = txtTS.Text.Trim()
                };

                if (isNewBN) ClinicDAL.LuuBenhNhan(bn, isNew: true);
                else ClinicDAL.LuuBenhNhan(bn, isNew: false);

                int stt = ClinicDAL.LaySTTTiepTheoTrongNgay();
                var pk = new PhieuKham
                {
                    MaPK = ClinicDAL.SinhMaPhieuKham(),
                    STT = stt,
                    MaBN = bn.MaBN,
                    MaNV_BacSi = cboBacSi.SelectedValue?.ToString() ?? "",
                    TrieuChung = txtTC.Text.Trim()
                };
                ClinicDAL.TaoPhieuKham(pk);

                MessageBox.Show($"✅ Cấp phiếu thành công!\n\n📄 Mã phiếu: {pk.MaPK}\n🎫 Số thứ tự: {stt}\n👤 Bệnh nhân: {bn.HoTen}",
                    "Cấp phiếu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtMaBN.Text = ClinicDAL.SinhMaBenhNhan();
                txtHoTen.Text = txtSDT.Text = txtDC.Text = txtTS.Text = txtTC.Text = "";
                lblSTT.Text = $"🎫 STT hôm nay: #{ClinicDAL.LaySTTTiepTheoTrongNgay()}";
                LoadHangCho();
            };

            pnlForm.Controls.AddRange(new Control[] {
                lblMaBN, txtMaBN, lblHoTen, txtHoTen, lblGT, cboGT, lblNS, dtpNS, lblSDT, txtSDT,
                lblDC, txtDC, lblTS, txtTS,
                lblBS, cboBacSi, lblTC, txtTC,
                lblSTT, btnCap, btnTaiKham, btnReset
            });
            grp.Controls.Add(pnlForm);

            // ─ Hàng chờ ─
            var grpCho = new GroupBox { Text = "HÀNG CHỜ KHÁM HÔM NAY", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            LoadHangCho();
            grpCho.Controls.Add(dgvHangCho);

            split.Panel1.Controls.Add(grp);
            split.Panel2.Controls.Add(grpCho);
            wrapper.Controls.Add(split);
            panelContent.Controls.Add(wrapper);
        }

        // ──────────────────────────────────────────────────────────────
        // VIEW 2: PHÒNG KHÁM BÁC SĨ
        // ──────────────────────────────────────────────────────────────
        private void ShowKhamBenhView()
        {
            HighlightMenu(btnMenuKhamBenh);
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            wrapper.Controls.Add(MakePageHeader("🩺 Phòng khám Bác sĩ", "Chẩn đoán, chỉ định dịch vụ CLS và kê đơn thuốc"));

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 370 };

            // ─ Left: Danh sách hàng chờ ─
            var grpList = new GroupBox { Text = "DANH SÁCH BỆNH NHÂN CHỜ KHÁM", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var dgvCho = MakeGrid();
            string? maBS = SessionState.IsDoctor ? SessionState.CurrentUser?.MaNV : null;
            void LoadCho() => dgvCho.DataSource = ClinicDAL.LayHangChoKhamTrongNgay(maBS);
            LoadCho();
            var btnRefresh = MakeButton("🔄 Làm mới", 5, 5, 120, 28, Color.FromArgb(108, 117, 125));
            btnRefresh.Click += (s, e) => LoadCho();
            var pL = new Panel { Dock = DockStyle.Fill };
            pL.Controls.Add(dgvCho);
            var pLT = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.White, Padding = new Padding(5, 5, 5, 0) };
            pLT.Controls.Add(btnRefresh);
            grpList.Controls.Add(pL);
            grpList.Controls.Add(pLT);

            // ─ Right: Chi tiết khám ─
            var grpDetail = new GroupBox { Text = "THÔNG TIN KHÁM BỆNH & KÊ ĐƠN", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var tabKham = new TabControl { Dock = DockStyle.Fill };

            // Tab 1: Chẩn đoán
            var tabChanDoan = new TabPage("  📝 Chẩn đoán  ");
            var txtMaPK = MakeTextBox(80, 20, 100, true);
            var txtTenBN = MakeTextBox(255, 20, 200, true);
            var txtTuoi = MakeTextBox(490, 20, 60, true);
            var txtGioiTinh = MakeTextBox(580, 20, 60, true);
            var txtChanDoan = MakeTextBox(130, 60, 500);
            var txtLoiDan = MakeTextBox(130, 95, 500);
            var txtTrieuChung = MakeTextBox(130, 130, 500, true);
            var cbTrangThai = new ComboBox { Location = new Point(130, 165), Size = new Size(200, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cbTrangThai.Items.AddRange(new object[] { "Chờ khám", "Đang khám", "Chờ kết quả CLS", "Đã khám xong" });
            cbTrangThai.SelectedIndex = 1;
            var btnLuuKham = MakeButton("💾 LƯU CHẨN ĐOÁN", 340, 162, 200, 34, Color.FromArgb(0, 122, 255));

            tabChanDoan.Controls.AddRange(new Control[] {
                MakeLabel("Mã PK:", 10, 23), txtMaPK,
                MakeLabel("Bệnh nhân:", 185, 23), txtTenBN,
                MakeLabel("Tuổi:", 460, 23), txtTuoi,
                MakeLabel("GT:", 555, 23), txtGioiTinh,
                MakeLabel("Chẩn đoán:", 10, 63), txtChanDoan,
                MakeLabel("Lời dặn:", 10, 98), txtLoiDan,
                MakeLabel("Triệu chứng:", 10, 133), txtTrieuChung,
                MakeLabel("Trạng thái:", 10, 168), cbTrangThai, btnLuuKham
            });

            btnLuuKham.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK.Text)) { MessageBox.Show("Chọn bệnh nhân từ danh sách bên trái!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                ClinicDAL.CapNhatKetQuaKham(txtMaPK.Text, txtChanDoan.Text.Trim(), txtLoiDan.Text.Trim(), cbTrangThai.SelectedItem?.ToString() ?? "Đang khám");
                MessageBox.Show("✅ Đã lưu chẩn đoán!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (cbTrangThai.SelectedItem?.ToString() == "Đã khám xong") LoadCho();
            };

            // Tab 2: Chỉ định dịch vụ CLS
            var tabCLS = new TabPage("  🔬 Dịch vụ CLS  ");
            var dgvCLS = MakeGrid();
            void LoadCLS() => dgvCLS.DataSource = string.IsNullOrEmpty(txtMaPK.Text) ? null : ClinicDAL.LayChiDinhDVTheoPhieuKham(txtMaPK.Text);
            var cboDV = new ComboBox { Location = new Point(10, 5), Size = new Size(250, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            var dtDV = ClinicDAL.LayDanhSachDichVu();
            cboDV.DataSource = dtDV; cboDV.DisplayMember = "TenDV"; cboDV.ValueMember = "MaDV";
            var btnChiDinh = MakeButton("➕ Chỉ định DV", 270, 4, 140, 28, Color.FromArgb(40, 167, 69));
            var btnNhapKQ = MakeButton("📝 Nhập kết quả", 420, 4, 150, 28, Color.FromArgb(255, 140, 0));
            var pCLS = new Panel { Dock = DockStyle.Fill }; pCLS.Controls.Add(dgvCLS);
            var pCLSTop = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.White, Padding = new Padding(5, 5, 5, 0) };
            pCLSTop.Controls.AddRange(new Control[] { cboDV, btnChiDinh, btnNhapKQ });
            tabCLS.Controls.Add(pCLS);
            tabCLS.Controls.Add(pCLSTop);

            btnChiDinh.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK.Text)) { MessageBox.Show("Chọn bệnh nhân trước!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (cboDV.SelectedItem is DataRowView drv)
                {
                    decimal donGia = Convert.ToDecimal(drv["DonGia"]);
                    ClinicDAL.ChiDinhDichVu(txtMaPK.Text, cboDV.SelectedValue?.ToString() ?? "", donGia);
                    LoadCLS();
                    MessageBox.Show($"✅ Đã chỉ định: {drv["TenDV"]} - {donGia:N0} đ", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            btnNhapKQ.Click += (s, e) =>
            {
                if (dgvCLS.SelectedRows.Count == 0) { MessageBox.Show("Chọn một dịch vụ CLS trong bảng!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                int maChiDinh = Convert.ToInt32(dgvCLS.SelectedRows[0].Cells["MaChiDinh"].Value);
                string tenDV = dgvCLS.SelectedRows[0].Cells["TenDV"].Value?.ToString() ?? "";
                string kq = Microsoft.VisualBasic.Interaction.InputBox($"Nhập kết quả cho dịch vụ:\n{tenDV}", "Kết quả CLS", "Bình thường");
                if (!string.IsNullOrEmpty(kq)) { ClinicDAL.CapNhatKetQuaDV(maChiDinh, kq); LoadCLS(); }
            };

            // Tab 3: Đơn thuốc
            var tabThuoc = new TabPage("  💊 Đơn thuốc  ");
            var dgvDon = MakeGrid();
            void LoadDon() => dgvDon.DataSource = string.IsNullOrEmpty(txtMaPK.Text) ? null : ClinicDAL.LayChiTietDonThuocTheoPK(txtMaPK.Text);

            var cboThuoc = new ComboBox { Location = new Point(10, 5), Size = new Size(220, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            var dtThuoc = ClinicDAL.LayDanhSachThuoc();
            cboThuoc.DataSource = dtThuoc; cboThuoc.DisplayMember = "TenThuoc"; cboThuoc.ValueMember = "MaThuoc";
            var lblSL = MakeLabel("SL:", 240, 8);
            var numSL = new NumericUpDown { Location = new Point(260, 5), Size = new Size(60, 26), Minimum = 1, Maximum = 9999, Value = 10 };
            var lblCD = MakeLabel("Cách dùng:", 330, 8);
            var txtCD = MakeTextBox(405, 5, 240, false, "Uống 1v x 2 lần/ngày sau ăn");
            var btnKeThuoc = MakeButton("➕ Kê thuốc", 655, 4, 120, 28, Color.FromArgb(40, 167, 69));
            var btnXoaThuoc = MakeButton("🗑 Bỏ thuốc", 785, 4, 120, 28, Color.FromArgb(220, 53, 69));

            var pDonTop = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.White, Padding = new Padding(5, 5, 5, 0) };
            pDonTop.Controls.AddRange(new Control[] { cboThuoc, lblSL, numSL, lblCD, txtCD, btnKeThuoc, btnXoaThuoc });
            var pDon = new Panel { Dock = DockStyle.Fill }; pDon.Controls.Add(dgvDon);
            tabThuoc.Controls.Add(pDon);
            tabThuoc.Controls.Add(pDonTop);

            btnKeThuoc.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK.Text)) { MessageBox.Show("Chọn bệnh nhân trước!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (cboThuoc.SelectedItem is DataRowView drv)
                {
                    var list = new System.Collections.Generic.List<ChiTietDonThuoc>
                    {
                        new ChiTietDonThuoc { MaThuoc = drv["MaThuoc"].ToString()!, TenThuoc = drv["TenThuoc"].ToString()!, SoLuong = (int)numSL.Value, DonGiaThoiDiem = Convert.ToDecimal(drv["DonGiaBan"]), CachDung = txtCD.Text.Trim() }
                    };
                    if (ClinicDAL.KeDonThuoc(txtMaPK.Text, "", list, out string err))
                    { MessageBox.Show($"✅ Đã kê thuốc & cập nhật tồn kho!\nTồn còn: {Convert.ToInt32(drv["SoLuongTon"]) - (int)numSL.Value}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information); LoadDon(); }
                    else MessageBox.Show("❌ " + err, "Không thể kê thuốc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnXoaThuoc.Click += (s, e) =>
            {
                if (dgvDon.SelectedRows.Count == 0) { MessageBox.Show("Chọn một dòng thuốc để bỏ!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                int maCTDT = Convert.ToInt32(dgvDon.SelectedRows[0].Cells["MaCTDT"].Value);
                string maThuoc = dgvDon.SelectedRows[0].Cells["MaThuoc"].Value?.ToString() ?? "";
                int soLuong = Convert.ToInt32(dgvDon.SelectedRows[0].Cells["SoLuong"].Value);
                if (MessageBox.Show("Bỏ thuốc này khỏi đơn và hoàn trả tồn kho?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    if (ClinicDAL.XoaThuocKhoiDon(maCTDT, maThuoc, soLuong, out string err)) LoadDon();
                    else MessageBox.Show("Lỗi: " + err, "Thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            tabKham.TabPages.AddRange(new[] { tabChanDoan, tabCLS, tabThuoc });
            grpDetail.Controls.Add(tabKham);

            // Khi chọn bệnh nhân ở grid trái -> load toàn bộ thông tin
            dgvCho.SelectionChanged += (s, e) =>
            {
                if (dgvCho.SelectedRows.Count == 0) return;
                var r = dgvCho.SelectedRows[0];
                txtMaPK.Text = r.Cells["MaPK"].Value?.ToString() ?? "";
                txtTenBN.Text = r.Cells["TenBN"].Value?.ToString() ?? "";
                txtTuoi.Text = r.Cells["Tuoi"].Value?.ToString() ?? "";
                txtGioiTinh.Text = r.Cells["GioiTinh"].Value?.ToString() ?? "";
                txtTrieuChung.Text = r.Cells["TrieuChung"].Value?.ToString() ?? "";
                cbTrangThai.SelectedItem = "Đang khám";
                LoadCLS(); LoadDon();
            };

            split.Panel1.Controls.Add(grpList);
            split.Panel2.Controls.Add(grpDetail);
            wrapper.Controls.Add(split);
            panelContent.Controls.Add(wrapper);
        }

        // ──────────────────────────────────────────────────────────────
        // VIEW 3: KHO DƯỢC PHẨM
        // ──────────────────────────────────────────────────────────────
        private void ShowKhoThuocView()
        {
            HighlightMenu(btnMenuKhoThuoc);
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            wrapper.Controls.Add(MakePageHeader("💊 Kho Dược phẩm", "Quản lý danh mục thuốc, tồn kho và nhập hàng"));

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.White, Padding = new Padding(10, 7, 10, 0) };
            var txtSearch = MakeTextBox(80, 6, 200);
            var btnSearch = MakeButton("🔍 Tìm", 290, 5, 80, 28, Color.FromArgb(0, 122, 255));
            var btnAddT = MakeButton("➕ Thêm thuốc", 385, 5, 130, 28, Color.FromArgb(40, 167, 69));
            var btnEditT = MakeButton("✏ Sửa", 525, 5, 80, 28, Color.FromArgb(255, 140, 0));
            var btnDeleteT = MakeButton("🗑 Xóa", 615, 5, 80, 28, Color.FromArgb(220, 53, 69));
            var btnNhapKho = MakeButton("📦 Nhập kho", 710, 5, 130, 28, Color.FromArgb(60, 120, 60));
            var btnCanhBao = MakeButton("⚠ Sắp hết (<30)", 850, 5, 155, 28, Color.FromArgb(200, 100, 0));
            toolPanel.Controls.AddRange(new Control[] { new Label { Text = "🔍 Tên thuốc:", Location = new Point(10, 10), AutoSize = true, Font = new Font("Segoe UI", 9F) }, txtSearch, btnSearch, btnAddT, btnEditT, btnDeleteT, btnNhapKho, btnCanhBao });

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 650 };

            // Grid thuốc
            var dgvT = MakeGrid();
            void LoadThuoc() => dgvT.DataSource = ClinicDAL.LayDanhSachThuoc(txtSearch.Text.Trim());
            LoadThuoc();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadThuoc(); };
            btnSearch.Click += (s, e) => LoadThuoc();
            split.Panel1.Controls.Add(dgvT);

            // Grid cảnh báo hết
            var grpCB = new GroupBox { Text = "⚠ THUỐC CÓ TỒN KHO THẤP (≤ 30)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var dgvCB = MakeGrid();
            dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
            grpCB.Controls.Add(dgvCB);
            split.Panel2.Controls.Add(grpCB);

            btnAddT.Click += (s, e) =>
            {
                var dlg = new FormThemSuaThuoc(null);
                if (dlg.ShowDialog() == DialogResult.OK) { LoadThuoc(); dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30); }
            };

            btnEditT.Click += (s, e) =>
            {
                if (dgvT.SelectedRows.Count == 0) { MessageBox.Show("Chọn một thuốc để sửa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var r = dgvT.SelectedRows[0];
                var t = new Thuoc
                {
                    MaThuoc = r.Cells["MaThuoc"].Value?.ToString() ?? "",
                    TenThuoc = r.Cells["TenThuoc"].Value?.ToString() ?? "",
                    DonViTinh = r.Cells["DonViTinh"].Value?.ToString() ?? "",
                    DonGiaBan = Convert.ToDecimal(r.Cells["DonGiaBan"].Value),
                    SoLuongTon = Convert.ToInt32(r.Cells["SoLuongTon"].Value),
                    CachDungMacDinh = r.Cells["CachDungMacDinh"].Value?.ToString()
                };
                var dlg = new FormThemSuaThuoc(t);
                if (dlg.ShowDialog() == DialogResult.OK) { LoadThuoc(); dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30); }
            };

            btnDeleteT.Click += (s, e) =>
            {
                if (dgvT.SelectedRows.Count == 0) return;
                string maThuoc = dgvT.SelectedRows[0].Cells["MaThuoc"].Value?.ToString() ?? "";
                string tenThuoc = dgvT.SelectedRows[0].Cells["TenThuoc"].Value?.ToString() ?? "";
                if (MessageBox.Show($"Xóa thuốc '{tenThuoc}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (ClinicDAL.XoaThuoc(maThuoc, out string err)) LoadThuoc();
                    else MessageBox.Show("Không thể xóa: " + err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnNhapKho.Click += (s, e) =>
            {
                if (dgvT.SelectedRows.Count == 0) { MessageBox.Show("Chọn thuốc cần nhập kho!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                string maThuoc = dgvT.SelectedRows[0].Cells["MaThuoc"].Value?.ToString() ?? "";
                string tenThuoc = dgvT.SelectedRows[0].Cells["TenThuoc"].Value?.ToString() ?? "";
                string input = Microsoft.VisualBasic.Interaction.InputBox($"Nhập số lượng cần bổ sung vào kho:\n{tenThuoc}", "Nhập kho dược", "100");
                if (int.TryParse(input, out int sl) && sl > 0)
                {
                    ClinicDAL.NhapKho(maThuoc, sl);
                    MessageBox.Show($"✅ Đã nhập thêm {sl} {dgvT.SelectedRows[0].Cells["DonViTinh"].Value} vào kho!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadThuoc(); dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
                }
            };

            btnCanhBao.Click += (s, e) => dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);

            wrapper.Controls.Add(split);
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ──────────────────────────────────────────────────────────────
        // VIEW 4: THU NGÂN & HÓA ĐƠN
        // ──────────────────────────────────────────────────────────────
        private void ShowThuNganView()
        {
            HighlightMenu(btnMenuThuNgan);
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            wrapper.Controls.Add(MakePageHeader("💰 Thu ngân & Hóa đơn", "Thanh toán viện phí và quản lý lịch sử hóa đơn"));

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 460 };

            // ─ Left: Danh sách chờ thanh toán ─
            var grpCho = new GroupBox { Text = "DANH SÁCH CHỜ THANH TOÁN (ĐÃ KHÁM XONG)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var dgvCho = MakeGrid();
            void LoadChoTT() => dgvCho.DataSource = ClinicDAL.LayDanhSachChoThanhToan();
            LoadChoTT();
            var btnRefreshTT = MakeButton("🔄 Làm mới", 5, 5, 120, 26, Color.FromArgb(108, 117, 125));
            btnRefreshTT.Click += (s, e) => LoadChoTT();
            var pCT = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.White, Padding = new Padding(5, 5, 5, 0) };
            pCT.Controls.Add(btnRefreshTT);
            grpCho.Controls.Add(new Panel { Dock = DockStyle.Fill, Controls = { dgvCho } });
            grpCho.Controls.Add(pCT);

            // ─ Right: Lập hóa đơn & Lịch sử ─
            var tabTN = new TabControl { Dock = DockStyle.Fill };

            // Tab lập hóa đơn
            var tabLap = new TabPage("  💳 Lập hóa đơn  ");
            var pLap = new Panel { Dock = DockStyle.Fill };

            var lblMaPK = MakeLabel("Mã phiếu khám:", 15, 20);
            var txtMaPK2 = MakeTextBox(135, 17, 120, true);
            var lblTenBN2 = MakeLabel("Bệnh nhân:", 270, 20);
            var txtTenBN2 = MakeTextBox(345, 17, 200, true);
            var lblChanDoan2 = MakeLabel("Chẩn đoán:", 15, 55);
            var txtChanDoan2 = MakeTextBox(95, 52, 530, true);

            // Bảng chi tiết tiền
            var panelTien = new Panel { Location = new Point(15, 88), Size = new Size(500, 120), BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White };
            var lblTienDV = new Label { Text = "Tiền dịch vụ CLS:", Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            var lblTienDVVal = new Label { Text = "0 VNĐ", Location = new Point(200, 15), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkBlue };
            var lblTienThuoc = new Label { Text = "Tiền thuốc:", Location = new Point(15, 45), AutoSize = true, Font = new Font("Segoe UI", 10F) };
            var lblTienThuocVal = new Label { Text = "0 VNĐ", Location = new Point(200, 45), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkBlue };
            var lblTong = new Label { Text = "TỔNG THANH TOÁN:", Location = new Point(15, 80), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold) };
            var lblTongVal = new Label { Text = "0 VNĐ", Location = new Point(200, 78), AutoSize = true, Font = new Font("Segoe UI", 13F, FontStyle.Bold), ForeColor = Color.Crimson };
            panelTien.Controls.AddRange(new Control[] { lblTienDV, lblTienDVVal, lblTienThuoc, lblTienThuocVal, lblTong, lblTongVal });

            var lblHT = MakeLabel("Hình thức TT:", 15, 225);
            var cboHT = new ComboBox { Location = new Point(120, 222), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboHT.Items.AddRange(new object[] { "Tiền mặt", "Chuyển khoản QR", "Thẻ" }); cboHT.SelectedIndex = 0;
            var lblGG = MakeLabel("Giảm giá (VNĐ):", 300, 225);
            var numGG = new NumericUpDown { Location = new Point(420, 222), Size = new Size(100, 26), Maximum = 10000000, Increment = 10000, Value = 0 };

            decimal curDV = 0, curThuoc = 0;
            var btnTinhTien = MakeButton("🔢 Tính chi phí", 15, 262, 160, 36, Color.FromArgb(30, 100, 200));
            var btnXuatHD = MakeButton("💳 XÁC NHẬN THANH TOÁN", 190, 262, 250, 36, Color.FromArgb(40, 167, 69));

            btnTinhTien.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK2.Text)) { MessageBox.Show("Chọn phiếu khám từ danh sách bên trái!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var (dv, th) = ClinicDAL.TinhChiPhiPhieuKham(txtMaPK2.Text);
                curDV = dv; curThuoc = th;
                lblTienDVVal.Text = $"{curDV:N0} VNĐ";
                lblTienThuocVal.Text = $"{curThuoc:N0} VNĐ";
                lblTongVal.Text = $"{(curDV + curThuoc - numGG.Value):N0} VNĐ";
                lblTongVal.ForeColor = Color.Crimson;
            };

            numGG.ValueChanged += (s, e) => { lblTongVal.Text = $"{(curDV + curThuoc - numGG.Value):N0} VNĐ"; };

            btnXuatHD.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK2.Text)) { MessageBox.Show("Chọn phiếu khám trước!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (curDV + curThuoc == 0) btnTinhTien.PerformClick();

                var hd = new HoaDon
                {
                    MaHD = ClinicDAL.SinhMaHoaDon(),
                    MaPK = txtMaPK2.Text.Trim(),
                    MaNV_ThuNgan = SessionState.CurrentUser?.MaNV ?? "NV004",
                    TienDichVu = curDV,
                    TienThuoc = curThuoc,
                    GiamGia = numGG.Value,
                    TongTien = curDV + curThuoc - numGG.Value,
                    HinhThucTT = cboHT.SelectedItem?.ToString() ?? "Tiền mặt",
                    TrangThai = "Đã thanh toán"
                };
                try
                {
                    ClinicDAL.TaoHoaDon(hd);
                    MessageBox.Show($"✅ Thanh toán thành công!\n\n📄 Mã HĐ: {hd.MaHD}\n💰 Tổng tiền: {hd.TongTien:N0} VNĐ\n📱 Hình thức: {hd.HinhThucTT}",
                        "Hóa đơn viện phí", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadChoTT();
                    txtMaPK2.Text = txtTenBN2.Text = txtChanDoan2.Text = "";
                    lblTienDVVal.Text = lblTienThuocVal.Text = "0 VNĐ"; lblTongVal.Text = "0 VNĐ";
                    curDV = curThuoc = 0;
                }
                catch (Exception ex) { MessageBox.Show("Lỗi xuất hóa đơn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            // Khi chọn từ danh sách chờ thanh toán -> điền tự động
            dgvCho.SelectionChanged += (s, e) =>
            {
                if (dgvCho.SelectedRows.Count == 0) return;
                var r = dgvCho.SelectedRows[0];
                txtMaPK2.Text = r.Cells["MaPK"].Value?.ToString() ?? "";
                txtTenBN2.Text = r.Cells["TenBN"].Value?.ToString() ?? "";
                txtChanDoan2.Text = r.Cells["ChanDoan"].Value?.ToString() ?? "";
                curDV = curThuoc = 0;
                lblTienDVVal.Text = lblTienThuocVal.Text = "0 VNĐ"; lblTongVal.Text = "0 VNĐ";
            };

            pLap.Controls.AddRange(new Control[] { lblMaPK, txtMaPK2, lblTenBN2, txtTenBN2, lblChanDoan2, txtChanDoan2, panelTien, lblHT, cboHT, lblGG, numGG, btnTinhTien, btnXuatHD });
            tabLap.Controls.Add(pLap);

            // Tab lịch sử hóa đơn
            var tabLS = new TabPage("  📋 Lịch sử HĐ  ");
            var dgvHD = MakeGrid();
            var btnRefreshHD = MakeButton("🔄 Tải lại", 5, 5, 100, 26, Color.FromArgb(108, 117, 125));
            btnRefreshHD.Click += (s, e) => dgvHD.DataSource = ClinicDAL.LayDanhSachHoaDon();
            dgvHD.DataSource = ClinicDAL.LayDanhSachHoaDon();
            var pLSTop = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = Color.White, Padding = new Padding(5, 5, 5, 0) };
            pLSTop.Controls.Add(btnRefreshHD);
            tabLS.Controls.Add(new Panel { Dock = DockStyle.Fill, Controls = { dgvHD } });
            tabLS.Controls.Add(pLSTop);

            tabTN.TabPages.AddRange(new[] { tabLap, tabLS });

            split.Panel1.Controls.Add(grpCho);
            split.Panel2.Controls.Add(tabTN);
            wrapper.Controls.Add(split);
            panelContent.Controls.Add(wrapper);
        }

        // ──────────────────────────────────────────────────────────────
        // VIEW 5: QUẢN LÝ NHÂN SỰ (Admin)
        // ──────────────────────────────────────────────────────────────
        private void ShowNhanVienView()
        {
            HighlightMenu(btnMenuNhanVien);
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            wrapper.Controls.Add(MakePageHeader("👥 Quản lý Nhân sự", "Thêm/sửa nhân viên và phân quyền tài khoản"));

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.White, Padding = new Padding(10, 7, 10, 0) };
            var btnAddNV = MakeButton("➕ Thêm nhân viên", 10, 5, 160, 28, Color.FromArgb(40, 167, 69));
            var btnEditNV = MakeButton("✏ Sửa thông tin", 180, 5, 150, 28, Color.FromArgb(255, 140, 0));
            var btnDisableNV = MakeButton("🚫 Vô hiệu hóa", 340, 5, 150, 28, Color.FromArgb(220, 53, 69));
            var btnCapTK = MakeButton("🔑 Cấp / Đặt lại mật khẩu", 500, 5, 220, 28, Color.FromArgb(60, 60, 200));
            toolPanel.Controls.AddRange(new Control[] { btnAddNV, btnEditNV, btnDisableNV, btnCapTK });

            var dgvNV = MakeGrid();
            void LoadNV() => dgvNV.DataSource = ClinicDAL.LayDanhSachNhanVien();
            LoadNV();
            var contentPanel = new Panel { Dock = DockStyle.Fill }; contentPanel.Controls.Add(dgvNV);

            btnAddNV.Click += (s, e) =>
            {
                var dlg = new FormThemSuaNhanVien(null);
                if (dlg.ShowDialog() == DialogResult.OK) LoadNV();
            };

            btnEditNV.Click += (s, e) =>
            {
                if (dgvNV.SelectedRows.Count == 0) { MessageBox.Show("Chọn nhân viên cần sửa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
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
                if (maNV == SessionState.CurrentUser?.MaNV) { MessageBox.Show("Không thể vô hiệu hóa tài khoản đang đăng nhập!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (MessageBox.Show("Vô hiệu hóa tài khoản nhân viên này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                { ClinicDAL.XoaNhanVien(maNV); LoadNV(); }
            };

            btnCapTK.Click += (s, e) =>
            {
                if (dgvNV.SelectedRows.Count == 0) { MessageBox.Show("Chọn nhân viên!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var r = dgvNV.SelectedRows[0];
                string maNV = r.Cells["MaNV"].Value?.ToString() ?? "";
                string tenDN = r.Cells["TenDangNhap"].Value?.ToString() ?? "";
                bool isNew = string.IsNullOrEmpty(tenDN);
                var dlg = new FormCapTaiKhoan(maNV, tenDN, r.Cells["ChucVu"].Value?.ToString() ?? "LeTan", isNew);
                if (dlg.ShowDialog() == DialogResult.OK) LoadNV();
            };

            wrapper.Controls.Add(contentPanel);
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ──────────────────────────────────────────────────────────────
        // VIEW 6: THỐNG KÊ & BÁO CÁO
        // ──────────────────────────────────────────────────────────────
        private void ShowThongKeView()
        {
            HighlightMenu(btnMenuThongKe);
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            wrapper.Controls.Add(MakePageHeader("📊 Báo cáo & Thống kê", "Tổng quan hoạt động phòng khám"));

            // KPI Cards
            var (kham, thu, thuocSapHet) = ClinicDAL.LayThongKeTongQuan();
            int tongBN = Convert.ToInt32(ClinicDAL.LayDanhSachBenhNhan().Rows.Count);
            var pCards = new Panel { Location = new Point(10, 65), Size = new Size(1100, 110) };
            pCards.Controls.Add(CreateKPICard("LƯỢT KHÁM HÔM NAY", $"{kham} lượt", Color.FromArgb(0, 122, 255), 0));
            pCards.Controls.Add(CreateKPICard("TỔNG DOANH THU", $"{thu:N0} đ", Color.FromArgb(40, 167, 69), 210));
            pCards.Controls.Add(CreateKPICard("TỔNG BỆNH NHÂN", $"{tongBN} người", Color.FromArgb(100, 60, 200), 420));
            pCards.Controls.Add(CreateKPICard("THUỐC SẮP HẾT KHO", $"{thuocSapHet} loại", Color.FromArgb(220, 53, 69), 630));

            // Bảng doanh thu 7 ngày
            var grpDT = new GroupBox { Text = "DOANH THU 7 NGÀY GẦN NHẤT", Location = new Point(10, 185), Size = new Size(550, 280), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var dgvDT = MakeGrid(DockStyle.None);
            dgvDT.Location = new Point(10, 25); dgvDT.Size = new Size(525, 240);
            dgvDT.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvDT.DataSource = ClinicDAL.LayDoanhThu7Ngay();
            grpDT.Controls.Add(dgvDT);

            // Top chẩn đoán thường gặp
            var grpCD = new GroupBox { Text = "TOP 10 CHẨN ĐOÁN THƯỜNG GẶP", Location = new Point(580, 185), Size = new Size(480, 280), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var dgvCD = MakeGrid(DockStyle.None);
            dgvCD.Location = new Point(10, 25); dgvCD.Size = new Size(455, 240);
            dgvCD.DataSource = ClinicDAL.LayTopChanDoan(10);
            grpCD.Controls.Add(dgvCD);

            // Thuốc sắp hết kho
            var grpThuoc = new GroupBox { Text = "⚠ DANH SÁCH THUỐC SẮP HẾT KHO (≤ 30)", Location = new Point(10, 475), Size = new Size(1050, 250), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var dgvTH = MakeGrid(DockStyle.None);
            dgvTH.Location = new Point(10, 25); dgvTH.Size = new Size(1025, 210);
            dgvTH.DataSource = ClinicDAL.LayThuocSapHet(30);
            grpThuoc.Controls.Add(dgvTH);

            wrapper.Controls.Add(pCards);
            wrapper.Controls.Add(grpDT);
            wrapper.Controls.Add(grpCD);
            wrapper.Controls.Add(grpThuoc);
            panelContent.Controls.Add(wrapper);
        }

        private Panel CreateKPICard(string title, string value, Color color, int x)
        {
            var p = new Panel { Location = new Point(x, 0), Size = new Size(200, 100), BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            var bar = new Panel { Dock = DockStyle.Top, Height = 5, BackColor = color };
            var lblT = new Label { Text = title, Location = new Point(12, 18), AutoSize = false, Size = new Size(175, 30), Font = new Font("Segoe UI", 8F), ForeColor = Color.Gray, TextAlign = ContentAlignment.TopLeft };
            var lblV = new Label { Text = value, Location = new Point(12, 52), AutoSize = true, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = color };
            p.Controls.AddRange(new Control[] { bar, lblT, lblV });
            return p;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // DIALOG: THÊM / SỬA BỆNH NHÂN
    // ──────────────────────────────────────────────────────────────
    public class FormThemSuaBN : Form
    {
        private readonly BenhNhan? _editing;
        public FormThemSuaBN(BenhNhan? bn)
        {
            _editing = bn;
            Text = bn == null ? "Thêm bệnh nhân mới" : "Sửa thông tin bệnh nhân";
            Size = new Size(540, 360); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5F);

            var txtMaBN = new TextBox { Location = new Point(130, 20), Size = new Size(120, 26), ReadOnly = bn != null, BackColor = bn != null ? Color.FromArgb(245, 245, 245) : Color.White, Text = bn?.MaBN ?? ClinicDAL.SinhMaBenhNhan() };
            var txtHoTen = new TextBox { Location = new Point(130, 55), Size = new Size(350, 26), Text = bn?.HoTen ?? "" };
            var cboGT = new ComboBox { Location = new Point(130, 90), Size = new Size(100, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboGT.Items.AddRange(new object[] { "Nam", "Nữ" }); cboGT.SelectedItem = bn?.GioiTinh ?? "Nam";
            var dtpNS = new DateTimePicker { Location = new Point(130, 125), Size = new Size(180, 26), Format = DateTimePickerFormat.Short };
            if (bn?.NgaySinh != null) dtpNS.Value = bn.NgaySinh.Value; else dtpNS.Value = DateTime.Today.AddYears(-30);
            var txtSDT = new TextBox { Location = new Point(130, 160), Size = new Size(180, 26), Text = bn?.SoDienThoai ?? "" };
            var txtDC = new TextBox { Location = new Point(130, 195), Size = new Size(350, 26), Text = bn?.DiaChi ?? "" };
            var txtTS = new TextBox { Location = new Point(130, 230), Size = new Size(350, 26), Text = bn?.TienSuBenh ?? "" };

            Controls.AddRange(new Control[] {
                new Label { Text = "Mã BN:", Location = new Point(20, 23), AutoSize = true },
                txtMaBN,
                new Label { Text = "Họ tên (*):", Location = new Point(20, 58), AutoSize = true }, txtHoTen,
                new Label { Text = "Giới tính:", Location = new Point(20, 93), AutoSize = true }, cboGT,
                new Label { Text = "Ngày sinh:", Location = new Point(20, 128), AutoSize = true }, dtpNS,
                new Label { Text = "Điện thoại:", Location = new Point(20, 163), AutoSize = true }, txtSDT,
                new Label { Text = "Địa chỉ:", Location = new Point(20, 198), AutoSize = true }, txtDC,
                new Label { Text = "Tiền sử bệnh:", Location = new Point(20, 233), AutoSize = true }, txtTS
            });

            var btnSave = new Button { Text = "💾 Lưu", Location = new Point(160, 273), Size = new Size(120, 34), BackColor = Color.FromArgb(0, 122, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold), DialogResult = DialogResult.None };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(295, 273), Size = new Size(90, 34), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtHoTen.Text)) { MessageBox.Show("Vui lòng nhập họ tên!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var newBN = new BenhNhan { MaBN = txtMaBN.Text.Trim(), HoTen = txtHoTen.Text.Trim(), GioiTinh = cboGT.SelectedItem?.ToString() ?? "Nam", NgaySinh = dtpNS.Value, SoDienThoai = txtSDT.Text.Trim(), DiaChi = txtDC.Text.Trim(), TienSuBenh = txtTS.Text.Trim() };
                ClinicDAL.LuuBenhNhan(newBN, _editing == null);
                DialogResult = DialogResult.OK; Close();
            };
        }
    }

    // ──────────────────────────────────────────────────────────────
    // DIALOG: LỊCH SỬ KHÁM BỆNH NHÂN
    // ──────────────────────────────────────────────────────────────
    public class FormLichSuKham : Form
    {
        public FormLichSuKham(string maBN, string tenBN)
        {
            Text = $"Lịch sử khám - {tenBN} ({maBN})";
            Size = new Size(900, 500); StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5F);
            var dgv = new DataGridView { Dock = DockStyle.Fill, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ReadOnly = true, BackgroundColor = Color.White, RowHeadersVisible = false, AllowUserToAddRows = false };
            dgv.DataSource = ClinicDAL.LayLichSuKhamBenhNhan(maBN);
            Controls.Add(dgv);
        }
    }

    // ──────────────────────────────────────────────────────────────
    // DIALOG: THÊM / SỬA THUỐC
    // ──────────────────────────────────────────────────────────────
    public class FormThemSuaThuoc : Form
    {
        private readonly Thuoc? _editing;
        public FormThemSuaThuoc(Thuoc? t)
        {
            _editing = t;
            Text = t == null ? "Thêm thuốc mới" : "Sửa thông tin thuốc";
            Size = new Size(490, 330); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5F);

            string sinhMa = ClinicDAL.SinhMaDichVu();
            var txtMa = new TextBox { Location = new Point(130, 18), Size = new Size(120, 26), ReadOnly = t != null, BackColor = t != null ? Color.FromArgb(245, 245, 245) : Color.White, Text = t?.MaThuoc ?? ("TH" + DateTime.Now.ToString("mmss")) };
            var txtTen = new TextBox { Location = new Point(130, 53), Size = new Size(300, 26), Text = t?.TenThuoc ?? "" };
            var cboDVT = new ComboBox { Location = new Point(130, 88), Size = new Size(120, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboDVT.Items.AddRange(new object[] { "Viên", "Vỉ", "Hộp", "Gói", "Chai", "Tuýp", "Ống" });
            cboDVT.SelectedItem = t?.DonViTinh ?? "Viên";
            var numDonGia = new NumericUpDown { Location = new Point(130, 123), Size = new Size(140, 26), Maximum = 10000000, Increment = 1000, Value = (decimal)(t?.DonGiaBan ?? 5000), ThousandsSeparator = true };
            var numTon = new NumericUpDown { Location = new Point(130, 158), Size = new Size(120, 26), Maximum = 999999, Value = t?.SoLuongTon ?? 100 };
            var txtCachDung = new TextBox { Location = new Point(130, 193), Size = new Size(300, 26), Text = t?.CachDungMacDinh ?? "" };

            Controls.AddRange(new Control[] {
                new Label { Text = "Mã thuốc:", Location = new Point(20, 21), AutoSize = true }, txtMa,
                new Label { Text = "Tên thuốc (*):", Location = new Point(20, 56), AutoSize = true }, txtTen,
                new Label { Text = "Đơn vị tính:", Location = new Point(20, 91), AutoSize = true }, cboDVT,
                new Label { Text = "Đơn giá bán:", Location = new Point(20, 126), AutoSize = true }, numDonGia,
                new Label { Text = "Số lượng tồn:", Location = new Point(20, 161), AutoSize = true }, numTon,
                new Label { Text = "Cách dùng:", Location = new Point(20, 196), AutoSize = true }, txtCachDung
            });

            var btnSave = new Button { Text = "💾 Lưu thuốc", Location = new Point(130, 238), Size = new Size(140, 34), BackColor = Color.FromArgb(0, 122, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(285, 238), Size = new Size(90, 34), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTen.Text)) { MessageBox.Show("Nhập tên thuốc!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var thuoc = new Thuoc { MaThuoc = txtMa.Text.Trim(), TenThuoc = txtTen.Text.Trim(), DonViTinh = cboDVT.SelectedItem?.ToString() ?? "Viên", DonGiaBan = numDonGia.Value, SoLuongTon = (int)numTon.Value, CachDungMacDinh = txtCachDung.Text.Trim() };
                ClinicDAL.LuuThuoc(thuoc, _editing == null);
                DialogResult = DialogResult.OK; Close();
            };
        }
    }

    // ──────────────────────────────────────────────────────────────
    // DIALOG: THÊM / SỬA NHÂN VIÊN (Admin)
    // ──────────────────────────────────────────────────────────────
    public class FormThemSuaNhanVien : Form
    {
        private readonly NhanVien? _editing;
        public FormThemSuaNhanVien(NhanVien? nv)
        {
            _editing = nv;
            Text = nv == null ? "Thêm nhân viên mới" : "Sửa thông tin nhân viên";
            Size = new Size(490, 330); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5F);

            var txtMa = new TextBox { Location = new Point(135, 18), Size = new Size(100, 26), ReadOnly = nv != null, BackColor = nv != null ? Color.FromArgb(245, 245, 245) : Color.White, Text = nv?.MaNV ?? ClinicDAL.SinhMaNhanVien() };
            var txtHoTen = new TextBox { Location = new Point(135, 53), Size = new Size(300, 26), Text = nv?.HoTen ?? "" };
            var cboCV = new ComboBox { Location = new Point(135, 88), Size = new Size(140, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboCV.Items.AddRange(new object[] { "Admin", "BacSi", "ThuNgan", "LeTan" });
            cboCV.SelectedItem = nv?.ChucVu ?? "LeTan";
            var txtCK = new TextBox { Location = new Point(135, 123), Size = new Size(250, 26), Text = nv?.ChuyenKhoa ?? "" };
            var txtSDT = new TextBox { Location = new Point(135, 158), Size = new Size(180, 26), Text = nv?.SoDienThoai ?? "" };
            var txtEmail = new TextBox { Location = new Point(135, 193), Size = new Size(280, 26), Text = nv?.Email ?? "" };

            Controls.AddRange(new Control[] {
                new Label { Text = "Mã nhân viên:", Location = new Point(20, 21), AutoSize = true }, txtMa,
                new Label { Text = "Họ tên (*):", Location = new Point(20, 56), AutoSize = true }, txtHoTen,
                new Label { Text = "Chức vụ:", Location = new Point(20, 91), AutoSize = true }, cboCV,
                new Label { Text = "Chuyên khoa:", Location = new Point(20, 126), AutoSize = true }, txtCK,
                new Label { Text = "Điện thoại:", Location = new Point(20, 161), AutoSize = true }, txtSDT,
                new Label { Text = "Email:", Location = new Point(20, 196), AutoSize = true }, txtEmail
            });

            var btnSave = new Button { Text = "💾 Lưu", Location = new Point(135, 238), Size = new Size(130, 34), BackColor = Color.FromArgb(0, 122, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(280, 238), Size = new Size(90, 34), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtHoTen.Text)) { MessageBox.Show("Nhập họ tên!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                var newNV = new NhanVien { MaNV = txtMa.Text.Trim(), HoTen = txtHoTen.Text.Trim(), ChucVu = cboCV.SelectedItem?.ToString() ?? "LeTan", ChuyenKhoa = txtCK.Text.Trim(), SoDienThoai = txtSDT.Text.Trim(), Email = txtEmail.Text.Trim() };
                ClinicDAL.LuuNhanVien(newNV, _editing == null);
                DialogResult = DialogResult.OK; Close();
            };
        }
    }

    // ──────────────────────────────────────────────────────────────
    // DIALOG: CẤP / ĐỔI MẬT KHẨU TÀI KHOẢN
    // ──────────────────────────────────────────────────────────────
    public class FormCapTaiKhoan : Form
    {
        public FormCapTaiKhoan(string maNV, string tenDangNhapCu, string chucVu, bool isNew)
        {
            Text = isNew ? "Cấp tài khoản mới" : "Đặt lại mật khẩu";
            Size = new Size(420, 260); StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            Font = new Font("Segoe UI", 9.5F);

            var txtMaNV = new TextBox { Location = new Point(150, 20), Size = new Size(220, 26), ReadOnly = true, BackColor = Color.FromArgb(245, 245, 245), Text = maNV };
            var txtTDN = new TextBox { Location = new Point(150, 55), Size = new Size(220, 26), ReadOnly = !isNew, BackColor = isNew ? Color.White : Color.FromArgb(245, 245, 245), Text = tenDangNhapCu };
            var txtMK = new TextBox { Location = new Point(150, 90), Size = new Size(220, 26), UseSystemPasswordChar = true };
            var txtMK2 = new TextBox { Location = new Point(150, 125), Size = new Size(220, 26), UseSystemPasswordChar = true };
            var cboQ = new ComboBox { Location = new Point(150, 160), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            cboQ.Items.AddRange(new object[] { "Admin", "BacSi", "ThuNgan", "LeTan" });
            cboQ.SelectedItem = chucVu;

            Controls.AddRange(new Control[] {
                new Label { Text = "Mã nhân viên:", Location = new Point(20, 23), AutoSize = true }, txtMaNV,
                new Label { Text = "Tên đăng nhập:", Location = new Point(20, 58), AutoSize = true }, txtTDN,
                new Label { Text = "Mật khẩu mới:", Location = new Point(20, 93), AutoSize = true }, txtMK,
                new Label { Text = "Xác nhận MK:", Location = new Point(20, 128), AutoSize = true }, txtMK2,
                new Label { Text = "Phân quyền:", Location = new Point(20, 163), AutoSize = true }, cboQ
            });

            var btnSave = new Button { Text = "💾 Xác nhận", Location = new Point(110, 198), Size = new Size(130, 34), BackColor = Color.FromArgb(0, 122, 255), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
            var btnCancel = new Button { Text = "Hủy", Location = new Point(255, 198), Size = new Size(90, 34), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { btnSave, btnCancel });

            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtTDN.Text)) { MessageBox.Show("Nhập tên đăng nhập!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                if (txtMK.Text != txtMK2.Text) { MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                if (txtMK.Text.Length < 4) { MessageBox.Show("Mật khẩu phải ít nhất 4 ký tự!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                ClinicDAL.LuuTaiKhoan(maNV, txtTDN.Text.Trim(), txtMK.Text, cboQ.SelectedItem?.ToString() ?? "LeTan", isNew);
                MessageBox.Show("✅ Đã lưu tài khoản thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK; Close();
            };
        }
    }
}
