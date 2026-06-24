using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ForzaUDPReader.WPF.Controls
{
    public partial class PedalsControl : UserControl
    {
        #region 依赖属性

        public double ClutchPercent
        {
            get => (double)GetValue(ClutchPercentProperty);
            set => SetValue(ClutchPercentProperty, value);
        }
        public static readonly DependencyProperty ClutchPercentProperty =
            DependencyProperty.Register(nameof(ClutchPercent), typeof(double), typeof(PedalsControl),
                new PropertyMetadata(0.0, OnPedalPercentChanged));

        public double BrakePercent
        {
            get => (double)GetValue(BrakePercentProperty);
            set => SetValue(BrakePercentProperty, value);
        }
        public static readonly DependencyProperty BrakePercentProperty =
            DependencyProperty.Register(nameof(BrakePercent), typeof(double), typeof(PedalsControl),
                new PropertyMetadata(0.0, OnPedalPercentChanged));

        public double ThrottlePercent
        {
            get => (double)GetValue(ThrottlePercentProperty);
            set => SetValue(ThrottlePercentProperty, value);
        }
        public static readonly DependencyProperty ThrottlePercentProperty =
            DependencyProperty.Register(nameof(ThrottlePercent), typeof(double), typeof(PedalsControl),
                new PropertyMetadata(0.0, OnPedalPercentChanged));

        public double HandbrakePercent
        {
            get => (double)GetValue(HandbrakePercentProperty);
            set => SetValue(HandbrakePercentProperty, value);
        }
        public static readonly DependencyProperty HandbrakePercentProperty =
            DependencyProperty.Register(nameof(HandbrakePercent), typeof(double), typeof(PedalsControl),
                new PropertyMetadata(0.0, OnPedalPercentChanged));

        public bool IsHandbrakeMode
        {
            get => (bool)GetValue(IsHandbrakeModeProperty);
            set => SetValue(IsHandbrakeModeProperty, value);
        }
        public static readonly DependencyProperty IsHandbrakeModeProperty =
            DependencyProperty.Register(nameof(IsHandbrakeMode), typeof(bool), typeof(PedalsControl),
                new PropertyMetadata(false, OnModeChanged));

        #endregion

        // 手刹颜色（橙色系）
        private static readonly SolidColorBrush HandbrakeBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
        private static readonly SolidColorBrush ClutchBrush = new SolidColorBrush(Color.FromRgb(74, 110, 224));

        public PedalsControl()
        {
            InitializeComponent();
            HandbrakeBrush.Freeze();
            ClutchBrush.Freeze();
        }

        private static void OnPedalPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PedalsControl)d).UpdateVisuals();
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (PedalsControl)d;
            bool isHandbrake = (bool)e.NewValue;

            // 切换标签文字
            c.LeftPedalLabel.Text = isHandbrake ? "H" : "C";

            // 切换颜色
            var brush = isHandbrake ? HandbrakeBrush : ClutchBrush;
            c.LeftCapTop.Background = brush;
            c.LeftCapBottom.Background = brush;
            c.LeftFill.Background = brush;

            c.UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // 最左侧踏板：根据模式选择数据源
            double leftPercent = IsHandbrakeMode ? HandbrakePercent : ClutchPercent;
            LeftFillScale.ScaleY = leftPercent / 100.0;
            LeftValue.Text = ((int)leftPercent).ToString();

            // 刹车
            BrakeFillScale.ScaleY = BrakePercent / 100.0;
            BrakeValue.Text = ((int)BrakePercent).ToString();

            // 油门
            ThrottleFillScale.ScaleY = ThrottlePercent / 100.0;
            ThrottleValue.Text = ((int)ThrottlePercent).ToString();
        }
    }
}
