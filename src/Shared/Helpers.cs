using Loconet.Msg.Accessor;
using Shared.Models;
using System.ComponentModel;

namespace Shared;

public static class Helpers
{
    /// <summary>
    /// This is intended for a slim implementation of a switch-default-case of an enumerator `en` like
    /// `throw en.InvalidValue()`.
    /// </summary>
    /// <typeparam name="TEnum"></typeparam>
    /// <param name="en"></param>
    /// <param name="variableName"></param>
    /// <returns></returns>
    public static Exception InvalidEnum<TEnum>(this TEnum en, string? variableName=null)
        where TEnum : Enum
    {
        return new InvalidEnumArgumentException(variableName ??= nameof(en), Convert.ToInt32(en), typeof(TEnum));
    }

    public static Direction ToDirection(this BitField7Bit<EDirf> dirf)
    {
        if (dirf[EDirf.Dir])
            return Direction.Reverse;
        else
            return Direction.Forward;
    }

    public static void SetDirection(this BitField7Bit<EDirf> dirf, Direction direction)
    {
        dirf[EDirf.Dir] = direction switch
        {
            Direction.Forward => true,
            Direction.Reverse => false,
            _ => throw direction.InvalidEnum()
        };
    }
}
