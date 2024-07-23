using System.Globalization;
using System.Text.RegularExpressions;

namespace RxDBDotNet.Tests.Setup;

internal static class NamingHelper
{
    private static readonly Regex RegexInvalidCharacters = new("[^_a-zA-Z0-9]");
    private static readonly Regex RegexNextWhiteSpace = new(@"(?<=\s)");
    private static readonly Regex RegexWhiteSpace = new(@"\s");
    private static readonly Regex RegexUpperCaseFirstLetter = new("^[a-z]");

    private static readonly Regex RegexFirstCharFollowedByUpperCasesOnly =
        new("(?<=[A-Z])[A-Z0-9]+$");

    private static readonly Regex RegexLowerCaseNextToNumber = new("(?<=[0-9])[a-z]");

    private static readonly Regex RegexUpperCaseInside =
        new("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");
    internal static readonly char[] Separator = ['_'];

    public static string ToPascalCase(string text)
    {
        // See https://stackoverflow.com/questions/18627112/how-can-i-convert-text-to-pascal-case
        var textWithoutWhiteSpace = RegexInvalidCharacters.Replace(
            RegexWhiteSpace.Replace(
                text,
                string.Empty),
            string.Empty);
        if (textWithoutWhiteSpace.All(c => c == '_'))
        {
            return textWithoutWhiteSpace;
        }

        var pascalCase = RegexInvalidCharacters
            // Replaces white spaces with underscore, then replace all invalid chars with an empty string.
            .Replace(
                RegexNextWhiteSpace.Replace(
                    text,
                    "_"),
                string.Empty)
            .Split(
                Separator,
                StringSplitOptions.RemoveEmptyEntries)
            .Select(
                w => RegexUpperCaseFirstLetter.Replace(
                    w,
                    m => m.Value.ToUpper(CultureInfo.InvariantCulture)))
            // Replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc).
            .Select(
                w => RegexFirstCharFollowedByUpperCasesOnly.Replace(
                    w,
                    m => m.Value.ToLower(CultureInfo.InvariantCulture)))
            // Set upper case the first lower case following a number (Ab9cd -> Ab9Cd).
            .Select(
                w => RegexLowerCaseNextToNumber.Replace(
                    w,
                    m => m.Value.ToUpper(CultureInfo.InvariantCulture)))
            // Lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef).
            .Select(
                w => RegexUpperCaseInside.Replace(
                    w,
                    m => m.Value.ToLower(CultureInfo.InvariantCulture)));

        return string.Concat(pascalCase);
    }
}
