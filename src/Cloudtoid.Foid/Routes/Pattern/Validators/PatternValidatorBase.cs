namespace Cloudtoid.Foid.Routes.Pattern
{
    internal abstract class PatternValidatorBase : PatternNodeVisitor
    {
        internal void Validate(PatternNode pattern) => Visit(pattern);
    }
}
