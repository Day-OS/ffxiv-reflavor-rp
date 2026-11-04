using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Configuration;

namespace ReflavorRP;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;
    public bool Enabled { get; set; } = true;

    // outgoing
    public bool ConversionEnabled { get; set; } = true;
    public bool ReplaceMeEmote { get; set; } = true;
    public bool ReplaceSCommand { get; set; } = true;
    public string VerbShout { get; set; } = "shouts";
    public string VerbLow { get; set; } = "whispers";
    public string VerbWhisper { get; set; } = "whispers to";

    // incoming
    public bool StyleIncomingChat { get; set; } = true;
    public bool FadeByDistance { get; set; } = true;
    public bool AlternativeChatBubbles { get; set; } = true;
    public float ChatBubbleReadingSpeed { get; set; } = 10f;
    public bool HideDistantPlayerNames { get; set; } = false;
    public float PlayerNameplateDistance { get; set; } = 20f;
    public bool ShowSpeechDirection { get; set; } = false;
    public float SpeechDirectionRange { get; set; } = 30f;
    public Vector3 SpeechDirectionColor { get; set; } = new(0.76f, 0.64f, 0.85f);
    public float SpeechDirectionOpacity { get; set; } = 0.70f;
    public float SpeechDirectionWidth { get; set; } = 250f;
    public float SpeechDirectionHeight { get; set; } = 1.0f;
    public bool PlaySpeechSound { get; set; } = false;
    public float SpeechSoundVolume { get; set; } = 1f;
    public float SpeechSoundRange { get; set; } = 30f;

    // Migrate the sound settings from when they were tied to the offscreen indicator.
    public bool PlaySpeechDirectionSound { set => PlaySpeechSound = value; }
    public float SpeechDirectionSoundVolume { set => SpeechSoundVolume = value; }
    public bool? NativeChatBubblesBeforeOverride { get; set; }
    public bool HighlightCharacterName { get; set; } = true;
    public string HighlightWords { get; set; } = "";
    public bool PlayHighlightSound { get; set; } = false;

    // accents
    public List<string> ActiveAccents { get; set; } = []; // run in this order
    public List<Accent> CustomAccents { get; set; } = [];

    // configs from before combining had a single active accent, this moves it into the list on load
    public string ActiveAccent
    {
        set
        {
            if (value != "" && !ActiveAccents.Contains(value))
                ActiveAccents.Add(value);
        }
    }

    public Accent? FindAccent(string name) => Accent.Templates.Concat(CustomAccents).FirstOrDefault(a => a.Name == name);

    // all active accents as one, null if there are none
    public Accent? CombinedAccent()
    {
        var active = ActiveAccents.Select(FindAccent).OfType<Accent>().ToList();
        if (active.Count == 0)
            return null;
        return Accent.Combine(string.Join(" + ", active.Select(a => a.Name)), active);
    }

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
