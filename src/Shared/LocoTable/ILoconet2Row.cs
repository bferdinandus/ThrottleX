using Shared.Models;

namespace Shared.LocoTable;

public interface ILoconet2Row
{
    /// <summary>
    /// The loco address. 
    /// This stays constant. A loco row is created for a certain address and this row never changes the address.
    /// This implies thread safety without any need for locking.
    /// </summary>
    IAddress Address { get; }

    /// <summary>
    /// This shall return the current value of the state.
    /// Access to this state must be synchronized. Different threads read _and_ write to this state.
    /// </summary>
    ELocoRowState LocoRowState { get; }

    /// <summary>
    /// Loconet connection calls this, when the current state of a loco address was queried from the command station successfully.
    /// As long as the state is still Requesting, this implies a transition to Operational.
    /// This is the only case where the Loconet send thread writes to the speed/dir/functions in the row,
    /// TBD if we need synchronization.
    /// </summary>
    /// <param name="speed">Speed</param>
    /// <param name="dir">Direction of travel</param>
    /// <param name="functions">Functions that are delivered by the command station. Higher functions can not be queried from the slot data</param>
    void DeliverCommandStationState(Speed speed, Direction dir, (int index, FunctionButton state)[] functions);

    /// <summary>
    /// Loconet connection calls this if fetching of data from command station ran into a non recoverable error
    /// like the slot is already controlled by another throttle ID.
    /// As long as the state is still Requesting, this implies a transition to Inactive.
    /// If the state is already Operational, than another Loconet connection already called DeliverCommandStationState(),
    /// what shall we do in this race condition?
    /// </summary>
    void FetchingFromCommandStationFailed();

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
}
