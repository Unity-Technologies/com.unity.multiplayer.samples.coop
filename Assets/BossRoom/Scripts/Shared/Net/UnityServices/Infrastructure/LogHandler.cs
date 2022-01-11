using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BossRoom.Scripts.Shared.Net.UnityServices.Infrastructure
{
    public enum LogMode
    {
        Critical, // Errors only
        Warnings, // Errors and Warnings
        Verbose // Everything
    }

    /// <summary>
    /// Overrides the default Unity logging with our own, so that verbose logs (both from the services and from any of our Debug.Log* calls) don't clutter the Console.
    /// </summary>
    public class LogHandler : ILogHandler
    {
        public LogMode mode = LogMode.Critical;

        static LogHandler s_instance;
        ILogHandler m_DefaultLogHandler = Debug.unityLogger.logHandler; // Store the default logger that prints to console.

        public static LogHandler Get()
        {
            if (s_instance != null) return s_instance;
            s_instance = new LogHandler();
            Debug.unityLogger.logHandler = s_instance;
            return s_instance;
        }

        public void LogFormat(LogType logType, Object context, string format, params object[] args)
        {
            if (logType == LogType.Exception) // Exceptions are captured by LogException and should always be logged.
                return;

            if (logType == LogType.Error || logType == LogType.Assert)
            {
                m_DefaultLogHandler.LogFormat(logType, context, format, args);
                return;
            }

            if (mode == LogMode.Critical)
                return;

            if (logType == LogType.Warning)
            {
                m_DefaultLogHandler.LogFormat(logType, context, format, args);
                return;
            }

            if (mode != LogMode.Verbose)
                return;

            m_DefaultLogHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, Object context)
        {
            m_DefaultLogHandler.LogException(exception, context);
        }
    }
}
