using MySql.Data.EntityFramework;
using MySql.Data.MySqlClient;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;

namespace WorkflowDesigner.Infrastructure.Data
{
    /// <summary>
    /// MySQL Entity Framework 配置
    /// </summary>
    public class MySqlEFConfiguration : DbConfiguration
    {
        public MySqlEFConfiguration()
        {
            // 设置MySQL数据库提供程序
            SetProviderServices("MySql.Data.MySqlClient", new MySqlProviderServices());
            SetProviderFactory("MySql.Data.MySqlClient", MySqlClientFactory.Instance);

            // 设置默认连接工厂
            SetDefaultConnectionFactory(new MySqlConnectionFactory());

            // 设置执行策略
            SetExecutionStrategy("MySql.Data.MySqlClient", () => new MySqlExecutionStrategy());

            // 设置迁移SQL生成器
            SetMigrationSqlGenerator("MySql.Data.MySqlClient", () => new MySqlMigrationSqlGenerator());
        }
    }
}