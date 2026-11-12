namespace Mes.WinUI;

/// <summary>统一卡片、顶栏、表格的视觉样式。</summary>
public static class UiChrome
{
    public static AntdUI.Panel CreateCard(DockStyle dock = DockStyle.Fill, int? shadow = null)
        => new()
        {
            Dock = dock,
            Padding = new Padding(UiTheme.SpaceMd),
            Back = UiTheme.CardBack,
            Radius = UiTheme.CardRadius,
            BorderWidth = 0,
            Shadow = shadow ?? UiTheme.CardShadow,
            Margin = new Padding(0, UiTheme.SpaceMd, 0, UiTheme.SpaceMd)
        };

    public static AntdUI.Panel CreatePagerBar()
        => new()
        {
            Dock = DockStyle.Bottom,
            Height = UiTheme.PagerHeight,
            Padding = new Padding(UiTheme.SpaceMd, UiTheme.SpaceSm, UiTheme.SpaceMd, UiTheme.SpaceSm),
            Back = UiTheme.CardBack,
            Radius = UiTheme.CardRadius,
            BorderWidth = 0,
            Shadow = 2
        };

    public static AntdUI.PageHeader CreateDialogHeader(string title, string? subText = null, bool maximizable = false)
        => new()
        {
            Text = title,
            SubText = subText,
            Dock = DockStyle.Top,
            Height = UiTheme.HeaderHeight,
            ShowButton = true,
            MaximizeBox = maximizable,
            MinimizeBox = false,
            BackColor = UiTheme.DialogHeaderBack,
            ForeColor = Color.White,
            DividerShow = false
        };

    public static void StyleGrid(DataGridView grid)
    {
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowHeadersVisible = false;
        grid.AutoGenerateColumns = false;
        grid.BackgroundColor = UiTheme.CardBack;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = UiTheme.TableHeader;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = UiTheme.TextPrimary;
        grid.ColumnHeadersDefaultCellStyle.Font = UiTheme.UiFont(UiTheme.FontSize, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.ColumnHeadersHeight = UiTheme.TableHeaderHeight;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.RowTemplate.Height = UiTheme.TableRowHeight;
        grid.DefaultCellStyle.Font = UiTheme.UiFont();
        grid.DefaultCellStyle.ForeColor = UiTheme.TextPrimary;
        grid.DefaultCellStyle.SelectionBackColor = UiTheme.RowHover;
        grid.DefaultCellStyle.SelectionForeColor = UiTheme.TextPrimary;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);
        grid.GridColor = UiTheme.Border;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }
}
