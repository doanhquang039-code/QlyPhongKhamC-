using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public partial class FormMain
    {
        private void ShowEmployeeShiftView()
        {
            string maNV = SessionState.CurrentUser?.MaNV ?? "";
            DataTable shifts = SessionState.IsDoctor
                ? ClinicDAL.LayLichTrucBacSi(maNV)
                : ClinicDAL.LayLichTrucYTa(maNV);
            string[] dayNames =
            {
                UiText.Translate("Thứ 2"), UiText.Translate("Thứ 3"), UiText.Translate("Thứ 4"),
                UiText.Translate("Thứ 5"), UiText.Translate("Thứ 6"), UiText.Translate("Thứ 7"),
                UiText.Translate("Chủ nhật")
            };

            panelContent.Controls.Clear();
            SetPageHeader(UiText.Translate("Lịch trực"), UiText.Translate("Lịch trực tuần"));
            SetActiveNav(btnNavLichTruc);

            var container = new Panel { Dock = DockStyle.Fill, BackColor = C_Bg };
            var intro = new Label
            {
                Text = UiText.Translate("Lịch làm việc được phân công trong tuần."),
                Dock = DockStyle.Top,
                Height = 44,
                Font = F_Base,
                ForeColor = C_TextGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = F_Base
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Day",
                HeaderText = UiText.Translate("Ngày trong tuần"),
                FillWeight = 35
            });
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Hours",
                HeaderText = UiText.Translate("Giờ trực"),
                FillWeight = 65
            });

            for (int day = 1; day <= 7; day++)
            {
                string hours = string.Join(", ", shifts.AsEnumerable()
                    .Where(row => Convert.ToInt32(row["Thu"]) == day)
                    .Select(row => $"{((TimeSpan)row["GioBatDau"]).ToString(@"hh\:mm")} - {((TimeSpan)row["GioKetThuc"]).ToString(@"hh\:mm")}"));
                grid.Rows.Add(dayNames[day - 1], string.IsNullOrEmpty(hours)
                    ? UiText.Translate("Chưa được phân ca")
                    : hours);
            }

            container.Controls.Add(grid);
            container.Controls.Add(intro);
            panelContent.Controls.Add(container);
        }
    }
}
