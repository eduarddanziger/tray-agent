using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;

namespace Tray.Agent
{
    public class StorageViewModel : INotifyPropertyChanged
    {
        private readonly Dispatcher _uiWpfThreadDispatcher;

        private bool _serverActive;
        private string _storageLocation;
        private string _webServerUri;

        internal StorageViewModel(Dispatcher wpfThreadDispatcher)
        {
            _uiWpfThreadDispatcher = wpfThreadDispatcher;
        }

        public ObservableCollection<string> InfoList { get; } = new ObservableCollection<string>();


        public bool ServerActive
        {
            get => _serverActive;
            set
            {
                if (_serverActive == value)
                    return;

                _serverActive = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ServerActive)));
            }
        }

        public string WebServerUri
        {
            get => _webServerUri;
            set
            {
                if (_webServerUri == value)
                    return;

                _webServerUri = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WebServerUri)));
            }
        }

        public string StorageLocation
        {
            get => _storageLocation;
            set
            {
                if (_storageLocation == value)
                    return;

                _storageLocation = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StorageLocation)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddToInfoList(string storageFile)
        {
            var dispatcher = _uiWpfThreadDispatcher;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Invoke(new Action<string>(AddToInfoList), storageFile);
                return;
            }

            InfoList.Clear();

            foreach (var s in File.ReadAllLines(storageFile))
            {
                InfoList.Add(s);
            }
        }
    }
}