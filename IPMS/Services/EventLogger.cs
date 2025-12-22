using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPMS.Services
{
    namespace IPMS.Services
    {
        public interface IEventLogger
        {
            void LogInfo(string message);
            void LogWarning(string message);
            void LogError(string message, Exception ex);
        }

        public class EventLogger<T> : IEventLogger
        {
            private readonly ILogger<T> _logger;

            public EventLogger(ILogger<T> logger)
            {
                _logger = logger;
            }

            public void LogInfo(string message) => _logger.LogInformation(message);
            public void LogWarning(string message) => _logger.LogWarning(message);
            public void LogError(string message, Exception ex) => _logger.LogError(ex, message);
        }
    }
}
