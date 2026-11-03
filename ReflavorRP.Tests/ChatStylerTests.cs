using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin.Services;
using Moq;
using Xunit.Abstractions;
using Builder = Lumina.Text.SeStringBuilder;

namespace ReflavorRP.Tests;

public class ChatStylerTests : IDisposable
{
    private readonly Mock<IChatGui> chat = new();
    private readonly Mock<IObjectTable> objects = new();
    private readonly Mock<IPlayerCharacter> localPlayer = new();
    private readonly Mock<Action<IChatMessage>> showChatBubble = new();
    private readonly Mock<Action> playHighlightSound = new();
    private readonly Configuration config = new() { HighlightCharacterName = false };
    private readonly ChatStyler styler;
    private readonly ITestOutputHelper output;
    private XivChatEntry? printed;

    public ChatStylerTests(ITestOutputHelper output)
    {
        this.output = output;
        localPlayer.SetupGet(p => p.Name).Returns((SeString)"Legaia");
        // These tests only read RowId, so no game sheet data is needed.
        localPlayer.SetupGet(p => p.OnlineStatus).Returns(
            new Lumina.Excel.RowRef<Lumina.Excel.Sheets.OnlineStatus>(null!, 22, Lumina.Data.Language.English));
        objects.SetupGet(o => o.LocalPlayer).Returns(localPlayer.Object);
        objects.SetupGet(o => o.PlayerObjects).Returns(Array.Empty<IPlayerCharacter>());
        chat.Setup(c => c.Print(It.IsAny<XivChatEntry>())).Callback<XivChatEntry>(entry => printed = entry);
        styler = new ChatStyler(chat.Object, objects.Object, config, Mock.Of<IPluginLog>(),
            showChatBubble.Object, playHighlightSound.Object);
    }

    public void Dispose() => styler.Dispose();

    [Theory]
    [InlineData(XivChatType.Say, "“Hello!”", "Raih’li says: Hello!")]
    [InlineData(XivChatType.Say, "Hello *waves* *smiles* again", "Raih’li says: Hello *waves* *smiles* again")]
    [InlineData(XivChatType.Say, "Hello *waves**smiles**sits* Bye. Again.", "Raih’li says: Hello *waves* *smiles* *sits* Bye. Again.")]
    [InlineData(XivChatType.Say, "Hello **waves****smiles** again", "Raih’li says: Hello *waves* *smiles* again")]
    [InlineData(XivChatType.Say, "“Hello!” **waves**", "Raih’li says: Hello! *waves*")]
    [InlineData(XivChatType.Say, "“*Hello!*”", "Raih’li says: *Hello!*")]
    [InlineData(XivChatType.Say, "\"I *really* mean it.\" waves.", "Raih’li says: I *really* mean it. *waves.*")]
    [InlineData(XivChatType.CustomEmote, "smiles. \"I *really* mean it.\"", "* Raih’li smiles. \"I *really* mean it.\"")]
    [InlineData(XivChatType.TellIncoming, "\"I *really* mean it.\" waves.", "[TELL:Raih’li] Raih’li says: I *really* mean it. *waves.*")]
    [InlineData(XivChatType.Say, "brb", "(( Raih’li: brb ))")]
    [InlineData(XivChatType.CustomEmote, "**waves. “ Hi. ”**", "* Raih’li waves. \"Hi.\"")]
    [InlineData(XivChatType.CustomEmote, "waves.  “Hi.”  nods.", "* Raih’li waves. \"Hi.\" nods.")]
    [InlineData(XivChatType.TellIncoming, "“Welcome to The Dancing Daisy, friend!”", "[TELL:Raih’li] Raih’li says: Welcome to The Dancing Daisy, friend!")]
    [InlineData(XivChatType.TellOutgoing, "“I’d love a menu, thank you!”", "[TELL:Raih’li] Legaia says: I’d love a menu, thank you!")]
    [InlineData(XivChatType.TellIncoming, "*Rai slid a menu onto the table. “We also have coffee.” His voice carried.", "[TELL:Raih’li] * Rai slid a menu onto the table. \"We also have coffee.\" His voice carried.")]
    [InlineData(XivChatType.TellOutgoing, "Legaia’s eyes scan the menu. “Fried Fish, please.” She closes the menu.", "[TELL:Raih’li] * Legaia’s eyes scan the menu. \"Fried Fish, please.\" She closes the menu.")]
    [InlineData(XivChatType.TellIncoming, "*waves*", "[TELL:Raih’li] * waves")]
    [InlineData(XivChatType.TellOutgoing, "**waves**", "[TELL:Raih’li] * waves")]
    [InlineData(XivChatType.TellIncoming, "and that’s why.” she said.", "[TELL:Raih’li] Raih’li says: and that’s why. *she said.*")]
    [InlineData(XivChatType.TellIncoming, "“Hello!\" she waves.", "[TELL:Raih’li] Raih’li says: Hello! *she waves.*")]
    public void FormatsChat(XivChatType kind, string input, string expected)
    {
        var message = Receive(kind, input);

        Assert.NotNull(printed);
        Assert.Equal(expected, printed.Message.TextValue);
        Assert.Equal(XivChatType.Echo, printed.Type);
        Assert.Equal(42, printed.Timestamp);
        Assert.Equal(input, message.Object.Message.TextValue);
        showChatBubble.Verify(show => show(message.Object), Times.Once);
        message.Verify(m => m.PreventOriginal(), Times.Once);
        chat.Verify(c => c.Print(It.IsAny<XivChatEntry>()), Times.Once);
        output.WriteLine($"Entrada: {input}");
        output.WriteLine($"Prévia: {printed.Message.TextValue}");
    }

