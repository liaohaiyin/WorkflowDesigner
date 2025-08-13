using System;
using System.Threading.Tasks;
using System.Linq;
using WorkflowDesigner.Core.Interfaces;
using WorkflowDesigner.Core.Models;
using WorkflowDesigner.UI.ViewModels;
using NLog;

namespace WorkflowDesigner.Core.Services
{
    /// <summary>
    /// 工作流加载器 - 负责从数据库加载工作流并显示在设计器中
    /// </summary>
    public class WorkflowLoader
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IWorkflowRepository _workflowRepository;

        public WorkflowLoader(IWorkflowRepository workflowRepository)
        {
            _workflowRepository = workflowRepository ?? throw new ArgumentNullException(nameof(workflowRepository));
        }

        /// <summary>
        /// 加载第一个工作流定义并在设计器中显示
        /// </summary>
        /// <param name="designerViewModel">工作流设计器视图模型</param>
        /// <returns>是否成功加载</returns>
        public async Task<bool> LoadFirstWorkflowAsync(WorkflowDesignerViewModel designerViewModel)
        {
            try
            {
                Logger.Info("开始加载第一个工作流定义");

                // 获取所有工作流定义
                var workflowDefinitions = await _workflowRepository.GetAllWorkflowDefinitionsAsync();

                if (workflowDefinitions == null || !workflowDefinitions.Any())
                {
                    Logger.Warn("数据库中没有找到工作流定义");
                    return false;
                }

                // 获取第一个工作流定义
                var firstWorkflow = workflowDefinitions.First();
                Logger.Info($"找到工作流: {firstWorkflow.Name} (ID: {firstWorkflow.Id})");

                // 在设计器中加载工作流
                await designerViewModel.LoadWorkflowAsync(firstWorkflow);

                Logger.Info($"工作流 '{firstWorkflow.Name}' 加载成功");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "加载第一个工作流失败");
                throw new ApplicationException($"加载工作流失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载指定ID的工作流定义
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <param name="designerViewModel">工作流设计器视图模型</param>
        /// <returns>是否成功加载</returns>
        public async Task<bool> LoadWorkflowByIdAsync(string workflowId, WorkflowDesignerViewModel designerViewModel)
        {
            try
            {
                Logger.Info($"开始加载工作流: {workflowId}");

                var workflowDefinition = await _workflowRepository.GetWorkflowDefinitionAsync(workflowId);

                if (workflowDefinition == null)
                {
                    Logger.Warn($"未找到工作流定义: {workflowId}");
                    return false;
                }

                // 在设计器中加载工作流
                await designerViewModel.LoadWorkflowAsync(workflowDefinition);

                Logger.Info($"工作流 '{workflowDefinition.Name}' 加载成功");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"加载工作流 {workflowId} 失败");
                throw new ApplicationException($"加载工作流失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取工作流定义的摘要信息
        /// </summary>
        /// <param name="workflowId">工作流ID</param>
        /// <returns>工作流摘要信息</returns>
        public async Task<WorkflowSummary> GetWorkflowSummaryAsync(string workflowId)
        {
            try
            {
                var workflowDefinition = await _workflowRepository.GetWorkflowDefinitionAsync(workflowId);

                if (workflowDefinition == null)
                    return null;

                return new WorkflowSummary
                {
                    Id = workflowDefinition.Id,
                    Name = workflowDefinition.Name,
                    Description = workflowDefinition.Description,
                    Version = workflowDefinition.Version,
                    Category = workflowDefinition.Category,
                    CreatedTime = workflowDefinition.CreatedTime,
                    CreatedBy = workflowDefinition.CreatedBy,
                    NodeCount = GetNodeCountFromJson(workflowDefinition.NodesJson),
                    ConnectionCount = GetConnectionCountFromJson(workflowDefinition.ConnectionsJson)
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"获取工作流摘要失败: {workflowId}");
                return null;
            }
        }

        /// <summary>
        /// 从JSON中获取节点数量
        /// </summary>
        private int GetNodeCountFromJson(string nodesJson)
        {
            try
            {
                if (string.IsNullOrEmpty(nodesJson))
                    return 0;

                var nodeDataList = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<dynamic>>(nodesJson);
                return nodeDataList?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 从JSON中获取连接数量
        /// </summary>
        private int GetConnectionCountFromJson(string connectionsJson)
        {
            try
            {
                if (string.IsNullOrEmpty(connectionsJson))
                    return 0;

                var connections = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<dynamic>>(connectionsJson);
                return connections?.Count ?? 0;
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// 工作流摘要信息
    /// </summary>
    public class WorkflowSummary
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public string Category { get; set; }
        public DateTime CreatedTime { get; set; }
        public string CreatedBy { get; set; }
        public int NodeCount { get; set; }
        public int ConnectionCount { get; set; }
    }
}