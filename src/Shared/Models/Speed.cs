using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models;

/// <summary>
/// Speed of a loco and conversion between wiThrottle and Loconet protocol
/// </summary>
public class Speed
{
    /// <summary>
    /// Stored as wiThrottle representation between 0 and 126, or -1 for eStop
    /// </summary>
    private int _speed = 0;

    public bool IsEmergencyStop => _speed == -1;
    public bool IsStop => _speed == 0;

    public void SetEmergencyStop() => _speed = -1;
    public void SetStop() => _speed = 0;

    public int WiThrottle
    {
        get => _speed;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, -1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 126);
            _speed = value;
        }
    }

    public int LocoNet
    {
        get
        {
            var temp = _speed; // Only one access. Considering this atomic, so we don't need synchronization
            return temp switch
            {
                -1 => 1,
                0 => 0,
                _ => _speed + 1
            };
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 127);
            _speed = value switch
            {
                0 => 0,
                1 => -1,
                _ => value - 1
            };
        }
    }
}
