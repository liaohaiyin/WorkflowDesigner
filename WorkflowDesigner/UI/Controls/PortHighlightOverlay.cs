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
    /// 端口高亮覆盖层 - 在端口连接过程中高亮显示兼容的端口
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
        /// <param name="sourceOutput">源输出端口</param>
        /// <param name="compatibilityChecker">端口兼容性检查器</param>
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
        /// 高亮指定输入端口（通常用于悬停效果）
        /// </summary>
        /// <param name="inputPort">要高亮的输入端口</param>
        /// <param name="isCompatible">是否兼容</param>
        public void HighlightInputPort(NodeInputViewModel inputPort, bool isCompatible)
        {
            try
            {
                var color = isCompatible ? Colors.LightGreen : Colors.LightCoral;
                CreatePortHighlight(inputPort, color);
                Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"高亮输入端口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 高亮指定输出端口（通常用于悬停效果）
        /// </summary>
        /// <param name="outputPort">要高亮的输出端口</param>
        /// <param name="isCompatible">是否兼容</param>
        public void HighlightOutputPort(NodeOutputViewModel outputPort, bool isCompatible)
        {
            try
            {
                var color = isCompatible ? Colors.LightGreen : Colors.LightCoral;
                CreatePortHighlight(outputPort, color);
                Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"高亮输出端口失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建端口高亮效果
        /// </summary>
        /// <param name="port">要高亮的端口（NodeInputViewModel或NodeOutputViewModel）</param>
        /// <param name="color">高亮颜色</param>
        private void CreatePortHighlight(object port, Color color)
        {
            try
            {
                var portPosition = GetPortPosition(port);
                if (portPosition.HasValue)
                {
                    var highlight = new Ellipse
                    {
                        Width = 16,
                        Height = 16,
                        Fill = new SolidColorBrush(Color.FromArgb(100, color.R, color.G, color.B)), // 半透明
                        Stroke = new SolidColorBrush(color),
                        StrokeThickness = 2,
                        Effect = new System.Windows.Media.Effects.DropShadowEffect
                        {
                            Color = color,
                            BlurRadius = 8,
                            ShadowDepth = 0,
                            Opacity = 0.8
                        }
                    };

                    // 设置位置（已为叠加容器坐标）
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
        /// 获取端口在叠加容器中的位置
        /// </summary>
        /// <param name="port">端口视图模型（NodeInputViewModel或NodeOutputViewModel）</param>
        /// <returns>端口位置，如果找不到则返回null</returns>
        private Point? GetPortPosition(object port)
        {
            try
            {
                // 验证端口有效性
                if (!PortViewModelHelper.IsValidPort(port))
                {
                    return null;
                }

                // 通过视觉树查找对应的PortView控件
                var portView = FindPortView(_networkView, port);
                if (portView != null)
                {
                    // 坐标从 NetworkView 转换到 叠加容器
                    var transform = portView.TransformToAncestor(_overlayContainer);
                    var portCenter = transform.Transform(new Point(portView.ActualWidth / 2, portView.ActualHeight / 2));
                    return portCenter;
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
        /// 在视觉树中查找指定端口的PortView
        /// </summary>
        /// <param name="parent">父容器</param>
        /// <param name="port">目标端口视图模型（NodeInputViewModel或NodeOutputViewModel）</param>
        /// <returns>对应的PortView，如果找不到则返回null</returns>
        private PortView FindPortView(DependencyObject parent, object port)
        {
            try
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);

                    if (child is PortView portView && portView.ViewModel == port)
                    {
                        return portView;
                    }

                    var result = FindPortView(child, port);
                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查找PortView失败: {ex.Message}");
                return null;
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
        /// <param name="inputPort">悬停的输入端口</param>
        /// <param name="isCompatible">是否与当前连接兼容</param>
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
        /// <param name="outputPort">悬停的输出端口</param>
        /// <param name="isCompatible">是否与当前连接兼容</param>
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