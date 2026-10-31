using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ClosedXML.Excel;

namespace QlyPhongKham.Helpers
{
    public static class ExcelHelper
    {
        /// <summary>
        /// Xuất dữ liệu từ DataGridView hoặc DataTable ra file Excel (.xlsx) với định dạng chuẩn đẹp
        /// </summary>
        public static bool ExportToExcel(DataTable dataTable, string sheetName, string title, string defaultFileName)
        {
            if (dataTable == null || dataTable.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất file Excel!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = defaultFileName
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return false;

            try
            {
                using var workbook = new XLWorkbook();
                var ws = workbook.Worksheets.Add(sheetName);

                // Title
                int colCount = dataTable.Columns.Count;
                var titleRange = ws.Range(1, 1, 1, Math.Max(colCount, 1));
                titleRange.Merge();
                titleRange.Value = title;
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontSize = 16;
                titleRange.Style.Font.FontColor = XLColor.FromHtml("#005F73");
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                ws.Row(1).Height = 35;

                // Subtitle / Date
                var subRange = ws.Range(2, 1, 2, Math.Max(colCount, 1));
                subRange.Merge();
                subRange.Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Người xuất: {SessionState.CurrentUser?.HoTenNhanVien ?? "Hệ thống"}";
                subRange.Style.Font.Italic = true;
                subRange.Style.Font.FontSize = 10;
                subRange.Style.Font.FontColor = XLColor.Gray;
                subRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Row(2).Height = 20;

                int startRow = 4;

                // Headers
                for (int c = 0; c < colCount; c++)
                {
                    var cell = ws.Cell(startRow, c + 1);
                    cell.Value = dataTable.Columns[c].ColumnName;
                    cell.Style.Font.Bold = true;
                    cell.Style.Font.FontColor = XLColor.White;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0A50A0");
                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.LightGray;
                }
                ws.Row(startRow).Height = 26;

                // Data Rows
                for (int r = 0; r < dataTable.Rows.Count; r++)
                {
                    int currentRow = startRow + 1 + r;
                    for (int c = 0; c < colCount; c++)
                    {
                        var cell = ws.Cell(currentRow, c + 1);
                        object val = dataTable.Rows[r][c];
                        if (val == DBNull.Value || val == null)
                        {
                            cell.Value = "";
                        }
                        else if (val is decimal || val is double || val is float)
                        {
                            cell.Value = Convert.ToDouble(val);
                            cell.Style.NumberFormat.Format = "#,##0";
                        }
                        else if (val is int || val is long)
                        {
                            cell.Value = Convert.ToInt64(val);
                        }
                        else if (val is DateTime dt)
                        {
                            cell.Value = dt.ToString("dd/MM/yyyy HH:mm");
                        }
                        else
                        {
                            cell.Value = val.ToString();
                        }

                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E2E8F0");
                        if (r % 2 == 1)
                        {
                            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                        }
                    }
                    ws.Row(currentRow).Height = 20;
                }

                ws.Columns().AdjustToContents();
                workbook.SaveAs(sfd.FileName);

                var choice = MessageBox.Show(
                    "Xuất file Excel thành công!\nBạn có muốn mở file ngay bây giờ không?",
                    "Thành công",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (choice == DialogResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = sfd.FileName,
                        UseShellExecute = true
                    });
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xuất file Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Đọc file Excel (.xlsx) trả về DataTable
        /// </summary>
        public static DataTable? ImportFromExcel(string? defaultTitle = null)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                Title = defaultTitle ?? "Chọn file Excel cần nhập"
            };

            if (ofd.ShowDialog() != DialogResult.OK)
                return null;

            try
            {
                using var workbook = new XLWorkbook(ofd.FileName);
                var ws = workbook.Worksheets.FirstOrDefault();
                if (ws == null)
                {
                    MessageBox.Show("File Excel không có trang tính nào!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                var range = ws.RangeUsed();
                if (range == null)
                {
                    MessageBox.Show("File Excel rỗng!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                var dt = new DataTable();
                var firstRow = range.FirstRow();
                foreach (var cell in firstRow.Cells())
                {
                    string colName = cell.GetString().Trim();
                    if (string.IsNullOrEmpty(colName))
                        colName = $"Cột_{cell.Address.ColumnNumber}";
                    dt.Columns.Add(colName);
                }

                foreach (var row in range.RowsUsed().Skip(1))
                {
                    var dataRow = dt.NewRow();
                    int cIdx = 0;
                    foreach (var cell in row.Cells(1, dt.Columns.Count))
                    {
                        dataRow[cIdx++] = cell.Value.ToString();
                    }
                    dt.Rows.Add(dataRow);
                }

                return dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đọc file Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
    }
}

