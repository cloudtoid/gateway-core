using System.Collections.Generic;
using System.Linq;

namespace Cloudtoid.GatewayCore.Settings
{
    public sealed class HeaderSettings
    {
        internal HeaderSettings(string[] valueExpressions)
        {
            ValueExpressions = valueExpressions;
        }

        public string[] ValueExpressions { get; }

        public IEnumerable<string> EvaluateValues(ProxyContext context)
            => ValueExpressions.Select(v => context.Evaluate(v));
    }
}