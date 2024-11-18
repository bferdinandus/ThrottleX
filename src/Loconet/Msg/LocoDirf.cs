using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class LocoDirf : LocoBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xA1;

    public static byte Length => 4;

    public readonly BitField7Bit<EDirf> Dirf = new(2);

    /// <summary>
    /// Parameterless constructor for reflective instantiation
    /// </summary>
    public LocoDirf()
    { }

    /// <summary>
    /// Construct for sending with values
    /// </summary>
    /// <param name="slotNumber"></param>
    /// <param name="dirf"></param>
    public LocoDirf(byte slotNumber, byte dirf)
    {
        Slot.Value = slotNumber;
        Dirf.Value = dirf;
    }
}
