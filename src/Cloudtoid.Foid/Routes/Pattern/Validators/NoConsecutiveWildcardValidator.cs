namespace Cloudtoid.Foid.Routes.Pattern
{
    /// <summary>
    /// Validates that there are no consecutive <see cref="PatternConstants.Wildcard"/> characters.
    /// </summary>
    internal sealed class NoConsecutiveWildcardValidator : PatternNodeVisitor
    {
        private bool allow = true;

        protected internal override void VisitLeaf(LeafNode node)
        {
            base.VisitLeaf(node);
            allow = true;
        }

        protected internal override void VisitWildcard(WildcardNode node)
        {
            if (!allow)
                throw new PatternException($"Found consecutive '{PatternConstants.Wildcard}' which is invalid.");

            allow = false;
        }
    }
}
