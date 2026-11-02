using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using QlyPhongKham.DAL;

namespace QlyPhongKham.Forms
{
    public sealed class FormCapPhatThuoc : Form
    {
        private readonly DataGridView grid = new() { Dock=DockStyle.Fill, ReadOnly=true, AllowUserToAddRows=false, SelectionMode=DataGridViewSelectionMode.FullRowSelect, AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill };
        public FormCapPhatThuoc(DataTable pending)
        {
            Text="Đơn thuốc chờ cấp phát"; StartPosition=FormStartPosition.CenterParent; Size=new Size(850,480); MinimumSize=new Size(700,360);
            grid.DataSource=pending; Controls.Add(grid);
            var bar=new Panel { Dock=DockStyle.Bottom,Height=52 };
            var issue=new Button { Text="Cấp thuốc (FEFO)",Dock=DockStyle.Right,Width=150 };
            var outside=new Button { Text="Bệnh nhân mua ngoài",Dock=DockStyle.Right,Width=155 };
            var skipped=new Button { Text="Không lấy thuốc",Dock=DockStyle.Right,Width=140 };
            bar.Controls.AddRange(new Control[]{issue,outside,skipped}); Controls.Add(bar);
            issue.Click+=(_,__)=>Run("issue"); outside.Click+=(_,__)=>Run("Mua ngoài"); skipped.Click+=(_,__)=>Run("Không lấy");
        }
        private string SelectedPK() => grid.SelectedRows.Count==0 ? "" : grid.SelectedRows[0].Cells["MaPK"].Value?.ToString()??"";
        private void Run(string action)
        {
            string pk=SelectedPK(); if(string.IsNullOrWhiteSpace(pk)){MessageBox.Show("Chọn đơn thuốc cần xử lý.");return;}
            try
            {
                if(action=="issue") ClinicDAL.CapPhatDonThuoc(pk);
                else { string reason=Microsoft.VisualBasic.Interaction.InputBox("Nhập lý do / ghi chú:","Xử lý đơn thuốc",action=="Mua ngoài"?"Bệnh nhân mua ngoài":"Bệnh nhân không lấy thuốc"); if(string.IsNullOrWhiteSpace(reason))return; ClinicDAL.DanhDauDonThuocKhongCap(pk,action,reason); }
                MessageBox.Show("Đã cập nhật trạng thái cấp phát.","Thành công",MessageBoxButtons.OK,MessageBoxIcon.Information); DialogResult=DialogResult.OK; Close();
            }
            catch(Exception ex){MessageBox.Show(ex.Message,"Không thể xử lý đơn",MessageBoxButtons.OK,MessageBoxIcon.Error);}
        }
    }
}
