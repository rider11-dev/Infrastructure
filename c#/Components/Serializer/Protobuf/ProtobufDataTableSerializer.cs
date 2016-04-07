namespace Components.Serialize.Protobuf
{
    using Components.Extensions;
    using Components.Serialize.Protobuf.Protobuf;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    internal static class ProtobufDataTableSerializer
    {
        public static DataTable ProtoRead(Stream stream)
        {
            DataTable table = new DataTable();
            object[] array = null;
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
            using (ProtoReader reader = new ProtoReader(stream, null, null))
            {
                int num;
                List<Func<object>> list = new List<Func<object>>();
                while ((num = reader.ReadFieldHeader()) != 0)
                {
                    SubItemToken token;
                    string str;
                    string str2;
                    MappedType type;
                    Type type2;
                    switch (num)
                    {
                        case 1:
                            {
                                table.TableName = reader.ReadString();
                                continue;
                            }
                        case 2:
                            str = null;
                            str2 = string.Empty;
                            type = ~MappedType.Boolean;
                            token = ProtoReader.StartSubItem(reader);
                            goto Label_00F1;

                        case 3:
                            if (array != null)
                            {
                                goto Label_0382;
                            }
                            array = new object[table.Columns.Count];
                            goto Label_038C;

                        default:
                            goto Label_03F3;
                    }
                Label_009B:
                    switch (num)
                    {
                        case 1:
                            str = reader.ReadString();
                            break;

                        case 2:
                            type = (MappedType)reader.ReadInt32();
                            break;

                        case 3:
                            str2 = reader.ReadString();
                            break;

                        default:
                            reader.SkipField();
                            break;
                    }
                Label_00F1:
                    if ((num = reader.ReadFieldHeader()) != 0)
                    {
                        goto Label_009B;
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
                    table.Columns.Add(str, type2);
                    if (!string.IsNullOrEmpty(str2))
                    {
                        table.Columns[str].Caption = str2;
                    }
                    array = null;
                    continue;
                Label_0382:
                    Array.Clear(array, 0, array.Length);
                Label_038C:
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
                Label_03F3:
                    reader.SkipField();
                }
            }
            return table;
        }

        public static void ProtoWrite(DataTable table, Stream stream)
        {
            using (ProtoWriter writer = new ProtoWriter(stream, null, null))
            {
                if (!string.IsNullOrEmpty(table.TableName))
                {
                    ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                    ProtoWriter.WriteString(table.TableName, writer);
                }
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
                    ProtoWriter.WriteInt32((int)boolean, writer);
                    ProtoWriter.WriteFieldHeader(3, WireType.String, writer);
                    ProtoWriter.WriteString(column.Caption, writer);
                    ProtoWriter.EndSubItem(token, writer);
                    int field = index + 1;
                    switch (boolean)
                    {
                        case MappedType.Boolean:
                            if (action2 == null)
                            {
                                action2 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                    ProtoWriter.WriteBoolean(Convert.ToBoolean(value), writer);
                                };
                            }
                            action = action2;
                            break;

                        case MappedType.Byte:
                            if (action3 == null)
                            {
                                action3 = delegate(object value)
                                {
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
                                action4 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.Fixed64, writer);
                                    ProtoWriter.WriteDouble(Convert.ToDouble(value), writer);
                                };
                            }
                            action = action4;
                            break;

                        case MappedType.Int16:
                            if (action7 == null)
                            {
                                action7 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                    ProtoWriter.WriteInt16((short)value, writer);
                                };
                            }
                            action = action7;
                            break;

                        case MappedType.Int32:
                            if (action8 == null)
                            {
                                action8 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                    ProtoWriter.WriteInt32(Convert.ToInt32(value), writer);
                                };
                            }
                            action = action8;
                            break;

                        case MappedType.Int64:
                            if (action9 == null)
                            {
                                action9 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.Variant, writer);
                                    ProtoWriter.WriteInt64((long)value, writer);
                                };
                            }
                            action = action9;
                            break;

                        case MappedType.String:
                            if (action5 == null)
                            {
                                action5 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.String, writer);
                                    ProtoWriter.WriteString(Convert.ToString(value), writer);
                                };
                            }
                            action = action5;
                            break;

                        case MappedType.Decimal:
                            if (action6 == null)
                            {
                                action6 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                    BclHelpers.WriteDecimal(Convert.ToDecimal(value), writer);
                                };
                            }
                            action = action6;
                            break;

                        case MappedType.Guid:
                            if (action10 == null)
                            {
                                action10 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                    BclHelpers.WriteGuid((Guid)value, writer);
                                };
                            }
                            action = action10;
                            break;

                        case MappedType.DateTime:
                            if (action11 == null)
                            {
                                action11 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                    BclHelpers.WriteDateTime(Convert.ToDateTime(value), writer);
                                };
                            }
                            action = action11;
                            break;

                        case MappedType.TimeSpan:
                            if (action12 == null)
                            {
                                action12 = delegate(object value)
                                {
                                    ProtoWriter.WriteFieldHeader(field, WireType.StartGroup, writer);
                                    if (value is TimeSpan)
                                    {
                                        BclHelpers.WriteTimeSpan((TimeSpan)value, writer);
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
        }

        private static byte[] ReadBytes(ProtoReader reader)
        {
            return Convert.FromBase64String(reader.ReadString());
        }

        private static object ReadObject(ProtoReader reader)
        {
            object obj = null;
            string str = reader.ReadString();
            if (str.IsNotEmpty())
            {
                IFormatter formatter = new BinaryFormatter();
                using (Stream stream = new MemoryStream(Convert.FromBase64String(reader.ReadString())))
                {
                    obj = formatter.Deserialize(stream);
                }
            }
            return obj;
        }
    }
}

