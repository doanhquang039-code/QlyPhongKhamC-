using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace QlyPhongKham.Helpers
{
    internal sealed class GridPageController
    {
        private readonly DataGridView _grid;
        private readonly Func<int, int, DataTable> _loadPage;
        private readonly TableLayoutPanel _layout;

        private readonly Label _pageLabel;
        private readonly Button _previousButton;
        private readonly Button _nextButton;
        private int _pageIndex;
        private int _pageSize = 10;
        private int _totalCount;

        private GridPageController(
            DataGridView grid,
            Func<int, int, DataTable> loadPage,
            TableLayoutPanel layout,
            Label pageLabel,
            Button previousButton,
            Button nextButton)
        {
            _grid = grid;
            _loadPage = loadPage;
            _layout = layout;
            _pageLabel = pageLabel;
            _previousButton = previousButton;
            _nextButton = nextButton;

            _grid.SizeChanged += GridSizeChanged;
            _previousButton.Click += (sender, args) =>
            {
                if (_pageIndex == 0) return;
                _pageIndex--;
                LoadCurrentPage();
            };
            _nextButton.Click += (sender, args) =>
            {
                if ((_pageIndex + 1) * _pageSize >= _totalCount) return;
                _pageIndex++;
                LoadCurrentPage();
            };

            _layout.SizeChanged += (sender, args) => UpdatePageSize();
            UpdatePageSize(refresh: false);
            LoadCurrentPage();
        }

        internal static GridPageController Attach(DataGridView grid, Func<int, int, DataTable> loadPage)
        {
            ArgumentNullException.ThrowIfNull(grid);
            ArgumentNullException.ThrowIfNull(loadPage);
            Control parent = grid.Parent ?? throw new InvalidOperationException("The grid must be added to a parent before paging is attached.");

            var previousButton = new Button
            {
                Text = "‹ Trước",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 32, 54),
                Font = new Font("Segoe UI", 9F),
                Padding = new Padding(8, 3, 8, 3),
                Margin = new Padding(0, 3, 8, 3)
            };
            var nextButton = new Button
            {
                Text = "Sau ›",
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 32, 54),
                Font = new Font("Segoe UI", 9F),
                Padding = new Padding(8, 3, 8, 3),
                Margin = new Padding(8, 3, 0, 3)
            };
            var pageLabel = new Label
            {
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(107, 114, 128),
                Margin = new Padding(4, 9, 4, 0)
            };
            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Padding = new Padding(8, 0, 8, 0),
                BackColor = Color.FromArgb(248, 250, 252)
            };
            footer.Controls.Add(nextButton);
            footer.Controls.Add(pageLabel);
            footer.Controls.Add(previousButton);


            var layout = new TableLayoutPanel
            {
                Dock = grid.Dock,
                Location = grid.Location,
                Size = grid.Size,
                Anchor = grid.Anchor,
                Margin = grid.Margin,
                Padding = Padding.Empty,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));

            int childIndex = parent.Controls.GetChildIndex(grid);
            parent.Controls.Remove(grid);
            grid.Dock = DockStyle.Fill;
            grid.Margin = Padding.Empty;
            layout.Controls.Add(grid, 0, 0);
            layout.Controls.Add(footer, 0, 1);
            parent.Controls.Add(layout);
            parent.Controls.SetChildIndex(layout, Math.Min(childIndex, parent.Controls.Count - 1));

            return new GridPageController(grid, loadPage, layout, pageLabel, previousButton, nextButton);
        }

        internal void Reload(bool resetPage = true)
        {
            if (resetPage) _pageIndex = 0;
            LoadCurrentPage();
        }

        private void GridSizeChanged(object? sender, EventArgs e) => UpdatePageSize();

        private void UpdatePageSize(bool refresh = true)
        {
            int rowHeight = Math.Max(1, _grid.RowTemplate.Height);
            int availableHeight = Math.Max(rowHeight * 3, _grid.ClientSize.Height - _grid.ColumnHeadersHeight);
            int newPageSize = Math.Clamp(availableHeight / rowHeight, 3, 100);
            if (newPageSize == _pageSize) return;

            int firstVisibleRow = _pageIndex * _pageSize;
            _pageSize = newPageSize;
            _pageIndex = firstVisibleRow / _pageSize;
            if (refresh)
                LoadCurrentPage();
        }

        private void LoadCurrentPage()
        {
            DataTable rows = _loadPage(_pageIndex * _pageSize, _pageSize);
            if (rows.Rows.Count == 0 && _pageIndex > 0)
            {
                _pageIndex = 0;
                rows = _loadPage(0, _pageSize);
            }
            _totalCount = rows.ExtendedProperties["TotalCount"] is int count ? count : rows.Rows.Count;

            if (_totalCount > 0 && _pageIndex * _pageSize >= _totalCount)
            {
                _pageIndex = (_totalCount - 1) / _pageSize;
                rows = _loadPage(_pageIndex * _pageSize, _pageSize);
                _totalCount = rows.ExtendedProperties["TotalCount"] is int adjustedCount
                    ? adjustedCount
                    : rows.Rows.Count;
            }

            _grid.DataSource = rows;
            int firstRow = _totalCount == 0 ? 0 : _pageIndex * _pageSize + 1;
            int lastRow = Math.Min((_pageIndex + 1) * _pageSize, _totalCount);
            int pageCount = Math.Max(1, (int)Math.Ceiling(_totalCount / (double)_pageSize));
            _pageLabel.Text = _totalCount == 0
                ? "Không có dữ liệu"
                : $"Trang {_pageIndex + 1}/{pageCount}  •  {firstRow}-{lastRow} / {_totalCount}";
            _previousButton.Enabled = _pageIndex > 0;
            _nextButton.Enabled = lastRow < _totalCount;
        }
    }
}
