using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public class FormLichSuKham : Form
    {
        public FormLichSuKham(string maBN, string tenBN)
        {
            Text = $"Lịch sử khám bệnh - {tenBN} ({maBN})";
            Size = new Size(920, 520);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.5F);
            BackColor = Color.FromArgb(244, 246, 250);

            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.White, Padding = new Padding(16, 12, 16, 0) };
            pnlTop.Paint += (s, e) => { using var pen = new Pen(Color.FromArgb(229, 231, 235), 1); e.Graphics.DrawLine(pen, 0, 49, pnlTop.Width, 49); };
            pnlTop.Controls.Add(new Label { Text = $"📋 LỊCH SỬ CÁC LẦN KHÁM CỦA BỆNH NHÂN: {tenBN.ToUpper()}", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Color.FromArgb(15, 32, 54), AutoSize = true, Location = new Point(16, 14) });

            var pnlGrid = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            var dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = true,
                BackgroundColor = Color.White,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                BorderStyle = BorderStyle.None
            };
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            dgv.ColumnHeadersHeight = 36;
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowTemplate.Height = 34;
            dgv.DataSource = ClinicDAL.LayLichSuKhamBenhNhan(maBN);

            pnlGrid.Controls.Add(dgv);
            Controls.AddRange(new Control[] { pnlGrid, pnlTop });
        }
    }
}
