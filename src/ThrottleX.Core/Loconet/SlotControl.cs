using Loconet;
using Loconet.Msg;
using Loconet.Msg.Accessor;
using Shared.LocoTable;
using Shared.Models;
using static ThrottleX.Core.Loconet.SlotControl;

namespace ThrottleX.Core.Loconet;

public class SlotControl
{
    public enum State
    {
        /// <summary>
        /// We know about this slot only because we are received it from our loconet
        /// </summary>
        None = 0,
        /// <summary>
        /// We released control of the slot as requested by loco table (or at least we tried to release it)
        /// </summary>
        Inactive,
        /// <summary>
        /// We control the slot after occupying it as requested by loco table
        /// </summary>
        Active,
        /// <summary>
        /// We tried to activate, but failed (loconet timeout or bad reply from command station)
        /// </summary>
        Failed,
        /// <summary>
        /// We tried to activate, but determined that the slot is under control by another device
        /// </summary>
        Occupied
    }

    private readonly ILoconet2Row _row;
    private readonly LoconetClient _loconetClient;

    private int _emergencyStopCounter = 0;

    public State SlotState = State.None;
    public byte? SlotNumber = null;
    public byte? LastKnownStat1 = null;
    public byte? LastSentSpeed = null;
    public byte? LastSentDirf = null;
    public byte? LastSentSnd = null;

    private Dictionary<int, bool> _function = new();

    public SlotControl(ILoconet2Row row, LoconetClient loconetClient)
    {
        _row = row;
        _loconetClient = loconetClient;
    }

    /// <summary>
    /// Did wiThrottle request an emergency stop?
    /// If counter in slot and passed counter in loco table are not equal,
    /// this function returns true and resets the local counter to the passed
    /// counter from loco table. Nominally this can only be with the loco table
    /// counter being ahead.
    /// </summary>
    /// <param name="counterInLocoTable">copy of the counter from the loco table. This is incremented when wiThrottle requests emergency stop</param>
    /// <returns>false if both counters are equal</returns>
    public bool IsEmergencyStopRequested(int counterInLocoTable)
    {
        if (counterInLocoTable == _emergencyStopCounter)
            return false; // nothing changed, nothing to do

        _emergencyStopCounter = counterInLocoTable;
        return true;
    }

    public void SendSpeed(byte newSpeed)
    {
        LastSentSpeed = newSpeed;
        _loconetClient.BlockingSend(new LocoSpd(SlotNumber!.Value, newSpeed));
    }

    public void SendDirf(EDirf mask, bool newValue)
    {
        var bitMask = (byte)mask;
        var requestedDirf = (byte) (LastSentDirf!.Value & ~bitMask); // remove bit
        if (newValue)
            requestedDirf |= bitMask; // set bit if requested

        LastSentDirf = requestedDirf;
        _loconetClient.BlockingSend(new LocoDirf(SlotNumber!.Value, requestedDirf));
    }


    public void SendSnd(ESlotSound mask, bool newValue)
    {
        var bitMask = (byte)mask;
        var requestedSnd = (byte) (LastSentSnd!.Value & ~bitMask);
        if (newValue)
            requestedSnd |= bitMask;

        LastSentSnd = requestedSnd;
        _loconetClient.BlockingSend(new LocoSnd(SlotNumber!.Value, requestedSnd));
    }

    public void InitializeFromSlot(SlRdData slotData)
    {
        SlotNumber = slotData.Slot.Value;
        LastKnownStat1 = slotData.Stat.Value;
        LastSentSpeed = slotData.Spd.Value;
        LastSentDirf = slotData.Dirf.Value;
        LastSentSnd = slotData.Snd.Value;
    }

    public void SendFunction(int number, bool isOn)
    {
        switch (number)
        {
            case 0: SendDirf(EDirf.F0, isOn); break;
            case 1: SendDirf(EDirf.F1, isOn); break;
            case 2: SendDirf(EDirf.F2, isOn); break;
            case 3: SendDirf(EDirf.F3, isOn); break;
            case 4: SendDirf(EDirf.F4, isOn); break;
            case 5: SendSnd(ESlotSound.F5, isOn); break;
            case 6: SendSnd(ESlotSound.F6, isOn); break;
            case 7: SendSnd(ESlotSound.F7, isOn); break;
            case 8: SendSnd(ESlotSound.F8, isOn); break;
        }
    }
}
