using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq; // Added for FirstOrDefault

namespace WorkflowDesigner.Core.Models
{
    /// <summary>
    /// 工作流实例状态
    /// </summary>
    public enum WorkflowInstanceStatus
    {
        /// <summary>
        /// 草稿
        /// </summary>
        Draft,
        
        /// <summary>
        /// 运行中
        /// </summary>
        Running,
        
        /// <summary>
        /// 暂停
        /// </summary>
        Paused,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 已终止
        /// </summary>
        Terminated,
        
        /// <summary>
        /// 出错
        /// </summary>
        Error
    }

    /// <summary>
    /// 工作流实例
    /// </summary>
    public class WorkflowInstance : INotifyPropertyChanged
    {
        private string _id;
        private string _workflowId;
        private string _name;
        private string _description;
        private WorkflowInstanceStatus _status;
        private DateTime _createdTime;
        private DateTime? _startTime;
        private DateTime? _completedTime;
        private string _initiatorId;
        private string _initiatorName;
        private Dictionary<string, object> _variables;
        private List<WorkflowNodeInstance> _nodeInstances;
        private List<WorkflowConnectionInstance> _connectionInstances;
        private string _currentNodeId;
        private string _errorMessage;

        public WorkflowInstance()
        {
            Id = Guid.NewGuid().ToString();
            Status = WorkflowInstanceStatus.Draft;
            CreatedTime = DateTime.Now;
            Variables = new Dictionary<string, object>();
            NodeInstances = new List<WorkflowNodeInstance>();
            ConnectionInstances = new List<WorkflowConnectionInstance>();
        }

        #region 属性

        /// <summary>
        /// 实例ID
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 工作流定义ID
        /// </summary>
        public string WorkflowId
        {
            get => _workflowId;
            set => SetProperty(ref _workflowId, value);
        }

        /// <summary>
        /// 实例名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// 实例描述
        /// </summary>
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        /// <summary>
        /// 实例状态
        /// </summary>
        public WorkflowInstanceStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime
        {
            get => _createdTime;
            set => SetProperty(ref _createdTime, value);
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        /// <summary>
        /// 完成时间
        /// </summary>
        public DateTime? CompletedTime
        {
            get => _completedTime;
            set => SetProperty(ref _completedTime, value);
        }

        /// <summary>
        /// 发起人ID
        /// </summary>
        public string InitiatorId
        {
            get => _initiatorId;
            set => SetProperty(ref _initiatorId, value);
        }

        /// <summary>
        /// 发起人姓名
        /// </summary>
        public string InitiatorName
        {
            get => _initiatorName;
            set => SetProperty(ref _initiatorName, value);
        }

        /// <summary>
        /// 工作流变量
        /// </summary>
        public Dictionary<string, object> Variables
        {
            get => _variables;
            set => SetProperty(ref _variables, value);
        }

        /// <summary>
        /// 节点实例列表
        /// </summary>
        public List<WorkflowNodeInstance> NodeInstances
        {
            get => _nodeInstances;
            set => SetProperty(ref _nodeInstances, value);
        }

        /// <summary>
        /// 连接实例列表
        /// </summary>
        public List<WorkflowConnectionInstance> ConnectionInstances
        {
            get => _connectionInstances;
            set => SetProperty(ref _connectionInstances, value);
        }

        /// <summary>
        /// 当前执行节点ID
        /// </summary>
        public string CurrentNodeId
        {
            get => _currentNodeId;
            set => SetProperty(ref _currentNodeId, value);
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 启动工作流
        /// </summary>
        public void Start()
        {
            if (Status == WorkflowInstanceStatus.Draft)
            {
                Status = WorkflowInstanceStatus.Running;
                StartTime = DateTime.Now;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StartTime));
            }
        }

        /// <summary>
        /// 暂停工作流
        /// </summary>
        public void Pause()
        {
            if (Status == WorkflowInstanceStatus.Running)
            {
                Status = WorkflowInstanceStatus.Paused;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// 恢复工作流
        /// </summary>
        public void Resume()
        {
            if (Status == WorkflowInstanceStatus.Paused)
            {
                Status = WorkflowInstanceStatus.Running;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// 终止工作流
        /// </summary>
        public void Terminate()
        {
            if (Status == WorkflowInstanceStatus.Running || Status == WorkflowInstanceStatus.Paused)
            {
                Status = WorkflowInstanceStatus.Terminated;
                CompletedTime = DateTime.Now;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CompletedTime));
            }
        }

        /// <summary>
        /// 完成工作流
        /// </summary>
        public void Complete()
        {
            if (Status == WorkflowInstanceStatus.Running)
            {
                Status = WorkflowInstanceStatus.Completed;
                CompletedTime = DateTime.Now;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CompletedTime));
            }
        }

        /// <summary>
        /// 设置错误状态
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public void SetError(string errorMessage)
        {
            Status = WorkflowInstanceStatus.Error;
            ErrorMessage = errorMessage;
            CompletedTime = DateTime.Now;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(CompletedTime));
        }

        /// <summary>
        /// 获取变量值
        /// </summary>
        /// <typeparam name="T">变量类型</typeparam>
        /// <param name="key">变量键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>变量值</returns>
        public T GetVariable<T>(string key, T defaultValue = default(T))
        {
            if (Variables.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置变量值
        /// </summary>
        /// <param name="key">变量键</param>
        /// <param name="value">变量值</param>
        public void SetVariable(string key, object value)
        {
            Variables[key] = value;
            OnPropertyChanged(nameof(Variables));
        }

        /// <summary>
        /// 获取当前节点实例
        /// </summary>
        /// <returns>当前节点实例</returns>
        public WorkflowNodeInstance GetCurrentNodeInstance()
        {
            return NodeInstances.FirstOrDefault(n => n.NodeId == CurrentNodeId);
        }

        /// <summary>
        /// 获取节点实例
        /// </summary>
        /// <param name="nodeId">节点ID</param>
        /// <returns>节点实例</returns>
        public WorkflowNodeInstance GetNodeInstance(string nodeId)
        {
            return NodeInstances.FirstOrDefault(n => n.NodeId == nodeId);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}