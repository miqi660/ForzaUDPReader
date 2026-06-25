using System;
using System.Globalization;
using System.Windows.Data;

namespace ForzaUDPReader.WPF.Converters
{
    /// <summary>
    /// 单边圈数 → 角度显示文本（如 "±180°"）
    /// 用于 Popup 中实时预览方向盘点对应的最大物理角度。
    /// </summary>
    public class TurnsToDegreeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double turns && turns > 0)
            {
                double angle = turns * 360.0;
                return $"±{angle:F0}°";
            }
            return "±0°";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
