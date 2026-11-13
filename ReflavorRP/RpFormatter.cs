using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReflavorRP;

public enum RpTextKind { Speech, Action, Ooc }

public readonly record struct RpTextSegment(RpTextKind Kind, string Text)
{
    public bool Speech => Kind == RpTextKind.Speech;
}

// Interprets and formats each RP message.
public static class RpFormatter
{
    private static readonly char[] Quotes = ['“', '”', '„', '‟', '″', '＂', '«', '»'];
    private static readonly Regex Tokens = new(
        """
          (?<ooc>\(\((?<body>.*?)\)\))
        | (?:(?<!\S)//(?=\s|$)|\bOOC\s*:)(?<body>.*)
        | (?<quote>"(?<spoken>[^"]*)(?:"|$))
        | (?<stars>(?<marker>\*+)(?<action>[^*]+)\k<marker>)
        | (?<square>\[(?<bracket>[^\[\]]+)\])
        | (?<brace>\{(?<bracket>[^{}]+)\})
        | (?<angle><(?<bracket>[^<>]+)>)
        | (?<parenthesis>\((?<aside>[^()]+)\))
        """,
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
    private static readonly Regex Dashes = new(@"(?<!\S)[—–-](?=\s|$)", RegexOptions.Compiled);
    private static readonly Regex NativeToken = new(
        """
        ^(?:
            t|tt|me|mo|focus|f|r|lasttarget|lastenemy|lastattacker|target
          | pos|flag|hp|hpp|mp|mpp|tp|job|class|level
          | wait\.[0-9.]+|se\.\d+|[1-8]
        )$
        """,
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

    private const int SayChatType = 1; // RaptureShellModule.ChatType

    #region Commands

    // null = let the game handle it
    public static string? Rewrite(string text, int chatType, Configuration config, string? playerName = null)
    {
        if (!IsCommand(text))
        {
            if (chatType != SayChatType)
                return null;
            text = "/say " + text;
        }

        var route = RpChannels.MatchRoute(text);
        if (route.Success)
        {
            var message = text[route.Length..];
            var channelKind = RpChannels.OutgoingKind(route.Value, config);
            if (config.AlwaysOocChannel.HasValue && message.StartsWith("/b ", StringComparison.OrdinalIgnoreCase))
                return RewriteCommand(message, config, RpTextKind.Ooc);
            var rewritten = RewriteCommand(IsCommand(message) ? message : "/say " + message,
                config, channelKind);
            if (rewritten == null)
                return null;

            // These channels have no native emotes. Keep the destination and put the
            // actor in the message, as /em would have done in local chat.
            var separator = rewritten.IndexOf(' ');
            var body = rewritten[(separator + 1)..];
            if (rewritten.StartsWith("/em ") && channelKind != RpTextKind.Ooc)
            {
                if (string.IsNullOrWhiteSpace(playerName))
                    return null;
                body = playerName + " " + body;
            }
            return route.Value + body;
        }

        var localKind = RpChannels.OutgoingKind(text, config);
        return RewriteCommand(text, config, localKind);
    }

    private static string? RewriteCommand(string text, Configuration config, RpTextKind defaultKind)
    {
        var parts = text.Split(' ', 2);
        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1].Trim() : "";
        if (args == "")
            return null;

        var accent = config.CombinedAccent();

        List<RpTextSegment> ParseBody(string body, RpTextKind kind) =>
            Parse(body, kind, config);

        string Speech(string body, string? verb, char end = '.', bool capitalize = true) =>
            Render(ParseBody(body, defaultKind), verb, accent, end, capitalize);

        switch (cmd)
        {
            case "/say":
                var pieces = ParseBody(args, defaultKind);
                if (pieces.Count == 0)
                    return null;
                var onlyActions = pieces.All(p => p.Kind == RpTextKind.Action);
                return (onlyActions ? "/em " : "/say ") +
                    Render(pieces, null, accent);
            case "/me" when config.ReplaceMeEmote:
            case "/ame":
            case "/em":
            case "/emote":
            case "/do":
                return "/em " + Render(ParseBody(args, defaultKind == RpTextKind.Ooc ? RpTextKind.Ooc : RpTextKind.Action), null, accent);
            case "/s" when config.ReplaceSCommand:
                return "/em " + Speech(args, config.VerbShout, '!');
            case "/low":
                return "/em " + Speech(args, config.VerbLow);
            case "/w":
                var whisper = args.Split(' ', 2);
                if (whisper.Length < 2)
                    return null;
                return "/em " + Speech(whisper[1], config.VerbWhisper + " " + whisper[0]);
            case "/b":
                return $"{RpChannels.OocCommand(config)} (( {args} ))";
        }

        return null;
    }

    #endregion

    #region Lines

    private static string Render(IReadOnlyList<RpTextSegment> pieces, string? verb, Accent? accent,
        char end = '.', bool capitalize = false)
    {
        var result = new List<string>();
        foreach (var segment in pieces)
        {
            var piece = segment.Text.Trim();
            if (piece == "")
                continue;

            if (segment.Kind == RpTextKind.Ooc)
            {
                result.Add("(( " + piece + " ))");
                continue;
            }
            if (segment.Kind == RpTextKind.Action)
            {
                piece = Punctuate(piece);
            }
            else
            {
                piece = Quote(piece, accent, end);
                if (verb != null)
                {
                    piece = verb + ", " + piece;
                    verb = null;
                }
            }

            result.Add(result.Count > 0 && capitalize ? Capitalize(piece) : piece);
        }

        return string.Join(" ", result);
    }

    #endregion

    #region Text helpers

    // only speech goes through here, so this is where the accent gets applied
    private static string Quote(string text, Accent? accent, char end = '.')
    {
        text = text.Trim().Trim('"', '“', '”').Trim();
        if (accent != null)
            text = accent.Apply(text);
        return "\"" + Punctuate(Capitalize(text), end) + "\"";
    }

    private static string Punctuate(string text, char end = '.')
    {
        if (text == "")
            return text;
        if (end != '.' && text.EndsWith('.') && !text.EndsWith(".."))
            return text[..^1] + end;
        if (".?!…\"".Contains(text[^1]))
            return text;
        return text + end;
    }

    private static string Capitalize(string text)
    {
        if (text == "")
            return text;
        return char.ToUpper(text[0]) + text[1..];
    }

    #endregion

    #region Parsing

    // Splits one message into speech, action and OOC segments. Channel rules belong to the caller.
    public static List<RpTextSegment> Parse(string text, RpTextKind defaultKind, Configuration config)
    {
        text = text.Trim();
        if (defaultKind == RpTextKind.Ooc)
            return [new(RpTextKind.Ooc, UnwrapOoc(text))];

        text = NormalizeQuotes(text);

        if (text.EndsWith(" //", StringComparison.Ordinal) && !text[..^2].Contains("//", StringComparison.Ordinal))
            return [new(RpTextKind.Ooc, UnwrapOoc(text[..^2]))];

        var tokens = Tokens.Matches(text)
            .Select(match => (Match: match, Role: Classify(match, config)))
            .Where(token => token.Role.Kind.HasValue)
            .ToList();
        var hasQuotes = tokens.Any(token => token.Role.Kind == RpTextKind.Speech);
        var hasActions = tokens.Any(token => token.Role.Kind == RpTextKind.Action);
        var freeKind = hasQuotes && !hasActions ? RpTextKind.Action : defaultKind;
        var dashes = Dashes.Matches(text);
        var literary = !hasQuotes && !hasActions && dashes.Count > 0 && dashes[0].Index == 0;
        var result = new List<RpTextSegment>();

        void Add(RpTextKind kind, string value)
        {
            value = value.Trim();
            if (value.Length > 0)
                result.Add(new(kind, value));
        }

        void AddFree(string value)
        {
            if (hasActions || literary)
            {
                var previous = 0;
                foreach (Match dash in Dashes.Matches(value))
                {
                    Add(freeKind, value[previous..dash.Index]);
                    if (literary)
                        freeKind = freeKind == RpTextKind.Speech ? RpTextKind.Action : RpTextKind.Speech;
                    previous = dash.Index + dash.Length;
                }
                Add(freeKind, value[previous..]);
            }
            else
                Add(freeKind, value.Trim().Trim('*').Trim());
        }

        if (literary)
            freeKind = RpTextKind.Action; // The opening dash switches into dialogue.
        var end = 0;
        foreach (var (token, (kind, value)) in tokens)
        {
            AddFree(text[end..token.Index]);
            end = token.Index + token.Length;
            if (kind == RpTextKind.Action)
                result.AddRange(Parse(value, RpTextKind.Action, config)); // actions may contain OOC
            else
                Add(kind!.Value, value);
        }
        AddFree(text[end..]);
        return result;
    }

    // Kind of one notation token; null leaves it as literal text (single parentheses, <t>, <se.1>...).
    private static (RpTextKind? Kind, string Text) Classify(Match token, Configuration config)
    {
        var groups = token.Groups;
        if (groups["body"].Success)
            return (RpTextKind.Ooc, groups["body"].Value);
        if (groups["quote"].Success)
            return (RpTextKind.Speech, groups["spoken"].Value);
        if (groups["stars"].Success)
            return (RpTextKind.Action, groups["action"].Value);
        if (groups["parenthesis"].Success)
            return (config.ParenthesesAreOoc ? RpTextKind.Ooc : null, groups["aside"].Value);
        var body = groups["bracket"].Value.Trim();
        if (body.StartsWith("OOC:", StringComparison.OrdinalIgnoreCase))
            return (RpTextKind.Ooc, body[4..]);
        if (groups["angle"].Success && NativeToken.IsMatch(groups["bracket"].Value))
            return (null, body);
        if (groups["square"].Success && config.BracketsAreOoc)
            return (RpTextKind.Ooc, body);
        return (RpTextKind.Action, body);
    }

    // Commands start with one slash; "//" is an OOC marker.
    private static bool IsCommand(string text) => text.StartsWith('/') && !text.StartsWith("//");

    private static string NormalizeQuotes(string text)
    {
        foreach (var quote in Quotes)
            text = text.Replace(quote, '"');
        return text;
    }

    public static bool IsWrappedOoc(string text) =>
        text.StartsWith("((", StringComparison.Ordinal) && text.EndsWith("))", StringComparison.Ordinal);

    private static string UnwrapOoc(string text)
    {
        text = text.Trim();
        if (IsWrappedOoc(text))
            text = text[2..^2];
        else if (text.StartsWith("//", StringComparison.Ordinal))
            text = text[2..];
        else if (text.StartsWith("OOC:", StringComparison.OrdinalIgnoreCase))
            text = text[4..];
        return text.Trim();
    }

    #endregion
}
