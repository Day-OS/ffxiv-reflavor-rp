using System.Text.Json;

namespace ReflavorRP.AccentTool;

// Type text, toggle accents, copy the result. Accents run in the order they were turned on, like in the plugin.
internal sealed class AccentForm : Form
{
    private const int ListWidth = 206;

    private readonly TextBox input = Theme.NewEditor(readOnly: false);
    private readonly TextBox output = Theme.NewEditor(readOnly: true);
    private readonly Label order = Theme.NewCaption("");
    private readonly Label status = new() { AutoSize = true, ForeColor = Theme.Highlight, Font = Theme.Caption };
    private readonly FlowLayoutPanel list = Theme.DarkScrollbars(new FlowLayoutPanel
    {
        Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false,
        AutoScroll = true, Padding = new Padding(0, 4, 0, 0),
    });
    private readonly Button copyActive = WideButton("Copy active as new");
    private readonly System.Windows.Forms.Timer statusTimer = new() { Interval = 1500 };

    private readonly List<Accent> custom;
    private readonly List<Accent> active = [];
    private readonly List<(Accent Accent, CheckBox Toggle)> toggles = [];

    public AccentForm()
    {
        Text = "Reflavor Accents";
        using (var icon = typeof(AccentForm).Assembly.GetManifestResourceStream("icon.ico")!)
            Icon = new Icon(icon);
        Theme.Apply(this);
        ClientSize = new Size(900, 600);
        MinimumSize = new Size(640, 460);
        Padding = new Padding(20);

        custom = LoadCustomAccents();
        input.PlaceholderText = "Type what your character says…";
        input.TextChanged += (_, _) => UpdateOutput();
        statusTimer.Tick += (_, _) => { status.Text = ""; statusTimer.Stop(); };
        copyActive.Click += (_, _) => CreateAccent(Accent.Combine("", active).Copy(UniqueName(string.Join(" + ", active.Select(a => a.Name)))));

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3 };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var header = BuildHeader();
        root.Controls.Add(header, 0, 0);
        root.SetColumnSpan(header, 2);
        root.Controls.Add(BuildAccentPanel(), 0, 1);
        root.Controls.Add(BuildEditors(), 1, 1);
        root.Controls.Add(BuildFooter(), 1, 2);
        Controls.Add(root);

