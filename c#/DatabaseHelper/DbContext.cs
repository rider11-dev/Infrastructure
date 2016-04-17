
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public class DbContext
    {
        static IDbHelper _currentDbHelper;
        public static IDbHelper DbHelper
        {
            get
            {
                if (_currentDbHelper == null)
                {
                    _currentDbHelper = DbHelperFactory.CreateInstance();
                }
                return _currentDbHelper;
            }
        }
    }
}