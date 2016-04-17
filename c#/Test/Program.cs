using ZPF.Infrastructure.DatabaseHelper;
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
            //TestConfManager();
            TestDbHelper();
            Console.ReadKey();
        }

        public static void TestConfManager()
        {
            new ConfigureManagerTest("app.config").Test();
        }

        public static void TestDbHelper()
        {
            DbHelperTest.Test();
        }
    }
}
