using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReflavorRP;

[Serializable]
public class AccentRule
{
    public string Pattern { get; set; } = "";
    public string Replacement { get; set; } = "";
}

[Serializable]
public class Accent
{
    public string Name { get; set; } = "";
    public List<AccentRule> Rules { get; set; } = [];

    public static readonly Accent[] Templates =
    [
        new()
        {
            Name = "Frontal lisp",
            Rules =
            [
                new() { Pattern = "s(?!h)", Replacement = "th" },
                new() { Pattern = "z", Replacement = "th" },
                new() { Pattern = "c(?=[eiy])", Replacement = "th" },
            ],
        },
        new()
        {
            Name = "Uwufication",
            Rules =
            [
                new() { Pattern = "[rl]", Replacement = "w" },
                new() { Pattern = "n([aeiou])", Replacement = "ny$1" },
                new() { Pattern = "ove", Replacement = "uv" },
            ],
        },
    ];

    public Accent Copy(string name)
    {
        return new Accent
        {
            Name = name,
            Rules = Rules.Select(r => new AccentRule { Pattern = r.Pattern, Replacement = r.Replacement }).ToList(),
        };
    }

    // rules run in order, case insensitive
    public string Apply(string text)
    {
        foreach (var rule in Rules)
        {
            if (rule.Pattern == "")
                continue;

            try
            {
                var input = text;
                text = Regex.Replace(input, rule.Pattern, m => MatchCase(input, m, m.Result(rule.Replacement)),
                                     RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
            }
            catch
            {
                // bad pattern (the editor flags those) or it timed out, skip it
            }
        }

        return text;
    }

    public static bool IsValidPattern(string pattern)
    {
        try
        {
            _ = new Regex(pattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // keep the case of what was replaced, and keep ALL CAPS words in caps
    private static string MatchCase(string input, Match match, string replacement)
    {
        if (replacement == "")
            return replacement;

        var start = match.Index;
        while (start > 0 && char.IsLetter(input[start - 1]))
            start--;
        var end = match.Index + match.Length;
        while (end < input.Length && char.IsLetter(input[end]))
            end++;

        var word = input[start..end];
        if (word.Count(char.IsLetter) > 1 && word.Where(char.IsLetter).All(char.IsUpper))
            return replacement.ToUpper();
        if (match.Length > 0 && char.IsUpper(match.Value[0]))
            return char.ToUpper(replacement[0]) + replacement[1..];
        return replacement;
    }
}
