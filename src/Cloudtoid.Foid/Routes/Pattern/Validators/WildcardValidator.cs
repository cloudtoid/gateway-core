namespace Cloudtoid.Foid.Routes.Pattern
{
    /// <summary>
    /// Ensures that there are no two more consecutive <see cref="PatternConstants.Wildcard"/> characters.
    /// </summary>
    internal sealed class WildcardValidator : PatternNodeVisitor
    {
        private bool allow = true;

        protected internal override void VisitLeaf(LeafNode node)
        {
            if (!(node is WildcardNode))
                allow = true;

            if (!allow)
                throw new PatternException($"Found two consecutive '{PatternConstants.Wildcard}' which is invalid.");

            allow = false;
        }
    }
}
