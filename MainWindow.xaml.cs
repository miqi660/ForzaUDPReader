using System;
using System.Windows;
using System.Windows.Threading;
using ForzaUDPReader.WPF.Data;

namespace ForzaUDPReader.WPF
{
    public partial class MainWindow : Window
    {
        private UdpReceiver _receiver;
        private ForzaTelemetryData _currentData;
        private bool _hasReceivedData;
        private readonly object _dataLock = new object();
        private DispatcherTimer _refreshTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeReceiver();
            StartRefreshTimer();
        }

        private void InitializeReceiver()
        {
            _receiver = new UdpReceiver(21337);
            _receiver.DataReceived += OnDataReceived;
            _receiver.ErrorOccurred += OnErrorOccurred;
            _receiver.Start();
        }

        private void StartRefreshTimer()
        {
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _refreshTimer.Tick += (s, e) =>
            {
                ForzaTelemetryData data;
                bool hasData;
                lock (_dataLock)
                {
                    data = _currentData;
                    hasData = _hasReceivedData;
                }

                if (hasData)
                {
                    HudControl.UpdateData(data);
                }
            };
            _refreshTimer.Start();
        }

        private void OnDataReceived(object sender, ForzaTelemetryData data)
        {
            lock (_dataLock)
            {
                _currentData = data;
                _hasReceivedData = true;
            }
        }

        private void OnErrorOccurred(object sender, Exception ex)
        {
            Console.WriteLine($"UDP Error: {ex.Message}");
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _refreshTimer?.Stop();
            _receiver?.Dispose();
        }
    }
}
