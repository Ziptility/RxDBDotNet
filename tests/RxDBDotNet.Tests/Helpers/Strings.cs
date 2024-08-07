namespace RxDBDotNet.Tests.Helpers;

public static class Strings
{
    private static readonly Random Random = new();

    public static string CreateString(int? length = null)
    {
        length ??= 10;

        const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";

        var chars = new char[length.Value];

        for (var i = 0; i < length; i++)
        {
#pragma warning disable SCS0005, CA5394 // Weak random number generator. // only used for unit tests
            chars[i] = allowedChars[Random.Next(
                0,
                allowedChars.Length)];
#pragma warning restore SCS0005, CA5394 // Weak random number generator.
        }

        return new string(chars);
    }
}
