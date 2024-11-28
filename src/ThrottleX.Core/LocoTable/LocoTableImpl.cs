using Microsoft.Extensions.Logging;
using Shared.LocoTable;
using Shared.Models;
using System.Collections;

namespace ThrottleX.Core.LocoTable;

public class LocoTableImpl : ILoconet2Table, IThrottle2Table
{
    public LocoTableImpl(ILogger<LocoTableImpl> logger)
    {
        _logger = logger;
    }

    private Dictionary<IAddress, LocoRowImpl> _rowByAddress = new();
    private List<LocoRowImpl> _growingTable = new();
    private ILogger _logger;

    /// <summary>
    /// Get row by its add-sequence-index.
    /// </summary>
    /// <param name="index">[0..Count-1]</param>
    /// <returns></returns>
    public ILoconet2Row this[int index]
    {
        get
        {
            lock (this)
            {
                return _growingTable[index];
            }
        }
    }

    /// <summary>
    /// Return row for the given address if alreday stored in table
    /// or add a new row and return that.
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public IThrottle2Row GetRowForAddress(IAddress address)
    {
        lock (this)
        {
            if (_rowByAddress.TryGetValue(address, out var row))
                return row!;

            var created = new LocoRowImpl(address, _logger);
            _rowByAddress[address] = created;
            _growingTable.Add(created);
            return created;
        }
    }

    public int Count => _growingTable.Count;
}
