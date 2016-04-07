using DatabaseHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            TestDb();
            Console.ReadKey();
        }

        static void TestDb()
        {
            DataSet ds = null;
            Database db = Database.CurrentDB;
            //ora
            //ds = db.GetDataSet("select * from all_users");
            //mss
            //ds = db.GetDataSet("select * from GB_CaoZhuo_ZiDian");
            //mysql
            db.ExecuteSql("use mysql");
            ds = db.GetDataSet("select * from user");
        }
    }
}
