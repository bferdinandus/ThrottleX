using System.ComponentModel;

namespace Shared;

public static class Helpers
{
    public static Exception InvalidEnum<TEnum>(this TEnum en, string? variableName=null)
        where TEnum : Enum
    {
        return new InvalidEnumArgumentException(variableName ??= nameof(en), Convert.ToInt32(en), typeof(TEnum));
    }
}