    [Fact]
    public void TellPrefixIsOrangeAndActionRestoresSpeechColor()
    {
        Receive(XivChatType.TellOutgoing, "“I *really* mean it.” *waves* “Bye.”");

        var expected = new Builder();
        expected.PushColorRgba(0xFF, 0xA6, 0x4D, 0xFF);
        expected.Append("[TELL:Raih’li] ");
        expected.PopColor();
        expected.PushColorRgba(0xE6, 0xE6, 0xE6, 0xFF);
        expected.Append("Legaia says: I *really* mean it. ");
        expected.PushColorRgba(0xC2, 0xA2, 0xDA, 0xFF);
        expected.Append("*waves*");
        expected.PopColor();
        expected.Append(" Bye.");
        expected.PopColor();
        Assert.NotNull(printed);
        Assert.Equal(expected.ToArray(), printed.Message.Encode());
        localPlayer.VerifyGet(p => p.Position, Times.Never);
    }

    [Fact]
    public void DialogueRestoresEmoteColor()
    {
        Receive(XivChatType.CustomEmote, "smiles. “Hi.” nods.");

        var expected = new Builder();
        expected.PushColorRgba(0xC2, 0xA2, 0xDA, 0xFF);
        expected.Append("* Raih’li smiles. ");
        expected.PushColorRgba(0xE6, 0xE6, 0xE6, 0xFF);
        expected.Append("\"Hi.\"");
        expected.PopColor();
        expected.Append(" nods.");
        expected.PopColor();
        Assert.NotNull(printed);
        Assert.Equal(expected.ToArray(), printed.Message.Encode());
    }

