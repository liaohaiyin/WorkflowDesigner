using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WorkflowDesigner.UI.ViewModels;
using WorkflowDesigner.UI.Converters;

namespace WorkflowDesigner.UI.Views
{
    /// <summary>
    /// OutputPanelView.xaml 的交互逻辑
    /// </summary>
    public partial class OutputPanelView : UserControl
    {
        private OutputPanelViewModel ViewModel => DataContext as OutputPanelViewModel;

        public OutputPanelView()
        {
            InitializeComponent();

            // 注册转换器资源
            Resources.Add("MessageTypeToStyleConverter", new MessageTypeToStyleConverter());
            Resources.Add("BooleanToVisibilityConverter", new Converters.BooleanToVisibilityConverter());

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // 监听消息变化，实现自动滚动
            if (ViewModel != null)
            {
                ViewModel.FilteredMessages.CollectionChanged += (s, args) =>
                {
                    if (ViewModel.AutoScroll && MessagesListBox.Items.Count > 0)
                    {
                        MessagesListBox.ScrollIntoView(MessagesListBox.Items[MessagesListBox.Items.Count - 1]);
                    }
                };
            }
        }

        private void ClearButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ViewModel?.ClearMessagesCommand.Execute(null);
        }
    }

    // 消息类型到样式的转换器
    public class MessageTypeToStyleConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is OutputMessageType messageType)
            {
                switch (messageType)
                {
                    case OutputMessageType.Info:
                        return new Style(typeof(TextBlock))
                        {
                            Setters = { new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(33, 150, 243))) }
                        };

                    case OutputMessageType.Warning:
                        return new Style(typeof(TextBlock))
                        {
                            Setters = { new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(255, 152, 0))) }
                        };

                    case OutputMessageType.Error:
                        return new Style(typeof(TextBlock))
                        {
                            Setters = { new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(244, 67, 54))) }
                        };

                    case OutputMessageType.Success:
                        return new Style(typeof(TextBlock))
                        {
                            Setters = { new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(76, 175, 80))) }
                        };

                    case OutputMessageType.Debug:
                        return new Style(typeof(TextBlock))
                        {
                            Setters = { new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(156, 39, 176))) }
                        };

                    default:
                        return new Style(typeof(TextBlock))
                        {
                            Setters = { new Setter(TextBlock.ForegroundProperty, Brushes.Black) }
                        };
                }
            }
            return new Style(typeof(TextBlock));
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}
