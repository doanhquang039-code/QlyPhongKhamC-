using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public sealed class FormDieuChinhKho : Form
    {
        public FormDieuChinhKho(string maThuoc, DataTable lots)
        {
            Text="Kiểm kê / điều chỉnh lô"; StartPosition=FormStartPosition.CenterParent;
            FormBorderStyle=FormBorderStyle.FixedDialog; MaximizeBox=MinimizeBox=false; ClientSize=new Size(520,250); Font=new Font("Segoe UI",9F);
            var lot=new ComboBox { Location=new Point(170,20),Width=325,DropDownStyle=ComboBoxStyle.DropDownList,DisplayMember="HienThi",ValueMember="MaLo" };
            var view=lots.Copy(); view.Columns.Add("HienThi",typeof(string)); foreach(DataRow row in view.Rows) row["HienThi"]=$"Lô {row["SoLo"]} | HSD {Convert.ToDateTime(row["NgayHetHan"]):dd/MM/yyyy} | còn {row["SoLuongCon"]}"; lot.DataSource=view;
            var delta=new NumericUpDown { Location=new Point(170,65),Width=150,Minimum=-1000000,Maximum=1000000,Value=1 };
            var reason=new TextBox { Location=new Point(170,110),Width=325,Height=55,Multiline=true,MaxLength=255 };
            Controls.AddRange(new Control[]{new Label{Text=$"Thuốc: {maThuoc}",Location=new Point(18,24),AutoSize=true},lot,new Label{Text="Thay đổi (+/-):",Location=new Point(18,69),AutoSize=true},delta,new Label{Text="Lý do:",Location=new Point(18,114),AutoSize=true},reason});
            var save=new Button{Text="Lưu điều chỉnh",Location=new Point(360,195),Size=new Size(135,32)}; var cancel=new Button{Text="Đóng",Location=new Point(270,195),Size=new Size(78,32),DialogResult=DialogResult.Cancel};
            save.Click+=(_,__)=>{try{if(lot.SelectedValue==null)throw new InvalidOperationException("Chọn lô thuốc.");ClinicDAL.DieuChinhLoThuoc(Convert.ToInt32(lot.SelectedValue),(int)delta.Value,reason.Text);DialogResult=DialogResult.OK;Close();}catch(Exception ex){MessageBox.Show(ex.Message,"Không thể điều chỉnh",MessageBoxButtons.OK,MessageBoxIcon.Error);}};
            Controls.AddRange(new Control[]{cancel,save}); AcceptButton=save;CancelButton=cancel;
        }
    }
}
