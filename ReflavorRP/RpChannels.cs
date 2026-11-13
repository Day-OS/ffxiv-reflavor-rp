using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;

namespace ReflavorRP;

internal static class RpChannels
{
    private static readonly string[] RecipientCommands = ["tell", "t"];
    private static readonly string[] ActiveLinkshellCommands = ["linkshell", "l", "cwlinkshell", "cwl"];
    public static readonly (XivChatType Kind, string Label, string[] Commands)[] Entries =
    [
        (XivChatType.Say, "Say", ["say"]),
        (XivChatType.CustomEmote, "Emote", ["em", "emote", "me", "ame", "do"]),
        (XivChatType.TellIncoming, "Tell received", []),
        (XivChatType.TellOutgoing, "Tell sent", [.. RecipientCommands, "reply", "r"]),
        (XivChatType.Party, "Party", ["party", "p"]),
        (XivChatType.CrossParty, "Cross-world party", []),
        (XivChatType.Alliance, "Alliance", ["alliance", "a"]),
        (XivChatType.FreeCompany, "Free Company", ["freecompany", "fc"]),
        (XivChatType.Ls1, "Linkshell 1", ["linkshell1", "l1"]),
        (XivChatType.Ls2, "Linkshell 2", ["linkshell2", "l2"]),
        (XivChatType.Ls3, "Linkshell 3", ["linkshell3", "l3"]),
        (XivChatType.Ls4, "Linkshell 4", ["linkshell4", "l4"]),
        (XivChatType.Ls5, "Linkshell 5", ["linkshell5", "l5"]),
        (XivChatType.Ls6, "Linkshell 6", ["linkshell6", "l6"]),
        (XivChatType.Ls7, "Linkshell 7", ["linkshell7", "l7"]),
        (XivChatType.Ls8, "Linkshell 8", ["linkshell8", "l8"]),
        (XivChatType.CrossLinkShell1, "Cross-world linkshell 1", ["cwlinkshell1", "cwl1"]),
        (XivChatType.CrossLinkShell2, "Cross-world linkshell 2", ["cwlinkshell2", "cwl2"]),
        (XivChatType.CrossLinkShell3, "Cross-world linkshell 3", ["cwlinkshell3", "cwl3"]),
        (XivChatType.CrossLinkShell4, "Cross-world linkshell 4", ["cwlinkshell4", "cwl4"]),
        (XivChatType.CrossLinkShell5, "Cross-world linkshell 5", ["cwlinkshell5", "cwl5"]),
        (XivChatType.CrossLinkShell6, "Cross-world linkshell 6", ["cwlinkshell6", "cwl6"]),
        (XivChatType.CrossLinkShell7, "Cross-world linkshell 7", ["cwlinkshell7", "cwl7"]),
        (XivChatType.CrossLinkShell8, "Cross-world linkshell 8", ["cwlinkshell8", "cwl8"]),
    ];

    private static readonly Regex RoutePattern = CreateRoutePattern();

    private static Regex CreateRoutePattern()
    {
        var commands = Entries.Where(entry => !IsPublic(entry.Kind))
            .SelectMany(entry => entry.Commands).Except(RecipientCommands).Concat(ActiveLinkshellCommands);
        var simple = string.Join("|", commands.Select(Regex.Escape));
        var recipients = string.Join("|", RecipientCommands.Select(Regex.Escape));
        return new Regex(@"^/(?:(?:" + simple + @")\s+|(?:" + recipients +
            @")\s+(?:""[^""]+""|<[^<>]+>|[^\s/]+[ \t]+[^\s/]+)\s+)(?=\S)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public static Match MatchRoute(string text) => RoutePattern.Match(text);

    public static bool IsPublic(XivChatType kind) => kind is XivChatType.Say or XivChatType.CustomEmote
        or XivChatType.Shout or XivChatType.Yell;

    public static bool IsTell(XivChatType kind) => kind is XivChatType.TellIncoming or XivChatType.TellOutgoing;
    public static bool IsIncomingTell(XivChatType kind) => kind == XivChatType.TellIncoming;
    public static bool IsOutgoingTell(XivChatType kind) => kind == XivChatType.TellOutgoing;
    public static bool IsShout(XivChatType kind) => kind is XivChatType.Shout or XivChatType.Yell;
    public static bool IsEmote(XivChatType kind) => kind == XivChatType.CustomEmote;
    public static bool IsSay(XivChatType kind) => kind == XivChatType.Say;
    public static bool IsPublicOrTell(XivChatType kind) => IsPublic(kind) || IsTell(kind);

    public static bool CanBeOocChannel(XivChatType kind) => !IsEmote(kind) && !IsTell(kind) &&
        Entries.Any(entry => entry.Kind == kind && entry.Commands.Length > 0);

    public static bool IsAlwaysOoc(XivChatType kind, Configuration config) => config.AlwaysOocChannel == kind ||
        (config.AlwaysOocChannel == XivChatType.Party && kind == XivChatType.CrossParty);

    public static bool ShouldStyle(XivChatType kind) => IsPublic(kind) || Entries.Any(entry => entry.Kind == kind);

    public static RpTextKind Convention(XivChatType kind, Configuration config) =>
        IsAlwaysOoc(kind, config) ? RpTextKind.Ooc : DefaultKind(kind);

    public static string OocCommand(Configuration config) => config.AlwaysOocChannel is { } kind
        ? "/" + Entries.First(entry => entry.Kind == kind).Commands[0] : "/say";

    public static RpTextKind DefaultKind(XivChatType kind) =>
        IsEmote(kind) ? RpTextKind.Action : RpTextKind.Speech;

    public static string Label(XivChatType kind)
    {
        foreach (var entry in Entries)
            if (entry.Kind == kind)
                return entry.Label;
        return kind.ToString();
    }

    public static RpTextKind OutgoingKind(string route, Configuration config)
    {
        var command = route.TrimStart('/').Split([' ', '\t', '\r', '\n'], 2)[0];
        foreach (var entry in Entries)
            foreach (var alias in entry.Commands)
                if (command.Equals(alias, StringComparison.OrdinalIgnoreCase))
                    return Convention(entry.Kind, config);
        // Unnumbered /l and /cwl select the game's active linkshell. Its slot is not in the command.
        return RpTextKind.Speech;
    }
}
