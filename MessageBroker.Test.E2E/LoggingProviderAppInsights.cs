/*TODO: This file should not be part of this proj. It should be as a separe nuget pkg. It was added to allow the solution to work out of the box. Another option is to add public pkg*/
using LoggingProvider.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace LoggingProvider.AppInsights
{
    public class LoggingProviderAppInsights : ILogger
    {
        TelemetryClient _appInsightsClient;
        readonly string _loggingSource;
        public LoggingProviderAppInsights(string loggingSource)
        {
            TelemetryConfiguration.Active.InstrumentationKey = "";
            _appInsightsClient = new TelemetryClient();
            _loggingSource = loggingSource;
        }

        public void LogError(string msg)
        {
            _appInsightsClient.TrackTrace($"[{_loggingSource}] " + msg, Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Error);
        }

        public void LogInfo(string msg)
        {
            _appInsightsClient.TrackTrace($"[{_loggingSource}] " + msg, Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information);
        }

        public void LogWarning(string msg)
        {
            _appInsightsClient.TrackTrace($"[{_loggingSource}] " + msg, Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Warning);
        }

        ~LoggingProviderAppInsights()
        {
            _appInsightsClient.Flush();
        }
    }
}
