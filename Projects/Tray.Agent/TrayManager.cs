using System;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace Tray.Agent
{
    /// <summary>
    ///     TrayManager creates tray icon / context menus and adds behaviour to control the main window
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal class TrayManager
    {
        private readonly NotifyIcon _notifyIcon;

        /// <summary>   Constructor creates tray, menus and tooltip. </summary>
        public TrayManager()
        {
            // Create and setup tray icon and context menu
            _notifyIcon = new NotifyIcon();

            var streamInfo =
                Application.GetResourceStream(new Uri("pack://application:,,,/resources/trayicon.ico"));
            if (streamInfo != null)
                using (streamInfo.Stream)
                {
                    _notifyIcon.Icon = new Icon(streamInfo.Stream);
                }

            _notifyIcon.ContextMenuStrip = new ContextMenuStrip();
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Show", null, OnMenuShow));
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            _notifyIcon.ContextMenuStrip.Items.Add(new ToolStripMenuItem("Exit", null, OnMenuClose));

            // Visualize tray icon and a tooltip
            _notifyIcon.Visible = true;

            _notifyIcon.MouseClick += NotifyIconOnClick;
        }

        private static void OnMenuShow(object sender, EventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.ShowAndActivateOrHideWindow(true);
        }

        private void OnMenuClose(object sender, EventArgs e)
        {
            _notifyIcon.Dispose();
            var myApplication = Application.Current as App; // can't be null

            // ReSharper disable once PossibleNullReferenceException
            myApplication.Shutdown();
        }

        private static void NotifyIconOnClick(object sender, MouseEventArgs eventArgs)
        {
            // ReSharper disable once InvertIf
            if (eventArgs.Button == MouseButtons.Left)
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                mainWindow?.SwitchShowAndActivateOrHideWindow();
            }
        }
    }
}