namespace Components.Serialize.Protobuf.Serializers
{
    using Components.Serialize.Protobuf.Compiler;
    using Components.Serialize.Protobuf.Meta;
    using Components.Serialize.Protobuf.Protobuf;
    using System;

    internal sealed class UriDecorator : ProtoDecoratorBase
    {
        private static readonly Type expectedType = typeof(Uri);

        public UriDecorator(TypeModel model, IProtoSerializer tail) : base(tail)
        {
        }

        protected override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            base.Tail.EmitRead(ctx, valueFrom);
            ctx.CopyValue();
            CodeLabel label = ctx.DefineLabel();
            CodeLabel label2 = ctx.DefineLabel();
            ctx.LoadValue(typeof(string).GetProperty("Length"));
            ctx.BranchIfTrue(label, true);
            ctx.DiscardValue();
            ctx.LoadNullRef();
            ctx.Branch(label2, true);
            ctx.MarkLabel(label);
            ctx.EmitCtor(ctx.MapType(typeof(Uri)), new Type[] { ctx.MapType(typeof(string)) });
            ctx.MarkLabel(label2);
        }

        protected override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.LoadValue(typeof(Uri).GetProperty("AbsoluteUri"));
            base.Tail.EmitWrite(ctx, null);
        }

        public override object Read(object value, ProtoReader source)
        {
            string uriString = (string) base.Tail.Read(null, source);
            if (uriString.Length != 0)
            {
                return new Uri(uriString);
            }
            return null;
        }

        public override void Write(object value, ProtoWriter dest)
        {
            base.Tail.Write(((Uri) value).AbsoluteUri, dest);
        }

        public override Type ExpectedType
        {
            get
            {
                return expectedType;
            }
        }

        public override bool RequiresOldValue
        {
            get
            {
                return false;
            }
        }

        public override bool ReturnsValue
        {
            get
            {
                return true;
            }
        }
    }
}

