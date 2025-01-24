using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class LocoSnd : LocoBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xA2;

    public static byte Length => 4;

    public readonly BitField7Bit<ESlotSound> Snd = new(2);

    /// <summary>
    /// Parameterless constructor for reflective instantiation
    /// </summary>
    public LocoSnd()
    { }

    /// <summary>
    /// Construct for sending with values
    /// </summary>
    /// <param name="slotNumber"></param>
    /// <param name="snd"></param>
    public LocoSnd(byte slotNumber, byte snd)
    {
        Slot.Value = slotNumber;
        Snd.Value = snd;
    }
}
