using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ForzaUDPReader.WPF.Converters
{
    /// <summary>
    /// 将 bool 转换为 GridLength：
    /// true  → Star（弹性填充）
    /// false → 0（释放列空间）
    /// </summary>
    public class BoolToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
