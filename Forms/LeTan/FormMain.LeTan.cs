using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    public partial class FormMain
    {
        // ═══════════════════════════════════════════════════════════
        //  LỄ TÂN & THU NGÂN: TIẾP ĐÓN & ĐĂNG KÝ KHÁM
        // ═══════════════════════════════════════════════════════════
        private void ShowTiepDonView()
        {
            SetActiveNav(btnNavLichHen);
            SetPageHeader("Lễ tân & Thu ngân  ›  Tiếp đón", "Đăng ký khám bệnh, cấp số thứ tự & theo dõi hàng chờ");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            var split   = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 350 };

            // ── PANEL ĐĂNG KÝ ──
            var grp = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = Padding.Empty };
            grp.Paint += PaintCardBorder;
            var registrationLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = C_Card,
                Margin = Padding.Empty
            };
            registrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            registrationLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            registrationLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));

            var registrationHeader = new Panel { Dock = DockStyle.Fill, BackColor = C_Sidebar };
            var iconBadge = new Panel
            {
                Location = new Point(18, 13),
                Size = new Size(32, 32),
                BackColor = C_Teal
            };
            iconBadge.Controls.Add(new Label
            {
                Text = "✚",
                Dock = DockStyle.Fill,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            });
            registrationHeader.Controls.Add(iconBadge);
            registrationHeader.Controls.Add(new Label
            {
                Text = "Đăng ký khám",
                Location = new Point(62, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White
            });
            registrationHeader.Controls.Add(new Label
            {
                Text = "Nhập thông tin người bệnh để tạo phiếu khám mới",
                Location = new Point(63, 32),
                AutoSize = true,
                Font = F_Small,
                ForeColor = Color.FromArgb(203, 213, 225)
            });

            var fieldsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 3,
                Padding = new Padding(16, 8, 16, 4),
                BackColor = C_Card
            };
            for (int i = 0; i < 3; i++)
            {
                fieldsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
                fieldsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f));
            }

            var actionBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 250, 252) };
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                Width = 540,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(8, 12, 12, 8),
                BackColor = Color.Transparent
            };
            actionBar.Controls.Add(actions);

            registrationLayout.Controls.Add(registrationHeader, 0, 0);
            registrationLayout.Controls.Add(fieldsGrid, 0, 1);
            registrationLayout.Controls.Add(actionBar, 0, 2);
            grp.Controls.Add(registrationLayout);

            // Row 1
            var txtMaBN  = MakeInput(65, 10, 90, readOnly: true);
            var txtHoTen = MakeInput(250, 10, 200);
            var cboGT    = new ComboBox { Location = new Point(530, 10), Size = new Size(75, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = F_Base };
            cboGT.Items.AddRange(new object[] { "Nam", "Nữ" });
            cboGT.SelectedIndex = 0;
            var dtpNS    = new DateTimePicker { Location = new Point(695, 10), Size = new Size(130, 26), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddYears(-25), Font = F_Base };
            var txtSDT   = MakeInput(915, 10, 130);

            // Row 2
            var txtDC    = MakeInput(65, 48, 400);
            var txtTS    = MakeInput(560, 48, 350);

            // Row 3
            var cboBacSi = new ComboBox { Location = new Point(115, 86), Size = new Size(230, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = F_Base };
            var dtBS     = ClinicDAL.LayDanhSachBacSi();
            cboBacSi.DataSource    = dtBS;
            cboBacSi.DisplayMember = "HoTen";
            cboBacSi.ValueMember   = "MaNV";
            var txtTC    = MakeInput(495, 86, 360);

            var lblSTT = new Label
            {
                Text      = $"🎫  SỐ THỨ TỰ TIẾP THEO   #{ClinicDAL.LaySTTTiepTheoTrongNgay()}",
                Location  = new Point(20, 24),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = C_Teal
            };
            var btnCap     = PrimaryBtn("🎟  CẤP PHIẾU KHÁM", 0, 0, 200, 38);
            var btnTaiKham = SecondaryBtn("🔁  Tái khám (BN cũ)", 0, 0, 180, 38, C_Blue);
            var btnReset   = SecondaryBtn("🔄  Làm mới", 0, 0, 110, 38, Color.FromArgb(107, 114, 128));
            btnCap.Margin = btnTaiKham.Margin = btnReset.Margin = new Padding(5, 3, 5, 3);
            actions.Controls.AddRange(new Control[] { btnCap, btnTaiKham, btnReset });
            actionBar.Controls.Add(lblSTT);

            txtMaBN.Text = ClinicDAL.SinhMaBenhNhan();

            Control CreateField(string label, Control input)
            {
                var field = new Panel { Dock = DockStyle.Fill, Margin = new Padding(7, 4, 7, 4) };
                field.Controls.Add(new Label
                {
                    Text = label,
                    Location = new Point(0, 0),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = C_TextGray
                });
                input.Location = new Point(0, 21);
                input.Size = new Size(100, 30);
                input.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                input.Font = F_Base;
                field.Controls.Add(input);
                return field;
            }

            fieldsGrid.Controls.Add(CreateField("MÃ BỆNH NHÂN", txtMaBN), 0, 0);
            fieldsGrid.Controls.Add(CreateField("HỌ VÀ TÊN *", txtHoTen), 1, 0);
            fieldsGrid.Controls.Add(CreateField("SỐ ĐIỆN THOẠI", txtSDT), 2, 0);
            fieldsGrid.Controls.Add(CreateField("GIỚI TÍNH", cboGT), 0, 1);
            fieldsGrid.Controls.Add(CreateField("NGÀY SINH", dtpNS), 1, 1);
            fieldsGrid.Controls.Add(CreateField("BÁC SĨ KHÁM *", cboBacSi), 2, 1);
            fieldsGrid.Controls.Add(CreateField("ĐỊA CHỈ", txtDC), 0, 2);
            fieldsGrid.Controls.Add(CreateField("TIỀN SỬ BỆNH / DỊ ỨNG", txtTS), 1, 2);
            fieldsGrid.Controls.Add(CreateField("TRIỆU CHỨNG", txtTC), 2, 2);

            var dgvHangCho = MakeGrid();
            void LoadHangCho() => dgvHangCho.DataSource = ClinicDAL.LayHangChoKhamTrongNgay();

            btnReset.Click += (s, e) =>
            {
                txtMaBN.Text  = ClinicDAL.SinhMaBenhNhan();
                txtHoTen.Text = txtSDT.Text = txtDC.Text = txtTS.Text = txtTC.Text = "";
                dtpNS.Value   = DateTime.Today.AddYears(-30);
                cboGT.SelectedIndex = 0;
                lblSTT.Text   = $"🎫  SỐ THỨ TỰ TIẾP THEO   #{ClinicDAL.LaySTTTiepTheoTrongNgay()}";
            };

            btnTaiKham.Click += (s, e) =>
            {
                string sdt = Microsoft.VisualBasic.Interaction.InputBox("Nhập số điện thoại bệnh nhân cũ để tìm kiếm:", "Tái khám - Tìm BN cũ", "");
                if (string.IsNullOrWhiteSpace(sdt)) return;
                var dt = ClinicDAL.LayDanhSachBenhNhan(sdt);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy bệnh nhân với SĐT này!", "Không tìm thấy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var row = dt.Rows[0];
                txtMaBN.Text  = row["MaBN"].ToString() ?? "";
                txtHoTen.Text = row["HoTen"].ToString() ?? "";
                cboGT.SelectedItem = row["GioiTinh"].ToString();
                if (DateTime.TryParse(row["NgaySinh"].ToString(), out DateTime ns)) dtpNS.Value = ns;
                txtSDT.Text = row["SoDienThoai"].ToString() ?? "";
                txtDC.Text  = row["DiaChi"].ToString() ?? "";
                txtTS.Text  = row["TienSuBenh"].ToString() ?? "";
                MessageBox.Show($"Đã nạp hồ sơ: {row["HoTen"]}\nBấm 'CẤP PHIẾU KHÁM' để tạo lượt khám.", "Tìm thấy bệnh nhân", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnCap.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtHoTen.Text))
                {
                    MessageBox.Show("Vui lòng nhập họ tên bệnh nhân!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (cboBacSi.DataSource == null || cboBacSi.SelectedValue == null)
                {
                    MessageBox.Show("Vui lòng chọn bác sĩ khám!", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool isNewBN = !txtMaBN.Text.StartsWith("BN") ||
                    !ClinicDAL.LayDanhSachBenhNhan(txtMaBN.Text).Rows.Cast<DataRow>().Any(r => r["MaBN"].ToString() == txtMaBN.Text);

                var bn = new BenhNhan
                {
                    MaBN        = txtMaBN.Text.Trim(),
                    HoTen       = txtHoTen.Text.Trim(),
                    GioiTinh    = cboGT.SelectedItem?.ToString() ?? "Nam",
                    NgaySinh    = dtpNS.Value,
                    SoDienThoai = txtSDT.Text.Trim(),
                    DiaChi      = txtDC.Text.Trim(),
                    TienSuBenh  = txtTS.Text.Trim()
                };

                ClinicDAL.LuuBenhNhan(bn, isNew: isNewBN);

                int stt = ClinicDAL.LaySTTTiepTheoTrongNgay();
                var pk = new PhieuKham
                {
                    MaPK        = ClinicDAL.SinhMaPhieuKham(),
                    STT         = stt,
                    MaBN        = bn.MaBN,
                    MaNV_BacSi  = cboBacSi.SelectedValue?.ToString() ?? "",
                    TrieuChung  = txtTC.Text.Trim()
                };
                ClinicDAL.TaoPhieuKham(pk);

                MessageBox.Show($"✅ Cấp phiếu thành công!\n\n📄 Mã phiếu: {pk.MaPK}\n🎫 Số thứ tự: #{stt}\n👤 Bệnh nhân: {bn.HoTen}",
                    "Cấp phiếu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtMaBN.Text  = ClinicDAL.SinhMaBenhNhan();
                txtHoTen.Text = txtSDT.Text = txtDC.Text = txtTS.Text = txtTC.Text = "";
                lblSTT.Text   = $"🎫  SỐ THỨ TỰ TIẾP THEO   #{ClinicDAL.LaySTTTiepTheoTrongNgay()}";
                LoadHangCho();
            };

            // ── HÀNG CHỜ KHÁM ──
            var grpCho = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(12) };
            grpCho.Paint += PaintCardBorder;
            grpCho.Controls.Add(MakeLbl("HÀNG CHỜ KHÁM BỆNH TRONG NGÀY", 12, 10, bold: true));

            var pnlGridCho = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 30, 0, 0) };
            LoadHangCho();
            pnlGridCho.Controls.Add(dgvHangCho);
            grpCho.Controls.Add(pnlGridCho);

            split.Panel1.Controls.Add(grp);
            split.Panel2.Controls.Add(grpCho);
            wrapper.Controls.Add(split);
            panelContent.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════════
        //  LỄ TÂN & THU NGÂN: QUẢN LÝ HỒ SƠ BỆNH NHÂN
        // ═══════════════════════════════════════════════════════════
        private void ShowBenhNhanView()
        {
            SetActiveNav(btnNavBenhNhan);
            SetPageHeader("Lễ tân & Thu ngân  ›  Bệnh nhân", "Tra cứu, chỉnh sửa hồ sơ & xem lịch sử khám");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;

            var txtSearch  = MakeInput(90, 8, 220);
            var btnSearch  = SecondaryBtn("🔍 Tìm", 320, 8, 70, 34, C_Blue);
            var btnAdd     = PrimaryBtn("➕ Thêm BN mới", 400, 8, 140, 34);
            var btnEdit    = SecondaryBtn("✏ Sửa", 550, 8, 85, 34, C_Orange);
            var btnDelete  = SecondaryBtn("🗑 Xóa", 645, 8, 85, 34, C_Red);
            var btnHistory = SecondaryBtn("📋 Lịch sử khám", 740, 8, 145, 34, C_Purple);

            toolPanel.Controls.AddRange(new Control[] {
                MakeLbl("Tìm kiếm:", 14, 15),
                txtSearch, btnSearch, btnAdd, btnEdit, btnDelete, btnHistory
            });

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            contentPanel.Paint += PaintCardBorder;
            var dgvBN = MakeGrid();
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
                if (dgvBN.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn một bệnh nhân để sửa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var row = dgvBN.SelectedRows[0];
                var bn = new BenhNhan
                {
                    MaBN        = row.Cells["MaBN"].Value?.ToString() ?? "",
                    HoTen       = row.Cells["HoTen"].Value?.ToString() ?? "",
                    GioiTinh    = row.Cells["GioiTinh"].Value?.ToString() ?? "Nam",
                    SoDienThoai = row.Cells["SoDienThoai"].Value?.ToString(),
                    DiaChi      = row.Cells["DiaChi"].Value?.ToString(),
                    TienSuBenh  = row.Cells["TienSuBenh"].Value?.ToString(),
                };
                if (DateTime.TryParse(row.Cells["NgaySinh"].Value?.ToString(), out DateTime ns))
                    bn.NgaySinh = ns;
                var dlg = new FormThemSuaBN(bn);
                if (dlg.ShowDialog() == DialogResult.OK) LoadBN();
            };

            btnDelete.Click += (s, e) =>
            {
                if (dgvBN.SelectedRows.Count == 0) return;
                string maBN  = dgvBN.SelectedRows[0].Cells["MaBN"].Value?.ToString() ?? "";
                string tenBN = dgvBN.SelectedRows[0].Cells["HoTen"].Value?.ToString() ?? "";
                if (MessageBox.Show($"Xóa hồ sơ bệnh nhân '{tenBN}'?\n(Lưu ý: Chỉ xóa được nếu bệnh nhân chưa có phiếu khám)", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (ClinicDAL.XoaBenhNhan(maBN, out string err))
                    {
                        MessageBox.Show("Đã xóa bệnh nhân thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadBN();
                    }
                    else
                    {
                        MessageBox.Show("Không thể xóa: " + err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            btnHistory.Click += (s, e) =>
            {
                if (dgvBN.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn một bệnh nhân để xem lịch sử khám!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string maBN  = dgvBN.SelectedRows[0].Cells["MaBN"].Value?.ToString() ?? "";
                string tenBN = dgvBN.SelectedRows[0].Cells["HoTen"].Value?.ToString() ?? "";
                var dlg = new FormLichSuKham(maBN, tenBN);
                dlg.ShowDialog();
            };

            wrapper.Controls.Add(contentPanel);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════════
        //  LỄ TÂN & THU NGÂN: KHO DƯỢC PHẨM
        // ═══════════════════════════════════════════════════════════
        private void ShowKhoThuocView()
        {
            SetActiveNav(btnNavKhoThuoc);
            SetPageHeader("Lễ tân & Thu ngân  ›  Kho thuốc", "Tra cứu danh mục, kiểm tra tồn kho & nhập kho");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;

            var txtSearch  = MakeInput(95, 8, 180);
            var btnSearch  = SecondaryBtn("🔍 Tìm", 285, 8, 75, 34, C_Blue);
            var btnAddT    = PrimaryBtn("➕ Thêm thuốc", 370, 8, 125, 34);
            var btnEditT   = SecondaryBtn("✏ Sửa", 505, 8, 80, 34, C_Orange);
            var btnDeleteT = SecondaryBtn("🗑 Xóa", 595, 8, 80, 34, C_Red);
            var btnNhapKho = SecondaryBtn("📦 Nhập kho", 685, 8, 115, 34, C_Green);
            var btnCanhBao = SecondaryBtn("⚠ Sắp hết", 810, 8, 105, 34, C_Orange);

            toolPanel.Controls.AddRange(new Control[] {
                MakeLbl("Tên thuốc:", 12, 15),
                txtSearch, btnSearch, btnAddT, btnEditT, btnDeleteT, btnNhapKho, btnCanhBao
            });

            var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 640 };

            var pnlT = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            pnlT.Paint += PaintCardBorder;
            var dgvT = MakeGrid();
            dgvT.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvT.DataBindingComplete += (s, e) =>
            {
                SetInventoryColumn("MaThuoc", 0.8f, 70);
                SetInventoryColumn("TenThuoc", 1.8f, 130);
                SetInventoryColumn("DonViTinh", 0.9f, 75);
                SetInventoryColumn("DonGiaBan", 1.2f, 95);
                SetInventoryColumn("SoLuongTon", 1.1f, 85);
                SetInventoryColumn("CachDungMacDinh", 2.2f, 150);

                void SetInventoryColumn(string name, float fillWeight, int minimumWidth)
                {
                    if (dgvT.Columns.Contains(name) && dgvT.Columns[name] is DataGridViewColumn column)
                    {
                        column.FillWeight = fillWeight;
                        column.MinimumWidth = minimumWidth;
                    }
                }
            };
            pnlT.Controls.Add(dgvT);

            void LoadThuoc() => dgvT.DataSource = ClinicDAL.LayDanhSachThuoc(txtSearch.Text.Trim());
            LoadThuoc();
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadThuoc(); };
            btnSearch.Click   += (s, e) => LoadThuoc();
            split.Panel1.Controls.Add(pnlT);

            var grpCB = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            grpCB.Paint += PaintCardBorder;
            grpCB.Controls.Add(MakeLbl("⚠ THUỐC CÓ TỒN KHO THẤP (≤ 30)", 10, 10, bold: true));
            var pnlCBGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 30, 0, 0) };
            var dgvCB = MakeGrid();
            dgvCB.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
            pnlCBGrid.Controls.Add(dgvCB);
            grpCB.Controls.Add(pnlCBGrid);
            split.Panel2.Controls.Add(grpCB);

            btnAddT.Click += (s, e) =>
            {
                var dlg = new FormThemSuaThuoc(null);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadThuoc();
                    dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
                }
            };

            btnEditT.Click += (s, e) =>
            {
                if (dgvT.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn một thuốc để sửa!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var r = dgvT.SelectedRows[0];
                var t = new Thuoc
                {
                    MaThuoc        = r.Cells["MaThuoc"].Value?.ToString() ?? "",
                    TenThuoc       = r.Cells["TenThuoc"].Value?.ToString() ?? "",
                    DonViTinh      = r.Cells["DonViTinh"].Value?.ToString() ?? "",
                    DonGiaBan      = Convert.ToDecimal(r.Cells["DonGiaBan"].Value),
                    SoLuongTon     = Convert.ToInt32(r.Cells["SoLuongTon"].Value),
                    CachDungMacDinh= r.Cells["CachDungMacDinh"].Value?.ToString()
                };
                var dlg = new FormThemSuaThuoc(t);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadThuoc();
                    dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
                }
            };

            btnDeleteT.Click += (s, e) =>
            {
                if (dgvT.SelectedRows.Count == 0) return;
                string maThuoc  = dgvT.SelectedRows[0].Cells["MaThuoc"].Value?.ToString() ?? "";
                string tenThuoc = dgvT.SelectedRows[0].Cells["TenThuoc"].Value?.ToString() ?? "";
                if (MessageBox.Show($"Xóa thuốc '{tenThuoc}'?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    if (ClinicDAL.XoaThuoc(maThuoc, out string err))
                        LoadThuoc();
                    else
                        MessageBox.Show("Không thể xóa: " + err, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnNhapKho.Click += (s, e) =>
            {
                if (dgvT.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn thuốc cần nhập kho!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string maThuoc  = dgvT.SelectedRows[0].Cells["MaThuoc"].Value?.ToString() ?? "";
                string tenThuoc = dgvT.SelectedRows[0].Cells["TenThuoc"].Value?.ToString() ?? "";
                string input = Microsoft.VisualBasic.Interaction.InputBox($"Nhập số lượng cần bổ sung vào kho:\n{tenThuoc}", "Nhập kho dược", "100");
                if (int.TryParse(input, out int sl) && sl > 0)
                {
                    ClinicDAL.NhapKho(maThuoc, sl);
                    MessageBox.Show($"✅ Đã nhập thêm {sl} {dgvT.SelectedRows[0].Cells["DonViTinh"].Value} vào kho!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadThuoc();
                    dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
                }
            };

            btnCanhBao.Click += (s, e) => dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);

            wrapper.Controls.Add(split);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════════
        //  LỄ TÂN & THU NGÂN: THU NGÂN & HÓA ĐƠN
        // ═══════════════════════════════════════════════════════════
        private void ShowThuNganView()
        {
            SetActiveNav(btnNavThuNgan);
            SetPageHeader("Lễ tân & Thu ngân  ›  Thu ngân", "Tính viện phí, lập hóa đơn & xuất biên lai");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            var split   = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 370 };

            // ── CỘT TRÁI: CHỜ THANH TOÁN ──
            var grpCho = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            grpCho.Paint += PaintCardBorder;
            grpCho.Controls.Add(MakeLbl("CHỜ THANH TOÁN VIỆN PHÍ", 10, 10, bold: true));

            var dgvCho = MakeGrid(DockStyle.None);
            dgvCho.Location = new Point(10, 40);
            dgvCho.Size     = new Size(grpCho.Width - 20, grpCho.Height - 50);
            dgvCho.Anchor   = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            void LoadChoTT() => dgvCho.DataSource = ClinicDAL.LayDanhSachChoThanhToan();
            LoadChoTT();

            var btnRefreshChoTT = SecondaryBtn("🔄 Làm mới", 230, 6, 95, 28, C_Blue);
            btnRefreshChoTT.Click += (s, e) => LoadChoTT();
            grpCho.Controls.Add(btnRefreshChoTT);
            grpCho.Controls.Add(dgvCho);

            // ── CỘT PHẢI: LẬP HÓA ĐƠN & LỊCH SỬ ──
            var tabTN = new TabControl { Dock = DockStyle.Fill, Font = F_Base };

            // Tab 1: Lập hóa đơn
            var tabLap = new TabPage("  💰 Lập Hóa đơn  ");
            tabLap.BackColor = Color.White;

            var pLap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14) };

            var txtMaPK2      = MakeInput(125, 15, 120, readOnly: true);
            var txtTenBN2     = MakeInput(350, 15, 230, readOnly: true);
            var txtChanDoan2  = MakeInput(125, 52, 455, readOnly: true);

            var panelTien = new Panel { Location = new Point(10, 95), Size = new Size(570, 105), BackColor = Color.FromArgb(244, 246, 250) };
            panelTien.Paint += PaintCardBorder;

            var lblTienDVVal    = new Label { Text = "0 đ", Location = new Point(140, 12), AutoSize = true, Font = F_Bold, ForeColor = C_Blue };
            var lblTienThuocVal = new Label { Text = "0 đ", Location = new Point(410, 12), AutoSize = true, Font = F_Bold, ForeColor = C_Purple };
            var lblTongVal      = new Label { Text = "0 đ", Location = new Point(140, 50), AutoSize = true, Font = new Font("Segoe UI", 16F, FontStyle.Bold), ForeColor = C_Green };

            panelTien.Controls.AddRange(new Control[] {
                MakeLbl("Tiền dịch vụ CLS:", 12, 12), lblTienDVVal,
                MakeLbl("Tiền thuốc:", 300, 12), lblTienThuocVal,
                MakeLbl("TỔNG CỘNG:", 12, 54, bold: true), lblTongVal
            });

            var cboHT = new ComboBox { Location = new Point(125, 215), Size = new Size(160, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = F_Base };
            cboHT.Items.AddRange(new object[] { "Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "BHYT" });
            cboHT.SelectedIndex = 0;

            var numGG = new NumericUpDown { Location = new Point(400, 215), Size = new Size(120, 26), Maximum = 10000000, Increment = 5000, ThousandsSeparator = true, Font = F_Base };

            var btnTinhTien = SecondaryBtn("📊 Tính tiền", 125, 260, 140, 36, C_Blue);
            var btnXuatHD   = PrimaryBtn("💳 XUẤT HÓA ĐƠN", 280, 260, 200, 36);

            decimal curDV = 0, curThuoc = 0;

            btnTinhTien.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK2.Text))
                {
                    MessageBox.Show("Chọn phiếu khám trước!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var (dv, th) = ClinicDAL.TinhChiPhiPhieuKham(txtMaPK2.Text);
                curDV = dv; curThuoc = th;
                decimal tong = curDV + curThuoc - numGG.Value;
                lblTienDVVal.Text    = $"{curDV:N0} đ";
                lblTienThuocVal.Text = $"{curThuoc:N0} đ";
                lblTongVal.Text      = $"{Math.Max(0, tong):N0} đ";
            };

            numGG.ValueChanged += (s, e) =>
            {
                decimal tong = curDV + curThuoc - numGG.Value;
                lblTongVal.Text = $"{Math.Max(0, tong):N0} đ";
            };

            btnXuatHD.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtMaPK2.Text))
                {
                    MessageBox.Show("Chọn phiếu khám trước!", "Nhắc nhở", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (curDV + curThuoc == 0) btnTinhTien.PerformClick();

                var hd = new HoaDon
                {
                    MaHD        = ClinicDAL.SinhMaHoaDon(),
                    MaPK        = txtMaPK2.Text.Trim(),
                    MaNV_ThuNgan= SessionState.CurrentUser?.MaNV ?? "NV004",
                    TienDichVu  = curDV,
                    TienThuoc   = curThuoc,
                    GiamGia     = numGG.Value,
                    TongTien    = curDV + curThuoc - numGG.Value,
                    HinhThucTT  = cboHT.SelectedItem?.ToString() ?? "Tiền mặt",
                    TrangThai   = "Đã thanh toán"
                };
                try
                {
                    ClinicDAL.TaoHoaDon(hd);
                    MessageBox.Show($"✅ Thanh toán thành công!\n\n📄 Mã HĐ: {hd.MaHD}\n💰 Tổng tiền: {hd.TongTien:N0} VNĐ\n📱 Hình thức: {hd.HinhThucTT}",
                        "Hóa đơn viện phí", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadChoTT();
                    txtMaPK2.Text = txtTenBN2.Text = txtChanDoan2.Text = "";
                    lblTienDVVal.Text = lblTienThuocVal.Text = "0 đ";
                    lblTongVal.Text   = "0 đ";
                    curDV = curThuoc  = 0;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi xuất hóa đơn: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dgvCho.SelectionChanged += (s, e) =>
            {
                if (dgvCho.SelectedRows.Count == 0) return;
                var r = dgvCho.SelectedRows[0];
                txtMaPK2.Text     = r.Cells["MaPK"].Value?.ToString() ?? "";
                txtTenBN2.Text    = r.Cells["TenBN"].Value?.ToString() ?? "";
                txtChanDoan2.Text = r.Cells["ChanDoan"].Value?.ToString() ?? "";
                curDV = curThuoc  = 0;
                lblTienDVVal.Text = lblTienThuocVal.Text = "0 đ";
                lblTongVal.Text   = "0 đ";
            };

            pLap.Controls.AddRange(new Control[] {
                MakeLbl("Mã phiếu:", 12, 18), txtMaPK2,
                MakeLbl("Bệnh nhân:", 265, 18), txtTenBN2,
                MakeLbl("Chẩn đoán:", 12, 55), txtChanDoan2,
                panelTien,
                MakeLbl("Hình thức:", 12, 218), cboHT,
                MakeLbl("Giảm trừ:", 320, 218), numGG,
                btnTinhTien, btnXuatHD
            });
            tabLap.Controls.Add(pLap);

            // Tab 2: Lịch sử hóa đơn
            var tabLS = new TabPage("  📋 Lịch sử Hóa đơn  ");
            tabLS.BackColor = Color.White;
            var dgvHD = MakeGrid();
            var btnRefreshHD = SecondaryBtn("🔄 Tải lại", 10, 6, 90, 28, C_Blue);
            btnRefreshHD.Click += (s, e) => dgvHD.DataSource = ClinicDAL.LayDanhSachHoaDon();
            dgvHD.DataSource = ClinicDAL.LayDanhSachHoaDon();
            var pLSTop = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White };
            pLSTop.Controls.Add(btnRefreshHD);
            tabLS.Controls.Add(new Panel { Dock = DockStyle.Fill, Controls = { dgvHD } });
            tabLS.Controls.Add(pLSTop);

            tabTN.TabPages.AddRange(new[] { tabLap, tabLS });

            split.Panel1.Controls.Add(grpCho);
            split.Panel2.Controls.Add(tabTN);
            wrapper.Controls.Add(split);
            panelContent.Controls.Add(wrapper);
        }
    }
}
