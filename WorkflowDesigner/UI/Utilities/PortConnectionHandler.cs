using DynamicData;
using NLog;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WorkflowDesigner.Nodes;
using WorkflowDesigner.UI.Controls;

namespace WorkflowDesigner.UI.Utilities
{
    /// <summary>
    /// 端口连接处理器
    /// </summary>
    public class PortConnectionHandler
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly NetworkView _networkView;
        private readonly NetworkViewModel _network;
        private readonly ConnectionManager _connectionManager;

        // 连接状态
        private bool _isConnecting = false;
        private NodeOutputViewModel _sourceOutput = null;
        private Point _startPosition;
        private Point _currentMousePosition;
        private ConnectionPreviewControl _connectionPreview = null;
        private PortHighlightOverlay _portHighlightOverlay = null;
        private System.Windows.Controls.Panel _overlayContainer = null;

        public PortConnectionHandler(NetworkView networkView, NetworkViewModel network, ConnectionManager connectionManager)
        {
            _networkView = networkView ?? throw new ArgumentNullException(nameof(networkView));
            _network = network ?? throw new ArgumentNullException(nameof(network));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));

            Initialize();
        }

        private void Initialize()
        {
            try
            {
                // 设置叠加容器
                SetupOverlayContainer();

                // 创建端口高亮覆盖层
                CreatePortHighlightOverlay();

                // 订阅网络视图事件 - 使用预览事件确保能够捕获
                _networkView.PreviewMouseLeftButtonDown += OnNetworkViewPreviewMouseLeftButtonDown;
                _networkView.PreviewMouseMove += OnNetworkViewPreviewMouseMove;
                _networkView.PreviewMouseLeftButtonUp += OnNetworkViewPreviewMouseLeftButtonUp;

                // 也订阅普通事件作为备份
                _networkView.MouseLeftButtonDown += OnNetworkViewMouseLeftButtonDown;
                _networkView.MouseMove += OnNetworkViewMouseMove;
                _networkView.MouseLeftButtonUp += OnNetworkViewMouseLeftButtonUp;

                Logger.Info("PortConnectionHandler 初始化成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "PortConnectionHandler 初始化失败");
                throw;
            }
        }

        private void SetupOverlayContainer()
        {
            try
            {
                // 查找合适的叠加容器
                _overlayContainer = FindOverlayContainer();

                if (_overlayContainer == null)
                {
                    // 创建一个Grid作为叠加容器
                    var parent = _networkView.Parent as System.Windows.Controls.Panel;
                    if (parent != null)
                    {
                        _overlayContainer = parent;
                    }
                    else
                    {
                        // 最后的备选方案：创建一个包装容器
                        var grid = new System.Windows.Controls.Grid();
                        var originalParent = _networkView.Parent as System.Windows.Controls.Panel;
                        if (originalParent != null)
                        {
                            originalParent.Children.Remove(_networkView);
                            originalParent.Children.Add(grid);
                            grid.Children.Add(_networkView);
                            _overlayContainer = grid;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "设置叠加容器失败");
            }
        }

        #region 鼠标事件处理 - 预览事件

        private void OnNetworkViewPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var hitElement = e.OriginalSource as DependencyObject;

                // 尝试获取输出端口
                var outputPort = GetOutputPortFromHit(hitElement);
                if (outputPort != null)
                {
                    Logger.Debug($"检测到输出端口点击: {outputPort.Name}");
                    StartConnection(outputPort, e.GetPosition(_networkView));
                    e.Handled = true;
                    return;
                }

                Logger.Debug($"预览事件未检测到端口，命中元素: {hitElement?.GetType().Name}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理预览鼠标左键按下事件失败");
            }
        }

        private void OnNetworkViewPreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isConnecting)
                {
                    _currentMousePosition = e.GetPosition(_networkView);
                    UpdateConnectionPreview();

                    // 检查悬停的输入端口
                    var hitElement = e.OriginalSource as DependencyObject;
                    var inputPort = GetInputPortFromHit(hitElement);
                    UpdatePortHighlight(inputPort);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理预览鼠标移动事件失败");
            }
        }

        private void OnNetworkViewPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isConnecting)
                {
                    var hitElement = e.OriginalSource as DependencyObject;
                    var inputPort = GetInputPortFromHit(hitElement);

                    if (inputPort != null)
                    {
                        Logger.Debug($"尝试连接到输入端口: {inputPort.Name}");
                        CompleteConnection(inputPort);
                    }
                    else
                    {
                        Logger.Debug("未检测到有效的输入端口，取消连接");
                        CancelConnection();
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理预览鼠标左键释放事件失败");
                CancelConnection();
            }
        }

        #endregion

        #region 鼠标事件处理 - 普通事件（备份）

        private void OnNetworkViewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 如果预览事件没有处理，这里作为备份
            if (!e.Handled)
            {
                OnNetworkViewPreviewMouseLeftButtonDown(sender, e);
            }
        }

        private void OnNetworkViewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isConnecting) return;
            OnNetworkViewPreviewMouseMove(sender, e);
        }

        private void OnNetworkViewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!e.Handled && _isConnecting)
            {
                OnNetworkViewPreviewMouseLeftButtonUp(sender, e);
            }
        }

        #endregion

        #region 端口查找逻辑 - 核心修复

        private NodeOutputViewModel GetOutputPortFromHit(DependencyObject hitElement)
        {
            if (hitElement == null) return null;

            try
            {
                // 方法1: 直接检查是否是NodeOutputView
                if (hitElement is NodeOutputView outputView)
                {
                    Logger.Debug("直接命中NodeOutputView");
                    return outputView.ViewModel as NodeOutputViewModel;
                }

                // 方法2: 向上遍历可视化树查找NodeOutputView
                var current = hitElement;
                while (current != null)
                {
                    if (current is NodeOutputView nodeOutputView)
                    {
                        Logger.Debug($"在可视化树中找到NodeOutputView，深度: {GetDepth(hitElement, current)}");
                        return nodeOutputView.ViewModel as NodeOutputViewModel;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }

                // 方法3: 检查DataContext
                if (hitElement is FrameworkElement element && element.DataContext is NodeOutputViewModel outputViewModel)
                {
                    Logger.Debug("通过DataContext找到NodeOutputViewModel");
                    return outputViewModel;
                }

                // 方法4: 通过Tag属性查找
                if (hitElement is FrameworkElement tagElement && tagElement.Tag is NodeOutputViewModel tagOutputViewModel)
                {
                    Logger.Debug("通过Tag找到NodeOutputViewModel");
                    return tagOutputViewModel;
                }

                Logger.Debug($"未能从命中元素找到输出端口: {hitElement.GetType().Name}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "从命中元素获取输出端口失败");
                return null;
            }
        }

        private NodeInputViewModel GetInputPortFromHit(DependencyObject hitElement)
        {
            if (hitElement == null) return null;

            try
            {
                // 方法1: 直接检查是否是NodeInputView
                if (hitElement is NodeInputView inputView)
                {
                    Logger.Debug("直接命中NodeInputView");
                    return inputView.ViewModel as NodeInputViewModel;
                }

                // 方法2: 向上遍历可视化树查找NodeInputView
                var current = hitElement;
                while (current != null)
                {
                    if (current is NodeInputView nodeInputView)
                    {
                        Logger.Debug($"在可视化树中找到NodeInputView，深度: {GetDepth(hitElement, current)}");
                        return nodeInputView.ViewModel as NodeInputViewModel;
                    }
                    current = VisualTreeHelper.GetParent(current);
                }

                // 方法3: 检查DataContext
                if (hitElement is FrameworkElement element && element.DataContext is NodeInputViewModel inputViewModel)
                {
                    Logger.Debug("通过DataContext找到NodeInputViewModel");
                    return inputViewModel;
                }

                // 方法4: 通过Tag属性查找
                if (hitElement is FrameworkElement tagElement && tagElement.Tag is NodeInputViewModel tagInputViewModel)
                {
                    Logger.Debug("通过Tag找到NodeInputViewModel");
                    return tagInputViewModel;
                }

                Logger.Debug($"未能从命中元素找到输入端口: {hitElement.GetType().Name}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "从命中元素获取输入端口失败");
                return null;
            }
        }

        private int GetDepth(DependencyObject child, DependencyObject parent)
        {
            int depth = 0;
            var current = child;
            while (current != null && current != parent)
            {
                current = VisualTreeHelper.GetParent(current);
                depth++;
            }
            return depth;
        }

        #endregion

        #region 连接操作

        private void StartConnection(NodeOutputViewModel sourceOutput, Point startPosition)
        {
            try
            {
                _isConnecting = true;
                _sourceOutput = sourceOutput;
                _startPosition = startPosition;
                _currentMousePosition = startPosition;

                // 捕获鼠标
                _networkView.CaptureMouse();

                // 创建连接预览
                CreateConnectionPreview();

                // 高亮兼容的输入端口
                ShowCompatiblePortHighlights();

                Logger.Info($"开始连接操作: {sourceOutput.Parent?.Name}({sourceOutput.Name})");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "开始连接操作失败");
                CancelConnection();
            }
        }

        private void CompleteConnection(NodeInputViewModel targetInput)
        {
            try
            {
                if (_sourceOutput?.Parent is WorkflowNodeViewModel sourceNode &&
                    targetInput?.Parent is WorkflowNodeViewModel targetNode)
                {
                    // 验证连接有效性
                    if (_connectionManager.IsValidPortConnection(_sourceOutput, targetInput, out string errorMessage))
                    {
                        // 创建连接
                        if (_connectionManager.CreatePortConnection(_sourceOutput, targetInput))
                        {
                            Logger.Info($"成功创建连接: {sourceNode.NodeName}({_sourceOutput.Name}) -> {targetNode.NodeName}({targetInput.Name})");
                        }
                        else
                        {
                            Logger.Warn("连接创建失败");
                            ShowConnectionError("连接创建失败");
                        }
                    }
                    else
                    {
                        Logger.Warn($"连接验证失败: {errorMessage}");
                        ShowConnectionError(errorMessage);
                    }
                }
                else
                {
                    Logger.Warn("端口所属节点无效");
                    ShowConnectionError("端口所属节点无效");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "完成连接操作失败");
                ShowConnectionError($"连接失败: {ex.Message}");
            }
            finally
            {
                CancelConnection();
            }
        }

        private void CancelConnection()
        {
            try
            {
                _isConnecting = false;
                _sourceOutput = null;

                // 释放鼠标捕获
                _networkView.ReleaseMouseCapture();

                // 移除连接预览
                RemoveConnectionPreview();

                // 清除端口高亮
                ClearPortHighlights();

                Logger.Debug("连接操作已取消");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "取消连接操作失败");
            }
        }

        #endregion

        #region 连接预览

        private void CreateConnectionPreview()
        {
            try
            {
                if (_connectionPreview == null && _overlayContainer != null)
                {
                    _connectionPreview = new ConnectionPreviewControl();

                    // 设置预览控件尺寸
                    _connectionPreview.Width = _overlayContainer.ActualWidth;
                    _connectionPreview.Height = _overlayContainer.ActualHeight;

                    // 添加到叠加容器
                    _overlayContainer.Children.Add(_connectionPreview);

                    // 监听容器尺寸变化
                    _overlayContainer.SizeChanged += OnOverlayContainerSizeChanged;
                }

                if (_connectionPreview != null)
                {
                    // 坐标转换并显示预览
                    var start = MapFromNetworkToOverlay(_startPosition);
                    var current = MapFromNetworkToOverlay(_currentMousePosition);
                    _connectionPreview.ShowPreview(start, current);
                }

                Logger.Debug("连接预览创建成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建连接预览失败");
            }
        }

        private void UpdateConnectionPreview()
        {
            try
            {
                if (_connectionPreview != null && _isConnecting)
                {
                    // 检查当前位置是否有有效的输入端口
                    var hitElement = GetElementUnderMouse();
                    var inputPort = GetInputPortFromHit(hitElement);
                    bool isValidTarget = inputPort != null && ArePortsCompatible(_sourceOutput, inputPort);

                    // 更新预览线
                    var start = MapFromNetworkToOverlay(_startPosition);
                    var current = MapFromNetworkToOverlay(_currentMousePosition);

                    _connectionPreview.UpdateBezierPreview(start, current);
                    _connectionPreview.SetValidationState(isValidTarget);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新连接预览失败");
            }
        }

        private void RemoveConnectionPreview()
        {
            try
            {
                if (_connectionPreview != null)
                {
                    _connectionPreview.Hide();

                    if (_overlayContainer != null)
                    {
                        _overlayContainer.Children.Remove(_connectionPreview);
                        _overlayContainer.SizeChanged -= OnOverlayContainerSizeChanged;
                    }

                    _connectionPreview = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "移除连接预览失败");
            }
        }

        private void OnOverlayContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_connectionPreview != null)
            {
                _connectionPreview.Width = e.NewSize.Width;
                _connectionPreview.Height = e.NewSize.Height;
            }
        }

        #endregion

        #region 端口高亮

        private void CreatePortHighlightOverlay()
        {
            try
            {
                if (_overlayContainer != null)
                {
                    _portHighlightOverlay = new PortHighlightOverlay(_overlayContainer, _networkView);
                    _overlayContainer.Children.Add(_portHighlightOverlay);
                    Logger.Debug("端口高亮覆盖层创建成功");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建端口高亮覆盖层失败");
            }
        }

        private void ShowCompatiblePortHighlights()
        {
            try
            {
                if (_portHighlightOverlay != null && _sourceOutput != null)
                {
                    _portHighlightOverlay.HighlightCompatibleInputPorts(_sourceOutput, ArePortsCompatible);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "显示兼容端口高亮失败");
            }
        }

        private void UpdatePortHighlight(NodeInputViewModel inputPort)
        {
            try
            {
                if (_portHighlightOverlay != null && inputPort != null)
                {
                    bool isCompatible = ArePortsCompatible(_sourceOutput, inputPort);
                    _portHighlightOverlay.HighlightHoveredInputPort(inputPort, isCompatible);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新端口高亮失败");
            }
        }

        private void ClearPortHighlights()
        {
            try
            {
                _portHighlightOverlay?.ClearHighlights();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "清除端口高亮失败");
            }
        }

        #endregion

        #region 辅助方法

        private System.Windows.Controls.Panel FindOverlayContainer()
        {
            // 查找NetworkView的父容器
            var parent = _networkView.Parent as System.Windows.Controls.Panel;
            if (parent != null) return parent;

            // 向上查找Grid或其他Panel
            var visualParent = VisualTreeHelper.GetParent(_networkView);
            while (visualParent != null)
            {
                if (visualParent is System.Windows.Controls.Panel panel)
                {
                    return panel;
                }
                visualParent = VisualTreeHelper.GetParent(visualParent);
            }

            return null;
        }

        private Point MapFromNetworkToOverlay(Point point)
        {
            try
            {
                if (_overlayContainer == null) return point;

                var transform = _networkView.TransformToAncestor(_overlayContainer);
                return transform.Transform(point);
            }
            catch
            {
                return point;
            }
        }

        private DependencyObject GetElementUnderMouse()
        {
            try
            {
                var hitTest = VisualTreeHelper.HitTest(_networkView, _currentMousePosition);
                return hitTest?.VisualHit;
            }
            catch
            {
                return null;
            }
        }

        private bool ArePortsCompatible(NodeOutputViewModel output, NodeInputViewModel input)
        {
            try
            {
                return _connectionManager.IsValidPortConnection(output, input, out _);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "检查端口兼容性失败");
                return false;
            }
        }

        private void ShowConnectionError(string errorMessage)
        {
            try
            {
                Logger.Warn($"连接错误: {errorMessage}");
                // 这里可以添加用户提示，比如状态栏消息或临时通知
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "显示连接错误信息失败");
            }
        }

        #endregion

        #region 释放资源

        public void Dispose()
        {
            try
            {
                // 取消当前连接
                CancelConnection();

                // 移除事件订阅
                if (_networkView != null)
                {
                    _networkView.PreviewMouseLeftButtonDown -= OnNetworkViewPreviewMouseLeftButtonDown;
                    _networkView.PreviewMouseMove -= OnNetworkViewPreviewMouseMove;
                    _networkView.PreviewMouseLeftButtonUp -= OnNetworkViewPreviewMouseLeftButtonUp;

                    _networkView.MouseLeftButtonDown -= OnNetworkViewMouseLeftButtonDown;
                    _networkView.MouseMove -= OnNetworkViewMouseMove;
                    _networkView.MouseLeftButtonUp -= OnNetworkViewMouseLeftButtonUp;
                }

                // 清理覆盖层
                if (_portHighlightOverlay != null && _overlayContainer != null)
                {
                    _overlayContainer.Children.Remove(_portHighlightOverlay);
                    _portHighlightOverlay = null;
                }

                Logger.Info("PortConnectionHandler 已释放");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "释放PortConnectionHandler失败");
            }
        }

        #endregion
    }
}