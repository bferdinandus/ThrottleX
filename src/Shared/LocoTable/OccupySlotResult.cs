namespace Shared.LocoTable;

public enum OccupySlotResult
{
    /// <summary>
    /// This command station is not requested to activate the slot
    /// </summary>
    NotRequesting,
    /// <summary>
    /// Loco row thinks this command station must answer
    /// </summary>
    Requesting,
    /// <summary>
    /// Send thread of the respective LocoNet understood that it is supposed to occupy the slot,
    /// but the result is still pending
    /// </summary>
    Busy,
    /// <summary>
    /// Reply: we are under control (be it by NULL_MOVE, write-slot or nothing)
    /// </summary>
    Success,
    /// <summary>
    /// Reply: the slot is under control by some other throttle, we won't touch it!
    /// </summary>
    Occupied,
    /// <summary>
    /// Reply: Failed to occupy the slot by us (Loconet error or command station not cooperative)
    /// </summary>
    Failure
}
