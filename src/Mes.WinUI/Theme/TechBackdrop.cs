using System.ComponentModel;
using System.Drawing.Imaging;
using System.Reflection;

namespace Mes.WinUI;

/// <summary>科技感背景图加载与绘制。</summary>
public static class TechBackdrop
{
    private static readonly object Sync = new();
    private static Image? _image;
    private static bool _tried;

    public static Image Image
    {
        get
        {
            if (_image != null)
                return _image;

            lock (Sync)
            {
                if (_image != null)
                    return _image;
                if (_tried)
                    return _image ??= CreateFallback();

                _tried = true;
                _image = LoadFromDisk() ?? LoadFromResource() ?? CreateFallback();
                return _image;
            }
        }
    }

    public static void Apply(Control control, ImageLayout layout = ImageLayout.Stretch)
    {
        control.BackgroundImage = Image;
        control.BackgroundImageLayout = layout;
    }

    public static void Paint(Graphics g, Rectangle bounds, float opacity = 0.55f)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        opacity = Math.Clamp(opacity, 0.08f, 1f);
        using var attrs = new ImageAttributes();
        var matrix = new ColorMatrix { Matrix33 = opacity };
        attrs.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

        var img = Image;
        var scale = Math.Max(bounds.Width / (float)img.Width, bounds.Height / (float)img.Height);
        var w = (int)Math.Ceiling(img.Width * scale);
        var h = (int)Math.Ceiling(img.Height * scale);
        var x = bounds.X + (bounds.Width - w) / 2;
        var y = bounds.Y + (bounds.Height - h) / 2;
        g.DrawImage(img, new Rectangle(x, y, w, h), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, attrs);
    }

    private static Image? LoadFromDisk()
    {
        foreach (var path in CandidatePaths())
        {
            try
            {
                if (File.Exists(path))
                    return System.Drawing.Image.FromFile(path);
            }
            catch
            {
                // try next
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidatePaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "Assets", "tech-backdrop.jpg");
        yield return Path.Combine(AppContext.BaseDirectory, "tech-backdrop.jpg");
        var asm = Assembly.GetExecutingAssembly().Location;
        if (!string.IsNullOrWhiteSpace(asm))
            yield return Path.Combine(Path.GetDirectoryName(asm)!, "Assets", "tech-backdrop.jpg");
    }

    private static Image? LoadFromResource()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var name = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("tech-backdrop.jpg", StringComparison.OrdinalIgnoreCase));
            if (name == null)
                return null;
            using var stream = asm.GetManifestResourceStream(name);
            if (stream == null)
                return null;
            return System.Drawing.Image.FromStream(stream);
        }
        catch
        {
            return null;
        }
    }

    private static Image CreateFallback()
    {
        var bmp = new Bitmap(1600, 900);
        using var g = Graphics.FromImage(bmp);
        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            Color.FromArgb(8, 28, 58),
            Color.FromArgb(22, 90, 160),
            35f);
        g.FillRectangle(bg, 0, 0, bmp.Width, bmp.Height);

        using var pen = new Pen(Color.FromArgb(55, 120, 200, 255), 1f);
        for (var x = 0; x < bmp.Width; x += 48)
            g.DrawLine(pen, x, 0, x, bmp.Height);
        for (var y = 0; y < bmp.Height; y += 48)
            g.DrawLine(pen, 0, y, bmp.Width, y);

        using var glow = new SolidBrush(Color.FromArgb(40, 80, 180, 255));
        g.FillEllipse(glow, 200, 120, 420, 280);
        g.FillEllipse(glow, 980, 420, 520, 340);
        return bmp;
    }
}

/// <summary>带科技背景的内容容器。</summary>
public sealed class TechBackPanel : Panel
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float BackdropOpacity { get; set; } = 0.62f;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color WashColor { get; set; } = Color.FromArgb(110, 236, 244, 252);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ClearColor { get; set; } = Color.FromArgb(16, 32, 58);

    public TechBackPanel()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        UpdateStyles();
        DoubleBuffered = true;
        BackColor = Color.FromArgb(20, 40, 70);
        TechBackdrop.Apply(this);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(ClearColor);
        TechBackdrop.Paint(e.Graphics, ClientRectangle, BackdropOpacity);
        using var brush = new SolidBrush(WashColor);
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }
}
