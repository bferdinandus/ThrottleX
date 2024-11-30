using Shared.Models;
using System;

namespace Shared.LocoTable;

public interface ILoconet2Row : ICommon2Row
{
    /// <summary>
    /// Loconet connection calls this, when polling the loco row and compares the
    /// return value with the state of the slot.
    /// </summary>
    /// <returns>true if the loco is activated by a wiThrottle client _and_ is enabled for the
    /// LocoNet connection specified as parameter</returns>
    /// <param name="loconetClient">Index of the LocoNet client</param>
    bool IsLocoActivatedForThisLoconet(int loconetClient);

    /// <summary>
    /// Loconet connection calls this, when the current state of a loco address was queried from the command station successfully.
    /// As long as the state is still Requesting, this implies a transition to Operational.
    /// This is the only case where the Loconet send thread writes to the speed/dir/functions in the row,
    /// </summary>
    /// <param name="loconetClient">Index of the LocoNet client</param>
    /// <param name="slotReply">What did we learn as current state of the loco?</param>
    void DeliverCommandStationState(int loconetClient, OccupySlotReply slotReply);

    /// <summary>
    /// Loconet connection calls this if fetching of data from command station ran into a non recoverable error
    /// like timeout or bad reply data.
    /// As long as the state is still Requesting, this implies a transition to Inactive.
    /// </summary>
    void FetchingFromCommandStationFailed(int loconetClient);

    /// <summary>
    /// Loconet connection calls this if fetching of data from command station revealed that the address
    /// is already controlled by another throttle device.
    /// As long as the state is still Requesting, this implies a transition to Inactive.
    /// </summary>
    void FetchingFromCommandStationOccupied(int loconetClient);

    /// <summary>
    /// Return the speed information stored by the loco table row. This could be the init value
    /// from the command station or an update set by wiFRED. Calling Loconet connection compares
    /// this with its last update sent to the command station and sends it out, if different.
    /// TBD special handling for ensuring an emergency stop does not get lost between two updates.
    /// </summary>
    Speed RequestedSpeed { get; }

    /// <summary>
    /// Return the direction information stored by the loco table row. This could be the init value
    /// from the command station or an update set by wiFRED. Calling Loconet connection compares
    /// this with its last update sent to the command station and sends it out, if different.
    Direction RequestedDirection { get; }

    /// <summary>
    /// Return the counter that increments whenever the wiThrottle wants to issure an emergency
    /// stop. This is not another value of the speed in order to ensure that emergency stop
    /// has priority and is never skipped by a subsequent change in speed.
    /// </summary>
    int EmergencyStopCounter { get; }

    /// <summary>
    /// When we occupy the slot, this ID shall be written into it.
    /// Plan is to set ID2=1 and use least 7 bits of IP address of wiFRED for ID1 maybe.
    /// From locope slot data explaination:
    /// ID1 is "7 bit ls ID code written by THROTTLE/PC when STAT2.4=1"
    /// ID2 is "7 bit ms ID code written by THROTTLE/PC when STAT2.4=1"
    /// From locope OPC_WR_SL_DATA:
    /// ID1/ID2 are two 7 bit values encoding a 14 bit unique DEVICE usage ID
    /// ID1/ID2#'s:
    /// 00/00 	       -means NO ID being used
    /// 01/00 to 7F/01 -ID shows PC usage.Lo nibble is TYP PC# (PC can use hi values)
    /// 00/02 to 7F/03 -SYSTEM reserved
    /// 00/04 to 7F/7E -NORMAL throttle RANGE
    /// </summary>
    (byte id1, byte id2) SlotId { get; }
}
