using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ZPF.Infrastructure.Components.Extensions
{
    public static class Extensions
    {
        #region String
        public static bool IsEmpty(this string str)
        {
            return (str == null) || (string.IsNullOrEmpty(str.Trim()));
        }

        public static bool IsNotEmpty(this string str)
        {
            return !str.IsEmpty();
        }
        #endregion

        #region Data

        public static bool HasRow(this DataSet ds)
        {
            return (ds != null) && (ds.Tables.Count > 0) && (ds.Tables[0].Rows.Count > 0);
        }

        public static bool HasRow(this DataSet ds, string tbName)
        {
            return HasTable(ds, tbName) && (ds.Tables[tbName].Rows.Count > 0);
        }

        public static bool HasTable(this DataSet ds, string tbName)
        {
            return (ds != null) && (ds.Tables.Count > 0) && (ds.Tables.Contains(tbName));
        }

        public static bool HasRow(this DataTable dt)
        {
            return (dt != null) && (dt.Rows.Count > 0);
        }

        public static void RemoveAllRows(this DataTable dt)
        {
            if (dt == null || dt.Rows.Count < 1)
            {
                return;
            }
            DataRowCollection rows = dt.Rows;
            for (int i = rows.Count - 1; i >= 0; i--)
            {
                rows.RemoveAt(i);
            }
        }

        public static void RemoveAllRows(this DataSet ds)
        {
            if (ds.Tables.Count > 0)
            {
                foreach (DataTable dt in ds.Tables)
                {
                    dt.RemoveAllRows();
                }
            }
        }

        public static void RemoveAllRows(this DataSet ds, string tbName)
        {
            if (ds.HasTable(tbName))
            {
                ds.Tables[tbName].RemoveAllRows();
            }
        }

        public static void AllowDBNull(this DataTable dt)
        {
            if (dt == null)
            {
                return;
            }

            foreach (DataColumn col in dt.Columns)
            {
                col.AllowDBNull = true;
            }
        }

        public static void AllowDBNull(this DataSet ds)
        {
            if (ds == null || ds.Tables.Count < 1)
            {
                return;
            }

            foreach (DataTable dt in ds.Tables)
            {
                dt.AllowDBNull();
            }
        }

        public static void AllowDBNull(this DataSet ds, string tbName)
        {
            if (HasTable(ds, tbName))
            {
                ds.Tables[tbName].AllowDBNull();
            }
        }

        public static string ToXml(this DataSet ds)
        {
            if (ds == null)
            {
                return string.Empty;
            }
            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb))
            {
                ds.WriteXml(sw);
                return sw.ToString();
            }
        }

        public static DataSet ToDataSet(this string str)
        {
            DataSet ds = new DataSet();
            using (StringReader sr = new StringReader(str))
            using (XmlTextReader xr = new XmlTextReader(sr))
            {
                ds.ReadXml(xr);
            }
            return ds;
        }

        public static void CopyOnSameColumn(this DataRow rowDest, DataRow rowSrc)
        {
            foreach (DataColumn col in rowDest.Table.Columns)
            {
                if (rowSrc.Table.Columns.Contains(col.ColumnName))
                {
                    rowDest[col.ColumnName] = (rowDest[col.ColumnName] is Boolean) ? rowSrc[col.ColumnName].ToBool() : rowSrc[col.ColumnName];
                }
            }
        }

        public static bool ToBool(this object obj)
        {
            if (obj is bool)
            {
                return (bool)obj;
            }
            if (obj == null || obj is DBNull)
            {
                return false;
            }

            string s = obj.ToString().ToLower();
            return s == "1" || s == "true";
        }

        #endregion

    }
}
