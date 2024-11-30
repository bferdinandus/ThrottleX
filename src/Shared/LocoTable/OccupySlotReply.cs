using Loconet.Msg;
using Shared.Models;

namespace Shared.LocoTable;

/// <summary>
/// Message from SendThread to LocoTable about success after trying to
/// occupy a slot.
/// </summary>
public struct OccupySlotReply
{
    public readonly Speed SlotSpeed = new ();
    public readonly Direction SlotDirection;
    public readonly (int index, FunctionButton state)[] SlotFunctions;

    public OccupySlotReply(SlRdData slotData) : this()
    {
        SlotSpeed.LocoNet = slotData.Spd.Value;
        //TODO SlotDirection
        //TODO SlotFunctions
    }
}
