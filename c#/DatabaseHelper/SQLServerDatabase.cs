using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class SQLServerDatabase : Database
    {
        public SQLServerDatabase() : base() { }

        protected override DbConnection GetConnection()
        {
            return new SqlConnection(base.GetConnectionString());
        }

        protected override DbCommand GetDBCommand(string sql)
        {
            return new SqlCommand(sql, (SqlConnection)base.Connection);
        }

        protected override DbDataAdapter GetDataAdapter(string sql)
        {
            return new SqlDataAdapter(sql, (SqlConnection)base.Connection);
        }

        protected override DbDataAdapter GetDataAdapter(string sql, List<DbParameter> dbParas)
        {
            SqlCommand cmd = base.GetDBCommandWithParameters(sql, dbParas) as SqlCommand;
            return new SqlDataAdapter(cmd);
        }

        protected override void RecordSqlError(Exception ex)
        {
            base.RecordSqlError(ex);
            //记录异常
            string insertError = "insert into ErrorsLog(ErrorID,ErrorTime,ErrorSummary,ErrorDetail) values(newid(),@errorTime,@errorSummary,@errorDetail)";
            List<DbParameter> lstPara = new List<DbParameter>
            {
                new SqlParameter { ParameterName = "@errorTime", SqlDbType = SqlDbType.DateTime,Size=20, Value = DateTime.Now },
                new SqlParameter { ParameterName = "@errorSummary", SqlDbType = SqlDbType.VarChar,Size=200, Value = ex.Message },
                new SqlParameter { ParameterName = "@errorDetail", SqlDbType = SqlDbType.VarChar,Size=4000, Value = base.SerializeException(ex) },
            };
            base.ExecuteSql(insertError, lstPara);
        }
    }
}
