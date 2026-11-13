using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;

namespace ReflavorRP;

// Shared result of incoming chat processing. Consume synchronously: Source and Speaker are borrowed.
public sealed class StyledChatMessage(IChatMessage source, IReadOnlyList<RpTextSegment> pieces,
    IPlayerCharacter? speaker, bool isOwn)
{
    public IChatMessage Source { get; } = source;
    public XivChatType Kind { get; } = source.LogKind;
    public IReadOnlyList<RpTextSegment> Pieces { get; } = pieces;
    public bool IsOoc { get; } = pieces.Count > 0 && pieces.All(piece => piece.Kind == RpTextKind.Ooc);
    public bool IsOwn { get; } = isOwn;
    public IPlayerCharacter? Speaker { get; } = speaker;
    public bool IsTell => RpChannels.IsTell(Kind);
    public bool IsPublic => RpChannels.IsPublic(Kind);
    public bool HasSpeech { get; } = pieces.Any(piece => piece.Speech && !string.IsNullOrWhiteSpace(piece.Text));
}
