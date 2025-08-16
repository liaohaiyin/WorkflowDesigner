using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WorkflowDesigner.UI.Converters
{
    /// <summary>
    /// 颜色字符串到Brush的转换器
    /// </summary>
    public class ColorStringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is string colorString)
                {
                    // 尝试解析颜色字符串
                    if (colorString.StartsWith("#"))
                    {
                        // 十六进制颜色
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
                    }
                    else
                    {
                        // 命名颜色
                        var color = (Color)ColorConverter.ConvertFromString(colorString);
                        return new SolidColorBrush(color);
                    }
                }
                
                // 默认返回黑色
                return new SolidColorBrush(Colors.Black);
            }
            catch
            {
                // 转换失败时返回默认颜色
                return new SolidColorBrush(Colors.Black);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("不支持反向转换");
        }
    }
}