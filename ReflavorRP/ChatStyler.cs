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
using OnlineStatusFlags = FFXIVClientStructs.FFXIV.Client.UI.Info.InfoProxyCommonList.CharacterData.OnlineStatus;

namespace ReflavorRP;

// GTA World look for chat we receive. Only changes what we see, nothing gets sent.
public sealed class ChatStyler : IDisposable
{
    private static readonly char[] QuoteChars = ['"', '“', '”', '„', '‟', '″', '\uff02', '«', '»'];
    private static readonly Regex OocRegex = new(@"^\(\((.*)\)\)$", RegexOptions.Compiled);
    private static readonly Regex ActionRegex = new(@"(?<markers>\*+)(?<action>[^*]+)\k<markers>", RegexOptions.Compiled);

    private static readonly byte[] EmoteColor = [0xC2, 0xA2, 0xDA];
    private static readonly byte[] SpeechColor = [0xE6, 0xE6, 0xE6];
    private static readonly byte[] OocColor = [0xAF, 0xAF, 0xAF];
    private static readonly byte[] PrefixColor = [0xFF, 0xA6, 0x4D];
    private static readonly byte[] HighlightColor = [0xFF, 0xD1, 0x66];

    private enum LineStyle { Speech, Emote, Shout, Ooc }

    private readonly IChatGui chatGui;
    private readonly IObjectTable objectTable;
    private readonly Configuration config;
    private readonly IPluginLog log;
    private readonly Action<StyledChatMessage> presentMessage;
    private readonly Action playHighlightSound;

    public ChatStyler(IChatGui chatGui, IObjectTable objectTable, Configuration config, IPluginLog log,
        Action<StyledChatMessage> presentMessage, Action playHighlightSound)
    {
        this.chatGui = chatGui;
        this.objectTable = objectTable;
        this.config = config;
        this.log = log;
        this.presentMessage = presentMessage;
        this.playHighlightSound = playHighlightSound;
        chatGui.ChatMessage += OnChatMessage;
    }

    public void Dispose()
    {
        chatGui.ChatMessage -= OnChatMessage;
    }

