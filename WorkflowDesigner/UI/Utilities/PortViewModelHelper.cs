using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NLog;

namespace WorkflowDesigner.UI.Utilities
{
    /// <summary>
    /// 端口ViewModel辅助类 - 增强版本
    /// </summary>
    public static class PortViewModelHelper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

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
        /// 检查端口是否有效
        /// </summary>
        public static bool IsValidPort(object port)
        {
            return port is NodeInputViewModel || port is NodeOutputViewModel;
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
        /// 从可视化元素查找输出端口 - 增强版本
        /// </summary>
        public static NodeOutputViewModel FindOutputPortFromVisual(DependencyObject visual)
        {
            if (visual == null) return null;

            try
            {
                // 方法1: 直接检查是否是NodeOutputView
                if (visual is NodeOutputView outputView)
                {
                    return outputView.ViewModel as NodeOutputViewModel;
                }

                // 方法2: 检查DataContext
                if (visual is FrameworkElement element && element.DataContext is NodeOutputViewModel outputViewModel)
                {
                    return outputViewModel;
                }

                // 方法3: 检查Tag属性
                if (visual is FrameworkElement tagElement && tagElement.Tag is NodeOutputViewModel tagOutputViewModel)
                {
                    return tagOutputViewModel;
                }

                // 方法4: 向上遍历可视化树
                var current = visual;
                int depth = 0;
                const int maxDepth = 10; // 限制搜索深度

                while (current != null && depth < maxDepth)
                {
                    if (current is NodeOutputView nodeOutputView)
                    {
                        return nodeOutputView.ViewModel as NodeOutputViewModel;
                    }

                    // 检查当前元素的DataContext
                    if (current is FrameworkElement currentElement &&
                        currentElement.DataContext is NodeOutputViewModel currentOutputViewModel)
                    {
                        return currentOutputViewModel;
                    }

                    current = VisualTreeHelper.GetParent(current);
                    depth++;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "从可视化元素查找输出端口失败");
                return null;
            }
        }

        /// <summary>
        /// 从可视化元素查找输入端口 - 增强版本
        /// </summary>
        public static NodeInputViewModel FindInputPortFromVisual(DependencyObject visual)
        {
            if (visual == null) return null;

            try
            {
                // 方法1: 直接检查是否是NodeInputView
                if (visual is NodeInputView inputView)
                {
                    return inputView.ViewModel as NodeInputViewModel;
                }

                // 方法2: 检查DataContext
                if (visual is FrameworkElement element && element.DataContext is NodeInputViewModel inputViewModel)
                {
                    return inputViewModel;
                }

                // 方法3: 检查Tag属性
                if (visual is FrameworkElement tagElement && tagElement.Tag is NodeInputViewModel tagInputViewModel)
                {
                    return tagInputViewModel;
                }

                // 方法4: 向上遍历可视化树
                var current = visual;
                int depth = 0;
                const int maxDepth = 10; // 限制搜索深度

                while (current != null && depth < maxDepth)
                {
                    if (current is NodeInputView nodeInputView)
                    {
                        return nodeInputView.ViewModel as NodeInputViewModel;
                    }

                    // 检查当前元素的DataContext
                    if (current is FrameworkElement currentElement &&
                        currentElement.DataContext is NodeInputViewModel currentInputViewModel)
                    {
                        return currentInputViewModel;
                    }

                    current = VisualTreeHelper.GetParent(current);
                    depth++;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "从可视化元素查找输入端口失败");
                return null;
            }
        }

        /// <summary>
        /// 查找网络视图中的所有端口视图
        /// </summary>
        public static List<PortView> FindAllPortViews(NetworkView networkView)
        {
            var portViews = new List<PortView>();

            try
            {
                FindPortViewsRecursive(networkView, portViews);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "查找端口视图失败");
            }

            return portViews;
        }

        private static void FindPortViewsRecursive(DependencyObject parent, List<PortView> portViews)
        {
            if (parent == null) return;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is PortView portView)
                {
                    portViews.Add(portView);
                }

