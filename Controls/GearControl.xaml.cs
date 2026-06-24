using System.Windows;
using System.Windows.Controls;

namespace ForzaUDPReader.WPF.Controls
{
    /// <summary>
    /// 档位显示 — 纯 XAML + 依赖属性绑定
    /// 支持 R / N / 1-16 档位显示
    /// </summary>
    public partial class GearControl : UserControl
    {
        public string GearString
        {
            get => (string)GetValue(GearStringProperty);
            set => SetValue(GearStringProperty, value);
        }

        public static readonly DependencyProperty GearStringProperty =
            DependencyProperty.Register(nameof(GearString), typeof(string), typeof(GearControl),
                new PropertyMetadata("N", OnGearStringChanged));

        public GearControl()
        {
            InitializeComponent();
        }

        private static void OnGearStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (GearControl)d;
            control.GearText.Text = e.NewValue as string ?? "N";
        }
    }
}
