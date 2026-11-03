using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReflavorRP;

public static class RpFormatter
{
    private const int SayChatType = 1; // RaptureShellModule.ChatType

    private static readonly Regex QuoteRegex = new("\"([^\"]*)\"", RegexOptions.Compiled);
    // Match quoted speech first so asterisks inside it remain literal emphasis.
    private static readonly Regex SpeechTokenRegex = new(@"""[^""]*""|\*(?<action>[^*]+)\*", RegexOptions.Compiled);
    private static readonly Regex RoutedCommandRegex = new(
        @"^/(?:(?:party|p|freecompany|fc|alliance|a|reply|r|linkshell|l|cwlinkshell|cwl|(?:linkshell|l|cwlinkshell|cwl)[1-8])\s+|(?:tell|t)\s+(?:""[^""]+""|<[^<>]+>|[^\s/]+[ \t]+[^\s/]+)\s+)(?=\S)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    #region Commands

    // null = let the game handle it
    public static string? Rewrite(string text, int chatType, Configuration config, string? playerName = null)
    {
        if (!text.StartsWith('/'))
        {
            if (chatType != SayChatType)
                return null;
            text = "/say " + text;
        }

        var route = RoutedCommandRegex.Match(text);
        if (route.Success)
        {
            var message = text[route.Length..];
            var rewritten = RewriteCommand(message.StartsWith('/') ? message : "/say " + message, config);
            if (rewritten == null)
                return null;

            // These channels have no native emotes. Keep the destination and put the
            // actor in the message, as /em would have done in local chat.
            var separator = rewritten.IndexOf(' ');
            var body = rewritten[(separator + 1)..];
            if (rewritten.StartsWith("/em "))
            {
                if (string.IsNullOrWhiteSpace(playerName))
                    return null;
                body = playerName + " " + body;
            }
            return route.Value + body;
        }

        return RewriteCommand(text, config);
    }

    private static string? RewriteCommand(string text, Configuration config)
    {
        var parts = text.Split(' ', 2);
        var cmd = parts[0].ToLowerInvariant();
        var args = parts.Length > 1 ? parts[1].Trim() : "";
        if (args == "")
            return null;

        var accent = config.CombinedAccent();

        switch (cmd)
        {
            case "/say":
                // nothing said, just an action
                if (SpeechTokenRegex.Replace(args, match => match.Groups["action"].Success ? "" : match.Value).Trim() == "")
                    return "/em " + Speech(args, null, accent);
                return "/say " + Speech(args, null, accent, capitalize: false);
            case "/me" when config.ReplaceMeEmote:
            case "/ame":
            case "/em":
                return "/em " + Emote(args, accent);
            case "/s" when config.ReplaceSCommand:
                return "/em " + Speech(args, config.VerbShout, accent, '!');
            case "/low":
                return "/em " + Speech(args, config.VerbLow, accent);
            case "/w":
                var whisper = args.Split(' ', 2);
                if (whisper.Length < 2)
                    return null;
                return "/em " + Speech(whisper[1], config.VerbWhisper + " " + whisper[0], accent);
            case "/b":
                return $"/say (( {args} ))";
        }

        return null;
    }

    #endregion

    #region Lines

    private static string Emote(string text, Accent? accent)
    {
        text = QuoteRegex.Replace(text, m => Quote(m.Groups[1].Value, accent));
        return Punctuate(text);
    }

    // hey *waves* how are you? -> shouts, "Hey!" Waves. "How are you?"
    private static string Speech(string text, string? verb, Accent? accent, char end = '.', bool capitalize = true)
    {
        var pieces = new List<(bool Action, string Text)>();
        var previousEnd = 0;
        foreach (Match token in SpeechTokenRegex.Matches(text))
        {
            if (!token.Groups["action"].Success)
                continue;

            pieces.Add((false, text[previousEnd..token.Index]));
            pieces.Add((true, token.Groups["action"].Value));
            previousEnd = token.Index + token.Length;
        }
        pieces.Add((false, text[previousEnd..]));
        var result = new List<string>();

        foreach (var (isAction, content) in pieces)
        {
            var piece = content.Trim();
            if (piece == "")
                continue;

            if (isAction)
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
}
