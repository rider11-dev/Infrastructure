namespace Components.Serialize.Protobuf
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text;
    using Components.Serialize.Protobuf.Protobuf;

    internal static class ProtobufDataSetSerializer
    {
        public static DataSet ProtoRead(Stream stream)
        {
            DataSet ds = new DataSet();
            using (ProtoReader reader = new ProtoReader(stream, null, null))
            {
                int num;
                while ((num = reader.ReadFieldHeader()) != 0)
                {
                    if (num <= 0)
                    {
                        continue;
                    }
                    switch (num)
                    {
                        case 1:
                        {
                            SubItemToken token = ProtoReader.StartSubItem(reader);
                            ProtoReadTable(ds, reader);
                            ProtoReader.EndSubItem(token, reader);
                            continue;
                        }
                        case 3:
                        {
                            ds.DataSetName = reader.ReadString();
                            continue;
                        }
                        case 4:
                        {
                            string s = reader.ReadString();
                            MemoryStream stream2 = new MemoryStream(Encoding.UTF8.GetBytes(s));
                            ds.ReadXmlSchema(stream2);
                            continue;
                        }
                    }
                    reader.SkipField();
                }
            }
            return ds;
        }

        private static void ProtoReadRelation(DataSet ds, ProtoReader reader)
        {
            int num;
            string relationName = null;
            string name = null;
            string str3 = null;
            string str4 = null;
            string str5 = null;
            while ((num = reader.ReadFieldHeader()) != 0)
            {
                switch (num)
                {
                    case 1:
                    {
                        relationName = reader.ReadString();
                        continue;
                    }
                    case 2:
                    {
                        name = reader.ReadString();
                        continue;
                    }
                    case 3:
                    {
                        str3 = reader.ReadString();
                        continue;
                    }
                    case 4:
                    {
                        str4 = reader.ReadString();
                        continue;
                    }
                    case 5:
                    {
                        str5 = reader.ReadString();
                        continue;
                    }
                }
                reader.SkipField();
            }
            if (ds.Tables.Contains(name) && ds.Tables.Contains(str4))
            {
                DataTable table = ds.Tables[name];
                DataTable table2 = ds.Tables[str4];
                string[] strArray = str5.Split(new char[] { ',' });
                string[] strArray2 = str3.Split(new char[] { ',' });
                List<DataColumn> list = new List<DataColumn>();
                List<DataColumn> list2 = new List<DataColumn>();
                foreach (string str6 in strArray)
                {
                    if (table2.Columns.Contains(str6))
                    {
                        list.Add(table2.Columns[str6]);
                    }
                }
                foreach (string str7 in strArray2)
                {
                    if (table.Columns.Contains(str7))
                    {
                        list2.Add(table.Columns[str7]);
                    }
                }
                DataRelation relation = new DataRelation(relationName, list2.ToArray(), list.ToArray(), false);
                ds.Relations.Add(relation);
            }
        }

        private static DataTable ProtoReadTable(DataSet ds, ProtoReader reader)
        {
            int num;
            Func<object> item = null;
            Func<object> func2 = null;
            Func<object> func3 = null;
            Func<object> func4 = null;
            Func<object> func5 = null;
            Func<object> func6 = null;
            Func<object> func7 = null;
            Func<object> func8 = null;
            Func<object> func9 = null;
            Func<object> func10 = null;
            Func<object> func11 = null;
            DataTable table = new DataTable();
            bool flag = false;
            object[] array = null;
            List<Func<object>> list = new List<Func<object>>();
            while ((num = reader.ReadFieldHeader()) != 0)
            {
                SubItemToken token;
                string str;
                string str2;
                string str3;
                MappedType type;
                Type type2;
                switch (num)
                {
                    case 1:
                    {
                        str = reader.ReadString();
                        if (!ds.Tables.Contains(str))
                        {
                            break;
                        }
                        table = ds.Tables[str];
                        flag = true;
                        continue;
                    }
                    case 2:
                        str2 = null;
                        str3 = string.Empty;
                        type = ~MappedType.Boolean;
                        token = ProtoReader.StartSubItem(reader);
                        goto Label_0115;

                    case 3:
                        if (array != null)
                        {
                            goto Label_03C0;
                        }
                        array = new object[table.Columns.Count];
                        goto Label_03CA;

                    default:
                        goto Label_0432;
                }
                table.TableName = str;
                continue;
            Label_00BF:
                switch (num)
                {
                    case 1:
                        str2 = reader.ReadString();
                        break;

                    case 2:
                        type = (MappedType) reader.ReadInt32();
                        break;

                    case 3:
                        str3 = reader.ReadString();
                        break;

                    default:
                        reader.SkipField();
                        break;
                }
            Label_0115:
                if ((num = reader.ReadFieldHeader()) != 0)
                {
                    goto Label_00BF;
                }
                switch (type)
                {
                    case MappedType.Boolean:
                        type2 = typeof(bool);
                        if (item == null)
                        {
                            item = () => reader.ReadBoolean();
                        }
                        list.Add(item);
                        break;

                    case MappedType.Byte:
                        type2 = typeof(byte[]);
                        if (func2 == null)
                        {
                            func2 = () => ReadBytes(reader);
                        }
                        list.Add(func2);
                        break;

                    case MappedType.Double:
                        type2 = typeof(double);
                        if (func3 == null)
                        {
                            func3 = () => reader.ReadDouble();
                        }
                        list.Add(func3);
                        break;

                    case MappedType.Int16:
                        type2 = typeof(short);
                        if (func6 == null)
                        {
                            func6 = () => reader.ReadInt16();
                        }
                        list.Add(func6);
                        break;

                    case MappedType.Int32:
                        type2 = typeof(int);
                        if (func5 == null)
                        {
                            func5 = () => reader.ReadInt32();
                        }
                        list.Add(func5);
                        break;

                    case MappedType.Int64:
                        type2 = typeof(long);
                        if (func4 == null)
                        {
                            func4 = () => reader.ReadInt64();
                        }
                        list.Add(func4);
                        break;

                    case MappedType.String:
                        type2 = typeof(string);
                        if (func8 == null)
                        {
                            func8 = () => reader.ReadString();
                        }
                        list.Add(func8);
                        break;

                    case MappedType.Decimal:
                        type2 = typeof(decimal);
                        if (func7 == null)
                        {
                            func7 = () => BclHelpers.ReadDecimal(reader);
                        }
                        list.Add(func7);
                        break;

                    case MappedType.Guid:
                        type2 = typeof(Guid);
                        if (func9 == null)
                        {
                            func9 = () => BclHelpers.ReadGuid(reader);
                        }
                        list.Add(func9);
                        break;

                    case MappedType.DateTime:
                        type2 = typeof(DateTime);
                        if (func10 == null)
                        {
                            func10 = () => BclHelpers.ReadDateTime(reader);
                        }
                        list.Add(func10);
                        break;

                    case MappedType.TimeSpan:
                        type2 = typeof(TimeSpan);
                        if (func11 == null)
                        {
                            func11 = () => BclHelpers.ReadTimeSpan(reader);
                        }
                        list.Add(func11);
                        break;

                    default:
                        throw new NotSupportedException(type.ToString());
                }
                ProtoReader.EndSubItem(token, reader);
                if (!table.Columns.Contains(str2))
                {
                    table.Columns.Add(str2, type2);
                }
                if (!string.IsNullOrEmpty(str3))
                {
                    table.Columns[str2].Caption = str3;
                }
                array = null;
                continue;
            Label_03C0:
                Array.Clear(array, 0, array.Length);
            Label_03CA:
                token = ProtoReader.StartSubItem(reader);
                while ((num = reader.ReadFieldHeader()) != 0)
                {
                    if (num > array.Length)
                    {
                        reader.SkipField();
                    }
                    else
                    {
                        int index = num - 1;
                        array[index] = list[index]();
                    }
                }
                ProtoReader.EndSubItem(token, reader);
                table.Rows.Add(array);
                continue;
            Label_0432:
                reader.SkipField();
            }
            if (!flag)
            {
                ds.Tables.Add(table);
            }
            return table;
        }

        public static void ProtoWrite(DataSet dataSet, Stream stream)
        {
            using (ProtoWriter writer = new ProtoWriter(stream, null, null))
            {
                if (dataSet != null)
                {
                    using (Stream stream2 = new MemoryStream())
                    {
                        dataSet.WriteXmlSchema(stream2);
                        stream2.Seek(0L, SeekOrigin.Begin);
                        StreamReader reader = new StreamReader(stream2, Encoding.UTF8);
                        string str = reader.ReadToEnd();
                        reader.Close();
                        ProtoWriter.WriteFieldHeader(4, WireType.String, writer);
                        ProtoWriter.WriteString(str, writer);
                    }
                    if (dataSet.Tables.Count > 0)
                    {
                        foreach (DataTable table in dataSet.Tables)
                        {
                            ProtoWriter.WriteFieldHeader(1, WireType.StartGroup, writer);
                            SubItemToken token = ProtoWriter.StartSubItem(table, writer);
                            ProtoWriteTable(table, writer);
                            ProtoWriter.EndSubItem(token, writer);
                        }
                    }
                    ProtoWriter.WriteFieldHeader(3, WireType.String, writer);
                    ProtoWriter.WriteString(dataSet.DataSetName, writer);
                }
            }
        }

        private static void ProtoWriteRelation(string relationName, string parentTableName, string parentColumns, string childTableName, string childColumns, ProtoWriter writer)
        {
            ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
            ProtoWriter.WriteString(relationName, writer);
            ProtoWriter.WriteFieldHeader(2, WireType.String, writer);
            ProtoWriter.WriteString(parentTableName, writer);
            ProtoWriter.WriteFieldHeader(3, WireType.String, writer);
            ProtoWriter.WriteString(parentColumns, writer);
            ProtoWriter.WriteFieldHeader(4, WireType.String, writer);
            ProtoWriter.WriteString(childTableName, writer);
            ProtoWriter.WriteFieldHeader(5, WireType.String, writer);
            ProtoWriter.WriteString(childColumns, writer);
        }

        private static void ProtoWriteTable(DataTable table, ProtoWriter writer)
        {
            if (string.IsNullOrEmpty(table.TableName))
            {
                table.TableName = Guid.NewGuid().ToString();
            }
            ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
            ProtoWriter.WriteString(table.TableName, writer);
            DataColumnCollection columns = table.Columns;
            Action<object>[] actionArray = new Action<object>[columns.Count];
            int index = 0;
            foreach (DataColumn column in columns)
            {
                MappedType boolean;
                Action<object> action;
                Action<object> action2 = null;
                Action<object> action3 = null;
                Action<object> action4 = null;
                Action<object> action5 = null;
                Action<object> action6 = null;
                Action<object> action7 = null;
                Action<object> action8 = null;
                Action<object> action9 = null;
                Action<object> action10 = null;
                Action<object> action11 = null;
                Action<object> action12 = null;
                ProtoWriter.WriteFieldHeader(2, WireType.StartGroup, writer);
                SubItemToken token = ProtoWriter.StartSubItem(column, writer);
                ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                ProtoWriter.WriteString(column.ColumnName, writer);
                ProtoWriter.WriteFieldHeader(2, WireType.Variant, writer);
                switch (Type.GetTypeCode(column.DataType))
                {
                    case TypeCode.Boolean:
                        boolean = MappedType.Boolean;
                        break;

                    case TypeCode.Byte:
                        boolean = MappedType.Byte;
                        break;

                    case TypeCode.Int16:
                        boolean = MappedType.Int16;
                        break;

                    case TypeCode.Int32:
                        boolean = MappedType.Int32;
                        break;

                    case TypeCode.Int64:
                        boolean = MappedType.Int64;
                        break;

                    case TypeCode.Double:
                        boolean = MappedType.Double;
                        break;

                    case TypeCode.Decimal:
                        boolean = MappedType.Decimal;
                        break;

                    case TypeCode.DateTime:
                        boolean = MappedType.DateTime;
                        break;

                    case TypeCode.String:
                        boolean = MappedType.String;
                        break;

                    default:
                        if (column.DataType == typeof(Guid))
                        {
                            boolean = MappedType.Guid;
                        }
                        else if (column.DataType == typeof(byte[]))
                        {
                            boolean = MappedType.Byte;
                        }
                        else
                        {
                            if (column.DataType != typeof(TimeSpan))
                            {
                                throw new NotSupportedException(column.DataType.Name);
                            }
                            boolean = MappedType.TimeSpan;
                        }
                        break;
                }
                ProtoWriter.WriteInt32((int) boolean, writer);
                ProtoWriter.WriteFieldHeader(3, WireType.String, writer);
                ProtoWriter.WriteString(column.Caption, writer);
                ProtoWriter.EndSubItem(token, writer);
                int field = index + 1;
                switch (boolean)
                {
                    case MappedType.Boolean:
                        if (action2 == null)
                        {
                            action2 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                ProtoWriter.WriteBoolean(Convert.ToBoolean(value), writer);
                            };
                        }
                        action = action2;
                        break;

                    case MappedType.Byte:
                        if (action3 == null)
                        {
                            action3 = delegate (object value) {
                                byte[] inArray = value as byte[];
                                string str = Convert.ToBase64String(inArray);
                                ProtoWriter.WriteFieldHeader(field, WireType.String, writer);
                                ProtoWriter.WriteString(str, writer);
                            };
                        }
                        action = action3;
                        break;

                    case MappedType.Double:
                        if (action4 == null)
                        {
                            action4 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.Fixed64, writer);
                                ProtoWriter.WriteDouble(Convert.ToDouble(value), writer);
                            };
                        }
                        action = action4;
                        break;

                    case MappedType.Int16:
                        if (action6 == null)
                        {
                            action6 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                ProtoWriter.WriteInt16((short) value, writer);
                            };
                        }
                        action = action6;
                        break;

                    case MappedType.Int32:
                        if (action8 == null)
                        {
                            action8 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                ProtoWriter.WriteInt32(Convert.ToInt32(value), writer);
                            };
                        }
                        action = action8;
                        break;

                    case MappedType.Int64:
                        if (action9 == null)
                        {
                            action9 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                ProtoWriter.WriteInt64((long) value, writer);
                            };
                        }
                        action = action9;
                        break;

                    case MappedType.String:
                        if (action5 == null)
                        {
                            action5 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.String, writer);
                                ProtoWriter.WriteString(Convert.ToString(value), writer);
                            };
                        }
                        action = action5;
                        break;

                    case MappedType.Decimal:
                        if (action7 == null)
                        {
                            action7 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                BclHelpers.WriteDecimal(Convert.ToDecimal(value), writer);
                            };
                        }
                        action = action7;
                        break;

                    case MappedType.Guid:
                        if (action10 == null)
                        {
                            action10 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                BclHelpers.WriteGuid((Guid) value, writer);
                            };
                        }
                        action = action10;
                        break;

                    case MappedType.DateTime:
                        if (action11 == null)
                        {
                            action11 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                BclHelpers.WriteDateTime(Convert.ToDateTime(value), writer);
                            };
                        }
                        action = action11;
                        break;

                    case MappedType.TimeSpan:
                        if (action12 == null)
                        {
                            action12 = delegate (object value) {
                                ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                if (value is TimeSpan)
                                {
                                    BclHelpers.WriteTimeSpan((TimeSpan) value, writer);
                                }
                                else
                                {
                                    BclHelpers.WriteTimeSpan(TimeSpan.Zero, writer);
                                }
                            };
                        }
                        action = action12;
                        break;

                    default:
                        throw new NotSupportedException(column.DataType.Name);
                }
                actionArray[index++] = action;
            }
            foreach (DataRow row in table.Rows)
            {
                index = 0;
                ProtoWriter.WriteFieldHeader(3, WireType.StartGroup, writer);
                SubItemToken token2 = ProtoWriter.StartSubItem(row, writer);
                foreach (DataColumn column2 in columns)
                {
                    object obj2 = row[column2];
                    if ((obj2 != null) && !(obj2 is DBNull))
                    {
                        actionArray[index](obj2);
                    }
                    index++;
                }
                ProtoWriter.EndSubItem(token2, writer);
            }
        }

        private static byte[] ReadBytes(ProtoReader reader)
        {
            return Convert.FromBase64String(reader.ReadString());
        }
    }
}

