using System;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public sealed class FormNhapLoThuoc : Form
    {
        public FormNhapLoThuoc(string maThuoc, string tenThuoc)
        {
            Text = "Nhập thuốc theo lô"; StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = MinimizeBox = false;
            ClientSize = new Size(430, 340); Font = new Font("Segoe UI", 9F);
            var supplier = Field("Nhà cung cấp", 18, 18, "");
            var lot = Field("Số lô", 18, 66, "");
            var expiry = new DateTimePicker { Location = new Point(160, 114), Width = 245, Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddMonths(6) };
            var qty = new NumericUpDown { Location = new Point(160, 162), Width = 245, Minimum = 1, Maximum = 1000000, Value = 1 };
            var cost = new NumericUpDown { Location = new Point(160, 210), Width = 245, Minimum = 0, Maximum = 1000000000, DecimalPlaces = 2, ThousandsSeparator = true };
            var note = Field("Ghi chú", 18, 258, "");
            Controls.Add(new Label { Text = $"Thuốc: {tenThuoc} ({maThuoc})", Location = new Point(18, 4), AutoSize = true, Font = new Font(Font, FontStyle.Bold) });
            Controls.AddRange(new Control[] { LabelAt("Nhà cung cấp",18),supplier,LabelAt("Số lô",66),lot,LabelAt("Hạn dùng",114),expiry,LabelAt("Số lượng",162),qty,LabelAt("Giá nhập / đơn vị",210),cost,LabelAt("Ghi chú",258),note });
            var save = new Button { Text = "Lưu phiếu nhập", Location = new Point(270, 298), Size = new Size(135, 30), DialogResult = DialogResult.None };
            var cancel = new Button { Text = "Đóng", Location = new Point(180, 298), Size = new Size(78, 30), DialogResult = DialogResult.Cancel };
            save.Click += (_,__) => { try { ClinicDAL.NhapLoThuoc(maThuoc,supplier.Text,lot.Text,expiry.Value,(int)qty.Value,cost.Value,note.Text); DialogResult=DialogResult.OK; Close(); } catch(Exception ex) { MessageBox.Show(ex.Message,"Không thể nhập kho",MessageBoxButtons.OK,MessageBoxIcon.Error); } };
            Controls.AddRange(new Control[] { cancel,save }); AcceptButton=save; CancelButton=cancel;
        }
        private static TextBox Field(string label,int x,int y,string value) => new TextBox { Location=new Point(x+142,y),Width=263,Text=value };
        private static Label LabelAt(string text,int y) => new Label { Text=text,Location=new Point(18,y+4),AutoSize=true };
    }
}
