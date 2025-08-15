# NodeNetwork PortView连接点功能

本功能为WorkflowDesigner的NetworkView实现了PortView连接点功能，支持通过拖拽在节点端口之间创建连接。

## 功能特性

### 1. 拖拽连接
- **从输出端口拖拽**：点击并拖拽输出端口开始创建连接
- **释放到输入端口**：将连接拖拽到兼容的输入端口完成连接
- **可视化预览**：拖拽过程中显示贝塞尔曲线连接预览
- **智能取消**：拖拽到无效位置自动取消连接

### 2. 视觉反馈
- **端口高亮**：连接过程中高亮显示兼容的输入端口
- **颜色指示**：
  - 绿色：兼容端口
  - 红色：不兼容端口
  - 金色：悬停端口
- **动画效果**：端口高亮带有呼吸动画效果
- **连接预览**：实时显示连接线预览

### 3. 连接验证
- **类型检查**：验证端口类型兼容性
- **循环检测**：防止创建循环依赖
- **重复检查**：避免重复连接
- **规则验证**：遵循工作流节点连接规则

## 核心组件

### 1. PortConnectionHandler
**位置**: `UI/Utilities/PortConnectionHandler.cs`

负责处理端口连接的核心逻辑：
- 监听NetworkView的鼠标事件
- 管理连接创建流程
- 协调预览和高亮显示
- 使用NodeInputViewModel和NodeOutputViewModel处理端口类型

```csharp
// 在WorkflowDesignerView中使用
_portConnectionHandler = new PortConnectionHandler(networkView, ViewModel.Network, _connectionManager);
```

### 1.1 PortViewModelHelper
**位置**: `UI/Utilities/PortViewModelHelper.cs`

提供端口类型处理的辅助方法：
- 统一处理NodeInputViewModel和NodeOutputViewModel
- 提供类型检查和转换功能
- 简化端口操作的复杂性

### 2. ConnectionPreviewControl
**位置**: `UI/Controls/ConnectionPreviewControl.cs`

提供连接拖拽过程中的可视化预览：
- 显示连接预览线
- 支持直线和贝塞尔曲线
- 根据连接有效性改变颜色

```csharp
// 显示连接预览
_connectionPreview.ShowPreview(startPoint, endPoint);
_connectionPreview.SetValidationState(isValid);
```

### 3. PortHighlightOverlay
**位置**: `UI/Controls/PortHighlightOverlay.cs`

管理端口高亮显示：
- 高亮兼容的输入端口
- 提供悬停效果
- 支持动画效果

```csharp
// 高亮兼容端口
_portHighlightOverlay.HighlightCompatibleInputPorts(sourceOutput, compatibilityChecker);
```

### 4. 增强的ConnectionManager
**位置**: `UI/Utilities/ConnectionManager.cs`

扩展了原有的连接管理功能：
- 添加端口级别的连接验证
- 提供端口连接状态查询
- 支持精确的端口连接操作

## 使用方法

### 基本使用
1. **初始化**：WorkflowDesignerView会自动初始化端口连接功能
2. **创建连接**：从输出端口拖拽到输入端口
3. **视觉指导**：根据颜色提示判断连接是否有效

### 程序化使用

```csharp
// 创建端口连接
bool success = _connectionManager.CreatePortConnection(sourceOutput, targetInput);

// 检查端口连接有效性
bool isValid = _connectionManager.IsValidPortConnection(sourceOutput, targetInput, out string errorMessage);

// 移除端口连接
bool removed = _connectionManager.RemovePortConnection(sourceOutput, targetInput);

// 查询端口连接状态
bool isConnected = _connectionManager.IsInputPortConnected(inputPort);

// 使用PortViewModelHelper处理端口类型
bool isInput = PortViewModelHelper.IsInputPort(port);
string portName = PortViewModelHelper.GetPortName(port);
NodeInputViewModel inputPort = PortViewModelHelper.AsInputPort(port);
```

## 连接规则

### 节点级别规则
- 开始节点不能有输入连接
- 结束节点不能有输出连接
- 不能连接节点到自身
- 不能创建循环依赖

### 端口级别规则
- 不能连接同一节点的端口
- 输出端口可连接多个输入端口
- 输入端口通常只能有一个连接（除非节点允许多输入）
- 端口类型必须兼容

## 自定义和扩展

### 自定义端口类型验证
在ConnectionManager中重写`ArePortTypesCompatible`方法：

```csharp
private bool ArePortTypesCompatible(NodeOutputViewModel output, NodeInputViewModel input)
{
    // 实现自定义的类型检查逻辑
    return output.ValueType == input.ValueType;
}
```

### 自定义视觉效果
修改ConnectionPreviewControl和PortHighlightOverlay的样式：

```csharp
// 自定义连接预览颜色
_previewLine.Stroke = new SolidColorBrush(customColor);

// 自定义高亮效果
var highlight = new Ellipse { Fill = customBrush };
```

### 添加连接事件
监听连接创建和删除事件：

```csharp
// 在PortConnectionHandler中添加事件
public event EventHandler<ConnectionCreatedEventArgs> ConnectionCreated;
public event EventHandler<ConnectionRemovedEventArgs> ConnectionRemoved;
```

## 注意事项

1. **性能考虑**：大量节点时，端口查找可能影响性能
2. **内存管理**：确保在控件卸载时清理事件订阅
3. **线程安全**：UI操作需要在主线程执行
4. **兼容性**：依赖NodeNetwork库的特定版本

## ⚠️ 当前状态警告

**重要**: 由于NodeNetwork库的类型兼容性问题，端口连接功能当前暂时禁用。

### 已知问题
- `PortView.ViewModel` 是 `PortViewModel` 类型，无法直接转换为 `NodeInputViewModel` 或 `NodeOutputViewModel`
- `NetworkView` 不是 `Panel` 类型，无法直接添加子控件

### 当前功能状态
- ✅ 基础框架和事件处理正常
- ✅ 连接管理器功能完整
- ⚠️ 实际端口连接创建暂时禁用
- ⚠️ 连接预览和高亮功能受限

详细信息请参考 `CURRENT_LIMITATIONS.md` 文件。

## 故障排除

### 常见问题
1. **连接无响应**：检查NetworkView是否正确初始化
2. **预览不显示**：确认ConnectionPreviewControl已添加到视觉树
3. **高亮失效**：验证PortHighlightOverlay的位置计算
4. **内存泄漏**：检查事件订阅的清理

### 调试建议
- 启用NLog查看详细日志
- 使用Visual Studio的WPF树查看器检查视觉树
- 在关键方法中添加断点调试

## 版本信息
- 实现版本：1.0
- 依赖NodeNetwork：6.0.0
- 目标框架：.NET Framework 4.8