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
    private static readonly byte[] EmoteColor = [0xC2, 0xA2, 0xDA];
    private static readonly byte[] SpeechColor = [0xE6, 0xE6, 0xE6];
    private static readonly byte[] OocColor = [0xAF, 0xAF, 0xAF];
    private static readonly byte[] PrefixColor = [0xFF, 0xA6, 0x4D];
    private static readonly byte[] HighlightColor = [0xFF, 0xD1, 0x66];


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

        if (!RpChannels.ShouldStyle(msg.LogKind))
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

        var isTell = RpChannels.IsTell(msg.LogKind);
        var isShout = RpChannels.IsShout(msg.LogKind);
        var localPlayer = objectTable.LocalPlayer;
        // Resolve the author once for chat styling, bubbles, visual cues and speech sound.
        var speaker = RpChannels.IsOutgoingTell(msg.LogKind) ? localPlayer : FindSpeaker(msg.Sender, localPlayer);

        // Only restyle RP shouts; their brightness does not depend on distance.
        if (isShout && !IsRoleplaying(speaker))
            return;

        var fade = RpChannels.IsPublic(msg.LogKind) && !isShout && config.FadeByDistance
            ? DistanceFade(speaker, localPlayer) : 1f;

        var senderName = WithoutWorld(msg.Sender);
        var authorName = senderName;
        var localName = localPlayer?.Name.TextValue;
        if (RpChannels.IsOutgoingTell(msg.LogKind))
        {
            authorName = string.IsNullOrWhiteSpace(localName) ? "Your character" : localName;
        }
        var prefix = new LuminaSeStringBuilder();
        if (isTell)
            prefix.Append("[TELL:").Append(new ReadOnlySeString(senderName.Encode())).Append("] ");
        else if (!RpChannels.IsPublic(msg.LogKind))
            prefix.Append("[").Append(RpChannels.Label(msg.LogKind)).Append("] ");

        var defaultKind = RpChannels.Convention(msg.LogKind, config);
        if (isTell && RpFormatter.IsWrappedOoc(text))
            defaultKind = RpTextKind.Ooc;
        var startsWithNarration = !RpChannels.IsPublic(msg.LogKind) && StartsWithActor(text, authorName.TextValue);
        if (startsWithNarration && defaultKind != RpTextKind.Ooc)
            defaultKind = RpTextKind.Action;
        var pieces = RpFormatter.Parse(text, defaultKind, config);
        if (config.UnquotedSayIsOoc && RpChannels.IsSay(msg.LogKind) && pieces is [{ Speech: true } only] && only.Text == text)
            pieces = [new(RpTextKind.Ooc, text)];
        if (pieces.Count == 0)
            return;
        var isOwnMessage = RpChannels.IsOutgoingTell(msg.LogKind) || senderName.TextValue == localName;
        var styledMessage = new StyledChatMessage(msg, pieces, speaker, isOwnMessage);
        var actionLine = RpChannels.IsEmote(msg.LogKind) || (!isShout &&
            ((!RpChannels.IsPublic(msg.LogKind) && (startsWithNarration || pieces[0].Kind == RpTextKind.Action)) ||
             !styledMessage.HasSpeech));
        var lineKind = styledMessage.IsOoc ? RpTextKind.Ooc : actionLine ? RpTextKind.Action : RpTextKind.Speech;

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

        var includeActionAuthor = !isTell && !startsWithNarration;
        var line = BuildLine(pieces, lineKind, includeActionAuthor, isShout, authorName,
            new ReadOnlySeString(prefix.ToArray()), fade, highlights);
        msg.PreventOriginal();
        chatGui.Print(new XivChatEntry
        {
            Type = XivChatType.Echo,
            Timestamp = msg.Timestamp,
            Message = line,
        });
        // Queue the replacement before scheduling the native bubble for the next frame.
        presentMessage(styledMessage);

        if (config.PlayHighlightSound && !isOwnMessage && highlights != null &&
            pieces.Any(piece => highlights.IsMatch(piece.Text)))
            playHighlightSound();
    }

    // Render the style chosen by the caller using the same parsed text.
    private static SeString BuildLine(IReadOnlyList<RpTextSegment> pieces, RpTextKind lineKind,
        bool includeActionAuthor, bool isShout, SeString authorName, ReadOnlySeString prefix, float fade, Regex? highlights)
    {
        var b = new LuminaSeStringBuilder();
        var sender = new ReadOnlySeString(authorName.Encode());
        var isEmote = lineKind == RpTextKind.Action;
        if (!prefix.IsEmpty)
        {
            PushColor(b, PrefixColor, 1f);
            b.Append(prefix);
            b.PopColor();
        }

        if (lineKind == RpTextKind.Ooc)
        {
            PushColor(b, OocColor, fade);
            b.Append("(( ").Append(sender).Append(": ");
            AppendHighlighted(b, string.Join(" ", pieces.Select(p => p.Text)), highlights, fade);
            b.Append(" ))");
            b.PopColor();
            return SeString.Parse(b.ToArray());
        }

        if (isEmote)
        {
            PushColor(b, EmoteColor, fade);
            b.Append("* ");
            if (includeActionAuthor)
                b.Append(sender).Append(" ");
        }
        else
        {
            PushColor(b, SpeechColor, fade);
            b.Append(sender).Append(isShout ? " shouts: " : " says: ");
        }

        for (var index = 0; index < pieces.Count; index++)
        {
            var segment = pieces[index];
            var isSpeechPiece = segment.Speech;
            var piece = segment.Text;
            if (index > 0)
                b.Append(" ");

            if (segment.Kind == RpTextKind.Ooc)
            {
                PushColor(b, OocColor, fade);
                b.Append("(( ");
                AppendHighlighted(b, piece, highlights, fade);
                b.Append(" ))");
                b.PopColor();
            }
            else if (isEmote && isSpeechPiece)
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

    // Drops the world name but keeps the player link and everything else in the sender,
    // since name plugins (e.g. Character Select+) replace the shown name and color there.
    private static SeString WithoutWorld(SeString sender)
    {
        var player = sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        if (player == null)
            return sender;

        var payloads = new List<Payload>();
        for (var i = 0; i < sender.Payloads.Count; i++)
        {
            var payload = sender.Payloads[i];
            if (payload is IconPayload { Icon: BitmapFontIcon.CrossWorld })
            {
                if (i + 1 < sender.Payloads.Count && sender.Payloads[i + 1] is TextPayload)
                    i++; // the world name
                continue;
            }
            if (payload is TextPayload { Text: { } text } && text.StartsWith(player.PlayerName + "@", StringComparison.Ordinal))
                payload = new TextPayload(player.PlayerName);
            payloads.Add(payload);
        }
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
