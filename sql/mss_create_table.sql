declare @num int,@sql nvarchar(max)
begin
	set @num=0
	select  @num=count(1) from dbo.SysObjects where id = object_id(N'test') and objectproperty(ID, 'IsTable') = 1
	if @num=0
	begin
		set @sql='create table test(id int primary key,code varchar(32))'
		exec sp_executesql @sql
	end
end