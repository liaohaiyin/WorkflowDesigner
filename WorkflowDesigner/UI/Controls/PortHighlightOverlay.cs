using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using WorkflowDesigner.UI.Utilities;

namespace WorkflowDesigner.UI.Controls
{
    /// <summary>
    /// 端口高亮覆盖层
    /// </summary>
    public class PortHighlightOverlay : Canvas
    {
        private readonly List<Ellipse> _highlightElements = new List<Ellipse>();
        private readonly NetworkView _networkView;
        private readonly Panel _overlayContainer;

        public PortHighlightOverlay(Panel overlayContainer, NetworkView networkView)
        {
            _overlayContainer = overlayContainer ?? throw new ArgumentNullException(nameof(overlayContainer));
            _networkView = networkView ?? throw new ArgumentNullException(nameof(networkView));
            IsHitTestVisible = false; // 不拦截鼠标事件
            Visibility = Visibility.Collapsed;

            Width = _overlayContainer.ActualWidth;
            Height = _overlayContainer.ActualHeight;
            _overlayContainer.SizeChanged += (s, e) =>
            {
                Width = e.NewSize.Width;
                Height = e.NewSize.Height;
            };
        }

        /// <summary>
        /// 高亮兼容的输入端口
        /// </summary>
        public void HighlightCompatibleInputPorts(NodeOutputViewModel sourceOutput, Func<NodeOutputViewModel, NodeInputViewModel, bool> compatibilityChecker)
        {
            try
            {
                ClearHighlights();

                if (sourceOutput?.Parent?.Parent is NetworkViewModel network)
                {
                    var compatibleInputs = new List<NodeInputViewModel>();

                    // 查找所有兼容的输入端口
                    foreach (var node in network.Nodes.Items)
                    {
                        if (node != sourceOutput.Parent) // 不包括源节点自身
                        {
                            foreach (var input in node.Inputs.Items)
                            {
                                if (compatibilityChecker(sourceOutput, input))
                                {
                                    compatibleInputs.Add(input);
                                }
                            }
                        }
                    }

                    // 为兼容的输入端口创建高亮效果
                    foreach (var input in compatibleInputs)
                    {
                        CreatePortHighlight(input, Colors.LightGreen);
                    }

                    // 显示覆盖层
                    Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"高亮兼容端口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建端口高亮效果
        /// </summary>
        private void CreatePortHighlight(object port, Color color)
        {
            try
            {
                var portPosition = GetPortPosition(port);
                if (portPosition.HasValue)
                {
                    var highlight = new Ellipse
                    {
                        Width = 20,
                        Height = 20,
                        Fill = new SolidColorBrush(Color.FromArgb(120, color.R, color.G, color.B)), // 半透明
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 2,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = color,
                            BlurRadius = 10,
                            ShadowDepth = 0,
                            Opacity = 0.8
                        }
                    };

                    // 设置位置
                    Canvas.SetLeft(highlight, portPosition.Value.X - highlight.Width / 2);
                    Canvas.SetTop(highlight, portPosition.Value.Y - highlight.Height / 2);

                    // 添加到覆盖层和跟踪列表
                    Children.Add(highlight);
                    _highlightElements.Add(highlight);

                    // 添加缩放动画效果
                    var scaleTransform = new ScaleTransform(1.0, 1.0);
                    highlight.RenderTransform = scaleTransform;
                    highlight.RenderTransformOrigin = new Point(0.5, 0.5);

                    // 简单的缩放动画
                    var animation = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 0.8,
                        To = 1.2,
                        Duration = TimeSpan.FromMilliseconds(500),
                        AutoReverse = true,
                        RepeatBehavior = System.Windows.Media.Animation.RepeatBehavior.Forever
                    };

                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"创建端口高亮失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取端口在叠加容器中的位置 - 修复版本
        /// </summary>
        private Point? GetPortPosition(object port)
        {
            try
            {
                if (!PortViewModelHelper.IsValidPort(port))
                {
                    return null;
                }

                // 使用递归方式查找PortView
                var portView = FindPortViewRecursive(_networkView, port);
                if (portView != null)
                {
                    try
                    {
                        // 获取端口在NetworkView中的位置
                        var portBounds = new Rect(0, 0, portView.ActualWidth, portView.ActualHeight);
                        var portCenter = new Point(
                            portBounds.X + portBounds.Width / 2,
                            portBounds.Y + portBounds.Height / 2
                        );

                        // 转换到overlay容器坐标系
                        var transform = portView.TransformToAncestor(_overlayContainer);
                        return transform.Transform(portCenter);
                    }
                    catch (InvalidOperationException)
                    {
                        // 如果坐标转换失败，尝试备用方法
                        return GetPortPositionAlternative(port);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"获取端口位置失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 备用的端口位置获取方法
        /// </summary>
        private Point? GetPortPositionAlternative(object port)
        {
            try
            {
                if (port is NodeInputViewModel inputPort && inputPort.Parent != null)
                {
                    // 对于输入端口，位置在节点左侧
                    var nodePosition = inputPort.Parent.Position;
                    return new Point(nodePosition.X - 10, nodePosition.Y + 25);
                }
                else if (port is NodeOutputViewModel outputPort && outputPort.Parent != null)
                {
                    // 对于输出端口，位置在节点右侧
                    var nodePosition = outputPort.Parent.Position;
                    return new Point(nodePosition.X + 150, nodePosition.Y + 55);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"备用端口位置获取失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 递归查找PortView
        /// </summary>
        private PortView FindPortViewRecursive(DependencyObject parent, object targetPort)
        {
            try
            {
                if (parent == null) return null;

                // 检查当前对象是否是目标PortView
                if (parent is PortView portView)
                {
                    if (IsMatchingPortView(portView, targetPort))
                    {
                        return portView;
                    }
                }

                // 递归搜索子元素
                var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    var result = FindPortViewRecursive(child, targetPort);
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"递归查找PortView失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 检查PortView是否匹配目标端口
        /// </summary>
        private bool IsMatchingPortView(PortView portView, object targetPort)
        {
            try
            {
                if (portView?.ViewModel == null) return false;

                // 方法1：直接比较ViewModel
                if (portView.ViewModel == targetPort) return true;

                // 方法2：通过反射获取Port属性
                var portProperty = portView.ViewModel.GetType().GetProperty("Port");
                if (portProperty != null)
                {
                    var port = portProperty.GetValue(portView.ViewModel);
                    if (port == targetPort) return true;
                }

                // 方法3：检查DataContext
                if (portView.DataContext == targetPort) return true;

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"匹配PortView失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 清除所有高亮效果
        /// </summary>
        public void ClearHighlights()
        {
            try
            {
                foreach (var highlight in _highlightElements)
                {
                    // 停止动画
                    if (highlight.RenderTransform is ScaleTransform transform)
                    {
                        transform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                        transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                    }
                    Children.Remove(highlight);
                }
                _highlightElements.Clear();
                Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清除高亮失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 高亮悬停的输入端口
        /// </summary>
        public void HighlightHoveredInputPort(NodeInputViewModel inputPort, bool isCompatible)
        {
            try
            {
                ClearHighlights();
                var color = isCompatible ? Colors.Gold : Colors.Red;
                CreatePortHighlight(inputPort, color);
                Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"高亮悬停输入端口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 高亮悬停的输出端口
        /// </summary>
        public void HighlightHoveredOutputPort(NodeOutputViewModel outputPort, bool isCompatible)
        {
            try
            {
                ClearHighlights();
                var color = isCompatible ? Colors.Gold : Colors.Red;
                CreatePortHighlight(outputPort, color);
                Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"高亮悬停输出端口失败: {ex.Message}");
            }
        }
    }
}