namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using System;
    using System.Runtime.InteropServices;

    internal sealed class EnumSerializer : IProtoSerializer
    {
        private readonly Type enumType;
        private readonly EnumPair[] map;

        public EnumSerializer(Type enumType, EnumPair[] map)
        {
            if (enumType == null)
            {
                throw new ArgumentNullException("enumType");
            }
            this.enumType = enumType;
            this.map = map;
            if (map != null)
            {
                for (int i = 1; i < map.Length; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if ((map[i].WireValue == map[j].WireValue) && !object.Equals(map[i].RawValue, map[j].RawValue))
                        {
                            throw new ProtoException("Multiple enums with wire-value " + map[i].WireValue.ToString());
                        }
                        if (object.Equals(map[i].RawValue, map[j].RawValue) && (map[i].WireValue != map[j].WireValue))
                        {
                            throw new ProtoException("Multiple enums with deserialized-value " + map[i].RawValue);
                        }
                    }
                }
            }
        }

        private int EnumToWire(object value)
        {
            switch (this.GetTypeCode())
            {
                case ProtoTypeCode.SByte:
                    return (sbyte) value;

                case ProtoTypeCode.Byte:
                    return (byte) value;

                case ProtoTypeCode.Int16:
                    return (short) value;

                case ProtoTypeCode.UInt16:
                    return (ushort) value;

                case ProtoTypeCode.Int32:
                    return (int) value;

                case ProtoTypeCode.UInt32:
                    return (int) ((uint) value);

                case ProtoTypeCode.Int64:
                    return (int) ((long) value);

                case ProtoTypeCode.UInt64:
                    return (int) ((ulong) value);
            }
            throw new InvalidOperationException();
        }

        private ProtoTypeCode GetTypeCode()
        {
            Type underlyingType = Helpers.GetUnderlyingType(this.enumType);
            if (underlyingType == null)
            {
                underlyingType = this.enumType;
            }
            return Helpers.GetTypeCode(underlyingType);
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ProtoTypeCode typeCode = this.GetTypeCode();
            if (this.map == null)
            {
                ctx.EmitBasicRead("ReadInt32", ctx.MapType(typeof(int)));
                ctx.ConvertFromInt32(typeCode, false);
            }
            else
            {
                int[] keys = new int[this.map.Length];
                object[] values = new object[this.map.Length];
                for (int i = 0; i < this.map.Length; i++)
                {
                    keys[i] = this.map[i].WireValue;
                    values[i] = this.map[i].RawValue;
                }
                using (Local local = new Local(ctx, this.ExpectedType))
                {
                    using (Local local2 = new Local(ctx, ctx.MapType(typeof(int))))
                    {
                        ctx.EmitBasicRead("ReadInt32", ctx.MapType(typeof(int)));
                        ctx.StoreValue(local2);
                        CodeLabel label = ctx.DefineLabel();
                        BasicList.NodeEnumerator enumerator = BasicList.GetContiguousGroups(keys, values).GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            BasicList.Group current = (BasicList.Group) enumerator.Current;
                            CodeLabel label2 = ctx.DefineLabel();
                            int count = current.Items.Count;
                            if (count == 1)
                            {
                                ctx.LoadValue(local2);
                                ctx.LoadValue(current.First);
                                CodeLabel label3 = ctx.DefineLabel();
                                ctx.BranchIfEqual(label3, true);
                                ctx.Branch(label2, false);
                                WriteEnumValue(ctx, typeCode, label3, label, current.Items[0], local);
                            }
                            else
                            {
                                ctx.LoadValue(local2);
                                ctx.LoadValue(current.First);
                                ctx.Subtract();
                                CodeLabel[] jumpTable = new CodeLabel[count];
                                for (int j = 0; j < count; j++)
                                {
                                    jumpTable[j] = ctx.DefineLabel();
                                }
                                ctx.Switch(jumpTable);
                                ctx.Branch(label2, false);
                                for (int k = 0; k < count; k++)
                                {
                                    WriteEnumValue(ctx, typeCode, jumpTable[k], label, current.Items[k], local);
                                }
                            }
                            ctx.MarkLabel(label2);
                        }
                        ctx.LoadReaderWriter();
                        ctx.LoadValue(this.ExpectedType);
                        ctx.LoadValue(local2);
                        ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("ThrowEnumException"));
                        ctx.MarkLabel(label);
                        ctx.LoadValue(local);
                    }
                }
            }
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ProtoTypeCode typeCode = this.GetTypeCode();
            if (this.map == null)
            {
                ctx.LoadValue(valueFrom);
                ctx.ConvertToInt32(typeCode, false);
                ctx.EmitBasicWrite("WriteInt32", null);
            }
            else
            {
                using (Local local = ctx.GetLocalWithValue(this.ExpectedType, valueFrom))
                {
                    CodeLabel label = ctx.DefineLabel();
                    for (int i = 0; i < this.map.Length; i++)
                    {
                        CodeLabel label2 = ctx.DefineLabel();
                        CodeLabel label3 = ctx.DefineLabel();
                        ctx.LoadValue(local);
                        WriteEnumValue(ctx, typeCode, this.map[i].RawValue);
                        ctx.BranchIfEqual(label3, true);
                        ctx.Branch(label2, true);
                        ctx.MarkLabel(label3);
                        ctx.LoadValue(this.map[i].WireValue);
                        ctx.EmitBasicWrite("WriteInt32", null);
                        ctx.Branch(label, false);
                        ctx.MarkLabel(label2);
                    }
                    ctx.LoadReaderWriter();
                    ctx.LoadValue(local);
                    ctx.CastToObject(this.ExpectedType);
                    ctx.EmitCall(ctx.MapType(typeof(ProtoWriter)).GetMethod("ThrowEnumException"));
                    ctx.MarkLabel(label);
                }
            }
        }

        public object Read(object value, ProtoReader source)
        {
            int num = source.ReadInt32();
            if (this.map == null)
            {
                return this.WireToEnum(num);
            }
            for (int i = 0; i < this.map.Length; i++)
            {
                if (this.map[i].WireValue == num)
                {
                    return this.map[i].TypedValue;
                }
            }
            source.ThrowEnumException(this.ExpectedType, num);
            return null;
        }

        private object WireToEnum(int value)
        {
            switch (this.GetTypeCode())
            {
                case ProtoTypeCode.SByte:
                    return Enum.ToObject(this.enumType, (sbyte) value);

                case ProtoTypeCode.Byte:
                    return Enum.ToObject(this.enumType, (byte) value);

                case ProtoTypeCode.Int16:
                    return Enum.ToObject(this.enumType, (short) value);

                case ProtoTypeCode.UInt16:
                    return Enum.ToObject(this.enumType, (ushort) value);

                case ProtoTypeCode.Int32:
                    return Enum.ToObject(this.enumType, value);

                case ProtoTypeCode.UInt32:
                    return Enum.ToObject(this.enumType, (uint) value);

                case ProtoTypeCode.Int64:
                    return Enum.ToObject(this.enumType, (long) value);

                case ProtoTypeCode.UInt64:
                    return Enum.ToObject(this.enumType, (ulong) value);
            }
            throw new InvalidOperationException();
        }

        public void Write(object value, ProtoWriter dest)
        {
            if (this.map == null)
            {
                ProtoWriter.WriteInt32(this.EnumToWire(value), dest);
            }
            else
            {
                for (int i = 0; i < this.map.Length; i++)
                {
                    if (object.Equals(this.map[i].TypedValue, value))
                    {
                        ProtoWriter.WriteInt32(this.map[i].WireValue, dest);
                        return;
                    }
                }
                ProtoWriter.ThrowEnumException(dest, value);
            }
        }

        private static void WriteEnumValue(CompilerContext ctx, ProtoTypeCode typeCode, object value)
        {
            switch (typeCode)
            {
                case ProtoTypeCode.SByte:
                    ctx.LoadValue((int) ((sbyte) value));
                    return;

                case ProtoTypeCode.Byte:
                    ctx.LoadValue((int) ((byte) value));
                    return;

                case ProtoTypeCode.Int16:
                    ctx.LoadValue((int) ((short) value));
                    return;

                case ProtoTypeCode.UInt16:
                    ctx.LoadValue((int) ((ushort) value));
                    return;

                case ProtoTypeCode.Int32:
                    ctx.LoadValue((int) value);
                    return;

                case ProtoTypeCode.UInt32:
                    ctx.LoadValue((int) ((uint) value));
                    return;

                case ProtoTypeCode.Int64:
                    ctx.LoadValue((long) value);
                    return;

                case ProtoTypeCode.UInt64:
                    ctx.LoadValue((long) ((ulong) value));
                    return;
            }
            throw new InvalidOperationException();
        }

        private static void WriteEnumValue(CompilerContext ctx, ProtoTypeCode typeCode, CodeLabel handler, CodeLabel @continue, object value, Local local)
        {
            ctx.MarkLabel(handler);
            WriteEnumValue(ctx, typeCode, value);
            ctx.StoreValue(local);
            ctx.Branch(@continue, false);
        }

        public Type ExpectedType
        {
            get
            {
                return this.enumType;
            }
        }

        bool IProtoSerializer.RequiresOldValue
        {
            get
            {
                return false;
            }
        }

        bool IProtoSerializer.ReturnsValue
        {
            get
            {
                return true;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EnumPair
        {
            public readonly object RawValue;
            public readonly Enum TypedValue;
            public readonly int WireValue;
            public EnumPair(int wireValue, object raw, Type type)
            {
                this.WireValue = wireValue;
                this.RawValue = raw;
                this.TypedValue = (Enum) Enum.ToObject(type, raw);
            }
        }
    }
}

