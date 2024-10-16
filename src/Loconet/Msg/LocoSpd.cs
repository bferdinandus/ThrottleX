﻿using Loconet.Msg.Accessor;

namespace Loconet.Msg;

public class LocoSpd : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xA0;

    public static byte Length => 4;

    public readonly Field7Bit Slot;

    /// <summary>
    /// 0=stop;
    /// 1=estop;
    /// 2..127=speed steps
    /// </summary>
    public readonly BitField7Bit<EDirf> Spd;

    public LocoSpd()
    {
        Slot = new(1);
        Spd = new(2);
    }
}
