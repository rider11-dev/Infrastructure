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
    public static class DataExtention
    {
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
    }
}
