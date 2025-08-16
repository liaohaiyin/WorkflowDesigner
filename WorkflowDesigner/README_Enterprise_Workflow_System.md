# 企业级工作流管理系统

## 系统概述

这是一个基于WPF + NodeNetwork的企业级工作流管理系统，专门设计用于处理企业的日常审批流程，包括但不限于：

- **OA审批流程**：请假申请、报销审批、出差申请等
- **采购审批**：物料采购、办公用品采购、设备采购等
- **文档审批**：合同审批、文档发布、软件发布等
- **工厂流程**：生产计划审批、质量检验、设备维护等

## 核心功能

### 1. 可视化工作流设计
- **拖拽式节点编辑**：支持审批、判断、任务、通知、数据操作等节点类型
- **节点连线**：定义流程顺序和业务逻辑
- **实时预览**：所见即所得的流程设计体验

### 2. 节点类型支持

#### 开始节点 (StartNode)
- 工作流入口点
- 自动生成工作流实例
- 支持初始变量设置

#### 任务节点 (TaskNode)
- 执行具体业务任务
- 支持超时设置
- 可配置执行人和角色
- 支持输入/输出数据

#### 审批节点 (ApprovalNode)
- 支持多人审批
- 审批意见记录
- 审批历史追踪
- 支持转办和委派

#### 判断节点 (DecisionNode)
- 条件分支判断
- 支持复杂表达式
- 多路径流程控制

#### 通知节点 (NotificationNode)
- 消息推送
- 邮件通知
- 支持多种通知方式

#### 结束节点 (EndNode)
- 工作流终止点
- 自动完成流程
- 结果汇总

### 3. 工作流实例管理

#### 实例生命周期
- **草稿**：初始创建状态
- **运行中**：正在执行的流程
- **暂停**：临时停止的流程
- **已完成**：正常结束的流程
- **已终止**：手动终止的流程
- **出错**：执行异常的流程

#### 实例操作
- 启动工作流
- 暂停/恢复
- 强制终止
- 查看执行状态

### 4. 实时监控与跟踪

#### 流程图高亮
- 当前执行节点高亮显示
- 已完成节点状态标识
- 等待审批节点特殊标记
- 错误节点醒目提示

#### 进度跟踪
- 实时显示执行进度
- 节点执行时间统计
- 审批等待时间监控
- 超时预警提醒

