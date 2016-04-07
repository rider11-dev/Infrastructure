namespace Components.Serialize.Protobuf.Serializers
{
    using System;
    using System.Reflection;
    using Components.Serialize.Protobuf.Compiler;
    using Components.Serialize.Protobuf.Meta;
    using Components.Serialize.Protobuf.Protobuf;
    internal sealed class FieldDecorator : ProtoDecoratorBase
    {
        private readonly FieldInfo field;
        private readonly Type forType;

        public FieldDecorator(Type forType, FieldInfo field, IProtoSerializer tail) : base(tail)
        {
            this.forType = forType;
            this.field = field;
        }

        protected override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            using (Local local = ctx.GetLocalWithValue(this.ExpectedType, valueFrom))
            {
                ctx.LoadAddress(local, this.ExpectedType);
                if (base.Tail.RequiresOldValue)
                {
                    ctx.CopyValue();
                    ctx.LoadValue(this.field);
                }
                ctx.ReadNullCheckedTail(this.field.FieldType, base.Tail, null);
                if (base.Tail.ReturnsValue)
                {
                    if (this.field.FieldType.IsValueType)
                    {
                        ctx.StoreValue(this.field);
                    }
                    else
                    {
                        CodeLabel label = ctx.DefineLabel();
                        CodeLabel label2 = ctx.DefineLabel();
                        ctx.CopyValue();
                        ctx.BranchIfTrue(label, true);
                        ctx.DiscardValue();
                        ctx.DiscardValue();
                        ctx.Branch(label2, true);
                        ctx.MarkLabel(label);
                        ctx.StoreValue(this.field);
                        ctx.MarkLabel(label2);
                    }
                }
                else
                {
                    ctx.DiscardValue();
                }
            }
        }

        protected override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ctx.LoadAddress(valueFrom, this.ExpectedType);
            ctx.LoadValue(this.field);
            ctx.WriteNullCheckedTail(this.field.FieldType, base.Tail, null);
        }

        public override object Read(object value, ProtoReader source)
        {
            object obj2 = base.Tail.Read(base.Tail.RequiresOldValue ? this.field.GetValue(value) : null, source);
            if (obj2 != null)
            {
                this.field.SetValue(value, obj2);
            }
            return null;
        }

        public override void Write(object value, ProtoWriter dest)
        {
            value = this.field.GetValue(value);
            if (value != null)
            {
                base.Tail.Write(value, dest);
            }
        }

        public override Type ExpectedType
        {
            get
            {
                return this.forType;
            }
        }

        public override bool RequiresOldValue
        {
            get
            {
                return true;
            }
        }

        public override bool ReturnsValue
        {
            get
            {
                return false;
            }
        }
    }
}

