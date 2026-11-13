namespace ReflavorRP.AccentTool;

// Edits a copy of one accent. OK hands back Result; Abort means the user deleted the accent.
internal sealed class AccentEditor : Form
{
    private const int PatternColumn = 0;
    private const int ReplacementColumn = 1;
    private const int ChanceColumn = 2;

    private readonly TextBox name = Theme.NewEditor(readOnly: false, multiline: false);
    private readonly DataGridView rules = NewGrid();
    private readonly TextBox sample = Theme.NewEditor(readOnly: false, multiline: false);
    private readonly Label sampleResult = new() { AutoSize = true, ForeColor = Theme.Accent, Font = Theme.Editor, Margin = new Padding(0, 8, 0, 0) };
    private readonly Label problem = new() { AutoSize = true, ForeColor = Theme.Error, Margin = new Padding(0, 8, 0, 0) };
    private readonly Func<string, bool> nameTaken;

    public Accent Result { get; private set; } = new();

    public AccentEditor(Accent source, Func<string, bool> nameTaken, bool isNew)
    {
        this.nameTaken = nameTaken;
        Text = isNew ? "New accent" : "Edit accent";
        Theme.Apply(this);
        ClientSize = new Size(760, 560);
        MinimumSize = new Size(560, 460);
        Padding = new Padding(20);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ShowIcon = false;
        MinimizeBox = false;

        name.Text = source.Name;
        foreach (var rule in source.Rules)
            rules.Rows.Add(rule.Pattern, rule.Replacement, rule.Chance);
        sample.Text = "Sorry, I really love this nice place.";

        rules.CellFormatting += MarkInvalidPattern;
        rules.CellEndEdit += FixChance;
        // Commit while typing, so the sample updates on every key.
        rules.CurrentCellDirtyStateChanged += (_, _) => rules.CommitEdit(DataGridViewDataErrorContexts.Commit);
        rules.CellValueChanged += (_, _) => UpdateSample();
        rules.RowsRemoved += (_, _) => UpdateSample();
        sample.TextChanged += (_, _) => UpdateSample();

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // name caption
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));       // name
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // rules caption
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // rules hint
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));      // rules
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // rule buttons
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // sample caption
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));       // sample
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // sample result
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // problem
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));          // footer

        layout.Controls.Add(Theme.NewCaption("NAME"));
        layout.Controls.Add(Theme.Card(name));
        layout.Controls.Add(Caption("RULES"));
        layout.Controls.Add(new Label
        {
            Text = "Rules run top to bottom, each on what the one above wrote. The pattern is a regular expression " +
                   "(case is ignored and kept); $1 in the replacement puts back the first group.",
            ForeColor = Theme.Muted, AutoSize = true, MaximumSize = new Size(700, 0), Margin = new Padding(0, 0, 0, 8),
        });
        layout.Controls.Add(rules);
        layout.Controls.Add(BuildRuleButtons());
        layout.Controls.Add(Caption("TRY IT"));
        layout.Controls.Add(Theme.Card(sample));
        layout.Controls.Add(sampleResult);
        layout.Controls.Add(problem);
        layout.Controls.Add(BuildFooter(isNew));
        Controls.Add(layout);

        UpdateSample();
    }

    private static Label Caption(string text)
    {
        var caption = Theme.NewCaption(text);
        caption.Margin = new Padding(0, 14, 0, 6);
        return caption;
    }

    private static DataGridView NewGrid()
    {
        var grid = Theme.DarkScrollbars(new DataGridView
        {
            Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackgroundColor = Theme.Surface,
            GridColor = Theme.Background, RowHeadersVisible = false, AllowUserToAddRows = false,
            AllowUserToResizeRows = false, EnableHeadersVisualStyles = false,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 32, SelectionMode = DataGridViewSelectionMode.CellSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Margin = Padding.Empty,
        });
        grid.RowTemplate.Height = 32;
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.SurfaceHover, ForeColor = Theme.Muted, Font = Theme.Caption,
            SelectionBackColor = Theme.SurfaceHover, SelectionForeColor = Theme.Muted, Padding = new Padding(6, 0, 0, 0),
        };
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Theme.Surface, ForeColor = Theme.Text, Font = Theme.Code,
            SelectionBackColor = Theme.SurfaceHover, SelectionForeColor = Theme.Text, Padding = new Padding(6, 0, 0, 0),
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "PATTERN (REGEX)", FillWeight = 55 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "REPLACEMENT", FillWeight = 35 });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "CHANCE %", AutoSizeMode = DataGridViewAutoSizeColumnMode.None, Width = 90,
            ToolTipText = "Percent of matches that get replaced",
        });
        foreach (DataGridViewColumn column in grid.Columns)
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
        grid.EditingControlShowing += (_, e) =>
        {
            e.Control.BackColor = Theme.SurfaceHover;
            e.Control.ForeColor = Theme.Text;
        };
        return grid;
    }

    private Control BuildRuleButtons()
    {
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 8, 0, 0) };
        var add = Theme.NewButton("+ Add rule", primary: false);
        add.Margin = new Padding(0, 0, 8, 0);
        add.Click += (_, _) =>
        {
            var row = rules.Rows.Add("", "", 100);
            rules.CurrentCell = rules.Rows[row].Cells[PatternColumn];
            rules.BeginEdit(selectAll: true);
        };
        var remove = Theme.NewButton("Remove rule", primary: false);
        remove.Margin = Padding.Empty;
        remove.Click += (_, _) =>
        {
            if (rules.CurrentRow != null)
                rules.Rows.Remove(rules.CurrentRow);
        };
        buttons.Controls.Add(add);
        buttons.Controls.Add(remove);
        return buttons;
    }

    private Control BuildFooter(bool isNew)
    {
        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 14, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        if (!isNew)
        {
            var delete = Theme.NewButton("Delete accent", primary: false);
            delete.ForeColor = Theme.Error;
            delete.Margin = Padding.Empty;
            delete.Click += (_, _) => Delete();
            footer.Controls.Add(delete, 0, 0);
        }

        var right = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, Margin = Padding.Empty };
        var save = Theme.NewButton("Save", primary: true);
        save.Click += (_, _) => Save();
        var cancel = Theme.NewButton("Cancel", primary: false);
        cancel.DialogResult = DialogResult.Cancel;
        CancelButton = cancel;
        right.Controls.Add(save);
        right.Controls.Add(cancel);
        footer.Controls.Add(right, 1, 0);
        return footer;
    }

    private List<AccentRule> ReadRules() => rules.Rows.Cast<DataGridViewRow>()
        .Select(row => new AccentRule
        {
            Pattern = row.Cells[PatternColumn].Value?.ToString() ?? "",
            Replacement = row.Cells[ReplacementColumn].Value?.ToString() ?? "",
            Chance = int.TryParse(row.Cells[ChanceColumn].Value?.ToString(), out var chance) ? Math.Clamp(chance, 0, 100) : 100,
        })
        .ToList();

    // Invalid patterns are skipped when the accent runs, as in the plugin, so they are flagged rather than refused.
    private void MarkInvalidPattern(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex != PatternColumn || e.RowIndex < 0 || e.CellStyle == null)
            return;
        var valid = Accent.IsValidPattern(e.Value?.ToString() ?? "");
        e.CellStyle.ForeColor = valid ? Theme.Text : Theme.Error;
        rules.Rows[e.RowIndex].Cells[PatternColumn].ToolTipText = valid ? "" : "Invalid regex, this rule is skipped";
    }

    // Corrected instead of refused: a refused cell keeps focus and would block Cancel too.
    private void FixChance(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex != ChanceColumn)
            return;
        var cell = rules.Rows[e.RowIndex].Cells[ChanceColumn];
        var typed = cell.Value?.ToString();
        var fixedChance = int.TryParse(typed, out var chance) ? Math.Clamp(chance, 0, 100) : 100;
        if (typed == fixedChance.ToString())
            return;
        cell.Value = fixedChance;
        problem.Text = $"Chance is a percent from 0 to 100, so \"{typed}\" became {fixedChance}.";
    }

    private void UpdateSample()
    {
        var accent = new Accent { Rules = ReadRules() };
        // A fixed seed keeps rules with a chance from flickering while typing, like the plugin's preview.
        sampleResult.Text = "→ " + accent.Apply(sample.Text, new Random(0));
        var invalid = accent.Rules.Select((rule, index) => (rule, index))
            .Where(item => item.rule.Pattern != "" && !Accent.IsValidPattern(item.rule.Pattern))
            .Select(item => (item.index + 1).ToString())
            .ToList();
        problem.Text = invalid.Count == 0 ? "" : $"Rule {string.Join(", ", invalid)}: invalid regex, it will be skipped.";
    }

    private void Save()
    {
        rules.EndEdit();
        var newName = name.Text.Trim();
        if (newName == "")
            problem.Text = "Give the accent a name.";
        else if (nameTaken(newName))
            problem.Text = "Another accent already has this name.";
        else
        {
            Result = new Accent { Name = newName, Rules = ReadRules().Where(rule => rule.Pattern != "").ToList() };
            DialogResult = DialogResult.OK;
        }
    }

    private void Delete()
    {
        var answer = MessageBox.Show(this, $"Delete \"{name.Text}\"? This cannot be undone.", "Delete accent",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer == DialogResult.Yes)
            DialogResult = DialogResult.Abort;
    }
}
