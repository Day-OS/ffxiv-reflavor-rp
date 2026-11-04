using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;
using Dalamud.Interface.Utility;
using Dalamud.Plugin.Services;
using SceneCameraManager = FFXIVClientStructs.FFXIV.Client.Graphics.Scene.CameraManager;

namespace ReflavorRP;

public sealed class ChatBubbles(IObjectTable objects, IGameGui gameGui, Configuration config)
{
    internal sealed record Bubble(long Id, string Name, uint World, string Text, XivChatType Kind,
        long DurationMilliseconds, long ExpiresAt = 0, bool Speech = true, bool Ooc = false,
        long LastUpdatedAt = 0, bool WasVisible = true, bool WasHovered = false);

    internal readonly record struct BubbleBounds(Vector2 Min, Vector2 Max);

    private readonly Dictionary<uint, List<Bubble>> bubbles = [];
    private long nextBubbleId;

    public void Add(StyledChatMessage message)
    {
        var speaker = message.Speaker;
        var pieces = message.Pieces;
        var isOoc = message.IsOoc;
        if (speaker == null || pieces.Count == 0)
            return;

        // Keep values, not the borrowed chat message or a character pointer, between frames.
        var now = Environment.TickCount64;
        var name = speaker.Name.TextValue;
        var world = speaker.HomeWorld.RowId;
        if (!bubbles.TryGetValue(speaker.EntityId, out var messages))
        {
            messages = [];
            bubbles.Add(speaker.EntityId, messages);
        }
        if (messages.Count > 0 && (messages[^1].Name != name || messages[^1].World != world))
            messages.Clear();
        foreach (var (speech, content) in pieces)
        {
            if (string.IsNullOrWhiteSpace(content))
                continue;
            var text = content;
            if (isOoc)
                text = "(( " + content + " ))";
            else if (!speech)
            {
                if (message.Kind == XivChatType.CustomEmote)
                    text = name + " " + text;
                text = "* " + text;
            }
            var duration = GetDurationMilliseconds(text, config.ChatBubbleReadingSpeed);
            messages.Add(new Bubble(nextBubbleId++, name, world, text, message.Kind,
                duration, Speech: speech, Ooc: isOoc));
        }
        AdvanceQueue(messages, now);
    }

