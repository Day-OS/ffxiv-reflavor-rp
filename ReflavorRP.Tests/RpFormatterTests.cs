namespace ReflavorRP.Tests;

public class RpFormatterTests
{
    [Theory]
    [InlineData("/me waves", "/em waves.")]
    [InlineData("/em waves", "/em waves.")]
    [InlineData("/ame waves", "/em waves.")]
    [InlineData("/say *waves*", "/em waves.")]
    [InlineData("/s help", "/em shouts, \"Help!\"")]
    [InlineData("/low hello", "/em whispers, \"Hello.\"")]
    [InlineData("/w Friend hello", "/em whispers to Friend, \"Hello.\"")]
    public void NativeEmotesLeaveActorNameToTheGame(string command, string expected)
    {
        // The hook supplies the player name for every command, including local ones.
        Assert.Equal(expected, RpFormatter.Rewrite(command, 1, new Configuration(), "Test Player"));
    }

    [Theory]
    [InlineData("/me waves", "Test Player waves.")]
    [InlineData("/ame waves", "Test Player waves.")]
    [InlineData("/em waves", "Test Player waves.")]
    [InlineData("/me paused. \"onigiri\" His head tilted. \"salmon?\"", "Test Player paused. \"Onigiri.\" His head tilted. \"Salmon?\"")]
    [InlineData("/say hello *waves*", "\"Hello.\" waves.")]
    [InlineData("/say *waves*", "Test Player waves.")]
    [InlineData("/s help", "Test Player shouts, \"Help!\"")]
    [InlineData("/low hello", "Test Player whispers, \"Hello.\"")]
    [InlineData("/w Friend hello", "Test Player whispers to Friend, \"Hello.\"")]
    [InlineData("/b brb", "(( brb ))")]
    [InlineData("hello", "\"Hello.\"")]
    [InlineData("Hello! *She coughs *", "\"Hello!\" She coughs.")]
    [InlineData("*waves*", "Test Player waves.")]
    [InlineData("*waves* hello", "waves. \"Hello.\"")]
    [InlineData("\"Hello!\" *She coughs*", "\"Hello!\" She coughs.")]
    public void FormatsRoleplayWithinDestination(string command, string body)
    {
        string[] routes = ["/party ", "/p ", "/linkshell ", "/l ", "/cwlinkshell ", "/cwl ",
            "/linkshell1 ", "/l8 ", "/cwlinkshell8 ", "/cwl1 ", "/fc ", "/freecompany ",
            "/alliance ", "/a ", "/reply ", "/r ", "/PARTY ",
            "/tell First Last@World ", "/t First Last ", "/tell \"First Last@World\" ", "/tell <t> "];

        foreach (var route in routes)
            Assert.Equal(route + body, RpFormatter.Rewrite(route + command, 1, new Configuration(), "Test Player"));
    }

    [Theory]
    [InlineData("/party")]
    [InlineData("/party ")]
    [InlineData("/tell First Last@World")]
    [InlineData("/tell First Last@World ")]
    [InlineData("/tell /me waves")]
    [InlineData("/tell First /me waves")]
    [InlineData("/party /dance")]
    [InlineData("/party /me")]
    [InlineData("/party /w Friend")]
    [InlineData("/party /tell First Last /me waves")]
    [InlineData("/linkshell9 /me waves")]
    public void LeavesUnsupportedAndIncompleteCommandsAlone(string text)
    {
        Assert.Null(RpFormatter.Rewrite(text, 1, new Configuration(), "Test Player"));
    }

    [Fact]
    public void RoutedAccentsOnlyChangeSpeech()
    {
        var config = new Configuration { ActiveAccents = ["Uwufication"] };
        Assert.Equal("/tell First Last@World Test Player rolls. \"Weawwy.\"",
            RpFormatter.Rewrite("/tell First Last@World /me rolls. \"really\"", 1, config, "Test Player"));
        Assert.Equal("/party (( really ))", RpFormatter.Rewrite("/party /b really", 1, config, "Test Player"));
        Assert.Equal("/tell First Last@World \"Weawwy!\" rolls.",
            RpFormatter.Rewrite("/tell First Last@World really! *rolls*", 1, config, "Test Player"));
    }

    [Fact]
    public void RespectsMeSettingAndRequiresActorForRoutedEmotes()
    {
        Assert.Null(RpFormatter.Rewrite("/party /me waves", 1, new Configuration { ReplaceMeEmote = false }, "Test Player"));
        Assert.Null(RpFormatter.Rewrite("/party /me waves", 1, new Configuration()));
    }

    [Fact]
    public void KeepsExistingLocalAndActiveChannelBehavior()
    {
        var config = new Configuration();
        Assert.Equal("/em waves.", RpFormatter.Rewrite("/me waves", 1, config));
        Assert.Equal("/say \"Hello.\"", RpFormatter.Rewrite("hello", 1, config));
        Assert.Null(RpFormatter.Rewrite("hello", 2, config));
        Assert.Equal("/party Test Player waves.", RpFormatter.Rewrite("/party /me waves", 2, config, "Test Player"));
    }

    [Theory]
    [InlineData("/s help")]
    [InlineData("/S help")]
    [InlineData("/party /s help")]
    [InlineData("/tell First Last@World /s help")]
    public void LeavesSCommandUntouchedWhenReplacementIsDisabled(string command)
    {
        var config = new Configuration { ReplaceSCommand = false };

        Assert.Null(RpFormatter.Rewrite(command, 1, config, "Test Player"));
        Assert.Equal("/say \"Hello.\"", RpFormatter.Rewrite("/say hello", 1, config));
    }
}
