namespace Components.Serialize.Protobuf.Protobuf
{
    using System;
    using System.Runtime.Serialization;

    public static class BclHelpers
    {
        internal static readonly DateTime EpochOrigin = new DateTime(0x7b2, 1, 1, 0, 0, 0, 0);
        private const int FieldDecimalHigh = 2;
        private const int FieldDecimalLow = 1;
        private const int FieldDecimalSignScale = 3;
        private const int FieldExistingObjectKey = 1;
        private const int FieldExistingTypeKey = 3;
        private const int FieldGuidHigh = 2;
        private const int FieldGuidLow = 1;
        private const int FieldNewObjectKey = 2;
        private const int FieldNewTypeKey = 4;
        private const int FieldObject = 10;
        private const int FieldTimeSpanScale = 2;
        private const int FieldTimeSpanValue = 1;
        private const int FieldTypeName = 8;

        public static object GetUninitializedObject(Type type)
        {
            return FormatterServices.GetUninitializedObject(type);
        }

        public static DateTime ReadDateTime(ProtoReader source)
        {
            long num = ReadTimeSpanTicks(source);
            switch (num)
            {
                case -9223372036854775808L:
                    return DateTime.MinValue;

                case 0x7fffffffffffffffL:
                    return DateTime.MaxValue;
            }
            return EpochOrigin.AddTicks(num);
        }

        public static decimal ReadDecimal(ProtoReader reader)
        {
            int num4;
            ulong num = 0L;
            uint num2 = 0;
            uint num3 = 0;
            SubItemToken token = ProtoReader.StartSubItem(reader);
            while ((num4 = reader.ReadFieldHeader()) > 0)
            {
                switch (num4)
                {
                    case 1:
                        {
                            num = reader.ReadUInt64();
                            continue;
                        }
                    case 2:
                        {
                            num2 = reader.ReadUInt32();
                            continue;
                        }
                    case 3:
                        {
                            num3 = reader.ReadUInt32();
                            continue;
                        }
                }
                reader.SkipField();
            }
            ProtoReader.EndSubItem(token, reader);
            if ((num == 0L) && (num2 == 0))
            {
                return 0M;
            }
            int lo = (int)(num & 0xffffffffL);
            int mid = (int)((num >> 0x20) & 0xffffffffL);
            int hi = (int)num2;
            bool isNegative = (num3 & 1) == 1;
            return new decimal(lo, mid, hi, isNegative, (byte)((num3 & 510) >> 1));
        }

        public static Guid ReadGuid(ProtoReader source)
        {
            int num3;
            ulong num = 0L;
            ulong num2 = 0L;
            SubItemToken token = ProtoReader.StartSubItem(source);
            while ((num3 = source.ReadFieldHeader()) > 0)
            {
                switch (num3)
                {
                    case 1:
                        {
                            num = source.ReadUInt64();
                            continue;
                        }
                    case 2:
                        {
                            num2 = source.ReadUInt64();
                            continue;
                        }
                }
                source.SkipField();
            }
            ProtoReader.EndSubItem(token, source);
            if ((num == 0L) && (num2 == 0L))
            {
                return Guid.Empty;
            }
            uint num4 = (uint)(num >> 0x20);
            uint num5 = (uint)num;
            uint num6 = (uint)(num2 >> 0x20);
            uint num7 = (uint)num2;
            return new Guid((int)num5, (short)num4, (short)(num4 >> 0x10), (byte)num7, (byte)(num7 >> 8), (byte)(num7 >> 0x10), (byte)(num7 >> 0x18), (byte)num6, (byte)(num6 >> 8), (byte)(num6 >> 0x10), (byte)(num6 >> 0x18));
        }

