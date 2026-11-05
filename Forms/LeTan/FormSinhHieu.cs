using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.Models;

namespace QlyPhongKham.Forms
{
    internal sealed class FormSinhHieu : Form
    {
        private readonly NumericUpDown _huyetApTamThu = Number(40, 300, 120, 0);
        private readonly NumericUpDown _huyetApTamTruong = Number(20, 200, 80, 0);
        private readonly NumericUpDown _mach = Number(20, 250, 72, 0);
        private readonly NumericUpDown _nhietDo = Number(25, 45, 36.5m, 1);
        private readonly NumericUpDown _spo2 = Number(50, 100, 98, 0);
        private readonly NumericUpDown _chieuCao = Number(30, 250, 165, 1);
        private readonly NumericUpDown _canNang = Number(1, 400, 60, 1);
        private readonly Label _bmi = new();

        public string SinhHieuText { get; private set; } = "";
        public SinhHieu? DuLieuSinhHieu { get; private set; }

        public FormSinhHieu()
        {
            Text = "Ghi nhận sinh hiệu trước khám";
            ClientSize = new Size(600, 390);
            MinimumSize = new Size(560, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            var title = new Label
            {
                Text = "SINH HIỆU TIỀN KHÁM",
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(18, 14, 0, 0),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 32, 54)
            };
            var fields = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(18, 10, 18, 10)
            };
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            for (int row = 0; row < 4; row++)
                fields.RowStyles.Add(new RowStyle(SizeType.Percent, 25));

            fields.Controls.Add(CreateField("Huyết áp (mmHg)", CreateBloodPressureInput()), 0, 0);
            fields.Controls.Add(CreateField("Mạch (lần/phút)", _mach), 1, 0);
            fields.Controls.Add(CreateField("Nhiệt độ (°C)", _nhietDo), 0, 1);
            fields.Controls.Add(CreateField("SpO₂ (%)", _spo2), 1, 1);
            fields.Controls.Add(CreateField("Chiều cao (cm)", _chieuCao), 0, 2);
            fields.Controls.Add(CreateField("Cân nặng (kg)", _canNang), 1, 2);
            _bmi.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _bmi.ForeColor = Color.FromArgb(13, 148, 136);
            fields.Controls.Add(CreateField("BMI (tự tính)", _bmi), 0, 3);

            foreach (NumericUpDown input in new[] { _chieuCao, _canNang })
                input.ValueChanged += (sender, args) => UpdateBmi();
            UpdateBmi();

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(12)
            };
            var save = new Button
            {
                Text = "Lưu & check-in",
                Width = 140,
                Height = 34,
                BackColor = Color.FromArgb(0, 180, 166),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += (sender, args) =>
            {
                DuLieuSinhHieu = new SinhHieu
                {
                    HuyetApTamThu = _huyetApTamThu.Value,
                    HuyetApTamTruong = _huyetApTamTruong.Value,
                    Mach = _mach.Value,
                    NhietDo = _nhietDo.Value,
                    Spo2 = _spo2.Value,
                    ChieuCao = _chieuCao.Value,
                    CanNang = _canNang.Value
                };
                SinhHieuText = DuLieuSinhHieu.ToSummary();
                DialogResult = DialogResult.OK;
                Close();
            };
            var cancel = new Button { Text = "Hủy", Width = 90, Height = 34, DialogResult = DialogResult.Cancel };
            actions.Controls.AddRange(new Control[] { save, cancel });
            Controls.Add(fields);
            Controls.Add(actions);
            Controls.Add(title);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private static NumericUpDown Number(decimal minimum, decimal maximum, decimal value, int decimals)
            => new()
            {
                Minimum = minimum,
                Maximum = maximum,
                Value = value,
                DecimalPlaces = decimals,
                Increment = decimals == 0 ? 1 : 0.1m,
                Width = 120,
                Font = new Font("Segoe UI", 10F)
            };

        private static Control CreateField(string title, Control input)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            panel.Controls.Add(new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105)
            });
            input.Dock = DockStyle.Top;
            input.Margin = new Padding(0, 4, 0, 0);
            panel.Controls.Add(input);
            return panel;
        }

        private Control CreateBloodPressureInput()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                WrapContents = false,
                Margin = Padding.Empty
            };
            _huyetApTamThu.Width = 82;
            _huyetApTamTruong.Width = 82;
            panel.Controls.AddRange(new Control[]
            {
                _huyetApTamThu,
                new Label { Text = "/", AutoSize = true, Padding = new Padding(0, 6, 0, 0) },
                _huyetApTamTruong
            });
            return panel;
        }

        private decimal CalculateBmi()
        {
            decimal heightMeters = _chieuCao.Value / 100m;
            return _canNang.Value / (heightMeters * heightMeters);
        }

        private void UpdateBmi() => _bmi.Text = CalculateBmi().ToString(
            "0.0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
