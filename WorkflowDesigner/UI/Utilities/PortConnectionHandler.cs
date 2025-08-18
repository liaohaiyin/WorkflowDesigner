// 方案：基于节点区域判断，自动识别输入/输出区域
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
        var (node, isOutputArea) = GetNodeAndPortArea(position);

        if (node != null && isOutputArea)
        {
            StartConnection(node, position);
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
            var (targetNode, isInputArea) = GetNodeAndPortArea(position);

            if (targetNode != null && isInputArea && targetNode != _sourceNode)
            {
                CompleteConnection(_sourceNode, targetNode);
            }

            CancelConnection();
            e.Handled = true;
        }
    }

    // 核心方法：根据位置判断节点和端口区域
    private (WorkflowNodeViewModel node, bool isPortArea) GetNodeAndPortArea(Point position)
    {
        var node = GetNodeAtPosition(position);
        if (node == null) return (null, false);

        var nodePosition = node.Position;
        var nodeWidth = 140;  // 节点宽度
        var nodeHeight = 80;  // 节点高度
        var portAreaWidth = 20; // 端口区域宽度

        // 计算相对于节点的位置
        var relativeX = position.X - nodePosition.X;
        var relativeY = position.Y - nodePosition.Y;

        // 检查是否在节点范围内
        if (relativeX < 0 || relativeX > nodeWidth || relativeY < 0 || relativeY > nodeHeight)
            return (node, false);

        // 左侧区域 = 输入端口区域
        if (relativeX <= portAreaWidth && HasInputPorts(node))
        {
            return (node, true); // 输入区域
        }

        // 右侧区域 = 输出端口区域  
        if (relativeX >= nodeWidth - portAreaWidth && HasOutputPorts(node))
        {
            return (node, true); // 输出区域
        }

        return (node, false);
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

    private void StartConnection(WorkflowNodeViewModel sourceNode, Point startPosition)
    {
        _isConnecting = true;
        _sourceNode = sourceNode;
        _startPosition = startPosition;
        _currentMousePosition = startPosition;

        _networkView.CaptureMouse();
        CreateConnectionPreview();
    }

    private void CompleteConnection(WorkflowNodeViewModel sourceNode, WorkflowNodeViewModel targetNode)
    {
        try
        {
            // 获取第一个可用的输出和输入端口
            var sourceOutput = sourceNode.Outputs?.Items?.FirstOrDefault();
            var targetInput = targetNode.Inputs?.Items?.FirstOrDefault();

            if (sourceOutput != null && targetInput != null)
            {
                if (_connectionManager.IsValidPortConnection(sourceOutput, targetInput, out string errorMessage))
                {
                    _connectionManager.CreatePortConnection(sourceOutput, targetInput);
                }
                else
                {
                    ShowConnectionError(errorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            ShowConnectionError($"连接失败: {ex.Message}");
        }
    }

    private void CancelConnection()
    {
        _isConnecting = false;
        _sourceNode = null;
        _networkView.ReleaseMouseCapture();
        RemoveConnectionPreview();
    }

    private void CreateConnectionPreview()
    {
        if (_connectionPreview == null)
        {
            _connectionPreview = new ConnectionPreviewControl();
            var parent = _networkView.Parent as Panel;
            parent?.Children.Add(_connectionPreview);
        }

        _connectionPreview.ShowPreview(_startPosition, _currentMousePosition);
    }

    private void UpdateConnectionPreview()
    {
        _connectionPreview?.UpdatePreview(_startPosition, _currentMousePosition);

        // 检查当前位置是否有有效目标
        var (targetNode, isInputArea) = GetNodeAndPortArea(_currentMousePosition);
        bool isValidTarget = targetNode != null && isInputArea && targetNode != _sourceNode;
        _connectionPreview?.SetValidationState(isValidTarget);
    }

    private void RemoveConnectionPreview()
    {
        if (_connectionPreview != null)
        {
            var parent = _networkView.Parent as Panel;
            parent?.Children.Remove(_connectionPreview);
            _connectionPreview = null;
        }
    }

    private void ShowConnectionError(string message)
    {
        // 可以显示临时提示或记录日志
        Logger.Warn($"连接错误: {message}");
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
    }
}

// 增强的ConnectionManager，支持智能端口选择
public class SmartConnectionManager : ConnectionManager
{
    public SmartConnectionManager(NetworkViewModel network) : base(network) { }

    // 智能选择最佳端口组合
    public bool CreateSmartConnection(WorkflowNodeViewModel sourceNode, WorkflowNodeViewModel targetNode)
    {
        try
        {
            if (!IsValidConnection(sourceNode, targetNode, out string errorMessage))
            {
                return false;
            }

            // 获取最佳端口组合
            var (bestOutput, bestInput) = GetBestPortPair(sourceNode, targetNode);

            if (bestOutput != null && bestInput != null)
            {
                return CreatePortConnection(bestOutput, bestInput);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private (NodeOutputViewModel output, NodeInputViewModel input) GetBestPortPair(
        WorkflowNodeViewModel sourceNode, WorkflowNodeViewModel targetNode)
    {
        var outputs = sourceNode.Outputs?.Items?.ToList() ?? new List<NodeOutputViewModel>();
        var inputs = targetNode.Inputs?.Items?.ToList() ?? new List<NodeInputViewModel>();

        if (!outputs.Any() || !inputs.Any())
            return (null, null);

        // 优先选择未连接的端口
        var availableOutputs = outputs.Where(o => !IsOutputPortConnected(o)).ToList();
        var availableInputs = inputs.Where(i => !IsInputPortConnected(i) || targetNode.AllowMultipleInputs).ToList();

        if (!availableOutputs.Any()) availableOutputs = outputs;
        if (!availableInputs.Any()) availableInputs = inputs;

        // 智能匹配端口名称
        foreach (var output in availableOutputs)
        {
            foreach (var input in availableInputs)
            {
                if (ArePortsCompatible(output, input))
                {
                    return (output, input);
                }
            }
        }
        // 如果没有完美匹配，返回第一个可用组合
        return (availableOutputs.FirstOrDefault(), availableInputs.FirstOrDefault());
    }

    private bool ArePortsCompatible(NodeOutputViewModel output, NodeInputViewModel input)
    {
        // 检查端口名称匹配
        var outputName = output.Name?.ToLower() ?? "";
        var inputName = input.Name?.ToLower() ?? "";

        // 标准端口名称匹配
        if ((outputName.Contains("output") && inputName.Contains("input")) ||
            (outputName.Contains("完成") && inputName.Contains("输入")) ||
            (outputName.Contains("success") && inputName.Contains("input")))
        {
            return true;
        }

        // 判断节点特殊匹配
        if (output.Parent is DecisionNodeViewModel)
        {
            return outputName.Contains("是") || outputName.Contains("否") ||
                   outputName.Contains("true") || outputName.Contains("false");
        }

        return true; // 默认兼容
    }
}