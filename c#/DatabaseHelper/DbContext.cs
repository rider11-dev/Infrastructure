
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
                return DbHelper.DbType;
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