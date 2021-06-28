using System;

namespace LoggingAuthApi.Model.Interface.Logging
{
    public interface ILogger
    {
        LogLevel? MinimumLevelToLog { get; set; }
        void Log(LogLevel level, string message);
        void Log(LogLevel level, string message, Exception ex);
    }
}