        RebuildList();
    }

    private static Control BuildHeader()
    {
        var header = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 16), WrapContents = false,
        };
        header.Controls.Add(new Label { Text = "Reflavor Accents", Font = Theme.Title, ForeColor = Theme.Accent, AutoSize = true, Margin = Padding.Empty });
        header.Controls.Add(new Label
        {
            Text = "Type a line, turn on accents, copy the result.",
            ForeColor = Theme.Muted, AutoSize = true, Margin = new Padding(2, 2, 0, 0),
        });
        return header;
    }

    private Control BuildAccentPanel()
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, Margin = new Padding(0, 0, 20, 0) };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(Theme.NewCaption("ACCENTS"));
        panel.Controls.Add(list);
        order.MaximumSize = new Size(230, 0);
        order.Margin = new Padding(0, 8, 0, 0);
        panel.Controls.Add(order);
        return panel;
    }

    private Control BuildEditors()
    {
        var editors = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Margin = Padding.Empty };
        editors.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editors.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        editors.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editors.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        editors.Controls.Add(Theme.NewCaption("YOUR TEXT"));
        editors.Controls.Add(Theme.Card(input));
        var resultCaption = Theme.NewCaption("WITH ACCENT");
        resultCaption.Margin = new Padding(0, 14, 0, 6);
        editors.Controls.Add(resultCaption);
        editors.Controls.Add(Theme.Card(output));
        return editors;
    }

    private Control BuildFooter()
    {
        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, 14, 0, 0), WrapContents = false,
        };
        var copy = Theme.NewButton("Copy result", primary: true);
        copy.Click += (_, _) => CopyResult();
        var clear = Theme.NewButton("Clear", primary: false);
        clear.Click += (_, _) => { input.Clear(); input.Focus(); };
        footer.Controls.Add(copy);
        footer.Controls.Add(clear);
        status.Margin = new Padding(0, 10, 12, 0);
        footer.Controls.Add(status);
        return footer;
    }

    // Templates first, then the user's accents with an Edit button each, then the actions that add accents.
    private void RebuildList()
    {
        list.SuspendLayout();
        var old = list.Controls.Cast<Control>().Where(control => control != copyActive).ToList();
        list.Controls.Clear();
        old.ForEach(control => control.Dispose());
        toggles.Clear();

        foreach (var accent in Accent.Templates)
            list.Controls.Add(NewToggle(accent, ListWidth));

        var yours = Theme.NewCaption("YOUR ACCENTS");
        yours.Margin = new Padding(0, 12, 0, 6);
        list.Controls.Add(yours);
        if (custom.Count == 0)
            list.Controls.Add(new Label { Text = "None yet.", AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(0, 0, 0, 6) });
        foreach (var accent in custom)
        {
            var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
            var toggle = NewToggle(accent, ListWidth - 54);
            toggle.Margin = new Padding(0, 0, 6, 0);
            var edit = Theme.NewButton("Edit", primary: false);
            edit.AutoSize = false;
            edit.Size = new Size(48, 36);
            edit.Padding = Padding.Empty;
            edit.Margin = Padding.Empty;
            edit.Click += (_, _) => EditAccent(accent);
            row.Controls.Add(toggle);
            row.Controls.Add(edit);
            list.Controls.Add(row);
        }

        var create = WideButton("+ New accent");
        create.Click += (_, _) => CreateAccent(new Accent { Name = UniqueName("New accent"), Rules = [new AccentRule()] });
        var import = WideButton("Import from plugin");
        import.Click += (_, _) => ImportFromPlugin();
        list.Controls.Add(create);
        list.Controls.Add(copyActive);
        list.Controls.Add(import);
        list.ResumeLayout();
        UpdateOutput();
    }

    private CheckBox NewToggle(Accent accent, int width)
    {
        var toggle = new CheckBox
        {
            Appearance = Appearance.Button, FlatStyle = FlatStyle.Flat, TextAlign = ContentAlignment.MiddleLeft,
            Size = new Size(width, 36), Margin = new Padding(0, 0, 0, 6), Padding = new Padding(8, 0, 0, 0),
            BackColor = Theme.Surface, ForeColor = Theme.Text, Cursor = Cursors.Hand, AutoEllipsis = true,
            Checked = active.Contains(accent),
        };
        toggle.FlatAppearance.BorderSize = 0;
        toggle.FlatAppearance.MouseOverBackColor = Theme.SurfaceHover;
        toggle.FlatAppearance.CheckedBackColor = Theme.Accent;
        toggle.CheckedChanged += (_, _) =>
        {
            if (toggle.Checked)
                active.Add(accent);
            else
                active.Remove(accent);
            UpdateOutput();
        };
        toggles.Add((accent, toggle));
        return toggle;
    }

    private static Button WideButton(string text)
    {
        var button = Theme.NewButton(text, primary: false);
        button.AutoSize = false;
        button.Size = new Size(ListWidth, 34);
        button.Margin = new Padding(0, 0, 0, 6);
        button.TextAlign = ContentAlignment.MiddleLeft;
        button.Padding = new Padding(8, 0, 0, 0);
        button.ForeColor = Theme.Accent;
        return button;
    }

    private void CreateAccent(Accent draft)
    {
        using var editor = new AccentEditor(draft, name => NameTaken(name, null), isNew: true);
        if (editor.ShowDialog(this) != DialogResult.OK)
            return;
        custom.Add(editor.Result);
        SaveAndRebuild();
    }

    private void EditAccent(Accent accent)
    {
        using var editor = new AccentEditor(accent, name => NameTaken(name, accent), isNew: false);
        switch (editor.ShowDialog(this))
        {
            case DialogResult.OK:
                // Changed in place, so the accent keeps its spot in the running order.
                accent.Name = editor.Result.Name;
                accent.Rules = editor.Result.Rules;
                break;
            case DialogResult.Abort:
                custom.Remove(accent);
                active.Remove(accent);
                break;
            default:
                return;
        }
        SaveAndRebuild();
    }

    private void ImportFromPlugin()
    {
        List<Accent> found;
        try
        {
            found = AccentStore.ReadPluginAccents();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            MessageBox.Show(this, "Could not read the plugin settings:\n" + e.Message, "Import from plugin",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // An accent with the same name is taken to be the same accent and left as it is here.
        var added = found.Where(accent => !NameTaken(accent.Name, null)).ToList();
        var message = found.Count == 0
            ? "No accents found. Make one under \"Your accents\" in the plugin settings first."
            : $"Imported {added.Count} of {found.Count} accents." +
              (added.Count < found.Count ? " The others already have a name here." : "");
        if (added.Count > 0)
        {
            custom.AddRange(added);
            SaveAndRebuild();
        }
        MessageBox.Show(this, message, "Import from plugin", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void SaveAndRebuild()
    {
        try
        {
            AccentStore.Save(custom);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(this, "Could not save your accents:\n" + e.Message, "Reflavor Accents",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        RebuildList();
    }

    // Runs before the window exists, so the message box has no owner.
    private static List<Accent> LoadCustomAccents()
    {
        try
        {
            return AccentStore.Load();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            MessageBox.Show("Could not load your accents:\n" + e.Message, "Reflavor Accents",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return [];
        }
    }

    private bool NameTaken(string name, Accent? self) =>
        Accent.Templates.Concat(custom).Any(accent => accent != self && accent.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private string UniqueName(string name)
    {
        var candidate = name;
        for (var n = 2; NameTaken(candidate, null); n++)
            candidate = $"{name} {n}";
        return candidate;
    }

    private void CopyResult()
    {
        if (output.Text == "")
            return;
        Clipboard.SetText(output.Text);
        status.Text = "Copied!";
        statusTimer.Stop();
        statusTimer.Start();
    }

    private void UpdateOutput()
    {
        // Number the active accents so the running order is visible on the buttons.
        foreach (var (accent, toggle) in toggles)
        {
            var position = active.IndexOf(accent);
            toggle.Text = position < 0 ? accent.Name : $"{position + 1}   {accent.Name}";
            toggle.ForeColor = position < 0 ? Theme.Text : Theme.AccentText;
            toggle.Font = position < 0 ? Theme.Body : Theme.Caption;
        }
        copyActive.Enabled = active.Count > 0;
        order.Text = active.Count == 0
            ? "No accent on. Accents run in the order you turn them on."
            : "Runs: " + string.Join(" → ", active.Select(a => a.Name));
        output.Text = active.Count == 0 ? input.Text : Accent.Combine("", active).Apply(input.Text);
    }
}
