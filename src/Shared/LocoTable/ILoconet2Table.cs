namespace Shared.LocoTable;

public interface ILoconet2Table
{
    /// <summary>
    /// Get a table row by its index.
    /// </summary>
    /// <param name="index">[0..Count-1]</param>
    /// <returns>Reference to loco table row identified by its index.</returns>
    ILoconet2Row this[int index] { get; }

    /// <summary>
    /// Number of entries in the table.
    /// </summary>
    int Count { get; }
}