        public static object ReadNetObject(object value, ProtoReader source, int key, Type type, NetObjectOptions options)
        {
            int num;
            SubItemToken token = ProtoReader.StartSubItem(source);
            int newObjectKey = -1;
            int num3 = -1;
            while ((num = source.ReadFieldHeader()) > 0)
            {
                int num4;
                bool flag;
                bool flag2;
                bool flag3;
                object keyedObject;
                switch (num)
                {
                    case 1:
                        {
                            num4 = source.ReadInt32();
                            value = source.NetCache.GetKeyedObject(num4);
                            continue;
                        }
                    case 2:
                        {
                            newObjectKey = source.ReadInt32();
                            continue;
                        }
                    case 3:
                        {
                            num4 = source.ReadInt32();
                            type = (Type)source.NetCache.GetKeyedObject(num4);
                            key = source.GetTypeKey(ref type);
                            continue;
                        }
                    case 4:
                        {
                            num3 = source.ReadInt32();
                            continue;
                        }
                    case 8:
                        {
                            string str = source.ReadString();
                            type = source.DeserializeType(str);
                            if (type == null)
                            {
                                throw new ProtoException("Unable to resolve type: " + str + " (you can use the TypeModel.DynamicTypeFormatting event to provide a custom mapping)");
                            }
                            break;
                        }
                    case 10:
                        flag = type == typeof(string);
                        flag2 = value == null;
                        flag3 = flag2 && (flag || (((byte)(options & NetObjectOptions.LateSet)) != 0));
                        if ((newObjectKey < 0) || flag3)
                        {
                            goto Label_0180;
                        }
                        if (value != null)
                        {
                            goto Label_0162;
                        }
                        source.TrapNextObject(newObjectKey);
                        goto Label_016F;

                    default:
                        goto Label_020D;
                }
                if (type == typeof(string))
                {
                    key = -1;
                    continue;
                }
                key = source.GetTypeKey(ref type);
                if (key >= 0)
                {
                    continue;
                }
                throw new InvalidOperationException("Dynamic type is not a contract-type: " + type.Name);
            Label_0162:
                source.NetCache.SetKeyedObject(newObjectKey, value);
            Label_016F:
                if (num3 >= 0)
                {
                    source.NetCache.SetKeyedObject(num3, type);
                }
            Label_0180:
                keyedObject = value;
                if (flag)
                {
                    value = source.ReadString();
                }
                else
                {
                    value = ProtoReader.ReadTypedObject(keyedObject, key, source, type);
                }
                if (newObjectKey >= 0)
                {
                    if (flag2 && !flag3)
                    {
                        keyedObject = source.NetCache.GetKeyedObject(newObjectKey);
                    }
                    if (flag3)
                    {
                        source.NetCache.SetKeyedObject(newObjectKey, value);
                        if (num3 >= 0)
                        {
                            source.NetCache.SetKeyedObject(num3, type);
                        }
                    }
                }
                if (((newObjectKey >= 0) && !flag3) && !object.ReferenceEquals(keyedObject, value))
                {
                    throw new ProtoException("A reference-tracked object changed reference during deserialization");
                }
                if ((newObjectKey < 0) && (num3 >= 0))
                {
                    source.NetCache.SetKeyedObject(num3, type);
                }
                continue;
            Label_020D:
                source.SkipField();
            }
            if ((newObjectKey >= 0) && (((byte)(options & NetObjectOptions.AsReference)) == 0))
            {
                throw new ProtoException("Object key in input stream, but reference-tracking was not expected");
            }
            ProtoReader.EndSubItem(token, source);
            return value;
        }

        public static TimeSpan ReadTimeSpan(ProtoReader source)
        {
            long num = ReadTimeSpanTicks(source);
            switch (num)
            {
                case -9223372036854775808L:
                    return TimeSpan.MinValue;

                case 0x7fffffffffffffffL:
                    return TimeSpan.MaxValue;
            }
            return TimeSpan.FromTicks(num);
        }

