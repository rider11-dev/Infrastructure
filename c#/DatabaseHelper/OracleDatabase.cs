using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class OracleDatabase : Database
    {
        public OracleDatabase() : base() { }

        protected override DbConnection GetConnection()
        {
            return new OracleConnection(base.GetConnectionString());
        }

        protected override DbCommand GetDBCommand(string sql)
        {
            return new OracleCommand(sql, (OracleConnection)base.Connection);
        }

        protected override DbDataAdapter GetDataAdapter(string sql)
        {
            return new OracleDataAdapter(sql, (OracleConnection)base.Connection);
        }

        protected override DbDataAdapter GetDataAdapter(string sql, List<DbParameter> dbParas)
        {
            OracleCommand cmd = new OracleCommand(sql, (OracleConnection)base.Connection);
            foreach (OracleParameter p in dbParas)
            {
                cmd.Parameters.Add(p);
            }
            return new OracleDataAdapter(cmd);
        }

        protected override void RecordSqlError(Exception ex)
        {
            base.RecordSqlError(ex);
            //记录异常
            string insertError = "insert into ErrorsLog(ErrorID,ErrorTime,ErrorSummary,ErrorDetail) values(sys_guid(),:errorTime,:errorSummary,:errorDetail)";
            List<DbParameter> lstPara = new List<DbParameter>
            {
                new OracleParameter{ParameterName=":errorTime",OracleDbType=OracleDbType.Date,Size=20,Value=DateTime.Now},
                new OracleParameter{ParameterName=":errorSummary",OracleDbType=OracleDbType.Varchar2,Size=200,Value=ex.Message},
                new OracleParameter{ParameterName=":errorDetail",OracleDbType=OracleDbType.Varchar2,Size=4000,Value=base.SerializeException(ex)}
            };
            base.ExecuteSql(insertError, lstPara);
        }
    }
}
