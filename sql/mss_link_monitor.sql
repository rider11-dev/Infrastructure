--查看数据库连接
SELECT * FROM
[Master].[dbo].[SYSPROCESSES] WHERE [DBID] IN ( SELECT 
   [DBID]
FROM 
   [Master].[dbo].[SYSDATABASES]
WHERE 
   NAME='test'
)