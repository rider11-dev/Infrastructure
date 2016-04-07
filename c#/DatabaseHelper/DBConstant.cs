using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseHelper
{
    public class DBConstant
    {
        public static string GetDBVar(string varName)
        {
            return (DataBase.DBType == DataBaseType.Oracle ? ":" : "@") + varName;
        }
    }
}
