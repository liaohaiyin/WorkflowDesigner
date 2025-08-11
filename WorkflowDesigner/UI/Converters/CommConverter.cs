using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WorkflowDesigner.UI.ViewModels;
// ============================================================================
// 2. 其他常用转换器
// ============================================================================
namespace WorkflowDesigner.UI.Converters
{
    public class MessageTypeToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is OutputMessageType messageType)
            {
                var style = new Style(typeof(System.Windows.Controls.TextBlock));

                var foregroundBrush = GetForegroundBrush(messageType);

                style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.ForegroundProperty, foregroundBrush));

                // 根据消息类型设置不同的字体样式
                switch (messageType)
                {
                    case OutputMessageType.Error:
                        style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty, FontWeights.SemiBold));
                        break;
                    case OutputMessageType.Warning:
                        style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontStyleProperty, FontStyles.Normal));
                        break;
                    case OutputMessageType.Success:
                        style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty, FontWeights.Medium));
                        break;
                    case OutputMessageType.Debug:
                        style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontStyleProperty, FontStyles.Italic));
                        style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty, 11.0));
                        break;
                }

                return style;
            }

            // 默认样式
            return new Style(typeof(System.Windows.Controls.TextBlock));
        }

        public Brush GetForegroundBrush(OutputMessageType messageType)
        {
            switch (messageType)
            {
                case OutputMessageType.Info:
                    return new SolidColorBrush(Color.FromRgb(33, 150, 243));      // 蓝色

                case OutputMessageType.Warning:
                    return new SolidColorBrush(Color.FromRgb(255, 152, 0));      // 橙色

                case OutputMessageType.Error:
                    return new SolidColorBrush(Color.FromRgb(244, 67, 54));      // 红色

                case OutputMessageType.Success:
                    return new SolidColorBrush(Color.FromRgb(76, 175, 80));      // 绿色

                case OutputMessageType.Debug:
                    return new SolidColorBrush(Color.FromRgb(156, 39, 176));     // 紫色

                default:
                    return new SolidColorBrush(Color.FromRgb(66, 66, 66));       // 深灰色
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // 布尔值转可见性转换器（WPF内置，但为了完整性包含在这里）
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }

    // 反向布尔值转可见性转换器
    public class InvertedBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return true;
        }
    }

    // 空值转可见性转换器
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 字符串为空转可见性转换器
    public class StringEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 时间跨度格式化转换器
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan timeSpan)
            {
                if (timeSpan.TotalDays >= 1)
                    return $"{(int)timeSpan.TotalDays}天 {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
                else
                    return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (TimeSpan.TryParse(value?.ToString(), out var result))
                return result;
            return TimeSpan.Zero;
        }
    }

    // 数字格式化转换器
    public class NumberFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue.ToString("N0");
            if (value is double doubleValue)
                return doubleValue.ToString("N2");
            if (value is decimal decimalValue)
                return decimalValue.ToString("N2");
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // 枚举到字符串转换器
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                switch (enumValue)
                {
                    case WorkflowDesigner.Core.Models.WorkflowInstanceStatus.Running:
                        return "运行中";
                    case WorkflowDesigner.Core.Models.WorkflowInstanceStatus.Paused:
                        return "已暂停";
                    case WorkflowDesigner.Core.Models.WorkflowInstanceStatus.Completed:
                        return "已完成";
                    case WorkflowDesigner.Core.Models.WorkflowInstanceStatus.Failed:
                        return "失败";
                    case WorkflowDesigner.Core.Models.WorkflowInstanceStatus.Terminated:
                        return "已终止";

                    case WorkflowDesigner.Core.Models.WorkflowNodeStatus.Pending:
                        return "待处理";
                    case WorkflowDesigner.Core.Models.WorkflowNodeStatus.InProgress:
                        return "进行中";
                    case WorkflowDesigner.Core.Models.WorkflowNodeStatus.Completed:
                        return "已完成";
                    case WorkflowDesigner.Core.Models.WorkflowNodeStatus.Failed:
                        return "失败";
                    case WorkflowDesigner.Core.Models.WorkflowNodeStatus.Skipped:
                        return "跳过";
                    case WorkflowDesigner.Core.Models.WorkflowNodeStatus.Timeout:
                        return "超时";
                    default:
                        return enumValue.ToString();
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColorStringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString && !string.IsNullOrEmpty(colorString))
            {
                try
                {
                    // 支持 #RRGGBB 格式
                    if (colorString.StartsWith("#"))
                    {
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
                    }

                    // 支持颜色名称
                    var color = (Color)ColorConverter.ConvertFromString(colorString);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    // 如果转换失败，返回默认颜色
                    return new SolidColorBrush(Colors.Gray);
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return "#666666";
        }
    }

}
