using System;
using System.Configuration;
using System.IO;

namespace ZPF.Infrastructure.Components
{
    /// <summary>
    /// 配置辅助类
    /// </summary>
    public class ConfigureHelper
    {
        private string Msg_Error_ReadConfigFile
        {
            get
            {
                return global::ZPF.Infrastructure.Components.Resource.Msg_Error_ReadConfigFile;
            }
        }
        ExeConfigurationFileMap _configMap = null;
        Configuration _conf;
        public Configuration Configuration
        {
            get
            {
                if (_conf == null)
                {
                    try
                    {
                        _conf = ConfigurationManager.OpenMappedExeConfiguration(this._configMap, ConfigurationUserLevel.None);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(Msg_Error_ReadConfigFile + "，实例化ConfigurationFileMap对象失败！", ex);
                    }
                }
                return _conf;
            }
        }

        public ConfigureHelper(string configFile)
        {
            if (!File.Exists(configFile))
            {
                throw new FileNotFoundException(Msg_Error_ReadConfigFile + "，未找到配置文件" + configFile);
            }
            _configMap = new ExeConfigurationFileMap();
            _configMap.ExeConfigFilename = configFile;
        }

        public ConfigureHelper()
            : this(Common.ConfigureFile)
        {

        }

        /// <summary>
        /// 写入appsetting
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetAppSettings(string key, string value)
        {
            GetAppSettingSection().Settings[key].Value = value;
            Configuration.Save();
        }
        /// <summary>
        /// 读取appsetting
        /// </summary>
        /// <param name="key"></param>
        /// <param name="throwWhenNotFound"></param>
        /// <returns></returns>
        public string GetAppSettings(string key, bool throwWhenNotFound = true)
        {
            var setting = GetAppSettingSection().Settings[key];
            if (setting == null)
            {
                if (throwWhenNotFound)
                {
                    throw new Exception(Msg_Error_ReadConfigFile + "，未能在appSettings配置节中找到键值" + key);
                }
                else
                {
                    return string.Empty;

                }
            }
            return setting.Value;
        }

        private AppSettingsSection GetAppSettingSection()
        {
            AppSettingsSection appSection = null;
            try
            {
                appSection = Configuration.AppSettings;
                if (appSection == null)
                {
                    throw new Exception(Msg_Error_ReadConfigFile + "，未找到appSettings配置节！");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(Msg_Error_ReadConfigFile + "，" + ex.Message, ex);
            }
            return appSection;
        }

        public void SetConnectionStringSettings(string name, string value, string providerName)
        {
            var connSection = GetConnectionStringsSection();
            connSection.ConnectionStrings.Remove(name);
            connSection.ConnectionStrings.Add(new ConnectionStringSettings { Name = name, ConnectionString = value, ProviderName = providerName });
            Configuration.Save();
        }

        public ConnectionStringSettings GetConnectionSettings(string name, bool throwWhenNotFound = true)
        {
            var setting = GetConnectionStringsSection().ConnectionStrings[name];
            if (setting == null)
            {
                if (throwWhenNotFound)
                {
                    throw new Exception(Msg_Error_ReadConfigFile + "，未能在connectionStrings配置节中找到name值" + name);
                }
                else
                {
                    return null;
                }
            }
            return setting;
        }

        public string GetConnectionString(string name, bool throwWhenNotFound = true)
        {
            return GetConnectionSettings(name, throwWhenNotFound).ConnectionString;
        }

        private ConnectionStringsSection GetConnectionStringsSection()
        {
            ConnectionStringsSection connSection = null;
            try
            {
                connSection = Configuration.ConnectionStrings;
                if (connSection == null)
                {
                    throw new Exception(Msg_Error_ReadConfigFile + "，未找到connectionStrings配置节！");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(Msg_Error_ReadConfigFile + "，" + ex.Message, ex);
            }
            return connSection;
        }

    }
}
