using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using QlyPhongKham.DAL;
using QlyPhongKham.Models;
using QlyPhongKham.Helpers;

namespace QlyPhongKham.Forms
{
    public partial class FormMain
    {
        private void ShowLeTanDashboard()
        {
            SetRefreshPageAction(ShowLeTanDashboard);
            SetActiveNav(btnNavTongQuan);
            SetPageHeader("Y tá  ›  Trang chủ", "Tổng quan tiếp đón & điều phối");
            panelContent.Controls.Clear();

            try
            {
                string maNV = SessionState.CurrentUser?.MaNV ?? "";
                DataTable queue = ClinicDAL.LayHangChoKhamTrongNgay(maLeTan: maNV);
                DataTable appointments = ClinicDAL.LayDanhSachLichHen();
                var allowedDoctors = ClinicDAL.LayDanhSachBacSiTheoNhanVien(maNV).AsEnumerable()
                    .Select(row => row["MaNV"].ToString() ?? "")
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                DateTime today = DateTime.Today;
                int todayAppointments = appointments.AsEnumerable()
                    .Count(row => row.Field<DateTime>("NgayHen").Date == today
                        && (allowedDoctors.Count == 0 || allowedDoctors.Contains(row["MaNV_BacSi"].ToString() ?? "")));
                int todayNoShows = appointments.AsEnumerable()
                    .Count(row => row.Field<DateTime>("NgayHen").Date == today
                        && row["TrangThai"].ToString() == "Không đến"
                        && (allowedDoctors.Count == 0 || allowedDoctors.Contains(row["MaNV_BacSi"].ToString() ?? "")));
                int waitingApproval = appointments.AsEnumerable()
                    .Count(row => row["TrangThai"].ToString() == "Chờ xác nhận"
                        && (allowedDoctors.Count == 0 || allowedDoctors.Contains(row["MaNV_BacSi"].ToString() ?? "")));

                var wrapper = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
                var roomInfo = ClinicDAL.LayThongTinPhongNhanVien(maNV);
                string roomText = !string.IsNullOrEmpty(roomInfo.maPhong)
                    ? $"📍 Đang trực tại: {roomInfo.tenPhong} ({roomInfo.tenKhoa})  •  Tổ phụ trách: {roomInfo.soBacSi} Bác sĩ & {roomInfo.soYTa} Y tá / Điều dưỡng"
                    : "📍 Quầy tiếp đón trung tâm  •  Phụ trách toàn bộ phòng khám";
                var title = MakeLbl("TIẾP NHẬN BỆNH NHÂN", 12, 6, bold: true);
                title.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
                var lblRoomBadge = new Label
                {
                    Text = roomText,
                    Location = new Point(230, 10),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = C_TealDark
                };
                var btnReceive = PrimaryBtn("＋ Tiếp nhận", 12, 40, 135, 36);
                var btnBook = SecondaryBtn("＋ Thêm lịch hẹn", 157, 40, 145, 36, C_Teal);
                btnReceive.Click += (s, e) => ShowTiepDonView();
                btnBook.Click += (s, e) => OpenReceptionAppointment();

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
                cards.Controls.Add(MakeLeTanKpi("HÀNG CHỜ KHÁM", queue.Rows.Count.ToString(), "Phiếu trong phạm vi phòng được giao", C_Teal, () => ShowTiepDonView()), 0, 0);
                cards.Controls.Add(MakeLeTanKpi("LỊCH HẸN HÔM NAY", todayAppointments.ToString(), "Lịch hẹn trong ngày hôm nay", C_Blue, () => ShowLeTanLichKhamView(selectedDate: today)), 1, 0);
                cards.Controls.Add(MakeLeTanKpi("CHỜ DUYỆT", waitingApproval.ToString(), "Lịch đặt chờ duyệt toàn viện", C_Orange, () => ShowLeTanLichKhamView(status: "Chờ xác nhận")), 2, 0);
                cards.Controls.Add(MakeLeTanKpi("KHÔNG ĐẾN", todayNoShows.ToString(), "Lịch vắng trong ngày", C_Purple, () => ShowLeTanLichKhamView(status: "Không đến", selectedDate: today)), 3, 0);

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
                queueGrid.DataBindingComplete += (s, e) =>
                {
                    SafeFormatCol(queueGrid, "STT", "STT", 55, fillWeight: 45f);
                    SafeFormatCol(queueGrid, "TenBN", "Họ tên bệnh nhân", 160, fillWeight: 150f);
                    SafeFormatCol(queueGrid, "MaBN", "Mã BN", 85, fillWeight: 75f);
                    SafeFormatCol(queueGrid, "NgayKham", "Giờ khám", 95, fillWeight: 80f);
                    SafeFormatCol(queueGrid, "TrangThai", "Trạng thái", 105, fillWeight: 95f);
                    SafeFormatCol(queueGrid, "TrieuChung", "Triệu chứng", 160, fillWeight: 145f);
                    SafeFormatCol(queueGrid, "TenBacSi", "Bác sĩ", 135, fillWeight: 120f);
                    SafeFormatCol(queueGrid, "TenPhong", "Phòng khám", 135, fillWeight: 120f);

                    foreach (string column in new[]
                    {
                        "MaPK", "GioiTinh", "Tuoi", "MaNV_BacSi", "MaPhong", "TenKhoa"
                    })
                    {
                        SafeFormatCol(queueGrid, column, column, null, visible: false);
                    }

                    string[] visibleColumnOrder =
                    {
                        "STT", "TenBN", "MaBN", "NgayKham", "TrangThai", "TrieuChung", "TenBacSi", "TenPhong"
                    };
                    for (int index = 0; index < visibleColumnOrder.Length; index++)
                    {
                        if (queueGrid.Columns.Contains(visibleColumnOrder[index]))
                            queueGrid.Columns[visibleColumnOrder[index]]!.DisplayIndex = index;
                    }
                    if (queueGrid.Columns.Contains("NgayKham"))
                        queueGrid.Columns["NgayKham"]!.DefaultCellStyle.Format = "dd/MM HH:mm";
                };
                queueGrid.Location = new Point(14, 48);
                queueGrid.Size = new Size(queueCard.ClientSize.Width - 28, queueCard.ClientSize.Height - 62);
                queueGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                queueGrid.DataSource = queue;
                queueCard.Controls.Add(queueGrid);

                wrapper.Controls.AddRange(new Control[] { title, lblRoomBadge, btnReceive, btnBook, cards, queueCard });
                panelContent.Controls.Add(wrapper);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không tải được tổng quan lễ tân",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel MakeLeTanKpi(string title, string value, string subtitle, Color accent, Action? onClick = null)
        {
            var card = new Panel { Dock = DockStyle.Fill, Margin = new Padding(5), BackColor = Color.White };
            card.Paint += PaintCardBorder;
            card.Controls.Add(new Panel { Dock = DockStyle.Left, Width = 4, BackColor = accent });
            var lblTitle = new Label
            {
                Text = title,
                Location = new Point(16, 12),
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = C_TextGray
            };
            var lblValue = new Label
            {
                Text = value,
                Location = new Point(16, 34),
                AutoSize = true,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = accent
            };
            var lblSub = new Label
            {
                Text = subtitle,
                Location = new Point(16, 74),
                AutoSize = true,
                Font = F_Small,
                ForeColor = C_TextGray
            };
            card.Controls.AddRange(new Control[] { lblTitle, lblValue, lblSub });

            if (onClick != null)
            {
                card.Cursor = Cursors.Hand;
                card.Click += (s, e) => onClick();
                lblTitle.Cursor = Cursors.Hand;
                lblTitle.Click += (s, e) => onClick();
                lblValue.Cursor = Cursors.Hand;
                lblValue.Click += (s, e) => onClick();
                lblSub.Cursor = Cursors.Hand;
                lblSub.Click += (s, e) => onClick();
            }

            return card;
        }

        private void ShowLeTanLichKhamView(string keyword = "", string? status = null, DateTime? selectedDate = null)
        {
            SetRefreshPageAction(() => ShowLeTanLichKhamView(keyword, status, selectedDate));
            SetActiveNav(btnNavKhamBenh);
            string maNVCurrent = SessionState.CurrentUser?.MaNV ?? "";
            var roomInfo = ClinicDAL.LayThongTinPhongNhanVien(maNVCurrent);
            string headerSubtitle = !string.IsNullOrEmpty(roomInfo.maPhong)
                ? $"Lịch khám & yêu cầu hẹn: {roomInfo.tenPhong} ({roomInfo.tenKhoa})  •  Tổ: {roomInfo.soBacSi} Bác sĩ & {roomInfo.soYTa} Y tá"
                : "Quản lý lịch khám và yêu cầu đặt lịch toàn viện";
            SetPageHeader("Y tá  ›  Lịch khám", headerSubtitle);
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 112, BackColor = Color.White, Padding = new Padding(12) };
            toolbar.Paint += PaintCardBorder;
            var datePicker = new DateTimePicker
            {
                Location = new Point(12, 10),
                Size = new Size(130, 30),
                Format = DateTimePickerFormat.Short,
                Value = selectedDate ?? DateTime.Today
            };
            bool isAllDatesInit = (selectedDate == null && status == "Chờ xác nhận");
            var chkAllDates = new CheckBox
            {
                Text = "Tất cả ngày",
                Location = new Point(148, 14),
                AutoSize = true,
                Font = F_Base,
                Cursor = Cursors.Hand,
                Checked = isAllDatesInit
            };
            datePicker.Enabled = !isAllDatesInit;

            var cboDoctorFilter = new ComboBox
            {
                Location = new Point(244, 10),
                Size = new Size(210, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            cboDoctorFilter.Items.Add("-- Tất cả Bác sĩ phòng này --");
            var dtDocs = ClinicDAL.LayDanhSachBacSiTheoNhanVien(maNVCurrent);
            var docIds = new List<string> { "" };
            foreach (DataRow r in dtDocs.Rows)
            {
                cboDoctorFilter.Items.Add($"BS. {r["HoTen"]}");
                docIds.Add(r["MaNV"].ToString() ?? "");
            }
            var pendingCount = MakeLbl("Chờ duyệt: 0", 540, 57, bold: true);
            pendingCount.ForeColor = C_Orange;
            GridPageController? appointmentPageController = null;
            void LoadAppointments()
            {
                ClinicDAL.DanhDauLichHenKhongDenQuaHan(
                    SessionState.CurrentUser?.MaNV ?? "", 60);
                appointmentPageController?.Reload();
                DateTime? filterDate = chkAllDates.Checked ? null : datePicker.Value.Date;
                string? filterBS = (cboDoctorFilter.SelectedIndex > 0 && cboDoctorFilter.SelectedIndex < docIds.Count)
                    ? docIds[cboDoctorFilter.SelectedIndex]
                    : null;
                int waitingCount = ClinicDAL.LaySoLichHenChoDuyetLeTan(
                    SessionState.CurrentUser?.MaNV ?? "", filterDate, filterBS);
                int totalWaiting = ClinicDAL.LaySoLichHenChoDuyetLeTan(
                    SessionState.CurrentUser?.MaNV ?? "", null, filterBS);
                pendingCount.Text = chkAllDates.Checked
                    ? $"Chờ duyệt: {totalWaiting}"
                    : $"Chờ duyệt: {waitingCount} (Tổng chờ: {totalWaiting})";
            }

            cboDoctorFilter.SelectedIndexChanged += (s, e) => LoadAppointments();

            var search = MakeInput(12, 53, 220);
            search.PlaceholderText = "Mã lịch, bệnh nhân, bác sĩ...";
            var statusFilter = new ComboBox
            {
                Location = new Point(244, 53),
                Size = new Size(165, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            statusFilter.Items.AddRange(new object[] { "Tất cả trạng thái", "Chờ xác nhận", "Đã xác nhận", "Đã đến", "Đã hủy", "Bị từ chối", "Không đến" });
            statusFilter.SelectedItem = status ?? "Tất cả trạng thái";
            if (statusFilter.SelectedIndex < 0)
                statusFilter.SelectedIndex = 0;
            var refresh = SecondaryBtn("⌕ Tìm kiếm", 420, 52, 110, 32, C_Blue);
            var approve = PrimaryBtn("✓ Duyệt lịch", 0, 52, 115, 32);
            approve.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var checkIn = SecondaryBtn("✓ Check-in", 0, 52, 105, 32, C_Green);
            checkIn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var refuse = SecondaryBtn("Từ chối", 0, 52, 90, 32, C_Red);
            refuse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var noShow = SecondaryBtn("Không đến", 0, 52, 95, 32, C_Orange);
            noShow.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var add = PrimaryBtn("＋ Đặt lịch", 0, 10, 125, 34);
            add.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var edit = SecondaryBtn("✎ Sửa lịch", 0, 10, 115, 34, C_Orange);
            edit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var cancel = SecondaryBtn("Hủy lịch", 0, 10, 105, 34, C_Red);
            cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            toolbar.Controls.AddRange(new Control[] { datePicker, chkAllDates, cboDoctorFilter, search, statusFilter, refresh, approve, checkIn, refuse, noShow, pendingCount, add, edit, cancel });

            var gridPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            gridPanel.Paint += PaintCardBorder;
            var grid = MakeGrid();
            grid.DataBindingComplete += (sender, args) =>
            {
                if (grid.Columns.Contains("TrangThai"))
                    grid.Columns["TrangThai"]!.Visible = false;
                if (grid.Columns.Contains("TrangThaiDuyet"))
                    grid.Columns["TrangThaiDuyet"]!.HeaderText = "Trạng thái";
                if (grid.Columns.Contains("SoLanVangMat"))
                    grid.Columns["SoLanVangMat"]!.HeaderText = "Số lần vắng";
            };
            gridPanel.Controls.Add(grid);

            DataRow? SelectedAppointment()
                => grid.SelectedRows.Count > 0 && grid.SelectedRows[0].DataBoundItem is DataRowView view
                    ? view.Row
                    : null;

            refresh.Click += (s, e) => LoadAppointments();
            chkAllDates.CheckedChanged += (s, e) =>
            {
                datePicker.Enabled = !chkAllDates.Checked;
                LoadAppointments();
            };
            search.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                    LoadAppointments();
            };
            datePicker.ValueChanged += (s, e) => LoadAppointments();
            statusFilter.SelectedIndexChanged += (s, e) =>
            {
                // Nếu chọn "Chờ xác nhận" thì tự động bật "Tất cả ngày" để dễ tìm lịch mới đặt
                if (statusFilter.SelectedItem?.ToString() == "Chờ xác nhận" && !chkAllDates.Checked)
                {
                    chkAllDates.Checked = true;
                    return;
                }
                LoadAppointments();
            };
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

                // ── Gửi email xác nhận + thông báo nội bộ ──────────
                try
                {
                    int maLH = Convert.ToInt32(selected["MaLichHen"]);
                    var info = ClinicDAL.LayThongTinLichHenDeDuyet(maLH);

                    // Email cho bệnh nhân (bất đồng bộ, không block UI)
                    if (!string.IsNullOrWhiteSpace(info.email))
                    {
                        string emailSubject = "[An Tâm] Lịch hẹn của bạn đã được xác nhận";
                        string emailBody    = Helpers.EmailHelper.BuildXacNhanLichHenBody(
                                                 info.tenBN, info.tenBacSi, info.ngayHen, info.lyDoKham);
                        Helpers.EmailHelper.SendAsync(info.email, emailSubject, emailBody);
                    }

                    // Thông báo bell cho nhân viên hiện tại & admin
                    Helpers.NotificationHelper.Push(
                        SessionState.CurrentUser?.MaNV ?? "",
                        $"Đã duyệt lịch hẹn #{maLH}",
                        $"BN: {info.tenBN} | Ngày: {info.ngayHen:dd/MM/yyyy HH:mm} | BS: {info.tenBacSi}",
                        "Lịch hẹn");
                    Helpers.NotificationHelper.PushToAdmins(
                        $"Lịch hẹn #{maLH} đã xác nhận",
                        $"Lễ tân {SessionState.CurrentUser?.HoTenNhanVien} duyệt lịch của {info.tenBN}",
                        "Lịch hẹn");
                }
                catch { /* email/notification không được phá workflow chính */ }

                MessageBox.Show($"Đã duyệt lịch của {selected["TenBN"]}. Lịch đã xác nhận sẽ hiển thị trong danh sách của bác sĩ.",
                    "Đã duyệt lịch", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadAppointments();
            };
            checkIn.Click += (s, e) =>
            {
                DataRow? selected = SelectedAppointment();
                if (selected == null)
                {
                    MessageBox.Show("Chọn lịch hẹn cần check-in.", "Chưa chọn lịch",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string appointmentStatus = selected["TrangThai"].ToString() ?? "";
                if (appointmentStatus != "Đã xác nhận")
                {
                    string message = appointmentStatus == "Không đến"
                        ? $"Lịch hẹn đã quá thời hạn check-in ({ClinicDAL.CheckInLateWindowMinutes} phút sau giờ hẹn). Vui lòng liên hệ quản lý để xử lý ngoại lệ."
                        : $"Lịch hẹn đang ở trạng thái '{appointmentStatus}', chỉ lịch đã xác nhận mới được check-in.";
                    MessageBox.Show(message, "Không thể check-in",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    using var vitalsDialog = new FormSinhHieu();
                    if (vitalsDialog.ShowDialog(this) != DialogResult.OK)
                        return;
                    var ticket = ClinicDAL.CheckInLichHen(
                        Convert.ToInt32(selected["MaLichHen"]),
                        SessionState.CurrentUser?.MaNV ?? "",
                        vitalsDialog.DuLieuSinhHieu);
                    MessageBox.Show($"Đã check-in và tạo phiếu khám {ticket.maPK}, số thứ tự #{ticket.stt}.",
                        "Check-in thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadAppointments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể check-in", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            };
            void EndAppointment(string targetStatus)
            {
                DataRow? selected = SelectedAppointment();
                if (selected == null)
                {
                    MessageBox.Show("Chọn lịch hẹn cần cập nhật.", "Chưa chọn lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string reason = Microsoft.VisualBasic.Interaction.InputBox("Nhập lý do xử lý lịch hẹn:", targetStatus, "").Trim();
                if (string.IsNullOrWhiteSpace(reason)) return;
                try
                {
                    if (!ClinicDAL.KetThucLichHen(Convert.ToInt32(selected["MaLichHen"]), targetStatus, reason))
                        throw new InvalidOperationException(targetStatus == "Không đến"
                            ? "Chỉ đánh dấu không đến sau giờ hẹn."
                            : "Lịch đã được xử lý hoặc không còn ở trạng thái phù hợp.");
                    LoadAppointments();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể cập nhật lịch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            refuse.Click += (s, e) => EndAppointment("Bị từ chối");
            noShow.Click += (s, e) => EndAppointment("Không đến");
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
                checkIn.Location = new Point(approve.Left - checkIn.Width - 8, 52);
                noShow.Location = new Point(checkIn.Left - noShow.Width - 8, 52);
                refuse.Location = new Point(noShow.Left - refuse.Width - 8, 52);
                pendingCount.Location = new Point(refuse.Left - pendingCount.Width - 12, 57);
            };
            panelContent.Controls.Add(gridPanel);
            panelContent.Controls.Add(toolbar);
            appointmentPageController = GridPageController.Attach(grid, (offset, pageSize) =>
            {
                string? filterBS = (cboDoctorFilter.SelectedIndex > 0 && cboDoctorFilter.SelectedIndex < docIds.Count)
                    ? docIds[cboDoctorFilter.SelectedIndex]
                    : null;
                return ClinicDAL.LayDanhSachLichHenLeTan(
                    SessionState.CurrentUser?.MaNV ?? "",
                    chkAllDates.Checked ? null : datePicker.Value.Date,
                    search.Text.Trim(),
                    statusFilter.SelectedItem?.ToString(),
                    offset,
                    pageSize,
                    filterBS);
            });
            string? initBS = (cboDoctorFilter.SelectedIndex > 0 && cboDoctorFilter.SelectedIndex < docIds.Count)
                ? docIds[cboDoctorFilter.SelectedIndex]
                : null;
            int waitingAppointments = ClinicDAL.LaySoLichHenChoDuyetLeTan(
                SessionState.CurrentUser?.MaNV ?? "",
                chkAllDates.Checked ? null : datePicker.Value.Date,
                initBS);
            int totalWaitingInit = ClinicDAL.LaySoLichHenChoDuyetLeTan(
                SessionState.CurrentUser?.MaNV ?? "", null, initBS);
            pendingCount.Text = chkAllDates.Checked
                ? $"Chờ duyệt: {totalWaitingInit}"
                : $"Chờ duyệt: {waitingAppointments} (Tổng chờ: {totalWaitingInit})";
            StartNoShowTimer(LoadAppointments);
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
            SetPageHeader("Y tá  ›  Tiếp đón", "Đăng ký khám bệnh, cấp số thứ tự & theo dõi hàng chờ");
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
            var doctorAssignmentHint = new Label
            {
                Location = new Point(0, 52),
                Size = new Size(300, 38),
                ForeColor = Color.FromArgb(71, 85, 105),
                Font = new Font("Segoe UI", 7.5f),
                AutoEllipsis = true
            };
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
                if (doctors.Rows.Count == 0)
                {
                    cboBacSi.DataSource = null;
                    cboBacSi.Items.Clear();
                    cboBacSi.Items.Add("Chưa có bác sĩ phân công phòng này");
                    cboBacSi.SelectedIndex = 0;
                }
                cboBacSi.Enabled = doctors.Rows.Count > 0;
                UpdateDoctorSchedule();
            }
            void UpdateDoctorSchedule()
            {
                if (cboBacSi.SelectedValue != null && cboBacSi.DataSource != null)
                {
                    doctorAssignmentHint.Text = ClinicDAL.MoTaLichTrucBacSi(cboBacSi.SelectedValue.ToString() ?? "");
                    doctorAssignmentHint.ForeColor = Color.FromArgb(71, 85, 105);
                }
                else
                {
                    doctorAssignmentHint.Text = "Chưa có bác sĩ phân công phòng này. Nhờ Admin phân công nhân sự.";
                    doctorAssignmentHint.ForeColor = Color.Firebrick;
                }
            }
            cboBacSi.SelectedValueChanged += (s, e) => UpdateDoctorSchedule();
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
            btnCap.Enabled = dtPhong.Rows.Count > 0 && cboBacSi.DataSource != null;
            cboPhong.SelectedValueChanged += (s, e) =>
            {
                LoadDoctorsForRoom();
                btnCap.Enabled = cboBacSi.DataSource != null && cboBacSi.SelectedValue != null;
            };
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
            var doctorField = CreateField("BÁC SĨ KHÁM *", cboBacSi);
            doctorField.Controls.Add(doctorAssignmentHint);
            fieldsGrid.Controls.Add(doctorField, 2, 1);
            fieldsGrid.Controls.Add(CreateField("ĐỊA CHỈ", txtDC), 3, 1);
            fieldsGrid.Controls.Add(CreateField("TIỀN SỬ BỆNH / DỊ ỨNG", txtTS), 0, 2);
            fieldsGrid.Controls.Add(CreateField("TRIỆU CHỨNG", txtTC), 1, 2);

            var dgvHangCho = MakeGrid();
            dgvHangCho.DataBindingComplete += (s, e) =>
            {
                SafeFormatCol(dgvHangCho, "STT", "STT", 55, fillWeight: 45f);
                SafeFormatCol(dgvHangCho, "TenBN", "Họ tên bệnh nhân", 160, fillWeight: 150f);
                SafeFormatCol(dgvHangCho, "MaBN", "Mã BN", 85, fillWeight: 75f);
                SafeFormatCol(dgvHangCho, "NgayKham", "Giờ khám", 85, fillWeight: 75f);
                SafeFormatCol(dgvHangCho, "TrieuChung", "Triệu chứng", 180, fillWeight: 170f);
                SafeFormatCol(dgvHangCho, "TenBacSi", "Bác sĩ", 140, fillWeight: 125f);
                SafeFormatCol(dgvHangCho, "TenPhong", "Phòng khám", 140, fillWeight: 120f);
                SafeFormatCol(dgvHangCho, "TrangThai", "Trạng thái", 110, fillWeight: 100f);

                foreach (string column in new[]
                {
                    "MaPK", "GioiTinh", "Tuoi", "MaNV_BacSi", "MaPhong", "TenKhoa"
                })
                {
                    SafeFormatCol(dgvHangCho, column, column, null, visible: false);
                }

                string[] visibleColumnOrder =
                {
                    "STT", "TenBN", "MaBN", "NgayKham", "TrieuChung", "TenBacSi", "TenPhong", "TrangThai"
                };
                for (int index = 0; index < visibleColumnOrder.Length; index++)
                {
                    if (dgvHangCho.Columns.Contains(visibleColumnOrder[index]))
                        dgvHangCho.Columns[visibleColumnOrder[index]]!.DisplayIndex = index;
                }
                if (dgvHangCho.Columns.Contains("NgayKham"))
                    dgvHangCho.Columns["NgayKham"]!.DefaultCellStyle.Format = "HH:mm";
            };
            GridPageController? queuePageController = null;
            void LoadHangCho() => queuePageController?.Reload();

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
                using var vitalsDialog = new FormSinhHieu();
                if (vitalsDialog.ShowDialog(this) != DialogResult.OK)
                    return;

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
                var pk = new PhieuKham
                {
                    MaPK        = "",
                    STT         = 0,
                    MaBN        = bn.MaBN,
                    MaNV_BacSi  = cboBacSi.SelectedValue?.ToString() ?? "",
                    MaPhong     = maPhong,
                    TrieuChung  = txtTC.Text.Trim()
                };
                if (!ClinicDAL.TaoPhieuKham(pk, maLeTan, vitalsDialog.DuLieuSinhHieu))
                {
                    MessageBox.Show("Không tạo được phiếu khám. Kiểm tra phân công bác sĩ/phòng và thử lại.", "Lỗi cấp phiếu", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                MessageBox.Show($"✅ Cấp phiếu thành công!\n\n📄 Mã phiếu: {pk.MaPK}\n🎫 Số thứ tự: #{pk.STT}\n👤 Bệnh nhân: {bn.HoTen}",
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
            var btnOpenQueueDisplay = SecondaryBtn("📺 Màn hình gọi số", 590, 4, 170, 30, C_Blue);
            btnOpenQueueDisplay.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenQueueDisplay.Click += (s, e) =>
            {
                using var queueDisplay = new FormManHinhCho();
                queueDisplay.ShowDialog(this);
            };
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
            pnlGridCho.Controls.Add(dgvHangCho);
            grpCho.Controls.Add(pnlGridCho);
            grpCho.Controls.Add(btnOpenQueueDisplay);
            grpCho.Controls.Add(btnCancelTicket);
            queuePageController = GridPageController.Attach(dgvHangCho, (offset, pageSize) =>
                ClinicDAL.LayHangChoKhamTrongNgay(
                    maPhong: cboPhong.SelectedValue?.ToString(),
                    maLeTan: maLeTan,
                    offset: offset,
                    pageSize: pageSize));

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
            string roleLabel = SessionState.IsAdmin ? "Quản trị viên" : "Y tá";
            SetPageHeader($"{roleLabel}  ›  Bệnh nhân", "Tra cứu, chỉnh sửa hồ sơ & xem lịch sử khám");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;

            var txtSearch  = MakeInput(90, 8, 180);
            var btnSearch  = SecondaryBtn("🔍 Tìm", 280, 8, 65, 34, C_Blue);
            var btnAdd     = PrimaryBtn("➕ Thêm BN mới", 355, 8, 130, 34);
            var btnEdit    = SecondaryBtn("✏ Sửa", 495, 8, 75, 34, C_Orange);
            var btnDelete  = SecondaryBtn("🗑 Xóa", 580, 8, 75, 34, C_Red);
            var btnHistory = SecondaryBtn("📋 Lịch sử khám", 665, 8, 125, 34, C_Purple);
            var btnExportBN = SecondaryBtn("📥 Xuất Excel", 800, 8, 110, 34, C_Green);
            var btnImportBN = SecondaryBtn("📤 Nhập Excel", 920, 8, 115, 34, C_Teal);

            toolPanel.Controls.AddRange(new Control[] {
                MakeLbl("Tìm kiếm:", 14, 15),
                txtSearch, btnSearch, btnAdd, btnEdit, btnDelete, btnHistory, btnExportBN, btnImportBN
            });

            var contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            contentPanel.Paint += PaintCardBorder;
            var dgvBN = MakeGrid();
            contentPanel.Controls.Add(dgvBN);

            GridPageController? patientPageController = null;
            void LoadBN() => patientPageController?.Reload();
            patientPageController = GridPageController.Attach(dgvBN, (offset, pageSize) =>
                ClinicDAL.LayDanhSachBenhNhan(txtSearch.Text.Trim(), offset, pageSize));

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
                    AnhDaiDien  = ClinicDAL.LayAnhBenhNhan(row.Cells["MaBN"].Value?.ToString() ?? ""),
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

            btnExportBN.Click += (s, e) =>
            {
                var dt = ClinicDAL.LayDanhSachBenhNhan(txtSearch.Text.Trim());
                ExcelHelper.ExportToExcel(dt, "DanhSachBenhNhan", "DANH SÁCH BỆNH NHÂN", $"DanhSachBenhNhan_{DateTime.Now:yyyyMMdd}.xlsx");
            };

            btnImportBN.Click += (s, e) =>
            {
                var dtExcel = ExcelHelper.ImportFromExcel("Chọn file Excel danh sách bệnh nhân");
                if (dtExcel == null || dtExcel.Rows.Count == 0) return;

                int success = 0;
                int failed = 0;
                foreach (DataRow r in dtExcel.Rows)
                {
                    try
                    {
                        string hoTen = r.Table.Columns.Contains("HoTen") ? r["HoTen"]?.ToString() ?? "" : r[0]?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(hoTen)) continue;

                        string gioiTinh = r.Table.Columns.Contains("GioiTinh") ? r["GioiTinh"]?.ToString() ?? "Nam" : "Nam";
                        string sdt = r.Table.Columns.Contains("SoDienThoai") ? r["SoDienThoai"]?.ToString() ?? "" : "";
                        string diaChi = r.Table.Columns.Contains("DiaChi") ? r["DiaChi"]?.ToString() ?? "" : "";
                        string tienSu = r.Table.Columns.Contains("TienSuBenh") ? r["TienSuBenh"]?.ToString() ?? "" : "";
                        DateTime? ns = null;
                        if (r.Table.Columns.Contains("NgaySinh") && DateTime.TryParse(r["NgaySinh"]?.ToString(), out var parsedNs))
                            ns = parsedNs;

                        var bn = new BenhNhan
                        {
                            MaBN = ClinicDAL.SinhMaBenhNhan(),
                            HoTen = hoTen.Trim(),
                            GioiTinh = gioiTinh.Trim(),
                            NgaySinh = ns,
                            SoDienThoai = string.IsNullOrWhiteSpace(sdt) ? null : sdt.Trim(),
                            DiaChi = string.IsNullOrWhiteSpace(diaChi) ? null : diaChi.Trim(),
                            TienSuBenh = string.IsNullOrWhiteSpace(tienSu) ? null : tienSu.Trim()
                        };

                        if (ClinicDAL.LuuBenhNhan(bn, isNew: true))
                            success++;
                        else
                            failed++;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                MessageBox.Show($"Hoàn tất nhập dữ liệu từ Excel!\n- Thành công: {success} bệnh nhân\n- Lỗi/Bỏ qua: {failed}",
                    "Kết quả nhập Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadBN();
            };

            wrapper.Controls.Add(contentPanel);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════════
        //  REUSABLE COMPONENT: DONUT CHART (GDI+)
        // ═══════════════════════════════════════════════════════════
        private static Panel CreateDonutChart(
            string title,
            Func<IEnumerable<(string Label, int Value, Color Color)>> getItems,
            Func<(string centerTop, string centerSub)> getCenterText)
        {
            var pnl = new Panel { Dock = DockStyle.None, BackColor = C_Card, Padding = new Padding(10) };
            pnl.Paint += PaintCardBorder;
            pnl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int w = pnl.ClientSize.Width;
                int h = pnl.ClientSize.Height;
                if (w < 80 || h < 60) return;

                // Title
                using var fontTitle = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var brushTitle = new SolidBrush(Color.FromArgb(75, 85, 99));
                g.DrawString(title, fontTitle, brushTitle, 10, 8);

                int topOffset = 28;
                int availH = h - topOffset - 10;
                int availW = w - 20;
                if (availH <= 10 || availW <= 10) return;

                var itemsList = (getItems?.Invoke() ?? Enumerable.Empty<(string, int, Color)>()).ToList();
                int total = itemsList.Sum(x => Math.Max(0, x.Item2));
                var (centerTop, centerSub) = getCenterText?.Invoke() ?? ("", "");

                // Donut bounds: placed on left
                int diameter = Math.Min(availH - 4, Math.Min((int)(availW * 0.44), 110));
                if (diameter < 40) diameter = Math.Max(20, Math.Min(availH, availW));

                int donutX = 14;
                int donutY = topOffset + (availH - diameter) / 2;
                var donutRect = new Rectangle(donutX, donutY, diameter, diameter);

                if (total <= 0)
                {
                    using var penEmpty = new Pen(Color.FromArgb(229, 231, 235), Math.Max(8, diameter * 0.18f));
                    float offset = penEmpty.Width / 2f;
                    g.DrawEllipse(penEmpty, donutX + offset, donutY + offset, diameter - offset * 2, diameter - offset * 2);

                    using var fontNone = new Font("Segoe UI", 8f, FontStyle.Italic);
                    using var brushNone = new SolidBrush(Color.FromArgb(156, 163, 175));
                    using var sfCenter = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString("Chưa có DL", fontNone, brushNone, donutRect, sfCenter);
                }
                else
                {
                    float startAngle = -90f;
                    foreach (var item in itemsList)
                    {
                        if (item.Value <= 0) continue;
                        float sweepAngle = (float)item.Value / total * 360f;
                        using var brush = new SolidBrush(item.Color);
                        g.FillPie(brush, donutRect, startAngle, sweepAngle);
                        startAngle += sweepAngle;
                    }

                    // Hole cutout
                    int holeInset = (int)(diameter * 0.28f);
                    int holeDiameter = diameter - holeInset * 2;
                    var holeRect = new Rectangle(donutX + holeInset, donutY + holeInset, holeDiameter, holeDiameter);
                    using var brushBg = new SolidBrush(pnl.BackColor);
                    g.FillEllipse(brushBg, holeRect);

                    // Center text
                    if (!string.IsNullOrEmpty(centerTop))
                    {
                        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        if (!string.IsNullOrEmpty(centerSub))
                        {
                            using var fontCenterTop = new Font("Segoe UI", diameter >= 95 ? 11f : 9.5f, FontStyle.Bold);
                            using var brushCenterTop = new SolidBrush(Color.FromArgb(17, 24, 39));
                            using var fontCenterSub = new Font("Segoe UI", 7.5f, FontStyle.Regular);
                            using var brushCenterSub = new SolidBrush(Color.FromArgb(107, 114, 128));

                            var rectTop = new RectangleF(holeRect.X, holeRect.Y + holeRect.Height * 0.14f, holeRect.Width, holeRect.Height * 0.46f);
                            var rectSub = new RectangleF(holeRect.X, holeRect.Y + holeRect.Height * 0.54f, holeRect.Width, holeRect.Height * 0.38f);
                            g.DrawString(centerTop, fontCenterTop, brushCenterTop, rectTop, sf);
                            g.DrawString(centerSub, fontCenterSub, brushCenterSub, rectSub, sf);
                        }
                        else
                        {
                            using var fontCenterTop = new Font("Segoe UI", diameter >= 95 ? 12f : 10f, FontStyle.Bold);
                            using var brushCenterTop = new SolidBrush(Color.FromArgb(17, 24, 39));
                            g.DrawString(centerTop, fontCenterTop, brushCenterTop, holeRect, sf);
                        }
                    }
                }

                // Legend on the right
                int legendX = donutX + diameter + 14;
                int legendW = w - legendX - 10;
                if (legendW > 40 && itemsList.Count > 0)
                {
                    int lineCount = itemsList.Count;
                    int lineH = Math.Min(22, Math.Max(16, availH / lineCount));
                    int curY = topOffset + (availH - lineH * lineCount) / 2;

                    using var fontLeg = new Font("Segoe UI", 8f, FontStyle.Regular);
                    using var fontVal = new Font("Segoe UI", 8f, FontStyle.Bold);
                    using var brushLeg = new SolidBrush(Color.FromArgb(75, 85, 99));

                    foreach (var item in itemsList)
                    {
                        // Dot
                        using var brushDot = new SolidBrush(item.Color);
                        g.FillEllipse(brushDot, legendX, curY + (lineH - 8) / 2, 8, 8);

                        // Label
                        g.DrawString(item.Label, fontLeg, brushLeg, legendX + 12, curY + (lineH - 14) / 2);

                        // Value & %
                        double pct = total > 0 ? (double)item.Value / total * 100 : 0;
                        string valStr = $"{item.Value} ({pct:0}%)";
                        using var brushVal = new SolidBrush(Color.FromArgb(17, 24, 39));
                        using var sfRight = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
                        g.DrawString(valStr, fontVal, brushVal, new RectangleF(legendX, curY, legendW, lineH), sfRight);

                        curY += lineH;
                    }
                }
            };
            return pnl;
        }

        // ═══════════════════════════════════════════════════════════
        //  LỄ TÂN & THU NGÂN: KHO DƯỢC PHẨM
        // ═══════════════════════════════════════════════════════════
        private void ShowKhoThuocView()
        {
            SetRefreshPageAction(ShowKhoThuocView);
            SetActiveNav(btnNavKhoThuoc);
            string roleLabel = SessionState.IsAdmin ? "Quản trị viên" : "Y tá";
            SetPageHeader($"{roleLabel}  ›  Kho thuốc", "Tra cứu danh mục, kiểm tra tồn kho & nhập kho");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill };

            var toolPanel = new Panel { Dock = DockStyle.Top, Height = 82, BackColor = C_Card, Padding = new Padding(12, 8, 12, 0) };
            toolPanel.Paint += PaintCardBorder;

            var txtSearch  = MakeInput(95, 8, 160);
            var btnSearch  = SecondaryBtn("🔍 Tìm", 265, 8, 70, 34, C_Blue);
            var btnAddT    = PrimaryBtn("➕ Thêm thuốc", 345, 8, 115, 34);
            var btnEditT   = SecondaryBtn("✏ Sửa", 470, 8, 70, 34, C_Orange);
            var btnDeleteT = SecondaryBtn("🗑 Xóa", 550, 8, 70, 34, C_Red);
            var btnNhapKho = SecondaryBtn("📦 Nhập kho", 630, 8, 105, 34, C_Green);
            var btnCanhBao = SecondaryBtn("⚠ Sắp hết", 745, 8, 95, 34, C_Orange);
            var btnCapPhat = SecondaryBtn("💊 Cấp đơn", 745, 44, 95, 32, C_Teal);
            var btnDieuChinh = SecondaryBtn("🧮 Kiểm kê", 850, 44, 100, 32, C_Orange);
            var btnExportT = SecondaryBtn("📥 Xuất Excel", 850, 8, 105, 34, C_Teal);
            var btnImportT = SecondaryBtn("📤 Nhập Excel", 965, 8, 105, 34, Color.FromArgb(79, 70, 229));

            toolPanel.Controls.AddRange(new Control[] {
                MakeLbl("Tên thuốc:", 12, 15),
                txtSearch, btnSearch, btnAddT, btnEditT, btnDeleteT, btnNhapKho, btnCanhBao, btnCapPhat, btnDieuChinh, btnExportT, btnImportT
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

            // Right sidebar: Donut chart + mini stats footer + clean alert list
            var pnlRight = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var chartKho = CreateDonutChart(
                "📊 CƠ CẤU TỒN KHO THUỐC",
                () =>
                {
                    var k = ClinicDAL.LayThongKeKhoThuoc();
                    return new (string Label, int Value, Color Color)[]
                    {
                        ("An toàn", k.anToanCount, Color.FromArgb(16, 185, 129)),
                        ("Sắp hết", k.thuocSapHet, Color.FromArgb(245, 158, 11)),
                        ("Hết hàng", k.hetHangCount, Color.FromArgb(239, 68, 68)),
                        ("Cận hạn", k.loCanHan, Color.FromArgb(139, 92, 246))
                    };
                },
                () =>
                {
                    var k = ClinicDAL.LayThongKeKhoThuoc();
                    return ($"{k.tongLoaiThuoc}", "loại");
                });
            chartKho.Dock = DockStyle.Top;
            chartKho.Height = 150;

            var pnlKhoFooter = new Panel { Dock = DockStyle.Top, Height = 34, BackColor = C_Card, Padding = new Padding(10, 6, 10, 6) };
            pnlKhoFooter.Paint += PaintCardBorder;
            pnlKhoFooter.Paint += (s, e) =>
            {
                var k = ClinicDAL.LayThongKeKhoThuoc();
                using var font = new Font("Segoe UI", 8f, FontStyle.Regular);
                using var brush = new SolidBrush(Color.FromArgb(75, 85, 99));
                e.Graphics.DrawString($"📦 Tổng tồn: {k.tongSoLuongTon:N0} đv  •  Trị giá: {k.tongGiaTriKho:N0} đ", font, brush, 10, 9);
            };

            var grpCB = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
            grpCB.Paint += PaintCardBorder;
            grpCB.Controls.Add(MakeLbl("⚠ CẦN BỔ SUNG / CẬN HẠN (≤ 30)", 10, 10, bold: true));
            var pnlCBGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 30, 0, 0) };
            var dgvCB = MakeGrid();
            dgvCB.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
            dgvCB.DataBindingComplete += (s, e) =>
            {
                string[] hideCols = { "DonGiaNhap", "DonGiaBan", "SoLo", "NgayHetHan", "SoNgayConLai" };
                foreach (var col in hideCols)
                    if (dgvCB.Columns.Contains(col)) dgvCB.Columns[col]!.Visible = false;

                if (dgvCB.Columns.Contains("MaThuoc"))
                {
                    dgvCB.Columns["MaThuoc"]!.HeaderText = "Mã";
                    dgvCB.Columns["MaThuoc"]!.FillWeight = 0.8f;
                    dgvCB.Columns["MaThuoc"]!.MinimumWidth = 60;
                }
                if (dgvCB.Columns.Contains("TenThuoc"))
                {
                    dgvCB.Columns["TenThuoc"]!.HeaderText = "Tên thuốc";
                    dgvCB.Columns["TenThuoc"]!.FillWeight = 2.0f;
                    dgvCB.Columns["TenThuoc"]!.MinimumWidth = 100;
                }
                if (dgvCB.Columns.Contains("DonViTinh"))
                {
                    dgvCB.Columns["DonViTinh"]!.HeaderText = "ĐVT";
                    dgvCB.Columns["DonViTinh"]!.FillWeight = 0.7f;
                    dgvCB.Columns["DonViTinh"]!.MinimumWidth = 45;
                }
                if (dgvCB.Columns.Contains("SoLuongTon"))
                {
                    dgvCB.Columns["SoLuongTon"]!.HeaderText = "Tồn";
                    dgvCB.Columns["SoLuongTon"]!.FillWeight = 0.7f;
                    dgvCB.Columns["SoLuongTon"]!.MinimumWidth = 45;
                }
                if (dgvCB.Columns.Contains("CanhBao"))
                {
                    dgvCB.Columns["CanhBao"]!.HeaderText = "Cảnh báo";
                    dgvCB.Columns["CanhBao"]!.FillWeight = 1.1f;
                    dgvCB.Columns["CanhBao"]!.MinimumWidth = 70;
                }
            };
            pnlCBGrid.Controls.Add(dgvCB);
            grpCB.Controls.Add(pnlCBGrid);

            pnlRight.Controls.Add(grpCB);
            pnlRight.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent });
            pnlRight.Controls.Add(pnlKhoFooter);
            pnlRight.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent });
            pnlRight.Controls.Add(chartKho);
            split.Panel2.Controls.Add(pnlRight);

            void RefreshKhoKPI()
            {
                chartKho.Invalidate();
                pnlKhoFooter.Invalidate();
                dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
            }

            GridPageController? medicinePageController = null;
            void LoadThuoc()
            {
                medicinePageController?.Reload();
                RefreshKhoKPI();
            }
            medicinePageController = GridPageController.Attach(dgvT, (offset, pageSize) =>
                ClinicDAL.LayDanhSachThuoc(txtSearch.Text.Trim(), offset, pageSize));
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) LoadThuoc(); };
            btnSearch.Click   += (s, e) => LoadThuoc();
            split.Panel1.Controls.Add(pnlT);

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
                using var dlg = new FormNhapLoThuoc(maThuoc, tenThuoc);
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadThuoc();
                    dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
                }
            };

            btnCapPhat.Click += (s, e) =>
            {
                DataTable pending = ClinicDAL.LayDanhSachDonChoCapPhat();
                if (pending.Rows.Count == 0) { MessageBox.Show("Không có đơn đã thanh toán đang chờ cấp phát."); return; }
                using var dlg = new FormCapPhatThuoc(pending);
                if (dlg.ShowDialog() == DialogResult.OK) { LoadThuoc(); dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30); }
            };

            btnDieuChinh.Click += (s, e) =>
            {
                if (dgvT.SelectedRows.Count == 0) { MessageBox.Show("Chọn thuốc cần kiểm kê."); return; }
                string drug = dgvT.SelectedRows[0].Cells["MaThuoc"].Value?.ToString() ?? "";
                DataTable lots = ClinicDAL.LayDanhSachLoThuoc(drug);
                if (lots.Rows.Count == 0) { MessageBox.Show("Thuốc chưa có lô nhập. Hãy nhập kho theo lô trước."); return; }
                using var dlg = new FormDieuChinhKho(drug, lots);
                if (dlg.ShowDialog() == DialogResult.OK) { LoadThuoc(); dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30); }
            };

            btnCanhBao.Click += (s, e) => dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);

            btnExportT.Click += (s, e) =>
            {
                var dt = ClinicDAL.LayDanhSachThuoc(txtSearch.Text.Trim());
                ExcelHelper.ExportToExcel(dt, "DanhMucThuoc", "DANH MỤC THUỐC VÀ TỒN KHO DƯỢC", $"KhoThuoc_{DateTime.Now:yyyyMMdd}.xlsx");
            };

            btnImportT.Click += (s, e) =>
            {
                var dtExcel = ExcelHelper.ImportFromExcel("Chọn file Excel danh mục thuốc");
                if (dtExcel == null || dtExcel.Rows.Count == 0) return;

                int success = 0;
                int failed = 0;
                foreach (DataRow r in dtExcel.Rows)
                {
                    try
                    {
                        string tenThuoc = r.Table.Columns.Contains("TenThuoc") ? r["TenThuoc"]?.ToString() ?? "" : r[0]?.ToString() ?? "";
                        if (string.IsNullOrWhiteSpace(tenThuoc)) continue;

                        string dvt = r.Table.Columns.Contains("DonViTinh") ? r["DonViTinh"]?.ToString() ?? "Viên" : "Viên";
                        decimal giaNhap = 0;
                        if (r.Table.Columns.Contains("DonGiaNhap") && decimal.TryParse(r["DonGiaNhap"]?.ToString(), out var gn))
                            giaNhap = gn;
                        decimal giaBan = 0;
                        if (r.Table.Columns.Contains("DonGiaBan") && decimal.TryParse(r["DonGiaBan"]?.ToString(), out var gb))
                            giaBan = gb;
                        int slTon = 0;
                        if (r.Table.Columns.Contains("SoLuongTon") && int.TryParse(r["SoLuongTon"]?.ToString(), out var sl))
                            slTon = sl;
                        string cachDung = r.Table.Columns.Contains("CachDungMacDinh") ? r["CachDungMacDinh"]?.ToString() ?? "" : "";

                        string maThuocMoi = "TH" + DateTime.Now.ToString("HHmmss") + (success % 100).ToString("D2");
                        var thuoc = new Thuoc
                        {
                            MaThuoc = maThuocMoi,
                            TenThuoc = tenThuoc.Trim(),
                            DonViTinh = string.IsNullOrWhiteSpace(dvt) ? "Viên" : dvt.Trim(),
                            DonGiaBan = giaBan > 0 ? giaBan : 10000m,
                            SoLuongTon = slTon,
                            CachDungMacDinh = string.IsNullOrWhiteSpace(cachDung) ? null : cachDung.Trim()
                        };

                        if (ClinicDAL.LuuThuoc(thuoc, isNew: true))
                            success++;
                        else
                            failed++;
                    }
                    catch
                    {
                        failed++;
                    }
                }

                MessageBox.Show($"Hoàn tất nhập dữ liệu từ Excel!\n- Thành công: {success} thuốc\n- Lỗi/Bỏ qua: {failed}",
                    "Kết quả nhập thuốc Excel", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadThuoc();
                dgvCB.DataSource = ClinicDAL.LayThuocSapHet(30);
            };

            wrapper.Controls.Add(split);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8, BackColor = Color.Transparent });
            wrapper.Controls.Add(toolPanel);
            panelContent.Controls.Add(wrapper);
        }

        // ═══════════════════════════════════════════════════════════
        //  LỄ TÂN & THU NGÂN: THU NGÂN & HÓA ĐƠN
        // ═══════════════════════════════════════════════════════════
        private void ShowLeTanThanhToanView()
        {
            if (!SessionState.IsReceptionist)
            {
                ShowThuNganView();
                return;
            }

            string maLeTan = SessionState.CurrentUser?.MaNV ?? "";
            if (string.IsNullOrWhiteSpace(maLeTan))
            {
                MessageBox.Show("Không xác định được tài khoản y tá đang đăng nhập.",
                    "Không thể mở thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetRefreshPageAction(ShowLeTanThanhToanView);
            SetActiveNav(btnNavThuNgan);
            SetPageHeader("Y tá  ›  Thanh toán", "Lập hóa đơn và thu tiền bệnh nhân tại phòng được phân công.");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var toolbar = new Panel { Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent };
            var shiftInfo = new Label
            {
                Location = new Point(0, 8),
                Size = new Size(Math.Max(120, toolbar.Width - 250), 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = F_Base,
                ForeColor = C_TextGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var shiftButton = SecondaryBtn("Mở ca", 0, 4, 105, 32, C_Teal);
            shiftButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var refresh = SecondaryBtn("Làm mới", 0, 4, 95, 32, C_Blue);
            refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            toolbar.SizeChanged += (sender, args) =>
            {
                refresh.Location = new Point(toolbar.ClientSize.Width - refresh.Width, 4);
                shiftButton.Location = new Point(refresh.Left - shiftButton.Width - 8, 4);
                shiftInfo.Width = Math.Max(120, shiftButton.Left - 16);
            };
            toolbar.Controls.AddRange(new Control[] { shiftInfo, shiftButton, refresh });

            var sections = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0, 6, 0, 0),
                BackColor = Color.Transparent
            };
            sections.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
            sections.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

            var pendingGrid = MakeGrid();
            var invoiceGrid = MakeGrid();
            var createInvoice = PrimaryBtn("Lập hóa đơn khám", 0, 0, 150, 32);
            var payInvoice = PrimaryBtn("Thanh toán hóa đơn", 0, 0, 165, 32);

            Panel CreateSection(string title, DataGridView grid, Control action)
            {
                var section = new Panel { Dock = DockStyle.Fill, BackColor = C_Card, Padding = new Padding(10) };
                section.Paint += PaintCardBorder;
                var titleLabel = MakeLbl(title, 10, 8, bold: true);
                titleLabel.Dock = DockStyle.Top;
                titleLabel.Height = 30;
                titleLabel.Padding = new Padding(2, 4, 0, 0);
                var actions = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    Height = 44,
                    FlowDirection = FlowDirection.RightToLeft,
                    Padding = new Padding(4, 4, 4, 2)
                };
                actions.Controls.Add(action);
                grid.Dock = DockStyle.Fill;
                section.Controls.AddRange(new Control[] { grid, actions, titleLabel });
                return section;
            }

            sections.Controls.Add(CreateSection("PHIẾU KHÁM ĐÃ HOÀN TẤT — CHỜ LẬP HÓA ĐƠN", pendingGrid, createInvoice), 0, 0);
            sections.Controls.Add(CreateSection("HÓA ĐƠN CHƯA THANH TOÁN", invoiceGrid, payInvoice), 0, 1);

            void FormatPendingGrid()
            {
                foreach (string column in new[] { "STT", "SoDienThoai", "TrangThai" })
                    SafeFormatCol(pendingGrid, column, "", null, false);
                SafeFormatCol(pendingGrid, "MaPK", "Mã phiếu", 90);
                SafeFormatCol(pendingGrid, "NgayKham", "Ngày khám", 145);
                SafeFormatCol(pendingGrid, "TenBN", "Bệnh nhân", 180);
                SafeFormatCol(pendingGrid, "TenBacSi", "Bác sĩ", 160);
                SafeFormatCol(pendingGrid, "YTaNhanBangKe", "Y tá nhận bảng kê", 150);
                SafeFormatCol(pendingGrid, "ChanDoan", "Chẩn đoán", 220);
                if (pendingGrid.Columns.Contains("NgayKham"))
                    pendingGrid.Columns["NgayKham"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }

            void FormatInvoiceGrid()
            {
                foreach (string column in new[]
                {
                    "ThuNgan", "TienDichVu", "TienThuoc", "GiamGia", "PhanTramBaoHiem", "HinhThucTT"
                })
                    SafeFormatCol(invoiceGrid, column, "", null, false);
                SafeFormatCol(invoiceGrid, "MaHD", "Mã hóa đơn", 100);
                SafeFormatCol(invoiceGrid, "MaPK", "Mã phiếu", 90);
                SafeFormatCol(invoiceGrid, "TenBN", "Bệnh nhân", 180);
                SafeFormatCol(invoiceGrid, "YTaNhanBangKe", "Y tá nhận bảng kê", 150);
                SafeFormatCol(invoiceGrid, "NgayLap", "Ngày lập", 145);
                SafeFormatCol(invoiceGrid, "TongTien", "Tổng viện phí", 120);
                SafeFormatCol(invoiceGrid, "TienBaoHiem", "Bảo hiểm", 100);
                SafeFormatCol(invoiceGrid, "DaThu", "Đã thu", 100);
                SafeFormatCol(invoiceGrid, "ConNo", "Còn nợ", 100);
                if (invoiceGrid.Columns.Contains("NgayLap"))
                    invoiceGrid.Columns["NgayLap"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                foreach (string column in new[] { "TongTien", "TienBaoHiem", "DaThu", "ConNo" })
                    if (invoiceGrid.Columns.Contains(column))
                    {
                        invoiceGrid.Columns[column]!.DefaultCellStyle.Format = "N0";
                        invoiceGrid.Columns[column]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
            }

            void UpdateShiftStatus()
            {
                DataTable currentShift = ClinicDAL.LayThongTinCaThuNgan();
                bool open = currentShift.Rows.Count > 0;
                shiftButton.Text = open ? "Đóng ca" : "Mở ca";
                shiftButton.BackColor = open ? C_Orange : C_Teal;
                payInvoice.Enabled = open;
                shiftInfo.Text = open
                    ? $"Ca thu ngân đang mở: #{currentShift.Rows[0]["MaCa"]}"
                    : "Cần mở ca trước khi ghi nhận thanh toán.";
            }

            void LoadData()
            {
                pendingGrid.DataSource = ClinicDAL.LayDanhSachChoThanhToan(maLeTan: maLeTan);
                FormatPendingGrid();
                UiText.Apply(pendingGrid);
                invoiceGrid.DataSource = ClinicDAL.LayDanhSachHoaDon(
                    trangThai: "Chưa thanh toán",
                    maLeTan: maLeTan);
                FormatInvoiceGrid();
                UiText.Apply(invoiceGrid);
                UpdateShiftStatus();
            }

            shiftButton.Click += (sender, args) =>
            {
                try
                {
                    DataTable currentShift = ClinicDAL.LayThongTinCaThuNgan();
                    if (currentShift.Rows.Count == 0)
                    {
                        string input = Microsoft.VisualBasic.Interaction.InputBox(
                            "Nhập tiền đầu ca:", "Mở ca thu ngân", "0");
                        if (!decimal.TryParse(input, System.Globalization.NumberStyles.Number,
                                System.Globalization.CultureInfo.CurrentCulture, out decimal openingAmount)
                            || openingAmount < 0)
                        {
                            MessageBox.Show("Tiền đầu ca phải là số không âm.", "Dữ liệu không hợp lệ",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        ClinicDAL.MoCaThuNgan(openingAmount);
                    }
                    else
                    {
                        decimal expected = Convert.ToDecimal(currentShift.Rows[0]["TienDuKien"]);
                        string input = Microsoft.VisualBasic.Interaction.InputBox(
                            $"Tiền dự kiến trong ca: {expected:N0} đ\nNhập số tiền thực tế kiểm đếm:",
                            "Đóng ca thu ngân", expected.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture));
                        if (!decimal.TryParse(input, System.Globalization.NumberStyles.Number,
                                System.Globalization.CultureInfo.CurrentCulture, out decimal actualAmount)
                            || actualAmount < 0)
                        {
                            MessageBox.Show("Tiền kiểm đếm phải là số không âm.", "Dữ liệu không hợp lệ",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        string note = Microsoft.VisualBasic.Interaction.InputBox(
                            "Ghi chú chênh lệch (nếu có):", "Đối soát cuối ca", "");
                        ClinicDAL.DongCaThuNgan(actualAmount, note);
                    }
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể cập nhật ca thu ngân",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            refresh.Click += (sender, args) => LoadData();
            createInvoice.Click += (sender, args) =>
            {
                if (pendingGrid.SelectedRows.Count == 0
                    || pendingGrid.SelectedRows[0].DataBoundItem is not DataRowView selected)
                {
                    MessageBox.Show("Chọn phiếu khám đã hoàn tất cần lập hóa đơn.",
                        "Chưa chọn phiếu khám", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                CreateInvoice(selected.Row);
                LoadData();
            };
            payInvoice.Click += (sender, args) =>
            {
                if (invoiceGrid.SelectedRows.Count == 0
                    || invoiceGrid.SelectedRows[0].DataBoundItem is not DataRowView selected)
                {
                    MessageBox.Show("Chọn hóa đơn cần thanh toán.",
                        "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (ShowInvoicePayment(selected.Row))
                    LoadData();
            };

            wrapper.Controls.Add(sections);
            wrapper.Controls.Add(toolbar);
            panelContent.Controls.Add(wrapper);
            LoadData();
        }

        private void ShowThuNganView()
        {
            if (!SessionState.IsAdmin && !SessionState.IsCashier)
            {
                MessageBox.Show("Chức năng thanh toán chỉ dành cho thu ngân và quản trị viên.",
                    "Không đủ quyền", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SetRefreshPageAction(ShowThuNganView);
            SetActiveNav(btnNavThuNgan);
            SetPageHeader("Thu ngân  ›  Thanh toán", "Quản lý hóa đơn và thanh toán viện phí");
            panelContent.Controls.Clear();

            var wrapper = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            // ── Khối Biểu đồ Thu ngân (Doanh thu & Donut Charts) ──
            var pnlChartThuNgan = new Panel { Dock = DockStyle.Top, Height = 142, BackColor = Color.Transparent, Padding = new Padding(0, 0, 0, 6) };

            var cardRevenue = new Panel { BackColor = C_Card, Padding = new Padding(12) };
            cardRevenue.Paint += PaintCardBorder;
            cardRevenue.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                int w = cardRevenue.ClientSize.Width;
                int h = cardRevenue.ClientSize.Height;
                if (w < 80 || h < 60) return;

                var k = ClinicDAL.LayThongKeThuNganHomNay();

                using var fontTitle = new Font("Segoe UI", 8.5f, FontStyle.Bold);
                using var brushTitle = new SolidBrush(Color.FromArgb(75, 85, 99));
                g.DrawString("💰 DOANH THU HÔM NAY", fontTitle, brushTitle, 12, 10);

                using var fontVal = new Font("Segoe UI", 16f, FontStyle.Bold);
                using var brushVal = new SolidBrush(Color.FromArgb(16, 185, 129));
                g.DrawString($"{k.doanhThuHomNay:N0} đ", fontVal, brushVal, 12, 30);

                using var fontSub = new Font("Segoe UI", 8f, FontStyle.Regular);
                using var brushSub = new SolidBrush(Color.FromArgb(107, 114, 128));
                g.DrawString($"Đã thu: {k.daThuCount} HĐ  •  Chờ thu: {k.tienChuaThu:N0} đ ({k.chuaThuCount} HĐ)", fontSub, brushSub, 12, 64);

                int totalHĐ = k.daThuCount + k.chuaThuCount;
                float rate = totalHĐ > 0 ? (float)k.daThuCount / totalHĐ : 0;
                int barX = 12;
                int barY = 88;
                int barW = Math.Max(20, w - 24);
                int barH = 7;

                using var brushBarBg = new SolidBrush(Color.FromArgb(243, 244, 246));
                g.FillRectangle(brushBarBg, barX, barY, barW, barH);

                int fillW = (int)(barW * Math.Clamp(rate, 0f, 1f));
                if (fillW > 0)
                {
                    using var brushFill = new SolidBrush(Color.FromArgb(16, 185, 129));
                    g.FillRectangle(brushFill, barX, barY, fillW, barH);
                }

                using var fontRate = new Font("Segoe UI", 7.5f, FontStyle.Regular);
                using var brushRate = new SolidBrush(Color.FromArgb(107, 114, 128));
                g.DrawString($"Tỷ lệ hoàn tất thu: {rate:P0}", fontRate, brushRate, 12, 100);
            };

            var chartStatus = CreateDonutChart(
                "📊 TRẠNG THÁI HÓA ĐƠN",
                () =>
                {
                    var k = ClinicDAL.LayThongKeThuNganHomNay();
                    return new (string Label, int Value, Color Color)[]
                    {
                        ("Đã thu", k.daThuCount, Color.FromArgb(16, 185, 129)),
                        ("Chờ thu", k.chuaThuCount, Color.FromArgb(245, 158, 11)),
                        ("Đã hủy", k.daHuyCount, Color.FromArgb(239, 68, 68))
                    };
                },
                () =>
                {
                    var k = ClinicDAL.LayThongKeThuNganHomNay();
                    int total = k.daThuCount + k.chuaThuCount + k.daHuyCount;
                    return ($"{total}", "HĐ");
                });

            var chartPayment = CreateDonutChart(
                "💳 HÌNH THỨC THANH TOÁN",
                () =>
                {
                    var k = ClinicDAL.LayThongKeThuNganHomNay();
                    return new (string Label, int Value, Color Color)[]
                    {
                        ("Tiền mặt", k.tienMatCount, Color.FromArgb(14, 165, 233)),
                        ("Chuyển khoản", k.chuyenKhoanCount, Color.FromArgb(99, 102, 241))
                    };
                },
                () =>
                {
                    var k = ClinicDAL.LayThongKeThuNganHomNay();
                    int total = k.tienMatCount + k.chuyenKhoanCount;
                    return ($"{total}", "Lượt");
                });

            pnlChartThuNgan.Controls.AddRange(new Control[] { cardRevenue, chartStatus, chartPayment });

            void LayoutThuNganCharts()
            {
                int totalW = pnlChartThuNgan.ClientSize.Width;
                int gap = 10;
                int revW = Math.Min(320, Math.Max(230, totalW / 4));
                int remW = Math.Max(0, totalW - revW - gap * 2);
                int chartW = Math.Max(180, remW / 2);

                cardRevenue.Location  = new Point(0, 0);
                cardRevenue.Size      = new Size(revW, pnlChartThuNgan.ClientSize.Height - 6);

                chartStatus.Location  = new Point(revW + gap, 0);
                chartStatus.Size      = new Size(chartW, pnlChartThuNgan.ClientSize.Height - 6);

                int paymentX = revW + gap + chartW + gap;
                chartPayment.Location = new Point(paymentX, 0);
                chartPayment.Size     = new Size(Math.Max(180, totalW - paymentX), pnlChartThuNgan.ClientSize.Height - 6);
            }
            pnlChartThuNgan.SizeChanged += (s, e) => LayoutThuNganCharts();
            LayoutThuNganCharts();

            void RefreshThuNganKPI()
            {
                cardRevenue.Invalidate();
                chartStatus.Invalidate();
                chartPayment.Invalidate();
            }

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
            var details = SecondaryBtn("Chi tiết hóa đơn", 680, 59, 140, 32, C_Teal);
            var printInvoice = SecondaryBtn("🖨 In/Lưu PDF", 830, 59, 120, 32, Color.FromArgb(10, 80, 160));
            var exportInvoices = SecondaryBtn("📥 Xuất Excel", 960, 59, 110, 32, C_Green);
            var cancelInvoice = SecondaryBtn("Hủy HĐ", 1080, 59, 85, 32, C_Red);
            var shiftButton = SecondaryBtn("Mở ca", 0, 8, 100, 30, C_Teal);
            shiftButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var refundButton = SecondaryBtn(SessionState.IsAdmin ? "Duyệt hoàn tiền" : "Đề nghị hoàn tiền",
                0, 8, 150, 30, C_Orange);
            refundButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            var btnToggleChart = SecondaryBtn("📊 Ẩn biểu đồ", 0, 8, 115, 30, Color.FromArgb(75, 85, 99));
            btnToggleChart.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnToggleChart.Click += (s, e) =>
            {
                pnlChartThuNgan.Visible = !pnlChartThuNgan.Visible;
                btnToggleChart.Text = pnlChartThuNgan.Visible ? "📊 Ẩn biểu đồ" : "📊 Mở biểu đồ";
            };

            header.SizeChanged += (sender, args) =>
            {
                shiftButton.Location = new Point(header.ClientSize.Width - shiftButton.Width - 8, 8);
                refundButton.Location = new Point(shiftButton.Left - refundButton.Width - 8, 8);
                btnToggleChart.Location = new Point(refundButton.Left - btnToggleChart.Width - 8, 8);
            };
            header.Controls.AddRange(new Control[] { search, status, refresh, pay, details, printInvoice,
                exportInvoices, cancelInvoice, shiftButton, refundButton, btnToggleChart });

            var grid = MakeGrid();
            grid.ScrollBars = ScrollBars.Both;
            grid.DataBindingComplete += (sender, args) =>
            {
                // Ẩn các cột bóc tách kỹ thuật nội bộ (đã có đầy đủ trong popup 'Chi tiết hóa đơn')
                // để bảng tổng hợp thoáng đẹp, chữ hiển thị trọn vẹn không bị co cụt
                string[] hideCols = { "MaPK", "TienDichVu", "TienThuoc", "GiamGia", "PhanTramBaoHiem" };
                foreach (string col in hideCols)
                    if (grid.Columns.Contains(col))
                        grid.Columns[col]!.Visible = false;

                SafeFormatCol(grid, "MaHD", "Mã HĐ", 90, fillWeight: 80f);
                SafeFormatCol(grid, "TenBN", "Bệnh nhân", 150, fillWeight: 140f);
                SafeFormatCol(grid, "NgayLap", "Ngày lập", 130, fillWeight: 110f);
                SafeFormatCol(grid, "TongTien", "Tổng chi phí", 115, fillWeight: 105f);
                SafeFormatCol(grid, "TienBaoHiem", "Bảo hiểm", 100, fillWeight: 90f);
                SafeFormatCol(grid, "DaThu", "Đã thu", 105, fillWeight: 95f);
                SafeFormatCol(grid, "ConNo", "Còn nợ", 100, fillWeight: 90f);
                SafeFormatCol(grid, "HinhThucTT", "Hình thức TT", 115, fillWeight: 100f);
                SafeFormatCol(grid, "ThuNgan", "Thu ngân", 130, fillWeight: 110f);
                SafeFormatCol(grid, "TrangThai", "Trạng thái", 120, fillWeight: 105f);

                if (grid.Columns.Contains("NgayLap"))
                    grid.Columns["NgayLap"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";

                foreach (string column in new[] { "TongTien", "TienBaoHiem", "DaThu", "ConNo" })
                {
                    if (grid.Columns.Contains(column))
                    {
                        grid.Columns[column]!.DefaultCellStyle.Format = "N0";
                        grid.Columns[column]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                    }
                }
            };
            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 250, Padding = new Padding(0, 8, 0, 0) };
            var pendingTitle = MakeLbl("PHIẾU KHÁM CHỜ LẬP HÓA ĐƠN", 0, 0, bold: true);
            var createInvoice = PrimaryBtn("＋ Lập hóa đơn", 0, 0, 130, 30);
            createInvoice.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            var pendingGrid = MakeGrid();
            pendingGrid.ScrollBars = ScrollBars.Both;
            pendingGrid.DataBindingComplete += (sender, args) =>
            {
                SafeFormatCol(pendingGrid, "MaPK", "Mã PK", 75, fillWeight: 75f);
                SafeFormatCol(pendingGrid, "STT", "STT", 45, fillWeight: 45f);
                SafeFormatCol(pendingGrid, "NgayKham", "Ngày khám", 125, fillWeight: 110f);
                SafeFormatCol(pendingGrid, "TenBN", "Bệnh nhân", 145, fillWeight: 140f);
                SafeFormatCol(pendingGrid, "SoDienThoai", "Số điện thoại", 100, fillWeight: 95f);
                SafeFormatCol(pendingGrid, "TenBacSi", "Bác sĩ phụ trách", 125, fillWeight: 115f);
                SafeFormatCol(pendingGrid, "ChanDoan", "Chẩn đoán", 180, fillWeight: 200f);
                SafeFormatCol(pendingGrid, "TrangThai", "Trạng thái", 100, fillWeight: 95f);

                if (pendingGrid.Columns.Contains("NgayKham"))
                    pendingGrid.Columns["NgayKham"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            };
            pendingGrid.Location = new Point(0, 32);
            pendingGrid.Size = new Size(bottom.ClientSize.Width, bottom.ClientSize.Height - 36);
            pendingGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            bottom.Controls.AddRange(new Control[] { pendingTitle, createInvoice, pendingGrid });
            bottom.SizeChanged += (s, e) =>
            {
                createInvoice.Location = new Point(bottom.ClientSize.Width - createInvoice.Width, 0);
            };
            var invoicesPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 5, 0, 5) };
            invoicesPanel.Paint += PaintCardBorder;
            invoicesPanel.Controls.Add(grid);

            GridPageController? invoicePageController = null;
            void UpdateShiftState(int totalInvoices, int waitingCount)
            {
                DataTable currentShift = ClinicDAL.LayThongTinCaThuNgan();
                bool open = currentShift.Rows.Count > 0;
                shiftButton.Text = open ? "Đóng ca" : "Mở ca";
                shiftButton.BackColor = open ? C_Orange : C_Teal;
                pay.Enabled = open;
                if (open)
                {
                    DataRow shift = currentShift.Rows[0];
                    summary.Text = $"Ca #{shift["MaCa"]} đang mở • Dự kiến {Convert.ToDecimal(shift["TienDuKien"]):N0} đ • {totalInvoices} hóa đơn, {waitingCount} phiếu chờ lập";
                }
                else
                {
                    summary.Text = $"Chưa mở ca thu ngân • Cần mở ca trước khi thu • {totalInvoices} hóa đơn, {waitingCount} phiếu chờ lập";
                }
            }
            void LoadInvoices()
            {
                invoicePageController?.Reload();
                var waiting = ClinicDAL.LayDanhSachChoThanhToan();
                pendingGrid.DataSource = waiting;
                int totalInvoices = grid.DataSource is DataTable invoices
                    && invoices.ExtendedProperties["TotalCount"] is int count
                    ? count
                    : grid.Rows.Count;
                UpdateShiftState(totalInvoices, waiting.Rows.Count);
                RefreshThuNganKPI();
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
            shiftButton.Click += (s, e) =>
            {
                try
                {
                    DataTable currentShift = ClinicDAL.LayThongTinCaThuNgan();
                    if (currentShift.Rows.Count == 0)
                    {
                        string input = Microsoft.VisualBasic.Interaction.InputBox(
                            "Nhập tiền đầu ca:", "Mở ca thu ngân", "0");
                        if (!decimal.TryParse(input, System.Globalization.NumberStyles.Number,
                                System.Globalization.CultureInfo.CurrentCulture, out decimal openingAmount)
                            || openingAmount < 0)
                        {
                            MessageBox.Show("Tiền đầu ca phải là số không âm.", "Dữ liệu không hợp lệ",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        ClinicDAL.MoCaThuNgan(openingAmount);
                    }
                    else
                    {
                        decimal expected = Convert.ToDecimal(currentShift.Rows[0]["TienDuKien"]);
                        string input = Microsoft.VisualBasic.Interaction.InputBox(
                            $"Tiền dự kiến trong ca: {expected:N0} đ\nNhập số tiền thực tế kiểm đếm:",
                            "Đóng ca thu ngân", expected.ToString("0.##", System.Globalization.CultureInfo.CurrentCulture));
                        if (!decimal.TryParse(input, System.Globalization.NumberStyles.Number,
                                System.Globalization.CultureInfo.CurrentCulture, out decimal actualAmount)
                            || actualAmount < 0)
                        {
                            MessageBox.Show("Tiền kiểm đếm phải là số không âm.", "Dữ liệu không hợp lệ",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        string note = Microsoft.VisualBasic.Interaction.InputBox(
                            "Ghi chú chênh lệch (nếu có):", "Đối soát cuối ca", "");
                        ClinicDAL.DongCaThuNgan(actualAmount, note);
                    }
                    LoadInvoices();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể cập nhật ca thu ngân",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            refundButton.Click += (s, e) =>
            {
                if (SessionState.IsAdmin)
                {
                    ShowRefundRequests();
                    return;
                }
                DataRow? invoice = SelectedInvoice();
                if (invoice == null
                    || invoice["TrangThai"].ToString() == "Đã hủy"
                    || Convert.ToDecimal(invoice["DaThu"]) <= 0)
                {
                    MessageBox.Show("Chọn hóa đơn có khoản đã thu để đề nghị hoàn tiền.",
                        "Không thể đề nghị", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                string amountText = Microsoft.VisualBasic.Interaction.InputBox(
                    "Nhập số tiền cần hoàn:", "Đề nghị hoàn tiền", "");
                if (!decimal.TryParse(amountText, System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.CurrentCulture, out decimal refundAmount)
                    || refundAmount <= 0)
                {
                    MessageBox.Show("Số tiền hoàn phải lớn hơn 0.", "Dữ liệu không hợp lệ",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string reason = Microsoft.VisualBasic.Interaction.InputBox(
                    "Nhập lý do đề nghị hoàn tiền:", "Đề nghị hoàn tiền", "");
                if (string.IsNullOrWhiteSpace(reason))
                    return;
                try
                {
                    ClinicDAL.TaoYeuCauHoanTien(invoice["MaHD"].ToString() ?? "", refundAmount, reason);
                    MessageBox.Show("Đã gửi yêu cầu hoàn tiền chờ quản trị viên duyệt.",
                        "Đã gửi yêu cầu", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể gửi yêu cầu hoàn tiền",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
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

            printInvoice.Click += (s, e) =>
            {
                DataRow? invoice = SelectedInvoice();
                if (invoice == null)
                {
                    MessageBox.Show("Vui lòng chọn một hóa đơn để in / xuất PDF!", "Chưa chọn hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string maHD = invoice["MaHD"]?.ToString() ?? "";
                string maPK = invoice["MaPK"]?.ToString() ?? "";
                string tenBN = invoice["TenBN"]?.ToString() ?? "";
                decimal tienDV = Convert.ToDecimal(invoice["TienDichVu"]);
                decimal tienThuoc = Convert.ToDecimal(invoice["TienThuoc"]);
                decimal giamGia = Convert.ToDecimal(invoice["GiamGia"]);
                decimal tongTien = Convert.ToDecimal(invoice["TongTien"]);
                string hinhThucTT = invoice["HinhThucTT"]?.ToString() ?? "Tiền mặt";
                string chanDoan = invoice.Table.Columns.Contains("ChanDoan") ? invoice["ChanDoan"]?.ToString() ?? "Khám bệnh" : "Khám bệnh tổng quát";
                string nguoiLap = SessionState.CurrentUser?.HoTenNhanVien ?? "Thu ngân";

                PdfPrintHelper.InHoaDon(
                    maHD, maPK, tenBN, chanDoan, hinhThucTT, tienDV, tienThuoc, giamGia, tongTien,
                    nguoiLap,
                    invoice.Table.Columns.Contains("TienBaoHiem") ? Convert.ToDecimal(invoice["TienBaoHiem"]) : 0,
                    invoice.Table.Columns.Contains("DaThu") ? Convert.ToDecimal(invoice["DaThu"]) : 0,
                    invoice.Table.Columns.Contains("ConNo") ? Convert.ToDecimal(invoice["ConNo"]) : tongTien);
            };

            exportInvoices.Click += (s, e) =>
            {
                string state = status.SelectedItem?.ToString() ?? "Tất cả trạng thái";
                DataTable dt = ClinicDAL.LayDanhSachHoaDon(
                    search.Text.Trim(),
                    state == "Tất cả trạng thái" ? null : state);
                ExcelHelper.ExportToExcel(dt, "DanhSachHoaDon", "DANH SÁCH HÓA ĐƠN VIỆN PHÍ", $"HoaDonVienPhi_{DateTime.Now:yyyyMMdd}.xlsx");
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
            wrapper.Controls.Add(pnlChartThuNgan);
            wrapper.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 6, BackColor = Color.Transparent });
            wrapper.Controls.Add(header);
            panelContent.Controls.Add(wrapper);
            invoicePageController = GridPageController.Attach(grid, (offset, pageSize) =>
            {
                string state = status.SelectedItem?.ToString() ?? "Tất cả trạng thái";
                return ClinicDAL.LayDanhSachHoaDon(
                    search.Text.Trim(),
                    state == "Tất cả trạng thái" ? null : state,
                    offset,
                    pageSize);
            });
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
            var tabControl = new TabControl { Dock = DockStyle.Fill, Font = F_Base };
            var itemsTab = new TabPage("Khoản phí");
            var transactionsTab = new TabPage("Lịch sử giao dịch");
            var grid = MakeGrid();
            grid.DataSource = ClinicDAL.LayChiTietHoaDon(maHD);
            DataGridViewColumnCollection? detailColumns = grid.Columns;
            foreach (string column in new[] { "DonGia", "ThanhTien" })
            {
                DataGridViewColumn? detailColumn = detailColumns?[column];
                if (detailColumn != null)
                    detailColumn.DefaultCellStyle.Format = "N0";
            }
            var transactionGrid = MakeGrid();
            transactionGrid.DataSource = ClinicDAL.LayGiaoDichVienPhi(maHD);
            foreach (string column in new[] { "SoTien" })
                if (transactionGrid.Columns.Contains(column))
                    transactionGrid.Columns[column]!.DefaultCellStyle.Format = "N0";
            if (transactionGrid.Columns.Contains("ThoiGian"))
                transactionGrid.Columns["ThoiGian"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            itemsTab.Controls.Add(grid);
            transactionsTab.Controls.Add(transactionGrid);
            tabControl.TabPages.AddRange(new[] { itemsTab, transactionsTab });
            decimal paidAmount = invoice.Table.Columns.Contains("DaThu")
                ? Convert.ToDecimal(invoice["DaThu"])
                : 0;
            decimal balance = invoice.Table.Columns.Contains("ConNo")
                ? Convert.ToDecimal(invoice["ConNo"])
                : Convert.ToDecimal(invoice["TongTien"]) - Convert.ToDecimal(invoice["TienBaoHiem"]) - paidAmount;
            var footer = new Label
            {
                Text = $"Tổng: {Convert.ToDecimal(invoice["TongTien"]):N0} đ   •   BHYT: {Convert.ToDecimal(invoice["TienBaoHiem"]):N0} đ   •   Đã thu: {paidAmount:N0} đ   •   Còn nợ: {balance:N0} đ",
                Dock = DockStyle.Bottom,
                Height = 48,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 0, 14, 0),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = C_Blue
            };
            dialog.Controls.Add(tabControl);
            dialog.Controls.Add(footer);
            dialog.Controls.Add(heading);
            dialog.ShowDialog(this);
            dialog.Dispose();
        }

        private bool ShowInvoicePayment(DataRow invoice)
        {
            string maHD = invoice["MaHD"].ToString() ?? "";
            string tenBN = invoice["TenBN"]?.ToString() ?? "Bệnh nhân";

            decimal balanceDue = invoice.Table.Columns.Contains("ConNo")
                ? Convert.ToDecimal(invoice["ConNo"])
                : Convert.ToDecimal(invoice["TongTien"]) - Convert.ToDecimal(invoice["TienBaoHiem"]);

            var bank = VietQRHelper.GetConfig();
            string transferContent = $"THANH TOAN {maHD}";

            using var dialog = new Form
            {
                Text = $"Xác nhận thanh toán - Hóa đơn #{maHD}",
                Size = new Size(880, 530),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            // ── CỘT TRÁI (THÔNG TIN & NHẬP LIỆU) ──
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 440,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White
            };

            var lblHeading = new Label
            {
                Text = $"HÓA ĐƠN: #{maHD}",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = C_TextDark,
                Location = new Point(14, 10),
                AutoSize = true
            };
            var lblPatient = new Label
            {
                Text = $"Bệnh nhân: {tenBN}",
                Font = F_Base,
                ForeColor = C_TextGray,
                Location = new Point(14, 34),
                AutoSize = true
            };

            var pnlDue = new Panel
            {
                Location = new Point(14, 60),
                Size = new Size(405, 56),
                BackColor = Color.FromArgb(240, 253, 244)
            };
            pnlDue.Paint += (s, e) =>
            {
                using var p = new Pen(C_Green, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlDue.Width - 1, pnlDue.Height - 1);
            };
            var lblDueTitle = new Label { Text = "SỐ TIỀN CÒN PHẢI THU", Location = new Point(10, 6), AutoSize = true, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(22, 101, 52) };
            var lblDueVal = new Label { Text = $"{balanceDue:N0} đ", Location = new Point(8, 22), AutoSize = true, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = C_Green };
            pnlDue.Controls.AddRange(new Control[] { lblDueTitle, lblDueVal });

            var lblPayAmount = new Label { Text = "Số tiền thu đợt này (VNĐ):", Location = new Point(14, 124), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };
            var paymentAmount = new NumericUpDown
            {
                Location = new Point(14, 146),
                Size = new Size(405, 30),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Minimum = 1,
                Maximum = Math.Max(1, balanceDue),
                Value = Math.Max(1, balanceDue),
                DecimalPlaces = 0,
                ThousandsSeparator = true
            };

            var lblMethod = new Label { Text = "Phương thức thanh toán:", Location = new Point(14, 184), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };
            var method = new ComboBox
            {
                Location = new Point(14, 206),
                Size = new Size(405, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            method.Items.AddRange(new object[] { "Chuyển khoản QR", "Tiền mặt", "Thẻ" });
            string currentMethod = invoice["HinhThucTT"]?.ToString() ?? "";
            method.SelectedItem = method.Items.Contains(currentMethod) ? currentMethod : "Chuyển khoản QR";

            // Panel thông tin chuyển khoản ngân hàng
            var pnlBankInfo = new Panel
            {
                Location = new Point(14, 244),
                Size = new Size(405, 175),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlBankInfo.Paint += (s, e) =>
            {
                using var p = new Pen(C_Border, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlBankInfo.Width - 1, pnlBankInfo.Height - 1);
            };

            var lblBTitle = new Label { Text = "🏦 TÀI KHOẢN NHẬN TIỀN (PHÒNG KHÁM)", Location = new Point(10, 8), AutoSize = true, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = C_TealDark };
            var lblBName = new Label { Text = $"Ngân hàng: {bank.BankName}", Location = new Point(10, 28), Size = new Size(385, 18), AutoEllipsis = true, Font = F_Small };
            var lblBAcc = new Label { Text = $"Số TK: {bank.AccountNo}", Location = new Point(10, 48), Size = new Size(290, 18), AutoEllipsis = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = C_TextDark };
            var btnCopyAcc = new Button { Text = "📋 Chép STK", Location = new Point(305, 46), Size = new Size(90, 24), Font = new Font("Segoe UI", 7.5f), FlatStyle = FlatStyle.Flat };
            btnCopyAcc.Click += (s, e) => { Clipboard.SetText(bank.AccountNo); MessageBox.Show("Đã sao chép số tài khoản!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); };

            var lblBHolder = new Label { Text = $"Chủ TK: {bank.AccountName}", Location = new Point(10, 72), Size = new Size(385, 18), AutoEllipsis = true, Font = F_Small };

            var lblBContent = new Label { Text = $"Nội dung CK: {transferContent}", Location = new Point(10, 96), Size = new Size(290, 18), AutoEllipsis = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = C_Blue };
            var btnCopyContent = new Button { Text = "📋 Chép ND", Location = new Point(305, 94), Size = new Size(90, 24), Font = new Font("Segoe UI", 7.5f), FlatStyle = FlatStyle.Flat };
            btnCopyContent.Click += (s, e) => { Clipboard.SetText(transferContent); MessageBox.Show("Đã sao chép nội dung chuyển khoản!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); };

            var btnChangeBank = new LinkLabel { Text = "⚙ Cấu hình thông tin STK ngân hàng phòng khám", Location = new Point(10, 124), AutoSize = true, Font = F_Small, LinkColor = C_Teal };

            pnlBankInfo.Controls.AddRange(new Control[] { lblBTitle, lblBName, lblBAcc, btnCopyAcc, lblBHolder, lblBContent, btnCopyContent, btnChangeBank });

            // Actions panel
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 14, 8)
            };
            var confirm = PrimaryBtn("✓ Xác nhận thanh toán", 0, 0, 180, 36);
            var cancel = new Button { Text = "Đóng", Width = 90, Height = 36, DialogResult = DialogResult.Cancel, BackColor = Color.White, FlatStyle = FlatStyle.Flat, Font = F_Base };
            pnlActions.Controls.AddRange(new Control[] { confirm, cancel });

            pnlLeft.Controls.AddRange(new Control[] { lblHeading, lblPatient, pnlDue, lblPayAmount, paymentAmount, lblMethod, method, pnlBankInfo, pnlActions });

            // ── CỘT PHẢI (MÃ VIETQR ĐỘNG) ──
            var pnlRightQR = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(249, 250, 251)
            };

            var pnlQrCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlQrCard.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(209, 213, 219), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlQrCard.Width - 1, pnlQrCard.Height - 1);
            };

            var lblQrTitle = new Label
            {
                Text = "QUÉT MÃ VIETQR ĐỂ THANH TOÁN 24/7",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = C_TealDark
            };

            var picQR = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            var lblQrStatus = new Label
            {
                Text = "Đang tải mã VietQR...",
                Dock = DockStyle.Bottom,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = F_Small,
                ForeColor = C_TextGray
            };

            var pnlQrButtons = new Panel { Dock = DockStyle.Bottom, Height = 34 };
            var btnReloadQR = SecondaryBtn("🔄 Tải lại QR", 0, 2, 110, 28, C_Blue);
            btnReloadQR.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReloadQR.Font = F_Small;
            pnlQrButtons.Controls.Add(btnReloadQR);

            var lblQrGuide = new Label
            {
                Text = "Quét bằng bất kỳ App Ngân hàng hoặc MoMo / ZaloPay",
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = C_TextGray
            };

            pnlQrCard.Controls.Add(picQR);
            pnlQrCard.Controls.Add(lblQrStatus);
            pnlQrCard.Controls.Add(pnlQrButtons);
            pnlQrCard.Controls.Add(lblQrGuide);
            pnlQrCard.Controls.Add(lblQrTitle);
            pnlRightQR.Controls.Add(pnlQrCard);

            dialog.Controls.Add(pnlRightQR);
            dialog.Controls.Add(pnlLeft);

            // Handler tải mã VietQR bất đồng bộ
            async void LoadQr()
            {
                lblQrStatus.Text = "⏳ Đang kết nối VietQR Napas...";
                lblQrStatus.ForeColor = C_Blue;
                btnReloadQR.Enabled = false;

                decimal currentAmount = paymentAmount.Value;
                var qrImg = await VietQRHelper.LoadQrImageAsync(bank.BankCode, bank.AccountNo, currentAmount, transferContent);
                if (qrImg != null)
                {
                    picQR.Image?.Dispose();
                    picQR.Image = qrImg;
                    lblQrStatus.Text = $"✓ Sẵn sàng nhận {currentAmount:N0} đ qua VietQR 24/7";
                    lblQrStatus.ForeColor = Color.FromArgb(22, 101, 52);
                }
                else
                {
                    picQR.Image?.Dispose();
                    picQR.Image = VietQRHelper.CreateFallbackCard(bank.BankName, bank.AccountNo, bank.AccountName, currentAmount, transferContent);
                    lblQrStatus.Text = "⚠ Không tải được ảnh online. Đã hiển thị thẻ thông tin CK.";
                    lblQrStatus.ForeColor = Color.FromArgb(180, 83, 9);
                }
                btnReloadQR.Enabled = true;
            }

            btnReloadQR.Click += (s, e) => LoadQr();

            paymentAmount.ValueChanged += (s, e) =>
            {
                if (method.SelectedItem?.ToString() == "Chuyển khoản QR")
                    LoadQr();
            };

            void UpdateMethodLayout()
            {
                bool isQR = method.SelectedItem?.ToString() == "Chuyển khoản QR";
                pnlRightQR.Visible = isQR;
                pnlBankInfo.Visible = isQR;
                if (isQR)
                {
                    dialog.ClientSize = new Size(880, 490);
                    LoadQr();
                }
                else
                {
                    dialog.ClientSize = new Size(460, 360);
                }
                dialog.Location = new Point(Math.Max(0, this.Left + (this.Width - dialog.Width) / 2), Math.Max(0, this.Top + (this.Height - dialog.Height) / 2));
            }

            btnChangeBank.LinkClicked += (s, e) =>
            {
                if (VietQRHelper.ShowBankConfigDialog(dialog))
                {
                    bank = VietQRHelper.GetConfig();
                    lblBName.Text = $"Ngân hàng: {bank.BankName}";
                    lblBAcc.Text = $"Số TK: {bank.AccountNo}";
                    lblBHolder.Text = $"Chủ TK: {bank.AccountName}";
                    LoadQr();
                }
            };

            method.SelectedIndexChanged += (s, e) => UpdateMethodLayout();
            UpdateMethodLayout();

            confirm.Click += (s, e) =>
            {
                try
                {
                    if (!ClinicDAL.ThanhToanHoaDon(
                            maHD,
                            method.SelectedItem?.ToString() ?? "Tiền mặt",
                            paymentAmount.Value))
                    {
                        MessageBox.Show("Hóa đơn đã được cập nhật hoặc không còn ở trạng thái chờ thanh toán.",
                            "Không thể thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    // ── Gửi email xác nhận thanh toán + thông báo bell ──
                    try
                    {
                        var hdInfo = ClinicDAL.LayThongTinHoaDonDaThanhToan(maHD);
                        if (!string.IsNullOrWhiteSpace(hdInfo.email))
                            Helpers.EmailHelper.SendAsync(
                                hdInfo.email,
                                $"[An Tâm] Hóa đơn #{maHD} đã thanh toán thành công",
                                Helpers.EmailHelper.BuildThanhToanBody(
                                    hdInfo.tenBN, maHD, paymentAmount.Value,
                                    method.SelectedItem?.ToString() ?? "Tiền mặt", DateTime.Now));

                        Helpers.NotificationHelper.Push(
                            SessionState.CurrentUser?.MaNV ?? "",
                            $"Thanh toán HĐ {maHD} thành công",
                            $"BN: {hdInfo.tenBN} | Số tiền: {paymentAmount.Value:N0} đ | {method.SelectedItem}",
                            "Thanh toán");
                        Helpers.NotificationHelper.PushToAdmins(
                            $"Hóa đơn {maHD} đã được thanh toán",
                            $"BN: {hdInfo.tenBN} | Số tiền: {paymentAmount.Value:N0} đ | Thu ngân: {SessionState.CurrentUser?.HoTenNhanVien}",
                            "Thanh toán");
                    }
                    catch { /* email/notification không được phá workflow chính */ }

                    // Chỉ cấp thuốc tự động khi hóa đơn được thanh toán đủ.
                    string pkCuaHoaDon = invoice.Table.Columns.Contains("MaPK") ? invoice["MaPK"]?.ToString() ?? "" : "";
                    if (!string.IsNullOrEmpty(pkCuaHoaDon))
                    {
                        try
                        {
                            var dtThuocCheck = ClinicDAL.LayChiTietDonThuocTheoPK(pkCuaHoaDon);
                            if (dtThuocCheck != null && dtThuocCheck.Rows.Count > 0)
                            {
                                if (paymentAmount.Value >= balanceDue)
                                {
                                    if (ClinicDAL.CapPhatDonThuoc(pkCuaHoaDon))
                                    {
                                        MessageBox.Show(
                                            $"Hóa đơn đã thanh toán đủ. Đã tự động cấp thuốc cho bệnh nhân {tenBN}; tồn kho đã được cập nhật theo lô.",
                                            "Cấp thuốc thành công",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Information);
                                    }
                                }
                                else
                                {
                                    MessageBox.Show(
                                        "Đã ghi nhận thanh toán một phần. Thuốc sẽ được tự động cấp và trừ kho khi hóa đơn được thanh toán đủ.",
                                        "Chưa cấp thuốc",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                                }
                            }
                        }
                        catch (Exception exIssue)
                        {
                            MessageBox.Show($"Thanh toán đã hoàn tất, nhưng phát sinh lưu ý cấp thuốc: {exIssue.Message}\r\nBạn có thể vào tab 'Kho thuốc' > 'Cấp đơn' để xử lý thủ công.",
                                "Thông báo cấp thuốc", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }

                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Lỗi thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dialog.AcceptButton = confirm;
            dialog.CancelButton = cancel;
            return dialog.ShowDialog(this) == DialogResult.OK;
        }

        private bool ShowAdvancePayment(DataRow ticket)
        {
            string maPK = ticket["MaPK"].ToString() ?? "";
            string tenBN = ticket["TenBN"]?.ToString() ?? "Bệnh nhân";
            var (tienDV, tienThuoc) = ClinicDAL.TinhChiPhiPhieuKham(maPK);
            decimal estimatedTotal = tienDV + tienThuoc;

            var bank = VietQRHelper.GetConfig();
            string transferContent = $"TAM UNG {maPK}";

            using var dialog = new Form
            {
                Text = $"Ghi nhận tạm ứng viện phí - Phiếu {maPK}",
                Size = new Size(880, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            // ── CỘT TRÁI (NHẬP LIỆU) ──
            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 440,
                Padding = new Padding(16, 12, 16, 12),
                BackColor = Color.White
            };

            var heading = new Label
            {
                Text = $"TẠM ỨNG: PHIẾU KHÁM #{maPK}",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = C_TextDark,
                Location = new Point(14, 10),
                AutoSize = true
            };
            var lblPatient = new Label
            {
                Text = $"Bệnh nhân: {tenBN}",
                Font = F_Base,
                ForeColor = C_TextGray,
                Location = new Point(14, 34),
                AutoSize = true
            };

            var pnlEst = new Panel
            {
                Location = new Point(14, 60),
                Size = new Size(405, 56),
                BackColor = Color.FromArgb(239, 246, 255)
            };
            pnlEst.Paint += (s, e) =>
            {
                using var p = new Pen(C_Blue, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlEst.Width - 1, pnlEst.Height - 1);
            };
            var lblEstTitle = new Label { Text = "CHI PHÍ TẠM TÍNH TỐI ĐA", Location = new Point(10, 6), AutoSize = true, Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Color.FromArgb(30, 64, 175) };
            var lblEstVal = new Label { Text = $"{estimatedTotal:N0} đ", Location = new Point(8, 22), AutoSize = true, Font = new Font("Segoe UI", 16f, FontStyle.Bold), ForeColor = C_Blue };
            pnlEst.Controls.AddRange(new Control[] { lblEstTitle, lblEstVal });

            var lblAmount = new Label { Text = "Số tiền tạm ứng thu đợt này (VNĐ):", Location = new Point(14, 124), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };
            var amount = new NumericUpDown
            {
                Location = new Point(14, 146),
                Size = new Size(405, 30),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Minimum = 1,
                Maximum = Math.Max(1, estimatedTotal),
                Value = Math.Max(1, estimatedTotal),
                DecimalPlaces = 0,
                ThousandsSeparator = true
            };

            var lblMethod = new Label { Text = "Phương thức tạm ứng:", Location = new Point(14, 184), AutoSize = true, Font = F_Bold, ForeColor = C_TextDark };
            var method = new ComboBox
            {
                Location = new Point(14, 206),
                Size = new Size(405, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = F_Base
            };
            method.Items.AddRange(new object[] { "Chuyển khoản QR", "Tiền mặt", "Thẻ" });
            method.SelectedIndex = 0;

            // Panel thông tin chuyển khoản ngân hàng
            var pnlBankInfo = new Panel
            {
                Location = new Point(14, 244),
                Size = new Size(405, 175),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            pnlBankInfo.Paint += (s, e) =>
            {
                using var p = new Pen(C_Border, 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlBankInfo.Width - 1, pnlBankInfo.Height - 1);
            };

            var lblBTitle = new Label { Text = "🏦 TÀI KHOẢN NHẬN TIỀN (PHÒNG KHÁM)", Location = new Point(10, 8), AutoSize = true, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = C_TealDark };
            var lblBName = new Label { Text = $"Ngân hàng: {bank.BankName}", Location = new Point(10, 28), Size = new Size(385, 18), AutoEllipsis = true, Font = F_Small };
            var lblBAcc = new Label { Text = $"Số TK: {bank.AccountNo}", Location = new Point(10, 48), Size = new Size(290, 18), AutoEllipsis = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = C_TextDark };
            var btnCopyAcc = new Button { Text = "📋 Chép STK", Location = new Point(305, 46), Size = new Size(90, 24), Font = new Font("Segoe UI", 7.5f), FlatStyle = FlatStyle.Flat };
            btnCopyAcc.Click += (s, e) => { Clipboard.SetText(bank.AccountNo); MessageBox.Show("Đã sao chép số tài khoản!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); };

            var lblBHolder = new Label { Text = $"Chủ TK: {bank.AccountName}", Location = new Point(10, 72), Size = new Size(385, 18), AutoEllipsis = true, Font = F_Small };

            var lblBContent = new Label { Text = $"Nội dung CK: {transferContent}", Location = new Point(10, 96), Size = new Size(290, 18), AutoEllipsis = true, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = C_Blue };
            var btnCopyContent = new Button { Text = "📋 Chép ND", Location = new Point(305, 94), Size = new Size(90, 24), Font = new Font("Segoe UI", 7.5f), FlatStyle = FlatStyle.Flat };
            btnCopyContent.Click += (s, e) => { Clipboard.SetText(transferContent); MessageBox.Show("Đã sao chép nội dung chuyển khoản!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information); };

            var btnChangeBank = new LinkLabel { Text = "⚙ Cấu hình thông tin STK ngân hàng phòng khám", Location = new Point(10, 124), AutoSize = true, Font = F_Small, LinkColor = C_Teal };

            pnlBankInfo.Controls.AddRange(new Control[] { lblBTitle, lblBName, lblBAcc, btnCopyAcc, lblBHolder, lblBContent, btnCopyContent, btnChangeBank });

            // Actions
            var pnlActions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 14, 8)
            };
            var save = PrimaryBtn("Ghi nhận tạm ứng", 0, 0, 160, 36);
            var cancel = new Button { Text = "Đóng", Width = 90, Height = 36, DialogResult = DialogResult.Cancel, BackColor = Color.White, FlatStyle = FlatStyle.Flat, Font = F_Base };
            pnlActions.Controls.AddRange(new Control[] { save, cancel });

            pnlLeft.Controls.AddRange(new Control[] { heading, lblPatient, pnlEst, lblAmount, amount, lblMethod, method, pnlBankInfo, pnlActions });

            // ── CỘT PHẢI (MÃ VIETQR ĐỘNG) ──
            var pnlRightQR = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                BackColor = Color.FromArgb(249, 250, 251)
            };

            var pnlQrCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(12)
            };
            pnlQrCard.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(209, 213, 219), 1);
                e.Graphics.DrawRectangle(p, 0, 0, pnlQrCard.Width - 1, pnlQrCard.Height - 1);
            };

            var lblQrTitle = new Label
            {
                Text = "QUÉT MÃ VIETQR TẠM ỨNG 24/7",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = C_TealDark
            };

            var picQR = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            var lblQrStatus = new Label
            {
                Text = "Đang tải mã VietQR...",
                Dock = DockStyle.Bottom,
                Height = 26,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = F_Small,
                ForeColor = C_TextGray
            };

            var pnlQrButtons = new Panel { Dock = DockStyle.Bottom, Height = 34 };
            var btnReloadQR = SecondaryBtn("🔄 Tải lại QR", 0, 2, 110, 28, C_Blue);
            btnReloadQR.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnReloadQR.Font = F_Small;
            pnlQrButtons.Controls.Add(btnReloadQR);

            var lblQrGuide = new Label
            {
                Text = "Quét bằng bất kỳ App Ngân hàng hoặc MoMo / ZaloPay",
                Dock = DockStyle.Bottom,
                Height = 22,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = C_TextGray
            };

            pnlQrCard.Controls.Add(picQR);
            pnlQrCard.Controls.Add(lblQrStatus);
            pnlQrCard.Controls.Add(pnlQrButtons);
            pnlQrCard.Controls.Add(lblQrGuide);
            pnlQrCard.Controls.Add(lblQrTitle);
            pnlRightQR.Controls.Add(pnlQrCard);

            dialog.Controls.Add(pnlRightQR);
            dialog.Controls.Add(pnlLeft);

            async void LoadQr()
            {
                lblQrStatus.Text = "⏳ Đang kết nối VietQR Napas...";
                lblQrStatus.ForeColor = C_Blue;
                btnReloadQR.Enabled = false;

                decimal currentAmount = amount.Value;
                var qrImg = await VietQRHelper.LoadQrImageAsync(bank.BankCode, bank.AccountNo, currentAmount, transferContent);
                if (qrImg != null)
                {
                    picQR.Image?.Dispose();
                    picQR.Image = qrImg;
                    lblQrStatus.Text = $"✓ Sẵn sàng nhận {currentAmount:N0} đ qua VietQR 24/7";
                    lblQrStatus.ForeColor = Color.FromArgb(22, 101, 52);
                }
                else
                {
                    picQR.Image?.Dispose();
                    picQR.Image = VietQRHelper.CreateFallbackCard(bank.BankName, bank.AccountNo, bank.AccountName, currentAmount, transferContent);
                    lblQrStatus.Text = "⚠ Không tải được ảnh online. Đã hiển thị thẻ thông tin CK.";
                    lblQrStatus.ForeColor = Color.FromArgb(180, 83, 9);
                }
                btnReloadQR.Enabled = true;
            }

            btnReloadQR.Click += (s, e) => LoadQr();

            amount.ValueChanged += (s, e) =>
            {
                if (method.SelectedItem?.ToString() == "Chuyển khoản QR")
                    LoadQr();
            };

            void UpdateMethodLayout()
            {
                bool isQR = method.SelectedItem?.ToString() == "Chuyển khoản QR";
                pnlRightQR.Visible = isQR;
                pnlBankInfo.Visible = isQR;
                if (isQR)
                {
                    dialog.ClientSize = new Size(880, 490);
                    LoadQr();
                }
                else
                {
                    dialog.ClientSize = new Size(460, 360);
                }
                dialog.Location = new Point(Math.Max(0, this.Left + (this.Width - dialog.Width) / 2), Math.Max(0, this.Top + (this.Height - dialog.Height) / 2));
            }

            btnChangeBank.LinkClicked += (s, e) =>
            {
                if (VietQRHelper.ShowBankConfigDialog(dialog))
                {
                    bank = VietQRHelper.GetConfig();
                    lblBName.Text = $"Ngân hàng: {bank.BankName}";
                    lblBAcc.Text = $"Số TK: {bank.AccountNo}";
                    lblBHolder.Text = $"Chủ TK: {bank.AccountName}";
                    LoadQr();
                }
            };

            method.SelectedIndexChanged += (s, e) => UpdateMethodLayout();
            UpdateMethodLayout();

            save.Click += (sender, args) =>
            {
                try
                {
                    ClinicDAL.TamUngPhieuKham(maPK, amount.Value, method.SelectedItem?.ToString() ?? "Tiền mặt");
                    dialog.DialogResult = DialogResult.OK;
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể ghi nhận tạm ứng",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            dialog.AcceptButton = save;
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
                    Size = new Size(460, 340),
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
                var insuranceRate = new NumericUpDown
                {
                    Minimum = 0,
                    Maximum = 100,
                    DecimalPlaces = 2,
                    Increment = 5,
                    Width = 230,
                    Font = F_Base,
                    Anchor = AnchorStyles.None
                };
                var insurancePanel = new Panel { Dock = DockStyle.Top, Height = 48 };
                insurancePanel.Controls.Add(new Label
                {
                    Text = "Bảo hiểm (%)",
                    Location = new Point(32, 13),
                    AutoSize = true,
                    Font = F_Base
                });
                insurancePanel.Controls.Add(insuranceRate);
                insurancePanel.Resize += (s, e) =>
                {
                    int start = (insurancePanel.ClientSize.Width - 340) / 2;
                    insurancePanel.Controls[0].Location = new Point(start, 13);
                    insuranceRate.Location = new Point(start + 110, 8);
                };
                var paymentNote = new Label
                {
                    Text = "Hình thức thanh toán sẽ chọn tại bước thu ngân.",
                    Dock = DockStyle.Top,
                    Height = 40,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = F_Small,
                    ForeColor = C_TextGray
                };
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
                            HinhThucTT = "Chưa chọn",
                            TrangThai = "Chưa thanh toán"
                        };
                        if (!ClinicDAL.TaoHoaDon(invoice, insuranceRate.Value))
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
                dialog.Controls.Add(paymentNote);
                dialog.Controls.Add(insurancePanel);
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

        private void ShowRefundRequests()
        {
            if (!SessionState.IsAdmin)
                throw new UnauthorizedAccessException("Chỉ quản trị viên được xử lý yêu cầu hoàn tiền.");
            using var dialog = new Form
            {
                Text = "Duyệt yêu cầu hoàn tiền",
                Size = new Size(900, 520),
                MinimumSize = new Size(720, 420),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White
            };
            var title = new Label
            {
                Text = "YÊU CẦU HOÀN TIỀN",
                Dock = DockStyle.Top,
                Height = 48,
                Padding = new Padding(14, 14, 0, 0),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
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
            var reject = SecondaryBtn("Từ chối", 0, 0, 110, 32, C_Red);
            var approve = PrimaryBtn("Duyệt hoàn tiền", 0, 0, 140, 32);
            actions.Controls.AddRange(new Control[] { close, reject, approve });

            void LoadRequests()
            {
                grid.DataSource = ClinicDAL.LayYeuCauHoanTien();
                if (grid.Columns.Contains("SoTien"))
                    grid.Columns["SoTien"]!.DefaultCellStyle.Format = "N0";
                if (grid.Columns.Contains("NgayDeNghi"))
                    grid.Columns["NgayDeNghi"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
                if (grid.Columns.Contains("NgayDuyet"))
                    grid.Columns["NgayDuyet"]!.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }

            long? SelectedRequestId()
            {
                if (grid.SelectedRows.Count == 0
                    || grid.SelectedRows[0].DataBoundItem is not DataRowView selected
                    || selected.Row["TrangThai"].ToString() != "Chờ duyệt")
                    return null;
                return Convert.ToInt64(selected.Row["MaYeuCau"]);
            }

            void ProcessRequest(bool approveRequest)
            {
                long? id = SelectedRequestId();
                if (!id.HasValue)
                {
                    MessageBox.Show("Chọn yêu cầu đang chờ duyệt.", "Chưa chọn yêu cầu",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string note = Microsoft.VisualBasic.Interaction.InputBox(
                    approveRequest ? "Ghi chú phê duyệt (nếu có):" : "Nhập lý do từ chối:",
                    approveRequest ? "Duyệt hoàn tiền" : "Từ chối hoàn tiền", "");
                if (!approveRequest && string.IsNullOrWhiteSpace(note))
                {
                    MessageBox.Show("Cần ghi lý do từ chối.", "Thiếu lý do",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                try
                {
                    ClinicDAL.XuLyYeuCauHoanTien(id.Value, approveRequest, note);
                    LoadRequests();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Không thể xử lý yêu cầu",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            approve.Click += (sender, args) => ProcessRequest(true);
            reject.Click += (sender, args) => ProcessRequest(false);
            dialog.Controls.Add(grid);
            dialog.Controls.Add(actions);
            dialog.Controls.Add(title);
            dialog.CancelButton = close;
            LoadRequests();
            dialog.ShowDialog(this);
        }

        private void ShowThuNganViewLegacy()
        {
            SetRefreshPageAction(ShowThuNganView);
            SetActiveNav(btnNavThuNgan);
            SetPageHeader("Y tá  ›  Thu ngân", "Tính viện phí, lập hóa đơn & xuất biên lai");
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
            cboHT.Items.AddRange(new object[] { "Tiền mặt", "Chuyển khoản QR", "Thẻ" });
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
                    MaNV_ThuNgan= SessionState.CurrentUser?.MaNV ?? "",
                    TienDichVu  = curDV,
                    TienThuoc   = curThuoc,
                    GiamGia     = numGG.Value,
                    TongTien    = curDV + curThuoc - numGG.Value,
                    HinhThucTT  = cboHT.SelectedItem?.ToString() ?? "Tiền mặt",
                    TrangThai   = "Đã thanh toán"
                };
                try
                {
                    if (string.IsNullOrWhiteSpace(hd.MaNV_ThuNgan))
                        throw new InvalidOperationException("Không xác định được nhân viên thu ngân đang đăng nhập.");
                    if (!ClinicDAL.TaoHoaDon(hd))
                        throw new InvalidOperationException("Phiếu khám đã được cập nhật, chưa hoàn tất CLS, hoặc mức giảm giá không hợp lệ.");
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
