namespace Cloudtoid.UrlPattern
{
    /// <summary>
    /// Validates that there are no consecutive <see cref="Constants.SegmentStart"/> characters.
    /// </summary>
    internal sealed class NoConsecutiveSegmentStartValidator : PatternValidatorBase
    {
        private bool fail;

        protected internal override void VisitLeaf(LeafNode node)
        {
            base.VisitLeaf(node);
            fail = false;
        }

        protected internal override void VisitSegmentStart(SegmentStartNode node)
        {
            if (fail)
                throw new PatternException($"Found consecutive '{Constants.SegmentStart}' which is invalid.");

            fail = true;
        }
    }
}
