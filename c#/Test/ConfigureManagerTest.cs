using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZPF.Infrastructure.Components;

namespace Test
{
    public class ConfigureManagerTest
    {
        ConfigureHelper confManager = null;
        public ConfigureManagerTest(string file)
        {
            confManager = new ConfigureHelper(file);
        }

        public void Test()
        {
            TestGetAppSetting("zpf");
            TestSetAppSetting("zpf", "dyl");
            TestGetAppSetting("zpf");

            TestGetConnString("mss");
            TestSetConnString("mss", "aaaaaaaaaaaaaaaa", "bbbbbbbbbbbbbbbbbbbb");
            TestGetConnString("mss");
        }

        public void TestGetAppSetting(string key)
        {
            string val = confManager.GetAppSettings(key);
            PrintResult(key, val);
        }

        public void TestSetAppSetting(string key, string value)
        {
            confManager.SetAppSettings(key, value);
        }

        public void TestGetConnString(string name)
        {
            string val = confManager.GetConnectionString(name);
            PrintResult(name, val);
        }

        public void TestSetConnString(string name, string value, string providerName)
        {
            confManager.SetConnectionStringSettings(name, value, providerName);
        }

        private void PrintResult(string key, string value)
        {
            Console.WriteLine(string.Format("key:{0},value:{1}", key, value));
        }
    }
}
