using Loconet.Msg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Loconet;

/// <summary>
/// Class for storing a LocoNet message as byte array.
/// </summary>
public class LoconetMessage : ReceivableLoconetMessage
{
    public readonly byte[] Bytes;

    /// <summary>
    /// Derived messages may initialize for a custom message
    /// </summary>
    /// <param name="bytes"></param>
    public LoconetMessage(byte[] bytes)
    {
        if (bytes == null)
            throw new ArgumentNullException(nameof(bytes));
        
        if (bytes.Length < 2)
            throw new ArgumentException($"too small message of {bytes.Length} bytes", nameof(bytes));

        Bytes = bytes;
    }

    /// <summary>
    /// Set check byte at end of message to correct value
    /// This will be called by LoconetClient.BlockingSend() - user does not need to take care
    /// </summary>
    public void SetCheckByte()
    {
        Bytes[Bytes.Length - 1] = CalcCheck();
    }

    private byte CalcCheck()
    {
        byte work = 0xff;

        for (int i = 0; i < Bytes.Length-1; i++)
            work ^= Bytes[i];

        return work;
    }

    public override string ToString()
    {
        return Bytes.ToHex();
    }
}
