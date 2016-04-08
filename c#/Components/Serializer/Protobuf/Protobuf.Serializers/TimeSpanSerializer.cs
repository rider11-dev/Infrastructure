namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using System;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    internal sealed class TimeSpanSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(TimeSpan);

        public TimeSpanSerializer(TypeModel model)
        {
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicRead(ctx.MapType(typeof(BclHelpers)), "ReadTimeSpan", this.ExpectedType);
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitWrite(ctx.MapType(typeof(BclHelpers)), "WriteTimeSpan", valueFrom);
        }

        public object Read(object value, ProtoReader source)
        {
            return BclHelpers.ReadTimeSpan(source);
        }

        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteTimeSpan((TimeSpan) value, dest);
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

