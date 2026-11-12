namespace Mes.WinUI;

/// <summary>
/// 表单字段布局。避免 AntdUI.Label 宽高为 0；绝对定位时对齐父容器 DisplayRectangle
///（AntdUI.Panel 的 Shadow 会内缩内容区，坐标从 0 起会落在阴影带，透出底层深蓝且文字被裁切）。
/// </summary>
public static class FormUi
{
    public const int LabelWidth = 104;
    public const int FieldGap = 14;
    public const int EditorLeft = LabelWidth + FieldGap;
    public const int RowHeight = 58;
    public const int EditorHeight = 44;

    /// <summary>内容区原点（已含 Padding / AntdUI Shadow 内缩）。</summary>
    public static Point ContentOrigin(Control parent)
    {
        var r = parent.DisplayRectangle;
        return new Point(r.X, r.Y);
    }

    public static Label Caption(string text, int x, int y, int width = LabelWidth)
        => new()
        {
            Text = text,
            AutoSize = false,
            Bounds = new Rectangle(x, y, width, EditorHeight),
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.UiFont(),
            BackColor = Color.Transparent
        };

    public static Label Section(string text, int x, int y, int width = 480)
        => new()
        {
            Text = text,
            AutoSize = false,
            Bounds = new Rectangle(x, y, width, 34),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.TextPrimary,
            Font = UiTheme.UiFont(UiTheme.FontSizeLg, FontStyle.Bold),
            BackColor = Color.Transparent
        };

    public static Label Hint(string text, int x, int y, int width)
        => new()
        {
            Text = text,
            AutoSize = false,
            Bounds = new Rectangle(x, y, width, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiTheme.MutedText,
            Font = UiTheme.UiFont(UiTheme.FontSizeSm),
            BackColor = Color.Transparent
        };

    public static void AddSection(Control parent, string text, ref int y, int width = 480)
    {
        var o = ContentOrigin(parent);
        parent.Controls.Add(Section(text, o.X, o.Y + y, width));
        y += 40;
    }

    public static void AddField(Control parent, string caption, Control editor, ref int y, int editorWidth = 360)
    {
        var o = ContentOrigin(parent);
        parent.Controls.Add(Caption(caption, o.X, o.Y + y));
        editor.SetBounds(o.X + EditorLeft, o.Y + y, editorWidth, EditorHeight);
        parent.Controls.Add(editor);
        y += RowHeight;
    }

    /// <summary>在内容区内放置控件（相对坐标 → 绝对 Bounds）。</summary>
    public static void Place(Control parent, Control child, int x, int y, int width, int height)
    {
        var o = ContentOrigin(parent);
        child.SetBounds(o.X + x, o.Y + y, width, height);
        if (!parent.Controls.Contains(child))
            parent.Controls.Add(child);
    }
}