    private void OnChatMessage(IHandleableChatMessage msg)
    {
        if (!config.Enabled || !config.StyleIncomingChat || msg.IsHandled)
            return;

        if (msg.LogKind is not (XivChatType.Say or XivChatType.CustomEmote or XivChatType.Shout
            or XivChatType.Yell or XivChatType.TellIncoming or XivChatType.TellOutgoing))
            return;

        // we rebuild from plain text, so skip anything with item links, auto-translate etc.
        var unsupported = msg.Message.Payloads.FirstOrDefault(p => p is not (TextPayload or UIForegroundPayload or UIGlowPayload or RawPayload));
        if (unsupported != null)
        {
            log.Debug($"Skipping {msg.LogKind} message with a {unsupported.Type} payload");
            return;
        }

        var text = msg.Message.TextValue.Trim();
        if (text == "")
            return;

        var isTell = msg.LogKind is XivChatType.TellIncoming or XivChatType.TellOutgoing;
        var isShout = msg.LogKind is XivChatType.Shout or XivChatType.Yell;
        var localPlayer = objectTable.LocalPlayer;
        // Resolve the author once for chat styling, bubbles, visual cues and speech sound.
        var speaker = msg.LogKind == XivChatType.TellOutgoing ? localPlayer : FindSpeaker(msg.Sender, localPlayer);

        // Only restyle RP shouts; their brightness does not depend on distance.
        if (isShout && !IsRoleplaying(speaker))
            return;

        var fade = !isTell && !isShout && config.FadeByDistance ? DistanceFade(speaker, localPlayer) : 1f;

        var senderName = WithoutWorld(msg.Sender);
        var authorName = senderName;
        var localName = localPlayer?.Name.TextValue;
        if (msg.LogKind == XivChatType.TellOutgoing)
        {
            authorName = string.IsNullOrWhiteSpace(localName) ? "Your character" : localName;
        }
        var prefix = new LuminaSeStringBuilder();
        if (isTell)
            prefix.Append("[TELL:").Append(new ReadOnlySeString(senderName.Encode())).Append("] ");

        var ooc = OocRegex.Match(text);
        var pieces = new List<(bool Speech, string Text)>();
        var quotedParts = text.Split(QuoteChars);
        var hasQuotes = quotedParts.Length > 1;
        var actions = ActionRegex.Matches(text);
        var startsWithNarration = isTell && (text.StartsWith('*') || StartsWithActor(text, authorName.TextValue));

        if (hasQuotes)
        {
            var quoteCount = quotedParts.Length - 1;
            var firstQuote = text[text.IndexOfAny(QuoteChars)];
            // An unmatched closing curly quote means the post starts mid-speech.
            var inSpeech = firstQuote == '”' && quoteCount % 2 != 0;
            foreach (var part in quotedParts)
            {
                pieces.Add((Speech: inSpeech, Text: part));
                inSpeech = !inSpeech;
            }
        }
        else if (msg.LogKind == XivChatType.CustomEmote || startsWithNarration)
        {
            pieces.Add((Speech: false, Text: text));
        }
        else
        {
            var previousEnd = 0;
            foreach (Match action in actions)
            {
                pieces.Add((Speech: true, Text: text[previousEnd..action.Index]));
                pieces.Add((Speech: false, Text: action.Groups["action"].Value));
                previousEnd = action.Index + action.Length;
            }
            pieces.Add((Speech: true, Text: text[previousEnd..]));
        }

        var first = pieces.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Text));
        var style = msg.LogKind switch
        {
            XivChatType.CustomEmote => LineStyle.Emote,
            XivChatType.Shout or XivChatType.Yell => LineStyle.Shout,
            _ when isTell && ooc.Success => LineStyle.Ooc,
            _ when isTell && (startsWithNarration || (first.Text != null && !first.Speech)) => LineStyle.Emote,
            _ when !isTell && !hasQuotes && actions.Count == 0 => LineStyle.Ooc,
            _ => LineStyle.Speech,
        };
        if (style == LineStyle.Ooc)
        {
            var body = ooc.Success ? ooc.Groups[1].Value.Trim() : text;
            pieces.Clear();
            pieces.Add((Speech: false, Text: body));
        }
        else
        {
            for (var i = 0; i < pieces.Count; i++)
            {
                var (speech, piece) = pieces[i];
                piece = piece.Trim();
                if (!speech)
                    piece = piece.Trim('*').Trim();
                pieces[i] = (speech, piece);
            }
            pieces.RemoveAll(piece => piece.Text == "");
        }

        var words = config.HighlightWords.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        if (config.HighlightCharacterName && !string.IsNullOrWhiteSpace(localName))
            words.AddRange(localName.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        Regex? highlights = null;
        if (words.Count > 0)
        {
            var alternatives = words.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(word => word.Length).Select(Regex.Escape);
            highlights = new Regex(string.Join("|", alternatives),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        var line = BuildLine(pieces, style, isTell, authorName,
            new ReadOnlySeString(prefix.ToArray()), fade, highlights);
        msg.PreventOriginal();
        chatGui.Print(new XivChatEntry
        {
            Type = XivChatType.Echo,
            Timestamp = msg.Timestamp,
            Message = line,
        });
        // Queue the replacement before scheduling the native bubble for the next frame.
        var isOwnMessage = msg.LogKind == XivChatType.TellOutgoing || senderName.TextValue == localName;
        presentMessage(new StyledChatMessage(msg, pieces, style == LineStyle.Ooc, speaker, isOwnMessage));

        if (config.PlayHighlightSound && !isOwnMessage && highlights != null &&
            pieces.Any(piece => highlights.IsMatch(piece.Text)))
            playHighlightSound();
    }

    // Render the style chosen by the caller using the same parsed text.
    private static SeString BuildLine(IReadOnlyList<(bool Speech, string Text)> pieces, LineStyle style,
        bool isTell, SeString authorName, ReadOnlySeString prefix, float fade, Regex? highlights)
    {
        var b = new LuminaSeStringBuilder();
        var sender = new ReadOnlySeString(authorName.Encode());
        var isEmote = style == LineStyle.Emote;
        if (!prefix.IsEmpty)
        {
            PushColor(b, PrefixColor, 1f);
            b.Append(prefix);
            b.PopColor();
        }

        if (style == LineStyle.Ooc)
        {
            PushColor(b, OocColor, fade);
            b.Append("(( ").Append(sender).Append(": ");
            AppendHighlighted(b, pieces[0].Text, highlights, fade);
            b.Append(" ))");
            b.PopColor();
            return SeString.Parse(b.ToArray());
        }

        if (isEmote)
        {
            PushColor(b, EmoteColor, fade);
            b.Append("* ");
            if (!isTell)
                b.Append(sender).Append(" ");
        }
        else
        {
            PushColor(b, SpeechColor, fade);
            b.Append(sender).Append(style == LineStyle.Shout ? " shouts: " : " says: ");
        }

        for (var index = 0; index < pieces.Count; index++)
        {
            var (isSpeechPiece, piece) = pieces[index];
            if (index > 0)
                b.Append(" ");

            if (isEmote && isSpeechPiece)
            {
                PushColor(b, SpeechColor, fade);
                b.Append("\"");
                AppendHighlighted(b, piece, highlights, fade);
                b.Append("\"");
                b.PopColor();
            }
            else if (!isEmote && !isSpeechPiece)
            {
                PushColor(b, EmoteColor, fade);
                b.Append("*");
                AppendHighlighted(b, piece, highlights, fade);
                b.Append("*");
                b.PopColor();
            }
            else
            {
                AppendHighlighted(b, piece, highlights, fade);
            }
        }

        b.PopColor();
        return SeString.Parse(b.ToArray());
    }

    private static void AppendHighlighted(LuminaSeStringBuilder builder, string text, Regex? highlights, float fade)
    {
        if (highlights == null)
        {
            builder.Append(text);
            return;
        }

        var previousEnd = 0;
        foreach (Match match in highlights.Matches(text))
        {
            builder.Append(text[previousEnd..match.Index]);
            PushColor(builder, HighlightColor, fade);
            builder.Append(match.Value);
            builder.PopColor();
            previousEnd = match.Index + match.Length;
        }
        builder.Append(text[previousEnd..]);
    }

    private static bool StartsWithActor(string text, string? authorName)
    {
        if (string.IsNullOrWhiteSpace(authorName))
            return false;

        var firstName = authorName.Split(' ', 2)[0];
        if (!text.StartsWith(firstName, StringComparison.OrdinalIgnoreCase))
            return false;

        if (text.Length == firstName.Length)
            return true;

        var nextCharacter = text[firstName.Length];
        if (char.IsWhiteSpace(nextCharacter))
            return true;

        // A possessive such as Legaia's also starts with the author's name.
        if (nextCharacter == '\'')
            return true;

        if (nextCharacter == '\u2019')
            return true;

        return false;
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

    // null when they're too far away to be loaded
    private IPlayerCharacter? FindSpeaker(SeString sender, IPlayerCharacter? localPlayer)
    {
        // our own lines have no player link
        var player = sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        if (player == null)
            return localPlayer;

        return objectTable.PlayerObjects
            .OfType<IPlayerCharacter>()
            .FirstOrDefault(p => p.Name.TextValue == player.PlayerName && p.HomeWorld.RowId == player.World.RowId);
    }

    private static bool IsRoleplaying(IPlayerCharacter? speaker)
    {
        // OnlineStatus flags use the sheet's row ID as the bit position.
        var roleplayingStatusRowId = (uint)BitOperations.TrailingZeroCount((ulong)OnlineStatusFlags.RolePlaying);
        return speaker?.OnlineStatus.RowId == roleplayingStatusRowId;
    }

    private static float DistanceFade(IPlayerCharacter? speaker, IPlayerCharacter? localPlayer)
    {
        if (speaker == null)
            return 1f;
        if (localPlayer == null)
            return 1f;

        var distance = Vector3.Distance(localPlayer.Position, speaker.Position);
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

}
