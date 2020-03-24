namespace Cloudtoid.Foid.Routes.Pattern
{
    /// <summary>
    /// Represents a forward slash in the pattern
    /// </summary>
    internal sealed class SegmentNode : LeafNode
    {
        private SegmentNode()
        {
        }

        internal static SegmentNode Instance { get; } = new SegmentNode();

        internal override void Accept(PatternNodeVisitor visitor)
            => visitor.VisitSegment(this);
    }
}
