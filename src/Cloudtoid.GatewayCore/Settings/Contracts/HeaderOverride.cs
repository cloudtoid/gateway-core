using System.Collections.Generic;
using System.Linq;

namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class HeaderOverride
    {
        internal HeaderOverride(
            string name,
            string[] valueExpressions)
        {
            Name = name;
            ValueExpressions = valueExpressions;
        }

        public string Name { get; }

        public string[] ValueExpressions { get; }

        public IEnumerable<string> EvaluateValues(ProxyContext context)
            => ValueExpressions.Select(v => context.Evaluate(v));
    }
}