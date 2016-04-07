namespace Components.Serialize.Protobuf.Serializers
{
    using System;
    using Components.Serialize.Protobuf.Meta;
    using Components.Serialize.Protobuf.Protobuf;
    using Components.Serialize.Protobuf.Compiler;
    internal sealed class NetObjectSerializer : IProtoSerializer
    {
        private readonly int key;
        private readonly BclHelpers.NetObjectOptions options;
        private readonly Type type;

        public NetObjectSerializer(TypeModel model, Type type, int key, BclHelpers.NetObjectOptions options)
        {
            bool flag = ((byte) (options & BclHelpers.NetObjectOptions.DynamicType)) != 0;
            this.key = flag ? -1 : key;
            this.type = flag ? model.MapType(typeof(object)) : type;
            this.options = options;
        }

        public void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.CastToObject(this.type);
            ctx.LoadReaderWriter();
            ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(this.key));
            if (this.type == ctx.MapType(typeof(object)))
            {
                ctx.LoadNullRef();
            }
            else
            {
                ctx.LoadValue(this.type);
            }
            ctx.LoadValue((int) this.options);
            ctx.EmitCall(ctx.MapType(typeof(BclHelpers)).GetMethod("ReadNetObject"));
            ctx.CastFromObject(this.type);
        }

        public void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.CastToObject(this.type);
            ctx.LoadReaderWriter();
            ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(this.key));
            ctx.LoadValue((int) this.options);
            ctx.EmitCall(ctx.MapType(typeof(BclHelpers)).GetMethod("WriteNetObject"));
        }

        public object Read(object value, ProtoReader source)
        {
            return BclHelpers.ReadNetObject(value, source, this.key, (this.type == typeof(object)) ? null : this.type, this.options);
        }

        public void Write(object value, ProtoWriter dest)
        {
            BclHelpers.WriteNetObject(value, dest, this.key, this.options);
        }

        public Type ExpectedType
        {
            get
            {
                return this.type;
            }
        }

        public bool RequiresOldValue
        {
            get
            {
                return true;
            }
        }

        public bool ReturnsValue
        {
            get
            {
                return true;
            }
        }
    }
}

