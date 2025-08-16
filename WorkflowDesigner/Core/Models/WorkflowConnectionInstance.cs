using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WorkflowDesigner.Core.Models
{
    /// <summary>
    /// 连接实例状态
    /// </summary>
    public enum ConnectionInstanceStatus
    {
        /// <summary>
        /// 未激活
        /// </summary>
        Inactive,
        
        /// <summary>
        /// 激活
        /// </summary>
        Active,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed,
        
        /// <summary>
        /// 已跳过
        /// </summary>
        Skipped
    }

    /// <summary>
    /// 工作流连接实例
    /// </summary>
    public class WorkflowConnectionInstance : INotifyPropertyChanged
    {
        private string _id;
        private string _connectionId;
        private string _sourceNodeId;
        private string _targetNodeId;
        private string _sourcePortName;
        private string _targetPortName;
        private ConnectionInstanceStatus _status;
        private DateTime _activatedTime;
        private DateTime? _completedTime;
        private Dictionary<string, object> _data;

        public WorkflowConnectionInstance()
        {
            Id = Guid.NewGuid().ToString();
            Status = ConnectionInstanceStatus.Inactive;
            Data = new Dictionary<string, object>();
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
        /// 连接定义ID
        /// </summary>
        public string ConnectionId
        {
            get => _connectionId;
            set => SetProperty(ref _connectionId, value);
        }

        /// <summary>
        /// 源节点ID
        /// </summary>
        public string SourceNodeId
        {
            get => _sourceNodeId;
            set => SetProperty(ref _sourceNodeId, value);
        }

        /// <summary>
        /// 目标节点ID
        /// </summary>
        public string TargetNodeId
        {
            get => _targetNodeId;
            set => SetProperty(ref _targetNodeId, value);
        }

        /// <summary>
        /// 源端口名称
        /// </summary>
        public string SourcePortName
        {
            get => _sourcePortName;
            set => SetProperty(ref _sourcePortName, value);
        }

        /// <summary>
        /// 目标端口名称
        /// </summary>
        public string TargetPortName
        {
            get => _targetPortName;
            set => SetProperty(ref _targetPortName, value);
        }

        /// <summary>
        /// 连接状态
        /// </summary>
        public ConnectionInstanceStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// 激活时间
        /// </summary>
        public DateTime ActivatedTime
        {
            get => _activatedTime;
            set => SetProperty(ref _activatedTime, value);
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
        /// 连接数据
        /// </summary>
        public Dictionary<string, object> Data
        {
            get => _data;
            set => SetProperty(ref _data, value);
        }

        #endregion

        #region 方法

        /// <summary>
        /// 激活连接
        /// </summary>
        public void Activate()
        {
            if (Status == ConnectionInstanceStatus.Inactive)
            {
                Status = ConnectionInstanceStatus.Active;
                ActivatedTime = DateTime.Now;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(ActivatedTime));
            }
        }

        /// <summary>
        /// 完成连接
        /// </summary>
        public void Complete()
        {
            if (Status == ConnectionInstanceStatus.Active)
            {
                Status = ConnectionInstanceStatus.Completed;
                CompletedTime = DateTime.Now;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(CompletedTime));
            }
        }

        /// <summary>
        /// 跳过连接
        /// </summary>
        public void Skip()
        {
            if (Status == ConnectionInstanceStatus.Inactive)
            {
                Status = ConnectionInstanceStatus.Skipped;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// 获取数据值
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">数据键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数据值</returns>
        public T GetData<T>(string key, T defaultValue = default(T))
        {
            if (Data.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 设置数据值
        /// </summary>
        /// <param name="key">数据键</param>
        /// <param name="value">数据值</param>
        public void SetData(string key, object value)
        {
            Data[key] = value;
            OnPropertyChanged(nameof(Data));
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