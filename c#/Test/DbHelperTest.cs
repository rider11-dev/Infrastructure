using Logger.LogHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZPF.Infrastructure.DatabaseHelper;
using ZPF.Infrastructure.Components.Extensions;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Data.SQLite;
using Oracle.ManagedDataAccess.Client;

namespace Test
{
    public class DbHelperTest
    {
        static ILogHelper<DbHelperTest> logHelper = new Log4NetHelper<DbHelperTest>();
        public static void Test()
        {
            ConnectTest();

            //ExecuteSqlTest();
            ExecuteSqlWithParamsTest();

            //ExecuteScalarTest();
            //ExecuteScalarWithParamsTest();

            //GetDataSetTest();
            //GetDataSetWithParameterTest();
        }

        static void ConnectTest()
        {
            logHelper.LogInfo("statrt:execute ConnectTest");
            try
            {
                bool rst = DbContext.DbHelper.TestConnection();
                Console.WriteLine("connect test result:" + (rst ? "true" : "false"));
            }
            catch (Exception ex)
            {
                logHelper.LogError(ex);
            }
            logHelper.LogInfo("end:execute ConnectTest");
        }

        static void ExecuteSqlTest()
        {
            //mss|ora
            //string sql = @"upate salesorder set count=count;";
            //mysql
            string sql = @"UPDATE salesorder SET `count`=`count`;";
            //执行ddl成功后，会返回-1
            int rst = DbContext.DbHelper.ExecuteSql(sql.Replace("\r\n", " "));
            Console.WriteLine("result:" + rst);
        }

        static void ExecuteSqlWithParamsTest()
        {
            //mss|sqlite
            //string sql = @"insert into SalesOrder(id,code,orderdate,count,amount) values(@id,@code,@orderdate,@count,@amount);";
            //mysql
            //string sql = @"insert into SalesOrder(id,code,orderdate,`count`,amount) values(@id,@code,@orderdate,@count,@amount);";
            //ora
            string sql = "insert into SCOTT.\"salesorder\"(\"id\",\"code\",\"orderdate\",\"count\",\"amount\") values(:id,:code,:orderdate,:count,:amount)";
            //mss
            //List<SqlParameter> paras = new List<SqlParameter>()
            //{
            //    new SqlParameter{ParameterName="@id",DbType=System.Data.DbType.Int32,Value=1},
            //    new SqlParameter{ParameterName="@code",DbType=System.Data.DbType.String,Value="001"},
            //    new SqlParameter{ParameterName="@orderdate",DbType=System.Data.DbType.DateTime,Value=DateTime.Now},
            //    new SqlParameter{ParameterName="@count",DbType=System.Data.DbType.Int32,Value=10},
            //    new SqlParameter{ParameterName="@amount",DbType=System.Data.DbType.Decimal,Value=129.92},
            //};
            //mysql
            //List<DbParameter> paras = new List<DbParameter>()
            //{
            //     new MySqlParameter{ParameterName="@id",DbType=System.Data.DbType.Int32,Value=1},
            //     new MySqlParameter{ParameterName="@code",DbType=System.Data.DbType.String,Value="001"},
            //     new MySqlParameter{ParameterName="@orderdate",DbType=System.Data.DbType.DateTime,Value=DateTime.Now},
            //     new MySqlParameter{ParameterName="@count",DbType=System.Data.DbType.Int32,Value=10},
            //     new MySqlParameter{ParameterName="@amount",DbType=System.Data.DbType.Decimal,Value=129.92},
            //};
            //sqlite
            //List<DbParameter> paras = new List<DbParameter>()
            //{
            //     new SQLiteParameter{ParameterName="@id",DbType=System.Data.DbType.Int32,Value=1},
            //     new SQLiteParameter{ParameterName="@code",DbType=System.Data.DbType.String,Value="001"},
            //     new SQLiteParameter{ParameterName="@orderdate",DbType=System.Data.DbType.DateTime,Value=DateTime.Now},
            //     new SQLiteParameter{ParameterName="@count",DbType=System.Data.DbType.Int32,Value=10},
            //     new SQLiteParameter{ParameterName="@amount",DbType=System.Data.DbType.Decimal,Value=129.92},
            //};
            //ora
            List<DbParameter> paras = new List<DbParameter>()
            {
                 new OracleParameter{ParameterName=":id",DbType=System.Data.DbType.Int32,Value=1},
                 new OracleParameter{ParameterName=":code",DbType=System.Data.DbType.String,Value="001"},
                 new OracleParameter{ParameterName=":orderdate",DbType=System.Data.DbType.DateTime,Value=DateTime.Now},
                 new OracleParameter{ParameterName=":count",DbType=System.Data.DbType.Int32,Value=10},
                 new OracleParameter{ParameterName=":amount",DbType=System.Data.DbType.Decimal,Value=129.92},
            };
            int rst = DbContext.DbHelper.ExecuteSql(sql.Replace("\r\n", " "), paras.ToArray());
            Console.WriteLine("result:" + rst);


            //sql = @"insert into salesorderitems(id,code,parent,name,amount,count) values(@id,@code,@parent,@name,@amount,@count);";
            //paras = new List<SqlParameter>()
            //{
            //    new SqlParameter{ParameterName="@id",DbType=System.Data.DbType.Int32,Value=1},
            //    new SqlParameter{ParameterName="@code",DbType=System.Data.DbType.String,Value="01"},
            //    new SqlParameter{ParameterName="@parent",DbType=System.Data.DbType.Int32,Value=1},
            //    new SqlParameter{ParameterName="@name",DbType=System.Data.DbType.String,Value="短袖"},
            //    new SqlParameter{ParameterName="@amount",DbType=System.Data.DbType.Decimal,Value=12},
            //    new SqlParameter{ParameterName="@count",DbType=System.Data.DbType.Int32,Value=2},
            //};
            //rst = DbContext.DbHelper.ExecuteSql(sql.Replace("\r\n", " "), paras.ToArray());
            //Console.WriteLine("result:" + rst);
        }

