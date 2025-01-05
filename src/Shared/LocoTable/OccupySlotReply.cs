using Loconet.Msg;
using Loconet.Msg.Accessor;
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
    public readonly (int Number, bool IsOn)[] SlotFunctions;

    public OccupySlotReply(SlRdData slotData) 
    : this()
    {
        SlotSpeed.LocoNet = slotData.Spd.Value;
        SlotDirection = slotData.Dirf.ToDirection();
        SlotFunctions = new[]
        {
            (0, slotData.Dirf[EDirf.F0]),
            (1, slotData.Dirf[EDirf.F1]),
            (2, slotData.Dirf[EDirf.F2]),
            (3, slotData.Dirf[EDirf.F3]),
            (4, slotData.Dirf[EDirf.F4]),
            (5, slotData.Snd[ESlotSound.F5]),
            (6, slotData.Snd[ESlotSound.F6]),
            (7, slotData.Snd[ESlotSound.F7]),
            (8, slotData.Snd[ESlotSound.F8]),
        };
    }
}
