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
    public abstract class DbHelper : IDbHelper
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
                //TODO:此处记录日志；暂时throw
                throw ex;
                return false;
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
            return ExecuteCore(cmd);
        }

        #endregion

        #region 私有方法
        private int ExecuteCore(DbCommand cmd)
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

        #endregion

        #region 子类实现方法
        protected abstract DbConnection GetConnection();
        protected abstract DbCommand GetDbCommand(string sql);
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
