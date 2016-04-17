using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZPF.Infrastructure.DatabaseHelper;

namespace Test
{
    public class DbHelperTest
    {
        public static void Test()
        {
            ConnectTest();
        }

        static void ConnectTest()
        {
            bool rst=DbContext.DbHelper.TestConnection();
            Console.WriteLine("connect test result:"+ (rst ? "true" : "false"));
        }
    }
}
