using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkflowDesigner.Core.Models
{
    /// <summary>
    /// 节点实例状态
    /// </summary>
    public enum NodeInstanceStatus
    {
        /// <summary>
        /// 待处理
        /// </summary>
        Pending,
        
        /// <summary>
        /// 进行中
        /// </summary>
        Running,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 已跳过
        /// </summary>
        Skipped,
        
        /// <summary>
        /// 失败
        /// </summary>
        Failed,
        
        /// <summary>
        /// 等待审批
        /// </summary>
        WaitingForApproval,
        
        /// <summary>
        /// 审批通过
        /// </summary>
        Approved,
        
        /// <summary>
        /// 审批拒绝
        /// </summary>
        Rejected
    }

    /// <summary>
    /// 工作流节点实例
    /// </summary>
    public class WorkflowNodeInstance : INotifyPropertyChanged
    {
        private string _id;
        private string _nodeId;
        private string _nodeName;
        private string _nodeType;
        private NodeInstanceStatus _status;
        private DateTime _createdTime;
        private DateTime? _startTime;
        private DateTime? _completedTime;
        private string _executorId;
        private string _executorName;
        private string _executorRole;
        private Dictionary<string, object> _inputData;
        private Dictionary<string, object> _outputData;
        private string _errorMessage;
        private List<string> _approverIds;
        private List<ApprovalRecord> _approvalRecords;
        private TimeSpan? _timeoutDuration;
        private bool _isTimeout;

        public WorkflowNodeInstance()
        {
            Id = Guid.NewGuid().ToString();
            Status = NodeInstanceStatus.Pending;
            CreatedTime = DateTime.Now;
            InputData = new Dictionary<string, object>();
            OutputData = new Dictionary<string, object>();
            ApproverIds = new List<string>();
            ApprovalRecords = new List<ApprovalRecord>();
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
        /// 节点ID
        /// </summary>
        public string NodeId
        {
            get => _nodeId;
            set => SetProperty(ref _nodeId, value);
        }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string NodeName
        {
            get => _nodeName;
            set => SetProperty(ref _nodeName, value);
        }

        /// <summary>
        /// 节点类型
        /// </summary>
        public string NodeType
        {
            get => _nodeType;
            set => SetProperty(ref _nodeType, value);
        }

        /// <summary>
        /// 节点状态
        /// </summary>
        public NodeInstanceStatus Status
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
        /// 执行人ID
        /// </summary>
        public string ExecutorId
        {
            get => _executorId;
            set => SetProperty(ref _executorId, value);
        }

        /// <summary>
        /// 执行人姓名
        /// </summary>
        public string ExecutorName
        {
            get => _executorName;
            set => SetProperty(ref _executorName, value);
        }

        /// <summary>
        /// 执行人角色
        /// </summary>
        public string ExecutorRole
        {
            get => _executorRole;
            set => SetProperty(ref _executorRole, value);
        }

        /// <summary>
        /// 输入数据
        /// </summary>
        public Dictionary<string, object> InputData
        {
            get => _inputData;
            set => SetProperty(ref _inputData, value);
        }

        /// <summary>
        /// 输出数据
        /// </summary>
        public Dictionary<string, object> OutputData
        {
            get => _outputData;
            set => SetProperty(ref _outputData, value);
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// 审批人ID列表
        /// </summary>
        public List<string> ApproverIds
        {
            get => _approverIds;
            set => SetProperty(ref _approverIds, value);
        }

        /// <summary>
        /// 审批记录
        /// </summary>
        public List<ApprovalRecord> ApprovalRecords
        {
            get => _approvalRecords;
            set => SetProperty(ref _approvalRecords, value);
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        public TimeSpan? TimeoutDuration
        {
            get => _timeoutDuration;
            set => SetProperty(ref _timeoutDuration, value);
        }

        /// <summary>
        /// 是否超时
        /// </summary>
        public bool IsTimeout
        {
            get => _isTimeout;
            set => SetProperty(ref _isTimeout, value);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 开始执行节点
        /// </summary>
        public void Start()
        {
            if (Status == NodeInstanceStatus.Pending)
            {
                Status = NodeInstanceStatus.Running;
                StartTime = DateTime.Now;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StartTime));
            }
        }

        /// <summary>
        /// 完成节点执行
        /// </summary>
        public void Complete()
        {
            if (Status == NodeInstanceStatus.Running)
            {
                Status = NodeInstanceStatus.Completed;
                CompletedTime = DateTime.Now;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CompletedTime));
            }
        }

        /// <summary>
        /// 跳过节点
        /// </summary>
        public void Skip()
        {
            if (Status == NodeInstanceStatus.Pending)
            {
                Status = NodeInstanceStatus.Skipped;
                CompletedTime = DateTime.Now;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CompletedTime));
            }
        }

        /// <summary>
        /// 设置节点失败
        /// </summary>
        /// <param name="errorMessage">错误信息</param>
        public void SetFailed(string errorMessage)
        {
            Status = NodeInstanceStatus.Failed;
            ErrorMessage = errorMessage;
            CompletedTime = DateTime.Now;
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(ErrorMessage));
            OnPropertyChanged(nameof(CompletedTime));
        }

        /// <summary>
        /// 等待审批
        /// </summary>
        public void WaitForApproval()
        {
            if (Status == NodeInstanceStatus.Running)
            {
                Status = NodeInstanceStatus.WaitingForApproval;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// 审批通过
        /// </summary>
        /// <param name="approverId">审批人ID</param>
        /// <param name="approverName">审批人姓名</param>
        /// <param name="comment">审批意见</param>
        public void Approve(string approverId, string approverName, string comment = "")
        {
            if (Status == NodeInstanceStatus.WaitingForApproval)
            {
                Status = NodeInstanceStatus.Approved;
                CompletedTime = DateTime.Now;
                
                // 添加审批记录
                var record = new ApprovalRecord
                {
                    ApproverId = approverId,
                    ApproverName = approverName,
                    Action = "Approve",
                    Comment = comment,
                    Timestamp = DateTime.Now
                };
                ApprovalRecords.Add(record);
                
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CompletedTime));
                OnPropertyChanged(nameof(ApprovalRecords));
            }
        }

        /// <summary>
        /// 审批拒绝
        /// </summary>
        /// <param name="approverId">审批人ID</param>
        /// <param name="approverName">审批人姓名</param>
        /// <param name="comment">拒绝原因</param>
        public void Reject(string approverId, string approverName, string comment = "")
        {
            if (Status == NodeInstanceStatus.WaitingForApproval)
            {
                Status = NodeInstanceStatus.Rejected;
                CompletedTime = DateTime.Now;
                
                // 添加审批记录
                var record = new ApprovalRecord
                {
                    ApproverId = approverId,
                    ApproverName = approverName,
                    Action = "Reject",
                    Comment = comment,
                    Timestamp = DateTime.Now
                };
                ApprovalRecords.Add(record);
                
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CompletedTime));
                OnPropertyChanged(nameof(ApprovalRecords));
            }
        }

        /// <summary>
        /// 检查是否超时
        /// </summary>
        public void CheckTimeout()
        {
            if (TimeoutDuration.HasValue && StartTime.HasValue && Status == NodeInstanceStatus.Running)
            {
                var elapsed = DateTime.Now - StartTime.Value;
                if (elapsed > TimeoutDuration.Value)
                {
                    IsTimeout = true;
                    SetFailed("节点执行超时");
                    OnPropertyChanged(nameof(IsTimeout));
                }
            }
        }

        /// <summary>
        /// 获取输入数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数据值</returns>
        public T GetInputData<T>(string key, T defaultValue = default(T))
        {
            if (InputData.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置输入数据
        /// </summary>
        /// <param name="key">数据键</param>
        /// <param name="value">数据值</param>
        public void SetInputData(string key, object value)
        {
            InputData[key] = value;
            OnPropertyChanged(nameof(InputData));
        }

        /// <summary>
        /// 获取输出数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数据值</returns>
        public T GetOutputData<T>(string key, T defaultValue = default(T))
        {
            if (OutputData.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置输出数据
        /// </summary>
        /// <param name="key">数据键</param>
        /// <param name="value">数据值</param>
        public void SetOutputData(string key, object value)
        {
            OutputData[key] = value;
            OnPropertyChanged(nameof(OutputData));
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