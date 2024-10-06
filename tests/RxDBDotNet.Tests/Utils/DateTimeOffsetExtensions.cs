// tests\RxDBDotNet.Tests\Utils\DateTimeOffsetExtensions.cs

using System;

namespace RxDBDotNet.Tests.Utils;

public static class DateTimeOffsetExtensions
{
    /// <summary>
    ///     Strips microseconds from the DateTimeOffset value, leaving only up to milliseconds.
    ///     This helps with comparing DateTimeOffset values in unit tests.
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset value to strip microseconds from.</param>
    /// <returns>A new DateTimeOffset value with microseconds stripped.</returns>
    public static DateTimeOffset StripMicroseconds(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.AddTicks(-(dateTimeOffset.Ticks % TimeSpan.TicksPerMillisecond));
    }
}
