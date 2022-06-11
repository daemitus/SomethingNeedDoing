using System.Collections.Generic;

namespace SomethingNeedDoing.Misc;

/// <summary>
/// Extension methods.
/// </summary>
internal static class Extensions
{
    /// <inheritdoc cref="string.Join(char, string?[])"/>
    public static string Join(this IEnumerable<string> values, char separator)
        => string.Join(separator, values);

    /// <inheritdoc cref="string.Join(string, string?[])"/>
    public static string Join(this IEnumerable<string> values, string separator)
        => string.Join(separator, values);
}
