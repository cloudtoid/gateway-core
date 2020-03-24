namespace Cloudtoid.Foid.Routes.Pattern
{
    /// <summary>
    /// Represents '*' in the pattern. '*' is the wild-card.
    /// </summary>
    internal sealed class WildcardNode : LeafNode
    {
        private WildcardNode()
        {
        }

        internal static WildcardNode Instance { get; } = new WildcardNode();

        internal override void Accept(PatternNodeVisitor visitor)
            => visitor.VisitWildcard(this);
    }
}
