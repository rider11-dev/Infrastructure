using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZPF.Infrastructure.Components;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class DbConfigureHelper
    {
        const string KeyDbType = "dbtype";
        const string NameConnectionStrings = "dbconn";

        static ConfigureHelper confHelper = new ConfigureHelper();
        static ConnectionStringSettings _connectionStringSettings = null;
        public static string GetDbType()
        {
            return confHelper.GetAppSettings(KeyDbType);
        }

        public static string ConnectString
        {
            get
            {
                return ConnectionStrings.ConnectionString;
            }
        }

        public static string Provider
        {
            get
            {
                return ConnectionStrings.ProviderName;
            }
        }

        public static ConnectionStringSettings ConnectionStrings
        {
            get
            {
                if (_connectionStringSettings == null)
                {
                    _connectionStringSettings = confHelper.GetConnectionSettings(NameConnectionStrings);
                }
                return _connectionStringSettings;
            }
        }
    }
}
