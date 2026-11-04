using Dalamud.Game.Text;
using System.Numerics;

namespace ReflavorRP.Tests;

public class ChatBubblesTests
{
    [Fact]
    public void OverlappingSpeakersInterleaveInConversationOrder()
    {
        // Newest first, just as Draw places them: A2, B1, A1.
        var a2 = new ChatBubbles.BubbleBounds(new(100, 300), new(300, 360));
        var b1 = new ChatBubbles.BubbleBounds(new(250, 310), new(450, 390));
        var a1 = new ChatBubbles.BubbleBounds(new(100, 300), new(300, 350));
        var placed = new List<ChatBubbles.BubbleBounds> { a2 };
        b1 = ChatBubbles.PlaceAboveOverlaps(b1, placed, 6);
        placed.Add(b1);
        a1 = ChatBubbles.PlaceAboveOverlaps(a1, placed, 6);

        Assert.True(a1.Max.Y + 6 <= b1.Min.Y);
        Assert.True(b1.Max.Y + 6 <= a2.Min.Y);
        Assert.Equal(100f, a1.Min.X);
        Assert.Equal(250f, b1.Min.X);
        Assert.Equal(50f, a1.Max.Y - a1.Min.Y);
        Assert.Equal(80f, b1.Max.Y - b1.Min.Y);
    }

    [Fact]
    public void MovingAboveOneBubbleChecksEarlierObstaclesAgain()
    {
        var placed = new List<ChatBubbles.BubbleBounds>
        {
            new(new(0, 100), new(100, 150)),
            new(new(0, 200), new(100, 250)),
        };
        var bounds = new ChatBubbles.BubbleBounds(new(0, 220), new(100, 280));

        var result = ChatBubbles.PlaceAboveOverlaps(bounds, placed, 6);

        Assert.Equal(new Vector2(0, 34), result.Min);
        Assert.Equal(new Vector2(100, 94), result.Max);
    }

    [Fact]
    public void SeparateConversationsKeepTheirOriginalPositions()
    {
        var placed = new List<ChatBubbles.BubbleBounds>
        {
            new(new(0, 100), new(100, 150)),
        };
        var beside = new ChatBubbles.BubbleBounds(new(200, 100), new(300, 150));
        var above = new ChatBubbles.BubbleBounds(new(0, 40), new(100, 94));

        Assert.Equal(beside, ChatBubbles.PlaceAboveOverlaps(beside, placed, 6));
        Assert.Equal(above, ChatBubbles.PlaceAboveOverlaps(above, placed, 6));
    }

    [Theory]
    [InlineData(1, 5000)]
    [InlineData(15, 5000)]
    [InlineData(30, 5000)]
    [InlineData(150, 13000)]
    [InlineData(450, 33000)]
    public void DurationFollowsTextLength(int characters, long expected)
    {
        Assert.Equal(expected, ChatBubbles.GetDurationMilliseconds(new string('x', characters), 15));
    }

    [Fact]
    public void EmojiCountsAsOneVisibleCharacter()
    {
        Assert.Equal(6000, ChatBubbles.GetDurationMilliseconds(string.Concat(Enumerable.Repeat("😀", 45)), 15));
    }

    [Fact]
    public void WaitingMessagesStartOnlyAfterTheOldestDisappears()
    {
        var messages = new List<ChatBubbles.Bubble>
        {
            new(1, "Legaia", 75, "First", XivChatType.Say, 3000),
            new(2, "Legaia", 75, "Second", XivChatType.Say, 6000),
        };

        ChatBubbles.AdvanceQueue(messages, 1000);
        Assert.Equal(4000, messages[0].ExpiresAt);
        Assert.Equal(0, messages[1].ExpiresAt);

        ChatBubbles.AdvanceQueue(messages, 3999);
        Assert.Equal(2, messages.Count);
        Assert.Equal(0, messages[1].ExpiresAt);

        ChatBubbles.AdvanceQueue(messages, 4000);
        Assert.Equal(2, Assert.Single(messages).Id);
        Assert.Equal(10000, messages[0].ExpiresAt);

        ChatBubbles.AdvanceQueue(messages, 10000);
        Assert.Empty(messages);
    }

    [Fact]
    public void HiddenBubbleCountsSlowlyThenResumesNormalTime()
    {
        var messages = new List<ChatBubbles.Bubble>
        {
            new(1, "Legaia", 75, "First", XivChatType.CustomEmote, 5000),
            new(2, "Legaia", 75, "Second", XivChatType.Say, 5000),
        };
        ChatBubbles.AdvanceQueue(messages, 1000);
        messages[0] = messages[0] with { WasVisible = false };

        ChatBubbles.AdvanceQueue(messages, 9000);
        Assert.Equal(12000, messages[0].ExpiresAt);
        Assert.Equal(0, messages[1].ExpiresAt);

        messages[0] = messages[0] with { WasVisible = true };
        ChatBubbles.AdvanceQueue(messages, 11999);
        Assert.Equal(1, messages[0].Id);
        ChatBubbles.AdvanceQueue(messages, 12000);
        Assert.Equal(2, Assert.Single(messages).Id);
        Assert.Equal(17000, messages[0].ExpiresAt);
    }

    [Fact]
    public void DelayedFramesDoNotConsumeWaitingMessagesTime()
    {
        var messages = new List<ChatBubbles.Bubble>
        {
            new(1, "Legaia", 75, "First", XivChatType.Say, 3000, 4000),
            new(2, "Legaia", 75, "Second", XivChatType.Say, 3000),
            new(3, "Legaia", 75, "Third", XivChatType.Say, 3000),
        };

        ChatBubbles.AdvanceQueue(messages, 20000);

        Assert.Equal(new long[] { 2, 3 }, messages.Select(message => message.Id));
        Assert.Equal(23000, messages[0].ExpiresAt);
        Assert.Equal(0, messages[1].ExpiresAt);
    }

    [Fact]
    public void HoverFreezesRemainingTimeThenResumesWithoutResettingIt()
    {
        var messages = new List<ChatBubbles.Bubble>
        {
            new(1, "Legaia", 75, "First", XivChatType.Say, 5000),
            new(2, "Legaia", 75, "Second", XivChatType.Say, 6000),
        };
        ChatBubbles.AdvanceQueue(messages, 1000);
        ChatBubbles.AdvanceQueue(messages, 2000);
        messages[0] = messages[0] with { WasHovered = true };

        ChatBubbles.AdvanceQueue(messages, 12000);
        Assert.Equal(4000, messages[0].ExpiresAt - 12000);
        Assert.Equal(0, messages[1].ExpiresAt);
        ChatBubbles.AdvanceQueue(messages, 15000);
        Assert.Equal(4000, messages[0].ExpiresAt - 15000);

        messages[0] = messages[0] with { WasHovered = false };
        ChatBubbles.AdvanceQueue(messages, 18999);
        Assert.Equal(1, messages[0].Id);
        ChatBubbles.AdvanceQueue(messages, 19000);
        Assert.Equal(2, Assert.Single(messages).Id);
        Assert.Equal(25000, messages[0].ExpiresAt);
    }

}
