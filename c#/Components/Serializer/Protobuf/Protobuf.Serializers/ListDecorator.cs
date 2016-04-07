namespace Components.Serialize.Protobuf.Serializers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Components.Serialize.Protobuf.Compiler;
    using Components.Serialize.Protobuf.Meta;
    using Components.Serialize.Protobuf.Protobuf;
    internal sealed class ListDecorator : ProtoDecoratorBase
    {
        private readonly MethodInfo add;
        private readonly Type concreteType;
        private readonly Type declaredType;
        private readonly int fieldNumber;
        private static readonly Type ienumerableType = typeof(IEnumerable);
        private static readonly Type ienumeratorType = typeof(IEnumerator);
        private readonly byte options;
        private const byte OPTIONS_IsList = 1;
        private const byte OPTIONS_OverwriteList = 0x10;
        private const byte OPTIONS_ReturnList = 8;
        private const byte OPTIONS_SupportNull = 0x20;
        private const byte OPTIONS_SuppressIList = 2;
        private const byte OPTIONS_WritePacked = 4;
        private readonly WireType packedWireType;

        public ListDecorator(TypeModel model, Type declaredType, Type concreteType, IProtoSerializer tail, int fieldNumber, bool writePacked, WireType packedWireType, bool returnList, bool overwriteList, bool supportNull) : base(tail)
        {
            bool flag;
            if (returnList)
            {
                this.options = (byte) (this.options | 8);
            }
            if (overwriteList)
            {
                this.options = (byte) (this.options | 0x10);
            }
            if (supportNull)
            {
                this.options = (byte) (this.options | 0x20);
            }
            if ((writePacked || (packedWireType != WireType.None)) && (fieldNumber <= 0))
            {
                throw new ArgumentOutOfRangeException("fieldNumber");
            }
            if (!CanPack(packedWireType))
            {
                if (writePacked)
                {
                    throw new InvalidOperationException("Only simple data-types can use packed encoding");
                }
                packedWireType = WireType.None;
            }
            this.fieldNumber = fieldNumber;
            if (writePacked)
            {
                this.options = (byte) (this.options | 4);
            }
            this.packedWireType = packedWireType;
            if (declaredType == null)
            {
                throw new ArgumentNullException("declaredType");
            }
            if (declaredType.IsArray)
            {
                throw new ArgumentException("Cannot treat arrays as lists", "declaredType");
            }
            this.declaredType = declaredType;
            this.concreteType = concreteType;
            this.add = TypeModel.ResolveListAdd(model, declaredType, tail.ExpectedType, out flag);
            if (flag)
            {
                this.options = (byte) (this.options | 1);
                string fullName = declaredType.FullName;
                if ((fullName != null) && fullName.StartsWith("System.Data.Linq.EntitySet`1[["))
                {
                    this.options = (byte) (this.options | 2);
                }
            }
            if (this.add == null)
            {
                throw new InvalidOperationException("Unable to resolve a suitable Add method for " + declaredType.FullName);
            }
        }

        internal static bool CanPack(WireType wireType)
        {
            switch (wireType)
            {
                case WireType.Variant:
                case WireType.Fixed64:
                case WireType.Fixed32:
                case WireType.SignedVariant:
                    return true;
            }
            return false;
        }

        protected override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            bool returnList = this.ReturnList;
            bool castListForAdd = !this.add.DeclaringType.IsAssignableFrom(this.declaredType);
            using (Local local = this.AppendToCollection ? ctx.GetLocalWithValue(this.ExpectedType, valueFrom) : new Local(ctx, this.declaredType))
            {
                using (Local local2 = (returnList && this.AppendToCollection) ? new Local(ctx, this.ExpectedType) : null)
                {
                    if (!this.AppendToCollection)
                    {
                        ctx.LoadNullRef();
                        ctx.StoreValue(local);
                    }
                    else if (returnList)
                    {
                        ctx.LoadValue(local);
                        ctx.StoreValue(local2);
                    }
                    if (this.concreteType != null)
                    {
                        ctx.LoadValue(local);
                        CodeLabel label = ctx.DefineLabel();
                        ctx.BranchIfTrue(label, true);
                        ctx.EmitCtor(this.concreteType);
                        ctx.StoreValue(local);
                        ctx.MarkLabel(label);
                    }
                    EmitReadList(ctx, local, base.Tail, this.add, this.packedWireType, castListForAdd);
                    if (returnList)
                    {
                        if (this.AppendToCollection)
                        {
                            ctx.LoadValue(local2);
                            ctx.LoadValue(local);
                            CodeLabel label2 = ctx.DefineLabel();
                            CodeLabel label3 = ctx.DefineLabel();
                            ctx.BranchIfEqual(label2, true);
                            ctx.LoadValue(local);
                            ctx.Branch(label3, true);
                            ctx.MarkLabel(label2);
                            ctx.LoadNullRef();
                            ctx.MarkLabel(label3);
                        }
                        else
                        {
                            ctx.LoadValue(local);
                        }
                    }
                }
            }
        }

        private static void EmitReadAndAddItem(CompilerContext ctx, Local list, IProtoSerializer tail, MethodInfo add, bool castListForAdd)
        {
            Type type2;
            ctx.LoadValue(list);
            if (castListForAdd)
            {
                ctx.Cast(add.DeclaringType);
            }
            Type expectedType = tail.ExpectedType;
            bool returnsValue = tail.ReturnsValue;
            if (tail.RequiresOldValue)
            {
                if (expectedType.IsValueType || !returnsValue)
                {
                    using (Local local = new Local(ctx, expectedType))
                    {
                        if (expectedType.IsValueType)
                        {
                            ctx.LoadAddress(local, expectedType);
                            ctx.EmitCtor(expectedType);
                        }
                        else
                        {
                            ctx.LoadNullRef();
                            ctx.StoreValue(local);
                        }
                        tail.EmitRead(ctx, local);
                        if (!returnsValue)
                        {
                            ctx.LoadValue(local);
                        }
                        goto Label_00A7;
                    }
                }
                ctx.LoadNullRef();
                tail.EmitRead(ctx, null);
            }
            else
            {
                if (!returnsValue)
                {
                    throw new InvalidOperationException();
                }
                tail.EmitRead(ctx, null);
            }
        Label_00A7:
            type2 = add.GetParameters()[0].ParameterType;
            if (type2 != expectedType)
            {
                if (type2 != ctx.MapType(typeof(object)))
                {
                    if (Helpers.GetUnderlyingType(type2) != expectedType)
                    {
                        throw new InvalidOperationException("Conflicting item/add type");
                    }
                    ConstructorInfo ctor = Helpers.GetConstructor(type2, new Type[] { expectedType }, false);
                    ctx.EmitCtor(ctor);
                }
                else
                {
                    ctx.CastToObject(expectedType);
                }
            }
            ctx.EmitCall(add);
            if (add.ReturnType != ctx.MapType(typeof(void)))
            {
                ctx.DiscardValue();
            }
        }

        internal static void EmitReadList(CompilerContext ctx, Local list, IProtoSerializer tail, MethodInfo add, WireType packedWireType, bool castListForAdd)
        {
            using (Local local = new Local(ctx, ctx.MapType(typeof(int))))
            {
                CodeLabel label = (packedWireType == WireType.None) ? new CodeLabel() : ctx.DefineLabel();
                if (packedWireType != WireType.None)
                {
                    ctx.LoadReaderWriter();
                    ctx.LoadValue(typeof(ProtoReader).GetProperty("WireType"));
                    ctx.LoadValue(2);
                    ctx.BranchIfEqual(label, false);
                }
                ctx.LoadReaderWriter();
                ctx.LoadValue(typeof(ProtoReader).GetProperty("FieldNumber"));
                ctx.StoreValue(local);
                CodeLabel label2 = ctx.DefineLabel();
                ctx.MarkLabel(label2);
                EmitReadAndAddItem(ctx, list, tail, add, castListForAdd);
                ctx.LoadReaderWriter();
                ctx.LoadValue(local);
                ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("TryReadFieldHeader"));
                ctx.BranchIfTrue(label2, false);
                if (packedWireType != WireType.None)
                {
                    CodeLabel label3 = ctx.DefineLabel();
                    ctx.Branch(label3, false);
                    ctx.MarkLabel(label);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("StartSubItem"));
                    CodeLabel label4 = ctx.DefineLabel();
                    CodeLabel label5 = ctx.DefineLabel();
                    ctx.MarkLabel(label4);
                    ctx.LoadValue((int) packedWireType);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("HasSubValue"));
                    ctx.BranchIfFalse(label5, false);
                    EmitReadAndAddItem(ctx, list, tail, add, castListForAdd);
                    ctx.Branch(label4, false);
                    ctx.MarkLabel(label5);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("EndSubItem"));
                    ctx.MarkLabel(label3);
                }
            }
        }

        protected override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            using (Local local = ctx.GetLocalWithValue(this.ExpectedType, valueFrom))
            {
                MethodInfo info;
                MethodInfo info2;
                MethodInfo method = this.GetEnumeratorInfo(ctx.Model, out info, out info2);
                Type returnType = method.ReturnType;
                bool writePacked = this.WritePacked;
                using (Local local2 = new Local(ctx, returnType))
                {
                    using (Local local3 = writePacked ? new Local(ctx, ctx.MapType(typeof(SubItemToken))) : null)
                    {
                        if (writePacked)
                        {
                            ctx.LoadValue(this.fieldNumber);
                            ctx.LoadValue(2);
                            ctx.LoadReaderWriter();
                            ctx.EmitCall(ctx.MapType(typeof(ProtoWriter)).GetMethod("WriteFieldHeader"));
                            ctx.LoadValue(local);
                            ctx.LoadReaderWriter();
                            ctx.EmitCall(ctx.MapType(typeof(ProtoWriter)).GetMethod("StartSubItem"));
                            ctx.StoreValue(local3);
                            ctx.LoadValue(this.fieldNumber);
                            ctx.LoadReaderWriter();
                            ctx.EmitCall(ctx.MapType(typeof(ProtoWriter)).GetMethod("SetPackedField"));
                        }
                        ctx.LoadAddress(local, this.ExpectedType);
                        ctx.EmitCall(method);
                        ctx.StoreValue(local2);
                        using (ctx.Using(local2))
                        {
                            CodeLabel label = ctx.DefineLabel();
                            CodeLabel label2 = ctx.DefineLabel();
                            ctx.Branch(label2, false);
                            ctx.MarkLabel(label);
                            ctx.LoadAddress(local2, returnType);
                            ctx.EmitCall(info2);
                            Type expectedType = base.Tail.ExpectedType;
                            if ((expectedType != ctx.MapType(typeof(object))) && (info2.ReturnType == ctx.MapType(typeof(object))))
                            {
                                ctx.CastFromObject(expectedType);
                            }
                            base.Tail.EmitWrite(ctx, null);
                            ctx.MarkLabel(label2);
                            ctx.LoadAddress(local2, returnType);
                            ctx.EmitCall(info);
                            ctx.BranchIfTrue(label, false);
                        }
                        if (writePacked)
                        {
                            ctx.LoadValue(local3);
                            ctx.LoadReaderWriter();
                            ctx.EmitCall(ctx.MapType(typeof(ProtoWriter)).GetMethod("EndSubItem"));
                        }
                    }
                }
            }
        }

        private MethodInfo GetEnumeratorInfo(TypeModel model, out MethodInfo moveNext, out MethodInfo current)
        {
            Type declaringType = null;
            Type returnType;
            Type expectedType = this.ExpectedType;
            MethodInfo instanceMethod = Helpers.GetInstanceMethod(expectedType, "GetEnumerator", null);
            Type type4 = base.Tail.ExpectedType;
            if (instanceMethod != null)
            {
                returnType = instanceMethod.ReturnType;
                moveNext = Helpers.GetInstanceMethod(returnType, "MoveNext", null);
                PropertyInfo property = Helpers.GetProperty(returnType, "Current", false);
                current = (property == null) ? null : Helpers.GetGetMethod(property, false, false);
                if ((moveNext == null) && model.MapType(ienumeratorType).IsAssignableFrom(returnType))
                {
                    moveNext = Helpers.GetInstanceMethod(model.MapType(ienumeratorType), "MoveNext", null);
                }
                if (((moveNext != null) && (moveNext.ReturnType == model.MapType(typeof(bool)))) && ((current != null) && (current.ReturnType == type4)))
                {
                    return instanceMethod;
                }
                moveNext = current = (MethodInfo) (instanceMethod = null);
            }
            Type type6 = model.MapType(typeof(IEnumerable<>), false);
            if (type6 != null)
            {
                declaringType = type6.MakeGenericType(new Type[] { type4 });
            }
            if ((declaringType != null) && declaringType.IsAssignableFrom(expectedType))
            {
                instanceMethod = Helpers.GetInstanceMethod(declaringType, "GetEnumerator");
                returnType = instanceMethod.ReturnType;
                moveNext = Helpers.GetInstanceMethod(model.MapType(ienumeratorType), "MoveNext");
                current = Helpers.GetGetMethod(Helpers.GetProperty(returnType, "Current", false), false, false);
                return instanceMethod;
            }
            instanceMethod = Helpers.GetInstanceMethod(model.MapType(ienumerableType), "GetEnumerator");
            returnType = instanceMethod.ReturnType;
            moveNext = Helpers.GetInstanceMethod(returnType, "MoveNext");
            current = Helpers.GetGetMethod(Helpers.GetProperty(returnType, "Current", false), false, false);
            return instanceMethod;
        }

        public override object Read(object value, ProtoReader source)
        {
            int fieldNumber = source.FieldNumber;
            object obj2 = value;
            if (value == null)
            {
                value = Activator.CreateInstance(this.concreteType);
            }
            bool flag = this.IsList && !this.SuppressIList;
            if ((this.packedWireType != WireType.None) && (source.WireType == WireType.String))
            {
                SubItemToken token = ProtoReader.StartSubItem(source);
                if (flag)
                {
                    IList list = (IList) value;
                    while (ProtoReader.HasSubValue(this.packedWireType, source))
                    {
                        list.Add(base.Tail.Read(null, source));
                    }
                }
                else
                {
                    object[] parameters = new object[1];
                    while (ProtoReader.HasSubValue(this.packedWireType, source))
                    {
                        parameters[0] = base.Tail.Read(null, source);
                        this.add.Invoke(value, parameters);
                    }
                }
                ProtoReader.EndSubItem(token, source);
            }
            else if (flag)
            {
                IList list2 = (IList) value;
                do
                {
                    list2.Add(base.Tail.Read(null, source));
                }
                while (source.TryReadFieldHeader(fieldNumber));
            }
            else
            {
                object[] objArray2 = new object[1];
                do
                {
                    objArray2[0] = base.Tail.Read(null, source);
                    this.add.Invoke(value, objArray2);
                }
                while (source.TryReadFieldHeader(fieldNumber));
            }
            if (obj2 != value)
            {
                return value;
            }
            return null;
        }

        public override void Write(object value, ProtoWriter dest)
        {
            SubItemToken token;
            bool writePacked = this.WritePacked;
            if (writePacked)
            {
                ProtoWriter.WriteFieldHeader(this.fieldNumber, WireType.String, dest);
                token = ProtoWriter.StartSubItem(value, dest);
                ProtoWriter.SetPackedField(this.fieldNumber, dest);
            }
            else
            {
                token = new SubItemToken();
            }
            bool flag2 = !this.SupportNull;
            foreach (object obj2 in (IEnumerable) value)
            {
                if (flag2 && (obj2 == null))
                {
                    throw new NullReferenceException();
                }
                base.Tail.Write(obj2, dest);
            }
            if (writePacked)
            {
                ProtoWriter.EndSubItem(token, dest);
            }
        }

        private bool AppendToCollection
        {
            get
            {
                return ((this.options & 0x10) == 0);
            }
        }

        public override Type ExpectedType
        {
            get
            {
                return this.declaredType;
            }
        }

        private bool IsList
        {
            get
            {
                return ((this.options & 1) != 0);
            }
        }

        public override bool RequiresOldValue
        {
            get
            {
                return this.AppendToCollection;
            }
        }

        private bool ReturnList
        {
            get
            {
                return ((this.options & 8) != 0);
            }
        }

        public override bool ReturnsValue
        {
            get
            {
                return this.ReturnList;
            }
        }

        private bool SupportNull
        {
            get
            {
                return ((this.options & 0x20) != 0);
            }
        }

        private bool SuppressIList
        {
            get
            {
                return ((this.options & 2) != 0);
            }
        }

        private bool WritePacked
        {
            get
            {
                return ((this.options & 4) != 0);
            }
        }
    }
}

