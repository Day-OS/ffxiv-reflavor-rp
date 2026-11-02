using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Configuration;

namespace ReflavorRP;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // outgoing
    public bool ConversionEnabled { get; set; } = true;
    public bool ReplaceMeEmote { get; set; } = true;
    public string VerbShout { get; set; } = "shouts";
    public string VerbLow { get; set; } = "whispers";
    public string VerbWhisper { get; set; } = "whispers to";

    // incoming
    public bool StyleIncomingChat { get; set; } = true;
    public bool RewriteFullChatLines { get; set; } = true;
    public bool FadeByDistance { get; set; } = true;

    // accents
    public string ActiveAccent { get; set; } = ""; // empty = none
    public List<Accent> CustomAccents { get; set; } = [];

    public Accent? FindAccent(string name) => Accent.Templates.Concat(CustomAccents).FirstOrDefault(a => a.Name == name);

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
