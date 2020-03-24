namespace Cloudtoid.Foid.Routes.Pattern
{
    /// <summary>
    /// Validates that there are no consecutive <see cref="PatternConstants.SegmentStart"/> characters.
    /// </summary>
    internal sealed class NoConsecutiveSegmentStartValidator : PatternNodeVisitor
    {
        private bool allow = true;

        protected internal override void VisitLeaf(LeafNode node)
        {
            base.VisitLeaf(node);
            allow = true;
        }

        protected internal override void VisitSegmentStart(SegmentStartNode node)
        {
            if (!allow)
                throw new PatternException($"Found consecutive '{PatternConstants.SegmentStart}' which is invalid.");

            allow = false;
        }
    }
}
