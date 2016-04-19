using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using ZPF.Infrastructure.Components;

namespace ZPF.Infrastructure.DatabaseHelper
{
    internal class DbHelper : IDbHelper
    {
        #region 属性变量
        private DbConnection _conn;
        /// <summary>
        /// 数据库连接
        /// </summary>
        protected DbConnection Connection
        {
            get
            {
                if (this._conn == null)
                {
                    this._conn = _dbProviderFactory.CreateConnection();
                    this._conn.ConnectionString = DbConfigureHelper.ConnectString;
                }
                return this._conn;
            }
            set
            {
                this._conn = value;
            }
        }

        const string Msg_Error = "数据库操作失败";

        DbProviderFactory _dbProviderFactory = null;
        #endregion

        public DbHelper()
        {
            try
            {
                _dbProviderFactory = DbProviderFactories.GetFactory(DbConfigureHelper.Provider);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("无法创建指定类型的DbProviderFactory实例");
            }
            if (_dbProviderFactory == null)
            {
                throw new ArgumentException("无法创建指定类型的DbProviderFactory实例");
            }
        }

        #region 外部方法
        /// <summary>
        /// 连接测试
        /// </summary>
        /// <returns></returns>
        public bool TestConnection()
        {
            try
            {
                Open();
            }
            catch (Exception ex)
            {
                throw new Exception("测试数据库连接失败：" + ex.Message, ex);
            }
            finally
            {
                this.Close();
            }
            return true;
        }

        public int ExecuteSql(string sql, params KeyValuePair<string, object>[] dbParams)
        {
            DbCommand cmd = BuildDbCommand(sql, dbParams);
            return ExecuteNonQueryCore(cmd);
        }


        public object ExecuteScalar(string sql, params KeyValuePair<string, object>[] dbParams)
        {
            DbCommand cmd = BuildDbCommand(sql, dbParams);
            return ExecuteScalarCore(cmd);
        }

        public DataSet GetDataSet(string sql, params KeyValuePair<string, object>[] dbParams)
        {
            DbCommand cmd = BuildDbCommand(sql, dbParams);
            DbDataAdapter adapter = _dbProviderFactory.CreateDataAdapter();
            adapter.SelectCommand = cmd;
            return GetDataSetCore(adapter);
        }

        #endregion

        #region 私有方法
        private DbCommand BuildDbCommand(string sql, params KeyValuePair<string, object>[] dbParams)
        {
            DbCommand cmd = _dbProviderFactory.CreateCommand();
            cmd.Connection = Connection;
            cmd.CommandText = sql;
            BuildDbParameters(dbParams, cmd);
            return cmd;
        }

        private void BuildDbParameters(KeyValuePair<string, object>[] dbParams, DbCommand cmd)
        {
            DbParameter para = null;
            foreach (KeyValuePair<string, object> kvp in dbParams)
            {
                para = _dbProviderFactory.CreateParameter();
                para.ParameterName = kvp.Key;
                para.Value = kvp.Value;

                cmd.Parameters.Add(para);
            }
        }
        private int ExecuteNonQueryCore(DbCommand cmd)
        {
            int result = 0;
            try
            {
                Open();
                result = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(Msg_Error + "," + ex.Message, ex);
            }
            finally
            {
                cmd.Dispose();
                Close();
            }

            return result;
        }

        private object ExecuteScalarCore(DbCommand cmd)
        {
            object result = 0;
            try
            {
                Open();
                result = cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new Exception(Msg_Error + "," + ex.Message, ex);
            }
            finally
            {
                cmd.Dispose();
                Close();
            }

            return result;
        }

        private DataSet GetDataSetCore(DbDataAdapter adapter)
        {
            DataSet ds = new DataSet();
            try
            {
                adapter.Fill(ds);
            }
            catch (Exception ex)
            {
                throw new Exception(Msg_Error + "," + ex.Message, ex);
            }
            finally
            {
                Close();
            }

            return ds;
        }

        #endregion

        /// <summary>
        /// 打开连接
        /// </summary>
        protected void Open()
        {
            if (Connection != null)
            {
                if (Connection.State == ConnectionState.Broken || Connection.State == ConnectionState.Closed)
                {
                    Connection.Open();
                }
            }
            else
            {
                throw new Exception("未能获取数据库连接！");
            }
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        protected void Close()
        {
            if (this._conn != null)
            {
                this._conn.Close();
            }
        }

    }
}
