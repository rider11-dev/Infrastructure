namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using System;

    internal sealed class UInt64Serializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(ulong);

        public UInt64Serializer(TypeModel model)
        {
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicRead("ReadUInt64", this.ExpectedType);
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteUInt64", valueFrom);
        }

        public object Read(object value, ProtoReader source)
        {
            return source.ReadUInt64();
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteUInt64((ulong) value, dest);
        }

        public Type ExpectedType
        {
            get
            {
                return expectedType;
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
    }
}

