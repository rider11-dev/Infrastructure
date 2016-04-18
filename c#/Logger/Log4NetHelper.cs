using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[assembly: log4net.Config.XmlConfigurator(Watch = true, ConfigFile = "log4net.config")]
namespace Logger.LogHelper
{
    /// <summary>
    /// 日志辅助类，使用log4net
    /// </summary>
    public class Log4NetHelper<T> : ILogHelper<T>
    {
        log4net.ILog logger = null;
        public Log4NetHelper()
        {
            logger = log4net.LogManager.GetLogger(typeof(T));
        }
        /// <summary>
        /// 输出日志到Log4Net
        /// </summary>
        /// <param name="t">类型</param>
        /// <param name="ex">异常对象</param>
        public void LogError(Exception ex)
        {

            if (logger == null)
            {
                return;
            }
            logger.Error("Error:", ex);
        }
        /// <summary>
        /// 输出日志到Log4Net
        /// </summary>
        /// <param name="t">类型</param>
        /// <param name="msg">错误信息</param>
        public void LogError(string msg)
        {

            if (logger == null)
            {
                return;
            }
            logger.Error(msg);
        }

        /// <summary>
        /// 输出日志到Log4Net
        /// </summary>
        /// <param name="t">类型</param>
        /// <param name="msg">日志信息</param>
        public void LogInfo(Exception ex)
        {

            if (logger == null)
            {
                return;
            }
            logger.Info("Info:", ex);
        }

        /// <summary>
        /// 输出日志到Log4Net
        /// </summary>
        /// <param name="t">类型</param>
        /// <param name="msg">日志信息</param>
        public void LogInfo(string msg)
        {

            if (logger == null)
            {
                return;
            }
            logger.Info(msg);
        }

        public void LogWarning(Exception ex)
        {

            if (logger == null)
            {
                return;
            }
            logger.Warn("Warn:", ex);
        }

        public void LogWarning(string msg)
        {

            if (logger == null)
            {
                return;
            }
            logger.Warn(msg);
        }
    }
}
