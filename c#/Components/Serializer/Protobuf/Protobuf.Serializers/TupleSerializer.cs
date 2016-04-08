namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using System;
    using System.Reflection;

    internal sealed class TupleSerializer : IProtoTypeSerializer, IProtoSerializer
    {
        private readonly ConstructorInfo ctor;
        private readonly MemberInfo[] members;
        private IProtoSerializer[] tails;

        public TupleSerializer(RuntimeTypeModel model, ConstructorInfo ctor, MemberInfo[] members)
        {
            if (ctor == null)
            {
                throw new ArgumentNullException("ctor");
            }
            if (members == null)
            {
                throw new ArgumentNullException("members");
            }
            this.ctor = ctor;
            this.members = members;
            this.tails = new IProtoSerializer[members.Length];
            ParameterInfo[] parameters = ctor.GetParameters();
            for (int i = 0; i < members.Length; i++)
            {
                WireType type;
                IProtoSerializer serializer2;
                Type parameterType = parameters[i].ParameterType;
                Type itemType = null;
                Type defaultType = null;
                MetaType.ResolveListTypes(model, parameterType, ref itemType, ref defaultType);
                Type type5 = (itemType == null) ? parameterType : itemType;
                bool asReference = false;
                if (model.FindOrAddAuto(type5, false, true, false) >= 0)
                {
                    asReference = model[type5].AsReferenceDefault;
                }
                IProtoSerializer tail = ValueMember.TryGetCoreSerializer(model, DataFormat.Default, type5, out type, asReference, false, false, true);
                if (tail == null)
                {
                    throw new InvalidOperationException("No serializer defined for type: " + type5.FullName);
                }
                tail = new TagDecorator(i + 1, type, false, tail);
                if (itemType == null)
                {
                    serializer2 = tail;
                }
                else if (parameterType.IsArray)
                {
                    serializer2 = new ArrayDecorator(model, tail, i + 1, false, type, parameterType, false, false);
                }
                else
                {
                    serializer2 = new ListDecorator(model, parameterType, defaultType, tail, i + 1, false, type, true, false, false);
                }
                this.tails[i] = serializer2;
            }
        }

        public void EmitCallback(CompilerContext ctx, Local valueFrom, TypeModel.CallbackType callbackType)
        {
        }

        public void EmitRead(CompilerContext ctx, Local incoming)
        {
            using (Local local = ctx.GetLocalWithValue(this.ExpectedType, incoming))
            {
                Local[] localArray = new Local[this.members.Length];
                try
                {
                    for (int i = 0; i < localArray.Length; i++)
                    {
                        Type memberType = this.GetMemberType(i);
                        bool flag = true;
                        localArray[i] = new Local(ctx, memberType);
                        if (this.ExpectedType.IsValueType)
                        {
                            continue;
                        }
                        if (memberType.IsValueType)
                        {
                            switch (Helpers.GetTypeCode(memberType))
                            {
                                case ProtoTypeCode.Boolean:
                                case ProtoTypeCode.SByte:
                                case ProtoTypeCode.Byte:
                                case ProtoTypeCode.Int16:
                                case ProtoTypeCode.UInt16:
                                case ProtoTypeCode.Int32:
                                case ProtoTypeCode.UInt32:
                                    ctx.LoadValue(0);
                                    goto Label_0108;

                                case ProtoTypeCode.Int64:
                                case ProtoTypeCode.UInt64:
                                    ctx.LoadValue((long) 0L);
                                    goto Label_0108;

                                case ProtoTypeCode.Single:
                                    ctx.LoadValue((float) 0f);
                                    goto Label_0108;

                                case ProtoTypeCode.Double:
                                    ctx.LoadValue((double) 0.0);
                                    goto Label_0108;

                                case ProtoTypeCode.Decimal:
                                    ctx.LoadValue((decimal) 0M);
                                    goto Label_0108;

                                case ProtoTypeCode.Guid:
                                    ctx.LoadValue(Guid.Empty);
                                    goto Label_0108;
                            }
                            ctx.LoadAddress(localArray[i], memberType);
                            ctx.EmitCtor(memberType);
                            flag = false;
                        }
                        else
                        {
                            ctx.LoadNullRef();
                        }
                    Label_0108:
                        if (flag)
                        {
                            ctx.StoreValue(localArray[i]);
                        }
                    }
                    CodeLabel label = this.ExpectedType.IsValueType ? new CodeLabel() : ctx.DefineLabel();
                    if (!this.ExpectedType.IsValueType)
                    {
                        ctx.LoadAddress(local, this.ExpectedType);
                        ctx.BranchIfFalse(label, false);
                    }
                    for (int j = 0; j < this.members.Length; j++)
                    {
                        ctx.LoadAddress(local, this.ExpectedType);
                        switch (this.members[j].MemberType)
                        {
                            case MemberTypes.Field:
                                ctx.LoadValue((FieldInfo) this.members[j]);
                                break;

                            case MemberTypes.Property:
                                ctx.LoadValue((PropertyInfo) this.members[j]);
                                break;
                        }
                        ctx.StoreValue(localArray[j]);
                    }
                    if (!this.ExpectedType.IsValueType)
                    {
                        ctx.MarkLabel(label);
                    }
                    using (Local local2 = new Local(ctx, ctx.MapType(typeof(int))))
                    {
                        CodeLabel label2 = ctx.DefineLabel();
                        CodeLabel label3 = ctx.DefineLabel();
                        CodeLabel label4 = ctx.DefineLabel();
                        ctx.Branch(label2, false);
                        CodeLabel[] jumpTable = new CodeLabel[this.members.Length];
                        for (int m = 0; m < this.members.Length; m++)
                        {
                            jumpTable[m] = ctx.DefineLabel();
                        }
                        ctx.MarkLabel(label3);
                        ctx.LoadValue(local2);
                        ctx.LoadValue(1);
                        ctx.Subtract();
                        ctx.Switch(jumpTable);
                        ctx.Branch(label4, false);
                        for (int n = 0; n < jumpTable.Length; n++)
                        {
                            ctx.MarkLabel(jumpTable[n]);
                            IProtoSerializer tail = this.tails[n];
                            Local valueFrom = tail.RequiresOldValue ? localArray[n] : null;
                            ctx.ReadNullCheckedTail(localArray[n].Type, tail, valueFrom);
                            if (tail.ReturnsValue)
                            {
                                if (localArray[n].Type.IsValueType)
                                {
                                    ctx.StoreValue(localArray[n]);
                                }
                                else
                                {
                                    CodeLabel label5 = ctx.DefineLabel();
                                    CodeLabel label6 = ctx.DefineLabel();
                                    ctx.CopyValue();
                                    ctx.BranchIfTrue(label5, true);
                                    ctx.DiscardValue();
                                    ctx.Branch(label6, true);
                                    ctx.MarkLabel(label5);
                                    ctx.StoreValue(localArray[n]);
                                    ctx.MarkLabel(label6);
                                }
                            }
                            ctx.Branch(label2, false);
                        }
                        ctx.MarkLabel(label4);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("SkipField"));
                        ctx.MarkLabel(label2);
                        ctx.EmitBasicRead("ReadFieldHeader", ctx.MapType(typeof(int)));
                        ctx.CopyValue();
                        ctx.StoreValue(local2);
                        ctx.LoadValue(0);
                        ctx.BranchIfGreater(label3, false);
                    }
                    for (int k = 0; k < localArray.Length; k++)
                    {
                        ctx.LoadValue(localArray[k]);
                    }
                    ctx.EmitCtor(this.ctor);
                    ctx.StoreValue(local);
                }
                finally
                {
                    for (int num6 = 0; num6 < localArray.Length; num6++)
                    {
                        if (localArray[num6] != null)
                        {
                            localArray[num6].Dispose();
                        }
                    }
                }
            }
        }

        public void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            using (Local local = ctx.GetLocalWithValue(this.ctor.DeclaringType, valueFrom))
            {
                for (int i = 0; i < this.tails.Length; i++)
                {
                    Type memberType = this.GetMemberType(i);
                    ctx.LoadAddress(local, this.ExpectedType);
                    switch (this.members[i].MemberType)
                    {
                        case MemberTypes.Field:
                            ctx.LoadValue((FieldInfo) this.members[i]);
                            break;

                        case MemberTypes.Property:
                            ctx.LoadValue((PropertyInfo) this.members[i]);
                            break;
                    }
                    ctx.WriteNullCheckedTail(memberType, this.tails[i], null);
                }
            }
        }

        private Type GetMemberType(int index)
        {
            Type memberType = Helpers.GetMemberType(this.members[index]);
            if (memberType == null)
            {
                throw new InvalidOperationException();
            }
            return memberType;
        }

        private object GetValue(object obj, int index)
        {
            PropertyInfo info = this.members[index] as PropertyInfo;
            if (info != null)
            {
                if (obj != null)
                {
                    return info.GetValue(obj, null);
                }
                if (!Helpers.IsValueType(info.PropertyType))
                {
                    return null;
                }
                return Activator.CreateInstance(info.PropertyType);
            }
            FieldInfo info2 = this.members[index] as FieldInfo;
            if (info2 == null)
            {
                throw new InvalidOperationException();
            }
            if (obj != null)
            {
                return info2.GetValue(obj);
            }
            if (!Helpers.IsValueType(info2.FieldType))
            {
                return null;
            }
            return Activator.CreateInstance(info2.FieldType);
        }

        public bool HasCallbacks(TypeModel.CallbackType callbackType)
        {
            return false;
        }

        void IProtoTypeSerializer.Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
        }

        bool IProtoTypeSerializer.CanCreateInstance()
        {
            return false;
        }

        object IProtoTypeSerializer.CreateInstance(ProtoReader source)
        {
            throw new NotSupportedException();
        }

        void IProtoTypeSerializer.EmitCreateInstance(CompilerContext ctx)
        {
            throw new NotSupportedException();
        }

        public object Read(object value, ProtoReader source)
        {
            int num2;
            object[] parameters = new object[this.members.Length];
            bool flag = false;
            if (value == null)
            {
                flag = true;
            }
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i] = this.GetValue(value, i);
            }
            while ((num2 = source.ReadFieldHeader()) > 0)
            {
                flag = true;
                if (num2 <= this.tails.Length)
                {
                    IProtoSerializer serializer = this.tails[num2 - 1];
                    parameters[num2 - 1] = this.tails[num2 - 1].Read(serializer.RequiresOldValue ? parameters[num2 - 1] : null, source);
                }
                else
                {
                    source.SkipField();
                }
            }
            if (!flag)
            {
                return value;
            }
            return this.ctor.Invoke(parameters);
        }

        public void Write(object value, ProtoWriter dest)
        {
            for (int i = 0; i < this.tails.Length; i++)
            {
                object obj2 = this.GetValue(value, i);
                if (obj2 != null)
                {
                    this.tails[i].Write(obj2, dest);
                }
            }
        }

        public Type ExpectedType
        {
            get
            {
                return this.ctor.DeclaringType;
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
                return false;
            }
        }
    }
}

