namespace Components.Serialize.Protobuf.Serializers
{
    using System;
    using Components.Serialize.Protobuf.Meta;
    using Components.Serialize.Protobuf.Protobuf;
    using Components.Serialize.Protobuf.Compiler;
    internal sealed class StringSerializer : IProtoSerializer
    {
        private static readonly Type expectedType = typeof(string);

        public StringSerializer(TypeModel model)
        {
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicRead("ReadString", this.ExpectedType);
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteString", valueFrom);
        }

        public object Read(object value, ProtoReader source)
        {
            return source.ReadString();
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteString((string) value, dest);
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

