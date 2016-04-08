namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using System;

    internal class UInt16Serializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(ushort);

        public UInt16Serializer(TypeModel model)
        {
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicRead("ReadUInt16", ctx.MapType(typeof(ushort)));
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteUInt16", valueFrom);
        }

        public virtual object Read(object value, ProtoReader source)
        {
            return source.ReadUInt16();
        }

        public virtual void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteUInt16((ushort) value, dest);
        }

        public virtual Type ExpectedType
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

