using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.Components.Extensions
{
    public static class EnumExtension
    {
        public static string GetString<TEnum>(this TEnum value)
        {
            return Enum.GetName(typeof(TEnum), value);
        }
    }
}
