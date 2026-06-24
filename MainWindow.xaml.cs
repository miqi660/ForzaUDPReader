using System;
using System.Windows;
using System.Windows.Input;
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

        #region 窗口交互

        /// <summary>
        /// 按住窗口任意非交互空白处拖拽移动
        /// </summary>
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 确保不是点击在按钮等控件上时才拖拽
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        /// <summary>
        /// 齿轮按钮：切换透明度调节弹窗
        /// </summary>
        private void OnGearButtonClick(object sender, RoutedEventArgs e)
        {
            OpacityPopup.IsOpen = !OpacityPopup.IsOpen;
        }

        /// <summary>
        /// 关闭按钮
        /// </summary>
        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region UDP 接收与刷新

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
            _refreshTimer.Tick += OnRefreshTimerTick;
            _refreshTimer.Start();
        }

        private void OnRefreshTimerTick(object sender, EventArgs e)
        {
            ForzaTelemetryData data;
            bool hasData;
            lock (_dataLock)
            {
                data = _currentData;
                hasData = _hasReceivedData;
            }

            if (!hasData) return;

            // 更新依赖属性驱动的控件
            Pedals.ClutchPercent = data.ClutchPercent;
            Pedals.BrakePercent = data.BrakePercent;
            Pedals.ThrottlePercent = data.ThrottlePercent;

            Gear.GearString = data.GearString;
            Speed.SpeedValue = (int)data.SpeedKmh;

            RpmLed.CurrentRpm = data.CurrentEngineRpm;
            RpmLed.MaxRpm = data.EngineMaxRpm;

            Steering.SteerPercent = data.SteerPercent;

            // 驱动自绘控件重绘
            Chart.InvalidateVisual();
            RpmLed.InvalidateVisual();
            Steering.InvalidateVisual();
        }

        private void OnDataReceived(object sender, ForzaTelemetryData data)
        {
            lock (_dataLock)
            {
                _currentData = data;
                _hasReceivedData = true;
            }

            Chart.AddDataPoint(data.ThrottlePercent, data.BrakePercent, data.ClutchPercent);
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

        #endregion
    }
}
