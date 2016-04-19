
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class DbContext
    {
        static IDbHelper _currentDbHelper;
        public static IDbHelper DbHelper
        {
            get
            {
                if (_currentDbHelper == null)
                {
                    _currentDbHelper = new DbHelper();
                }
                return _currentDbHelper;
            }
        }

        public static DbType DbType
        {
            get
            {
                var provider = DbConfigureHelper.Provider.ToLower();
                if (provider.Contains("sqlclient"))
                {
                    return DbType.SqlServer;
                }
                if (provider.Contains("sqlite"))
                {
                    return DbType.SQLite;
                }
                if (provider.Contains("oracle"))
                {
                    return DbType.Oracle;
                }
                if (provider.Contains("mysql"))
                {
                    return DbType.MySQL;
                }
                return DbType.UnKnown;
            }
        }

        public static string VariablePrefix
        {
            get
            {
                switch (DbType)
                {
                    case DbType.SqlServer:
                    case DbType.MySQL:
                    case DbType.SQLite:
                        return "@";
                    case DbType.Oracle:
                        return ":";
                    default:
                        return "";
                }
            }
        }
    }
}