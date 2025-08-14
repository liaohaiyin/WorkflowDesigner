using Prism.Mvvm;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.ViewModels
{
    public class ToolboxViewModel : ReactiveObject
    {
        public ObservableCollection<ToolboxItemViewModel> ToolboxItems { get; }

        public ToolboxViewModel()
        {
            ToolboxItems = new ObservableCollection<ToolboxItemViewModel>
            {
                new ToolboxItemViewModel
                {
                    Name = "开始",
                    Description = "工作流开始节点，每个工作流必须包含一个开始节点",
                    Icon = "▶", // Unicode播放符号
                    IconColor = "#4CAF50", // 绿色
                    NodeType = typeof(StartNodeViewModel),
                    Category = "控制流"
                },
                new ToolboxItemViewModel
                {
                    Name = "结束",
                    Description = "工作流结束节点，标志工作流执行完成",
                    Icon = "⏹", // Unicode停止符号
                    IconColor = "#F44336", // 红色
                    NodeType = typeof(EndNodeViewModel),
                    Category = "控制流"
                },
                new ToolboxItemViewModel
                {
                    Name = "审批",
                    Description = "审批节点，用于人工审批环节",
                    Icon = "✓", // Unicode勾号
                    IconColor = "#2196F3", // 蓝色
                    NodeType = typeof(ApprovalNodeViewModel),
                    Category = "业务流程"
                },
                new ToolboxItemViewModel
                {
                    Name = "判断",
                    Description = "条件判断节点，根据条件分支执行",
                    Icon = "♦", // Unicode菱形
                    IconColor = "#FF9800", // 橙色
                    NodeType = typeof(DecisionNodeViewModel),
                    Category = "控制流"
                },
                new ToolboxItemViewModel
                {
                    Name = "任务",
                    Description = "任务执行节点，执行具体的业务逻辑",
                    Icon = "⚙", // Unicode齿轮
                    IconColor = "#9C27B0", // 紫色
                    NodeType = typeof(TaskNodeViewModel),
                    Category = "业务流程"
                },
                new ToolboxItemViewModel
                {
                    Name = "通知",
                    Description = "消息通知节点，发送邮件、信息等通知",
                    Icon = "📧", // Unicode邮件
                    IconColor = "#607D8B", // 蓝灰色
                    NodeType = typeof(NotificationNodeViewModel),
                    Category = "集成服务"
                }
            };
        }
    }

    public class ToolboxItemViewModel : ReactiveObject
    {
        private bool _isSelected;
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; } // 改为Unicode字符
        public string IconColor { get; set; } = "#666666"; // 图标颜色
        public Type NodeType { get; set; }
        public string Category { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set => this.RaiseAndSetIfChanged(ref _isSelected, value);
        }
    }
}

