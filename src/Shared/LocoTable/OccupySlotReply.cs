using Shared.Models;

namespace Shared.LocoTable;

/// <summary>
/// Message from SendThread to LocoTable about success after trying to
/// occupy a slot.
/// </summary>
public struct OccupySlotReply
{
    public Speed SlotSpeed;
    public Direction SlotDirection;
    public (int index, FunctionButton state)[] SlotFunctions;

    public OccupySlotReply(Speed speed, Direction dir, (int index, FunctionButton state)[] funcs)
    {
        SlotSpeed = speed;
        SlotDirection = dir;
        SlotFunctions = funcs;
    }
}
