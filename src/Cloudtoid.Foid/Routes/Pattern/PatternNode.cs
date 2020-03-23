namespace Cloudtoid.Foid.Routes.Pattern
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using static Contract;

    internal abstract class PatternNode
    {
        [return: NotNullIfNotNull("left")]
        [return: NotNullIfNotNull("right")]
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

            var nodes = Enumerable.Empty<PatternNode>();

            nodes = left is SequenceNode leftSeq
                ? nodes.Concat(leftSeq.Nodes)
                : nodes.Concat(left);

            nodes = right is SequenceNode rightSeq
                ? nodes.Concat(rightSeq.Nodes)
                : nodes.Concat(right);

            return new SequenceNode(nodes);
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
    internal sealed class SegmentNode : LeafNode
    {
        private SegmentNode()
        {
        }

        internal static SegmentNode Instance { get; } = new SegmentNode();

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

    internal sealed class SequenceNode : PatternNode
    {
        internal SequenceNode(IEnumerable<PatternNode> nodes)
        {
            Nodes = CheckNonEmpty(
                CheckValue(nodes, nameof(nodes)).AsReadOnlyList(),
                nameof(nodes));
        }

        internal SequenceNode(params PatternNode[] nodes)
        {
            Nodes = CheckNonEmpty(nodes, nameof(nodes));
        }

        public IReadOnlyList<PatternNode> Nodes { get; }

        internal override void Accept(PatternNodeVisitor visitor)
            => visitor.VisitSequence(this);
    }
}
