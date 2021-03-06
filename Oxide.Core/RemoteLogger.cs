﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;

using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Core
{
    public static class RemoteLogger
    {
        private static int projectId = 3;
        private static string host = "logg.re";
        private static string publicKey = "5bd22fdca1ad47eeb8bf81b82f1d05f8";
        private static string secretKey = "90925e2f297944db853a6c872d2b6c60";
        private static string url = "https://" + host + "/api/" + projectId + "/store/";

        private static string[][] sentryAuth =
        {
            new string[] { "sentry_version", "5" },
            new string[] { "sentry_client", "MiniRaven/1.0" },
            new string[] { "sentry_key", publicKey },
            new string[] { "sentry_secret", secretKey }
        };

        private static Dictionary<string, string> BuildHeaders()
        {
            var auth_string = string.Join(", ", sentryAuth.Select(x => string.Join("=", x)).ToArray());
            auth_string += ", sentry_timestamp=" + (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            return new Dictionary<string, string> { { "X-Sentry-Auth", "Sentry " + auth_string } };
        }

        private static Dictionary<string, string> tags = new Dictionary<string, string>
        {
            { "arch", IntPtr.Size == 4 ? "x86" : "x64" },
            { "game", Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName) }
        };

        private class QueuedReport
        {
            public Dictionary<string, string> Headers;
            public string Body;

            public QueuedReport(Report report)
            {
                Headers = BuildHeaders();
                Body = JsonConvert.SerializeObject(report);
            }
        }

        public class Report
        {
            public string event_id;
            public string message;
            public string level;
            public string platform = "csharp";
            public string culprit;
            public string release = OxideMod.Version.ToString();
            public Dictionary<string, string> tags = RemoteLogger.tags;
            public Dictionary<string, string> modules;
            public Dictionary<string, string> extra;

            private Dictionary<string, string> headers;

            public Report(string level, string culprit, string message, string exception=null)
            {
                this.headers = BuildHeaders();
                this.level = level;
                this.message = message.Length > 1000 ? message.Substring(0, 1000) : message;
                this.event_id = this.message.GetHashCode().ToString();
                this.culprit = culprit;
                this.modules = new Dictionary<string, string>();
                foreach (var extension in Interface.Oxide.GetAllExtensions())
                    modules[extension.GetType().Assembly.GetName().Name] = extension.Version.ToString();
                if (exception != null)
                {
                    extra = new Dictionary<string, string>();
                    var exception_lines = exception.Split('\n').Take(31).ToArray();
                    for (var i = 0; i < exception_lines.Length; i++)
                    {
                        var line = exception_lines[i].Trim(' ', '\r', '\n').Replace('\t', ' ');
                        if (line.Length > 0) extra["line_" + i.ToString("00")] = line;
                    }
                }
            }

            public void DetectModules(Assembly assembly)
            {
                var assembly_name = assembly.GetName().Name;
                var extension_type = assembly.GetTypes().FirstOrDefault(t => t.BaseType == typeof(Extension));
                if (extension_type == null)
                {
                    var plugin_type = assembly.GetTypes().FirstOrDefault(t => IsTypeDerivedFrom(t, typeof(Plugin)));
                    if (plugin_type != null)
                    {
                        var plugin = Interface.Oxide.RootPluginManager.GetPlugin(plugin_type.Name);
                        if (plugin != null) modules["Plugins." + plugin.Name] = plugin.Version.ToString();
                    }
                }
            }

            public void DetectModules(string[] stack_trace)
            {
                foreach (var line in stack_trace)
                {
                    if (!line.StartsWith("Oxide.Plugins.") || !line.Contains("+")) continue;
                    var plugin_name = line.Split('+')[0];
                    var plugin = Interface.Oxide.RootPluginManager.GetPlugin(plugin_name);
                    if (plugin != null) modules["Plugins." + plugin.Name] = plugin.Version.ToString();
                    break;
                }
            }

            private bool IsTypeDerivedFrom(Type type, Type base_type)
            {
                while (type != null && type != base_type)
                    if ((type = type.BaseType) == base_type) return true;
                return false;
            }
        }

        private static Timer timers = Interface.Oxide.GetLibrary<Timer>("Timer");
        private static WebRequests webrequests = Interface.Oxide.GetLibrary<WebRequests>("WebRequests");
        private static List<QueuedReport> queuedReports = new List<QueuedReport>();
        private static bool submittingReports;

        public static void SetTag(string name, string value)
        {
            tags[name] = value;
        }

        public static string GetTag(string name)
        {
            string value;
            if (tags.TryGetValue(name, out value)) return value;
            return null;
        }

        public static void Debug(string message)
        {
            EnqueueReport("debug", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Info(string message)
        {
            EnqueueReport("info", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Warning(string message)
        {
            EnqueueReport("warning", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Error(string message)
        {
            EnqueueReport("error", Assembly.GetCallingAssembly(), GetCurrentMethod(), message);
        }

        public static void Exception(string message, Exception exception)
        {
            EnqueueReport("exception", Assembly.GetCallingAssembly(), GetCurrentMethod(), message, exception.ToString());
        }

        public static void Exception(string message, string raw_stack_trace)
        {
            var stack_trace = raw_stack_trace.Split('\r', '\n');
            var culprit = stack_trace[0].Split('(')[0].Trim();
            EnqueueReport("exception", stack_trace, culprit, message, raw_stack_trace);
        }

        private static void EnqueueReport(string level, Assembly assembly, string culprit, string message, string exception = null)
        {
            var report = new Report(level, culprit, message, exception);
            report.DetectModules(assembly);
            EnqueueReport(report);
        }

        private static void EnqueueReport(string level, string[] stack_trace, string culprit, string message, string exception = null)
        {
            var report = new Report(level, culprit, message, exception);
            report.DetectModules(stack_trace);
            EnqueueReport(report);
        }

        private static void EnqueueReport(Report report)
        {
            queuedReports.Add(new QueuedReport(report));
            if (!submittingReports) SubmitNextReport();
        }

        private static void SubmitNextReport()
        {
            if (queuedReports.Count < 1) return;
            var queued_report = queuedReports[0];
            submittingReports = true;
            Action<int, string> on_request_complete = (code, response) =>
            {
                if (code == 200)
                {
                    queuedReports.RemoveAt(0);
                    submittingReports = false;
                    SubmitNextReport();
                }
                else
                {
                    timers.Once(5f, SubmitNextReport);
                }
            };
            webrequests.EnqueuePost(url, queued_report.Body, on_request_complete, null, queued_report.Headers);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetCurrentMethod()
        {
            var calling_method = (new StackTrace()).GetFrame(2).GetMethod();
            return calling_method.DeclaringType.FullName + "." + calling_method.Name;
        }
    }
}