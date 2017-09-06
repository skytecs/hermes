using Skytecs.Hermes.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging
{
    public static class LoggerExtensions
    {
        public static void Error(this ILogger logger, Exception e)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotNull(e, nameof(e));

            logger.LogError(new EventId(-1), e, e.Message);
        }

        public static void Info(this ILogger logger, string message)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotEmpty(message, nameof(message));

            logger.LogInformation(message);
        }

        public static void Warn(this ILogger logger, string message)
        {
            Check.NotNull(logger, nameof(logger));
            Check.NotEmpty(message, nameof(message));

            logger.LogWarning(message);
        }
    }
}
