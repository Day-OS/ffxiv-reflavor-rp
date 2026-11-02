using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace ReflavorRP.Windows;

public class ConfigWindow : Window
{
    // roughly the GTA World emote purple
    private static readonly Vector4 Purple = new(0.76f, 0.64f, 0.85f, 1f);
    private static readonly Vector4 PurpleDark = new(0.42f, 0.30f, 0.56f, 1f);
    private static readonly Vector4 PurpleHover = new(0.52f, 0.38f, 0.68f, 1f);
    private static readonly Vector4 PurpleActive = new(0.34f, 0.24f, 0.46f, 1f);
    private static readonly Vector4 Muted = new(0.62f, 0.62f, 0.66f, 1f);
    private static readonly Vector4 ErrorColor = new(1f, 0.45f, 0.45f, 1f);

    private static readonly (ImGuiCol, Vector4)[] ThemeColors =
    [
        (ImGuiCol.TitleBgActive, PurpleActive),
        (ImGuiCol.Button, PurpleDark),
        (ImGuiCol.ButtonHovered, PurpleHover),
        (ImGuiCol.ButtonActive, PurpleActive),
        (ImGuiCol.Tab, PurpleActive),
        (ImGuiCol.TabHovered, PurpleHover),
        (ImGuiCol.TabActive, PurpleDark),
        (ImGuiCol.Header, PurpleDark),
        (ImGuiCol.HeaderHovered, PurpleHover),
        (ImGuiCol.HeaderActive, PurpleActive),
        (ImGuiCol.CheckMark, Purple),
        (ImGuiCol.FrameBgHovered, PurpleActive),
        (ImGuiCol.SeparatorHovered, PurpleHover),
        (ImGuiCol.TableHeaderBg, PurpleActive),
    ];

    private const int ThemeVarCount = 4; // keep in sync with PreDraw

    private readonly Configuration config;

    private int editingAccent = -1;
    private string previewText = "Sorry, I really love this nice place!";

    public ConfigWindow(Configuration config) : base("ReflavorRP###ReflavorRP config")
    {
        this.config = config;

        Flags = ImGuiWindowFlags.NoCollapse;
        Size = new Vector2(460, 420);
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = new WindowSizeConstraints { MinimumSize = new Vector2(380, 300) };
    }

