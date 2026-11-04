using System;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Game.Chat;
using Dalamud.Game.Command;
using Dalamud.Game.Config;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using FFXIVClientStructs.FFXIV.Component.Shell;
using ReflavorRP.Windows;

namespace ReflavorRP;

public sealed unsafe class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInterop { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IGameConfig GameConfig { get; private set; } = null!;

    private static readonly string[] Commands = ["/reflavor", "/reflavorrp", "/textrp"];

    private readonly Configuration config;
    private readonly WindowSystem windows = new("ReflavorRP");
    private readonly ConfigWindow configWindow;
    private readonly ChatStyler chatStyler;
    private readonly ChatBubbles chatBubbles;
    private bool disposed;

    // CommandManager only gets commands the game doesn't know, so /me, /say etc. need a hook
    private readonly Hook<ShellCommandModule.Delegates.ExecuteCommandInner> commandHook;

    public Plugin()
    {
        config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        chatBubbles = new ChatBubbles(ObjectTable, GameGui, config);
        configWindow = new ConfigWindow(config);
        windows.AddWindow(configWindow);
        chatStyler = new ChatStyler(ChatGui, ObjectTable, config, Log, ShowChatBubble,
            () => UIGlobals.PlayChatSoundEffect(1));

        commandHook = GameInterop.HookFromAddress<ShellCommandModule.Delegates.ExecuteCommandInner>(
            (nint)ShellCommandModule.MemberFunctionPointers.ExecuteCommandInner, OnExecuteCommand);
        commandHook.Enable();
        Framework.Update += UpdateNativeChatBubbles;
        UpdateNativeChatBubbles(Framework);

        foreach (var cmd in Commands)
        {
            CommandManager.AddHandler(cmd, new CommandInfo((_, _) => configWindow.Toggle())
            {
                HelpMessage = "Opens the ReflavorRP window (also /reflavorrp, /textrp)",
                ShowInHelp = cmd == Commands[0],
            });
        }

        PluginInterface.UiBuilder.Draw += chatBubbles.Draw;
        PluginInterface.UiBuilder.Draw += windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
    }

    public void Dispose()
    {
        disposed = true;
        Framework.Update -= UpdateNativeChatBubbles;
        UpdateNativeChatBubbles(Framework);
        PluginInterface.UiBuilder.Draw -= chatBubbles.Draw;
        PluginInterface.UiBuilder.Draw -= windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Toggle;
        windows.RemoveAllWindows();

        foreach (var cmd in Commands)
            CommandManager.RemoveHandler(cmd);

        commandHook.Dispose();
        chatStyler.Dispose();
    }

    private void UpdateNativeChatBubbles(IFramework _)
    {
        const string option = nameof(UiConfigOption.LogChatBubbleEnableChatBubble);
        var customActive = !disposed && config.Enabled && config.StyleIncomingChat && config.AlternativeChatBubbles;
        if (customActive)
        {
            if (!GameConfig.UiConfig.TryGet(option, out bool nativeEnabled))
                return;
            if (config.NativeChatBubblesBeforeOverride == null)
            {
                // Save before changing the game setting, including across an interrupted session.
                config.NativeChatBubblesBeforeOverride = nativeEnabled;
                config.Save();
            }
            if (nativeEnabled)
                GameConfig.UiConfig.Set(option, false);
        }
        else if (config.NativeChatBubblesBeforeOverride is bool previousValue)
        {
            if (!GameConfig.UiConfig.TryGet(option, out bool nativeEnabled))
                return;
            if (nativeEnabled != previousValue)
                GameConfig.UiConfig.Set(option, previousValue);
            config.NativeChatBubblesBeforeOverride = null;
            config.Save();
        }
    }

    private void ShowChatBubble(IChatMessage message, IReadOnlyList<(bool Speech, string Text)> pieces, bool isOoc)
    {
        if (config.AlternativeChatBubbles)
        {
            chatBubbles.Add(message, pieces, isOoc);
            return;
        }

        var player = message.Sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        // Outgoing Tell identifies its recipient; unlinked public senders are ourselves.
        var isLocalPlayer = message.LogKind == XivChatType.TellOutgoing || player == null;
        var senderName = isLocalPlayer ? ObjectTable.LocalPlayer?.Name.TextValue : player!.PlayerName;
        if (string.IsNullOrWhiteSpace(senderName))
            return;

        // Dalamud reuses IChatMessage; capture values now, never retain the event object.
        var chatType = (ushort)message.LogKind;
        var messageBytes = message.Message.Encode();
        var worldId = (ushort)(player?.World.RowId ?? 0);
        _ = Framework.RunOnTick(() =>
        {
            if (disposed || !config.Enabled || !config.StyleIncomingChat || config.AlternativeChatBubbles)
                return;
            var logModule = RaptureLogModule.Instance();
            if (logModule == null)
                return;

            using var sender = new Utf8String();
            using var text = new Utf8String();
            sender.SetString(senderName);
            text.SetString(messageBytes);
            logModule->ShowMiniTalkPlayer(chatType, &sender, &text, worldId, isLocalPlayer);
        }, delayTicks: 1);
    }

    private void OnExecuteCommand(ShellCommandModule* self, Utf8String* command, UIModule* uiModule)
    {
        string? rewritten = null;
        try
        {
            if (config.Enabled && config.ConversionEnabled)
                rewritten = RpFormatter.Rewrite(command->ToString(), RaptureShellModule.Instance()->ChatType, config,
                    ObjectTable.LocalPlayer?.Name.TextValue);
        }
        catch (Exception e)
        {
            Log.Error(e, "Couldn't rewrite command");
        }

        if (rewritten == null)
        {
            commandHook.Original(self, command, uiModule);
            return;
        }

        // Original, not the detour, or we'd rewrite our own command again
        var str = Utf8String.FromString(rewritten);
        commandHook.Original(self, str, uiModule);
        str->Dtor(true);
    }
}
