using System;
using NodeNetwork.ViewModels;

namespace WorkflowDesigner.UI.Utilities
{
    /// <summary>
    /// 端口ViewModel辅助类 - 用于处理不同类型的端口
    /// </summary>
    public static class PortViewModelHelper
    {
        /// <summary>
        /// 检查对象是否为输入端口
        /// </summary>
        public static bool IsInputPort(object port)
        {
            return port is NodeInputViewModel;
        }

        /// <summary>
        /// 检查对象是否为输出端口
        /// </summary>
        public static bool IsOutputPort(object port)
        {
            return port is NodeOutputViewModel;
        }

        /// <summary>
        /// 获取端口名称
        /// </summary>
        public static string GetPortName(object port)
        {
            switch (port)
            {
                case NodeInputViewModel input:
                    return input.Name;
                case NodeOutputViewModel output:
                    return output.Name;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 获取端口所属的节点
        /// </summary>
        public static object GetPortParent(object port)
        {
            switch (port)
            {
                case NodeInputViewModel input:
                    return input.Parent;
                case NodeOutputViewModel output:
                    return output.Parent;
                default:
                    return null;
            }
        }

        /// <summary>
        /// 将对象转换为输入端口
        /// </summary>
        public static NodeInputViewModel AsInputPort(object port)
        {
            return port as NodeInputViewModel;
        }

        /// <summary>
        /// 将对象转换为输出端口
        /// </summary>
        public static NodeOutputViewModel AsOutputPort(object port)
        {
            return port as NodeOutputViewModel;
        }

        /// <summary>
        /// 检查端口是否有效
        /// </summary>
        public static bool IsValidPort(object port)
        {
            return port is NodeInputViewModel || port is NodeOutputViewModel;
        }
    }
}