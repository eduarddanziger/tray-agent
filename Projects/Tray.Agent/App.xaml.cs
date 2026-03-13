using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Ed.Logging;

namespace Tray.Agent
{
    public partial class App
    {
        private volatile bool _isShuttingDown;
        public bool IsAppExitingOnStart { get; private set; }

        public void NotifyAppToExitOnStart()
        {
            IsAppExitingOnStart = true;
        }

        public new void Shutdown(int exitCode = 0)
        {
            _isShuttingDown = true;
            base.Shutdown(exitCode);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var thisProc = Process.GetCurrentProcess();
            if (Process.GetProcessesByName(thisProc.ProcessName).Length > 1)
            {
                NotifyAppToExitOnStart();
                if (_isShuttingDown)
                {
                    return;
                }

                Exit -= Application_Exit;
                ShowErrorMessageBox(new Exception($"{thisProc.ProcessName} already running!"));
                Shutdown(1);
                return;
            }

            InitLogChannelAndStartLoggingToFile();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Dispatcher.CurrentDispatcher.UnhandledException += CurrentDispatcher_UnhandledException;
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            L.FinAndFlush();
        }

        private static void InitLogChannelAndStartLoggingToFile()
        {
            var recordDir = Settings.Default.RecordDirectory.Trim();
            if (!Directory.Exists(recordDir))
            {
                try
                {
                    var fullPath = Path.GetFullPath(recordDir);
                    var fullPathWithNoEndSeparator =
                        fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var parentDir = Path.GetDirectoryName(fullPathWithNoEndSeparator);
                    if (Directory.Exists(parentDir))
                    {
                        Directory.CreateDirectory(recordDir);
                    }
                }
                catch
                {
                    // ignored
                }
            }

            L.DirLocation = Directory.Exists(recordDir)
                ? recordDir
                : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            L.FilePrefix = Assembly.GetExecutingAssembly().GetName().Name + "-";
            L.LogFileMaxSizeInBytes = 5300000;
            L.WriteToWindowsConsole = false;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            GracefulShutdown(exception);
        }

        private void CurrentDispatcher_UnhandledException(object sender,
            DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            GracefulShutdown(dispatcherUnhandledExceptionEventArgs.Exception);
        }

        private void GracefulShutdown(Exception exceptionOrNull)
        {
            if (_isShuttingDown)
            {
                return;
            }

            L.Error("ERROR: " + (exceptionOrNull?.ToString() ?? "Unknown"));
            L.FinAndFlush();

            Exit -= Application_Exit;
            ShowErrorMessageBox(exceptionOrNull);
            Shutdown(1);
        }

        private static void ShowErrorMessageBox(Exception exceptionOrNull,
            string messageBoxBaseMessage = "Unrecoverable error found")
        {
            var messageBoxMessage = messageBoxBaseMessage;
            if (exceptionOrNull != null)
            {
                messageBoxMessage = $"{messageBoxBaseMessage}:\n{exceptionOrNull.Message}";
                if (exceptionOrNull.InnerException != null)
                {
                    messageBoxMessage += $"\n--> {exceptionOrNull.InnerException.Message}.";
                }
            }
            else
            {
                messageBoxMessage += "\n:Unknown.";
            }

            MessageBox.Show(messageBoxMessage, Agent.MainWindow.AssemblyTitle, MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private void Application_DispatcherUnhandledException(object sender,
            DispatcherUnhandledExceptionEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            var exception = eventArgs.Exception;

            GracefulShutdown(exception);
        }
    }
}