        static void ExecuteScalarTest()
        {
            //mss
            //string sql = @"select top 1 amount from salesorder where amount > 10";
            //mysql
            string sql = "select amount from salesorder limit 1";
            object rst = DbContext.DbHelper.ExecuteScalar(sql);
            Console.WriteLine("amount:" + Convert.ToDecimal(rst));
        }
        static void ExecuteScalarWithParamsTest()
        {
            //mss
            //string sql = @"select top 1 amount from salesorder where amount > @amount";
            //mysql
            string sql = @"select amount from salesorder where amount > @amount limit 1";
            //object rst = DbContext.DbHelper.ExecuteScalar(sql, new SqlParameter { ParameterName = "@amount", DbType = System.Data.DbType.Decimal, Value = 10 });
            object rst = DbContext.DbHelper.ExecuteScalar(sql, new MySqlParameter { ParameterName = "@amount", DbType = System.Data.DbType.Decimal, Value = 10 });
            Console.WriteLine("amount:" + Convert.ToDecimal(rst));
        }

        static void GetDataSetTest()
        {
            //mss
            //string sql = "select * from salesorder where count > 1";
            //mysql
            string sql = "select * from salesorder where `count` > 1";
            DataSet ds = DbContext.DbHelper.GetDataSet(sql);
            Console.WriteLine("row count:" + (ds.HasRow() ? ds.Tables[0].Rows.Count : 0));
        }

        static void GetDataSetWithParameterTest()
        {
            //mss|mysql
            string sql = "select * from salesorder where orderdate > @date";
            //mss
            //DataSet ds = DbContext.DbHelper.GetDataSet(sql, new SqlParameter { ParameterName = "@date", DbType = System.Data.DbType.DateTime, Value = new DateTime(2016, 4, 17) });
            DataSet ds = DbContext.DbHelper.GetDataSet(sql, new MySqlParameter { ParameterName = "@date", DbType = System.Data.DbType.DateTime, Value = new DateTime(2016, 4, 17) });
            Console.WriteLine("row count:" + (ds.HasRow() ? ds.Tables[0].Rows.Count : 0));
        }
    }
}
