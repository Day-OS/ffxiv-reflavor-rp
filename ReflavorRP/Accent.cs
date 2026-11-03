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
    // percent of matches that get replaced
    public int Chance { get; set; } = 100;
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
                // a letter on its own is said by its name, spelling "S A D" is "ess A D" -> "'eth' A D",
                // quoted to show it's a struggle to get out
                new() { Pattern = @"(?<![\w'’])s\b", Replacement = "'eth'" },
                new() { Pattern = @"(?<![\w'’])[zc]\b", Replacement = "'thee'" },
                new() { Pattern = @"(?<![\w'’])x\b", Replacement = "'ekth'" },
                // xylophone, psychic (silent p)
                new() { Pattern = @"\b(?:x|ps)", Replacement = "th" },
                // anxious, luxury,
                new() { Pattern = "x(?=i[aou]|ur|ual)", Replacement = "ksh" },
                // box, excellent, accept, exhausted (silent h), but not Louisoix (French, silent x)
                new() { Pattern = "[xc]c(?=[eiy])|(?<!oi)xh?", Replacement = "kth" },
                // school (but not mischief)
                new() { Pattern = @"\bsch", Replacement = "thk" },
                // any run of s/z/soft c sounds is one "th" (miss, science, pizza),
                // unless what comes after makes it not an s sound
                new()
                {
                    Pattern = "(?<![sz])(?>(?:[sz]|c(?=[eiy]))+)(?!" +
                              "h" +                        // shoe, fish
                              @"|(?<=[^\Wszc][szc]+)i[aou]" + // mission, vision, special, delicious, Asian (but not Scion)
                              "|ual" +                     // usual, casual, visual
                              "|ient" +                    // ancient, efficient
                              "|(?<=c)ean" +               // ocean
                              "|ur(?:e|anc)" +             // sure, measure, azure, insurance
                              "|ugar" +                    // sugar
                              "|(?<=ss)ue" +               // issue, tissue (but not sue, pursue)
                              @"|(?<=\ba?is)l(?:e|and)" +  // island, aisle (silent s)
                              ")",
                    Replacement = "th",
                },
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
        new()
        {
            Name = "Miqo'te nya",
            Rules =
            [
                // no -> nyo, now -> nyow, nice -> nyice
                new() { Pattern = "n([aeiou])", Replacement = "ny$1" },
            ],
        },
        new()
        {
            Name = "Rolled R",
            Rules =
            [
                // rolled r, one more r than there was: brother -> brrotherr, sorry -> sorrry
                new() { Pattern = "r+", Replacement = "$0r" },
            ],
        },
        new()
        {
            Name = "Moogle",
            Rules =
            [
                // a kupo at the end of every sentence, unless there already is one
                new() { Pattern = @"(?<=\w)(?<!kupo)(?=[.!?…]*$|[.!?…]+\s)", Replacement = ", kupo" },
            ],
        },
        new()
        {
            Name = "Ishgardian noble",
            Rules =
            [
                new() { Pattern = @"\bare you\b", Replacement = "art thou" },
                new() { Pattern = @"\byou are\b|\byou['’]re\b", Replacement = "thou art" },
                new() { Pattern = @"\bdo you\b", Replacement = "dost thou" },
                new() { Pattern = @"\bhave you\b", Replacement = "hast thou" },
                new() { Pattern = @"\bwill you\b", Replacement = "wilt thou" },
                // thank you, I love you
                new() { Pattern = @"\byou\b(?=\s*(?:[.!?,;]|$))", Replacement = "thee" },
                new() { Pattern = @"\byou\b", Replacement = "thou" },
                new() { Pattern = @"\byourself\b", Replacement = "thyself" },
                new() { Pattern = @"\byours\b", Replacement = "thine" },
                // thine eyes
                new() { Pattern = @"\byour(?=\s+[aeiou])", Replacement = "thine" },
                new() { Pattern = @"\byour\b", Replacement = "thy" },
                new() { Pattern = @"\bdoes\b", Replacement = "doth" },
                new() { Pattern = @"\bhas\b", Replacement = "hath" },
                new() { Pattern = @"\bit(?: i|['’])s\b", Replacement = "'tis" },
                new() { Pattern = @"\b(?:hello|hi)\b", Replacement = "hail" },
                new() { Pattern = @"\byes\b", Replacement = "aye" },
                new() { Pattern = @"\b(?:maybe|perhaps)\b", Replacement = "perchance" },
                new() { Pattern = @"\boften\b", Replacement = "oft" },
            ],
        },
        new()
        {
            Name = "Limsa pirate",
            Rules =
            [
                new() { Pattern = @"\b(?:hello|hi|hey)\b", Replacement = "ahoy" },
                new() { Pattern = @"\bfriend(s?)\b", Replacement = "matey$1" },
                new() { Pattern = @"\byes\b", Replacement = "aye" },
                new() { Pattern = @"\byour\b", Replacement = "yer" },
                new() { Pattern = @"\byou\b", Replacement = "ye" },
                new() { Pattern = @"\bmy\b", Replacement = "me" },
                new() { Pattern = @"\b(?:is|are)\b", Replacement = "be" },
                new() { Pattern = @"\bof\b", Replacement = "o'" },
                new() { Pattern = @"\bfor\b", Replacement = "fer" },
                // goin', nothin', but not thing or king
                new() { Pattern = @"(?<=[aeiouy]\w*)ing\b", Replacement = "in'" },
                // dropped h: 'ow, 'ere
                new() { Pattern = @"\bh([aeiou])", Replacement = "'$1" },
            ],
        },
        new()
        {
            Name = "Drunk",
            Rules =
            [
                new() { Pattern = "s+(?!h)", Replacement = "sh" },
                // goin', nothin', but not thing or king
                new() { Pattern = @"(?<=[aeiouy]\w*)ing\b", Replacement = "in'" },
            ],
        },
        new()
        {
            Name = "Stuffy nose",
            Rules =
            [
                // know (silent k)
                new() { Pattern = @"\bkn", Replacement = "d" },
                // morning, thing
                new() { Pattern = @"ng\b", Replacement = "g" },
                new() { Pattern = "m", Replacement = "b" },
                new() { Pattern = "n", Replacement = "d" },
            ],
        },
        new()
        {
            Name = "Hissing",
            Rules =
            [
                new() { Pattern = "(?>(?:s|c(?=[eiy]))+)(?!h)", Replacement = "sss" },
                new() { Pattern = "z+", Replacement = "zzz" },
            ],
        },
        new()
        {
            Name = "Stutter",
            Rules =
            [
                // I-I, W-What, Th-This, at the start of a word (not the t in don't)
                new() { Pattern = @"(?<![\w'’])([cstwp]h|[a-z])", Replacement = "$1-$1", Chance = 25 },
            ],
        },
        new()
        {
            Name = "Tough talk",
            Rules =
            [
                // the, this, that, they, them... (voiced th)
                new() { Pattern = @"\bth(?=(?:e|is|at|ey|em|eir|ere|ese|ose|en|an|ough|us)\b)", Replacement = "d" },
                // mother, other, together, but not thermal
                new() { Pattern = @"\Bth(?=er)", Replacement = "d" },
                // thing, think, with, both
                new() { Pattern = "th", Replacement = "t" },
            ],
        },
    ];

    // running accents one after another is the same as one accent with all their rules in order
    public static Accent Combine(string name, IEnumerable<Accent> accents)
    {
        return new Accent { Name = name, Rules = accents.SelectMany(a => a.Rules).ToList() };
    }

    public Accent Copy(string name)
    {
        return new Accent
        {
            Name = name,
            Rules = Rules.Select(r => new AccentRule { Pattern = r.Pattern, Replacement = r.Replacement, Chance = r.Chance }).ToList(),
        };
    }

    // rules run in order, case insensitive
    public string Apply(string text, Random? random = null)
    {
        random ??= Random.Shared;

        foreach (var rule in Rules)
        {
            if (rule.Pattern == "")
                continue;

            try
            {
                var regex = new Regex(rule.Pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
                var input = text;

                // m.Result fills in $1 etc, MatchCase fixes the casing
                string ReplaceMatch(Match m)
                {
                    if (random.Next(100) >= rule.Chance)
                        return m.Value;
                    return MatchCase(input, m, m.Result(rule.Replacement));
                }

                text = regex.Replace(input, ReplaceMatch);
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

        var letters = WordAround(input, match).Where(char.IsLetter).ToList();
        var shouting = letters.Count > 1 && letters.All(char.IsUpper);
        if (shouting)
            return replacement.ToUpper();

        var capitalized = char.IsUpper(match.Value.FirstOrDefault(char.IsLetter));
        if (capitalized)
            return CapitalizeFirstLetter(replacement);

        return replacement;
    }

    // 'tis -> 'Tis
    private static string CapitalizeFirstLetter(string text)
    {
        var i = text.ToList().FindIndex(char.IsLetter);
        if (i < 0)
            return text;
        return text[..i] + char.ToUpper(text[i]) + text[(i + 1)..];
    }

    // the whole word the match sits in, e.g. "Sorry" for the "S" in "Sorry",
    // apostrophes included so "IT'S" is one word and not "IT" and "S"
    private static string WordAround(string input, Match match)
    {
        static bool InWord(char c) => char.IsLetter(c) || c is '\'' or '’';

        var start = match.Index;
        while (start > 0 && InWord(input[start - 1]))
            start--;

        var end = match.Index + match.Length;
        while (end < input.Length && InWord(input[end]))
            end++;

        return input[start..end];
    }
}
