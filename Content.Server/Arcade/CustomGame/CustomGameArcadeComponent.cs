using Content.Shared.Arcade;

[RegisterComponent]
public sealed partial class CustomGameArcadeComponent : SharedCustomGameArcadeComponent
{
    //Player that is currently using this arcade
    public EntityUid? Player = null;
    //Players that are spectating (not controlling) this arcade
    public readonly List<EntityUid> Spectators = new();

    [DataField]
    public int DebugValue { get; set; } = 1024;
}

