using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.DatabaseHelper
{
    internal class SQLiteDbHelper : DbHelper
    {
        public override DbType DbType
        {
            get
            {
                return DbType.SqlLite;
            }
        }

        protected override DbConnection GetConnection()
        {
            return new SQLiteConnection(base.ConnString);
        }

        protected override DbCommand GetDbCommand(string sql)
        {
            return new SQLiteCommand(sql, base.Connection as SQLiteConnection);
        }
        
        protected override DbDataAdapter GetDataAdapter(string sql)
        {
            SQLiteCommand cmd = this.GetDbCommand(sql) as SQLiteCommand;
            return new SQLiteDataAdapter(cmd);
        }

        protected override DbDataAdapter GetDataAdapter(string sql, params DbParameter[] dbParams)
        {
            SQLiteCommand cmd = this.GetDbCommand(sql) as SQLiteCommand;
            cmd.Parameters.AddRange(dbParams);
            return new SQLiteDataAdapter(cmd);
        }
    }
}
