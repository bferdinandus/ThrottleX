using Loconet.Msg;

namespace Loconet;

/// <summary>
/// Detect different command station models by examination of slot 0.
/// One instance of this class is one known model and exists as static member of this class.
/// All instances can be queried with Enumerate()
/// </summary>
public class CommandStation
{
    private readonly static byte I = (byte)'I';
    private readonly static byte B = (byte)'B';
    private readonly static byte M = (byte)'M';

    public static readonly CommandStation Daisy         = new(0x44, 0x02, 0x59, 0x01, I, B, "DAISY");
    public static readonly CommandStation IbTcOld       = new(0x00, 0x02, 0x00, 0x00, I, B, "IB/TC 1 until 1.55");
    /// <summary>Maerklin 6021 Adapter from Uhlenbrock</summary>
    public static readonly CommandStation M6021Adapter  = new(0x4C, 0x01, 0x49, 0x02, I, B, "6021 Adapter");
    public static readonly CommandStation IbTcNew       = new(0x00, 0x02, 0x00, 0x00, I, B, "IB/TC 1 2.0");
    public static readonly CommandStation Intellibox2   = new(0x42, 0x03, 0x00, 0x15, I, B, "IB 2/Basic/COM");
    public static readonly CommandStation DigitraxChief = new(0x00, 0x00, 0x00, 0x00, 0, 0, "Digitrax Chief");
    public static readonly CommandStation Minibox1      = new(0x00, 0x00, 0x00, 0x00, M, B, "Minibox");

    public static IEnumerable<CommandStation> Enumerate()
    {
        yield return Daisy;
        yield return IbTcOld;
        yield return M6021Adapter;
        yield return DigitraxChief;
        yield return IbTcNew;
        yield return Intellibox2;
        yield return Minibox1;
    }

    public readonly byte? AddressLow;
    public readonly byte? Speed;
    public readonly byte? AddressHigh;
    public readonly byte? Sound;
    public readonly byte? Id1;
    public readonly byte? Id2;
    public readonly string Title;

    private CommandStation(byte? addressLow, 
                           byte? speed, 
                           byte? addressHigh, 
                           byte? sound, 
                           byte? id1, 
                           byte? id2,
                           string title)
    {
        AddressLow = addressLow;
        Speed = speed;
        AddressHigh = addressHigh;
        Sound = sound;
        Id1 = id1;
        Id2 = id2;
        Title = title;
    }

    /// <summary>
    /// Return the command station with the highest number of matching fields in slot 0.
    /// </summary>
    /// <param name="slot0">OPC_RD_SL_DATA as queried from command station to be detected</param>
    /// <returns>If there are more than one with the same count, take the first in the enumeration.
    /// If there are none, return null.</returns>
    public static CommandStation? Guess(SlRdData slot0)
    {
        int highestPercent = 0;
        CommandStation? bestMatch = null;

        foreach (var tuple in Evaluate(slot0))
        {
            if (tuple.percent > highestPercent)
            {
                highestPercent = tuple.percent;
                bestMatch = tuple.cs;
            }
        }

        return bestMatch;
    }

    /// <summary>
    /// Compare given slot 0 data against known command station models.
    /// </summary>
    /// <param name="slot0">OPC_RD_SL_DATA as queried from command station to be detected</param>
    /// <returns>Return enumeration of tuples of all known command stations and the determined
    /// percentage of fields in slot 0 that match the expectation for that model.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static IEnumerable<(CommandStation cs, int percent)> Evaluate(SlRdData slot0)
    {
        if (slot0.Slot.Value != 0)
            throw new InvalidOperationException("Detection must be based on slot 0");

        foreach (var cs in Enumerate())
        {
            int percent = cs.CountMatches(slot0) * 100 / 6;
            yield return (cs, percent);
        }
    }

    private int CountMatches(SlRdData slot0)
    {
        int countMatches = 0;
        if (slot0.Adr.Value == AddressLow)
            countMatches++;
        if (slot0.Spd.Value == Speed)
            countMatches++;
        if (slot0.Adr2.Value == AddressHigh)
            countMatches++;
        if (slot0.Snd.Value == Sound)
            countMatches++;
        if (slot0.Id1.Value == Id1)
            countMatches++;
        if (slot0.Id2.Value == Id2)
            countMatches++;
        return countMatches;
    }

    public override string ToString()
    {
        return Title;
    }
}