        private static long ReadTimeSpanTicks(ProtoReader source)
        {
            long num2;
            switch (source.WireType)
            {
                case WireType.Fixed64:
                    return source.ReadInt64();

                case WireType.String:
                case WireType.StartGroup:
                    {
                        int num;
                        SubItemToken token = ProtoReader.StartSubItem(source);
                        TimeSpanScale days = TimeSpanScale.Days;
                        num2 = 0L;
                    Label_0065:
                        if ((num = source.ReadFieldHeader()) <= 0)
                        {
                            ProtoReader.EndSubItem(token, source);
                            switch (days)
                            {
                                case TimeSpanScale.Days:
                                    return (num2 * 0xc92a69c000L);

                                case TimeSpanScale.Hours:
                                    return (num2 * 0x861c46800L);

                                case TimeSpanScale.Minutes:
                                    return (num2 * 0x23c34600L);

                                case TimeSpanScale.Seconds:
                                    return (num2 * 0x989680L);

                                case TimeSpanScale.Milliseconds:
                                    return (num2 * 0x2710L);

                                case TimeSpanScale.Ticks:
                                    return num2;

                                case TimeSpanScale.MinMax:
                                    {
                                        long num4 = num2;
                                        if ((num4 <= 1L) && (num4 >= -1L))
                                        {
                                            switch (((int)(num4 - -1L)))
                                            {
                                                case 0:
                                                    return -9223372036854775808L;

                                                case 2:
                                                    return 0x7fffffffffffffffL;
                                            }
                                        }
                                        goto Label_0115;
                                    }
                            }
                            throw new ProtoException("Unknown timescale: " + days.ToString());
                        }
                        switch (num)
                        {
                            case 1:
                                source.Assert(WireType.SignedVariant);
                                num2 = source.ReadInt64();
                                goto Label_0065;

                            case 2:
                                days = (TimeSpanScale)source.ReadInt32();
                                goto Label_0065;
                        }
                        source.SkipField();
                        goto Label_0065;
                    }
                default:
                    throw new ProtoException("Unexpected wire-type: " + source.WireType.ToString());
            }
        Label_0115:
            throw new ProtoException("Unknown min/max value: " + num2.ToString());
        }

        public static void WriteDateTime(DateTime value, ProtoWriter dest)
        {
            TimeSpan maxValue;
            if (dest == null)
            {
                throw new ArgumentNullException("dest");
            }
            switch (dest.WireType)
            {
                case WireType.String:
                case WireType.StartGroup:
                    if (!(value == DateTime.MaxValue))
                    {
                        if (value == DateTime.MinValue)
                        {
                            maxValue = TimeSpan.MinValue;
                        }
                        else
                        {
                            maxValue = (TimeSpan)(value - EpochOrigin);
                        }
                        break;
                    }
                    maxValue = TimeSpan.MaxValue;
                    break;

                default:
                    maxValue = (TimeSpan)(value - EpochOrigin);
                    break;
            }
            WriteTimeSpan(maxValue, dest);
        }

