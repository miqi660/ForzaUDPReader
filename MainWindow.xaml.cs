using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
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

        // ═══════════════════════════════════════════════════════════
        // 图表列抽屉式动画 — 字段与常量
        // ═══════════════════════════════════════════════════════════

        // 图表列逻辑宽度：ChartControl(227) + Margin(10+10) = 237
        private const double ChartColumnLogicalWidth = 237.0;
        // 内部 Grid 初始总逻辑宽度（三列之和）：237 + 89 + 251 = 577
        private const double InitialInnerLogicalWidth = 577.0;
        // 图表隐藏后的逻辑宽度：89 + 251 = 340
        private const double NoChartInnerLogicalWidth = InitialInnerLogicalWidth - ChartColumnLogicalWidth;
        // Window 初始 MinWidth
        private const double InitialMinWidth = 500.0;
        // MinWidth 中图表列所占分量：500 × (237/577) ≈ 205.4
        private static readonly double ChartMinWidthComponent =
            InitialMinWidth * (ChartColumnLogicalWidth / InitialInnerLogicalWidth);
        // 图表实际目标 MinWidth
        private static readonly double ChartHiddenMinWidth = InitialMinWidth - ChartMinWidthComponent;

        // 启动时从 InnerGrid 实际测量捕获，杜绝一切魔法数字
        private double _innerLogicalWidth;   // ≈ 577（全量内容宽）
        private double _innerLogicalHeight;  // ≈ 147（行高之和，不含 Border Margin）

        // 动画状态机
        private bool _isAnimating;           // 是否正在播放宽度动画
        private bool _animationTargetShow;   // 本次动画目标：true=展开 / false=收缩

        public MainWindow()
        {
            InitializeComponent();
            InitializeReceiver();
            StartRefreshTimer();

            // 捕获 InnerGrid 的初始逻辑尺寸（仅首次 Loaded，图表尚未被隐藏过）
            Loaded += OnWindowLoaded;
            // 用户手动拖拽缩放时，中止正在进行的动画，防止动画与用户操作冲突
            SizeChanged += OnWindowSizeChanged;
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

        /// <summary>
        /// 踏板模式切换（离合/手刹）
        /// </summary>
        private void OnPedalModeChanged(object sender, RoutedEventArgs e)
        {
            if (Pedals == null || RbHandbrake == null) return;
            Pedals.IsHandbrakeMode = RbHandbrake.IsChecked == true;
        }

        /// <summary>
        /// 开度曲线显示/隐藏 — 抽屉式平滑过渡
        ///
        /// ═══ 数学推导 ═══
        /// Viewbox Stretch="Uniform" 恒等式：
        ///   Window.Width     InnerGrid.LogicalWidth
        ///   ────────────── = ──────────────────────
        ///   Window.Height    InnerGrid.LogicalHeight
        ///
        /// ⇒  Scale = Window.Height / InnerGrid.LogicalHeight（仅与高度有关，零累积误差）
        /// ⇒  TargetWidth = Scale × 目标逻辑宽度
        ///
        /// ═══ 动画时序 ═══
        /// 隐藏：① Collapsed → ② 动画缩 Width（右组件连续膨胀填满窗口）
        /// 显示：① 动画扩 Width（右组件在窄内容中填满）→ ② Visible（图表就位，无跳跃）
        /// </summary>
        private void OnShowTraceToggled(object sender, RoutedEventArgs e)
        {
            if (Chart == null || _innerLogicalHeight <= 0) return;

            bool show = ShowTraceToggle.IsChecked == true;

            // ── 1. 最大化状态：仅切换可见性，跳过动画 ──
            if (WindowState == WindowState.Maximized)
            {
                Chart.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
                return;
            }

            // ── 2. 中止当前动画（防止重叠冲突）──
            StopWidthAnimation();
            // StopWidthAnimation 已把当前动画值提交为 Width 的基值，可安全读取
            double currentWidth = this.Width;

            // ── 3. 绝对精准的缩放比（基于高度，与宽度无关）──
            //   Scale = ActualHeight / _innerLogicalHeight
            //   例：167 / 147.34 ≈ 1.133（初始状态，恒定参考值）
            double scale = this.ActualHeight / _innerLogicalHeight;

            // ── 4. 根据方向执行不同的 Visibility 时序 ──
            if (show)
            {
                // ★ 展开：先动画扩窗口，图表在动画结束后才显示
                // 动画期间内容宽度 = 340（图表仍 Collapsed）
                // 右组件在 Viewbox 内连续膨胀，视觉完全稳定
                _animationTargetShow = true;
            }
            else
            {
                // ★ 收缩：先隐藏图表，再动画缩窗口
                // Visibility 改变 → Grid 列折叠 → 内容宽度 577→340
                // 紧接着动画驱动 Width 729→430，Viewbox 比例连续变化，无跳跃
                Chart.Visibility = Visibility.Collapsed;
                _animationTargetShow = false;
            }

            // ── 5. 计算绝对目标宽度（非增量累加，零漂移）──
            double targetLogicalWidth = _animationTargetShow
                ? InitialInnerLogicalWidth      // 577（图表列展开）
                : NoChartInnerLogicalWidth;     // 340（图表列折叠）
            double targetWidth = scale * targetLogicalWidth;

            // ── 6. 临时解除 MinWidth 约束（避免动画过程中被截断）──
            this.MinWidth = 0;

            // ── 7. 配置缓动动画 ──
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var duration = new Duration(TimeSpan.FromSeconds(0.3));

            var widthAnim = new DoubleAnimation(currentWidth, targetWidth, duration)
            {
                EasingFunction = ease,
                FillBehavior = FillBehavior.HoldEnd
            };
            widthAnim.Completed += OnWidthAnimationCompleted;

            // ── 8. 开始动画 ──
            _isAnimating = true;
            this.BeginAnimation(WidthProperty, widthAnim);
        }

        // ═══════════════════════════════════════════════════════════
        //  动画辅助方法
        // ═══════════════════════════════════════════════════════════

        /// <summary>
        /// 中止正在进行的宽度动画，并将当前动画值提交为基值。
        /// 调用后读取 Width 即为动画此刻的实际物理宽度。
        /// </summary>
        private void StopWidthAnimation()
        {
            if (!_isAnimating) return;

            // 读取当前动画值（用于下一次动画的起始帧）
            double animatedWidth = this.Width;
            // 移除动画 → 当前值提交为本地基值
            this.BeginAnimation(WidthProperty, null);
            this.Width = animatedWidth;
            _isAnimating = false;
        }

        /// <summary>
        /// 宽度动画播放完成 → 提交最终值、恢复 MinWidth、显示图表（展开方向）。
        /// </summary>
        private void OnWidthAnimationCompleted(object sender, EventArgs e)
        {
            if (sender is DoubleAnimation anim)
                anim.Completed -= OnWidthAnimationCompleted;

            // 清除动画，使 Width 回到本地值
            this.BeginAnimation(WidthProperty, null);
            _isAnimating = false;

            // 恢复 MinWidth（目标状态下应有的值）
            this.MinWidth = _animationTargetShow ? InitialMinWidth : ChartHiddenMinWidth;

            // 展开动画结束 → 此时窗口已到位，显示图表（零闪烁就位）
            if (_animationTargetShow)
                Chart.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 启动时捕获 InnerGrid 的实际逻辑尺寸（一次测量，永不修改）。
        /// 这两个值是绝对精准缩放的基准线，替代一切硬编码魔法数字。
        /// </summary>
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _innerLogicalWidth = InnerGrid.ActualWidth;   // ≈ 577
            _innerLogicalHeight = InnerGrid.ActualHeight;  // ≈ 147
        }

        /// <summary>
        /// 用户手动拖拽窗口边框 → 立即中止动画，让用户的操作直接接管。
        /// 防止动画与手动缩放互相"打架"导致窗口抖动。
        /// </summary>
        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_isAnimating)
                StopWidthAnimation();
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
            Pedals.HandbrakePercent = data.HandbrakePercent;

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

            Chart.AddDataPoint(data.ThrottlePercent, data.BrakePercent, data.ClutchPercent, data.HandbrakePercent);
        }

        private void OnErrorOccurred(object sender, Exception ex)
        {
            Console.WriteLine($"UDP Error: {ex.Message}");
        }

        protected override void OnClosed(EventArgs e)
        {
            // 中止可能正在进行的动画
            StopWidthAnimation();
            Loaded -= OnWindowLoaded;
            SizeChanged -= OnWindowSizeChanged;
            _refreshTimer?.Stop();
            _receiver?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}
