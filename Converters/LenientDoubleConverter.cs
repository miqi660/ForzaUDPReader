using System;
using System.Globalization;
using System.Windows.Data;

namespace ForzaUDPReader.WPF.Converters
{
    /// <summary>
    /// 宽容的 string ↔ double 双向转换器。
    /// 配合 UpdateSourceTrigger=PropertyChanged 使用：
    /// 输入过程中不合法的中间态（如 "0."、""、"-"）不会写回源属性，
    /// 而是返回 Binding.DoNothing，让用户继续输入而不被回滚。
    /// </summary>
    public class LenientDoubleConverter : IValueConverter
    {
        public double Min { get; set; } = double.MinValue;
        public double Max { get; set; } = double.MaxValue;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 源 → TextBox：直接显示数字
            return value is double d ? d.ToString("G", culture) : "0.5";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s)
                return Binding.DoNothing;

            s = s.Trim();
            if (string.IsNullOrEmpty(s))
                return Binding.DoNothing;

            // 容忍中间态：单个负号、末尾小数点、末尾的零（如 "1.50"）
            if (s == "-" || s.EndsWith(".") || s.EndsWith(".0"))
                return Binding.DoNothing;

            if (!double.TryParse(s, NumberStyles.Float, culture, out double result))
                return Binding.DoNothing;

            // 范围钳制：越界时不写回源，避免用户输入中途被截断
            if (result < Min || result > Max)
                return Binding.DoNothing;

            return result;
        }
    }
}
