using System.Runtime.InteropServices;

namespace ReflavorRP.AccentTool;

// The plugin's chat colours: lilac for actions, amber for highlights. Also builds the styled controls.
internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(0x1B, 0x1A, 0x24);
    public static readonly Color Surface = Color.FromArgb(0x26, 0x24, 0x33);
    public static readonly Color SurfaceHover = Color.FromArgb(0x32, 0x2F, 0x42);
    public static readonly Color Text = Color.FromArgb(0xE6, 0xE6, 0xE6);
    public static readonly Color Muted = Color.FromArgb(0x9A, 0x96, 0xAE);
    public static readonly Color Accent = Color.FromArgb(0xC2, 0xA2, 0xDA);
    public static readonly Color AccentText = Color.FromArgb(0x1B, 0x1A, 0x24);
    public static readonly Color Highlight = Color.FromArgb(0xFF, 0xD1, 0x66);
    public static readonly Color Error = Color.FromArgb(0xFF, 0x7A, 0x7A);

    public static readonly Font Body = new("Segoe UI", 10f);
    public static readonly Font Editor = new("Segoe UI", 11.5f);
    public static readonly Font Code = new("Consolas", 10.5f);
    public static readonly Font Caption = new("Segoe UI Semibold", 9f);
    public static readonly Font Title = new("Segoe UI Semibold", 18f);

    public static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        form.Font = Body;
        // Dark title bar on Windows 10 20H1+ and 11; ignored elsewhere.
        form.HandleCreated += (_, _) =>
        {
            var dark = 1;
            _ = DwmSetWindowAttribute(form.Handle, 20, ref dark, sizeof(int));
        };
    }

    // Scrollbars follow the dark Explorer theme instead of the default light one.
    public static T DarkScrollbars<T>(T control) where T : Control
    {
        control.HandleCreated += (_, _) => _ = SetWindowTheme(control.Handle, "DarkMode_Explorer", null);
        return control;
    }

    public static Label NewCaption(string text) => new()
    {
        Text = text, AutoSize = true, Font = Caption, ForeColor = Muted, Margin = new Padding(0, 0, 0, 6),
    };

    public static Button NewButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
            Padding = new Padding(14, 6, 14, 6), Margin = new Padding(8, 0, 0, 0), Font = Caption,
            BackColor = primary ? Accent : Surface,
            ForeColor = primary ? AccentText : Text,
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = primary ? Highlight : SurfaceHover;
        return button;
    }

    public static TextBox NewEditor(bool readOnly, bool multiline = true) => DarkScrollbars(new TextBox
    {
        Dock = DockStyle.Fill, Multiline = multiline, ReadOnly = readOnly,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
        AcceptsReturn = multiline, BorderStyle = BorderStyle.None, Font = Editor,
        BackColor = Surface, ForeColor = readOnly ? Accent : Text,
    });

    // TextBoxes have no inner padding, so they sit in a padded panel of the same colour.
    public static Panel Card(Control content)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(12, 10, 8, 10), Margin = Padding.Empty };
        card.Controls.Add(content);
        return card;
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? appName, string? idList);
}
