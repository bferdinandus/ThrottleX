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
    public readonly Field7Bit Slot;

    /// <summary>
    /// Bit field for status 1
    /// </summary>
    public readonly BitField7Bit<ESlotStatus1> Stat;
    public readonly BitGroupAccessEnum<ESlotStatusBusyActive> StatBusyActive;

    /// <summary>
    /// Lower bits of address or short address
    /// </summary>
    public readonly Field7Bit Adr;

    /// <summary>
    /// Speed
    /// </summary>
    public readonly Field7Bit Spd;

    /// <summary>
    /// Direction and F0..F4
    /// </summary>
    public readonly BitField7Bit<EDirf> Dirf;

    /// <summary>
    /// Global track status of command station, read only
    /// </summary>
    public readonly Field7Bit Trk;

    /// <summary>
    /// Bit field slot status 2
    /// </summary>
    public readonly Field7Bit SS2;

    /// <summary>
    /// High address bits of long address or zero for short address
    /// </summary>
    public readonly Field7Bit Adr2;

    /// <summary>
    /// "Slot sound" contains F5..F8
    /// </summary>
    public readonly Field7Bit Snd;

    /// <summary>
    /// First byte of device ID
    /// </summary>
    public readonly Field7Bit Id1;

    /// <summary>
    /// Second byte of device ID
    /// </summary>
    public readonly Field7Bit Id2;

    protected SlotDataBase() 
    {
        Slot = new(this, 2);
        Stat = new(this, 3);
        Adr  = new(this, 4);
        Spd  = new(this, 5);
        Dirf = new(this, 6);
        Trk  = new(this, 7);
        SS2  = new(this, 8);
        Adr2 = new(this, 9);
        Snd  = new(this, 10);
        Id1  = new(this, 11);
        Id2  = new(this, 12);

        StatBusyActive = BitGroupAccessEnum<ESlotStatusBusyActive>.Make(Stat, ESlotStatus1.Active, ESlotStatus1.Busy);
    }
}
