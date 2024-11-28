using Loconet.Msg;
using Microsoft.Extensions.Logging;

namespace ThrottleX.Core.Loconet;

public class SlotData
{
    public byte SlotNumber;
    public byte Status;
    public byte Status2;
    public (byte Low, byte High) Address;
    public byte Dirf;
    public byte Speed;
    public byte Sound;
    public byte Id1;
    public byte Id2;

    public enum ControlState
    {
        /// <summary>
        /// We know about this slot only because we are received it from our loconet
        /// </summary>
        None = 0,
    }

    public ControlState Control { get; private set; } = ControlState.None;

    private int _emergencyStopCounter = 0;

    public SlotData(SlotDataBase slotMsg)
    {
        Set(slotMsg);
    }

    public void SetBySlotMsg(SlotDataBase slotMsg, ILogger _logger)
    {
        void Check(string name, byte newValue, byte oldValue)
        {
            if (newValue != oldValue)
                _logger.LogDebug($"field {name} changes from {oldValue:X02} to 0x{newValue:X02}");
        }

        _logger.LogInformation($"Storing new values for slot number {SlotNumber} from {slotMsg.GetType().Name}");

        Check("Stat", slotMsg.Stat.Value, Status);
        Check("SS2",  slotMsg.SS2.Value,  Status2);
        Check("Adr2", slotMsg.Adr2.Value, Address.High);
        Check("Adr",  slotMsg.Adr.Value,  Address.Low);
        Check("Dirf", slotMsg.Dirf.Value, Dirf);
        Check("Spd",  slotMsg.Spd.Value,  Speed);
        Check("Snd",  slotMsg.Snd.Value,  Sound);
        Check("Id1",  slotMsg.Id1.Value,  Id1);
        Check("Id2",  slotMsg.Id2.Value,  Id2);

        Set(slotMsg);
    }

    private void Set(SlotDataBase slotMsg)
    {
        SlotNumber = slotMsg.Slot.Value;
        Status = slotMsg.Stat.Value;
        Status2 = slotMsg.SS2.Value;
        Address.High = slotMsg.Adr2.Value;
        Address.Low = slotMsg.Adr.Value;
        Dirf = slotMsg.Dirf.Value;
        Speed = slotMsg.Spd.Value;
        Sound = slotMsg.Snd.Value;
        Id1 = slotMsg.Id1.Value;
        Id2 = slotMsg.Id2.Value;
    }

    /// <summary>
    /// Did wiThrottle request an emergency stop?
    /// 
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
        Speed = 1;
        return true;
    }
}
