using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loconet.Msg.Accessor;

public enum ESlotStatus1 : byte
{
    /// <summary>
    /// D6 | SL_CONUP
    /// CONDN/CONUP: bit encoding-Control double linked Consist List
    /// 11=LOGICAL MID CONSIST, Linked up AND down
    /// 10=LOGICAL CONSIST TOP, Only linked downwards
    /// 01=LOGICAL CONSIST SUB-MEMBER, Only linked upwards
    /// 00=FREE locomotive, no CONSIST indirection/linking
    /// ALLOWS "CONSISTS of CONSISTS". Uplinked means that Slot SPD number is now SLOT adr of SPD/DIR and STATUS of consist. i.e. is an Indirect pointer. This Slot has same BUSY/ACTIVE bits as TOP of Consist. TOP is loco with SPD/DIR for whole consist. (top of list). 
    /// </summary>
    ConUp = 0x40,

    /// <summary>
    /// D5 | SL_BUSY
    /// BUSY/ACTIVE: bit encoding for SLOT activity
    /// 11=IN_USE loco adr in SLOT  	-REFRESHED
    /// 10=IDLE   loco adr in SLOT  	-NOT refreshed
    /// 01=COMMON loco adr IN SLOT     -refreshed
    /// 00=FREE SLOT, no valid DATA      -not refreshed
    /// </summary>
    Busy = 0x20,
    /// <summary>
    /// D4 | SL_ACTIVE
    /// </summary>
    Active = 0x10,

    /// <summary>
    /// D3 | SL_CONDN | shows other SLOT Consist linked INTO this slot, see SL_CONUP
    /// </summary>
    ConDn = 0x08,

    /// <summary>
    /// D2 | SL_SPDEX | 3 BITS for Decoder TYPE encoding for this SLOT
    /// 011=send 128 speed mode packets
    /// 010=14 step MODE
    /// 001=28 step. Generate Trinary packets for this Mobile ADR
    /// 000=28 step/ 3 BYTE PKT regular mode
    /// 111=128 Step decoder, Allow Advanced DCC consisting
    /// 100=28 Step decoder, Allow Advanced DCC consisting
    /// </summary>
    SpdEx = 0x04,
    /// <summary>
    /// D1 | SL_SPD14
    /// </summary>
    Spd14 = 0x02,
    /// <summary>
    /// D0 | SL_SPD28
    /// </summary>
    Spd28 = 0x01,
}