    public unsafe void Draw()
    {
        var localPlayer = objects.LocalPlayer;
        if (!config.Enabled || !config.StyleIncomingChat || !config.AlternativeChatBubbles || localPlayer == null)
        {
            bubbles.Clear();
            return;
        }

        var now = Environment.TickCount64;
        var scale = ImGuiHelpers.GlobalScale;
        var cameraManager = SceneCameraManager.Instance();
        var camera = cameraManager == null ? null : cameraManager->CurrentCamera;
        Vector3 cameraPosition = camera == null ? default : camera->Position;
        var visible = new List<(Bubble Bubble, List<Bubble> Queue, Vector2 Tip, Vector2 Anchor, float Opacity,
            bool HasTail, int HiddenCount)>();
        foreach (var (id, messages) in bubbles)
        {
            // Charge the previous frame's elapsed time before checking visibility again.
            AdvanceQueue(messages, now);
            if (messages.Count == 0)
            {
                bubbles.Remove(id);
                continue;
            }

            messages[0] = messages[0] with { WasVisible = false, WasHovered = false };

            var latest = messages[^1];
            var character = localPlayer.EntityId == id ? localPlayer
                : objects.PlayerObjects.OfType<IPlayerCharacter>().FirstOrDefault(player => player.EntityId == id);
            if (character == null || !character.IsValid() ||
                character.Name.TextValue != latest.Name || character.HomeWorld.RowId != latest.World)
            {
                bubbles.Remove(id);
                continue;
            }

            if (camera == null)
                continue;

            var height = ChatSpatial.GetCharacterHeight(character);
            var head = character.Position + new Vector3(0, height + 0.25f, 0);
            if (!gameGui.WorldToScreen(head, out var screen))
                continue;

            var face = character.Position + new Vector3(0, height * 0.9f, 0);
            if (!gameGui.WorldToScreen(face, out var faceScreen))
                continue;
            if (gameGui.ScreenToWorld(faceScreen, out var hit))
            {
                if (Vector3.Distance(cameraPosition, hit) + 0.05f < Vector3.Distance(cameraPosition, face))
                    continue;
            }

            // Gradually fade from 3 to 30 yalms, keeping text at its normal size.
            var distance = Vector3.Distance(localPlayer.Position, character.Position);
            var distanceOpacity = 0.3f + 0.7f * ChatSpatial.GetDistanceStrength(distance, 30);
            // Pixel offsets at the reference camera distance; no maximum as the camera approaches.
            const float referenceDistance = 18f;
            const float verticalOffset = 60f;
            const float tailHeight = 12f;
            var cameraDistance = Vector3.Distance(cameraPosition, head);
            var offsetScale = referenceDistance / Math.Max(cameraDistance, 0.1f);
            var bubbleTip = screen - new Vector2(0, verticalOffset * scale * offsetScale);
            var anchor = bubbleTip - new Vector2(0, tailHeight * scale * offsetScale);
            var visibleCount = Math.Min(messages.Count, Math.Clamp(config.MaxVisibleChatBubbles, 1, 10));
            for (var index = 0; index < visibleCount; index++)
            {
                var bottom = index == visibleCount - 1;
                visible.Add((messages[index], messages, bubbleTip, anchor, distanceOpacity,
                    bottom, bottom ? messages.Count - visibleCount : 0));
            }
        }

        // Place newer messages first, so every collision moves the older message above them.
        var placed = new List<BubbleBounds>();
        foreach (var item in visible.OrderByDescending(item => item.Bubble.Id))
        {
            var bubble = item.Bubble;
            var anchor = item.Anchor;
            var bubbleTip = item.Tip;
            var isActive = bubble.ExpiresAt != 0;
            var hasTail = item.HasTail;
            var opacity = isActive ? Math.Min(1f, (bubble.ExpiresAt - now) / 750f) : 1f;
            opacity *= item.Opacity;
            var isAction = !bubble.Speech && !bubble.Ooc;
            var background = new Vector4(1f, 1f, 1f, 0.98f);
            var border = new Vector4(0.7f, 0.68f, 0.74f, 1f);
            var textColor = new Vector4(0.14f, 0.12f, 0.17f, 1f);
            var padding = new Vector2(12, 8) * scale;
            if (isAction)
            {
                background = new Vector4(0.89f, 0.82f, 0.96f, 0.98f);
                border = new Vector4(0.57f, 0.35f, 0.73f, 1f);
                textColor = new Vector4(0.3f, 0.16f, 0.41f, 1f);
                padding = new Vector2(18, 16) * scale;
            }
            else if (bubble.Ooc)
            {
                background = new Vector4(0.87f, 0.87f, 0.89f, 0.9f);
                border = new Vector4(0.58f, 0.58f, 0.62f, 0.55f);
                textColor = new Vector4(0.32f, 0.32f, 0.36f, 1f);
            }
            var isTell = bubble.Kind is XivChatType.TellIncoming or XivChatType.TellOutgoing;
            var heading = isTell ? "[Tell] " + bubble.Name : bubble.Name;
            const float headingScale = 0.8f;
            var headingSize = ImGui.CalcTextSize(heading) * headingScale;
            var queueLabel = item.HiddenCount > 0 ? item.HiddenCount.ToString(CultureInfo.InvariantCulture) : "";
            var queueLabelSize = ImGui.CalcTextSize(queueLabel) * headingScale;
            var queueBadgeRadius = item.HiddenCount > 0
                ? Math.Max(10 * scale, queueLabelSize.Length() / 2 + 3 * scale) : 0;
            var labelHeight = headingSize.Y + 2 * scale;
            // Keep the emote's 3px ornaments below the nameplate, then leave room for text.
            var actionOrnamentY = labelHeight / 2 + 7 * scale;
            var textTop = isAction ? actionOrnamentY + 8 * scale : labelHeight / 2 + 3 * scale;
            var headingSpace = Math.Max(0, textTop - padding.Y);
            var contentWidth = Math.Max(ImGui.CalcTextSize(bubble.Text).X, headingSize.X + 6 * scale);
            var width = Math.Clamp(contentWidth + padding.X * 2,
                90 * scale, 340 * scale);
            var textSize = ImGui.CalcTextSize(bubble.Text, false, width - padding.X * 2);
            // Leave 8px between the text and the lower ornament's tip (11px inset + 3px radius).
            var bottomPadding = isAction ? 22 * scale : padding.Y;
            var windowHeight = Math.Max(ImGui.GetStyle().WindowMinSize.Y,
                textSize.Y + headingSpace + padding.Y + bottomPadding);
            // Include the nameplate, countdown bar and short tail in collision bounds.
            var bounds = new BubbleBounds(
                new Vector2(anchor.X - width / 2, anchor.Y - windowHeight - labelHeight / 2 - scale),
                new Vector2(anchor.X + width / 2 + Math.Max(10 * scale, queueBadgeRadius + scale),
                    Math.Max(hasTail ? bubbleTip.Y : anchor.Y, anchor.Y + queueBadgeRadius + scale)));
            var placement = PlaceAboveOverlaps(bounds, placed, 6 * scale);
            var shift = placement.Min.Y - bounds.Min.Y;
            anchor.Y += shift;
            bubbleTip.Y += shift;
            placed.Add(placement);
            ImGui.SetNextWindowPos(anchor, ImGuiCond.Always, new Vector2(0.5f, 1f));
            ImGui.SetNextWindowSize(new Vector2(width, windowHeight), ImGuiCond.Always);
            ImGuiHelpers.ForceNextWindowMainViewport();
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, opacity);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, padding);
            var thickness = (isAction ? 1.5f : 1f) * scale;
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, thickness);
            ImGui.PushStyleColor(ImGuiCol.Text, textColor);

            var flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoBackground |
                ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing |
                ImGuiWindowFlags.NoBringToFrontOnFocus;
            if (ImGui.Begin($"###ReflavorRPBubble_{bubble.Id}", flags))
            {
                if (isActive)
                    item.Queue[0] = item.Queue[0] with { WasVisible = true };
                var draw = ImGui.GetWindowDrawList();
                var top = ImGui.GetWindowPos();
                var size = ImGui.GetWindowSize();
                background.W *= opacity;
                border.W *= opacity;
                var decoration = ImGui.ColorConvertFloat4ToU32(border);
                var backgroundColor = ImGui.ColorConvertFloat4ToU32(background);
                // Keep the upper-left corner square where the nameplate meets the body.
                const ImDrawFlags roundedCorners = ImDrawFlags.RoundCornersTopRight |
                    ImDrawFlags.RoundCornersBottomLeft | ImDrawFlags.RoundCornersBottomRight;
                var rounding = (isAction ? 14 : 10) * scale;
                draw.PushClipRect(top - new Vector2(thickness), top + size + new Vector2(thickness), false);
                draw.AddRectFilled(top, top + size, backgroundColor, rounding, roundedCorners);
                draw.AddRect(top, top + size, decoration, rounding, roundedCorners, thickness);
                draw.PopClipRect();
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + headingSpace);
                ImGui.TextWrapped(bubble.Text);
                // One drawing area includes the nameplate above and the bar and tail outside the box.
                var clipBottom = top + size + new Vector2(Math.Max(10 * scale, queueBadgeRadius + thickness),
                    Math.Max(thickness, queueBadgeRadius + thickness));
                if (hasTail)
                    clipBottom.Y = Math.Max(clipBottom.Y, bubbleTip.Y + thickness);
                draw.PushClipRect(top - new Vector2(scale, labelHeight / 2 + scale), clipBottom, false);
                if (isAction)
                {
                    // An inset frame and matching filigree above and below the narration.
                    draw.AddRect(top + new Vector2(4, 4) * scale,
                        top + size - new Vector2(4, 4) * scale, decoration, 10 * scale, roundedCorners);
                    for (var edge = 0; edge < 2; edge++)
                    {
                        var y = edge == 0 ? actionOrnamentY : size.Y - 11 * scale;
                        var left = top + new Vector2(16 * scale, y);
                        var right = top + new Vector2(size.X - 16 * scale, y);
                        var center = top + new Vector2(size.X / 2, y);
                        var horizontal = new Vector2(3 * scale, 0);
                        var vertical = new Vector2(0, 3 * scale);
                        draw.AddLine(left + horizontal * 2, center - horizontal * 3, decoration, scale);
                        draw.AddLine(center + horizontal * 3, right - horizontal * 2, decoration, scale);
                        draw.AddQuadFilled(center - horizontal, center - vertical,
                            center + horizontal, center + vertical, decoration);
                        draw.AddQuad(left - horizontal, left - vertical,
                            left + horizontal, left + vertical, decoration, scale);
                        draw.AddQuad(right - horizontal, right - vertical,
                            right + horizontal, right + vertical, decoration, scale);
                        draw.AddCircleFilled(center - horizontal * 2, scale, decoration);
                        draw.AddCircleFilled(center + horizontal * 2, scale, decoration);
                    }
                }
                else if (bubble.Ooc)
                {
                    // Muted corner accents and dots make OOC look like an aside.
                    var corner = top + new Vector2(5, 5) * scale;
                    var opposite = top + size - new Vector2(5, 5) * scale;
                    var horizontal = new Vector2(8 * scale, 0);
                    var vertical = new Vector2(0, 5 * scale);
                    draw.AddLine(corner, corner + horizontal, decoration, scale);
                    draw.AddLine(corner, corner + vertical, decoration, scale);
                    draw.AddLine(opposite, opposite - horizontal, decoration, scale);
                    draw.AddLine(opposite, opposite - vertical, decoration, scale);
                    for (var dot = -1; dot <= 1; dot++)
                    {
                        var position = top + new Vector2(size.X / 2 + dot * 4 * scale, size.Y - 4 * scale);
                        draw.AddCircleFilled(position, scale, decoration);
                    }
                }
                else
                {
                    // A light inner rim keeps speech bubbles softer than the emote frame.
                    var highlight = ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.65f * opacity));
                    draw.AddRect(top + new Vector2(3, 3) * scale,
                        top + size - new Vector2(3, 3) * scale, highlight, 7 * scale, roundedCorners);
                    draw.AddLine(top + new Vector2(14 * scale, size.Y - 4 * scale),
                        top + new Vector2(size.X - 14 * scale, size.Y - 4 * scale), decoration, scale);
                }
                // Center the nameplate vertically on the body's upper border.
                var labelTop = top - new Vector2(0, labelHeight / 2);
                var labelWidth = Math.Min(headingSize.X + 6 * scale, size.X);
                var labelBottom = labelTop + new Vector2(labelWidth, labelHeight);
                var headingColor = new Vector4(textColor.X, textColor.Y, textColor.Z, 0.85f * opacity);
                if (isTell)
                    headingColor = new Vector4(0.58f, 0.3f, 0.02f, 0.9f * opacity);
                draw.AddRectFilled(labelTop, labelBottom, backgroundColor, 3 * scale);
                draw.AddRect(labelTop, labelBottom, decoration, 3 * scale);
                draw.PushClipRect(labelTop, labelBottom, true);
                draw.AddText(ImGui.GetFont(), ImGui.GetFontSize() * headingScale,
                    labelTop + new Vector2(3, 1) * scale, ImGui.ColorConvertFloat4ToU32(headingColor), heading);
                draw.PopClipRect();

                if (isActive)
                {
                    var remaining = Math.Clamp((bubble.ExpiresAt - now) / (float)bubble.DurationMilliseconds, 0f, 1f);
                    var barTop = top + new Vector2(size.X + 5 * scale, 0);
                    var barBottom = top + new Vector2(size.X + 9 * scale, size.Y);
                    var track = ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.13f, 0.18f, 0.25f * opacity));
                    draw.AddRectFilled(barTop, barBottom, track, 2 * scale);
                    if (remaining > 0f)
                    {
                        var fillTop = new Vector2(barTop.X, barBottom.Y - (barBottom.Y - barTop.Y) * remaining);
                        draw.AddRectFilled(fillTop, barBottom, decoration, 2 * scale);
                    }
                }

                if (hasTail)
                {
                    var bottom = top + new Vector2(size.X / 2, size.Y);
                    var overlap = thickness + scale;
                    var left = bottom - new Vector2(6 * scale, overlap);
                    var right = bottom + new Vector2(6 * scale, -overlap);
                    draw.AddTriangleFilled(left, right, bubbleTip, backgroundColor);
                    draw.AddLine(left, bubbleTip, decoration, thickness);
                    draw.AddLine(bubbleTip, right, decoration, thickness);
                }
                if (item.HiddenCount > 0)
                {
                    // Like the nameplate, the counter overlaps the body's border instead of taking text space.
                    var center = top + size;
                    draw.AddCircleFilled(center, queueBadgeRadius, backgroundColor);
                    draw.AddCircle(center, queueBadgeRadius, decoration, 0, thickness);
                    draw.AddText(ImGui.GetFont(), ImGui.GetFontSize() * headingScale,
                        center - queueLabelSize / 2, ImGui.ColorConvertFloat4ToU32(headingColor), queueLabel);
                }
                draw.PopClipRect();
                // A real ImGui item captures the click instead of also clicking the world behind it.
                ImGui.SetCursorScreenPos(top);
                if (ImGui.InvisibleButton("DismissBubble", size))
                {
                    // Expire the oldest (topmost) bubble; the normal timer update advances it next frame.
                    item.Queue[0] = item.Queue[0] with
                    {
                        ExpiresAt = now, LastUpdatedAt = now, WasVisible = true, WasHovered = false,
                    };
                }
                else if (isActive && ImGui.IsItemHovered())
                    item.Queue[0] = item.Queue[0] with { WasHovered = true };
                if (ImGui.IsItemHovered())
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            }
            ImGui.End();
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(3);
        }

    }

    internal static BubbleBounds PlaceAboveOverlaps(BubbleBounds bounds, IReadOnlyList<BubbleBounds> placed, float gap)
    {
        // Bottom to top: moving up cannot hit an obstacle we have already passed.
        foreach (var other in placed.OrderByDescending(other => other.Max.Y))
        {
            if (bounds.Max.X <= other.Min.X || bounds.Min.X >= other.Max.X ||
                bounds.Max.Y <= other.Min.Y - gap || bounds.Min.Y >= other.Max.Y + gap)
                continue;

            var shift = new Vector2(0, other.Min.Y - gap - bounds.Max.Y);
            bounds = new BubbleBounds(bounds.Min + shift, bounds.Max + shift);
        }
        return bounds;
    }

    internal static long GetDurationMilliseconds(string text, float readingSpeed)
    {
        var characters = new StringInfo(text).LengthInTextElements;
        var readingTime = (long)Math.Ceiling(characters * 1000d / Math.Clamp(readingSpeed, 5f, 40f));
        return Math.Max(5000, 3000 + readingTime);
    }

    internal static void AdvanceQueue(List<Bubble> messages, long now)
    {
        if (messages.Count > 0 && messages[0].ExpiresAt != 0)
        {
            var active = messages[0];
            var elapsed = active.LastUpdatedAt == 0 ? 0 : Math.Max(0, now - active.LastUpdatedAt);
            // Hover pauses the timer; hidden bubbles run at roughly a quarter of normal speed.
            var extraTime = active.WasHovered ? elapsed
                : active.WasVisible ? 0 : (long)Math.Round(elapsed * 0.75);
            messages[0] = active with { ExpiresAt = active.ExpiresAt + extraTime, LastUpdatedAt = now };
        }

        if (messages.Count > 0 && messages[0].ExpiresAt != 0 && messages[0].ExpiresAt <= now)
            messages.RemoveAt(0);

        if (messages.Count > 0 && messages[0].ExpiresAt == 0)
            messages[0] = messages[0] with
            {
                ExpiresAt = now + messages[0].DurationMilliseconds,
                LastUpdatedAt = now,
            };
    }
}
