using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public TacticalDoor? DungeonDoorAt(WorldPosition position)
    {
        if (State.Tactics is not { } tactics) return null;
        for (int i = 0; i < tactics.Doors.Nodes.Count; i++)
            if (tactics.Doors.Nodes[i].Position == position) return tactics.Doors.Nodes[i];
        return null;
    }
}
