namespace Components.Serialize.Protobuf.Serializers
{
    using System;
    using System.Reflection;
    using Components.Serialize.Protobuf.Meta;
    using Components.Serialize.Protobuf.Protobuf;
    using Components.Serialize.Protobuf.Compiler;
    internal sealed class ParseableSerializer : IProtoSerializer
    {
        private readonly MethodInfo parse;

        private ParseableSerializer(MethodInfo parse)
        {
            this.parse = parse;
        }

        private static MethodInfo GetCustomToString(Type type)
        {
            return type.GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, Helpers.EmptyTypes, null);
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ctx.EmitBasicRead("ReadString", ctx.MapType(typeof(string)));
            ctx.EmitCall(this.parse);
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            Type expectedType = this.ExpectedType;
            if (expectedType.IsValueType)
            {
                using (Local local = ctx.GetLocalWithValue(expectedType, valueFrom))
                {
                    ctx.LoadAddress(local, expectedType);
                    ctx.EmitCall(GetCustomToString(expectedType));
                    goto Label_0058;
                }
            }
            ctx.EmitCall(ctx.MapType(typeof(object)).GetMethod("ToString"));
        Label_0058:
            ctx.EmitBasicWrite("WriteString", valueFrom);
        }

        public object Read(object value, ProtoReader source)
        {
            return this.parse.Invoke(null, new object[] { source.ReadString() });
        }

        public static ParseableSerializer TryCreate(Type type, TypeModel model)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            MethodInfo parse = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly, null, new Type[] { model.MapType(typeof(string)) }, null);
            if ((parse == null) || !(parse.ReturnType == type))
            {
                return null;
            }
            if (Helpers.IsValueType(type))
            {
                MethodInfo customToString = GetCustomToString(type);
                if ((customToString == null) || (customToString.ReturnType != model.MapType(typeof(string))))
                {
                    return null;
                }
            }
            return new ParseableSerializer(parse);
        }

        public void Write(object value, ProtoWriter dest)
        {
            ProtoWriter.WriteString(value.ToString(), dest);
        }

        public Type ExpectedType
        {
            get
            {
                return this.parse.DeclaringType;
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

