using System.Collections.Concurrent;
using Shared.LocoTable;
using Shared.Models;

namespace ThrottleX.Core.LocoTable;

public class LocoTable : ILoconet2Table, IThrottle2Table
{
    private readonly ConcurrentDictionary<IAddress, LocoRow> _rowByAddress = new();
    private readonly ILogger _logger;

    public LocoTable(ILogger<LocoTable> logger) => _logger = logger;

    /// <summary>
    /// Get row by its add-sequence-index.
    /// </summary>
    /// <param name="index">[0..Count-1]</param>
    /// <returns></returns>
    public ILoconet2Row this[int index]
    {
        get
        {
            return _rowByAddress.Values.First(row => row.SortOrder == index);
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
        return _rowByAddress.GetOrAdd(address, _ =>
        {
            return new LocoRow(address, _logger, Count);
        });
    }

    public int Count => _rowByAddress.Count;
}
