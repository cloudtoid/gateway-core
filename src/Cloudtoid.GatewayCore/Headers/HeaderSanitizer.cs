using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using static Cloudtoid.Contract;

namespace Cloudtoid.GatewayCore.Headers
{
    internal sealed class HeaderSanitizer
    {
        private static readonly Func<string, bool> NotNullOrEmpty = s => !string.IsNullOrEmpty(s);
        private readonly ILogger logger;

        internal HeaderSanitizer(ILogger logger)
        {
            this.logger = CheckValue(logger, nameof(logger));
        }

        internal bool IsValid(
            string name,
            IEnumerable<string> values,
            bool discardEmpty,
            bool discardUnderscore)
        {
            // Remove empty headers
            if (discardEmpty && !values.Any(NotNullOrEmpty))
            {
                logger.LogInformation("Removing header '{0}' as its value is empty.", name);
                return false;
            }

            // Remove headers with underscore in their names
            if (discardUnderscore && name.Contains('_', StringComparison.Ordinal))
            {
                logger.LogInformation("Removing header '{0}' as headers should not have underscores in their name.", name);
                return false;
            }

            return true;
        }
    }
}
