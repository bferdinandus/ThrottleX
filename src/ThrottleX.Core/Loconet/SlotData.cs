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

    public SlotData(SlotDataBase slotMsg)
    {
        Set(slotMsg);
    }

    public void SetBySlotMsg(SlotDataBase slotMsg, ILogger _logger)
    {
        void Check(string name, byte newValue, byte oldValue)
        {
            if (newValue != oldValue)
                _logger.LogDebug("field {Name} changes from 0x{OldValue:X02} to 0x{NewValue:X02}", name, oldValue, newValue);
        }

        _logger.LogInformation("Storing new values for slot number {SlotNumber} from {Name}", SlotNumber, slotMsg.GetType().Name);

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
}
