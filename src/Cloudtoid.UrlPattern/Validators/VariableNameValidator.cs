namespace Cloudtoid.UrlPattern
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Validates that all variable names are unique
    /// </summary>
    internal sealed class VariableNameValidator : PatternValidatorBase
    {
        private readonly ISet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        protected internal override void VisitVariable(VariableNode node)
        {
            if (!names.Add(node.Name))
                throw new PatternException($"The variable name '{node.Name}' has already been used. Variable names should be unique.");

            ////if (VariableNames.SystemVariables.Contains(node.Name))
            ////    throw new PatternException($"The variable name '{node.Name}' collides with a system variable with the same name.");
        }
    }
}
