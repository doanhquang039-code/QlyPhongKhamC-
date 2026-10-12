using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public class FormMain : Form
    {
        private Panel panelSidebar = null!;
        private Panel panelHeader = null!;
        private Panel panelContent = null!;
        private Label lblUserGreeting = null!;
        private Button btnMenuTiepDon = null!;
        private Button btnMenuKhamBenh = null!;
        private Button btnMenuKhoThuoc = null!;
        private Button btnMenuThuNgan = null!;
        private Button btnMenuNhanVien = null!;
        private Button btnMenuThongKe = null!;
        private Button btnLogout = null!;

        public FormMain()
        {
            InitializeComponent();
            ApplyRolePermissions();
            ShowTiepDonView(); // Mặc định hiển thị tab đầu tiên
        }

        private void InitializeComponent()
        {
            this.Text = "HỆ THỐNG QUẢN LÝ PHÒNG KHÁM TƯ NHÂN";
            this.Size = new Size(1200, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 650);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            this.BackColor = Color.FromArgb(245, 247, 250);

            // 1. Sidebar Panel
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(18, 38, 58)
            };

            Label lblAppLogo = new Label
            {
                Text = "🏥 PHÒNG KHÁM",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 20),
                AutoSize = true
            };
            panelSidebar.Controls.Add(lblAppLogo);

            int btnY = 80;
            int step = 48;

            btnMenuTiepDon = CreateSidebarButton("📋 Tiếp đón & Khám", btnY);
            btnMenuKhamBenh = CreateSidebarButton("🩺 Phòng khám BS", btnY + step);
            btnMenuKhoThuoc = CreateSidebarButton("💊 Kho Dược phẩm", btnY + step * 2);
            btnMenuThuNgan = CreateSidebarButton("💰 Thu ngân & HĐ", btnY + step * 3);
            btnMenuNhanVien = CreateSidebarButton("👥 QL Nhân viên", btnY + step * 4);
            btnMenuThongKe = CreateSidebarButton("📊 Báo cáo Thống kê", btnY + step * 5);

            btnMenuTiepDon.Click += (s, e) => ShowTiepDonView();
            btnMenuKhamBenh.Click += (s, e) => ShowKhamBenhView();
            btnMenuKhoThuoc.Click += (s, e) => ShowKhoThuocView();
            btnMenuThuNgan.Click += (s, e) => ShowThuNganView();
            btnMenuNhanVien.Click += (s, e) => ShowNhanVienView();
            btnMenuThongKe.Click += (s, e) => ShowThongKeView();

            btnLogout = new Button
            {
                Text = "🚪 Đăng xuất",
                Location = new Point(15, 630),
                Size = new Size(190, 40),
                BackColor = Color.FromArgb(180, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            btnLogout.Click += (s, e) => {
                SessionState.CurrentUser = null;
                this.Close();
            };

            panelSidebar.Controls.AddRange(new Control[] {
                btnMenuTiepDon, btnMenuKhamBenh, btnMenuKhoThuoc,
                btnMenuThuNgan, btnMenuNhanVien, btnMenuThongKe, btnLogout
            });

            // 2. Header Panel
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblUserGreeting = new Label
            {
                Text = $"👤 Xin chào: {SessionState.CurrentUser?.HoTenNhanVien} | Vai trò: [{SessionState.CurrentUser?.Quyen}]",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 50, 80),
                Location = new Point(20, 18),
                AutoSize = true
            };

            Button btnServerSetting = new Button
            {
                Text = "⚙ Cấu hình DB",
                Size = new Size(110, 32),
                Location = new Point(830, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(235, 240, 248),
                FlatStyle = FlatStyle.Flat
            };
            btnServerSetting.Click += (s, e) => new FormConfigDb().ShowDialog();

            panelHeader.Controls.AddRange(new Control[] { lblUserGreeting, btnServerSetting });

            // 3. Content Panel
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(15)
            };

            this.Controls.Add(panelContent);
            this.Controls.Add(panelHeader);
            this.Controls.Add(panelSidebar);
        }

        private Button CreateSidebarButton(string text, int top)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(12, top),
                Size = new Size(196, 42),
                BackColor = Color.FromArgb(26, 52, 78),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void HighlightMenu(Button activeBtn)
        {
            foreach (Control c in panelSidebar.Controls)
            {
                if (c is Button b && b != btnLogout)
                {
                    b.BackColor = Color.FromArgb(26, 52, 78);
                }
            }
            activeBtn.BackColor = Color.FromArgb(0, 122, 255);
        }

        private void ApplyRolePermissions()
        {
            // Bác sĩ chỉ truy cập Phòng khám & Kho thuốc
            if (SessionState.IsDoctor)
            {
                btnMenuTiepDon.Enabled = false;
                btnMenuThuNgan.Enabled = false;
                btnMenuNhanVien.Enabled = false;
                btnMenuThongKe.Enabled = false;
                ShowKhamBenhView();
            }
            // Lễ tân chỉ tiếp đón
            else if (SessionState.IsReceptionist)
            {
                btnMenuKhamBenh.Enabled = false;
                btnMenuKhoThuoc.Enabled = false;
                btnMenuThuNgan.Enabled = false;
                btnMenuNhanVien.Enabled = false;
                btnMenuThongKe.Enabled = false;
            }
            // Thu ngân chỉ thu tiền & xem kho
            else if (SessionState.IsCashier)
            {
                btnMenuTiepDon.Enabled = false;
                btnMenuKhamBenh.Enabled = false;
                btnMenuNhanVien.Enabled = false;
                ShowThuNganView();
            }
        }

        // ==========================================
        // CÁC MÀN HÌNH CHỨC NĂNG (VIEWS)
        // ==========================================

        #region VIEW 1: TIẾP ĐÓN & HÀNG CHỜ KHÁM (STT TRONG NGÀY)
        private void ShowTiepDonView()
        {
            HighlightMenu(btnMenuTiepDon);
            panelContent.Controls.Clear();

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 310
            };

            // Top: Đăng ký & Cấp STT
            GroupBox grpDangKy = new GroupBox
            {
                Text = "ĐĂNG KÝ KHÁM MỚI & CẤP SỐ THỨ TỰ (STT)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            Label lblMaBN = new Label { Text = "Mã BN:", Location = new Point(20, 30), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtMaBN = new TextBox { Location = new Point(90, 27), Size = new Size(110, 25), ReadOnly = true, BackColor = Color.LightYellow };

            Label lblHoTen = new Label { Text = "Họ tên:", Location = new Point(220, 30), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtHoTen = new TextBox { Location = new Point(280, 27), Size = new Size(180, 25) };

            Label lblGT = new Label { Text = "Giới tính:", Location = new Point(480, 30), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            ComboBox cboGT = new ComboBox { Location = new Point(545, 27), Size = new Size(80, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboGT.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGT.SelectedIndex = 0;

            Label lblNS = new Label { Text = "Ngày sinh:", Location = new Point(645, 30), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            DateTimePicker dtpNS = new DateTimePicker { Location = new Point(720, 27), Size = new Size(130, 25), Format = DateTimePickerFormat.Short };

            Label lblSDT = new Label { Text = "Số ĐT:", Location = new Point(20, 68), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtSDT = new TextBox { Location = new Point(90, 65), Size = new Size(110, 25) };

            Label lblDiaChi = new Label { Text = "Địa chỉ:", Location = new Point(220, 68), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtDiaChi = new TextBox { Location = new Point(280, 65), Size = new Size(345, 25) };

            Label lblBS = new Label { Text = "Bác sĩ khám:", Location = new Point(20, 105), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            ComboBox cboBacSi = new ComboBox { Location = new Point(110, 102), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList };

            // Load bác sĩ
            var dtBS = ClinicDAL.LayDanhSachBacSi();
            cboBacSi.DataSource = dtBS;
            cboBacSi.DisplayMember = "HoTen";
            cboBacSi.ValueMember = "MaNV";

            Label lblTrieuChung = new Label { Text = "Triệu chứng ban đầu:", Location = new Point(330, 105), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtTrieuChung = new TextBox { Location = new Point(470, 102), Size = new Size(380, 25) };

            Label lblNextSTT = new Label
            {
                Text = "STT tiếp theo hôm nay: " + ClinicDAL.LaySTTTiepTheoTrongNgay(),
                Location = new Point(20, 145),
                AutoSize = true,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };

            Button btnCapSTT = new Button
            {
                Text = "🎟 CẤP PHIẾU KHÁM & STT",
                Location = new Point(320, 140),
                Size = new Size(220, 36),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };

            Button btnLamMoi = new Button
            {
                Text = "🔄 Làm mới / Mã mới",
                Location = new Point(555, 140),
                Size = new Size(160, 36),
                BackColor = Color.FromArgb(230, 235, 245),
                FlatStyle = FlatStyle.Flat
            };

            void ResetFormBN()
            {
                txtMaBN.Text = ClinicDAL.SinhMaBenhNhan();
                txtHoTen.Clear();
                txtSDT.Clear();
                txtDiaChi.Clear();
                txtTrieuChung.Clear();
                lblNextSTT.Text = "STT tiếp theo hôm nay: " + ClinicDAL.LaySTTTiepTheoTrongNgay();
            }

            ResetFormBN();
            btnLamMoi.Click += (s, e) => ResetFormBN();

            grpDangKy.Controls.AddRange(new Control[] {
                lblMaBN, txtMaBN, lblHoTen, txtHoTen, lblGT, cboGT, lblNS, dtpNS,
                lblSDT, txtSDT, lblDiaChi, txtDiaChi,
                lblBS, cboBacSi, lblTrieuChung, txtTrieuChung,
                lblNextSTT, btnCapSTT, btnLamMoi
            });

            // Bottom: Danh sách hàng chờ khám trong ngày
            GroupBox grpHangCho = new GroupBox
            {
                Text = "DANH SÁCH BỆNH NHÂN ĐANG CHỜ KHÁM TRONG NGÀY",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            DataGridView dgvHangCho = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                BackgroundColor = Color.White
            };

            void LoadHangCho()
            {
                dgvHangCho.DataSource = ClinicDAL.LayHangChoKhamTrongNgay();
            }
            LoadHangCho();

            btnCapSTT.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtHoTen.Text))
                {
                    MessageBox.Show("Vui lòng nhập tên bệnh nhân!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Lưu Bệnh nhân
                var bn = new Models.BenhNhan
                {
                    MaBN = txtMaBN.Text.Trim(),
                    HoTen = txtHoTen.Text.Trim(),
                    GioiTinh = cboGT.SelectedItem?.ToString() ?? "Nam",
                    NgaySinh = dtpNS.Value,
                    SoDienThoai = txtSDT.Text.Trim(),
                    DiaChi = txtDiaChi.Text.Trim()
                };
                ClinicDAL.LuuBenhNhan(bn, isNew: true);

                // Cấp Phiếu khám
                int stt = ClinicDAL.LaySTTTiepTheoTrongNgay();
                var pk = new Models.PhieuKham
                {
                    MaPK = ClinicDAL.SinhMaPhieuKham(),
                    STT = stt,
                    MaBN = bn.MaBN,
                    MaNV_BacSi = cboBacSi.SelectedValue?.ToString() ?? "NV002",
                    TrieuChung = txtTrieuChung.Text.Trim()
                };
                ClinicDAL.TaoPhieuKham(pk);

                MessageBox.Show($"Cấp phiếu thành công!\nMã phiếu: {pk.MaPK}\nSố thứ tự (STT): {stt}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetFormBN();
                LoadHangCho();
            };

            grpHangCho.Controls.Add(dgvHangCho);

            split.Panel1.Controls.Add(grpDangKy);
            split.Panel2.Controls.Add(grpHangCho);
            panelContent.Controls.Add(split);
        }
        #endregion

        #region VIEW 2: PHÒNG KHÁM BÁC SĨ (CHẨN ĐOÁN & KÊ ĐƠN)
        private void ShowKhamBenhView()
        {
            HighlightMenu(btnMenuKhamBenh);
            panelContent.Controls.Clear();

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 380
            };

            // Trái: Danh sách bệnh nhân chờ khám của bác sĩ
            GroupBox grpList = new GroupBox
            {
                Text = "DANH SÁCH BỆNH NHÂN CHỜ KHÁM",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            DataGridView dgvCho = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                BackgroundColor = Color.White
            };

            string? maBS = SessionState.IsDoctor ? SessionState.CurrentUser?.MaNV : null;
            void LoadChoKham()
            {
                dgvCho.DataSource = ClinicDAL.LayHangChoKhamTrongNgay(maBS);
            }
            LoadChoKham();
            grpList.Controls.Add(dgvCho);

            // Phải: Màn hình khám bệnh & kê toa
            GroupBox grpDetail = new GroupBox
            {
                Text = "THÔNG TIN KHÁM BỆNH & KÊ ĐƠN THUỐC",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            Label lblChonPK = new Label { Text = "Mã PK:", Location = new Point(15, 28), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtMaPK = new TextBox { Location = new Point(70, 25), Size = new Size(90, 24), ReadOnly = true, BackColor = Color.LightYellow };

            Label lblTenBN = new Label { Text = "Bệnh nhân:", Location = new Point(175, 28), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtTenBN = new TextBox { Location = new Point(255, 25), Size = new Size(180, 24), ReadOnly = true };

            Label lblChanDoan = new Label { Text = "Chẩn đoán bệnh:", Location = new Point(15, 62), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtChanDoan = new TextBox { Location = new Point(125, 59), Size = new Size(420, 24) };

            Label lblLoiDan = new Label { Text = "Lời dặn / Tái khám:", Location = new Point(15, 95), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtLoiDan = new TextBox { Location = new Point(125, 92), Size = new Size(420, 24) };

            // Danh sách thuốc trong đơn
            Label lblDon = new Label { Text = "ĐƠN THUỐC ĐÃ KÊ:", Location = new Point(15, 130), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Navy };
            DataGridView dgvDonThuoc = new DataGridView
            {
                Location = new Point(15, 155),
                Size = new Size(540, 160),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                BackgroundColor = Color.White
            };

            // Chọn thuốc để thêm vào đơn
            Label lblChonThuoc = new Label { Text = "Thêm thuốc:", Location = new Point(15, 330), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            ComboBox cboThuoc = new ComboBox { Location = new Point(95, 327), Size = new Size(180, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            var dtThuoc = ClinicDAL.LayDanhSachThuoc();
            cboThuoc.DataSource = dtThuoc;
            cboThuoc.DisplayMember = "TenThuoc";
            cboThuoc.ValueMember = "MaThuoc";

            Label lblSL = new Label { Text = "SL:", Location = new Point(285, 330), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            NumericUpDown numSL = new NumericUpDown { Location = new Point(315, 327), Size = new Size(60, 25), Minimum = 1, Maximum = 500, Value = 10 };

            Label lblCD = new Label { Text = "Cách dùng:", Location = new Point(15, 365), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtCachDung = new TextBox { Location = new Point(95, 362), Size = new Size(280, 24), Text = "Uống 1v x 2 lần/ngày sau ăn" };

            Button btnThemThuoc = new Button
            {
                Text = "➕ Kê thuốc này",
                Location = new Point(390, 345),
                Size = new Size(140, 36),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            Button btnHoanThanhKham = new Button
            {
                Text = "✔ HOÀN THÀNH KHÁM & CHỐT ĐƠN",
                Location = new Point(15, 415),
                Size = new Size(320, 42),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };

            void LoadDonThuocCuaPK(string pk)
            {
                dgvDonThuoc.DataSource = ClinicDAL.LayChiTietDonThuocTheoPK(pk);
            }

            dgvCho.SelectionChanged += (s, e) =>
            {
                if (dgvCho.SelectedRows.Count > 0)
                {
                    var r = dgvCho.SelectedRows[0];
                    txtMaPK.Text = r.Cells["MaPK"].Value?.ToString();
                    txtTenBN.Text = r.Cells["TenBN"].Value?.ToString();
                    txtChanDoan.Text = r.Cells["TrieuChung"].Value?.ToString();
                    LoadDonThuocCuaPK(txtMaPK.Text);
                }
            };

            btnThemThuoc.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK.Text))
                {
                    MessageBox.Show("Vui lòng chọn một bệnh nhân trong hàng chờ bên trái!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string maThuoc = cboThuoc.SelectedValue?.ToString() ?? "";
                DataRowView drv = (DataRowView)cboThuoc.SelectedItem!;
                decimal donGia = Convert.ToDecimal(drv["DonGiaBan"]);
                string tenThuoc = drv["TenThuoc"].ToString()!;
                int sl = (int)numSL.Value;

                var list = new System.Collections.Generic.List<Models.ChiTietDonThuoc>
                {
                    new Models.ChiTietDonThuoc
                    {
                        MaThuoc = maThuoc,
                        TenThuoc = tenThuoc,
                        SoLuong = sl,
                        DonGiaThoiDiem = donGia,
                        CachDung = txtCachDung.Text.Trim()
                    }
                };

                if (ClinicDAL.KeDonThuoc(txtMaPK.Text, "Kê đơn tại phòng khám", list, out string err))
                {
                    MessageBox.Show("Đã thêm thuốc vào đơn và cập nhật tồn kho!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadDonThuocCuaPK(txtMaPK.Text);
                }
                else
                {
                    MessageBox.Show("Lỗi: " + err, "Không thể kê thuốc", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnHoanThanhKham.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK.Text)) return;
                ClinicDAL.CapNhatKetQuaKham(txtMaPK.Text, txtChanDoan.Text.Trim(), txtLoiDan.Text.Trim(), "Đã khám xong");
                MessageBox.Show("Đã hoàn thành lượt khám! Phiếu đã chuyển sang bộ phận Thu ngân.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadChoKham();
            };

            grpDetail.Controls.AddRange(new Control[] {
                lblChonPK, txtMaPK, lblTenBN, txtTenBN,
                lblChanDoan, txtChanDoan, lblLoiDan, txtLoiDan,
                lblDon, dgvDonThuoc,
                lblChonThuoc, cboThuoc, lblSL, numSL, lblCD, txtCachDung, btnThemThuoc,
                btnHoanThanhKham
            });

            split.Panel1.Controls.Add(grpList);
            split.Panel2.Controls.Add(grpDetail);
            panelContent.Controls.Add(split);
        }
        #endregion

        #region VIEW 3: KHO DƯỢC PHẨM
        private void ShowKhoThuocView()
        {
            HighlightMenu(btnMenuKhoThuoc);
            panelContent.Controls.Clear();

            GroupBox grpKho = new GroupBox
            {
                Text = "QUẢN LÝ DANH MỤC THUỐC & TỒN KHO DƯỢC PHẨM",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            Label lblTim = new Label { Text = "Tìm kiếm thuốc:", Location = new Point(20, 30), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtTim = new TextBox { Location = new Point(130, 27), Size = new Size(250, 24) };

            DataGridView dgvThuoc = new DataGridView
            {
                Location = new Point(20, 65),
                Size = new Size(900, 520),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                BackgroundColor = Color.White
            };

            void LoadThuoc()
            {
                dgvThuoc.DataSource = ClinicDAL.LayDanhSachThuoc(txtTim.Text.Trim());
            }
            LoadThuoc();
            txtTim.TextChanged += (s, e) => LoadThuoc();

            grpKho.Controls.AddRange(new Control[] { lblTim, txtTim, dgvThuoc });
            panelContent.Controls.Add(grpKho);
        }
        #endregion

        #region VIEW 4: THU NGÂN & XUẤT HÓA ĐƠN
        private void ShowThuNganView()
        {
            HighlightMenu(btnMenuThuNgan);
            panelContent.Controls.Clear();

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 250
            };

            GroupBox grpLapHD = new GroupBox
            {
                Text = "LẬP HÓA ĐƠN VIỆN PHÍ",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            Label lblPK = new Label { Text = "Nhập Mã Phiếu Khám:", Location = new Point(20, 35), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            TextBox txtPK = new TextBox { Location = new Point(165, 32), Size = new Size(130, 24), Text = "PK0001" };

            Button btnTinhTien = new Button
            {
                Text = "🔍 Tính chi phí",
                Location = new Point(310, 30),
                Size = new Size(120, 28),
                BackColor = Color.FromArgb(230, 235, 245),
                FlatStyle = FlatStyle.Flat
            };

            Label lblTienDV = new Label { Text = "Tiền Dịch vụ: 0 VNĐ", Location = new Point(20, 80), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkSlateBlue };
            Label lblTienThuoc = new Label { Text = "Tiền Thuốc: 0 VNĐ", Location = new Point(250, 80), AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = Color.DarkSlateBlue };
            Label lblTongTien = new Label { Text = "TỔNG THANH TOÁN: 0 VNĐ", Location = new Point(480, 78), AutoSize = true, Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = Color.Crimson };

            Label lblHTTT = new Label { Text = "Hình thức TT:", Location = new Point(20, 130), AutoSize = true, Font = new Font("Segoe UI", 9F) };
            ComboBox cboHT = new ComboBox { Location = new Point(115, 127), Size = new Size(150, 25), DropDownStyle = ComboBoxStyle.DropDownList };
            cboHT.Items.AddRange(new object[] { "Tiền mặt", "Chuyển khoản QR", "Thẻ" });
            cboHT.SelectedIndex = 0;

            Button btnXuatHD = new Button
            {
                Text = "💳 XÁC NHẬN THANH TOÁN & XUẤT HÓA ĐƠN",
                Location = new Point(310, 122),
                Size = new Size(330, 36),
                BackColor = Color.FromArgb(0, 122, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            decimal curDV = 0, curThuoc = 0;
            btnTinhTien.Click += (s, e) =>
            {
                var (dv, th) = ClinicDAL.TinhChiPhiPhieuKham(txtPK.Text.Trim());
                curDV = dv;
                curThuoc = th;
                lblTienDV.Text = $"Tiền Dịch vụ: {curDV:N0} VNĐ";
                lblTienThuoc.Text = $"Tiền Thuốc: {curThuoc:N0} VNĐ";
                lblTongTien.Text = $"TỔNG THANH TOÁN: {(curDV + curThuoc):N0} VNĐ";
            };

            btnXuatHD.Click += (s, e) =>
            {
                if (curDV + curThuoc == 0)
                {
                    btnTinhTien.PerformClick();
                }

                var hd = new Models.HoaDon
                {
                    MaHD = ClinicDAL.SinhMaHoaDon(),
                    MaPK = txtPK.Text.Trim(),
                    MaNV_ThuNgan = SessionState.CurrentUser?.MaNV ?? "NV004",
                    TienDichVu = curDV,
                    TienThuoc = curThuoc,
                    TongTien = curDV + curThuoc,
                    HinhThucTT = cboHT.SelectedItem?.ToString() ?? "Tiền mặt",
                    TrangThai = "Đã thanh toán"
                };

                try
                {
                    ClinicDAL.TaoHoaDon(hd);
                    MessageBox.Show($"Thanh toán thành công!\nMã hóa đơn: {hd.MaHD}\nTổng tiền: {hd.TongTien:N0} VNĐ", "Hóa đơn viện phí", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ShowThuNganView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xuất hóa đơn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            grpLapHD.Controls.AddRange(new Control[] {
                lblPK, txtPK, btnTinhTien,
                lblTienDV, lblTienThuoc, lblTongTien,
                lblHTTT, cboHT, btnXuatHD
            });

            GroupBox grpLichSu = new GroupBox
            {
                Text = "LỊCH SỬ HÓA ĐƠN ĐÃ THANH TOÁN",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            DataGridView dgvHD = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                BackgroundColor = Color.White
            };
            dgvHD.DataSource = ClinicDAL.LayDanhSachHoaDon();
            grpLichSu.Controls.Add(dgvHD);

            split.Panel1.Controls.Add(grpLapHD);
            split.Panel2.Controls.Add(grpLichSu);
            panelContent.Controls.Add(split);
        }
        #endregion

        #region VIEW 5: QUẢN TRỊ NHÂN VIÊN
        private void ShowNhanVienView()
        {
            HighlightMenu(btnMenuNhanVien);
            panelContent.Controls.Clear();

            GroupBox grpNV = new GroupBox
            {
                Text = "DANH SÁCH NHÂN SỰ & TÀI KHOẢN TRONG HỆ THỐNG",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            DataGridView dgvNV = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                BackgroundColor = Color.White
            };
            dgvNV.DataSource = ClinicDAL.LayDanhSachNhanVien();
            grpNV.Controls.Add(dgvNV);

            panelContent.Controls.Add(grpNV);
        }
        #endregion

        #region VIEW 6: THỐNG KÊ DOANH THU & TỔNG QUAN
        private void ShowThongKeView()
        {
            HighlightMenu(btnMenuThongKe);
            panelContent.Controls.Clear();

            GroupBox grpTK = new GroupBox
            {
                Text = "BÁO CÁO THỐNG KÊ HOẠT ĐỘNG PHÒNG KHÁM",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            var (kham, thu, thuoc) = ClinicDAL.LayThongKeTongQuan();

            Panel card1 = CreateStatCard("SỐ LƯỢT KHÁM HÔM NAY", $"{kham} lượt", Color.FromArgb(0, 122, 255), 30, 40);
            Panel card2 = CreateStatCard("TỔNG DOANH THU THỰC THU", $"{thu:N0} đ", Color.FromArgb(40, 167, 69), 320, 40);
            Panel card3 = CreateStatCard("THUỐC CẢNH BÁO SẮP HẾT", $"{thuoc} loại", Color.FromArgb(220, 53, 69), 610, 40);

            grpTK.Controls.AddRange(new Control[] { card1, card2, card3 });
            panelContent.Controls.Add(grpTK);
        }

        private Panel CreateStatCard(string title, string value, Color accentColor, int x, int y)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(260, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            var bar = new Panel { Dock = DockStyle.Top, Height = 6, BackColor = accentColor };
            var lblT = new Label { Text = title, Location = new Point(15, 20), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9F) };
            var lblV = new Label { Text = value, Location = new Point(15, 50), AutoSize = true, ForeColor = Color.FromArgb(20, 30, 50), Font = new Font("Segoe UI", 16F, FontStyle.Bold) };

            p.Controls.AddRange(new Control[] { bar, lblT, lblV });
            return p;
        }
        #endregion
    }
}
