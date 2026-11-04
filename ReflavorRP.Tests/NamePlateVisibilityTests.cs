using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using Moq;

namespace ReflavorRP.Tests;

public class NamePlateVisibilityTests : IDisposable
{
    private readonly Mock<INamePlateGui> namePlates = new();
    private readonly Mock<IObjectTable> objects = new();
    private readonly Mock<IFramework> framework = new();
    private readonly Queue<(Action Action, TaskCompletionSource Completion)> pendingTicks = new();
    private readonly Mock<IPlayerCharacter> localPlayer = new();
    private readonly Configuration config = new() { HideDistantPlayerNames = true };
    private readonly NamePlateVisibility visibility;

    public NamePlateVisibilityTests()
    {
        localPlayer.SetupGet(p => p.GameObjectId).Returns(1);
        localPlayer.SetupGet(p => p.Position).Returns(new Vector3(100, 10, 100));
        objects.SetupGet(o => o.LocalPlayer).Returns(localPlayer.Object);
        framework.Setup(f => f.RunOnTick(It.IsAny<Action>(), It.IsAny<TimeSpan>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((Action action, TimeSpan delay, int ticks, CancellationToken cancellation) =>
            {
                Assert.Equal(1, ticks);
                var completion = new TaskCompletionSource();
                pendingTicks.Enqueue((action, completion));
                return completion.Task;
            });
        visibility = new NamePlateVisibility(namePlates.Object, objects.Object, config,
            framework.Object, Mock.Of<IPluginLog>());
    }

    public void Dispose() => visibility.Dispose();

    private void NextTick()
    {
        var (action, completion) = pendingTicks.Dequeue();
        action();
        completion.SetResult();
    }

    private Mock<INamePlateUpdateHandler> Plate(float distance, ulong id = 2,
        NamePlateKind kind = NamePlateKind.PlayerCharacter, int flags = 7)
    {
        var player = new Mock<IPlayerCharacter>();
        player.SetupGet(p => p.GameObjectId).Returns(id);
        player.SetupGet(p => p.Position).Returns(new Vector3(100 + distance, 10, 100));
        var handler = new Mock<INamePlateUpdateHandler>();
        handler.SetupGet(h => h.NamePlateKind).Returns(kind);
        handler.SetupGet(h => h.PlayerCharacter).Returns(player.Object);
        handler.SetupProperty(h => h.VisibilityFlags, flags);
        return handler;
    }

    private void Update(params Mock<INamePlateUpdateHandler>[] handlers) => namePlates.Raise(
        n => n.OnDataUpdate += null!, Mock.Of<INamePlateUpdateContext>(),
        (IReadOnlyList<INamePlateUpdateHandler>)handlers.Select(h => h.Object).ToArray());

    [Theory]
    [InlineData(19, 7)]
    [InlineData(20, 7)]
    [InlineData(21, 6)]
    public void OnlyHidesTextBeyondTheConfiguredDistance(float distance, int expectedFlags)
    {
        var plate = Plate(distance);
        Update(plate);
        Assert.Equal(expectedFlags, plate.Object.VisibilityFlags);
    }

    [Fact]
    public void MovingCloserUsesNativeVisibilityAgain()
    {
        var distant = Plate(30);
        Update(distant);
        Assert.Equal(6, distant.Object.VisibilityFlags);

        // A new handler represents the next native update; handlers are never retained across frames.
        var nearby = Plate(10);
        Update(nearby);
        Assert.Equal(7, nearby.Object.VisibilityFlags);
        namePlates.Verify(n => n.RequestRedraw(), Times.Never);
    }

    [Fact]
    public void DoesNotRevealNamesHiddenByTheGame()
    {
        var plate = Plate(10, flags: 6);
        Update(plate);
        Assert.Equal(6, plate.Object.VisibilityFlags);
    }

    [Fact]
    public void PreservesSelfNpcsAndEnemies()
    {
        var self = Plate(100, id: 1);
        var npc = Plate(100, kind: NamePlateKind.EventNpcCompanion);
        var enemy = Plate(100, kind: NamePlateKind.BattleNpcEnemy);
        Update(self, npc, enemy);
        Assert.All(new[] { self, npc, enemy }, p => Assert.Equal(7, p.Object.VisibilityFlags));
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void PausingOrDisablingRequestsOneNativeRedraw(bool enabled, bool hideNames)
    {
        Update(Plate(30));
        config.Enabled = enabled;
        config.HideDistantPlayerNames = hideNames;
        var plate = Plate(30);
        Update(plate);
        Update(plate);
        Assert.Equal(7, plate.Object.VisibilityFlags);
        namePlates.Verify(n => n.RequestRedraw(), Times.Never);
        Assert.Single(pendingTicks);
        NextTick();
        Update(plate);
        namePlates.Verify(n => n.RequestRedraw(), Times.Once);
    }

    [Fact]
    public void MissingLocalPlayerDefersRestorationUntilPlayerReturns()
    {
        Update(Plate(30));
        objects.SetupGet(o => o.LocalPlayer).Returns((IPlayerCharacter?)null);
        Update(Plate(30));
        Assert.Empty(pendingTicks);
        namePlates.Verify(n => n.RequestRedraw(), Times.Never);
        config.HideDistantPlayerNames = false;
        objects.SetupGet(o => o.LocalPlayer).Returns(localPlayer.Object);
        Update(Plate(30));
        NextTick();
        namePlates.Verify(n => n.RequestRedraw(), Times.Once);
    }

    [Fact]
    public void DisposingUnsubscribesAndRestoresNativeDisplay()
    {
        Update(Plate(30));
        visibility.Dispose();
        var plate = Plate(30);
        Update(plate);
        Assert.Equal(7, plate.Object.VisibilityFlags);
        namePlates.Verify(n => n.RequestRedraw(), Times.Never);
        NextTick();
        namePlates.Verify(n => n.RequestRedraw(), Times.Once);
    }

    [Fact]
    public void CustomDistanceDoesNotDependOnChatStyling()
    {
        config.StyleIncomingChat = false;
        config.PlayerNameplateDistance = 40;
        var nearby = Plate(30);
        var distant = Plate(41);
        Update(nearby, distant);
        Assert.Equal(7, nearby.Object.VisibilityFlags);
        Assert.Equal(6, distant.Object.VisibilityFlags);
    }

    [Fact]
    public void DistanceChangesCoalesceRedrawsOutsideTheUpdateCallback()
    {
        Update(Plate(50));
        config.PlayerNameplateDistance = 30;
        Update(Plate(50));
        config.PlayerNameplateDistance = 40;
        Update(Plate(50));
        namePlates.Verify(n => n.RequestRedraw(), Times.Never);
        Assert.Single(pendingTicks);
        NextTick();
        namePlates.Verify(n => n.RequestRedraw(), Times.Once);
    }

    [Fact]
    public void InvalidNativeIndexIsNotDereferenced()
    {
        foreach (var index in new[] { -1, AddonNamePlate.NumNamePlateObjects })
        {
            var plate = Plate(50);
            plate.SetupGet(p => p.NamePlateIndex).Returns(index);
            Update(plate);
            plate.VerifyGet(p => p.PlayerCharacter, Times.Never);
            plate.VerifySet(p => p.VisibilityFlags = It.IsAny<int>(), Times.Never);
        }
    }

    [Fact]
    public void UnloadingDoesNotQueueNativeRedraw()
    {
        Update(Plate(50));
        framework.SetupGet(f => f.IsFrameworkUnloading).Returns(true);
        config.HideDistantPlayerNames = false;
        Update();
        Assert.Empty(pendingTicks);
        namePlates.Verify(n => n.RequestRedraw(), Times.Never);
    }

    [Fact]
    public void PendingRedrawChecksShutdownAgain()
    {
        Update(Plate(50));
        config.HideDistantPlayerNames = false;
        Update();
        framework.SetupGet(f => f.IsFrameworkUnloading).Returns(true);
        NextTick();
        namePlates.Verify(n => n.RequestRedraw(), Times.Never);
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void InvalidDistanceUsesDefault(float distance)
    {
        config.PlayerNameplateDistance = distance;
        var nearby = Plate(19);
        var distant = Plate(21);
        Update(nearby, distant);
        Assert.Equal(7, nearby.Object.VisibilityFlags);
        Assert.Equal(6, distant.Object.VisibilityFlags);
    }
}
