using System.Windows;
using System.Windows.Controls;

namespace ForzaUDPReader.WPF.Controls
{
    /// <summary>
    /// 踏板区 — 纯 XAML + 依赖属性驱动
    /// 通过 ScaleTransform 实现从底部向上的填充效果
    /// </summary>
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

        #endregion

        public PedalsControl()
        {
            InitializeComponent();
        }

        private static void OnPedalPercentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((PedalsControl)d).UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // 离合
            ClutchFillScale.ScaleY = ClutchPercent / 100.0;
            ClutchValue.Text = ((int)ClutchPercent).ToString();

            // 刹车
            BrakeFillScale.ScaleY = BrakePercent / 100.0;
            BrakeValue.Text = ((int)BrakePercent).ToString();

            // 油门
            ThrottleFillScale.ScaleY = ThrottlePercent / 100.0;
            ThrottleValue.Text = ((int)ThrottlePercent).ToString();
        }
    }
}
