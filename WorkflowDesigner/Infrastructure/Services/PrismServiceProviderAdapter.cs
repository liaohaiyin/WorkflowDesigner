using System;
using Prism.Ioc;

namespace WorkflowDesigner.Infrastructure.Services
{
    /// <summary>
    /// Prism容器到IServiceProvider的适配器
    /// </summary>
    public class PrismServiceProviderAdapter : IServiceProvider
    {
        private readonly IContainerProvider _containerProvider;

        public PrismServiceProviderAdapter(IContainerProvider containerProvider)
        {
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));
        }

        public object GetService(Type serviceType)
        {
            try
            {
                return _containerProvider.Resolve(serviceType);
            }
            catch (Exception)
            {
                // 如果无法解析，返回null（符合IServiceProvider契约）
                return null;
            }
        }
    }
}