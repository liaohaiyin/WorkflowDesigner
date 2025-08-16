using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic; // Added missing import

namespace WorkflowDesigner.Core.Models
{
    /// <summary>
    /// 审批记录
    /// </summary>
    public class ApprovalRecord : INotifyPropertyChanged
    {
        private string _id;
        private string _approverId;
        private string _approverName;
        private string _approverRole;
        private string _action;
        private string _comment;
        private DateTime _timestamp;
        private string _attachmentPath;
        private bool _isUrgent;

        public ApprovalRecord()
        {
            Id = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }

        #region 属性

        /// <summary>
        /// 记录ID
        /// </summary>
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 审批人ID
        /// </summary>
        public string ApproverId
        {
            get => _approverId;
            set => SetProperty(ref _approverId, value);
        }

        /// <summary>
        /// 审批人姓名
        /// </summary>
        public string ApproverName
        {
            get => _approverName;
            set => SetProperty(ref _approverName, value);
        }

        /// <summary>
        /// 审批人角色
        /// </summary>
        public string ApproverRole
        {
            get => _approverRole;
            set => SetProperty(ref _approverRole, value);
        }

        /// <summary>
        /// 审批动作（Approve/Reject/Forward/Return等）
        /// </summary>
        public string Action
        {
            get => _action;
            set => SetProperty(ref _action, value);
        }

        /// <summary>
        /// 审批意见
        /// </summary>
        public string Comment
        {
            get => _comment;
            set => SetProperty(ref _comment, value);
        }

        /// <summary>
        /// 审批时间
        /// </summary>
        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        /// <summary>
        /// 附件路径
        /// </summary>
        public string AttachmentPath
        {
            get => _attachmentPath;
            set => SetProperty(ref _attachmentPath, value);
        }

        /// <summary>
        /// 是否加急
        /// </summary>
        public bool IsUrgent
        {
            get => _isUrgent;
            set => SetProperty(ref _isUrgent, value);
        }

        #endregion

        #region 计算属性

        /// <summary>
        /// 审批动作显示文本
        /// </summary>
        public string ActionDisplayText
        {
            get
            {
                return Action switch
                {
                    "Approve" => "通过",
                    "Reject" => "拒绝",
                    "Forward" => "转办",
                    "Return" => "退回",
                    "Delegate" => "委派",
                    _ => Action
                };
            }
        }

        /// <summary>
        /// 审批动作颜色
        /// </summary>
        public string ActionColor
        {
            get
            {
                return Action switch
                {
                    "Approve" => "#4CAF50", // 绿色
                    "Reject" => "#F44336",   // 红色
                    "Forward" => "#2196F3",  // 蓝色
                    "Return" => "#FF9800",   // 橙色
                    "Delegate" => "#9C27B0", // 紫色
                    _ => "#757575"           // 灰色
                };
            }
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