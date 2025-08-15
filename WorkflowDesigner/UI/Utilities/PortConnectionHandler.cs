using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using NLog;
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
                if (hitTarget is PortView portView && portView.ViewModel is NodeOutputViewModel outputPort)
                {
                    StartConnection(outputPort, e.GetPosition(_networkView));
                    e.Handled = true;
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
                    if (hitTarget is PortView portView && portView.ViewModel is NodeInputViewModel inputPort)
                    {
                        CompleteConnection(inputPort);
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
        /// 开始连接操作
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
        /// 完成连接操作
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
                    
                    // 将预览控件添加到NetworkView中
                    // 注意：这里假设NetworkView内部有一个可以添加子元素的容器
                    // 在实际实现中可能需要根据NetworkView的具体结构进行调整
                    if (_networkView is System.Windows.Controls.Panel panel)
                    {
                        panel.Children.Add(_connectionPreview);
                    }
                    else
                    {
                        // 如果NetworkView不是Panel，尝试找到其内部的容器
                        var children = System.Windows.LogicalTreeHelper.GetChildren(_networkView);
                        foreach (var child in children)
                        {
                            if (child is System.Windows.Controls.Panel childPanel)
                            {
                                childPanel.Children.Add(_connectionPreview);
                                break;
                            }
                        }
                    }
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
                    
                    if (hitTarget is PortView portView && portView.ViewModel is NodeInputViewModel inputPort)
                    {
                        isValidTarget = ArePortsCompatible(_sourceOutput, inputPort);
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
                    if (_connectionPreview.Parent is System.Windows.Controls.Panel parent)
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
        public Point GetPortPosition(NodePortViewModel port)
        {
            try
            {
                // 这里需要找到PortView并获取其在NetworkView中的位置
                // 具体实现可能需要遍历视觉树来找到对应的PortView
                return new Point(0, 0); // 临时返回
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "获取端口位置失败");
                return new Point(0, 0);
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
                
                // 将高亮覆盖层添加到NetworkView中
                if (_networkView is System.Windows.Controls.Panel panel)
                {
                    panel.Children.Add(_portHighlightOverlay);
                }
                else
                {
                    var children = System.Windows.LogicalTreeHelper.GetChildren(_networkView);
                    foreach (var child in children)
                    {
                        if (child is System.Windows.Controls.Panel childPanel)
                        {
                            childPanel.Children.Add(_portHighlightOverlay);
                            break;
                        }
                    }
                }
                
                Logger.Debug("端口高亮覆盖层创建成功");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "创建端口高亮覆盖层失败");
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
                    if (_portHighlightOverlay.Parent is System.Windows.Controls.Panel parent)
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