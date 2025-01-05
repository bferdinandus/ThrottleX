using Loconet.Msg.Accessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg;

/// <summary>
/// Base class for OPC_SL_RD_DATA and OPC_WR_SL_DATA
/// </summary>
public class SlotDataBase : FormatBase
{
    /// <summary>
    /// Id of slot is index in slot table of command station
    /// </summary>
    public readonly Field7Bit Slot = new(2);

    /// <summary>
    /// Bit field for status 1
    /// </summary>
    public readonly BitField7Bit<ESlotStatus1> Stat = new(3);
    public readonly BitGroupAccessEnum<ESlotStatusBusyActive> StatBusyActive;

    /// <summary>
    /// Lower bits of address or short address
    /// </summary>
    public readonly Field7Bit Adr  = new(4);

    /// <summary>
    /// Speed
    /// </summary>
    public readonly Field7Bit Spd  = new(5);

    /// <summary>
    /// Direction and F0..F4
    /// </summary>
    public readonly BitField7Bit<EDirf> Dirf = new(6);

    /// <summary>
    /// Global track status of command station, read only
    /// </summary>
    public readonly Field7Bit Trk = new(7);

    /// <summary>
    /// Bit field slot status 2
    /// </summary>
    public readonly Field7Bit SS2 = new(8);

    /// <summary>
    /// High address bits of long address or zero for short address
    /// </summary>
    public readonly Field7Bit Adr2 = new(9);

    /// <summary>
    /// "Slot sound" contains F5..F8
    /// </summary>
    public readonly BitField7Bit<ESlotSound> Snd = new(10);

    /// <summary>
    /// First byte of device ID
    /// </summary>
    public readonly Field7Bit Id1 = new(11);

    /// <summary>
    /// Second byte of device ID
    /// </summary>
    public readonly Field7Bit Id2 = new(12);

    public (byte Id1, byte Id2) Id => (this.Id1.Value, this.Id2.Value);

    protected SlotDataBase() 
    {
        StatBusyActive = BitGroupAccessEnum<ESlotStatusBusyActive>.Make(Stat, ESlotStatus1.Active, ESlotStatus1.Busy);
    }

    protected SlotDataBase(SlotDataBase copyFrom)
        : this()
    {
        Slot.Value = copyFrom.Slot.Value;
        Stat.Value = copyFrom.Stat.Value;
        Adr .Value = copyFrom.Adr.Value;
        Spd .Value = copyFrom.Spd.Value;
        Dirf.Value = copyFrom.Dirf.Value;
        Trk .Value = copyFrom.Trk.Value;
        SS2 .Value = copyFrom.SS2.Value;
        Adr2.Value = copyFrom.Adr2.Value;
        Snd .Value = copyFrom.Snd.Value;
        Id1 .Value = copyFrom.Id1.Value;
        Id2 .Value = copyFrom.Id2.Value;
    }
}
