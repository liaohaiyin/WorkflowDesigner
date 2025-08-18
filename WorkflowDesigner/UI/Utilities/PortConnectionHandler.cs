using NLog;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WorkflowDesigner.Nodes;
using WorkflowDesigner.UI.Controls;
using WorkflowDesigner.UI.Utilities;

public class PortConnectionHandler
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly NetworkView _networkView;
    private readonly NetworkViewModel _network;
    private readonly ConnectionManager _connectionManager;

    private bool _isConnecting = false;
    private WorkflowNodeViewModel _sourceNode = null;
    private NodeOutputViewModel _sourceOutput = null; // 添加具体的源端口引用
    private Point _startPosition;
    private Point _currentMousePosition;
    private ConnectionPreviewControl _connectionPreview = null;

    public PortConnectionHandler(NetworkView networkView, NetworkViewModel network, ConnectionManager connectionManager)
    {
        _networkView = networkView;
        _network = network;
        _connectionManager = connectionManager;
        Initialize();
    }

    private void Initialize()
    {
        _networkView.PreviewMouseLeftButtonDown += OnNetworkViewPreviewMouseLeftButtonDown;
        _networkView.PreviewMouseMove += OnNetworkViewPreviewMouseMove;
        _networkView.PreviewMouseLeftButtonUp += OnNetworkViewPreviewMouseLeftButtonUp;
    }

    private void OnNetworkViewPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var position = e.GetPosition(_networkView);
        var (node, outputPort, isOutputArea) = GetNodeAndOutputPort(position);

        if (node != null && outputPort != null && isOutputArea)
        {
            StartConnection(node, outputPort, position);
            e.Handled = true;
        }
    }

    private void OnNetworkViewPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isConnecting)
        {
            _currentMousePosition = e.GetPosition(_networkView);
            UpdateConnectionPreview();
        }
    }

    private void OnNetworkViewPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isConnecting)
        {
            var position = e.GetPosition(_networkView);
            var (targetNode, inputPort, isInputArea) = GetNodeAndInputPort(position);

            if (targetNode != null && inputPort != null && isInputArea && targetNode != _sourceNode)
            {
                CompleteConnection(_sourceOutput, inputPort);
            }

            CancelConnection();
            e.Handled = true;
        }
    }

    // 修复：更精确的端口检测，分别处理输入和输出端口
    private (WorkflowNodeViewModel node, NodeOutputViewModel outputPort, bool isPortArea) GetNodeAndOutputPort(Point position)
    {
        try
        {
            var found = PortViewModelHelper.FindPortAtPosition(_networkView, position, tolerance: 4);
            if (found is NodeOutputViewModel outVm && outVm.Parent is WorkflowNodeViewModel parentNode)
            {
                return (parentNode, outVm, true);
            }
            // 如果命中到 Input（可能用户从右向左拖），忽略
            if (found is NodeInputViewModel)
            {
                return (null, null, false);
            }
        }
        catch
        {
        }

        // 2) 兜底：保留原先的矩形判断（原实现）
        var node = GetNodeAtPosition(position);
        if (node == null) return (null, null, false);

        var nodePosition = node.Position;
        var nodeWidth = 140;
        var nodeHeight = 80;
        var portAreaWidth = 20;

        var relativeX = position.X - nodePosition.X;
        var relativeY = position.Y - nodePosition.Y;

        if (relativeX < 0 || relativeX > nodeWidth || relativeY < 0 || relativeY > nodeHeight)
            return (node, null, false);

        // 右侧区域 = 输出端口区域  
        if (relativeX >= nodeWidth - portAreaWidth && HasOutputPorts(node))
        {
            var outputPort = GetBestOutputPort(node);
            return (node, outputPort, true);
        }

        return (node, null, false);
    }

    private (WorkflowNodeViewModel node, NodeInputViewModel inputPort, bool isPortArea) GetNodeAndInputPort(Point position)
    {
        try
        {
            var found = PortViewModelHelper.FindPortAtPosition(_networkView, position, tolerance: 4);
            if (found is NodeInputViewModel inVm && inVm.Parent is WorkflowNodeViewModel parentNode)
            {
                return (parentNode, inVm, true);
            }
            if (found is NodeOutputViewModel)
            {
                return (null, null, false);
            }
        }
        catch
        {
            // 忽略，走兜底逻辑
        }

        // 2) 兜底：原始矩形判断
        var node = GetNodeAtPosition(position);
        if (node == null) return (null, null, false);

        var nodePosition = node.Position;
        var nodeWidth = 140;
        var nodeHeight = 80;
        var portAreaWidth = 20;

        var relativeX = position.X - nodePosition.X;
        var relativeY = position.Y - nodePosition.Y;

        if (relativeX < 0 || relativeX > nodeWidth || relativeY < 0 || relativeY > nodeHeight)
            return (node, null, false);

        // 左侧区域 = 输入端口区域
        if (relativeX <= portAreaWidth && HasInputPorts(node))
        {
            var inputPort = GetBestInputPort(node);
            return (node, inputPort, true);
        }

        return (node, null, false);
    }

    private WorkflowNodeViewModel GetNodeAtPosition(Point position)
    {
        try
        {
            if (_network?.Nodes == null) return null;

            return _network.Nodes.Items.OfType<WorkflowNodeViewModel>()
                .FirstOrDefault(node =>
                {
                    var nodePos = node.Position;
                    var nodeRect = new Rect(nodePos.X, nodePos.Y, 140, 80);
                    return nodeRect.Contains(position);
                });
        }
        catch
        {
            return null;
        }
    }

    private bool HasInputPorts(WorkflowNodeViewModel node)
    {
        return node.Inputs?.Items?.Any() == true;
    }

    private bool HasOutputPorts(WorkflowNodeViewModel node)
    {
        return node.Outputs?.Items?.Any() == true;
    }

    private NodeOutputViewModel GetBestOutputPort(WorkflowNodeViewModel node)
    {
        return node.Outputs?.Items?.FirstOrDefault();
    }

    private NodeInputViewModel GetBestInputPort(WorkflowNodeViewModel node)
    {
        return node.Inputs?.Items?.FirstOrDefault();
    }

    private void StartConnection(WorkflowNodeViewModel sourceNode, NodeOutputViewModel sourceOutput, Point startPosition)
    {
        _isConnecting = true;
        _sourceNode = sourceNode;
        _sourceOutput = sourceOutput; // 保存具体的输出端口引用
        _startPosition = startPosition;
        _currentMousePosition = startPosition;

        _networkView.CaptureMouse();
        CreateConnectionPreview();

        Logger.Debug($"开始连接: {sourceNode.NodeName}.{sourceOutput.Name}");
    }

    private void CompleteConnection(NodeOutputViewModel sourceOutput, NodeInputViewModel targetInput)
    {
        try
        {
            if (sourceOutput != null && targetInput != null)
            {
                if (_connectionManager.IsValidPortConnection(sourceOutput, targetInput, out string errorMessage))
                {
                    bool success = _connectionManager.CreatePortConnection(sourceOutput, targetInput);

                    if (success)
                    {
                        Logger.Info($"连接成功: {sourceOutput.Parent?.Name}.{sourceOutput.Name} -> {targetInput.Parent?.Name}.{targetInput.Name}");

                        // 关键修复：强制刷新 NetworkView 以显示新连接
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                _networkView.InvalidateVisual();
                                _networkView.UpdateLayout();

                                // 额外确保连接在视觉树中正确渲染
                                if (_networkView.Parent is Panel parentPanel)
                                {
                                    parentPanel.InvalidateVisual();
                                    parentPanel.UpdateLayout();
                                }

                                Logger.Debug("NetworkView 连接渲染已刷新");
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "刷新 NetworkView 连接显示失败");
                            }
                        }), System.Windows.Threading.DispatcherPriority.Render);
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
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "完成连接时发生异常");
            ShowConnectionError($"连接失败: {ex.Message}");
        }
    }

    private void CancelConnection()
    {
        _isConnecting = false;
        _sourceNode = null;
        _sourceOutput = null; // 清理端口引用
        _networkView.ReleaseMouseCapture();
        RemoveConnectionPreview();

        Logger.Debug("连接已取消");
    }

    private void CreateConnectionPreview()
    {
        if (_connectionPreview == null)
        {
            _connectionPreview = new ConnectionPreviewControl();
            var parent = FindOverlayContainer();
            if (parent != null)
            {
                parent.Children.Add(_connectionPreview);
                // 确保预览控件在最顶层
                Panel.SetZIndex(_connectionPreview, 1000);
            }
        }

        _connectionPreview.ShowPreview(_startPosition, _currentMousePosition);
    }

    private void UpdateConnectionPreview()
    {
        _connectionPreview?.UpdatePreview(_startPosition, _currentMousePosition);

        // 检查当前位置是否有有效目标
        var (targetNode, inputPort, isInputArea) = GetNodeAndInputPort(_currentMousePosition);
        bool isValidTarget = targetNode != null && inputPort != null && isInputArea &&
                            targetNode != _sourceNode &&
                            _connectionManager.IsValidPortConnection(_sourceOutput, inputPort, out _);

        _connectionPreview?.SetValidationState(isValidTarget);
    }

    private void RemoveConnectionPreview()
    {
        if (_connectionPreview != null)
        {
            var parent = FindOverlayContainer();
            if (parent != null && parent.Children.Contains(_connectionPreview))
            {
                parent.Children.Remove(_connectionPreview);
            }
            _connectionPreview = null;
        }
    }

    private Panel FindOverlayContainer()
    {
        // 查找适合放置预览控件的容器
        var current = _networkView.Parent;
        while (current != null)
        {
            if (current is Panel panel)
            {
                return panel;
            }
            current = LogicalTreeHelper.GetParent(current);
        }
        return _networkView.Parent as Panel;
    }

    private void ShowConnectionError(string message)
    {
        Logger.Warn($"连接错误: {message}");
        // 这里可以显示临时提示或气泡提示
    }

    public void Dispose()
    {
        CancelConnection();
        if (_networkView != null)
        {
            _networkView.PreviewMouseLeftButtonDown -= OnNetworkViewPreviewMouseLeftButtonDown;
            _networkView.PreviewMouseMove -= OnNetworkViewPreviewMouseMove;
            _networkView.PreviewMouseLeftButtonUp -= OnNetworkViewPreviewMouseLeftButtonUp;
        }
        Logger.Debug("PortConnectionHandler 已释放");
    }
}