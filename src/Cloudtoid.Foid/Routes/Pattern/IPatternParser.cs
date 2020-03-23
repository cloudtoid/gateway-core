namespace Cloudtoid.Foid.Routes.Pattern
{
    using System.Diagnostics.CodeAnalysis;

    internal interface IPatternParser
    {
        bool TryParse(
           string route,
           out PatternNode? pattern,
           [MaybeNullWhen(true)] out string errors);
    }
}
