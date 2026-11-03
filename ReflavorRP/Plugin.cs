using System;
using System.Linq;
using Dalamud.Game.Chat;
using Dalamud.Game.Command;
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

    private static readonly string[] Commands = ["/reflavor", "/reflavorrp", "/textrp"];

    private readonly Configuration config;
    private readonly WindowSystem windows = new("ReflavorRP");
    private readonly ConfigWindow configWindow;
    private readonly ChatStyler chatStyler;

    // CommandManager only gets commands the game doesn't know, so /me, /say etc. need a hook
    private readonly Hook<ShellCommandModule.Delegates.ExecuteCommandInner> commandHook;

    public Plugin()
    {
        config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        configWindow = new ConfigWindow(config);
        windows.AddWindow(configWindow);
        chatStyler = new ChatStyler(ChatGui, ObjectTable, config, Log, ShowChatBubble);

        commandHook = GameInterop.HookFromAddress<ShellCommandModule.Delegates.ExecuteCommandInner>(
            (nint)ShellCommandModule.MemberFunctionPointers.ExecuteCommandInner, OnExecuteCommand);
        commandHook.Enable();

        foreach (var cmd in Commands)
        {
            CommandManager.AddHandler(cmd, new CommandInfo((_, _) => configWindow.Toggle())
            {
                HelpMessage = "Opens the ReflavorRP window (also /reflavorrp, /textrp)",
                ShowInHelp = cmd == Commands[0],
            });
        }

        PluginInterface.UiBuilder.Draw += windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= windows.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Toggle;
        windows.RemoveAllWindows();

        foreach (var cmd in Commands)
            CommandManager.RemoveHandler(cmd);

        commandHook.Dispose();
        chatStyler.Dispose();
    }

    private static void ShowChatBubble(IChatMessage message)
    {
        var logModule = RaptureLogModule.Instance();
        if (logModule == null)
            return;

        var player = message.Sender.Payloads.OfType<PlayerPayload>().FirstOrDefault();
        // Outgoing Tell identifies its recipient; unlinked public senders are ourselves.
        var isLocalPlayer = message.LogKind == XivChatType.TellOutgoing || player == null;
        var senderName = isLocalPlayer ? ObjectTable.LocalPlayer?.Name.TextValue : player!.PlayerName;
        if (string.IsNullOrWhiteSpace(senderName))
            return;

        using var sender = new Utf8String();
        using var text = new Utf8String();
        sender.SetString(senderName);
        text.SetString(message.Message.Encode());
        logModule->ShowMiniTalkPlayer((ushort)message.LogKind, &sender, &text,
            (ushort)(player?.World.RowId ?? 0), isLocalPlayer);
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
