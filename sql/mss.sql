/*1、存储过程*/
if exists (select * from sysobjects where id = object_id(N'pro_test') and OBJECTPROPERTY(id, N'IsProcedure') = 1)
drop procedure pro_test
go

create procedure pro_test
(
	@p1 int
)
as
begin
	declare @v1 nvarchar(20)
	declare @v2 float
	set @v1='v1';
	set @v2=25;
	select 'p1:'+CAST(@p1 as varchar(10))+',v1:'+@v1+'v2:'+CAST(@v2 as varchar(10))
end
go


/*2、函数*/
if OBJECT_ID('fun_test') is not null drop function fun_test
go

create function fun_test
(
	@id int
)
returns varchar(20)
as
begin 
	declare @name varchar(20);
	select @name=name from user where id = @id
	return @name
end


/*3、新增列*/
alter table test add col1 varchar(100) not null default ''

/*4、修改列类型*/
alter table test alter column col1 varchar(20)


