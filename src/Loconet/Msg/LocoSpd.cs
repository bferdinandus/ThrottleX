using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class LocoSpd : LocoBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xA0;

    public static byte Length => 4;

    /// <summary>
    /// 0=stop;
    /// 1=estop;
    /// 2..127=speed steps
    /// </summary>
    public readonly Field7Bit Spd = new(2);

    /// <summary>
    /// Parameterless constructor for reflective instantiation
    /// </summary>
    public LocoSpd() 
    { }

    /// <summary>
    /// Construct for sending with values
    /// </summary>
    /// <param name="slotNumber"></param>
    /// <param name="spd"></param>
    public LocoSpd(byte slotNumber, byte spd)
    {
        Slot.Value = slotNumber;
        Spd.Value  = spd;
    }
}
