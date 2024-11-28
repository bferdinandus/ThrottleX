using Shared.Models;

namespace Shared.LocoTable;

/// <summary>
/// Interface for throttle code calling into loco table
/// </summary>
public interface IThrottle2Table
{
    /// <summary>
    /// Return row for the given address if alreday stored in table
    /// or add a new row and return that.
    /// </summary>
    /// <param name="address">throttle identifies a loco over its DCC address</param>
    /// <returns>A row for access by the throttle code</returns>
    IThrottle2Row GetRowForAddress(IAddress address);

    /// <summary>
    /// Number of entries in the table.
    /// </summary>
    int Count { get; }
}
