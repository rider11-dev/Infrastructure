using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace DatabaseHelper
{
    /// <summary>
    /// 配置操作类
    /// </summary>
    public class DbConfigure
    {
        /// <summary>
        /// 获取数据库类型
        /// </summary>
        /// <returns></returns>
        public static DatabaseType GetDBType()
        {
            string typeStr = ConfigurationManager.AppSettings["dbtype"];
            switch (typeStr)
            {
                case "mss":
                    return DatabaseType.SqlServer;
                case "ora":
                    return DatabaseType.Oracle;
                case "mysql":
                    return DatabaseType.MySQL;
                default:
                    return DatabaseType.None;
            }
        }

        /// <summary>
        /// 获取数据库连接字符串
        /// </summary>
        /// <returns></returns>
        public static string GetDBConnectionString()
        {
            return ConfigurationManager.AppSettings["dbconn"];
        }
        /// <summary>
        /// 是否记录错误日志
        /// </summary>
        public static bool LogError { get { return Convert.ToBoolean(ConfigurationManager.AppSettings["logerror"]); } }
    }
}
