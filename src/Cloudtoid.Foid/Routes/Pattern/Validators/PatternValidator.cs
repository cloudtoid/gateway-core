namespace Cloudtoid.Foid.Routes.Pattern
{
    using System.Diagnostics.CodeAnalysis;

    internal sealed class PatternValidator : IPatternValidator
    {
        public bool Validate(
            PatternNode pattern,
            [MaybeNullWhen(false)] out string? error)
        {
            try
            {
                new SegmentStartValidator().Visit(pattern);
                new WildcardValidator().Visit(pattern);
            }
            catch (PatternException pe)
            {
                error = pe.Message;
                return false;
            }

            error = null;
            return true;
        }
    }
}