                FindPortViewsRecursive(child, portViews);
            }
        }

        /// <summary>
        /// 查找特定端口的视图
        /// </summary>
        public static PortView FindPortView(NetworkView networkView, object port)
        {
            try
            {
                var allPortViews = FindAllPortViews(networkView);

                foreach (var portView in allPortViews)
                {
                    if (portView.ViewModel == port)
                    {
                        return portView;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "查找特定端口视图失败");
                return null;
            }
        }

        /// <summary>
        /// 获取端口在网络视图中的位置
        /// </summary>
        public static Point? GetPortPosition(NetworkView networkView, object port)
        {
            try
            {
                var portView = FindPortView(networkView, port);
                if (portView != null)
                {
                    var transform = portView.TransformToAncestor(networkView);
                    var centerPoint = new Point(portView.ActualWidth / 2, portView.ActualHeight / 2);
                    return transform.Transform(centerPoint);
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取端口位置失败");
                return null;
            }
        }

        /// <summary>
        /// 检查两个端口是否可以连接
        /// </summary>
        public static bool CanConnect(object sourcePort, object targetPort)
        {
            // 基本类型检查
            if (!IsOutputPort(sourcePort) || !IsInputPort(targetPort))
                return false;

            var source = AsOutputPort(sourcePort);
            var target = AsInputPort(targetPort);

            // 不能连接同一个节点
            if (source?.Parent == target?.Parent)
                return false;

            // 检查目标端口是否已有连接（如果不允许多连接）
            var targetNode = GetPortParent(target);
            if (targetNode is WorkflowDesigner.Nodes.WorkflowNodeViewModel workflowNode &&
                !workflowNode.AllowMultipleInputs)
            {
                // 这里需要检查是否已有连接，需要访问网络模型
                // 具体实现需要依赖ConnectionManager
            }

            return true;
        }

        /// <summary>
        /// 在指定位置查找端口
        /// </summary>
        public static object FindPortAtPosition(NetworkView networkView, Point position, double tolerance = 20)
        {
            try
            {
                // 执行命中测试
                var hitTest = VisualTreeHelper.HitTest(networkView, position);
                if (hitTest?.VisualHit != null)
                {
                    // 首先尝试查找输出端口
                    var outputPort = FindOutputPortFromVisual(hitTest.VisualHit);
                    if (outputPort != null) return outputPort;

                    // 然后尝试查找输入端口
                    var inputPort = FindInputPortFromVisual(hitTest.VisualHit);
                    if (inputPort != null) return inputPort;
                }

                // 如果精确命中没有找到，尝试在容差范围内查找
                if (tolerance > 0)
                {
                    return FindPortNearPosition(networkView, position, tolerance);
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "在指定位置查找端口失败");
                return null;
            }
        }

        /// <summary>
        /// 在指定位置附近查找端口
        /// </summary>
        private static object FindPortNearPosition(NetworkView networkView, Point position, double tolerance)
        {
            try
            {
                var allPortViews = FindAllPortViews(networkView);

                foreach (var portView in allPortViews)
                {
                    var portPosition = GetPortPosition(networkView, portView.ViewModel);
                    if (portPosition.HasValue)
                    {
                        var distance = Math.Sqrt(
                            Math.Pow(portPosition.Value.X - position.X, 2) +
                            Math.Pow(portPosition.Value.Y - position.Y, 2));

                        if (distance <= tolerance)
                        {
                            return portView.ViewModel;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "在容差范围内查找端口失败");
                return null;
            }
        }

        /// <summary>
        /// 获取端口的边界矩形
        /// </summary>
        public static Rect? GetPortBounds(NetworkView networkView, object port)
        {
            try
            {
                var portView = FindPortView(networkView, port);
                if (portView != null)
                {
                    var transform = portView.TransformToAncestor(networkView);
                    var bounds = new Rect(0, 0, portView.ActualWidth, portView.ActualHeight);
                    return transform.TransformBounds(bounds);
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取端口边界失败");
                return null;
            }
        }

        /// <summary>
        /// 调试用：打印端口信息
        /// </summary>
        public static void DebugPrintPortInfo(object port, string prefix = "")
        {
            try
            {
                if (port == null)
                {
                    Logger.Debug($"{prefix}端口为null");
                    return;
                }

                var portType = IsInputPort(port) ? "输入" : (IsOutputPort(port) ? "输出" : "未知");
                var portName = GetPortName(port);
                var parentNode = GetPortParent(port);
                var parentNodeName = parentNode?.GetType().Name ?? "null";

                Logger.Debug($"{prefix}端口信息: 类型={portType}, 名称={portName}, 父节点={parentNodeName}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "打印端口调试信息失败");
            }
        }

        /// <summary>
        /// 验证端口视图模型的完整性
        /// </summary>
        public static bool ValidatePortViewModel(object port)
        {
            try
            {
                if (port == null) return false;

                // 检查基本类型
                if (!IsValidPort(port)) return false;

                // 检查名称
                var name = GetPortName(port);
                if (string.IsNullOrEmpty(name)) return false;

                // 检查父节点
                var parent = GetPortParent(port);
                if (parent == null) return false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "验证端口视图模型失败");
                return false;
            }
        }
    }
}