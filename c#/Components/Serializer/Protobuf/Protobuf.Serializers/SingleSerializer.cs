namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using System;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    internal sealed class SingleSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(float);

        public SingleSerializer(TypeModel model)
        {
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicRead("ReadSingle", this.ExpectedType);
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteSingle", valueFrom);
        }

        public object Read(object value, ProtoReader source)
        {
            return source.ReadSingle();
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteSingle((float) value, dest);
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

