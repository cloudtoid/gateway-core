namespace Cloudtoid.Foid.Routes.Pattern
{
    /// <summary>
    /// Ensures that there are no two more consecutive <see cref="PatternConstants.SegmentStart"/> characters.
    /// </summary>
    internal sealed class SegmentStartValidator : PatternNodeVisitor
    {
        private bool allow = true;

        protected internal override void VisitLeaf(LeafNode node)
        {
            if (!(node is SegmentNode))
                allow = true;

            if (!allow)
                throw new PatternException($"Found two consecutive '{PatternConstants.SegmentStart}' which is invalid.");

            allow = false;
        }
    }
}
