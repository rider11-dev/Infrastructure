using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public interface IDbHelper
    {
        bool TestConnection();
        int ExecuteSql(string sql, params KeyValuePair<string, object>[] dbParams);

        object ExecuteScalar(string sql, params KeyValuePair<string, object>[] dbParams);

        DataSet GetDataSet(string sql, params KeyValuePair<string, object>[] dbParams);
    }
}
