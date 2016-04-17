using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class OracleDbHelper : DbHelper
    {
        public OracleDbHelper() : base() { }

        public override DbType DbType
        {
            get
            {
                return DbType.Oracle;
            }
        }

        protected override DbConnection GetConnection()
        {
            return new OracleConnection(base.ConnString);
        }

        protected override DbCommand GetDbCommand(string sql)
        {
            return new OracleCommand(sql, base.Connection as OracleConnection);
        }
    }
}
