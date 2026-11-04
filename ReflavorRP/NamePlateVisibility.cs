using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Gui.NamePlate;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace ReflavorRP;

public sealed class NamePlateVisibility : IDisposable
{
    // VisibilityFlags bit 0 controls name text (including title and FC tag); bit 1 is the HP gauge.
    private const int DrawNameText = 1;

    private readonly INamePlateGui namePlates;
    private readonly IObjectTable objects;
    private readonly Configuration config;
    private readonly IFramework framework;
    private readonly IPluginLog log;
    private bool hasHiddenNameplates;
    private bool redrawQueued;
    private float lastDistance;

    public NamePlateVisibility(INamePlateGui namePlates, IObjectTable objects, Configuration config,
        IFramework framework, IPluginLog log)
    {
        this.namePlates = namePlates;
        this.objects = objects;
        this.config = config;
        this.framework = framework;
        this.log = log;
        lastDistance = GetDistance();
        namePlates.OnDataUpdate += OnDataUpdate;
    }

    public void Dispose()
    {
        namePlates.OnDataUpdate -= OnDataUpdate;
        QueueRedraw();
    }

    private float GetDistance() => float.IsFinite(config.PlayerNameplateDistance)
        ? Math.Clamp(config.PlayerNameplateDistance, 5f, 100f) : 20f;

    private void QueueRedraw()
    {
        if (!hasHiddenNameplates || redrawQueued || framework.IsFrameworkUnloading || objects.LocalPlayer == null)
            return;
        redrawQueued = true;
        _ = RedrawNextTick();
    }

    private async Task RedrawNextTick()
    {
        try
        {
            // Do not mutate the native nameplate arrays while their update callback is running.
            await framework.RunOnTick(() =>
            {
                if (!framework.IsFrameworkUnloading && objects.LocalPlayer != null)
                {
                    namePlates.RequestRedraw();
                    hasHiddenNameplates = false;
                }
            }, delayTicks: 1);
        }
        catch (OperationCanceledException)
        {
            // The framework can cancel pending work while exiting.
        }
        catch (InvalidOperationException) when (framework.IsFrameworkUnloading)
        {
        }
        catch (Exception exception)
        {
            hasHiddenNameplates = false;
            log.Warning(exception, "Could not refresh native nameplates.");
        }
        finally
        {
            redrawQueued = false;
        }
    }

    private void OnDataUpdate(INamePlateUpdateContext context, IReadOnlyList<INamePlateUpdateHandler> handlers)
    {
        if (!config.Enabled || !config.HideDistantPlayerNames || objects.LocalPlayer is not { } localPlayer)
        {
            QueueRedraw();
            return;
        }

        var distance = GetDistance();
        if (distance != lastDistance)
        {
            // Previously suppressed plates may need their text rebuilt when the range changes.
            QueueRedraw();
            lastDistance = distance;
        }
        var origin = localPlayer.Position;
        foreach (var handler in handlers)
        {
            if (handler.NamePlateIndex < 0 || handler.NamePlateIndex >= AddonNamePlate.NumNamePlateObjects ||
                handler.NamePlateKind != NamePlateKind.PlayerCharacter ||
                handler.PlayerCharacter is not { } player || player.GameObjectId == localPlayer.GameObjectId ||
                Vector3.DistanceSquared(origin, player.Position) <= distance * distance)
                continue;

            // Native visibility is recomputed by the game. Only suppress distant text; never force it on.
            if ((handler.VisibilityFlags & DrawNameText) != 0)
            {
                handler.VisibilityFlags &= ~DrawNameText;
                hasHiddenNameplates = true;
            }
        }
    }
}
