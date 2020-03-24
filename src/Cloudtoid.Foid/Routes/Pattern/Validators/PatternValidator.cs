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
                Validate<NoConsecutiveSegmentStartValidator>(pattern);
                Validate<NoConsecutiveWildcardValidator>(pattern);
                Validate<OneVariablePerSegmentValidator>(pattern);
                Validate<NoVariableFollowedByWildcardValidator>(pattern);
            }
            catch (PatternException pe)
            {
                error = pe.Message;
                return false;
            }

            error = null;
            return true;
        }

        private static void Validate<TValidator>(PatternNode pattern)
            where TValidator : PatternValidatorBase, new()
        {
            new TValidator().Validate(pattern);
        }
    }
}
