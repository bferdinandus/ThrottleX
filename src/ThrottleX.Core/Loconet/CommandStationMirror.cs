using Loconet;
using Loconet.Msg;
using Microsoft.Extensions.Logging;

namespace ThrottleX.Core.Loconet;

/// <summary>
/// Locally known parts of the slot table of one command station
/// </summary>
public class CommandStationMirror
{
    private readonly Dictionary<(byte low, byte high), SlotData> _byAddress = new();
    private readonly SlotData?[] _bySlot = new SlotData[127];
    private readonly ILogger _logger;

    public CommandStationMirror(ILogger logger)
    {
        _logger = logger;
    }

    private SlotData Add(SlotDataBase init)
    {
        var s = new SlotData(init);
        _byAddress[s.Address] = s;
        _bySlot[s.SlotNumber] = s;
        return s;
    }

    public bool Remove(int slotNumber)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slotNumber, _bySlot.Length - 1, nameof(slotNumber));
        var s = _bySlot[slotNumber];
        if (s == null)
            return false;

        _bySlot[slotNumber] = null;
        return _byAddress.Remove(s.Address);
    }

    /// <summary>
    /// Called for every received message.
    /// If this is from the net, we copy the data to our slot table mirror.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="message"></param>
    public void OnMessage(LoconetClient.ReceiveOrigin origin, ReceivableLoconetMessage message)
    {
        if (origin == LoconetClient.ReceiveOrigin.Sent)
            return; // ignoring our own messages, because we setting fields directly while issuing send request

        switch (message)
        {
            case SlotDataBase msg: // OPC_WR_SL_DATA, OPC_SL_RD_DATA
                OnSlotMessage(msg);
                break;

            case LocoBase msg:     // OPC_LOCO_DIRF, OPC_LOCO_SPD, OPC_LOCO_SND, OPC_SLOT_STAT1
                OnLocoMessage(msg);
                break;
        }
    }

    private void OnSlotMessage(SlotDataBase msg)
    {
        var data = _bySlot[msg.Slot.Value];
        if (data == null)
        {
            data = Add(msg);
            _logger.LogInformation("Starting to track slot {slot}", data.SlotNumber);
        }
        else
        {
            data.SetBySlotMsg(msg, _logger);
        }
    }

    private void OnLocoMessage(LocoBase locoMsg)
    {
        var data = _bySlot[locoMsg.Slot.Value];
        if (data == null)
            return;
        var name = locoMsg.GetType().Name;
        var slotNumber = data.SlotNumber;

        void Change(byte newValue, ref byte slotValue)
        {
            _logger.LogInformation("Received loconet message {Name} for slot {SlotNumber}, changing from 0x{SlotValue:X02} to 0x{NewValue:X02}", name, slotNumber, slotValue, newValue);
            slotValue = newValue;
        }

        switch (locoMsg)
        {
            case LocoDirf locoDirf: Change(locoDirf.Dirf.Value, ref data.Dirf); break;
            case LocoSnd locoSnd: Change(locoSnd.Snd.Value, ref data.Sound); break;
            case LocoSpd locoSpd: Change(locoSpd.Spd.Value, ref data.Speed); break;
            case SlotStat1 slotStat1: Change(slotStat1.Stat1.Value, ref data.Status); break;
        }
    }

    public SlotData? this[(byte high, byte low) address] => _byAddress.TryGetValue(address, out var r) ? r : null;
}
