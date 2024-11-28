using Shared.Models;

namespace Shared.LocoTable;

public interface ICommon2Row
{
    /// <summary>
    /// The loco address. 
    /// This stays constant. A loco row is created for a certain address and this row never changes the address.
    /// This implies thread safety without any need for locking.
    /// </summary>
    IAddress Address { get; }
}
