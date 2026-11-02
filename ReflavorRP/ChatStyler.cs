using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Lumina.Text.ReadOnly;
using LuminaSeStringBuilder = Lumina.Text.SeStringBuilder;

namespace ReflavorRP;

// GTA World look for chat we receive. Only changes what we see, nothing gets sent.
public sealed class ChatStyler : IDisposable
{
    private static readonly char[] QuoteChars = ['"', '“', '”', '„', '‟', '″', '＂', '«', '»'];
    private static readonly Regex OocRegex = new(@"^\(\((.*)\)\)$", RegexOptions.Compiled);
    private static readonly Regex ActionRegex = new(@"\*([^*]+)\*", RegexOptions.Compiled);

    private static readonly byte[] EmoteColor = [0xC2, 0xA2, 0xDA];
    private static readonly byte[] SpeechColor = [0xE6, 0xE6, 0xE6];
    private static readonly byte[] OocColor = [0xAF, 0xAF, 0xAF];

    private readonly IChatGui chatGui;
    private readonly IObjectTable objectTable;
    private readonly Configuration config;

    public ChatStyler(IChatGui chatGui, IObjectTable objectTable, Configuration config)
    {
        this.chatGui = chatGui;
        this.objectTable = objectTable;
        this.config = config;
        chatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        chatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(IHandleableChatMessage msg)
    {
        if (!config.StyleIncomingChat || msg.IsHandled)
            return;

        // we rebuild from plain text, so skip anything with item links, auto-translate etc.
        // colors are fine to lose (ChatProximity adds those)
        var unsupported = msg.Message.Payloads.FirstOrDefault(p => p is not (TextPayload or UIForegroundPayload or UIGlowPayload or RawPayload));
        if (unsupported != null)
        {
            Plugin.Log.Debug($"Skipping {msg.LogKind} message with a {unsupported.Type} payload");
            return;
        }

        var text = msg.Message.TextValue.Trim();
        if (text == "")
            return;

        var full = config.RewriteFullChatLines;
        var fade = config.FadeByDistance ? DistanceFade(msg.Sender) : 1f;
        var name = WithoutWorld(msg.Sender);
        var sender = new ReadOnlySeString(name.Encode());
        var b = new LuminaSeStringBuilder();

        switch (msg.LogKind)
        {
            // no quotes and no *actions*, probably OOC
            case XivChatType.Say when text.IndexOfAny(QuoteChars) < 0 && !ActionRegex.IsMatch(text):
                var ooc = OocRegex.Match(text);
                var oocText = ooc.Success ? ooc.Groups[1].Value.Trim() : text;
                PushColor(b, OocColor, fade);
                if (full)
                    b.Append("(( ").Append(sender).Append(": " + oocText + " ))");
                else
                    b.Append("(( " + oocText + " ))");
                b.PopColor();
                break;

            case XivChatType.Say:
                PushColor(b, SpeechColor, fade);
                if (full)
                    b.Append(sender).Append(" says: ");
                AppendSay(b, text, fade);
                b.PopColor();
                break;

            case XivChatType.Shout:
            case XivChatType.Yell:
                PushColor(b, SpeechColor, fade);
                if (full)
                    b.Append(sender).Append(" shouts: ");
                b.Append(Unquote(text));
                b.PopColor();
                break;

            case XivChatType.CustomEmote:
                PushColor(b, EmoteColor, fade);
                if (full)
                    b.Append("* ").Append(sender).Append(" ");
                AppendEmote(b, text, fade);
                b.PopColor();
                break;

            default:
                return;
        }

        var line = SeString.Parse(b.ToArray());
        if (!full)
        {
            msg.Sender = name;
            msg.Message = line;
            return;
        }

        // can't change how the game lays out "Name: text", so hide it and print our own line as echo
        msg.PreventOriginal();
        chatGui.Print(new XivChatEntry
        {
            Type = XivChatType.Echo,
            Timestamp = msg.Timestamp,
            Message = line,
        });
    }

    private static void AppendEmote(LuminaSeStringBuilder b, string text, float fade)
    {
        foreach (var (speech, piece) in SplitSpeech(text))
        {
            if (!speech)
            {
                b.Append(piece);
                continue;
            }

            PushColor(b, SpeechColor, fade);
            b.Append("\"" + piece + "\"");
            b.PopColor();
        }
    }

    private static void AppendSay(LuminaSeStringBuilder b, string text, float fade)
    {
        // no quotes = GTA style, only the *actions* aren't speech
        var pieces = text.IndexOfAny(QuoteChars) >= 0
            ? SplitSpeech(text)
            : ActionRegex.Split(text).Select((piece, i) => (i % 2 == 0, piece)).ToList();

        var first = true;
        foreach (var (speech, rawPiece) in pieces)
        {
            var piece = speech ? rawPiece.Trim() : rawPiece.Trim().Trim('*').Trim();
            if (piece == "")
                continue;

            if (!first)
                b.Append(" ");
            first = false;

            if (speech)
            {
                b.Append(piece);
                continue;
            }

            PushColor(b, EmoteColor, fade);
            b.Append("*" + piece + "*");
            b.PopColor();
        }
    }

    // any quote mark flips speech on/off, people mix quote styles a lot.
    // odd count = a post split over several messages, a leading ” means we start mid-speech
    private static List<(bool Speech, string Text)> SplitSpeech(string text)
    {
        var quotes = text.Count(c => QuoteChars.Contains(c));
        var first = text.IndexOfAny(QuoteChars);
        var speech = quotes % 2 == 1 && text[first] == '”';

        var pieces = new List<(bool, string)>();
        var start = 0;
        for (var i = 0; i < text.Length; i++)
        {
            if (!QuoteChars.Contains(text[i]))
                continue;

            pieces.Add((speech, text[start..i]));
            speech = !speech;
            start = i + 1;
        }
        pieces.Add((speech, text[start..]));

        return pieces;
    }

    // drops the world name but keeps the player link
    private static SeString WithoutWorld(SeString sender)
    {
        var index = sender.Payloads.FindIndex(p => p is PlayerPayload);
        if (index < 0)
            return sender;

        var player = (PlayerPayload)sender.Payloads[index];
        var payloads = sender.Payloads.Take(index + 1).ToList();
        payloads.Add(new TextPayload(player.PlayerName));
        payloads.Add(RawPayload.LinkTerminator);
        return new SeString(payloads);
    }

    private float DistanceFade(SeString sender)
    {
        // our own lines have no player link
        var player = sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        var me = objectTable.LocalPlayer;
        if (player == null || me == null)
            return 1f;

        var speaker = objectTable.PlayerObjects
            .OfType<IPlayerCharacter>()
            .FirstOrDefault(p => p.Name.TextValue == player.PlayerName && p.HomeWorld.RowId == player.World.RowId);
        if (speaker == null)
            return 1f;

        var distance = Vector3.Distance(me.Position, speaker.Position);
        if (distance < 4)
            return 1f;
        if (distance < 8)
            return 0.87f;
        if (distance < 12)
            return 0.74f;
        if (distance < 16)
            return 0.61f;
        return 0.48f;
    }

    private static void PushColor(LuminaSeStringBuilder b, byte[] rgb, float fade)
    {
        b.PushColorRgba((byte)(rgb[0] * fade), (byte)(rgb[1] * fade), (byte)(rgb[2] * fade), 0xFF);
    }

    private static string Unquote(string text)
    {
        return text.Trim(QuoteChars).Trim();
    }
}
