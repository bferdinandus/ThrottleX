using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.LocoTable;

/// <summary>
/// State of a row in the loco table
/// </summary>
public enum ELocoRowState
{
    /// <summary>
    /// Initial state: wiFRED switched on, we need to poll the state from the command station(s)
    /// </summary>
    Requesting = 1,
    /// <summary>
    /// We fetched data from at least one command stations, now we can process speed updates
    /// </summary>
    Operational = 2,
    /// <summary>
    ///  wiFRED switched off or any other error
    /// </summary>
    Inactive = 3
}
