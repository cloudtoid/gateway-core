namespace Cloudtoid.UrlPattern
{
    using System.Diagnostics.CodeAnalysis;

    internal interface IPatternValidator
    {
        bool Validate(
            PatternNode pattern,
            [NotNullWhen(false)] out string? error);
    }
}
