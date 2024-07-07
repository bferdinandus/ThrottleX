namespace Loconet.Msg.Accessor;

/// <summary>
/// Values of SL_ACTIVE and SL_BUSY in slot status 1
/// </summary>
public enum ESlotStatusBusyActive
{
    /// <summary>loco adr in SLOT  	-REFRESHED</summary>
    IN_USE = 0b11,
    /// <summary>loco adr in SLOT  	-NOT refreshed</summary>
    IDLE = 0b10,
    /// <summary>loco adr IN SLOT     -refreshed</summary>
    COMMON = 0b01,
    /// <summary>no valid DATA      -not refreshed</summary>
    FREE = 0b00
}
