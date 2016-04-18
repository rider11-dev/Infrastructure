using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZPF.Infrastructure.Components;

namespace ZPF.Infrastructure.DatabaseHelper
{
    /// <summary>
    /// 数据库操作工厂类
    /// </summary>
    internal class DbHelperFactory
    {
        public static IDbHelper CreateInstance()
        {
            try
            {
                string dbtype = DbConfigureHelper.GetDbType();
                switch (dbtype)
                {
                    case "mss":
                        return new SqlServerDbHelper();
                    case "ora":
                        return new OracleDbHelper();
                    case "mysql":
                        return new MySqlDbHelper();
                    case "sqlite":
                        return new SQLiteDbHelper();
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("创建数据库辅助类失败！" + ex.Message, ex);
            }
        }
    }
}
