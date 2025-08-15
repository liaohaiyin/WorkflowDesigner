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
    /// 端口连接处理器 - 处理NetworkView中PortView的连接点功能
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
                // 订阅NetworkView的鼠标事件
                _networkView.MouseLeftButtonDown += OnNetworkViewMouseLeftButtonDown;
                _networkView.MouseMove += OnNetworkViewMouseMove;
                _networkView.MouseLeftButtonUp += OnNetworkViewMouseLeftButtonUp;

                // 创建端口高亮覆盖层
                CreatePortHighlightOverlay();

                Logger.Info("PortConnectionHandler 初始化成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "PortConnectionHandler 初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 鼠标左键按下事件处理
        /// </summary>
        private void OnNetworkViewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var hitTarget = GetHitTarget(e.GetPosition(_networkView));

                // 检查是否点击了输出端口
                if (hitTarget is PortView portView)
                {
                    Logger.Debug($"点击了端口视图，ViewModel类型: {portView.ViewModel?.GetType().Name}");

                    // 暂时禁用连接功能，直到我们解决类型转换问题
                    var outputPort = GetOutputPortFromView(portView);
                    if (outputPort != null)
                    {
                        StartConnection(outputPort, e.GetPosition(_networkView));
                        e.Handled = true;
                    }

                    e.Handled = true; // 防止事件继续传播
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理鼠标左键按下事件失败");
            }
        }

        /// <summary>
        /// 鼠标移动事件处理
        /// </summary>
        private void OnNetworkViewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_isConnecting)
                {
                    _currentMousePosition = e.GetPosition(_networkView);
                    UpdateConnectionPreview();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理鼠标移动事件失败");
            }
        }

        /// <summary>
        /// 鼠标左键释放事件处理
        /// </summary>
        private void OnNetworkViewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_isConnecting)
                {
                    var hitTarget = GetHitTarget(e.GetPosition(_networkView));

                    // 检查是否释放在输入端口上
                    if (hitTarget is PortView portView)
                    {
                        Logger.Debug($"释放在端口视图上，ViewModel类型: {portView.ViewModel?.GetType().Name}");

                        // 暂时直接取消连接
                        CancelConnection();

                        var inputPort = GetInputPortFromView(portView);
                        if (inputPort != null)
                        {
                            CompleteConnection(inputPort);
                        }
                        else
                        {
                            CancelConnection();
                        }
                    }
                    else
                    {
                        CancelConnection();
                    }

                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "处理鼠标左键释放事件失败");
                CancelConnection();
            }
        }

        /// <summary>
        /// 开始连接操作（当前暂时禁用）
        /// </summary>
        private void StartConnection(NodeOutputViewModel sourceOutput, Point startPosition)
        {
            try
            {
                _isConnecting = true;
                _sourceOutput = sourceOutput;
                _startPosition = startPosition;
                _currentMousePosition = startPosition;

                // 捕获鼠标以确保能够接收到MouseUp事件
                _networkView.CaptureMouse();

                // 创建连接预览
                CreateConnectionPreview();

                // 高亮兼容的输入端口
                ShowCompatiblePortHighlights();

                Logger.Debug($"开始从端口 '{sourceOutput.Name}' 创建连接");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "开始连接操作失败");
                CancelConnection();
            }
        }

        /// <summary>
        /// 完成连接操作（当前暂时禁用）
        /// </summary>
        private void CompleteConnection(NodeInputViewModel targetInput)
        {
            try
            {
                if (_sourceOutput?.Parent is WorkflowNodeViewModel sourceNode &&
                    targetInput?.Parent is WorkflowNodeViewModel targetNode)
                {
                    // 验证连接有效性
                    if (_connectionManager.IsValidConnection(sourceNode, targetNode, out string errorMessage))
                    {
                        // 创建连接
                        var connection = new ConnectionViewModel(_network, targetInput, _sourceOutput);
                        _network.Connections.Add(connection);

                        Logger.Info($"成功创建连接: {sourceNode.NodeName}({_sourceOutput.Name}) -> {targetNode.NodeName}({targetInput.Name})");
                    }
                    else
                    {
                        Logger.Warn($"连接验证失败: {errorMessage}");
                        ShowConnectionError(errorMessage);
                    }
                }

                CancelConnection();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "完成连接操作失败");
                CancelConnection();
            }
        }

        /// <summary>
        /// 取消连接操作
        /// </summary>
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

        /// <summary>
        /// 创建连接预览线
        /// </summary>
        private void CreateConnectionPreview()
        {
            try
            {
                if (_connectionPreview == null)
                {
                    _connectionPreview = new ConnectionPreviewControl();

                    // NetworkView不是Panel类型，需要添加到其父容器中
                    // 直接尝试添加到父容器
                    AddPreviewToParentContainer();
                }

                // 显示预览
                _connectionPreview.ShowPreview(_startPosition, _currentMousePosition);

                Logger.Debug("创建连接预览");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建连接预览失败");
            }
        }

        /// <summary>
        /// 更新连接预览
        /// </summary>
        private void UpdateConnectionPreview()
        {
            try
            {
                if (_connectionPreview != null && _isConnecting)
                {
                    // 检查当前鼠标位置是否在有效的输入端口上
                    var hitTarget = GetHitTarget(_currentMousePosition);
                    bool isValidTarget = false;

                    if (hitTarget is PortView portView)
                    {
                        // 暂时设置为无效目标，直到我们解决类型转换问题
                        // var inputPort = GetInputPortFromView(portView);
                        // if (inputPort != null)
                        // {
                        //     isValidTarget = ArePortsCompatible(_sourceOutput, inputPort);
                        // }
                        isValidTarget = false;
                    }

                    // 更新预览线
                    _connectionPreview.UpdateBezierPreview(_startPosition, _currentMousePosition);
                    _connectionPreview.SetValidationState(isValidTarget);
                }

                Logger.Debug($"更新连接预览位置: ({_currentMousePosition.X}, {_currentMousePosition.Y})");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "更新连接预览失败");
            }
        }

        /// <summary>
        /// 移除连接预览
        /// </summary>
        private void RemoveConnectionPreview()
        {
            try
            {
                if (_connectionPreview != null)
                {
                    // 隐藏预览
                    _connectionPreview.Hide();

                    // 从父容器中移除预览控件
                    var parent = _connectionPreview.Parent as System.Windows.Controls.Panel;
                    if (parent != null)
                    {
                        parent.Children.Remove(_connectionPreview);
                    }

                    _connectionPreview = null;
                }
                Logger.Debug("移除连接预览");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "移除连接预览失败");
            }
        }

        /// <summary>
        /// 获取鼠标位置处的命中目标
        /// </summary>
        private Visual GetHitTarget(Point position)
        {
            try
            {
                var hitTest = VisualTreeHelper.HitTest(_networkView, position);
                return hitTest?.VisualHit as Visual;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取命中目标失败");
                return null;
            }
        }

        /// <summary>
        /// 显示连接错误信息
        /// </summary>
        private void ShowConnectionError(string errorMessage)
        {
            try
            {
                // 这里可以显示一个临时的错误提示
                // 可以使用Popup或者ToolTip来显示错误信息
                Logger.Warn($"连接错误: {errorMessage}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "显示连接错误信息失败");
            }
        }

        /// <summary>
        /// 检查端口是否兼容
        /// </summary>
        public bool ArePortsCompatible(NodeOutputViewModel output, NodeInputViewModel input)
        {
            try
            {
                if (output?.Parent is WorkflowNodeViewModel sourceNode &&
                    input?.Parent is WorkflowNodeViewModel targetNode)
                {
                    return _connectionManager.IsValidConnection(sourceNode, targetNode, out _);
                }
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "检查端口兼容性失败");
                return false;
            }
        }

        /// <summary>
        /// 获取端口在NetworkView中的位置
        /// </summary>
        /// <param name="port">端口视图模型（NodeInputViewModel或NodeOutputViewModel）</param>
        /// <returns>端口位置</returns>
        public Point GetPortPosition(object port)
        {
            try
            {
                if (!PortViewModelHelper.IsValidPort(port))
                {
                    return new Point(0, 0);
                }

                // 这里需要找到PortView并获取其在NetworkView中的位置
                // 具体实现可能需要遍历视觉树来找到对应的PortView
                // 由于复杂性，暂时返回默认位置
                return new Point(0, 0);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取端口位置失败");
                return new Point(0, 0);
            }
        }

        /// <summary>
        /// 将预览控件添加到父容器中
        /// </summary>
        private void AddPreviewToParentContainer()
        {
            try
            {
                // 尝试找到NetworkView的父容器
                var parent = _networkView.Parent as System.Windows.Controls.Panel;
                if (parent != null)
                {
                    parent.Children.Add(_connectionPreview);
                    return;
                }

                // 如果直接父容器不是Panel，尝试通过视觉树查找
                var visualParent = VisualTreeHelper.GetParent(_networkView);
                while (visualParent != null)
                {
                    if (visualParent is System.Windows.Controls.Panel visualPanel)
                    {
                        visualPanel.Children.Add(_connectionPreview);
                        return;
                    }
                    visualParent = VisualTreeHelper.GetParent(visualParent);
                }

                Logger.Warn("无法找到合适的父容器来添加连接预览");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "添加预览到父容器失败");
            }
        }

        /// <summary>
        /// 创建端口高亮覆盖层
        /// </summary>
        private void CreatePortHighlightOverlay()
        {
            try
            {
                _portHighlightOverlay = new PortHighlightOverlay(_networkView);

                // NetworkView不是Panel类型，直接添加到父容器
                AddHighlightOverlayToParentContainer();

                Logger.Debug("端口高亮覆盖层创建成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建端口高亮覆盖层失败");
            }
        }

        /// <summary>
        /// 将高亮覆盖层添加到父容器中
        /// </summary>
        private void AddHighlightOverlayToParentContainer()
        {
            try
            {
                // 尝试找到NetworkView的父容器
                var parent = _networkView.Parent as System.Windows.Controls.Panel;
                if (parent != null)
                {
                    parent.Children.Add(_portHighlightOverlay);
                    return;
                }

                // 如果直接父容器不是Panel，尝试通过视觉树查找
                var visualParent = VisualTreeHelper.GetParent(_networkView);
                while (visualParent != null)
                {
                    if (visualParent is System.Windows.Controls.Panel visualPanel)
                    {
                        visualPanel.Children.Add(_portHighlightOverlay);
                        return;
                    }
                    visualParent = VisualTreeHelper.GetParent(visualParent);
                }

                Logger.Warn("无法找到合适的父容器来添加端口高亮覆盖层");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "添加高亮覆盖层到父容器失败");
            }
        }

        /// <summary>
        /// 显示兼容端口的高亮效果
        /// </summary>
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

        /// <summary>
        /// 清除端口高亮效果
        /// </summary>
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

        /// <summary>
        /// 检查端口视图是否为输出端口，并获取对应的NodeOutputViewModel
        /// </summary>
        private NodeOutputViewModel GetOutputPortFromView(PortView portView)
        {
            try
            {
                if (portView?.ViewModel == null) return null;

                // 由于PortView.ViewModel是PortViewModel类型，无法直接转换
                // 我们需要通过反射或其他方式来获取实际的端口对象
                var viewModel = portView.ViewModel;

                // 尝试通过反射获取实际的端口对象
                var portProperty = viewModel.GetType().GetProperty("Port");
                if (portProperty != null)
                {
                    var port = portProperty.GetValue(viewModel);
                    if (port is NodeOutputViewModel outputPort)
                    {
                        return outputPort;
                    }
                }

                // 如果反射失败，检查类型名称来判断
                var typeName = viewModel.GetType().Name;
                Logger.Debug($"端口类型: {typeName}");

                // 这里需要更深入了解NodeNetwork的结构
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取输出端口失败");
                return null;
            }
        }

        /// <summary>
        /// 检查端口视图是否为输入端口，并获取对应的NodeInputViewModel
        /// </summary>
        private NodeInputViewModel GetInputPortFromView(PortView portView)
        {
            try
            {
                if (portView?.ViewModel == null) return null;

                // 由于PortView.ViewModel是PortViewModel类型，无法直接转换
                var viewModel = portView.ViewModel;

                // 尝试通过反射获取实际的端口对象
                var portProperty = viewModel.GetType().GetProperty("Port");
                if (portProperty != null)
                {
                    var port = portProperty.GetValue(viewModel);
                    if (port is NodeInputViewModel inputPort)
                    {
                        return inputPort;
                    }
                }

                // 如果反射失败，检查类型名称来判断
                var typeName = viewModel.GetType().Name;
                Logger.Debug($"端口类型: {typeName}");

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取输入端口失败");
                return null;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_networkView != null)
                {
                    _networkView.MouseLeftButtonDown -= OnNetworkViewMouseLeftButtonDown;
                    _networkView.MouseMove -= OnNetworkViewMouseMove;
                    _networkView.MouseLeftButtonUp -= OnNetworkViewMouseLeftButtonUp;
                }

                CancelConnection();

                // 清理端口高亮覆盖层
                if (_portHighlightOverlay != null)
                {
                    var parent = _portHighlightOverlay.Parent as System.Windows.Controls.Panel;
                    if (parent != null)
                    {
                        parent.Children.Remove(_portHighlightOverlay);
                    }
                    _portHighlightOverlay = null;
                }

                Logger.Info("PortConnectionHandler 已释放");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "释放PortConnectionHandler失败");
            }
        }
    }
}