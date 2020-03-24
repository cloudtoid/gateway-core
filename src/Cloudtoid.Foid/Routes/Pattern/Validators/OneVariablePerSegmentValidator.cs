namespace Cloudtoid.Foid.Routes.Pattern
{
    /// <summary>
    /// Validates that each URL segment only includes a maximum of one variable
    /// </summary>
    internal sealed class OneVariablePerSegmentValidator : PatternNodeVisitor
    {
        private bool allow = true;

        protected internal override void VisitVariable(VariableNode node)
        {
            if (!allow)
                throw new PatternException($"Each URL segment can only include a single variable definition.");

            allow = false;
        }

        protected internal override void VisitSegmentStart(SegmentStartNode node)
        {
            base.VisitSegmentStart(node);
            allow = true;
        }
    }
}
