using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Ed.Logging;
using Ed.Logging.Common;
using Ed.Logging.Log;

namespace Tray.Agent
{
    public partial class MainWindow
    {
        public const string AssemblyTitle = "The Storage";

        #region Public Ctor

        [SupportedOSPlatform("windows")]
        public MainWindow()
        {
            if (((App) Application.Current).IsAppExitingOnStart) return;

            _uiWpfThreadDispatcher = Dispatcher.CurrentDispatcher;

            StorageViewModel = new StorageViewModel(_uiWpfThreadDispatcher);
            AttachLoggingToListBox(L.LogChannel);
            StorageViewModel.StorageLocation = L.DirLocation;
            StorageViewModel.WebServerUri = Settings.Default.HttpServerUri;

            InitializeComponent();

            ClientConnected.DataContext = StorageViewModel;
            InfoList.DataContext = StorageViewModel;
            WebServerTextBox.DataContext = StorageViewModel;

            WindowTitle = EntryAssemblyInfo.Title + " " + EntryAssemblyInfo.FileVersion;

            // Attach log to list box

            // Setup rest of main window event handlers
            StateChanged += MainWindow_OnWindowStateChanged;

            Closing += MainWindow_OnClosing;

            var unused = new TrayManager();
        }

        #endregion

        #region Internal Application Info and Tittle properties

        private readonly Dispatcher _uiWpfThreadDispatcher;

        internal string WindowTitle
        {
            get => (string) GetValue(WindowTitleProperty);
            set => SetValue(WindowTitleProperty, value);
        }

        #endregion

        #region Dependency Property Static Defs

        internal static readonly DependencyProperty ObservableLogTargetProperty = DependencyProperty.Register(
            "ObservableLogTarget",
            typeof(IObservableLogTarget),
            typeof(MainWindow),
            new FrameworkPropertyMetadata(default(ObservableCappedQueueLogTarget),
                ObservableLogTargetPropertyChangedCallback)
        );

        internal static readonly DependencyProperty WindowTitleProperty = DependencyProperty.Register(
            "WindowTitle", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        #endregion

        #region Logging & Scroll-To-End

        private ScrollViewer _logScroller;
        public StorageViewModel StorageViewModel { get; }

        internal IObservableLogTarget ObservableLogTarget
        {
            get => (IObservableLogTarget) GetValue(ObservableLogTargetProperty);
            set => SetValue(ObservableLogTargetProperty, value);
        }

        private void WriteToObservableLogTargetForwardedToWpfThread(string info)
        {
            var dispatcher = _uiWpfThreadDispatcher;
            if (!dispatcher.CheckAccess())
                dispatcher.Invoke(new Action<string>(WriteToObservableLogTargetForwardedToWpfThread), info);
            else
                ObservableLogTarget.Write(info);
        }

        private void AttachLoggingToListBox(LogChannel logChannel)
        {
            ObservableLogTarget = new ObservableCappedQueueLogTarget();

            // info type parameter ("error", "info" etc.) is ignored below
            logChannel.OnForwardingLineToLogTargets +=
                (info, infoT) => WriteToObservableLogTargetForwardedToWpfThread(info);
        }

        private void ObservableLogTargetCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_logScroller == null)
                _logScroller = LogControl.FindVisualChild<ScrollViewer>();

            _logScroller?.ScrollToEnd();
        }

        private static void ObservableLogTargetPropertyChangedCallback(
            DependencyObject source,
            DependencyPropertyChangedEventArgs arguments)
        {
            var control = source as MainWindow;
            if (control == null)
                return;

            if (arguments.OldValue is INotifyCollectionChanged observableLogTargetOldValue)
                observableLogTargetOldValue.CollectionChanged -=
                    control.ObservableLogTargetCollectionChangedEventHandler;

            if (arguments.NewValue is INotifyCollectionChanged observableLogTargetNewValue)
                observableLogTargetNewValue.CollectionChanged +=
                    control.ObservableLogTargetCollectionChangedEventHandler;
        }

        #endregion

        #region Helper Functions

        public void SwitchShowAndActivateOrHideWindow()
        {
            ShowAndActivateOrHideWindow(Visibility != Visibility.Visible);
        }

        public void ShowAndActivateOrHideWindow(bool show)
        {
            Visibility = show ? Visibility.Visible : Visibility.Hidden;

            ShowInTaskbar = show;

            if (show)
                WindowState = WindowState.Normal;
        }

        #endregion

        #region Window Event Handlers

        private void MainWindow_OnInitialized(object sender, EventArgs e)
        {
            if (((App) Application.Current).IsAppExitingOnStart) return;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (((App) Application.Current).IsAppExitingOnStart) return;

            ShowAndActivateOrHideWindow(false);
            e.Cancel = true;
        }

        private void MainWindow_OnWindowStateChanged(object sender, EventArgs eventArgs)
        {
            if (Visibility != Visibility.Visible)
                ShowInTaskbar = false;
        }

        private void MainWindow_OnClosed(object sender, EventArgs e)
        {
            if (((App) Application.Current).IsAppExitingOnStart) return;
        }

        #endregion
    }
}