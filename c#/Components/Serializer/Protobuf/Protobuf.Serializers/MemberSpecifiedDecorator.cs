namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using System;
    using System.Reflection;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    internal sealed class MemberSpecifiedDecorator : ProtoDecoratorBase
    {
        private readonly MethodInfo getSpecified;
        private readonly MethodInfo setSpecified;

        public MemberSpecifiedDecorator(MethodInfo getSpecified, MethodInfo setSpecified, IProtoSerializer tail) : base(tail)
        {
            if ((getSpecified == null) && (setSpecified == null))
            {
                throw new InvalidOperationException();
            }
            this.getSpecified = getSpecified;
            this.setSpecified = setSpecified;
        }

        protected override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            if (this.setSpecified == null)
            {
                base.Tail.EmitRead(ctx, valueFrom);
            }
            else
            {
                using (Local local = ctx.GetLocalWithValue(this.ExpectedType, valueFrom))
                {
                    base.Tail.EmitRead(ctx, local);
                    ctx.LoadAddress(local, this.ExpectedType);
                    ctx.LoadValue(1);
                    ctx.EmitCall(this.setSpecified);
                }
            }
        }

        protected override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            if (this.getSpecified == null)
            {
                base.Tail.EmitWrite(ctx, valueFrom);
            }
            else
            {
                using (Local local = ctx.GetLocalWithValue(this.ExpectedType, valueFrom))
                {
                    ctx.LoadAddress(local, this.ExpectedType);
                    ctx.EmitCall(this.getSpecified);
                    CodeLabel label = ctx.DefineLabel();
                    ctx.BranchIfFalse(label, false);
                    base.Tail.EmitWrite(ctx, local);
                    ctx.MarkLabel(label);
                }
            }
        }

        public override object Read(object value, ProtoReader source)
        {
            object obj2 = base.Tail.Read(value, source);
            if (this.setSpecified != null)
            {
                this.setSpecified.Invoke(value, new object[] { true });
            }
            return obj2;
        }

        public override void Write(object value, ProtoWriter dest)
        {
            if ((this.getSpecified == null) || ((bool) this.getSpecified.Invoke(value, null)))
            {
                base.Tail.Write(value, dest);
            }
        }

        public override Type ExpectedType
        {
            get
            {
                return base.Tail.ExpectedType;
            }
        }

        public override bool RequiresOldValue
        {
            get
            {
                return base.Tail.RequiresOldValue;
            }
        }

        public override bool ReturnsValue
        {
            get
            {
                return base.Tail.ReturnsValue;
            }
        }
    }
}

