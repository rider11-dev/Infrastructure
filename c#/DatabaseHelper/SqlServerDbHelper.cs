using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data;

namespace ZPF.Infrastructure.DatabaseHelper
{
    internal class SqlServerDbHelper : DbHelper
    {
        public override DbType DbType
        {
            get
            {
                return DbType.SqlServer;
            }
        }

        public SqlServerDbHelper() : base() { }

        protected override DbConnection GetConnection()
        {
            return new SqlConnection(base.ConnString);
        }

        protected override DbCommand GetDbCommand(string sql)
        {
            return new SqlCommand(sql, base.Connection as SqlConnection);
        }

        protected override DbDataAdapter GetDataAdapter(string sql)
        {
            SqlCommand cmd = this.GetDbCommand(sql) as SqlCommand;
            return new SqlDataAdapter(cmd);
        }

        protected override DbDataAdapter GetDataAdapter(string sql, params DbParameter[] dbParams)
        {
            SqlCommand cmd = this.GetDbCommand(sql) as SqlCommand;
            cmd.Parameters.AddRange(dbParams);
            return new SqlDataAdapter(cmd);
        }
    }
}