    [Fact]
    public void OocIsGrey()
    {
        config.HighlightWords = "brb";
        Receive(XivChatType.Say, "(( brb ))");

        var expected = new Builder();
        expected.PushColorRgba(0xAF, 0xAF, 0xAF, 0xFF);
        expected.Append("(( Raih’li: ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("brb");
        expected.PopColor();
        expected.Append(" ))");
        expected.PopColor();
        Assert.NotNull(printed);
        Assert.Equal(expected.ToArray(), printed.Message.Encode());
    }

    [Fact]
    public void HighlightsFirstAndLastNameInsideWordsIgnoringCase()
    {
        config.HighlightCharacterName = true;
        config.PlayHighlightSound = true;
        localPlayer.SetupGet(p => p.Name).Returns((SeString)"Legaia Lindblum");
        Receive(XivChatType.Say, "\"Hey LEGAIA, Lindblum's here. Legaian and preLindblumpost.\"");

        var expected = new Builder();
        expected.PushColorRgba(0xE6, 0xE6, 0xE6, 0xFF);
        expected.Append("Raih’li says: Hey ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("LEGAIA");
        expected.PopColor();
        expected.Append(", ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("Lindblum");
        expected.PopColor();
        expected.Append("'s here. ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("Legaia");
        expected.PopColor();
        expected.Append("n and pre");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("Lindblum");
        expected.PopColor();
        expected.Append("post.");
        expected.PopColor();
        Assert.NotNull(printed);
        Assert.Equal(expected.ToArray(), printed.Message.Encode());
        playHighlightSound.Verify(play => play(), Times.Once);
    }

    [Theory]
    [InlineData(XivChatType.TellIncoming, "menu menus MENU", "Raih’li", true, 1)]
    [InlineData(XivChatType.Say, "\"A menu.\" offers menus.", "Raih’li", true, 1)]
    [InlineData(XivChatType.CustomEmote, "offers a menu.", "Raih’li", true, 1)]
    [InlineData(XivChatType.TellIncoming, "Hello!", "Raih’li", true, 0)]
    [InlineData(XivChatType.TellIncoming, "menu", "Raih’li", false, 0)]
    [InlineData(XivChatType.TellOutgoing, "menu", "Raih’li", true, 0)]
    [InlineData(XivChatType.Say, "\"A menu.\"", "Legaia", true, 0)]
    public void PlaysHighlightSoundOnceForMatchingReceivedMessages(
        XivChatType kind, string text, string sender, bool soundEnabled, int expectedCalls)
    {
        config.HighlightWords = "menu";
        config.PlayHighlightSound = soundEnabled;
        Receive(kind, text, sender);

        playHighlightSound.Verify(play => play(), Times.Exactly(expectedCalls));
    }

    [Fact]
    public void CustomHighlightsRestoreSpeechAndActionColorsAndMatchLiteralPhrases()
    {
        config.HighlightWords = " menu \nfish\nfish and chips\nC++\n[RP]\nMENU\n\n";
        Receive(XivChatType.TellIncoming, "\"The menu is ready.\" offers fish and chips. \"C++ is [RP]. Other menus.\"");

        var expected = new Builder();
        expected.PushColorRgba(0xFF, 0xA6, 0x4D, 0xFF);
        expected.Append("[TELL:Raih’li] ");
        expected.PopColor();
        expected.PushColorRgba(0xE6, 0xE6, 0xE6, 0xFF);
        expected.Append("Raih’li says: The ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("menu");
        expected.PopColor();
        expected.Append(" is ready. ");
        expected.PushColorRgba(0xC2, 0xA2, 0xDA, 0xFF);
        expected.Append("*offers ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("fish and chips");
        expected.PopColor();
        expected.Append(".*");
        expected.PopColor();
        expected.Append(" ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("C++");
        expected.PopColor();
        expected.Append(" is ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("[RP]");
        expected.PopColor();
        expected.Append(". Other ");
        expected.PushColorRgba(0xFF, 0xD1, 0x66, 0xFF);
        expected.Append("menu");
        expected.PopColor();
        expected.Append("s.");
        expected.PopColor();
        Assert.NotNull(printed);
        Assert.Equal(expected.ToArray(), printed.Message.Encode());
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void LeavesNameMentionsUncoloredWhenDisabledOrLocalPlayerIsMissing(bool highlightName, bool hasLocalPlayer)
    {
        config.HighlightCharacterName = highlightName;
        if (!hasLocalPlayer)
            objects.SetupGet(o => o.LocalPlayer).Returns((IPlayerCharacter?)null);
        Receive(XivChatType.TellIncoming, "\"Hello Legaia.\"");

        var expected = new Builder();
        expected.PushColorRgba(0xFF, 0xA6, 0x4D, 0xFF);
        expected.Append("[TELL:Raih’li] ");
        expected.PopColor();
        expected.PushColorRgba(0xE6, 0xE6, 0xE6, 0xFF);
        expected.Append("Raih’li says: Hello Legaia.");
        expected.PopColor();
        Assert.NotNull(printed);
        Assert.Equal(expected.ToArray(), printed.Message.Encode());
    }

    [Theory]
    [InlineData(XivChatType.CustomEmote, 1)]
    [InlineData(XivChatType.TellIncoming, 2)]
    [InlineData(XivChatType.TellOutgoing, 1)]
    public void KeepsClickablePlayerLinks(XivChatType kind, int expectedLinks)
    {
        config.HighlightWords = "Raih’li";
        var sender = new Builder();
        sender.PushLinkCharacter("Raih’li", 75);
        sender.Append("Raih’li@World");
        sender.PopLink();
        var playerName = SeString.Parse(sender.ToArray());
        var originalLink = Assert.Single(playerName.Payloads.OfType<PlayerPayload>());
        var message = Receive(kind, "“Hi.”", playerName);

        Assert.NotNull(printed);
        var links = printed.Message.Payloads.OfType<PlayerPayload>().ToArray();
        Assert.Equal(expectedLinks, links.Length);
        Assert.All(links, link => Assert.Equal(originalLink.Encode(), link.Encode()));
        Assert.DoesNotContain("@World", printed.Message.TextValue);
        Assert.Same(playerName, message.Object.Sender);
    }

    [Theory]
    [InlineData(XivChatType.Shout)]
    [InlineData(XivChatType.Yell)]
    public void RoleplayShoutsHaveActionsAndNeverFade(XivChatType kind)
    {
        Receive(kind, "Help! *waves*");

        Assert.NotNull(printed);
        Assert.Equal("Raih’li shouts: Help! *waves*", printed.Message.TextValue);
        localPlayer.VerifyGet(p => p.Position, Times.Never);
        objects.VerifyGet(o => o.LocalPlayer, Times.Once);
    }

    [Theory]
    [InlineData(XivChatType.Shout)]
    [InlineData(XivChatType.Yell)]
    public void LeavesNonRoleplayShoutsUntouched(XivChatType kind)
    {
        localPlayer.SetupGet(p => p.OnlineStatus).Returns(default(Lumina.Excel.RowRef<Lumina.Excel.Sheets.OnlineStatus>));
        var message = Receive(kind, "WTS item");

        Assert.Null(printed);
        showChatBubble.Verify(show => show(It.IsAny<IChatMessage>()), Times.Never);
        message.Verify(m => m.PreventOriginal(), Times.Never);
    }

    [Theory]
    [InlineData(XivChatType.Party)]
    [InlineData(XivChatType.Echo)]
    public void LeavesOtherChannelsUntouched(XivChatType kind)
    {
        var message = Receive(kind, "Hello!");

        Assert.Null(printed);
        showChatBubble.Verify(show => show(It.IsAny<IChatMessage>()), Times.Never);
        message.Verify(m => m.PreventOriginal(), Times.Never);
        objects.VerifyGet(o => o.LocalPlayer, Times.Never);
    }

    [Theory]
    [InlineData("plugin-disabled")]
    [InlineData("disabled")]
    [InlineData("handled")]
    [InlineData("empty")]
    [InlineData("item-link")]
    public void LeavesProtectedMessagesUntouched(string reason)
    {
        config.PlayHighlightSound = true;
        config.HighlightWords = "Hello";
        config.Enabled = reason != "plugin-disabled";
        config.StyleIncomingChat = reason != "disabled";
        var text = reason == "empty" ? "  " : "“Hello!”";
        SeString original = text;
        if (reason == "item-link")
            original.Payloads.Add(new ItemPayload(1, false));
        var message = Receive(XivChatType.TellIncoming, original, handled: reason == "handled");

        Assert.Null(printed);
        Assert.Same(original, message.Object.Message);
        playHighlightSound.Verify(play => play(), Times.Never);
        showChatBubble.Verify(show => show(It.IsAny<IChatMessage>()), Times.Never);
        message.Verify(m => m.PreventOriginal(), Times.Never);
    }

    [Fact]
    public void ResumesStylingWhenPluginIsEnabledAgain()
    {
        config.Enabled = false;
        var message = Receive(XivChatType.TellIncoming, "Hello!");
        Assert.Null(printed);
        message.Verify(m => m.PreventOriginal(), Times.Never);

        config.Enabled = true;
        Receive(XivChatType.TellIncoming, "Hello!");
        Assert.NotNull(printed);
        Assert.Equal("[TELL:Raih’li] Raih’li says: Hello!", printed.Message.TextValue);
    }

    [Fact]
    public void MissingLocalPlayerDoesNotAttributeOurTellToTheRecipient()
    {
        objects.SetupGet(o => o.LocalPlayer).Returns((IPlayerCharacter?)null);
        Receive(XivChatType.TellOutgoing, "“Hi.”");

        Assert.NotNull(printed);
        Assert.Equal("[TELL:Raih’li] Your character says: Hi.", printed.Message.TextValue);
    }

    [Fact]
    public void DisposeUnsubscribesFromChat()
    {
        styler.Dispose();
        Receive(XivChatType.TellIncoming, "“Hi.”");

        Assert.Null(printed);
    }

    private Mock<IHandleableChatMessage> Receive(XivChatType kind, SeString text, SeString? sender = null, bool handled = false)
    {
        var message = new Mock<IHandleableChatMessage>();
        message.SetupProperty(m => m.Message, text);
        message.SetupProperty(m => m.Sender, sender ?? (SeString)"Raih’li");
        message.SetupGet(m => m.LogKind).Returns(kind);
        message.SetupGet(m => m.Timestamp).Returns(42);
        message.SetupGet(m => m.IsHandled).Returns(handled);
        chat.Raise(c => c.ChatMessage += null, message.Object);
        return message;
    }
}
