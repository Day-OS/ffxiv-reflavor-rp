using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Text;

namespace ReflavorRP;

// Shared result of incoming chat processing. Consume synchronously: Source and Speaker are borrowed.
public sealed class StyledChatMessage(IChatMessage source, IReadOnlyList<(bool Speech, string Text)> pieces,
    bool isOoc, IPlayerCharacter? speaker, bool isOwn)
{
    public IChatMessage Source { get; } = source;
    public XivChatType Kind { get; } = source.LogKind;
    public IReadOnlyList<(bool Speech, string Text)> Pieces { get; } = pieces;
    public bool IsOoc { get; } = isOoc;
    public bool IsOwn { get; } = isOwn;
    public IPlayerCharacter? Speaker { get; } = speaker;
    public bool IsTell => Kind is XivChatType.TellIncoming or XivChatType.TellOutgoing;
    public bool HasSpeech { get; } = !isOoc && pieces.Any(piece => piece.Speech && !string.IsNullOrWhiteSpace(piece.Text));
}
