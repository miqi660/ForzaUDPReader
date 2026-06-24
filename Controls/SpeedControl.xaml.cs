using System.Windows;
using System.Windows.Controls;

namespace ForzaUDPReader.WPF.Controls
{
    /// <summary>
    /// 速度显示 — 纯 XAML + 依赖属性绑定
    /// 显示整数速度值和 "km/h" 单位
    /// </summary>
    public partial class SpeedControl : UserControl
    {
        public int SpeedValue
        {
            get => (int)GetValue(SpeedValueProperty);
            set => SetValue(SpeedValueProperty, value);
        }

        public static readonly DependencyProperty SpeedValueProperty =
            DependencyProperty.Register(nameof(SpeedValue), typeof(int), typeof(SpeedControl),
                new PropertyMetadata(0, OnSpeedValueChanged));

        public SpeedControl()
        {
            InitializeComponent();
        }

        private static void OnSpeedValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SpeedControl)d;
            control.SpeedText.Text = ((int)e.NewValue).ToString();
        }
    }
}
