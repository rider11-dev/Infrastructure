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
    internal abstract class DbHelper : IDbHelper
    {
        #region 属性变量
        public abstract DbType DbType { get; }
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
                    string connstr = DbConfigureHelper.GetConnectString();
                    this._conn = this.GetConnection();
                }
                return this._conn;
            }
            set
            {
                this._conn = value;
            }
        }

        string _connStr = string.Empty;
        protected string ConnString
        {
            get
            {
                if (string.IsNullOrEmpty(_connStr))
                {
                    _connStr = DbConfigureHelper.GetConnectString();
                }
                return _connStr;
            }
        }

        /// <summary>
        /// 是否自动关闭连接
        /// </summary>
        public bool AutoClose = true;

        const string Msg_Error = "数据库操作失败";
        #endregion

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

        public int ExecuteSql(string sql)
        {
            DbCommand cmd = this.GetDbCommand(sql);
            return ExecuteNonQueryCore(cmd);
        }

        public int ExecuteSql(string sql, params DbParameter[] dbParams)
        {
            DbCommand cmd = this.GetDbCommand(sql);
            cmd.Parameters.AddRange(dbParams);
            return ExecuteNonQueryCore(cmd);
        }

        public object ExecuteScalar(string sql)
        {
            DbCommand cmd = this.GetDbCommand(sql);
            return ExecuteScalarCore(cmd);
        }

        public object ExecuteScalar(string sql, params DbParameter[] dbParams)
        {
            DbCommand cmd = this.GetDbCommand(sql);
            cmd.Parameters.AddRange(dbParams);
            return ExecuteScalarCore(cmd);
        }

        public DataSet GetDataSet(string sql)
        {
            DbDataAdapter adapter = GetDataAdapter(sql);
            return GetDataSetCore(adapter);
        }

        public DataSet GetDataSet(string sql, params DbParameter[] dbParams)
        {
            DbDataAdapter adapter = GetDataAdapter(sql, dbParams);
            return GetDataSetCore(adapter);
        }
        #endregion

        #region 私有方法
        private int ExecuteNonQueryCore(DbCommand cmd)
        {
            int result = 0;
            bool error = false;
            try
            {
                Open();
                result = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                error = true;
                if (cmd.Transaction != null)
                {
                    cmd.Transaction.Rollback();
                }
                throw new Exception(Msg_Error + "," + ex.Message, ex);
            }
            finally
            {
                cmd.Dispose();
                if (AutoClose || error)
                {
                    Close();
                }
            }

            return result;
        }

        private object ExecuteScalarCore(DbCommand cmd)
        {
            object result = 0;
            bool error = false;
            try
            {
                Open();
                result = cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                error = true;
                if (cmd.Transaction != null)
                {
                    cmd.Transaction.Rollback();
                }
                throw new Exception(Msg_Error + "," + ex.Message, ex);
            }
            finally
            {
                cmd.Dispose();
                if (AutoClose || error)
                {
                    Close();
                }
            }

            return result;
        }

        private DataSet GetDataSetCore(DbDataAdapter adapter)
        {
            DataSet ds = new DataSet();
            bool exHappened = false;
            try
            {
                adapter.Fill(ds);
            }
            catch (Exception ex)
            {
                exHappened = true;
                throw new Exception(Msg_Error + "," + ex.Message, ex);
            }
            finally
            {
                if (AutoClose || exHappened)
                {
                    Close();
                }
            }

            return ds;
        }

        #endregion

        #region 子类实现方法
        protected abstract DbConnection GetConnection();
        protected abstract DbCommand GetDbCommand(string sql);
        protected abstract DbDataAdapter GetDataAdapter(string sql);
        protected abstract DbDataAdapter GetDataAdapter(string sql, params DbParameter[] dbParams);
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
