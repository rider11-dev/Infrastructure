using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class MySqlDbHelper : DbHelper
    {
        public MySqlDbHelper() : base() { }

        public override DbType DbType
        {
            get
            {
                return DbType.MySQL;
            }
        }

        protected override DbConnection GetConnection()
        {
            return new MySqlConnection(base.ConnString);
        }

        protected override DbCommand GetDbCommand(string sql)
        {
            return new MySqlCommand(sql, base.Connection as MySqlConnection);
        }
    }
}
