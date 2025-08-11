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

        public PropertyPanelViewModel(IUserService userService)
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
            }
        }

        public bool IsNodeSelected => SelectedNode != null;
        public bool IsApprovalNode => SelectedNode is ApprovalNodeViewModel;
        public bool IsDecisionNode => SelectedNode is DecisionNodeViewModel;
        public bool IsTaskNode => SelectedNode is TaskNodeViewModel;
        public bool IsNotificationNode => SelectedNode is NotificationNodeViewModel;

        public ObservableCollection<User> AvailableUsers { get; } = new ObservableCollection<User>();
        public ObservableCollection<string> AvailableRoles { get; } = new ObservableCollection<string>
        {
            "Manager", "Employee", "HR", "Finance", "Admin"
        };

        public ObservableCollection<string> TaskTypes { get; } = new ObservableCollection<string>
        {
            "Manual", "Auto", "Script", "WebService"
        };

        public ObservableCollection<string> NotificationTypes { get; } = new ObservableCollection<string>
        {
            "Email", "SMS", "System", "WeChat"
        };

        private async void LoadAvailableUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                AvailableUsers.Clear();
                foreach (var user in users)
                {
                    AvailableUsers.Add(user);
                }
            }
            catch (Exception ex)
            {
                // 记录错误
                System.Diagnostics.Debug.WriteLine($"加载用户列表失败: {ex.Message}");
            }
        }
    }
}
