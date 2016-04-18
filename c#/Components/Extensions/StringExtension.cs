using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.Components.Extensions
{
    /// <summary>
    /// 字符串扩展类
    /// </summary>
    public static class StringExtension
    {
        /// <summary>
        /// 针对单个格式字符串
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <example>
        /// "welcome to {0}! welcome to {1}!".FormatWith("www.cnblogs.com", "q.cnblogs.com");
        /// </example>
        public static string FormatWith(this string format, params object[] args)
        {
            if (format == null || args == null)
            {
                throw new ArgumentNullException((format == null) ? "format" : "args");
            }
            else
            {
                var capacity = format.Length + args.Where(a => a != null).Select(p => p.ToString()).Sum(p => p.Length);
                StringBuilder sb = new StringBuilder(capacity);
                sb.AppendFormat(format, args);
                return sb.ToString();
            }
        }

        /// <summary>
        ///  针对多个格式字符串
        /// </summary>
        /// <param name="formats"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <example>
        /// new string[] { "welcome to {0}!", " welcome to {1}!" }.FormatWith("www.cnblogs.com", "q.cnblogs.com");
        /// </example>
        public static string FormatWith(this IEnumerable<string> formats, params object[] args)
        {
            if (formats == null || args == null)
            {
                throw new ArgumentNullException((formats == null) ? "formats" : "args");
            }
            else
            {
                var capacity = formats.Where(f => !string.IsNullOrEmpty(f)).Sum(f => f.Length) +
                    args.Where(a => a != null).Select(p => p.ToString()).Sum(p => p.Length);
                StringBuilder sb = new StringBuilder(capacity);
                foreach (var f in formats)
                {
                    if (!string.IsNullOrEmpty(f))
                    {
                        sb.AppendFormat(f, args);
                    }
                }
                return sb.ToString();
            }
        }

        public static bool IsEmpty(this string str)
        {
            return (str == null) || (string.IsNullOrEmpty(str.Trim()));
        }

        public static bool IsNotEmpty(this string str)
        {
            return !str.IsEmpty();
        }
    }
}
