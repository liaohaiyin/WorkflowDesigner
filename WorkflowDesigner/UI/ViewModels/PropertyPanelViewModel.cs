using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Services;
using WorkflowDesigner.Nodes;

namespace WorkflowDesigner.UI.ViewModels
{
    public class PropertyPanelViewModel : BindableBase
    {
        private WorkflowNodeViewModel _selectedNode;
        private readonly IUserService _userService;

        public PropertyPanelViewModel()
        {
            AvailableUsers = new ObservableCollection<User>
            {
                new User { Id = "1", Name = "张三", Role = "Manager" },
                new User { Id = "2", Name = "李四", Role = "Employee" }
            };
        }

        public PropertyPanelViewModel(IUserService userService) : this()
        {
            _userService = userService;
            LoadAvailableUsers();
        }

        public WorkflowNodeViewModel SelectedNode
        {
            get => _selectedNode;
            set
            {
                SetProperty(ref _selectedNode, value);
                RaisePropertyChanged(nameof(IsNodeSelected));
                RaisePropertyChanged(nameof(IsApprovalNode));
                RaisePropertyChanged(nameof(IsDecisionNode));
                RaisePropertyChanged(nameof(IsTaskNode));
                RaisePropertyChanged(nameof(IsNotificationNode));

                // 当节点改变时，通知所有属性更改
                NotifyNodePropertiesChanged();
            }
        }

        public bool IsNodeSelected => SelectedNode != null;
        public bool IsApprovalNode => SelectedNode is ApprovalNodeViewModel;
        public bool IsDecisionNode => SelectedNode is DecisionNodeViewModel;
        public bool IsTaskNode => SelectedNode is TaskNodeViewModel;
        public bool IsNotificationNode => SelectedNode is NotificationNodeViewModel;

        // 审批节点属性的安全访问器
        public string ApprovalTitle
        {
            get => (SelectedNode as ApprovalNodeViewModel)?.ApprovalTitle ?? "";
            set
            {
                if (SelectedNode is ApprovalNodeViewModel approvalNode)
                {
                    approvalNode.ApprovalTitle = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string ApprovalContent
        {
            get => (SelectedNode as ApprovalNodeViewModel)?.ApprovalContent ?? "";
            set
            {
                if (SelectedNode is ApprovalNodeViewModel approvalNode)
                {
                    approvalNode.ApprovalContent = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool RequireAllApproval
        {
            get => (SelectedNode as ApprovalNodeViewModel)?.RequireAllApproval ?? false;
            set
            {
                if (SelectedNode is ApprovalNodeViewModel approvalNode)
                {
                    approvalNode.RequireAllApproval = value;
                    RaisePropertyChanged();
                }
            }
        }

        public List<string> Approvers
        {
            get => (SelectedNode as ApprovalNodeViewModel)?.Approvers ?? new List<string>();
            set
            {
                if (SelectedNode is ApprovalNodeViewModel approvalNode)
                {
                    approvalNode.Approvers = value;
                    RaisePropertyChanged();
                }
            }
        }

        // 判断节点属性的安全访问器
        public string ConditionExpression
        {
            get => (SelectedNode as DecisionNodeViewModel)?.ConditionExpression ?? "";
            set
            {
                if (SelectedNode is DecisionNodeViewModel decisionNode)
                {
                    decisionNode.ConditionExpression = value;
                    RaisePropertyChanged();
                }
            }
        }

        // 任务节点属性的安全访问器
        public string TaskName
        {
            get => (SelectedNode as TaskNodeViewModel)?.TaskName ?? "";
            set
            {
                if (SelectedNode is TaskNodeViewModel taskNode)
                {
                    taskNode.TaskName = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string TaskType
        {
            get => (SelectedNode as TaskNodeViewModel)?.TaskType ?? "Manual";
            set
            {
                if (SelectedNode is TaskNodeViewModel taskNode)
                {
                    taskNode.TaskType = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string TaskDescription
        {
            get => (SelectedNode as TaskNodeViewModel)?.TaskDescription ?? "";
            set
            {
                if (SelectedNode is TaskNodeViewModel taskNode)
                {
                    taskNode.TaskDescription = value;
                    RaisePropertyChanged();
                }
            }
        }

        // 通知节点属性的安全访问器
        public string NotificationTitle
        {
            get => (SelectedNode as NotificationNodeViewModel)?.NotificationTitle ?? "";
            set
            {
                if (SelectedNode is NotificationNodeViewModel notificationNode)
                {
                    notificationNode.NotificationTitle = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string NotificationContent
        {
            get => (SelectedNode as NotificationNodeViewModel)?.NotificationContent ?? "";
            set
            {
                if (SelectedNode is NotificationNodeViewModel notificationNode)
                {
                    notificationNode.NotificationContent = value;
                    RaisePropertyChanged();
                }
            }
        }

        public string NotificationType
        {
            get => (SelectedNode as NotificationNodeViewModel)?.NotificationType ?? "Email";
            set
            {
                if (SelectedNode is NotificationNodeViewModel notificationNode)
                {
                    notificationNode.NotificationType = value;
                    RaisePropertyChanged();
                }
            }
        }

        public List<string> Recipients
        {
            get => (SelectedNode as NotificationNodeViewModel)?.Recipients ?? new List<string>();
            set
            {
                if (SelectedNode is NotificationNodeViewModel notificationNode)
                {
                    notificationNode.Recipients = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ObservableCollection<User> AvailableUsers { get; } = new ObservableCollection<User>();
        public ObservableCollection<string> AvailableRoles { get; } = new ObservableCollection<string>
        {
            "管理者", "员工", "HR", "财务", "管理员"
        };

        public ObservableCollection<string> TaskTypes { get; } = new ObservableCollection<string>
        {
            "手动", "自动", "脚本", "Web服务"
        };

        public ObservableCollection<string> NotificationTypes { get; } = new ObservableCollection<string>
        {
            "Email", "SMS", "系统", "即时工具"
        };

        private async void LoadAvailableUsers()
        {
            try
            {
                if (_userService != null)
                {
                    var users = await _userService.GetAllUsersAsync();
                    AvailableUsers.Clear();
                    foreach (var user in users)
                    {
                        AvailableUsers.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                // 记录错误
                System.Diagnostics.Debug.WriteLine($"加载用户列表失败: {ex.Message}");

                // 如果加载失败，确保至少有一些默认用户
                if (AvailableUsers.Count == 0)
                {
                    AvailableUsers.Add(new User { Id = "1", Name = "张三", Role = "Manager" });
                    AvailableUsers.Add(new User { Id = "2", Name = "李四", Role = "Employee" });
                }
            }
        }

        /// <summary>
        /// 当节点改变时，通知所有相关属性的更改
        /// </summary>
        private void NotifyNodePropertiesChanged()
        {
            // 审批节点属性
            RaisePropertyChanged(nameof(ApprovalTitle));
            RaisePropertyChanged(nameof(ApprovalContent));
            RaisePropertyChanged(nameof(RequireAllApproval));
            RaisePropertyChanged(nameof(Approvers));

            // 判断节点属性
            RaisePropertyChanged(nameof(ConditionExpression));

            // 任务节点属性
            RaisePropertyChanged(nameof(TaskName));
            RaisePropertyChanged(nameof(TaskType));
            RaisePropertyChanged(nameof(TaskDescription));

            // 通知节点属性
            RaisePropertyChanged(nameof(NotificationTitle));
            RaisePropertyChanged(nameof(NotificationContent));
            RaisePropertyChanged(nameof(NotificationType));
            RaisePropertyChanged(nameof(Recipients));
        }
    }
}