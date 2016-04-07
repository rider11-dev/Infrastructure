namespace Components.Serialize.Protobuf.Protobuf
{
    using Components.Serialize.Protobuf.Meta;
    using System;
    using System.IO;
    using System.Text;

    public sealed class ProtoWriter : IDisposable
    {
        private readonly SerializationContext context;
        private int depth;
        private Stream dest;
        private static readonly UTF8Encoding encoding = new UTF8Encoding();
        private int fieldNumber;
        private int flushLock;
        private byte[] ioBuffer;
        private int ioIndex;
        private TypeModel model;
        private readonly NetObjectCache netCache = new NetObjectCache();
        private int packedFieldNumber;
        private int position;
        private const int RecursionCheckDepth = 0x19;
        private MutableList recursionStack;
        private WireType wireType;

        public ProtoWriter(Stream dest, TypeModel model, SerializationContext context)
        {
            if (dest == null)
            {
                throw new ArgumentNullException("dest");
            }
            if (!dest.CanWrite)
            {
                throw new ArgumentException("Cannot write to stream", "dest");
            }
            this.dest = dest;
            this.ioBuffer = BufferPool.GetBuffer();
            this.model = model;
            this.wireType = WireType.None;
            if (context == null)
            {
                context = SerializationContext.Default;
            }
            else
            {
                context.Freeze();
            }
            this.context = context;
        }

        public static void AppendExtensionData(IExtensible instance, ProtoWriter writer)
        {
            if (instance == null)
            {
                throw new ArgumentNullException("instance");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (writer.wireType != WireType.None)
            {
                throw CreateException(writer);
            }
            IExtension extensionObject = instance.GetExtensionObject(false);
            if (extensionObject != null)
            {
                Stream source = extensionObject.BeginQuery();
                try
                {
                    CopyRawFromStream(source, writer);
                }
                finally
                {
                    extensionObject.EndQuery(source);
                }
            }
        }

        internal void CheckDepthFlushlock()
        {
            if ((this.depth != 0) || (this.flushLock != 0))
            {
                throw new InvalidOperationException("The writer is in an incomplete state");
            }
        }

        private void CheckRecursionStackAndPush(object instance)
        {
            if (this.recursionStack == null)
            {
                this.recursionStack = new MutableList();
            }
            else
            {
                int num;
                if ((instance != null) && ((num = this.recursionStack.IndexOfReference(instance)) >= 0))
                {
                    int num2 = this.recursionStack.Count - num;
                    throw new ProtoException("Possible recursion detected (offset: " + num2.ToString() + " level(s)): " + instance.ToString());
                }
            }
            this.recursionStack.Add(instance);
        }

        public void Close()
        {
            if ((this.depth != 0) || (this.flushLock != 0))
            {
                throw new InvalidOperationException("Unable to close stream in an incomplete state");
            }
            this.Dispose();
        }

        private static void CopyRawFromStream(Stream source, ProtoWriter writer)
        {
            byte[] ioBuffer = writer.ioBuffer;
            int count = ioBuffer.Length - writer.ioIndex;
            int num2 = 1;
            while ((count > 0) && ((num2 = source.Read(ioBuffer, writer.ioIndex, count)) > 0))
            {
                writer.ioIndex += num2;
                writer.position += num2;
                count -= num2;
            }
            if (num2 <= 0)
            {
                return;
            }
            if (writer.flushLock == 0)
            {
                Flush(writer);
                while ((num2 = source.Read(ioBuffer, 0, ioBuffer.Length)) > 0)
                {
                    writer.dest.Write(ioBuffer, 0, num2);
                    writer.position += num2;
                }
                return;
            }
            while (true)
            {
                DemandSpace(0x80, writer);
                num2 = source.Read(writer.ioBuffer, writer.ioIndex, writer.ioBuffer.Length - writer.ioIndex);
                if (num2 <= 0)
                {
                    return;
                }
                writer.position += num2;
                writer.ioIndex += num2;
            }
        }

        internal static Exception CreateException(ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            return new ProtoException("Invalid serialization operation with wire-type " + writer.wireType.ToString() + " at position " + writer.position.ToString());
        }

        private static void DemandSpace(int required, ProtoWriter writer)
        {
            if ((writer.ioBuffer.Length - writer.ioIndex) < required)
            {
                if (writer.flushLock == 0)
                {
                    Flush(writer);
                    if ((writer.ioBuffer.Length - writer.ioIndex) >= required)
                    {
                        return;
                    }
                }
                BufferPool.ResizeAndFlushLeft(ref writer.ioBuffer, required + writer.ioIndex, 0, writer.ioIndex);
            }
        }

        private void Dispose()
        {
            if (this.dest != null)
            {
                Flush(this);
                this.dest = null;
            }
            this.model = null;
            BufferPool.ReleaseBufferToPool(ref this.ioBuffer);
        }

        public static void EndSubItem(SubItemToken token, ProtoWriter writer)
        {
            EndSubItem(token, writer, PrefixStyle.Base128);
        }

        private static void EndSubItem(SubItemToken token, ProtoWriter writer, PrefixStyle style)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (writer.wireType != WireType.None)
            {
                throw CreateException(writer);
            }
            int index = token.value;
            if (writer.depth <= 0)
            {
                throw CreateException(writer);
            }
            if (writer.depth-- > 0x19)
            {
                writer.PopRecursionStack();
            }
            writer.packedFieldNumber = 0;
            if (index < 0)
            {
                WriteHeaderCore(-index, WireType.EndGroup, writer);
                writer.wireType = WireType.None;
            }
            else
            {
                int num2;
                switch (style)
                {
                    case PrefixStyle.Base128:
                        {
                            num2 = (writer.ioIndex - index) - 1;
                            int required = 0;
                            uint num5 = (uint)num2;
                            while ((num5 = num5 >> 7) != 0)
                            {
                                required++;
                            }
                            if (required == 0)
                            {
                                writer.ioBuffer[index] = (byte)(num2 & 0x7f);
                            }
                            else
                            {
                                DemandSpace(required, writer);
                                byte[] ioBuffer = writer.ioBuffer;
                                Helpers.BlockCopy(ioBuffer, index + 1, ioBuffer, (index + 1) + required, num2);
                                num5 = (uint)num2;
                                do
                                {
                                    ioBuffer[index++] = (byte)((num5 & 0x7f) | 0x80);
                                }
                                while ((num5 = num5 >> 7) != 0);
                                ioBuffer[index - 1] = (byte)(ioBuffer[index - 1] & -129);
                                writer.position += required;
                                writer.ioIndex += required;
                            }
                            break;
                        }
                    case PrefixStyle.Fixed32:
                        num2 = (writer.ioIndex - index) - 4;
                        WriteInt32ToBuffer(num2, writer.ioBuffer, index);
                        break;

                    case PrefixStyle.Fixed32BigEndian:
                        {
                            num2 = (writer.ioIndex - index) - 4;
                            byte[] buffer = writer.ioBuffer;
                            WriteInt32ToBuffer(num2, buffer, index);
                            byte num3 = buffer[index];
                            buffer[index] = buffer[index + 3];
                            buffer[index + 3] = num3;
                            num3 = buffer[index + 1];
                            buffer[index + 1] = buffer[index + 2];
                            buffer[index + 2] = num3;
                            break;
                        }
                    default:
                        throw new ArgumentOutOfRangeException("style");
                }
                if ((--writer.flushLock == 0) && (writer.ioIndex >= 0x400))
                {
                    Flush(writer);
                }
            }
        }

        internal static void Flush(ProtoWriter writer)
        {
            if ((writer.flushLock == 0) && (writer.ioIndex != 0))
            {
                writer.dest.Write(writer.ioBuffer, 0, writer.ioIndex);
                writer.ioIndex = 0;
            }
        }

        internal static int GetPosition(ProtoWriter writer)
        {
            return writer.position;
        }

        internal int GetTypeKey(ref Type type)
        {
            return this.model.GetKey(ref type);
        }

        private static void IncrementedAndReset(int length, ProtoWriter writer)
        {
            writer.ioIndex += length;
            writer.position += length;
            writer.wireType = WireType.None;
        }

        private void PopRecursionStack()
        {
            this.recursionStack.RemoveLast();
        }

        internal string SerializeType(Type type)
        {
            return TypeModel.SerializeType(this.model, type);
        }

        public static void SetPackedField(int fieldNumber, ProtoWriter writer)
        {
            if (fieldNumber <= 0)
            {
                throw new ArgumentOutOfRangeException("fieldNumber");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            writer.packedFieldNumber = fieldNumber;
        }

        public void SetRootObject(object value)
        {
            this.NetCache.SetKeyedObject(0, value);
        }

        public static SubItemToken StartSubItem(object instance, ProtoWriter writer)
        {
            return StartSubItem(instance, writer, false);
        }

        private static SubItemToken StartSubItem(object instance, ProtoWriter writer, bool allowFixed)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (++writer.depth > 0x19)
            {
                writer.CheckRecursionStackAndPush(instance);
            }
            if (writer.packedFieldNumber != 0)
            {
                throw new InvalidOperationException("Cannot begin a sub-item while performing packed encoding");
            }
            switch (writer.wireType)
            {
                case WireType.String:
                    writer.wireType = WireType.None;
                    DemandSpace(0x20, writer);
                    writer.flushLock++;
                    writer.position++;
                    return new SubItemToken(writer.ioIndex++);

                case WireType.StartGroup:
                    writer.wireType = WireType.None;
                    return new SubItemToken(-writer.fieldNumber);

                case WireType.Fixed32:
                    {
                        if (!allowFixed)
                        {
                            throw CreateException(writer);
                        }
                        DemandSpace(0x20, writer);
                        writer.flushLock++;
                        SubItemToken token = new SubItemToken(writer.ioIndex);
                        IncrementedAndReset(4, writer);
                        return token;
                    }
            }
            throw CreateException(writer);
        }

        void IDisposable.Dispose()
        {
            this.Dispose();
        }

        public static void ThrowEnumException(ProtoWriter writer, object enumValue)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            string str = (enumValue == null) ? "<null>" : (enumValue.GetType().FullName + "." + enumValue.ToString());
            throw new ProtoException("No wire-value is mapped to the enum " + str + " at position " + writer.position.ToString());
        }

        public static void WriteBoolean(bool value, ProtoWriter writer)
        {
            WriteUInt32((uint)(value ? 1 : 0), writer);

        }

        public static void WriteByte(byte value, ProtoWriter writer)
        {
            WriteUInt32(value, writer);
        }

        public static void WriteBytes(byte[] data, ProtoWriter writer)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            WriteBytes(data, 0, data.Length, writer);
        }

        public static void WriteBytes(byte[] data, int offset, int length, ProtoWriter writer)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            switch (writer.wireType)
            {
                case WireType.Fixed64:
                    if (length != 8)
                    {
                        throw new ArgumentException("length");
                    }
                    break;

                case WireType.String:
                    WriteUInt32Variant((uint)length, writer);
                    writer.wireType = WireType.None;
                    if (length != 0)
                    {
                        if ((writer.flushLock == 0) && (length > writer.ioBuffer.Length))
                        {
                            Flush(writer);
                            writer.dest.Write(data, offset, length);
                            writer.position += length;
                            return;
                        }
                        break;
                    }
                    return;

                case WireType.Fixed32:
                    if (length != 4)
                    {
                        throw new ArgumentException("length");
                    }
                    break;

                default:
                    throw CreateException(writer);
            }
            DemandSpace(length, writer);
            Helpers.BlockCopy(data, offset, writer.ioBuffer, writer.ioIndex, length);
            IncrementedAndReset(length, writer);
        }

        public static unsafe void WriteDouble(double value, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            WireType wireType = writer.wireType;
            if (wireType != WireType.Fixed64)
            {
                if (wireType != WireType.Fixed32)
                {
                    throw CreateException(writer);
                }
                float num = (float)value;
                if (Helpers.IsInfinity(num) && !Helpers.IsInfinity(value))
                {
                    throw new OverflowException();
                }
                WriteSingle(num, writer);
            }
            else
            {
                WriteInt64(*((long*)&value), writer);
            }
        }

        public static void WriteFieldHeader(int fieldNumber, WireType wireType, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (writer.wireType != WireType.None)
            {
                throw new InvalidOperationException("Cannot write a " + wireType.ToString() + " header until the " + writer.wireType.ToString() + " data has been written");
            }
            if (fieldNumber < 0)
            {
                throw new ArgumentOutOfRangeException("fieldNumber");
            }
            if (writer.packedFieldNumber == 0)
            {
                writer.fieldNumber = fieldNumber;
                writer.wireType = wireType;
                WriteHeaderCore(fieldNumber, wireType, writer);
            }
            else
            {
                if (writer.packedFieldNumber != fieldNumber)
                {
                    throw new InvalidOperationException("Field mismatch during packed encoding; expected " + writer.packedFieldNumber.ToString() + " but received " + fieldNumber.ToString());
                }
                switch (wireType)
                {
                    case WireType.Variant:
                    case WireType.Fixed64:
                    case WireType.Fixed32:
                    case WireType.SignedVariant:
                        writer.fieldNumber = fieldNumber;
                        writer.wireType = wireType;
                        return;
                }
                throw new InvalidOperationException("Wire-type cannot be encoded as packed: " + wireType.ToString());
            }
        }

        internal static void WriteHeaderCore(int fieldNumber, WireType wireType, ProtoWriter writer)
        {
            uint num = (uint)((WireType)(fieldNumber << 3) | (wireType & (WireType.Fixed32 | WireType.String)));
            WriteUInt32Variant(num, writer);
        }

        public static void WriteInt16(short value, ProtoWriter writer)
        {
            WriteInt32(value, writer);
        }

        public static void WriteInt32(int value, ProtoWriter writer)
        {
            byte[] ioBuffer;
            int ioIndex;
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            switch (writer.wireType)
            {
                case WireType.Variant:
                    if (value < 0)
                    {
                        byte num5;
                        byte num6;
                        DemandSpace(10, writer);
                        ioBuffer = writer.ioBuffer;
                        ioIndex = writer.ioIndex;
                        ioBuffer[ioIndex] = (byte)(value | 0x80);
                        ioBuffer[ioIndex + 1] = (byte)((value >> 7) | 0x80);
                        ioBuffer[ioIndex + 2] = (byte)((value >> 14) | 0x80);
                        ioBuffer[ioIndex + 3] = (byte)((value >> 0x15) | 0x80);
                        ioBuffer[ioIndex + 4] = (byte)((value >> 0x1c) | 0x80);
                        ioBuffer[ioIndex + 8] = (byte)(num5 = 0xff);
                        ioBuffer[ioIndex + 7] = num6 = num5;
                        ioBuffer[ioIndex + 5] = ioBuffer[ioIndex + 6] = num6;
                        ioBuffer[ioIndex + 9] = 1;
                        IncrementedAndReset(10, writer);
                        return;
                    }
                    WriteUInt32Variant((uint)value, writer);
                    writer.wireType = WireType.None;
                    return;

                case WireType.Fixed64:
                    byte num2;
                    byte num3;
                    DemandSpace(8, writer);
                    ioBuffer = writer.ioBuffer;
                    ioIndex = writer.ioIndex;
                    ioBuffer[ioIndex] = (byte)value;
                    ioBuffer[ioIndex + 1] = (byte)(value >> 8);
                    ioBuffer[ioIndex + 2] = (byte)(value >> 0x10);
                    ioBuffer[ioIndex + 3] = (byte)(value >> 0x18);
                    ioBuffer[ioIndex + 7] = (byte)(num2 = 0);
                    ioBuffer[ioIndex + 6] = num3 = num2;
                    ioBuffer[ioIndex + 4] = ioBuffer[ioIndex + 5] = num3;
                    IncrementedAndReset(8, writer);
                    return;

                case WireType.Fixed32:
                    DemandSpace(4, writer);
                    WriteInt32ToBuffer(value, writer.ioBuffer, writer.ioIndex);
                    IncrementedAndReset(4, writer);
                    return;

                case WireType.SignedVariant:
                    WriteUInt32Variant(Zig(value), writer);
                    writer.wireType = WireType.None;
                    return;
            }
            throw CreateException(writer);
        }

        private static void WriteInt32ToBuffer(int value, byte[] buffer, int index)
        {
            buffer[index] = (byte)value;
            buffer[index + 1] = (byte)(value >> 8);
            buffer[index + 2] = (byte)(value >> 0x10);
            buffer[index + 3] = (byte)(value >> 0x18);
        }

        public static void WriteInt64(long value, ProtoWriter writer)
        {
            byte[] ioBuffer;
            int ioIndex;
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            switch (writer.wireType)
            {
                case WireType.Variant:
                    if (value < 0L)
                    {
                        DemandSpace(10, writer);
                        ioBuffer = writer.ioBuffer;
                        ioIndex = writer.ioIndex;
                        ioBuffer[ioIndex] = (byte)(value | 0x80L);
                        ioBuffer[ioIndex + 1] = (byte)(((int)(value >> 7)) | 0x80);
                        ioBuffer[ioIndex + 2] = (byte)(((int)(value >> 14)) | 0x80);
                        ioBuffer[ioIndex + 3] = (byte)(((int)(value >> 0x15)) | 0x80);
                        ioBuffer[ioIndex + 4] = (byte)(((int)(value >> 0x1c)) | 0x80);
                        ioBuffer[ioIndex + 5] = (byte)(((int)(value >> 0x23)) | 0x80);
                        ioBuffer[ioIndex + 6] = (byte)(((int)(value >> 0x2a)) | 0x80);
                        ioBuffer[ioIndex + 7] = (byte)(((int)(value >> 0x31)) | 0x80);
                        ioBuffer[ioIndex + 8] = (byte)(((int)(value >> 0x38)) | 0x80);
                        ioBuffer[ioIndex + 9] = 1;
                        IncrementedAndReset(10, writer);
                        return;
                    }
                    WriteUInt64Variant((ulong)value, writer);
                    writer.wireType = WireType.None;
                    return;

                case WireType.Fixed64:
                    DemandSpace(8, writer);
                    ioBuffer = writer.ioBuffer;
                    ioIndex = writer.ioIndex;
                    ioBuffer[ioIndex] = (byte)value;
                    ioBuffer[ioIndex + 1] = (byte)(value >> 8);
                    ioBuffer[ioIndex + 2] = (byte)(value >> 0x10);
                    ioBuffer[ioIndex + 3] = (byte)(value >> 0x18);
                    ioBuffer[ioIndex + 4] = (byte)(value >> 0x20);
                    ioBuffer[ioIndex + 5] = (byte)(value >> 40);
                    ioBuffer[ioIndex + 6] = (byte)(value >> 0x30);
                    ioBuffer[ioIndex + 7] = (byte)(value >> 0x38);
                    IncrementedAndReset(8, writer);
                    return;

                case WireType.Fixed32:
                    WriteInt32((int)value, writer);
                    return;

                case WireType.SignedVariant:
                    WriteUInt64Variant(Zig(value), writer);
                    writer.wireType = WireType.None;
                    return;
            }
            throw CreateException(writer);
        }

        public static void WriteObject(object value, int key, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (writer.model == null)
            {
                throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
            }
            SubItemToken token = StartSubItem(value, writer);
            if (key >= 0)
            {
                writer.model.Serialize(key, value, writer);
            }
            else if ((writer.model == null) || !writer.model.TrySerializeAuxiliaryType(writer, value.GetType(), DataFormat.Default, 1, value, false))
            {
                TypeModel.ThrowUnexpectedType(value.GetType());
            }
            EndSubItem(token, writer);
        }

        internal static void WriteObject(object value, int key, ProtoWriter writer, PrefixStyle style, int fieldNumber)
        {
            if (writer.model == null)
            {
                throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
            }
            if (writer.wireType != WireType.None)
            {
                throw CreateException(writer);
            }
            switch (style)
            {
                case PrefixStyle.Base128:
                    writer.wireType = WireType.String;
                    writer.fieldNumber = fieldNumber;
                    if (fieldNumber > 0)
                    {
                        WriteHeaderCore(fieldNumber, WireType.String, writer);
                    }
                    break;

                case PrefixStyle.Fixed32:
                case PrefixStyle.Fixed32BigEndian:
                    writer.fieldNumber = 0;
                    writer.wireType = WireType.Fixed32;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("style");
            }
            SubItemToken token = StartSubItem(value, writer, true);
            if (key < 0)
            {
                if (!writer.model.TrySerializeAuxiliaryType(writer, value.GetType(), DataFormat.Default, 1, value, false))
                {
                    TypeModel.ThrowUnexpectedType(value.GetType());
                }
            }
            else
            {
                writer.model.Serialize(key, value, writer);
            }
            EndSubItem(token, writer, style);
        }

        public static void WriteRecursionSafeObject(object value, int key, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (writer.model == null)
            {
                throw new InvalidOperationException("Cannot serialize sub-objects unless a model is provided");
            }
            SubItemToken token = StartSubItem(null, writer);
            writer.model.Serialize(key, value, writer);
            EndSubItem(token, writer);
        }

        public static void WriteSByte(sbyte value, ProtoWriter writer)
        {
            WriteInt32(value, writer);
        }

        public static unsafe void WriteSingle(float value, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            WireType wireType = writer.wireType;
            if (wireType != WireType.Fixed64)
            {
                if (wireType != WireType.Fixed32)
                {
                    throw CreateException(writer);
                }
                WriteInt32(*((int*)&value), writer);
            }
            else
            {
                WriteDouble((double)value, writer);
            }
        }

        public static void WriteString(string value, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            if (writer.wireType != WireType.String)
            {
                throw CreateException(writer);
            }
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (value.Length == 0)
            {
                WriteUInt32Variant(0, writer);
                writer.wireType = WireType.None;
            }
            else
            {
                int byteCount = encoding.GetByteCount(value);
                WriteUInt32Variant((uint)byteCount, writer);
                DemandSpace(byteCount, writer);
                IncrementedAndReset(encoding.GetBytes(value, 0, value.Length, writer.ioBuffer, writer.ioIndex), writer);
            }
        }

        public static void WriteType(Type value, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            WriteString(writer.SerializeType(value), writer);
        }

        public static void WriteUInt16(ushort value, ProtoWriter writer)
        {
            WriteUInt32(value, writer);
        }

        public static void WriteUInt32(uint value, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            WireType wireType = writer.wireType;
            switch (wireType)
            {
                case WireType.Variant:
                    WriteUInt32Variant(value, writer);
                    writer.wireType = WireType.None;
                    return;

                case WireType.Fixed64:
                    WriteInt64((long)value, writer);
                    return;
            }
            if (wireType != WireType.Fixed32)
            {
                throw CreateException(writer);
            }
            WriteInt32((int)value, writer);
        }

        private static void WriteUInt32Variant(uint value, ProtoWriter writer)
        {
            DemandSpace(5, writer);
            int num = 0;
            do
            {
                writer.ioBuffer[writer.ioIndex++] = (byte)((value & 0x7f) | 0x80);
                num++;
            }
            while ((value = value >> 7) != 0);
            writer.ioBuffer[writer.ioIndex - 1] = (byte)(writer.ioBuffer[writer.ioIndex - 1] & 0x7f);
            writer.position += num;
        }

        public static void WriteUInt64(ulong value, ProtoWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }
            switch (writer.wireType)
            {
                case WireType.Variant:
                    WriteUInt64Variant(value, writer);
                    writer.wireType = WireType.None;
                    return;

                case WireType.Fixed64:
                    WriteInt64((long)value, writer);
                    return;

                case WireType.Fixed32:
                    WriteUInt32((uint)value, writer);
                    return;
            }
            throw CreateException(writer);
        }

        private static void WriteUInt64Variant(ulong value, ProtoWriter writer)
        {
            DemandSpace(10, writer);
            int num = 0;
            do
            {
                writer.ioBuffer[writer.ioIndex++] = (byte)((value & ((ulong)0x7fL)) | ((ulong)0x80L));
                num++;
            }
            while ((value = value >> 7) != 0L);
            writer.ioBuffer[writer.ioIndex - 1] = (byte)(writer.ioBuffer[writer.ioIndex - 1] & 0x7f);
            writer.position += num;
        }

        internal static uint Zig(int value)
        {
            return (uint)((value << 1) ^ (value >> 0x1f));
        }

        internal static ulong Zig(long value)
        {
            return (ulong)((value << 1) ^ (value >> 0x3f));
        }

        public SerializationContext Context
        {
            get
            {
                return this.context;
            }
        }

        public TypeModel Model
        {
            get
            {
                return this.model;
            }
        }

        internal NetObjectCache NetCache
        {
            get
            {
                return this.netCache;
            }
        }

        internal WireType WireType
        {
            get
            {
                return this.wireType;
            }
        }
    }
}

