using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ReflavorRP;

public static class RpFormatter
{
    private const int SayChatType = 1; // RaptureShellModule.ChatType

    private static readonly Regex QuoteRegex = new("\"([^\"]*)\"", RegexOptions.Compiled);
    private static readonly Regex ActionRegex = new(@"\*([^*]+)\*", RegexOptions.Compiled);

    #region Commands

    // null = let the game handle it
    public static string? Rewrite(string text, int chatType, Configuration config)
    {
        if (!text.StartsWith('/'))
        {
            if (chatType != SayChatType)
                return null;
            text = "/say " + text;
        }

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
                if (ActionRegex.Replace(args, "").Trim() == "")
                    return "/em " + Speech(args, null, accent);
                return "/say " + Speech(args, null, accent, capitalize: false);
            case "/me" when config.ReplaceMeEmote:
            case "/ame":
            case "/em":
                return "/em " + Emote(args, accent);
            case "/s":
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
        // the regex has a capture group so Split keeps the actions: even = speech, odd = action
        var pieces = ActionRegex.Split(text);
        var result = new List<string>();

        for (var i = 0; i < pieces.Length; i++)
        {
            var piece = pieces[i].Trim();
            if (piece == "")
                continue;

            if (i % 2 == 1)
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
