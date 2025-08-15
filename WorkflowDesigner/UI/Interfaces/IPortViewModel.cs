using System;

namespace WorkflowDesigner.UI.Interfaces
{
    /// <summary>
    /// 端口ViewModel接口 - 用于统一处理输入和输出端口
    /// </summary>
    public interface IPortViewModel
    {
        /// <summary>
        /// 端口名称
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// 端口所属的节点
        /// </summary>
        object Parent { get; }

        /// <summary>
        /// 端口是否为输入端口
        /// </summary>
        bool IsInput { get; }

        /// <summary>
        /// 端口是否为输出端口
        /// </summary>
        bool IsOutput { get; }
    }
}