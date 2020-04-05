namespace Cloudtoid.GatewayCore.Headers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using static Contract;

    internal sealed class HeaderSanetizer
    {
        private readonly ILogger logger;

        internal HeaderSanetizer(ILogger logger)
        {
            this.logger = CheckValue(logger, nameof(logger));
        }

        internal bool IsValid(
            string name,
            IEnumerable<string> values,
            bool allowHeadersWithEmptyValue,
            bool allowHeadersWithUnderscoreInName)
        {
            // Remove empty headers
            if (!allowHeadersWithEmptyValue && values.All(string.IsNullOrEmpty))
            {
                logger.LogInformation("Removing header '{0}' as its value is empty.", name);
                return false;
            }

            // Remove headers with underscore in their names
            if (!allowHeadersWithUnderscoreInName && name.Contains('_'))
            {
                logger.LogInformation("Removing header '{0}' as headers should not have underscores in their name.", name);
                return false;
            }

            return true;
        }
    }
}
