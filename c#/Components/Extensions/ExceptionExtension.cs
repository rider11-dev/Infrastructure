using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.Components.Extensions
{
    public static class ExceptionExtension
    {
        /// <summary>
        /// 获取最底层异常
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        public static Exception Deepest(this Exception ex)
        {
            if (ex == null)
            {
                return null;
            }
            if (ex.InnerException == null)
            {
                return ex;
            }

            return ex.Deepest();
        }
    }
}
