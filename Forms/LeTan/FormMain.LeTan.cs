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
        private void ShowLeTanDashboard()
        {
            SetRefreshPageAction(ShowLeTanDashboard);
            SetActiveNav(btnNavTongQuan);
            SetPageHeader("Lễ tân & Thu ngân  ›  Trang chủ", "Tổng quan tiếp đón và thanh toán");
            panelContent.Controls.Clear();

            try
            {
                string maNV = SessionState.CurrentUser?.MaNV ?? "";
                DataTable queue = ClinicDAL.LayHangChoKhamTrongNgay(maLeTan: maNV);
                DataTable appointments = ClinicDAL.LayDanhSachLichHen();
                var allowedDoctors = ClinicDAL.LayDanhSachBacSiTheoNhanVien(maNV).AsEnumerable()
                    .Select(row => row["MaNV"].ToString() ?? "")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                DataTable waitingBills = ClinicDAL.LayDanhSachChoThanhToan();
                DataTable invoices = ClinicDAL.LayDanhSachHoaDon();
                DateTime today = DateTime.Today;
                int todayAppointments = appointments.AsEnumerable()
                    .Count(row => row.Field<DateTime>("NgayHen").Date == today
                        && allowedDoctors.Contains(row["MaNV_BacSi"].ToString() ?? ""));
                int todayBills = invoices.AsEnumerable()
                    .Count(row => row.Field<DateTime>("NgayLap").Date == today);

                var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
                var title = MakeLbl("TIẾP NHẬN BỆNH NHÂN", 12, 8, bold: true);
                title.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
                var btnReceive = PrimaryBtn("＋ Tiếp nhận", 12, 40, 135, 36);
                var btnBook = SecondaryBtn("＋ Thêm lịch hẹn", 157, 40, 145, 36, C_Teal);
                var btnPayment = SecondaryBtn("💳 Thanh toán", 312, 40, 135, 36, C_Blue);
                btnReceive.Click += (s, e) => ShowTiepDonView();
                btnBook.Click += (s, e) => OpenReceptionAppointment();
                btnPayment.Click += (s, e) => ShowThuNganView();

                var cards = new TableLayoutPanel
                {
                    Location = new Point(8, 88),
                    Size = new Size(1000, 100),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    ColumnCount = 4,
                    RowCount = 1,
                    BackColor = Color.Transparent
                };
                for (int index = 0; index < 4; index++)
                    cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
                cards.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                cards.Controls.Add(MakeLeTanKpi("HÀNG CHỜ KHÁM", queue.Rows.Count.ToString(), "Phiếu trong phạm vi phòng được giao", C_Teal), 0, 0);
                cards.Controls.Add(MakeLeTanKpi("LỊCH HẸN HÔM NAY", todayAppointments.ToString(), "Lịch hẹn toàn phòng khám", C_Blue), 1, 0);
                cards.Controls.Add(MakeLeTanKpi("CHỜ THANH TOÁN", waitingBills.Rows.Count.ToString(), "Hồ sơ đã khám xong", C_Orange), 2, 0);
                cards.Controls.Add(MakeLeTanKpi("HÓA ĐƠN HÔM NAY", todayBills.ToString(), "Hóa đơn đã lập hôm nay", C_Purple), 3, 0);

                var queueCard = new Panel
                {
                    Location = new Point(8, 202),
                    Size = new Size(1000, 430),
                    Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                    BackColor = Color.White,
                    Padding = new Padding(12)
                };
                queueCard.Paint += PaintCardBorder;
                queueCard.Controls.Add(MakeLbl("DANH SÁCH TIẾP NHẬN HÔM NAY", 14, 12, bold: true));
                var queueGrid = MakeGrid();
                queueGrid.Location = new Point(14, 48);
                queueGrid.Size = new Size(queueCard.ClientSize.Width - 28, queueCard.ClientSize.Height - 62);
                queueGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                queueGrid.DataSource = queue;
                queueCard.Controls.Add(queueGrid);

                wrapper.Controls.AddRange(new Control[] { title, btnReceive, btnBook, btnPayment, cards, queueCard });
                panelContent.Controls.Add(wrapper);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không tải được tổng quan lễ tân",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel MakeLeTanKpi(string title, string value, string subtitle, Color accent)
        {
            var card = new Panel { Dock = DockStyle.Fill, Margin = new Padding(5), BackColor = Color.White };
            card.Paint += PaintCardBorder;
            card.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent });
            card.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(16, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = C_TextGray
            });
            card.Controls.Add(new Label
            {
                Text = value,
                Location = new Point(16, 34),
                AutoSize = true,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = accent
            });
            card.Controls.Add(new Label
            {
                Text = subtitle,
                Location = new Point(16, 74),
                AutoSize = true,
                Font = F_Small,
                ForeColor = C_TextGray
            });
            return card;
        }

        private void ShowLeTanLichKhamView(string keyword = "", string? status = null, DateTime? selectedDate = null)
        {
            SetRefreshPageAction(() => ShowLeTanLichKhamView(keyword, status, selectedDate));
            SetActiveNav(btnNavKhamBenh);
            SetPageHeader("Lễ tân & Thu ngân  ›  Lịch khám", "Quản lý lịch khám và yêu cầu đặt lịch");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 112, BackColor = Color.White, Padding = new Padding(12) };
            toolbar.Paint += PaintCardBorder;
            var datePicker = new DateTimePicker
            {
                Location = new Point(12, 10),
                Size = new Size(150, 30),
                Format = DateTimePickerFormat.Short,
                Value = selectedDate ?? DateTime.Today
            };
            var search = MakeInput(12, 53, 220);
            search.PlaceholderText = "Mã lịch, bệnh nhân, bác sĩ...";
            var statusFilter = new ComboBox
            {
                Location = new Point(244, 53),
                Size = new Size(165, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            statusFilter.Items.AddRange(new object[] { "Tất cả trạng thái", "Chờ xác nhận", "Đã xác nhận", "Đã đến", "Đã hủy" });
            statusFilter.SelectedItem = status ?? "Tất cả trạng thái";
            if (statusFilter.SelectedIndex < 0)
                statusFilter.SelectedIndex = 0;
            var refresh = SecondaryBtn("⌕ Tìm kiếm", 420, 52, 110, 32, C_Blue);
            var approve = PrimaryBtn("✓ Duyệt lịch", 0, 52, 115, 32);
            approve.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var add = PrimaryBtn("＋ Đặt lịch", 0, 10, 125, 34);
            add.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var edit = SecondaryBtn("✎ Sửa lịch", 0, 10, 115, 34, C_Orange);
            edit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var cancel = SecondaryBtn("Hủy lịch", 0, 10, 105, 34, C_Red);
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var pendingCount = MakeLbl("Chờ duyệt: 0", 540, 57, bold: true);
            pendingCount.ForeColor = C_Orange;
            toolbar.Controls.AddRange(new Control[] { datePicker, search, statusFilter, refresh, approve, pendingCount, add, edit, cancel });

            var gridPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            gridPanel.Paint += PaintCardBorder;
            var grid = MakeGrid();
            gridPanel.Controls.Add(grid);

            void LoadAppointments()
            {
                string selectedStatus = statusFilter.SelectedItem?.ToString() ?? "Tất cả trạng thái";
                var all = ClinicDAL.LayDanhSachLichHen(search.Text.Trim());
                var allowedDoctors = ClinicDAL.LayDanhSachBacSiTheoNhanVien(
                    SessionState.CurrentUser?.MaNV ?? "").AsEnumerable()
                    .Select(row => row["MaNV"].ToString() ?? "")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var filtered = all.Clone();
                filtered.Columns.Add("TrangThaiDuyet", typeof(string));
                foreach (DataRow row in all.Rows)
                    if (row.Field<DateTime>("NgayHen").Date == datePicker.Value.Date
                        && allowedDoctors.Contains(row["MaNV_BacSi"].ToString() ?? "")
                        && (selectedStatus == "Tất cả trạng thái" || row["TrangThai"].ToString() == selectedStatus))
                    {
                        DataRow displayed = filtered.NewRow();
                        foreach (DataColumn column in all.Columns)
                            displayed[column.ColumnName] = row[column];
                        displayed["TrangThaiDuyet"] = row["TrangThai"].ToString() switch
                        {
                            "Chờ xác nhận" => "Chờ duyệt",
                            "Đã xác nhận" => "Đã duyệt",
                            _ => row["TrangThai"].ToString() ?? ""
                        };
                        filtered.Rows.Add(displayed);
                    }
                grid.DataSource = filtered;
                int waitingCount = all.AsEnumerable().Count(row =>
                    row.Field<DateTime>("NgayHen").Date == datePicker.Value.Date
                    && allowedDoctors.Contains(row["MaNV_BacSi"].ToString() ?? "")
                    && row["TrangThai"].ToString() == "Chờ xác nhận");
                pendingCount.Text = $"Chờ duyệt: {waitingCount}";
                DataGridViewColumn? rawStatusColumn = grid.Columns["TrangThai"];
                if (rawStatusColumn != null)
                    rawStatusColumn.Visible = false;
                DataGridViewColumn? statusColumn = grid.Columns["TrangThaiDuyet"];
                if (statusColumn != null)
                    statusColumn.HeaderText = "Trạng thái";
            }

            DataRow? SelectedAppointment()
                => grid.SelectedRows.Count > 0 && grid.SelectedRows[0].DataBoundItem is DataRowView view
                    ? view.Row
                    : null;

            refresh.Click += (s, e) => LoadAppointments();
            search.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    LoadAppointments();
            };
            datePicker.ValueChanged += (s, e) => LoadAppointments();
            statusFilter.SelectedIndexChanged += (s, e) => LoadAppointments();
            add.Click += (s, e) => OpenReceptionAppointment();
            approve.Click += (s, e) =>
            {
                DataRow? selected = SelectedAppointment();
                if (selected == null)
                {
                    MessageBox.Show("Chọn lịch hẹn đang chờ duyệt.", "Chưa chọn lịch",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (selected["TrangThai"].ToString() != "Chờ xác nhận")
                {
                    MessageBox.Show("Chỉ lịch đang chờ xác nhận mới được duyệt.",
                        "Không thể duyệt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show(
                        $"Xác nhận lịch hẹn của {selected["TenBN"]} với bác sĩ {selected["TenBacSi"]}?",
                        "Duyệt lịch hẹn", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
                if (!ClinicDAL.DuyetLichHen(
                        Convert.ToInt32(selected["MaLichHen"]),
                        SessionState.CurrentUser?.MaNV ?? ""))
                {
                    MessageBox.Show("Không thể duyệt lịch. Lịch có thể đã được xử lý hoặc nằm ngoài phòng được phân công.",
                        "Duyệt lịch thất bại", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                MessageBox.Show($"Đã duyệt lịch của {selected["TenBN"]}. Lịch đã xác nhận sẽ hiển thị trong danh sách của bác sĩ.",
                    "Đã duyệt lịch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAppointments();
            };
            edit.Click += (s, e) =>
            {
                DataRow? selected = SelectedAppointment();
                if (selected == null)
                {
                    MessageBox.Show("Chọn lịch hẹn cần sửa.", "Chưa chọn lịch",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (selected["TrangThai"].ToString() != "Chờ xác nhận")
                {
                    MessageBox.Show("Chỉ được sửa lịch đang chờ xác nhận. Lịch đã duyệt cần được hủy và đặt lại nếu cần thay đổi.",
                        "Không thể sửa lịch",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                var assignedDoctors = ClinicDAL.LayDanhSachBacSiTheoNhanVien(
                    SessionState.CurrentUser?.MaNV ?? "").AsEnumerable()
                    .Select(row => row["MaNV"].ToString() ?? "")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!assignedDoctors.Contains(selected["MaNV_BacSi"].ToString() ?? ""))
                {
                    MessageBox.Show("Bác sĩ của lịch này không thuộc phòng được phân công cho tài khoản hiện tại.",
                        "Ngoài phạm vi phân công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                using var dialog = new FormThemLichHenLeTan(SessionState.CurrentUser?.MaNV ?? "", selected);
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    LoadAppointments();
            };
            cancel.Click += (s, e) =>
            {
                DataRow? selected = SelectedAppointment();
                if (selected == null)
                {
                    MessageBox.Show("Chọn lịch hẹn cần hủy.", "Chưa chọn lịch",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (MessageBox.Show($"Hủy lịch hẹn của {selected["TenBN"]}?",
                        "Xác nhận hủy", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                if (!ClinicDAL.HuyLichHen(Convert.ToInt32(selected["MaLichHen"])))
                {
                    MessageBox.Show("Chỉ lịch chờ xác nhận hoặc đã xác nhận mới hủy được.",
                        "Không thể hủy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                LoadAppointments();
            };

            toolbar.SizeChanged += (s, e) =>
            {
                int right = toolbar.ClientSize.Width - 12;
                cancel.Location = new Point(right - cancel.Width, 10);
                edit.Location = new Point(cancel.Left - edit.Width - 8, 10);
                add.Location = new Point(edit.Left - add.Width - 8, 10);
                approve.Location = new Point(right - approve.Width, 52);
                pendingCount.Location = new Point(approve.Left - pendingCount.Width - 12, 57);
            };
            panelContent.Controls.Add(gridPanel);
            panelContent.Controls.Add(toolbar);
            LoadAppointments();
        }

        private void OpenReceptionAppointment()
        {
            using var dialog = new FormThemLichHenLeTan(SessionState.CurrentUser?.MaNV ?? "");
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                if (SessionState.IsReceptionist)
                    ShowLeTanLichKhamView(selectedDate: DateTime.Today);
                else
                    ShowLeTanDashboard();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  LỄ TÂN & THU NGÂN: TIẾP ĐÓN & ĐĂNG KÝ KHÁM
        // ═══════════════════════════════════════════════════════════
        private void ShowTiepDonView()
        {
            SetRefreshPageAction(ShowTiepDonView);
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
                ColumnCount = 4,
                RowCount = 3,
                Padding = new Padding(16, 8, 16, 4),
                BackColor = C_Card
            };
            for (int i = 0; i < 4; i++)
            {
                fieldsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            }
            for (int i = 0; i < 3; i++)
            {
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

            // Room assignment restricts the available doctors to the receptionist's rooms.
            string maLeTan = SessionState.CurrentUser?.MaNV ?? "";
            var cboPhong = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Font = F_Base };
            var dtPhong = ClinicDAL.LayDanhSachPhongTheoNhanVien(maLeTan);
            cboPhong.DataSource = dtPhong;
            cboPhong.DisplayMember = "TenPhong";
            cboPhong.ValueMember = "MaPhong";
            if (dtPhong.Rows.Count == 0)
            {
                registrationHeader.Controls.Add(new Label
                {
                    Text = "Tài khoản chưa được phân công phòng khám",
                    Location = new Point(520, 18),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(252, 165, 165),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold)
                });
            }

            var cboBacSi = new ComboBox { Location = new Point(115, 86), Size = new Size(230, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = F_Base };
            void LoadDoctorsForRoom()
            {
                string? maPhong = cboPhong.SelectedValue?.ToString();
                var doctors = string.IsNullOrEmpty(maPhong)
                    ? new DataTable()
                    : ClinicDAL.LayDanhSachBacSiTheoPhong(maPhong);
                cboBacSi.DataSource = doctors;
                cboBacSi.DisplayMember = "HoTen";
                cboBacSi.ValueMember = "MaNV";
                cboBacSi.Enabled = doctors.Rows.Count > 0;
            }
            cboPhong.SelectedValueChanged += (s, e) => LoadDoctorsForRoom();
            if (dtPhong.Rows.Count > 0)
                LoadDoctorsForRoom();

            var txtTC    = MakeInput(495, 86, 360);

            var lblSTT = new Label
            {
                Text      = $"🎫  SỐ THỨ TỰ TIẾP THEO   #{ClinicDAL.LaySTTTiepTheoTrongNgay(cboPhong.SelectedValue?.ToString())}",
                Location  = new Point(20, 24),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = C_Teal
            };
            var btnCap     = PrimaryBtn("🎟  CẤP PHIẾU KHÁM", 0, 0, 200, 38);
            btnCap.Enabled = dtPhong.Rows.Count > 0;
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
            fieldsGrid.Controls.Add(CreateField("GIỚI TÍNH", cboGT), 3, 0);
            fieldsGrid.Controls.Add(CreateField("NGÀY SINH", dtpNS), 0, 1);
            fieldsGrid.Controls.Add(CreateField("PHÒNG KHÁM *", cboPhong), 1, 1);
            fieldsGrid.Controls.Add(CreateField("BÁC SĨ KHÁM *", cboBacSi), 2, 1);
            fieldsGrid.Controls.Add(CreateField("ĐỊA CHỈ", txtDC), 3, 1);
            fieldsGrid.Controls.Add(CreateField("TIỀN SỬ BỆNH / DỊ ỨNG", txtTS), 0, 2);
            fieldsGrid.Controls.Add(CreateField("TRIỆU CHỨNG", txtTC), 1, 2);

            var dgvHangCho = MakeGrid();
            void LoadHangCho() => dgvHangCho.DataSource = ClinicDAL.LayHangChoKhamTrongNgay(
                maPhong: cboPhong.SelectedValue?.ToString(),
                maLeTan: maLeTan);

            void UpdateRoomQueue()
            {
                LoadHangCho();
                lblSTT.Text = $"🎫  SỐ THỨ TỰ TIẾP THEO   #{ClinicDAL.LaySTTTiepTheoTrongNgay(cboPhong.SelectedValue?.ToString())}";
            }
            cboPhong.SelectedValueChanged += (s, e) => UpdateRoomQueue();

            btnReset.Click += (s, e) =>
            {
                txtMaBN.Text  = ClinicDAL.SinhMaBenhNhan();
                txtHoTen.Text = txtSDT.Text = txtDC.Text = txtTS.Text = txtTC.Text = "";
                dtpNS.Value   = DateTime.Today.AddYears(-30);
                cboGT.SelectedIndex = 0;
                lblSTT.Text   = $"🎫  SỐ THỨ TỰ TIẾP THEO   #{ClinicDAL.LaySTTTiepTheoTrongNgay(cboPhong.SelectedValue?.ToString())}";
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
                    MessageBox.Show("Phòng khám chưa được phân công bác sĩ. Vui lòng liên hệ quản trị viên.", "Thiếu phân công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                if (!ClinicDAL.LuuBenhNhan(bn, isNew: isNewBN))
                {
                    MessageBox.Show("Không lưu được hồ sơ bệnh nhân. Vui lòng kiểm tra dữ liệu và thử lại.", "Lỗi lưu hồ sơ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string maPhong = cboPhong.SelectedValue?.ToString() ?? "";
                int stt = ClinicDAL.LaySTTTiepTheoTrongNgay(maPhong);
                var pk = new PhieuKham
                {
                    MaPK        = ClinicDAL.SinhMaPhieuKham(),
                    STT         = stt,
                    MaBN        = bn.MaBN,
                    MaNV_BacSi  = cboBacSi.SelectedValue?.ToString() ?? "",
                    MaPhong     = maPhong,
                    TrieuChung  = txtTC.Text.Trim()
                };
                if (!ClinicDAL.TaoPhieuKham(pk, maLeTan))
                {
                    MessageBox.Show("Không tạo được phiếu khám. Kiểm tra phân công bác sĩ/phòng và thử lại.", "Lỗi cấp phiếu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"✅ Cấp phiếu thành công!\n\n📄 Mã phiếu: {pk.MaPK}\n🎫 Số thứ tự: #{stt}\n👤 Bệnh nhân: {bn.HoTen}",
                    "Cấp phiếu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtMaBN.Text  = ClinicDAL.SinhMaBenhNhan();
                txtHoTen.Text = txtSDT.Text = txtDC.Text = txtTS.Text = txtTC.Text = "";
                lblSTT.Text   = $"🎫  SỐ THỨ TỰ TIẾP THEO   #{ClinicDAL.LaySTTTiepTheoTrongNgay(cboPhong.SelectedValue?.ToString())}";
                LoadHangCho();
            };

            // ── HÀNG CHỜ KHÁM ──
            var grpCho = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(12) };
            grpCho.Paint += PaintCardBorder;
            grpCho.Controls.Add(MakeLbl("HÀNG CHỜ KHÁM BỆNH TRONG NGÀY", 12, 10, bold: true));
            var btnCancelTicket = SecondaryBtn("Hủy phiếu đang chờ", 780, 4, 180, 30, C_Red);
            btnCancelTicket.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancelTicket.Click += (s, e) =>
            {
                if (dgvHangCho.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn phiếu đang chờ cần hủy.", "Chưa chọn phiếu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string maPK = dgvHangCho.SelectedRows[0].Cells["MaPK"].Value?.ToString() ?? "";
                if (MessageBox.Show(
                        $"Hủy phiếu {maPK}? Chỉ phiếu đang chờ khám và chưa có hóa đơn mới được hủy.",
                        "Xác nhận hủy phiếu",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                if (!ClinicDAL.HuyPhieuKhamChoKham(maPK))
                {
                    MessageBox.Show("Không hủy được phiếu. Phiếu có thể đã bắt đầu khám hoặc đã có hóa đơn.",
                        "Không thể hủy phiếu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                UpdateRoomQueue();
            };

            var pnlGridCho = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 30, 0, 0) };
            LoadHangCho();
            pnlGridCho.Controls.Add(dgvHangCho);
            grpCho.Controls.Add(pnlGridCho);
            grpCho.Controls.Add(btnCancelTicket);

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
            SetRefreshPageAction(ShowBenhNhanView);
            SetActiveNav(btnNavBenhNhan);
            string roleLabel = SessionState.IsAdmin ? "Quản trị viên" : "Lễ tân & Thu ngân";
            SetPageHeader($"{roleLabel}  ›  Bệnh nhân", "Tra cứu, chỉnh sửa hồ sơ & xem lịch sử khám");
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
            SetRefreshPageAction(ShowKhoThuocView);
            SetActiveNav(btnNavKhoThuoc);
            string roleLabel = SessionState.IsAdmin ? "Quản trị viên" : "Lễ tân & Thu ngân";
            SetPageHeader($"{roleLabel}  ›  Kho thuốc", "Tra cứu danh mục, kiểm tra tồn kho & nhập kho");
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
            SetRefreshPageAction(ShowThuNganView);
            SetActiveNav(btnNavThuNgan);
            SetPageHeader("Lễ tân & Thu ngân  ›  Thanh toán", "Quản lý hóa đơn và thanh toán viện phí");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            var header = new Panel { Dock = DockStyle.Top, Height = 112 };
            header.Controls.Add(MakeLbl("THANH TOÁN & HÓA ĐƠN", 0, 0, bold: true));
            var summary = new Label
            {
                Location = new Point(0, 28),
                AutoSize = true,
                Font = F_Small,
                ForeColor = C_TextGray,
                Text = "Tra cứu hóa đơn, xem chi tiết và ghi nhận thanh toán."
            };
            header.Controls.Add(summary);
            var search = MakeInput(0, 60, 250);
            search.PlaceholderText = "Mã hóa đơn, phiếu khám, bệnh nhân...";
            var status = new ComboBox
            {
                Location = new Point(260, 60),
                Size = new Size(170, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            status.Items.AddRange(new object[] { "Tất cả trạng thái", "Chưa thanh toán", "Đã thanh toán", "Đã hủy" });
            status.SelectedIndex = 0;
            var refresh = SecondaryBtn("Làm mới", 440, 59, 100, 32, C_Blue);
            var pay = PrimaryBtn("Thanh toán", 550, 59, 120, 32);
            var details = SecondaryBtn("Chi tiết hóa đơn", 680, 59, 150, 32, C_Teal);
            var cancelInvoice = SecondaryBtn("Hủy HĐ", 840, 59, 95, 32, C_Red);
            header.Controls.AddRange(new Control[] { search, status, refresh, pay, details, cancelInvoice });

            var grid = MakeGrid();
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 250, Padding = new Padding(0, 8, 0, 0) };
            var pendingTitle = MakeLbl("PHIẾU KHÁM CHỜ LẬP HÓA ĐƠN", 0, 0, bold: true);
            var createInvoice = PrimaryBtn("＋ Lập hóa đơn", 0, 0, 130, 30);
            createInvoice.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var pendingGrid = MakeGrid();
            pendingGrid.Location = new Point(0, 32);
            pendingGrid.Size = new Size(bottom.ClientSize.Width, bottom.ClientSize.Height - 36);
            pendingGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            bottom.Controls.AddRange(new Control[] { pendingTitle, createInvoice, pendingGrid });
            bottom.SizeChanged += (s, e) => createInvoice.Location = new Point(bottom.ClientSize.Width - createInvoice.Width, 0);
            var invoicesPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 5) };
            invoicesPanel.Paint += PaintCardBorder;
            invoicesPanel.Controls.Add(grid);

            DataTable allInvoices = new DataTable();
            void LoadInvoices()
            {
                allInvoices = ClinicDAL.LayDanhSachHoaDon();
                string keyword = search.Text.Trim();
                string state = status.SelectedItem?.ToString() ?? "Tất cả trạng thái";
                var filtered = allInvoices.Clone();
                foreach (DataRow row in allInvoices.Rows)
                {
                    bool matchesKeyword = string.IsNullOrEmpty(keyword)
                        || new[] { "MaHD", "MaPK", "TenBN" }.Any(column =>
                            row[column]?.ToString()?.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) == true);
                    bool matchesStatus = state == "Tất cả trạng thái" || row["TrangThai"].ToString() == state;
                    if (matchesKeyword && matchesStatus)
                        filtered.ImportRow(row);
                }
                grid.DataSource = filtered;
                DataGridViewColumnCollection? invoiceColumns = grid.Columns;
                DataGridViewColumn? totalColumn = invoiceColumns?["TongTien"];
                if (totalColumn != null)
                    totalColumn.DefaultCellStyle.Format = "N0";
                DataGridViewColumn? dateColumn = invoiceColumns?["NgayLap"];
                if (dateColumn != null)
                    dateColumn.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";

                var waiting = ClinicDAL.LayDanhSachChoThanhToan();
                pendingGrid.DataSource = waiting;
                summary.Text = $"Tổng {allInvoices.Rows.Count} hóa đơn  •  {waiting.Rows.Count} phiếu khám chờ lập hóa đơn";
            }

            DataRow? SelectedInvoice()
                => grid.SelectedRows.Count > 0 && grid.SelectedRows[0].DataBoundItem is DataRowView view
                    ? view.Row
                    : null;

            refresh.Click += (s, e) => LoadInvoices();
            search.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    LoadInvoices();
            };
            status.SelectedIndexChanged += (s, e) => LoadInvoices();
            details.Click += (s, e) =>
            {
                DataRow? invoice = SelectedInvoice();
                if (invoice == null)
                {
                    MessageBox.Show("Chọn hóa đơn cần xem chi tiết.", "Chưa chọn hóa đơn",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                ShowInvoiceDetails(invoice);
            };
            pay.Click += (s, e) =>
            {
                DataRow? invoice = SelectedInvoice();
                if (invoice == null)
                {
                    MessageBox.Show("Chọn hóa đơn cần thanh toán.", "Chưa chọn hóa đơn",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (invoice["TrangThai"].ToString() != "Chưa thanh toán")
                {
                    MessageBox.Show("Chỉ có thể thanh toán hóa đơn đang ở trạng thái chưa thanh toán.",
                        "Không thể thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (ShowInvoicePayment(invoice))
                    LoadInvoices();
            };
            cancelInvoice.Click += (s, e) =>
            {
                DataRow? invoice = SelectedInvoice();
                if (invoice == null)
                {
                    MessageBox.Show("Chọn hóa đơn cần hủy.", "Chưa chọn hóa đơn",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string maHD = invoice["MaHD"].ToString() ?? "";
                if (invoice["TrangThai"].ToString() != "Chưa thanh toán")
                {
                    MessageBox.Show("Chỉ hủy được hóa đơn chưa thanh toán.",
                        "Không thể hủy", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (MessageBox.Show($"Hủy hóa đơn {maHD} chưa thanh toán?",
                        "Xác nhận hủy hóa đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
                if (!ClinicDAL.HuyHoaDonChuaThanhToan(maHD))
                {
                    MessageBox.Show("Không thể hủy hóa đơn; trạng thái có thể đã thay đổi.",
                        "Lỗi hủy hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                LoadInvoices();
            };
            pendingGrid.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex < 0 || pendingGrid.Rows[e.RowIndex].DataBoundItem is not DataRowView view)
                    return;
                CreateInvoice(view.Row);
                LoadInvoices();
            };
            createInvoice.Click += (s, e) =>
            {
                if (pendingGrid.SelectedRows.Count == 0
                    || pendingGrid.SelectedRows[0].DataBoundItem is not DataRowView view)
                {
                    MessageBox.Show("Chọn phiếu khám cần lập hóa đơn.", "Chưa chọn phiếu khám",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                CreateInvoice(view.Row);
                LoadInvoices();
            };

            wrapper.Controls.Add(invoicesPanel);
            wrapper.Controls.Add(bottom);
            wrapper.Controls.Add(header);
            panelContent.Controls.Add(wrapper);
            LoadInvoices();
        }

        private void ShowInvoiceDetails(DataRow invoice)
        {
            string maHD = invoice["MaHD"].ToString() ?? "";
            var dialog = new Form
            {
                Text = $"Chi tiết hóa đơn {maHD}",
                Size = new Size(760, 500),
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                BackColor = Color.White
            };
            var heading = new Label
            {
                Text = $"HÓA ĐƠN {maHD}   •   {invoice["TenBN"]}",
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(14, 12, 0, 0),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = C_TextDark
            };
            var grid = MakeGrid();
            grid.Dock = DockStyle.Fill;
            grid.DataSource = ClinicDAL.LayChiTietHoaDon(maHD);
            DataGridViewColumnCollection? detailColumns = grid.Columns;
            foreach (string column in new[] { "DonGia", "ThanhTien" })
            {
                DataGridViewColumn? detailColumn = detailColumns?[column];
                if (detailColumn != null)
                    detailColumn.DefaultCellStyle.Format = "N0";
            }
            var footer = new Label
            {
                Text = $"Dịch vụ: {Convert.ToDecimal(invoice["TienDichVu"]):N0} đ     Thuốc: {Convert.ToDecimal(invoice["TienThuoc"]):N0} đ     Giảm giá: {Convert.ToDecimal(invoice["GiamGia"]):N0} đ     TỔNG: {Convert.ToDecimal(invoice["TongTien"]):N0} đ",
                Dock = DockStyle.Bottom,
                Height = 48,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 14, 0),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = C_Blue
            };
            dialog.Controls.Add(grid);
            dialog.Controls.Add(footer);
            dialog.Controls.Add(heading);
            dialog.ShowDialog(this);
            dialog.Dispose();
        }

        private bool ShowInvoicePayment(DataRow invoice)
        {
            string maHD = invoice["MaHD"].ToString() ?? "";
            using var dialog = new Form
            {
                Text = "Xác nhận thanh toán",
                Size = new Size(480, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };
            var amount = new Label
            {
                Text = $"{Convert.ToDecimal(invoice["TongTien"]):N0} đ",
                Dock = DockStyle.Top,
                Height = 78,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 23F, FontStyle.Bold),
                ForeColor = C_Green
            };
            var label = new Label { Text = $"Hóa đơn {maHD}  •  {invoice["TenBN"]}", Dock = DockStyle.Top, Height = 30, TextAlign = ContentAlignment.MiddleCenter };
            var method = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 250,
                Font = F_Base,
                Anchor = AnchorStyles.None
            };
            method.Items.AddRange(new object[] { "Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "BHYT" });
            string currentMethod = invoice["HinhThucTT"].ToString() ?? "";
            method.SelectedItem = method.Items.Contains(currentMethod) ? currentMethod : "Tiền mặt";
            var methodPanel = new Panel { Dock = DockStyle.Top, Height = 48 };
            methodPanel.Controls.Add(method);
            methodPanel.Resize += (s, e) => method.Location = new Point((methodPanel.ClientSize.Width - method.Width) / 2, 8);
            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12)
            };
            var confirm = PrimaryBtn("Xác nhận thanh toán", 0, 0, 160, 34);
            var cancel = new Button { Text = "Đóng", Width = 90, Height = 34, DialogResult = DialogResult.Cancel };
            confirm.Click += (s, e) =>
            {
                try
                {
                    if (!ClinicDAL.ThanhToanHoaDon(maHD, method.SelectedItem?.ToString() ?? "Tiền mặt"))
                    {
                        MessageBox.Show("Hóa đơn đã được cập nhật hoặc không còn ở trạng thái chờ thanh toán.",
                            "Không thể thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            actions.Controls.AddRange(new Control[] { confirm, cancel });
            dialog.Controls.Add(actions);
            dialog.Controls.Add(methodPanel);
            dialog.Controls.Add(label);
            dialog.Controls.Add(amount);
            dialog.AcceptButton = confirm;
            dialog.CancelButton = cancel;
            return dialog.ShowDialog(this) == DialogResult.OK;
        }

        private void CreateInvoice(DataRow ticket)
        {
            string maPK = ticket["MaPK"].ToString() ?? "";
            try
            {
                var (tienDV, tienThuoc) = ClinicDAL.TinhChiPhiPhieuKham(maPK);
                using var dialog = new Form
                {
                    Text = "Lập hóa đơn",
                    Size = new Size(460, 300),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.White
                };
                var heading = new Label
                {
                    Text = $"Lập hóa đơn cho {ticket["TenBN"]}  •  {maPK}",
                    Dock = DockStyle.Top,
                    Height = 52,
                    Padding = new Padding(14, 12, 0, 0),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                var amount = new Label
                {
                    Text = $"Dịch vụ: {tienDV:N0} đ     Thuốc: {tienThuoc:N0} đ\nTỔNG CỘNG: {tienDV + tienThuoc:N0} đ",
                    Dock = DockStyle.Top,
                    Height = 75,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = C_Blue
                };
                var method = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Width = 230,
                    Font = F_Base,
                    Anchor = AnchorStyles.None
                };
                method.Items.AddRange(new object[] { "Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "BHYT" });
                method.SelectedIndex = 0;
                var methodPanel = new Panel { Dock = DockStyle.Top, Height = 48 };
                methodPanel.Controls.Add(method);
                methodPanel.Resize += (s, e) => method.Location = new Point((methodPanel.ClientSize.Width - method.Width) / 2, 8);
                var actions = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 55,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(12)
                };
                var create = PrimaryBtn("Tạo hóa đơn", 0, 0, 130, 34);
                var cancel = new Button { Text = "Hủy", Width = 85, Height = 34, DialogResult = DialogResult.Cancel };
                create.Click += (s, e) =>
                {
                    try
                    {
                        var invoice = new HoaDon
                        {
                            MaHD = ClinicDAL.SinhMaHoaDon(),
                            MaPK = maPK,
                            MaNV_ThuNgan = SessionState.CurrentUser?.MaNV ?? "",
                            TienDichVu = tienDV,
                            TienThuoc = tienThuoc,
                            GiamGia = 0,
                            TongTien = tienDV + tienThuoc,
                            HinhThucTT = method.SelectedItem?.ToString() ?? "Tiền mặt",
                            TrangThai = "Chưa thanh toán"
                        };
                        if (!ClinicDAL.TaoHoaDon(invoice))
                            throw new InvalidOperationException("Không thể tạo hóa đơn cho phiếu khám này.");
                        dialog.DialogResult = DialogResult.OK;
                        dialog.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Lỗi lập hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                actions.Controls.AddRange(new Control[] { create, cancel });
                dialog.Controls.Add(actions);
                dialog.Controls.Add(methodPanel);
                dialog.Controls.Add(amount);
                dialog.Controls.Add(heading);
                dialog.AcceptButton = create;
                dialog.CancelButton = cancel;
                dialog.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể tính viện phí", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowThuNganViewLegacy()
        {
            SetRefreshPageAction(ShowThuNganView);
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
            var btnCancelInvoice = SecondaryBtn("Hủy HĐ chưa thanh toán", 110, 6, 190, 28, C_Red);
            void LoadInvoices() => dgvHD.DataSource = ClinicDAL.LayDanhSachHoaDon();
            btnRefreshHD.Click += (s, e) => LoadInvoices();
            btnCancelInvoice.Click += (s, e) =>
            {
                if (dgvHD.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Chọn hóa đơn cần hủy.", "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataGridViewRow selected = dgvHD.SelectedRows[0];
                string maHD = selected.Cells["MaHD"].Value?.ToString() ?? "";
                string trangThai = selected.Cells["TrangThai"].Value?.ToString() ?? "";
                if (trangThai != "Chưa thanh toán")
                {
                    MessageBox.Show("Chỉ hủy được hóa đơn chưa thanh toán. Hóa đơn đã thanh toán cần được bảo toàn để đối soát.",
                        "Không thể hủy hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (MessageBox.Show($"Hủy hóa đơn {maHD} chưa thanh toán?", "Xác nhận hủy hóa đơn",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                if (!ClinicDAL.HuyHoaDonChuaThanhToan(maHD))
                {
                    MessageBox.Show("Không thể hủy hóa đơn; trạng thái có thể đã thay đổi.", "Lỗi hủy hóa đơn",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                LoadInvoices();
            };
            LoadInvoices();
            var pLSTop = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.White };
            pLSTop.Controls.AddRange(new Control[] { btnRefreshHD, btnCancelInvoice });
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
