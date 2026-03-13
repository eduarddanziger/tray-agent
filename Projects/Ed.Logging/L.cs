using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ed.Logging.Log;

namespace Ed.Logging
{
    /// <summary>
    ///     Simple logging L-class, usage: L.Error("FAIL cock-a-doodle-do") or L.MethodInfo("Done.");
    /// </summary>
    public class L
    {
        #region Private Fields & Props

        private static FileLogTarget _logFile;
        private static string _logFileLocation = FileLogTarget.LogFileBaseDirPathname;
        private static string _logFileNamePrefix = "-";
        private static int _logFileMaxSizeInBytes = FileLogTarget.LogFileMaxSizeInBytes;
        private static bool _writeToWindowsConsole = true;
        private static bool _writeToFile = true;
        private static Action<string, int> WriteToFileAction { get; set; }
        private static Action<string, int> WriteToWindowsConsoleAction { get; }
        private static LogChannelWithTransformerAndFilter Channel { get; }

        #endregion

        #region Static Ctor

        static L()
        {
            Channel = new LogChannelWithTransformerAndFilter();
            var entryAssembly = Assembly.GetEntryAssembly();

            if (null != entryAssembly && File.Exists(entryAssembly.Location))
            {
                _logFileLocation = Path.GetDirectoryName(entryAssembly.Location);
                _logFileNamePrefix = Path.GetFileNameWithoutExtension(entryAssembly.Location) + "-";
            }

            WriteToWindowsConsoleAction = (line, type) => Console.WriteLine(line);
        }

        private static void EnsureFileLogTargetReady()
        {
            if (null == _logFile)
            {
                if (_writeToFile)
                    _logFile = AddFileLogTarget(Channel);
                if (_writeToWindowsConsole)
                    Channel.OnForwardingLineToLogTargets += WriteToWindowsConsoleAction;
            }
        }

        #endregion

        #region Private Helpers

        private static void DestroyFileLogTarget()
        {
            if (null == _logFile)
                return;

            if (_writeToFile)
            {
                Channel.OnForwardingLineToLogTargets -= WriteToFileAction;
                _logFile.NotifyWorkerToStopAndWaitForCompletionInMs(100);
                _logFile = null;
            }

            if (_writeToWindowsConsole)
                Channel.OnForwardingLineToLogTargets -= WriteToWindowsConsoleAction;
        }

        private static string CombineClassAndMethodName(string filePath, string method, string message)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (method == null) throw new ArgumentNullException(nameof(method));

            return Path.GetFileNameWithoutExtension(filePath) + (method[0] == '.' ? "" : ".") + // e.g. .ctor 
                   method + ": " + message;
        }


        private static string CombineClassAndMethodName(string filePath, string method, string message,
            int totalInfoCount,
            int currInfoCount)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            if (method == null) throw new ArgumentNullException(nameof(method));

            var typeName = Path.GetFileNameWithoutExtension(filePath);
            var methodPrefix = method[0] == '.' ? "" : "."; // for ctor
            return $"{typeName}{methodPrefix}{method}: {currInfoCount} of {totalInfoCount}: {message}";
        }


        private static FileLogTarget AddFileLogTarget(LogChannel logChannel)
        {
            var logFile = new FileLogTarget(
                _logFileMaxSizeInBytes,
                _logFileLocation,
                _logFileNamePrefix,
                FileLogTarget.LogFileExtention,
                FileLogTarget.LogFileMaxDaysToKeepFile,
                FileLogTarget.SleepTimeoutInMs,
                FileLogTarget.LastChanceTimeoutInMs);

            WriteToFileAction = (line, type) => logFile.Write(line);
            logChannel.OnForwardingLineToLogTargets += WriteToFileAction;

            logFile.StartBackgroundWorker();

            return logFile;
        }

        public static LogChannelWithTransformerAndFilter LogChannel
        {
            get
            {
                EnsureFileLogTargetReady();
                return Channel;
            }
        }

        #endregion

        #region Interface

        public static void Info(string line)
        {
            LogChannel.PushInfo(line);
        }

        public static void MethodInfo(string message,
            [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "")
        {
            LogChannel.PushInfo("INFO " + CombineClassAndMethodName(sourceFilePath, memberName, message));
        }

        public static void MethodInfo(string message, int totalInfoCount, int currInfoCount, string method,
            string sourceFilePath)
        {
            LogChannel.PushInfo("INFO " + CombineClassAndMethodName(sourceFilePath, method, message, totalInfoCount,
                                    currInfoCount));
        }

        public static void Error(string message)
        {
            LogChannel.PushError(message);
        }

        public static void MethodError(string message,
            [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "")
        {
            LogChannel.PushError("FAIL " + CombineClassAndMethodName(sourceFilePath, memberName, message));
        }

        public static void MethodError(string message, int totalInfoCount, int currInfoCount, string method,
            string sourceFilePath)
        {
            LogChannel.PushError("FAIL " + CombineClassAndMethodName(sourceFilePath, method, message, totalInfoCount,
                                     currInfoCount));
        }

        /// <summary>
        ///     Should be used on application exit, e.g. in Application_Exit + Application_DispatcherUnhandledException if WPF
        /// </summary>
        public static void FinAndFlush()
        {
            try
            {
                DestroyFileLogTarget();
            }
            catch
            {
                // ignored
            }
        }

        public static string DirLocation
        {
            get { return _logFileLocation; }
            set
            {
                if (0 != string.Compare(_logFileLocation, value, true, CultureInfo.InvariantCulture))
                {
                    DestroyFileLogTarget();
                    _logFileLocation = value;
                }
            }
        }

        public static bool WriteToWindowsConsole
        {
            get { return _writeToWindowsConsole; }
            set
            {
                if (_writeToWindowsConsole != value)
                {
                    DestroyFileLogTarget();
                    _writeToWindowsConsole = value;
                }
            }
        }

        public static bool WriteToFile
        {
            get { return _writeToFile; }
            set
            {
                if (_writeToFile != value)
                {
                    DestroyFileLogTarget();
                    _writeToFile = value;
                }
            }
        }


        public static string FilePrefix
        {
            get { return _logFileNamePrefix; }
            set
            {
                if (0 != string.Compare(_logFileNamePrefix, value, true, CultureInfo.InvariantCulture))
                {
                    DestroyFileLogTarget();
                    _logFileNamePrefix = value;
                }
            }
        }

        public static int LogFileMaxSizeInBytes
        {
            get { return _logFileMaxSizeInBytes; }
            set
            {
                if (_logFileMaxSizeInBytes != value)
                {
                    DestroyFileLogTarget();
                    _logFileMaxSizeInBytes = value;
                }
            }
        }

        #endregion
    }
}