using Loconet.Msg.Accessor;
using static System.Net.Mime.MediaTypeNames;

namespace Loconet.Msg;

/// <summary>
/// OPC_MOVE_SLOTS
/// 
/// If SRC is not IN_USE, clr SRC
/// SPECIAL CASES:
/// If SRC = 0(DISPATCH GET), DEST = dont care, Return SLOT READ DATA of DISPATCH Slot
/// IF SRC =DEST(NULL move) then SRC = DEST is set to IN_USE , if legal move
/// If DEST = 0, is DISPATCH Put, mark SLOT as DISPATCH
/// RETURN slot status<0xE7> of DESTINATION slot DEST if move legal
/// RETURN Fail LACK code if illegal move <B4>,<3A>,<0>,<chk>
/// illegal to move to/from slots 120/127
/// </summary>
public class MoveSlots : FormatBase, ILoconetMessageFormat
{
    public static byte Opcode => 0xBA;

    public static byte Length => 4;

    /// <summary>
    /// Slot _from_ where we are moving
    /// </summary>
    public readonly Field7Bit Src = new(1);

    /// <summary>
    /// Slot _to_ where we are moving
    /// </summary>
    public readonly Field7Bit Dest = new(2);
}