        public static void WriteDecimal(decimal value, ProtoWriter writer)
        {
            int[] bits = decimal.GetBits(value);
            ulong num = ((ulong)bits[1]) << 0x20;
            ulong num2 = (ulong)(bits[0] & 0xffffffffL);
            ulong num3 = num | num2;
            uint num4 = (uint)bits[2];
            uint num5 = (uint)(((bits[3] >> 15) & 510) | ((bits[3] >> 0x1f) & 1));
            SubItemToken token = ProtoWriter.StartSubItem(null, writer);
            if (num3 != 0L)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer);
                ProtoWriter.WriteUInt64(num3, writer);
            }
            if (num4 != 0)
            {
                ProtoWriter.WriteFieldHeader(2, WireType.Variant, writer);
                ProtoWriter.WriteUInt32(num4, writer);
            }
            if (num5 != 0)
            {
                ProtoWriter.WriteFieldHeader(3, WireType.Variant, writer);
                ProtoWriter.WriteUInt32(num5, writer);
            }
            ProtoWriter.EndSubItem(token, writer);
        }

        public static void WriteGuid(Guid value, ProtoWriter dest)
        {
            byte[] data = value.ToByteArray();
            SubItemToken token = ProtoWriter.StartSubItem(null, dest);
            if (value != Guid.Empty)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Fixed64, dest);
                ProtoWriter.WriteBytes(data, 0, 8, dest);
                ProtoWriter.WriteFieldHeader(2, WireType.Fixed64, dest);
                ProtoWriter.WriteBytes(data, 8, 8, dest);
            }
            ProtoWriter.EndSubItem(token, dest);
        }

        public static void WriteNetObject(object value, ProtoWriter dest, int key, NetObjectOptions options)
        {
            if (dest == null)
            {
                throw new ArgumentNullException("dest");
            }
            bool flag = ((byte)(options & NetObjectOptions.DynamicType)) != 0;
            bool flag2 = ((byte)(options & NetObjectOptions.AsReference)) != 0;
            WireType wireType = dest.WireType;
            SubItemToken token = ProtoWriter.StartSubItem(null, dest);
            bool flag3 = true;
            if (flag2)
            {
                bool flag4;
                int num = dest.NetCache.AddObjectKey(value, out flag4);
                ProtoWriter.WriteFieldHeader(flag4 ? 1 : 2, WireType.Variant, dest);
                ProtoWriter.WriteInt32(num, dest);
                if (flag4)
                {
                    flag3 = false;
                }
            }
            if (flag3)
            {
                if (flag)
                {
                    bool flag5;
                    Type type = value.GetType();
                    if (!(value is string))
                    {
                        key = dest.GetTypeKey(ref type);
                        if (key < 0)
                        {
                            throw new InvalidOperationException("Dynamic type is not a contract-type: " + type.Name);
                        }
                    }
                    int num2 = dest.NetCache.AddObjectKey(type, out flag5);
                    ProtoWriter.WriteFieldHeader(flag5 ? 3 : 4, WireType.Variant, dest);
                    ProtoWriter.WriteInt32(num2, dest);
                    if (!flag5)
                    {
                        ProtoWriter.WriteFieldHeader(8, WireType.String, dest);
                        ProtoWriter.WriteString(dest.SerializeType(type), dest);
                    }
                }
                ProtoWriter.WriteFieldHeader(10, wireType, dest);
                if (value is string)
                {
                    ProtoWriter.WriteString((string)value, dest);
                }
                else
                {
                    ProtoWriter.WriteObject(value, key, dest);
                }
            }
            ProtoWriter.EndSubItem(token, dest);
        }

        public static void WriteTimeSpan(TimeSpan timeSpan, ProtoWriter dest)
        {
            long ticks;
            TimeSpanScale minMax;
            if (dest == null)
            {
                throw new ArgumentNullException("dest");
            }
            switch (dest.WireType)
            {
                case WireType.Fixed64:
                    ProtoWriter.WriteInt64(timeSpan.Ticks, dest);
                    return;

                case WireType.String:
                case WireType.StartGroup:
                    ticks = timeSpan.Ticks;
                    if (!(timeSpan == TimeSpan.MaxValue))
                    {
                        if (timeSpan == TimeSpan.MinValue)
                        {
                            ticks = -1L;
                            minMax = TimeSpanScale.MinMax;
                        }
                        else if ((ticks % 0xc92a69c000L) == 0L)
                        {
                            minMax = TimeSpanScale.Days;
                            ticks /= 0xc92a69c000L;
                        }
                        else if ((ticks % 0x861c46800L) == 0L)
                        {
                            minMax = TimeSpanScale.Hours;
                            ticks /= 0x861c46800L;
                        }
                        else if ((ticks % 0x23c34600L) == 0L)
                        {
                            minMax = TimeSpanScale.Minutes;
                            ticks /= 0x23c34600L;
                        }
                        else if ((ticks % 0x989680L) == 0L)
                        {
                            minMax = TimeSpanScale.Seconds;
                            ticks /= 0x989680L;
                        }
                        else if ((ticks % 0x2710L) == 0L)
                        {
                            minMax = TimeSpanScale.Milliseconds;
                            ticks /= 0x2710L;
                        }
                        else
                        {
                            minMax = TimeSpanScale.Ticks;
                        }
                        break;
                    }
                    ticks = 1L;
                    minMax = TimeSpanScale.MinMax;
                    break;

                default:
                    throw new ProtoException("Unexpected wire-type: " + dest.WireType.ToString());
            }
            SubItemToken token = ProtoWriter.StartSubItem(null, dest);
            if (ticks != 0L)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.SignedVariant, dest);
                ProtoWriter.WriteInt64(ticks, dest);
            }
            if (minMax != TimeSpanScale.Days)
            {
                ProtoWriter.WriteFieldHeader(2, WireType.Variant, dest);
                ProtoWriter.WriteInt32((int)minMax, dest);
            }
            ProtoWriter.EndSubItem(token, dest);
        }

        [Flags]
        public enum NetObjectOptions : byte
        {
            AsReference = 1,
            DynamicType = 2,
            LateSet = 8,
            None = 0,
            UseConstructor = 4
        }
    }
}

