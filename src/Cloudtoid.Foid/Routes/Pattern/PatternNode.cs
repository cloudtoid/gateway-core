namespace Cloudtoid.Foid.Routes.Pattern
{
    using static Contract;

    internal abstract class PatternNode
    {
        public static PatternNode? operator +(PatternNode? left, PatternNode? right)
        {
            if (left is null)
                return right;

            if (right is null)
                return null;

            if (Equals(left, MatchNode.Empty))
                return right;

            if (Equals(right, MatchNode.Empty))
                return left;

            return new ConcatNode(left, right);
        }

        internal abstract void Accept(PatternNodeVisitor visitor);
    }

    internal abstract class LeafNode : PatternNode
    {
    }

    internal sealed class MatchNode : LeafNode
    {
        private MatchNode()
        {
            Value = string.Empty;
        }

        internal MatchNode(string value)
        {
            Value = CheckNonEmpty(value, nameof(value));
        }

        public string Value { get; }

        internal static MatchNode Empty { get; } = new MatchNode();

        internal override void Accept(PatternNodeVisitor visitor)
            => visitor.VisitMatch(this);
    }

    internal sealed class VariableNode : LeafNode
    {
        internal VariableNode(string name)
        {
            Name = CheckValue(name, nameof(name));
        }

        public string Name { get; }

        internal override void Accept(PatternNodeVisitor visitor)
            => visitor.VisitVariable(this);
    }

    /// <summary>
    /// Represents a forward slash in the pattern
    /// </summary>
    internal sealed class SegmentlNode : LeafNode
    {
        private SegmentlNode()
        {
        }

        internal static SegmentlNode Instance { get; } = new SegmentlNode();

        internal override void Accept(PatternNodeVisitor visitor)
            => visitor.VisitSegment(this);
    }

    /// <summary>
    /// Represents '*' in the pattern
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

    internal sealed class OptionalNode : PatternNode
    {
        internal OptionalNode(PatternNode node)
        {
            Node = CheckValue(node, nameof(node));
        }

        public PatternNode Node { get; }

        internal override void Accept(PatternNodeVisitor visitor)
            => visitor.VisitOptional(this);
    }

    internal sealed class ConcatNode : PatternNode
    {
        internal ConcatNode(PatternNode left, PatternNode right)
        {
            Left = CheckValue(left, nameof(left));
            Right = CheckValue(right, nameof(right));
        }

        public PatternNode Left { get; }

        public PatternNode Right { get; }

        internal override void Accept(PatternNodeVisitor visitor)
            => visitor.VisitConcat(this);
    }
}
