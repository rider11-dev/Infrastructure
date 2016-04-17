using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZPF.Infrastructure.Components;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class DbConfigureHelper
    {
        const string KeyDbType = "dbtype";
        const string NameDbConnString = "dbconn";

        static ConfigureHelper confHelper = new ConfigureHelper();

        public static string GetDbType()
        {
            return confHelper.GetAppSettings(KeyDbType);
        }

        public static string GetConnectString()
        {
            return confHelper.GetConnectionString(NameDbConnString);
        }
    }
}
