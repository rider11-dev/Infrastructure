namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using System;

    internal abstract class ProtoDecoratorBase : IProtoSerializer
    {
        protected readonly IProtoSerializer Tail;

        protected ProtoDecoratorBase(IProtoSerializer tail)
        {
            this.Tail = tail;
        }

        protected abstract void EmitRead(CompilerContext ctx, Local valueFrom);
        protected abstract void EmitWrite(CompilerContext ctx, Local valueFrom);
        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            this.EmitRead(ctx, valueFrom);
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            this.EmitWrite(ctx, valueFrom);
        }

        public abstract object Read(object value, ProtoReader source);
        public abstract void Write(object value, ProtoWriter dest);

        public abstract Type ExpectedType { get; }

        public abstract bool RequiresOldValue { get; }

        public abstract bool ReturnsValue { get; }
    }
}

