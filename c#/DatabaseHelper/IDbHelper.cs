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
        DbType DbType { get; }
        bool TestConnection();
        int ExecuteSql(string sql);
        int ExecuteSql(string sql, params DbParameter[] dbParams);

        object ExecuteScalar(string sql);
        object ExecuteScalar(string sql, params DbParameter[] dbParams);

        DataSet GetDataSet(string sql);
        DataSet GetDataSet(string sql, params DbParameter[] dbParams);
    }
}
