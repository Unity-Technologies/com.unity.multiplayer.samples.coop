using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;
using UnityEngine;
using UnityEngine.SceneManagement;
using Logger = Serilog.Core.Logger;

namespace Unity.Multiplayer.Samples
{
    public static class DedicatedServerUtilities
    {
        public static bool IsServerBuildTarget
        {
            get
            {
#if UNITY_SERVER
                return true;
#else
                return false;
#endif
            }
        }

        // todo improve perf on this, don't do string concatenation everywhere, especially if log level is too high
        // TODO use json structure for log analysis tools (kibana, elasticsearch, etc)
        // todo find a way to disable full stack trace if needed, this could take a lot of resources.
        // Logging format should change following which logging analytics service you use. Elasticsearch could
        // require a different format than splunk for example.
        /*
         * https://serilog.net/
         * not usable for burst jobs
         * a few quick PRs to land before able to
         *
         * use serilog until we have iurii's logging, should be swappable.
         * don't know enough about when our logging will be pre-release to use it for now. do like ParrelSync and wait until we have officially supported solution.
         *
         * dogfood iurii's logging now to give early feedback before it goes in pre-release. current setup makes it easier for him to ask for burst changes (since it's "dots")
         *
         * structured
         * log rotation
         * bursted, other thread, faster, asynchronous.
         *
         * todo use You can try passing -timestamps on the command line to enable timestamps in the unity log. Failing that, you can set the UNITY_EXT_LOGGING environment variable.
    Yes, these are both undocumented.
         */
        public static void Log(string message)
        {
            // Debug.LogFormat($"<b>===[{DateTime.UtcNow}]|{Time.realtimeSinceStartup}|{Time.time}|pid[{Process.GetCurrentProcess().Id}]</b> - {message}");
            Log(message, null);
        }

        public class DGSEnricher : ILogEventEnricher
        {
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UTCTime", DateTime.UtcNow));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RealTimeSinceStartup", Time.realtimeSinceStartup));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Time", Time.time));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("pid", Process.GetCurrentProcess().Id));
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("currentScene", SceneManager.GetActiveScene().name));
            }
        }

        public static void Log(string messageTemplate, [CanBeNull] params object[] propertyValues)
        {
            logger.Information(messageTemplate, propertyValues);
        }

        static Logger logger;

        public static void InitLogging()
        {
            // download from github (below links). remove test folder. add asmdef and link to it, add OS_MUTEX to defines in asmdef (looks like it's needed when not using .net45 according to https://github.com/serilog/serilog-sinks-file/blob/d2117ab67d588a49e36a9a38d5a5ed3f16e23157/src/Serilog.Sinks.File/Serilog.Sinks.File.csproj
            // https://github.com/serilog/serilog/tree/v2.11.0
            // https://github.com/serilog/serilog-sinks-file/tree/v5.0.0
            // easier download from nuget https://www.nuget.org/packages/serilog/
            // rename to .zip
            // extract
            // copy lib/netstandard2.1 to plugins
            // same with file sink https://www.nuget.org/packages/Serilog.Sinks.File/
            // same with json formatter https://www.nuget.org/packages/Serilog.Formatting.Compact/
            // really need to check with legal about this

            // for perf reasons, buffering is enabled, instead of writing to disk each time
            // todo better flush interval configuration? better all in one go or many small ones?
            // todo convert other logs to using structured logging
            var fileName = "serilog-logging-test";
            var logPathPrefix = "DGSEditorLogging/" + fileName;
            var flushToDiskInterval = TimeSpan.FromSeconds(1);
            var rollingInterval = RollingInterval.Day;
            logger = new LoggerConfiguration()
                .Enrich.With(new DGSEnricher())
                .WriteTo.File(path: logPathPrefix + ".json",
                    flushToDiskInterval: flushToDiskInterval,
                    buffered: true,
                    formatter: new CompactJsonFormatter(),
                    rollingInterval: rollingInterval) // todo use command line arg from Multiplay to set this file, with proper path for editor as well
                .WriteTo.File(path: logPathPrefix + ".log",
                    flushToDiskInterval: flushToDiskInterval,
                    buffered: true,
                    outputTemplate: "UTC Time:{@UTCTime}, real time:{RealTimeSinceStartup}, UnityTime:{Time}, pid[{pid}] [{Level}] [{currentScene}] - {Message}{NewLine}{Exception}",
                    rollingInterval: rollingInterval) // todo use command line arg from Multiplay to set this file, with proper path for editor as well
                .CreateLogger();

            Log("Logging started");
            Log("Some logs with params isDGS?[{@IsDGS}]", IsServerBuildTarget);
        }

        // [MenuItem("Debug/GetAll")]
        public static void PrintSceneHierarchy()
        {
            List<GameObject> rootObjects = new List<GameObject>();
            Scene scene = SceneManager.GetActiveScene();
            scene.GetRootGameObjects(rootObjects);
            string toPrint = "\n";
            foreach (var rootObject in rootObjects)
            {
                toPrint += $"{GetInfoForObject(rootObject)}\n";
                PrintChildObjectsRecursive(rootObject, depth: 0, ref toPrint);
            }

            Log(toPrint);
        }

        private static void PrintChildObjectsRecursive(GameObject parentObject, int depth, ref string toPrint)
        {
            if (parentObject.transform.childCount == 0)
            {
                return;
            }

            string tabs = new string(' ', ++depth * 4);
            foreach (Transform child in parentObject.transform)
            {
                toPrint += $"{tabs}{GetInfoForObject(child.gameObject)}\n";
                PrintChildObjectsRecursive(child.gameObject, depth, ref toPrint); // asdf
            }
        }

        private static string GetInfoForObject(GameObject obj)
        {
            List<Component> allComponents = new();
            obj.GetComponents(allComponents);
            var nullCount = allComponents.FindAll(component => component == null).Count;
            return $"{obj.name}\tnb null {nullCount}/{allComponents.Count}";
        }

        public static void ApplicationQuit()
        {
            Serilog.Log.CloseAndFlush();
        }
    }
}