#### 状态颜色区分
- **已完成**：绿色 (#4CAF50)
- **进行中**：蓝色 (#2196F3)
- **待处理**：灰色 (#9E9E9E)
- **失败**：红色 (#F44336)
- **等待审批**：紫色 (#9C27B0)

### 5. 审批流程管理

#### 审批操作
- **通过**：同意并继续流程
- **拒绝**：拒绝并终止流程
- **转办**：转给其他审批人
- **委派**：临时委派审批权限

#### 审批记录
- 完整的审批历史
- 审批意见和附件
- 审批时间追踪
- 审批人信息记录

### 6. 节点属性配置

#### 基础属性
- **节点名称**：业务标识
- **执行条件**：触发条件表达式
- **执行人/角色**：权限控制
- **超时时间**：执行时限设置

#### 高级属性
- **输入数据映射**：数据流转配置
- **输出数据定义**：结果数据规范
- **错误处理策略**：异常情况处理
- **并行执行设置**：并发控制

## 技术架构

### 前端技术栈
- **WPF (.NET Framework 4.8)**：用户界面框架
- **NodeNetwork (v6.0.0)**：节点网络可视化组件
- **ReactiveUI (v13.2.18)**：响应式编程框架
- **MVVM模式**：架构设计模式

### 后端服务
- **工作流执行引擎**：流程控制核心
- **状态管理服务**：实例状态维护
- **审批处理服务**：审批流程处理
- **数据持久化**：工作流数据存储

### 核心组件

#### WorkflowExecutionEngine
```csharp
public class WorkflowExecutionEngine
{
    // 工作流实例管理
    public WorkflowInstance CreateWorkflowInstance(...)
    public async Task<bool> StartWorkflowInstanceAsync(string instanceId)
    public bool PauseWorkflowInstance(string instanceId)
    
    // 节点执行
    public async Task<bool> ExecuteNodeAsync(string instanceId, string nodeId)
    
    // 审批处理
    public async Task<bool> ApproveNodeAsync(...)
    public async Task<bool> RejectNodeAsync(...)
}
```

#### WorkflowInstance
```csharp
public class WorkflowInstance : INotifyPropertyChanged
{
    // 实例状态管理
    public void Start()
    public void Pause()
    public void Resume()
    public void Terminate()
    public void Complete()
    
    // 变量管理
    public T GetVariable<T>(string key, T defaultValue = default(T))
    public void SetVariable(string key, object value)
}
```

#### WorkflowNodeInstance
```csharp
public class WorkflowNodeInstance : INotifyPropertyChanged
{
    // 节点状态管理
    public void Start()
    public void Complete()
    public void Skip()
    public void SetFailed(string errorMessage)
    
    // 审批状态
    public void WaitForApproval()
    public void Approve(string approverId, string approverName, string comment)
    public void Reject(string approverId, string approverName, string comment)
}
```

## 使用指南

### 1. 创建工作流

#### 步骤1：设计流程
1. 在WorkflowDesigner中拖拽节点到画布
2. 连接节点定义流程顺序
3. 配置节点属性和参数

#### 步骤2：配置节点
1. 双击节点打开属性面板
2. 设置节点名称、执行条件
3. 配置执行人和超时时间
4. 定义输入输出数据映射

#### 步骤3：保存工作流
1. 验证流程完整性
2. 保存工作流定义
3. 发布到生产环境

### 2. 启动工作流实例

#### 创建实例
```csharp
var engine = new WorkflowExecutionEngine();
var instance = engine.CreateWorkflowInstance(
    workflowId: "leave-approval-001",
    name: "张三请假申请",
    description: "年假申请3天",
    initiatorId: "user001",
    initiatorName: "张三",
    variables: new Dictionary<string, object>
    {
        ["LeaveType"] = "年假",
        ["LeaveDays"] = 3,
        ["StartDate"] = DateTime.Now.AddDays(1)
    }
);
```

#### 启动实例
```csharp
await engine.StartWorkflowInstanceAsync(instance.Id);
```

### 3. 监控工作流执行

#### 实时状态查看
- 在WorkflowMonitorView中查看实例状态
- 流程图实时高亮当前节点
- 节点执行状态实时更新

#### 审批操作
```csharp
// 审批通过
await engine.ApproveNodeAsync(
    instanceId: instance.Id,
    nodeId: "approval-node-001",
    approverId: "manager001",
    approverName: "李经理",
    comment: "同意请假申请"
);

// 审批拒绝
await engine.RejectNodeAsync(
    instanceId: instance.Id,
    nodeId: "approval-node-001",
    approverId: "manager001",
    approverName: "李经理",
    comment: "请假时间过长，建议调整"
);
```

### 4. 工作流控制

#### 暂停/恢复
```csharp
// 暂停工作流
engine.PauseWorkflowInstance(instance.Id);

// 恢复工作流
engine.ResumeWorkflowInstance(instance.Id);
```

#### 强制终止
```csharp
// 终止工作流
engine.TerminateWorkflowInstance(instance.Id);
```

## 配置说明

### 工作流定义配置
```xml
<WorkflowDefinition>
  <Id>leave-approval-001</Id>
  <Name>请假审批流程</Name>
  <Version>1.0</Version>
  <Category>人事管理</Category>
  
  <Nodes>
    <Node>
      <Id>start-node</Id>
      <Type>StartNode</Type>
      <Name>开始</Name>
      <Position>100,100</Position>
    </Node>
    
    <Node>
      <Id>task-node</Id>
      <Type>TaskNode</Type>
      <Name>填写申请</Name>
      <Position>250,100</Position>
      <Properties>
        <Property Name="ExecutorRole" Value="Employee"/>
        <Property Name="Timeout" Value="24:00:00"/>
      </Properties>
    </Node>
    
    <Node>
      <Id>approval-node</Id>
      <Type>ApprovalNode</Type>
      <Name>经理审批</Name>
      <Position>400,100</Position>
      <Properties>
        <Property Name="ExecutorRole" Value="Manager"/>
        <Property Name="Timeout" Value="48:00:00"/>
        <Property Name="ApprovalType" Value="Single"/>
      </Properties>
    </Node>
    
    <Node>
      <Id>end-node</Id>
      <Type>EndNode</Type>
      <Name>结束</Name>
      <Position>550,100</Position>
    </Node>
  </Nodes>
  
  <Connections>
    <Connection>
      <Id>conn-1</Id>
      <SourceNode>start-node</SourceNode>
      <TargetNode>task-node</TargetNode>
      <SourcePort>Output</SourcePort>
      <TargetPort>Input</TargetPort>
    </Connection>
    
    <Connection>
      <Id>conn-2</Id>
      <SourceNode>task-node</SourceNode>
      <TargetNode>approval-node</TargetNode>
      <SourcePort>Output</SourcePort>
      <TargetPort>Input</TargetPort>
    </Connection>
    
    <Connection>
      <Id>conn-3</Id>
      <SourceNode>approval-node</SourceNode>
      <TargetNode>end-node</TargetNode>
      <SourcePort>Output</SourcePort>
      <TargetPort>Input</TargetPort>
    </Connection>
  </Connections>
</WorkflowDefinition>
```

### 节点属性配置
```csharp
public class NodeProperties
{
    // 基础属性
    public string Name { get; set; }
    public string Description { get; set; }
    public string ExecutorRole { get; set; }
    public TimeSpan? Timeout { get; set; }
    
    // 执行条件
    public string ConditionExpression { get; set; }
    
    // 数据映射
    public Dictionary<string, string> InputMapping { get; set; }
    public Dictionary<string, string> OutputMapping { get; set; }
    
    // 审批设置
    public string ApprovalType { get; set; } // Single, Multiple, Parallel
    public List<string> ApproverRoles { get; set; }
    public bool RequireComment { get; set; }
    
    // 通知设置
    public List<string> NotificationChannels { get; set; }
    public string NotificationTemplate { get; set; }
}
```

## 扩展开发

### 自定义节点类型
```csharp
public class CustomNodeViewModel : WorkflowNodeViewModel
{
    public CustomNodeViewModel()
    {
        // 定义输入端口
        Inputs.Add(new ValueNodeInputViewModel<object> { Name = "Input" });
        
        // 定义输出端口
        Outputs.Add(new ValueNodeOutputViewModel<object> { Name = "Output" });
        
        // 自定义属性
        CustomProperty = new ReactiveProperty<string>("默认值");
    }
    
    public ReactiveProperty<string> CustomProperty { get; }
}
```

### 自定义执行逻辑
```csharp
public class CustomNodeExecutor : INodeExecutor
{
    public async Task<ExecutionResult> ExecuteAsync(WorkflowNodeInstance nodeInstance)
    {
        try
        {
            // 获取输入数据
            var inputData = nodeInstance.GetInputData<string>("InputKey");
            
            // 执行自定义逻辑
            var result = await ProcessCustomLogic(inputData);
            
            // 设置输出数据
            nodeInstance.SetOutputData("OutputKey", result);
            
            // 完成执行
            nodeInstance.Complete();
            
            return ExecutionResult.Success();
        }
        catch (Exception ex)
        {
            nodeInstance.SetFailed(ex.Message);
            return ExecutionResult.Failure(ex.Message);
        }
    }
    
    private async Task<string> ProcessCustomLogic(string input)
    {
        // 实现自定义业务逻辑
        await Task.Delay(1000); // 模拟处理时间
        return $"处理结果: {input}";
    }
}
```

## 性能优化

### 1. 数据库优化
- 使用索引优化查询性能
- 分页查询大量数据
- 定期清理历史数据

### 2. 内存管理
- 及时释放不需要的对象
- 使用对象池减少GC压力
- 限制并发实例数量

### 3. 异步处理
- 使用async/await处理耗时操作
- 后台任务处理大量数据
- 消息队列处理审批请求

## 部署说明

### 系统要求
- **操作系统**：Windows 10/11, Windows Server 2016+
- **.NET Framework**：4.8或更高版本
- **内存**：建议8GB以上
- **存储**：建议100GB以上可用空间

### 安装步骤
1. 安装.NET Framework 4.8
2. 部署应用程序文件
3. 配置数据库连接
4. 启动应用程序

### 配置检查
- 数据库连接正常
- 文件权限配置正确
- 网络端口开放
- 依赖服务运行正常

## 故障排除

### 常见问题

#### 1. 工作流无法启动
- 检查节点配置完整性
- 验证执行人权限
- 查看错误日志

#### 2. 审批流程卡住
- 检查审批人配置
- 验证审批权限
- 查看审批历史

#### 3. 性能问题
- 检查数据库性能
- 监控内存使用
- 优化查询语句

### 日志分析
```csharp
// 启用详细日志
Logger.SetLogLevel(LogLevel.Debug);

// 查看工作流执行日志
var logs = await logService.GetWorkflowLogsAsync(instanceId);
foreach (var log in logs)
{
    Console.WriteLine($"[{log.Timestamp}] {log.Level}: {log.Message}");
}
```

## 总结

这个企业级工作流管理系统提供了完整的业务流程自动化解决方案，具有以下特点：

1. **可视化设计**：直观的拖拽式流程设计
2. **灵活配置**：丰富的节点类型和属性配置
3. **实时监控**：完整的执行状态跟踪
4. **审批管理**：强大的审批流程支持
5. **扩展性强**：支持自定义节点和执行逻辑
6. **性能优化**：异步处理和资源管理

系统适用于各种企业审批场景，能够显著提高工作效率，规范业务流程，减少人为错误，为企业数字化转型提供有力支持。