    public override void PreDraw()
    {
        foreach (var (col, color) in ThemeColors)
            ImGui.PushStyleColor(col, color);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4f);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6));
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(ThemeVarCount);
        ImGui.PopStyleColor(ThemeColors.Length);
    }

    public override void Draw()
    {
        DrawHeader();

        if (!ImGui.BeginTabBar("tabs"))
            return;

        if (ImGui.BeginTabItem("Outgoing"))
        {
            DrawOutgoingTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Incoming"))
        {
            DrawIncomingTab();
            ImGui.EndTabItem();
        }

        if (ImGui.BeginTabItem("Accents"))
        {
            DrawAccentsTab();
            ImGui.EndTabItem();
        }

        ImGui.EndTabBar();
    }

    private void DrawHeader()
    {
        ImGui.TextColored(Purple, "ReflavorRP");
        ImGui.SameLine();
        ImGui.TextColored(Muted, "SA-MP / GTA World style RP chat");
        ImGui.Spacing();
    }


    private void DrawOutgoingTab()
    {
        Section("What you send");
        DrawCheckbox("Conversion enabled", config.ConversionEnabled, v => config.ConversionEnabled = v,
                     "Turns /me, /s, /low, /w, /b and plain Say into FFXIV RP style lines.");
        DrawCheckbox("Replace the /me emote", config.ReplaceMeEmote, v => config.ReplaceMeEmote = v,
                     "When on, /me works like /em. When off, /me plays the game's own emote.");

        Section("Verbs");
        DrawTextInput("/s", config.VerbShout, v => config.VerbShout = v,
                      "/s help -> shouts, \"Help!\"");
        DrawTextInput("/low", config.VerbLow, v => config.VerbLow = v,
                      "/low come here -> whispers, \"Come here.\"");
        DrawTextInput("/w", config.VerbWhisper, v => config.VerbWhisper = v,
                      "/w Ziero hi -> whispers to Ziero, \"Hi.\"");
    }

    private void DrawIncomingTab()
    {
        Section("What you see");
        DrawCheckbox("GTA World style chat", config.StyleIncomingChat, v => config.StyleIncomingChat = v,
                     "Restyles Say, Emote, Shout and Yell. Only changes what you see, nothing is sent.");
        DrawCheckbox("Rewrite full lines", config.RewriteFullChatLines, v => config.RewriteFullChatLines = v,
                     "Needed for \"Name says:\" and \"* Name\". Turn off if it clashes with Chat 2.");
        DrawCheckbox("Darker text for distant players", config.FadeByDistance, v => config.FadeByDistance = v,
                     "The further away the speaker, the darker their line.");
    }

    private void DrawAccentsTab()
    {
        var active = config.FindAccent(config.ActiveAccent);

        Section("Active accent");
        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##active", active?.Name ?? "None"))
        {
            if (ImGui.Selectable("None", active == null))
                SetActiveAccent("");

            foreach (var accent in Accent.Templates)
            {
                if (ImGui.Selectable(accent.Name + " (template)", accent == active))
                    SetActiveAccent(accent.Name);
            }

            foreach (var accent in config.CustomAccents)
            {
                if (ImGui.Selectable(accent.Name, accent == active))
                    SetActiveAccent(accent.Name);
            }

            ImGui.EndCombo();
        }

        if (active != null)
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##preview", "Type something to test", ref previewText, 256);
            ImGui.TextColored(Purple, "->");
            ImGui.SameLine();
            ImGui.TextWrapped(active.Apply(previewText));
        }

        Section("Your accents");
        if (ImGui.Button("New accent"))
            AddAccent(new Accent { Name = UniqueName("New accent") });

        if (active != null)
        {
            ImGui.SameLine();
            if (ImGui.Button("Copy active accent"))
                AddAccent(active.Copy(UniqueName(active.Name)));
        }

        if (config.CustomAccents.Count == 0)
        {
            ImGui.TextColored(Muted, "No accents yet. Copy a template to start from it.");
            return;
        }

        if (editingAccent >= config.CustomAccents.Count)
            editingAccent = config.CustomAccents.Count - 1;

        var editing = editingAccent >= 0 ? config.CustomAccents[editingAccent] : null;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##edit", editing?.Name ?? "Choose an accent to edit"))
        {
            for (var i = 0; i < config.CustomAccents.Count; i++)
            {
                if (ImGui.Selectable(config.CustomAccents[i].Name + "##" + i, i == editingAccent))
                    editingAccent = i;
            }

            ImGui.EndCombo();
        }

        if (editing != null)
            DrawAccentEditor(editing);
    }



    private void DrawAccentEditor(Accent accent)
    {
        var name = accent.Name;
        ImGui.SetNextItemWidth(-60);
        if (ImGui.InputText("Name", ref name, 64) && name != "" && config.FindAccent(name) == null)
        {
            if (config.ActiveAccent == accent.Name)
                config.ActiveAccent = name;
            accent.Name = name;
            config.Save();
        }

        var tableFlags = ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchSame;
        if (ImGui.BeginTable("rules", 3, tableFlags))
        {
            ImGui.TableSetupColumn("Pattern (regex)");
            ImGui.TableSetupColumn("Replacement ($1 = group)");
            ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 24);
            ImGui.TableHeadersRow();

            for (var i = 0; i < accent.Rules.Count; i++)
            {
                var rule = accent.Rules[i];
                ImGui.PushID(i);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                var pattern = rule.Pattern;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##pattern", ref pattern, 128))
                {
                    rule.Pattern = pattern;
                    config.Save();
                }

                if (!Accent.IsValidPattern(rule.Pattern))
                    ImGui.TextColored(ErrorColor, "Invalid regex, skipped");

                ImGui.TableNextColumn();
                var replacement = rule.Replacement;
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputText("##replacement", ref replacement, 128))
                {
                    rule.Replacement = replacement;
                    config.Save();
                }

                ImGui.TableNextColumn();
                var removed = ImGui.Button("X");
                ImGui.PopID();

                if (removed)
                {
                    accent.Rules.RemoveAt(i);
                    config.Save();
                    break;
                }
            }

            ImGui.EndTable();
        }

        if (ImGui.Button("Add rule"))
        {
            accent.Rules.Add(new AccentRule());
            config.Save();
        }

        ImGui.SameLine();
        if (ImGui.Button("Delete accent"))
        {
            if (config.ActiveAccent == accent.Name)
                config.ActiveAccent = "";
            config.CustomAccents.Remove(accent);
            editingAccent = -1;
            config.Save();
        }
    }

    private void AddAccent(Accent accent)
    {
        config.CustomAccents.Add(accent);
        editingAccent = config.CustomAccents.Count - 1;
        config.Save();
    }

    private void SetActiveAccent(string name)
    {
        config.ActiveAccent = name;
        config.Save();
    }

    private string UniqueName(string name)
    {
        var candidate = name;
        for (var n = 2; config.FindAccent(candidate) != null; n++)
            candidate = $"{name} {n}";
        return candidate;
    }



    private static void Section(string title)
    {
        ImGui.Spacing();
        ImGui.TextColored(Purple, title);
        ImGui.Separator();
    }

    private static void Help(string? text)
    {
        if (text == null)
            return;

        ImGui.SameLine();
        ImGui.TextColored(Muted, "(?)");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(text);
    }

    private void DrawCheckbox(string label, bool value, Action<bool> set, string? help = null)
    {
        if (ImGui.Checkbox(label, ref value))
        {
            set(value);
            config.Save();
        }
        Help(help);
    }

    private void DrawTextInput(string label, string value, Action<string> set, string? help = null)
    {
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText(label, ref value, 64))
        {
            set(value);
            config.Save();
        }
        Help(help);
    }

}
