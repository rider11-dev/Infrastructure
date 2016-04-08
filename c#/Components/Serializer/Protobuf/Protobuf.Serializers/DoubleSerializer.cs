namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using System;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    internal sealed class DoubleSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(double);

        public DoubleSerializer(TypeModel model)
        {
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicRead("ReadDouble", this.ExpectedType);
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteDouble", valueFrom);
        }

        public object Read(object value, ProtoReader source)
        {
            return source.ReadDouble();
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteDouble((double) value, dest);
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

