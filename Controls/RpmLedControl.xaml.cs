using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace ForzaUDPReader.WPF.Controls
{
    /// <summary>
    /// 7个圆形 RPM LED 指示灯条 (3绿 + 2黄 + 2红)
    /// 支持高转速全红爆闪逻辑，使用 OnRender 自绘 + DispatcherTimer 驱动闪烁
    /// </summary>
    public partial class RpmLedControl : System.Windows.Controls.UserControl
    {
        private const int LedCount = 7;
        private const double LedSize = 10;
        private const double LedSpacing = 3;

        // 爆闪状态
        private bool _blinkState;
        private readonly DispatcherTimer _blinkTimer;

        // 缓存最大转速，避免颠簸时不稳定
        private float _cachedMaxRpm;

        #region 依赖属性

        public float CurrentRpm
        {
            get => (float)GetValue(CurrentRpmProperty);
            set => SetValue(CurrentRpmProperty, value);
        }
        public static readonly DependencyProperty CurrentRpmProperty =
            DependencyProperty.Register(nameof(CurrentRpm), typeof(float), typeof(RpmLedControl),
                new PropertyMetadata(0f));

        public float MaxRpm
        {
            get => (float)GetValue(MaxRpmProperty);
            set => SetValue(MaxRpmProperty, value);
        }
        public static readonly DependencyProperty MaxRpmProperty =
            DependencyProperty.Register(nameof(MaxRpm), typeof(float), typeof(RpmLedControl),
                new PropertyMetadata(0f));

        #endregion

        // 预冻结画刷
        private static readonly SolidColorBrush OffBrush = CreateFrozenBrush(60, 60, 60);
        private static readonly SolidColorBrush GreenBrush = CreateFrozenBrush(0, 200, 0);
        private static readonly SolidColorBrush YellowBrush = CreateFrozenBrush(255, 200, 0);
        private static readonly SolidColorBrush RedBrush = CreateFrozenBrush(255, 50, 50);
        private static readonly SolidColorBrush LedBorderBrush = CreateFrozenBrush(30, 30, 30);

        public RpmLedControl()
        {
            InitializeComponent();

            // 100ms 爆闪定时器
            _blinkTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _blinkTimer.Tick += (s, e) =>
            {
                _blinkState = !_blinkState;
                InvalidateVisual(); // 强制重绘以显示闪烁效果
            };
            _blinkTimer.Start();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            // 更新缓存最大转速
            if (MaxRpm > 0 && MaxRpm > _cachedMaxRpm)
                _cachedMaxRpm = MaxRpm;

            float maxRpm = _cachedMaxRpm > 0 ? _cachedMaxRpm : MaxRpm;
            float rpmPercent = maxRpm > 0 ? CurrentRpm / maxRpm : 0;
            rpmPercent = Math.Min(rpmPercent, 1f);

            // 转速 >= 85% 时全红警告
            bool allRed = rpmPercent >= 0.85f;

            // LED 总宽度 = 7*10 + 6*3 = 88
            double ledTotalWidth = LedCount * LedSize + (LedCount - 1) * LedSpacing;
            double startX = (w - ledTotalWidth) / 2;
            double startY = (h - LedSize) / 2;

            var borderPen = new Pen(LedBorderBrush, 1);
            borderPen.Freeze();

            for (int i = 0; i < LedCount; i++)
            {
                float threshold = (i + 0.3f) / LedCount;
                bool isOn = rpmPercent >= threshold;

                SolidColorBrush ledBrush;
                if (!isOn)
                    ledBrush = OffBrush;
                else if (allRed && !_blinkState)
                    ledBrush = OffBrush;           // 爆闪关闭
                else if (allRed)
                    ledBrush = RedBrush;            // 全红警告
                else if (i < 3)
                    ledBrush = GreenBrush;          // 绿色
                else if (i < 5)
                    ledBrush = YellowBrush;         // 黄色
                else
                    ledBrush = RedBrush;            // 红色

                double cx = startX + i * (LedSize + LedSpacing) + LedSize / 2;
                double cy = startY + LedSize / 2;

                // 填充圆形 LED
                dc.DrawEllipse(ledBrush, null, new Point(cx, cy), LedSize / 2, LedSize / 2);
                // LED 边框
                dc.DrawEllipse(null, borderPen, new Point(cx, cy), LedSize / 2, LedSize / 2);
            }
        }

        private static SolidColorBrush CreateFrozenBrush(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
