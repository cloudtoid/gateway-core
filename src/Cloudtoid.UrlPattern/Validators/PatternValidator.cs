namespace Cloudtoid.UrlPattern
{
    using System.Diagnostics.CodeAnalysis;

    internal sealed class PatternValidator : IPatternValidator
    {
        public bool Validate(
            PatternNode pattern,
            [NotNullWhen(false)] out string? error)
        {
            try
            {
                Validate<NoConsecutiveSegmentStartValidator>(pattern);
                Validate<NoConsecutiveWildcardValidator>(pattern);
                Validate<OneVariablePerSegmentValidator>(pattern);
                Validate<NoVariableFollowedByWildcardValidator>(pattern);
                Validate<VariableNameValidator>(pattern);
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
