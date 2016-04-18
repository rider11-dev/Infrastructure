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
        DbConnection Connection { get; set; }

        bool AutoClose { get; set; }

        bool TestConnection();
        int ExecuteSql(string sql, params DbParameter[] dbParams);

        object ExecuteScalar(string sql, params DbParameter[] dbParams);

        DataSet GetDataSet(string sql, params DbParameter[] dbParams);

        void BeginTransaction();
        void Commit();
        void Rollback();
    }
}
