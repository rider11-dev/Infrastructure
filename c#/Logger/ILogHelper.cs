using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger.LogHelper
{
    public interface ILogHelper<T>
    {
        void LogError(Exception ex);
        void LogError(string msg);
        void LogInfo(Exception ex);
        void LogInfo(string msg);
        void LogWarning(Exception ex);
        void LogWarning(string msg);
    }
}
