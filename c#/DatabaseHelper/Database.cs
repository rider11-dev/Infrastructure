using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace ZPF.Infrastructure.DatabaseHelper
{
    public abstract class Database
    {
        #region 属性变量
        private static DatabaseType DBType
        {
            get
            {
                return DbConfigure.GetDBType();
            }
        }
        private DbConnection _conn;
        /// <summary>
        /// 数据库连接
        /// </summary>
        protected DbConnection Connection
        {
            get
            {
                if (this._conn == null)
                    this._conn = this.GetConnection();
                return this._conn;
            }
            set
            {
                this._conn = value;
            }
        }
        private static Database _instance = null;
        public static Database CurrentDB
        {
            get
            {
                if (_instance == null)
                {
                    switch (DBType)
                    {
                        case DatabaseType.SqlServer:
                            _instance = new SQLServerDatabase();
                            break;
                        case DatabaseType.Oracle:
                            _instance = new OracleDatabase();
                            break;
                        case DatabaseType.MySQL:
                            _instance = new MySQLDatabase();
                            break;
                        default:
                            _instance = null;
                            break;
                    }
                }
                return _instance;
            }
        }
        /// <summary>
        /// 是否自动关闭连接
        /// </summary>
        public bool AutoClose = true;
        #endregion

        #region 外部方法
        public int ExecuteSql(string sql)
        {
            DbCommand cmd = GetDBCommand(sql);
            return ExecuteSqlInternal(cmd);
        }
        public int ExecuteSql(string sql, List<DbParameter> dbParas)
        {
            DbCommand cmd = this.GetDBCommandWithParameters(sql, dbParas);
            return ExecuteSqlInternal(cmd);
        }
        public int ExecuteSqlInTransaction(string sql, DbTransaction trans)
        {
            DbCommand cmd = GetDBCommand(sql);
            cmd.Transaction = trans;
            return ExecuteSqlInternal(cmd);
        }
        public int ExecuteSqlInTransaction(string sql, List<DbParameter> dbParas, DbTransaction trans)
        {
            DbCommand cmd = this.GetDBCommandWithParameters(sql, dbParas);
            cmd.Transaction = trans;
            return ExecuteSqlInternal(cmd);
        }
        /// <summary>
        /// 获取DataSet
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public DataSet GetDataSet(string sql)
        {
            DbDataAdapter adapter = GetDataAdapter(sql);
            return GetDataSetInternal(adapter);
        }
        /// <summary>
        /// 使用绑定变量方式获取ds
        /// </summary>
        /// <param name="sql">例:select Code,Name from Stuent where ID = :ID</param>
        /// <param name="dbParas"></param>
        /// <returns></returns>
        public DataSet GetDataSet(string sql, List<DbParameter> dbParas)
        {
            DbDataAdapter adapter = GetDataAdapter(sql, dbParas);
            return GetDataSetInternal(adapter);
        }
        
        /// <summary>
        /// 打开连接
        /// </summary>
        public void Open()
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
                throw new Exception("未获取数据库连接！");
            }
        }
        /// <summary>
        /// 关闭连接
        /// </summary>
        public void Close()
        {
            if (this._conn != null)
            {
                this._conn.Close();
            }
        }
        public DbTransaction BeginTransaction(IsolationLevel isoLevel)
        {
            Open();
            return Connection.BeginTransaction(isoLevel);
        }
        public DbTransaction BeginTransaction()
        {
            Open();
            return Connection.BeginTransaction();
        }
        #endregion

        #region 虚方法、抽象方法
        /// <summary>
        /// 数据库连接
        /// </summary>
        /// <returns></returns>
        protected abstract DbConnection GetConnection();
        /// <summary>
        /// 获取数据库命令对象
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected abstract DbCommand GetDBCommand(string sql);

        /// <summary>
        /// 获取DataSet适配器
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected abstract DbDataAdapter GetDataAdapter(string sql);
        /// <summary>
        /// 获取带参数DataSet适配器
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dbParas"></param>
        /// <returns></returns>
        protected abstract DbDataAdapter GetDataAdapter(string sql, List<DbParameter> dbParas);
        /// <summary>
        /// 记录sql错误
        /// </summary>
        /// <param name="ex"></param>
        protected virtual void RecordSqlError(Exception ex)
        {
            AutoClose = true;//自动关闭连接
        }
        #endregion

        #region 内部方法
        /// <summary>
        /// 序列化异常对象
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected string SerializeException(Exception ex)
        {
            BinaryFormatter bFmt = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bFmt.Serialize(ms, ex);
                return ms.ToArray().ToString();
            }
        }
        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <returns></returns>
        protected string GetConnectionString()
        {
            return DbConfigure.GetDBConnectionString();
        }

        /// <summary>
        /// 构造带参数数据库操作命令
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="dbParas"></param>
        /// <returns></returns>
        protected DbCommand GetDBCommandWithParameters(string sql, List<DbParameter> dbParas)
        {
            DbCommand cmd = this.GetDBCommand(sql);
            cmd.Parameters.AddRange(dbParas.ToArray());
            return cmd;
        }
        private int ExecuteSqlInternal(DbCommand cmd)
        {
            int result = 0;
            bool exHappened = false;
            try
            {
                Open();
                result = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                exHappened = true;
                if (cmd.Transaction != null)
                {
                    cmd.Transaction.Rollback();
                }
                if (DbConfigure.LogError)
                {
                    RecordSqlError(ex);
                }
                throw ex;
            }
            finally
            {
                cmd.Dispose();
                if (AutoClose || exHappened)
                {
                    Close();
                }
            }

            return result;
        }

        private DataSet GetDataSetInternal(DbDataAdapter adapter)
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
                RecordSqlError(ex);
                throw ex;
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
    }
}
