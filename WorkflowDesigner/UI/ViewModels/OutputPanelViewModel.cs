using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Prism.Commands;
using ReactiveUI;

namespace WorkflowDesigner.UI.ViewModels
{
    public class OutputPanelViewModel : ReactiveObject
    {
        private string _filterText = "";
        private OutputMessageType _selectedFilter = OutputMessageType.All;
        private bool _autoScroll = true;

        public OutputPanelViewModel()
        {
            Messages = new ObservableCollection<OutputMessage>();
            FilteredMessages = new ObservableCollection<OutputMessage>();

            InitializeCommands();
            AddMessage("工作流设计器已启动", OutputMessageType.Info);
        }

        public ObservableCollection<OutputMessage> Messages { get; }
        public ObservableCollection<OutputMessage> FilteredMessages { get; }

        public string FilterText
        {
            get => _filterText;
            set
            {
                this.RaiseAndSetIfChanged(ref _filterText, value);
                ApplyFilter();
            }
        }

        public OutputMessageType SelectedFilter
        {
            get => _selectedFilter;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFilter, value);
                ApplyFilter();
            }
        }

        public bool AutoScroll
        {
            get => _autoScroll;
            set => this.RaiseAndSetIfChanged(ref _autoScroll, value);
        }

        public ICommand ClearMessagesCommand { get; private set; }
        public ICommand CopyAllCommand { get; private set; }
        public ICommand ExportToFileCommand { get; private set; }

        private void InitializeCommands()
        {
            ClearMessagesCommand = new DelegateCommand(ClearMessages);
            CopyAllCommand = new DelegateCommand(CopyAllMessages);
            ExportToFileCommand = new DelegateCommand(ExportToFile);
        }

        public void AddMessage(string message, OutputMessageType type = OutputMessageType.Info)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var outputMessage = new OutputMessage
                {
                    Timestamp = DateTime.Now,
                    Message = message,
                    Type = type
                };

                Messages.Add(outputMessage);

                // 限制消息数量，避免内存溢出
                while (Messages.Count > 2000)
                {
                    Messages.RemoveAt(0);
                }

                ApplyFilter();
            });
        }

        public void AddInfo(string message) => AddMessage(message, OutputMessageType.Info);
        public void AddWarning(string message) => AddMessage(message, OutputMessageType.Warning);
        public void AddError(string message) => AddMessage(message, OutputMessageType.Error);
        public void AddSuccess(string message) => AddMessage(message, OutputMessageType.Success);
        public void AddDebug(string message) => AddMessage(message, OutputMessageType.Debug);

        private void ApplyFilter()
        {
            FilteredMessages.Clear();

            var filtered = Messages.AsEnumerable();

            // 按类型过滤
            if (SelectedFilter != OutputMessageType.All)
            {
                filtered = filtered.Where(m => m.Type == SelectedFilter);
            }

            // 按文本过滤
            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                var filterLower = FilterText.ToLower();
                filtered = filtered.Where(m => m.Message.ToLower().Contains(filterLower));
            }

            foreach (var message in filtered)
            {
                FilteredMessages.Add(message);
            }
        }

        private void ClearMessages()
        {
            if (MessageBox.Show("确定要清空所有消息吗？", "确认",
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Messages.Clear();
                FilteredMessages.Clear();
                AddMessage("消息已清空", OutputMessageType.Info);
            }
        }

        private void CopyAllMessages()
        {
            try
            {
                var text = string.Join("\n", FilteredMessages.Select(m => m.FormattedMessage));
                Clipboard.SetText(text);
                AddMessage("消息已复制到剪贴板", OutputMessageType.Success);
            }
            catch (Exception ex)
            {
                AddMessage($"复制失败: {ex.Message}", OutputMessageType.Error);
            }
        }

        private void ExportToFile()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "文本文件 (*.txt)|*.txt|日志文件 (*.log)|*.log|所有文件 (*.*)|*.*",
                    Title = "导出消息到文件",
                    FileName = $"WorkflowDesigner_Log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var content = string.Join("\n", FilteredMessages.Select(m => m.FormattedMessage));
                    System.IO.File.WriteAllText(saveFileDialog.FileName, content);
                    AddMessage($"消息已导出到: {saveFileDialog.FileName}", OutputMessageType.Success);
                }
            }
            catch (Exception ex)
            {
                AddMessage($"导出失败: {ex.Message}", OutputMessageType.Error);
            }
        }
    }

    public class OutputMessage
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public OutputMessageType Type { get; set; }

        public string FormattedMessage => $"[{Timestamp:HH:mm:ss.fff}] [{Type}] {Message}";
        public string TypeIcon
        {
            get
            {
                switch (Type)
                {
                    case OutputMessageType.Info:
                        return "ℹ";
                    case OutputMessageType.Warning:
                        return "⚠";
                    case OutputMessageType.Error:
                        return "❌";
                    case OutputMessageType.Success:
                        return "✅";
                    case OutputMessageType.Debug:
                        return "🐛";
                    default:
                        return "📝";
                }
            }
        }
    }

    public enum OutputMessageType
    {
        All = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Success = 4,
        Debug = 5
    }
}