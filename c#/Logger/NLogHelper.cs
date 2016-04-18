using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger.LogHelper
{
    public class NLogHelper<T> : ILogHelper<T>
    {
        NLog.ILogger logger = null;
        public NLogHelper()
        {
            logger = LogManager.GetLogger(this.GetType().FullName);
        }
        public void LogError(Exception ex)
        {
            if (logger == null)
            {
                return;
            }
            logger.Error(ex, "ERROR: {0}", ex);
        }

        public void LogError(string msg)
        {
            if (logger == null)
            {
                return;
            }
            logger.Error(msg);
        }

        public void LogInfo(Exception ex)
        {
            if (logger == null)
            {
                return;
            }
            logger.Info(ex, "Info: {0}", ex);
        }

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
            logger.Warn(ex, "Warn: {0}", ex);
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
