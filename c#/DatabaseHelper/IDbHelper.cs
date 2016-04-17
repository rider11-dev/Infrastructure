using System;
using System.Collections.Generic;
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
    }
}
