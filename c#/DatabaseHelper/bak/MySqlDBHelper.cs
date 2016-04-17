using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class MySqlDBHelper : DBHelper
    {
        public MySqlDBHelper() : base() { }

        protected override DbConnection GetConnection()
        {
            return new MySqlConnection(base.GetConnectionString());
        }

        protected override DbCommand GetDBCommand(string sql)
        {
            return new MySqlCommand(sql, (MySqlConnection)base.Connection);
        }

        protected override DbDataAdapter GetDataAdapter(string sql)
        {
            return new MySqlDataAdapter(sql, (MySqlConnection)base.Connection);
        }

        protected override DbDataAdapter GetDataAdapter(string sql, List<System.Data.Common.DbParameter> dbParas)
        {
            MySqlCommand cmd = base.GetDBCommandWithParameters(sql, dbParas) as MySqlCommand;
            return new MySqlDataAdapter(cmd);
        }
    }
}
