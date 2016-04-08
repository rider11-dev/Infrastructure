using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.Components
{
    /// <summary>
    /// 配置管理类
    /// </summary>
    public class ConfigureManager
    {
        ExeConfigurationFileMap _configMap = null;
        public ConfigureManager(string configFile)
        {
            _configMap = new ExeConfigurationFileMap(configFile);
        }

        /// <summary>
        /// 写入appsetting
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetAppSettings(string key, string value)
        {
            Configuration config = this.GetConfig();
            var appSection = config.GetSection("appSettings") as AppSettingsSection;
            appSection.Settings[key].Value = value;
            config.Save();
        }
        /// <summary>
        /// 读取appsetting
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetAppSettings(string key)
        {
            Configuration config = this.GetConfig();
            var appSection = config.AppSettings[]
            return appSection.Settings[key].Value;
        }

        public void Set

        private Configuration GetConfig()
        {
            return ConfigurationManager.OpenMappedExeConfiguration(this._configMap, ConfigurationUserLevel.None);
        }
    }
}
