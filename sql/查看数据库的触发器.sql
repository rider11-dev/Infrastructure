select t.*,b.name
from (select name,id,xtype,status,parent_obj,crdate,refdate 
		from   sysobjects where xtype='TR') t
left join sysobjects b on t.parent_obj=b.id 