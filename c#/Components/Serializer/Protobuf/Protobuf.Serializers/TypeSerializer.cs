namespace Components.Serialize.Protobuf.Serializers
{
    using Components.Serialize.Protobuf.Compiler;
    using Components.Serialize.Protobuf.Meta;
    using Components.Serialize.Protobuf.Protobuf;
    using System;
    using System.Reflection;
    using System.Runtime.Serialization;

    internal sealed class TypeSerializer : IProtoTypeSerializer, IProtoSerializer
    {
        private readonly MethodInfo[] baseCtorCallbacks;
        private readonly CallbackSet callbacks;
        private readonly Type constructType;
        private readonly MethodInfo factory;
        private readonly int[] fieldNumbers;
        private readonly Type forType;
        private readonly bool hasConstructor;
        private static readonly Type iextensible = typeof(IExtensible);
        private readonly bool isExtensible;
        private readonly bool isRootType;
        private readonly IProtoSerializer[] serializers;
        private readonly bool useConstructor;

        public TypeSerializer(TypeModel model, Type forType, int[] fieldNumbers, IProtoSerializer[] serializers, MethodInfo[] baseCtorCallbacks, bool isRootType, bool useConstructor, CallbackSet callbacks, Type constructType, MethodInfo factory)
        {
            Helpers.Sort(fieldNumbers, serializers);
            bool flag = false;
            for (int i = 1; i < fieldNumbers.Length; i++)
            {
                if (fieldNumbers[i] == fieldNumbers[i - 1])
                {
                    throw new InvalidOperationException("Duplicate field-number detected; " + fieldNumbers[i].ToString() + " on: " + forType.FullName);
                }
                if (!flag && (serializers[i].ExpectedType != forType))
                {
                    flag = true;
                }
            }
            this.forType = forType;
            this.factory = factory;
            if (constructType == null)
            {
                constructType = forType;
            }
            else if (!forType.IsAssignableFrom(constructType))
            {
                throw new InvalidOperationException(forType.FullName + " cannot be assigned from " + constructType.FullName);
            }
            this.constructType = constructType;
            this.serializers = serializers;
            this.fieldNumbers = fieldNumbers;
            this.callbacks = callbacks;
            this.isRootType = isRootType;
            this.useConstructor = useConstructor;
            if ((baseCtorCallbacks != null) && (baseCtorCallbacks.Length == 0))
            {
                baseCtorCallbacks = null;
            }
            this.baseCtorCallbacks = baseCtorCallbacks;
            if (Helpers.GetUnderlyingType(forType) != null)
            {
                throw new ArgumentException("Cannot create a TypeSerializer for nullable types", "forType");
            }
            if (model.MapType(iextensible).IsAssignableFrom(forType))
            {
                if ((forType.IsValueType || !isRootType) || flag)
                {
                    throw new NotSupportedException("IExtensible is not supported in structs or classes with inheritance");
                }
                this.isExtensible = true;
            }
            this.hasConstructor = !constructType.IsAbstract && (Helpers.GetConstructor(constructType, Helpers.EmptyTypes, true) != null);
            if (((constructType != forType) && useConstructor) && !this.hasConstructor)
            {
                throw new ArgumentException("The supplied default implementation cannot be created: " + constructType.FullName, "constructType");
            }
        }

        public void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context)
        {
            if (this.callbacks != null)
            {
                this.InvokeCallback(this.callbacks[callbackType], value, context);
            }
            IProtoTypeSerializer moreSpecificSerializer = (IProtoTypeSerializer) this.GetMoreSpecificSerializer(value);
            if (moreSpecificSerializer != null)
            {
                moreSpecificSerializer.Callback(value, callbackType, context);
            }
        }

        private object CreateInstance(ProtoReader source, bool includeLocalCallback)
        {
            object uninitializedObject;
            if (this.factory != null)
            {
                uninitializedObject = this.InvokeCallback(this.factory, null, source.Context);
            }
            else if (this.useConstructor)
            {
                if (!this.hasConstructor)
                {
                    TypeModel.ThrowCannotCreateInstance(this.constructType);
                }
                uninitializedObject = Activator.CreateInstance(this.constructType, true);
            }
            else
            {
                uninitializedObject = BclHelpers.GetUninitializedObject(this.constructType);
            }
            ProtoReader.NoteObject(uninitializedObject, source);
            if (this.baseCtorCallbacks != null)
            {
                for (int i = 0; i < this.baseCtorCallbacks.Length; i++)
                {
                    this.InvokeCallback(this.baseCtorCallbacks[i], uninitializedObject, source.Context);
                }
            }
            if (includeLocalCallback && (this.callbacks != null))
            {
                this.InvokeCallback(this.callbacks.BeforeDeserialize, uninitializedObject, source.Context);
            }
            return uninitializedObject;
        }

        private void EmitCallbackIfNeeded(CompilerContext ctx, Local valueFrom, TypeModel.CallbackType callbackType)
        {
            if (this.isRootType && this.HasCallbacks(callbackType))
            {
                ((IProtoTypeSerializer) this).EmitCallback(ctx, valueFrom, callbackType);
            }
        }

        private void EmitCreateIfNull(CompilerContext ctx, Local storage)
        {
            if (!this.ExpectedType.IsValueType)
            {
                CodeLabel label = ctx.DefineLabel();
                ctx.LoadValue(storage);
                ctx.BranchIfTrue(label, false);
                ((IProtoTypeSerializer) this).EmitCreateInstance(ctx);
                if (this.callbacks != null)
                {
                    EmitInvokeCallback(ctx, this.callbacks.BeforeDeserialize, true, null, this.forType);
                }
                ctx.StoreValue(storage);
                ctx.MarkLabel(label);
            }
        }

        private static void EmitInvokeCallback(CompilerContext ctx, MethodInfo method, bool copyValue, Type constructType, Type type)
        {
            if (method != null)
            {
                if (copyValue)
                {
                    ctx.CopyValue();
                }
                ParameterInfo[] parameters = method.GetParameters();
                bool flag = true;
                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[0].ParameterType;
                    if (parameterType == ctx.MapType(typeof(SerializationContext)))
                    {
                        ctx.LoadSerializationContext();
                    }
                    else if (parameterType == ctx.MapType(typeof(Type)))
                    {
                        Type type3 = constructType;
                        if (type3 == null)
                        {
                            type3 = type;
                        }
                        ctx.LoadValue(type3);
                    }
                    else if (parameterType == ctx.MapType(typeof(StreamingContext)))
                    {
                        ctx.LoadSerializationContext();
                        MethodInfo info = ctx.MapType(typeof(SerializationContext)).GetMethod("op_Implicit", new Type[] { ctx.MapType(typeof(SerializationContext)) });
                        if (info != null)
                        {
                            ctx.EmitCall(info);
                            flag = true;
                        }
                    }
                    else
                    {
                        flag = false;
                    }
                }
                if (!flag)
                {
                    throw CallbackSet.CreateInvalidCallbackSignature(method);
                }
                ctx.EmitCall(method);
                if ((constructType != null) && (method.ReturnType == ctx.MapType(typeof(object))))
                {
                    ctx.CastFromObject(type);
                }
            }
        }

        private IProtoSerializer GetMoreSpecificSerializer(object value)
        {
            if (this.CanHaveInheritance)
            {
                Type type = value.GetType();
                if (type == this.forType)
                {
                    return null;
                }
                for (int i = 0; i < this.serializers.Length; i++)
                {
                    IProtoSerializer serializer = this.serializers[i];
                    if ((serializer.ExpectedType != this.forType) && Helpers.IsAssignableFrom(serializer.ExpectedType, type))
                    {
                        return serializer;
                    }
                }
                if (type == this.constructType)
                {
                    return null;
                }
                TypeModel.ThrowUnexpectedSubtype(this.forType, type);
            }
            return null;
        }

        public bool HasCallbacks(TypeModel.CallbackType callbackType)
        {
            if ((this.callbacks != null) && (this.callbacks[callbackType] != null))
            {
                return true;
            }
            for (int i = 0; i < this.serializers.Length; i++)
            {
                if ((this.serializers[i].ExpectedType != this.forType) && ((IProtoTypeSerializer) this.serializers[i]).HasCallbacks(callbackType))
                {
                    return true;
                }
            }
            return false;
        }

        private object InvokeCallback(MethodInfo method, object obj, SerializationContext context)
        {
            object obj2 = null;
            object[] objArray;
            bool flag;
            if (method == null)
            {
                return obj2;
            }
            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                objArray = null;
                flag = true;
            }
            else
            {
                objArray = new object[parameters.Length];
                flag = true;
                for (int i = 0; i < objArray.Length; i++)
                {
                    object constructType;
                    Type parameterType = parameters[i].ParameterType;
                    if (parameterType == typeof(SerializationContext))
                    {
                        constructType = context;
                    }
                    else if (parameterType == typeof(Type))
                    {
                        constructType = this.constructType;
                    }
                    else if (parameterType == typeof(StreamingContext))
                    {
                        constructType = (StreamingContext) context;
                    }
                    else
                    {
                        constructType = null;
                        flag = false;
                    }
                    objArray[i] = constructType;
                }
            }
            if (!flag)
            {
                throw CallbackSet.CreateInvalidCallbackSignature(method);
            }
            return method.Invoke(obj, objArray);
        }

        void IProtoSerializer.EmitRead(CompilerContext ctx, Local valueFrom)
        {
            Type expectedType = this.ExpectedType;
            using (Local local = ctx.GetLocalWithValue(expectedType, valueFrom))
            {
                using (Local local2 = new Local(ctx, ctx.MapType(typeof(int))))
                {
                    if (this.HasCallbacks(TypeModel.CallbackType.BeforeDeserialize))
                    {
                        if (this.ExpectedType.IsValueType)
                        {
                            this.EmitCallbackIfNeeded(ctx, local, TypeModel.CallbackType.BeforeDeserialize);
                        }
                        else
                        {
                            CodeLabel label = ctx.DefineLabel();
                            ctx.LoadValue(local);
                            ctx.BranchIfFalse(label, false);
                            this.EmitCallbackIfNeeded(ctx, local, TypeModel.CallbackType.BeforeDeserialize);
                            ctx.MarkLabel(label);
                        }
                    }
                    CodeLabel label2 = ctx.DefineLabel();
                    CodeLabel label3 = ctx.DefineLabel();
                    ctx.Branch(label2, false);
                    ctx.MarkLabel(label3);
                    BasicList.NodeEnumerator enumerator = BasicList.GetContiguousGroups(this.fieldNumbers, this.serializers).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        BasicList.Group current = (BasicList.Group) enumerator.Current;
                        CodeLabel label4 = ctx.DefineLabel();
                        int count = current.Items.Count;
                        if (count == 1)
                        {
                            ctx.LoadValue(local2);
                            ctx.LoadValue(current.First);
                            CodeLabel label5 = ctx.DefineLabel();
                            ctx.BranchIfEqual(label5, true);
                            ctx.Branch(label4, false);
                            this.WriteFieldHandler(ctx, expectedType, local, label5, label2, (IProtoSerializer) current.Items[0]);
                        }
                        else
                        {
                            ctx.LoadValue(local2);
                            ctx.LoadValue(current.First);
                            ctx.Subtract();
                            CodeLabel[] jumpTable = new CodeLabel[count];
                            for (int i = 0; i < count; i++)
                            {
                                jumpTable[i] = ctx.DefineLabel();
                            }
                            ctx.Switch(jumpTable);
                            ctx.Branch(label4, false);
                            for (int j = 0; j < count; j++)
                            {
                                this.WriteFieldHandler(ctx, expectedType, local, jumpTable[j], label2, (IProtoSerializer) current.Items[j]);
                            }
                        }
                        ctx.MarkLabel(label4);
                    }
                    this.EmitCreateIfNull(ctx, local);
                    ctx.LoadReaderWriter();
                    if (this.isExtensible)
                    {
                        ctx.LoadValue(local);
                        ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("AppendExtensionData"));
                    }
                    else
                    {
                        ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("SkipField"));
                    }
                    ctx.MarkLabel(label2);
                    ctx.EmitBasicRead("ReadFieldHeader", ctx.MapType(typeof(int)));
                    ctx.CopyValue();
                    ctx.StoreValue(local2);
                    ctx.LoadValue(0);
                    ctx.BranchIfGreater(label3, false);
                    this.EmitCreateIfNull(ctx, local);
                    this.EmitCallbackIfNeeded(ctx, local, TypeModel.CallbackType.AfterDeserialize);
                    if ((valueFrom != null) && !local.IsSame(valueFrom))
                    {
                        ctx.LoadValue(local);
                        ctx.Cast(valueFrom.Type);
                        ctx.StoreValue(valueFrom);
                    }
                }
            }
        }

        void IProtoSerializer.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            Type expectedType = this.ExpectedType;
            using (Local local = ctx.GetLocalWithValue(expectedType, valueFrom))
            {
                this.EmitCallbackIfNeeded(ctx, local, TypeModel.CallbackType.BeforeSerialize);
                CodeLabel label = ctx.DefineLabel();
                if (!this.CanHaveInheritance)
                {
                    goto Label_0206;
                }
                for (int i = 0; i < this.serializers.Length; i++)
                {
                    IProtoSerializer serializer = this.serializers[i];
                    Type type = serializer.ExpectedType;
                    if (type != this.forType)
                    {
                        CodeLabel label2 = ctx.DefineLabel();
                        CodeLabel label3 = ctx.DefineLabel();
                        ctx.LoadValue(local);
                        ctx.TryCast(type);
                        ctx.CopyValue();
                        ctx.BranchIfTrue(label2, true);
                        ctx.DiscardValue();
                        ctx.Branch(label3, true);
                        ctx.MarkLabel(label2);
                        serializer.EmitWrite(ctx, null);
                        ctx.Branch(label, false);
                        ctx.MarkLabel(label3);
                    }
                }
                if ((this.constructType != null) && (this.constructType != this.forType))
                {
                    using (Local local2 = new Local(ctx, ctx.MapType(typeof(Type))))
                    {
                        ctx.LoadValue(local);
                        ctx.EmitCall(ctx.MapType(typeof(object)).GetMethod("GetType"));
                        ctx.CopyValue();
                        ctx.StoreValue(local2);
                        ctx.LoadValue(this.forType);
                        ctx.BranchIfEqual(label, true);
                        ctx.LoadValue(local2);
                        ctx.LoadValue(this.constructType);
                        ctx.BranchIfEqual(label, true);
                        goto Label_01B1;
                    }
                }
                ctx.LoadValue(local);
                ctx.EmitCall(ctx.MapType(typeof(object)).GetMethod("GetType"));
                ctx.LoadValue(this.forType);
                ctx.BranchIfEqual(label, true);
            Label_01B1:
                ctx.LoadValue(this.forType);
                ctx.LoadValue(local);
                ctx.EmitCall(ctx.MapType(typeof(object)).GetMethod("GetType"));
                ctx.EmitCall(ctx.MapType(typeof(TypeModel)).GetMethod("ThrowUnexpectedSubtype", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static));
            Label_0206:
                ctx.MarkLabel(label);
                for (int j = 0; j < this.serializers.Length; j++)
                {
                    IProtoSerializer serializer2 = this.serializers[j];
                    if (serializer2.ExpectedType == this.forType)
                    {
                        serializer2.EmitWrite(ctx, local);
                    }
                }
                if (this.isExtensible)
                {
                    ctx.LoadValue(local);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(ctx.MapType(typeof(ProtoWriter)).GetMethod("AppendExtensionData"));
                }
                this.EmitCallbackIfNeeded(ctx, local, TypeModel.CallbackType.AfterSerialize);
            }
        }

        bool IProtoTypeSerializer.CanCreateInstance()
        {
            return true;
        }

        object IProtoTypeSerializer.CreateInstance(ProtoReader source)
        {
            return this.CreateInstance(source, false);
        }

        void IProtoTypeSerializer.EmitCallback(CompilerContext ctx, Local valueFrom, TypeModel.CallbackType callbackType)
        {
            bool copyValue = false;
            if (this.CanHaveInheritance)
            {
                for (int i = 0; i < this.serializers.Length; i++)
                {
                    IProtoSerializer serializer = this.serializers[i];
                    if ((serializer.ExpectedType != this.forType) && ((IProtoTypeSerializer) serializer).HasCallbacks(callbackType))
                    {
                        copyValue = true;
                    }
                }
            }
            MethodInfo method = (this.callbacks == null) ? null : this.callbacks[callbackType];
            if ((method != null) || copyValue)
            {
                ctx.LoadAddress(valueFrom, this.ExpectedType);
                EmitInvokeCallback(ctx, method, copyValue, null, this.forType);
                if (copyValue)
                {
                    CodeLabel label = ctx.DefineLabel();
                    for (int j = 0; j < this.serializers.Length; j++)
                    {
                        IProtoTypeSerializer serializer3;
                        IProtoSerializer serializer2 = this.serializers[j];
                        Type expectedType = serializer2.ExpectedType;
                        if ((expectedType != this.forType) && (serializer3 = (IProtoTypeSerializer) serializer2).HasCallbacks(callbackType))
                        {
                            CodeLabel label2 = ctx.DefineLabel();
                            CodeLabel label3 = ctx.DefineLabel();
                            ctx.CopyValue();
                            ctx.TryCast(expectedType);
                            ctx.CopyValue();
                            ctx.BranchIfTrue(label2, true);
                            ctx.DiscardValue();
                            ctx.Branch(label3, false);
                            ctx.MarkLabel(label2);
                            serializer3.EmitCallback(ctx, null, callbackType);
                            ctx.Branch(label, false);
                            ctx.MarkLabel(label3);
                        }
                    }
                    ctx.MarkLabel(label);
                    ctx.DiscardValue();
                }
            }
        }

        void IProtoTypeSerializer.EmitCreateInstance(CompilerContext ctx)
        {
            bool flag = true;
            if (this.factory != null)
            {
                EmitInvokeCallback(ctx, this.factory, false, this.constructType, this.forType);
            }
            else if (!this.useConstructor)
            {
                ctx.LoadValue(this.constructType);
                ctx.EmitCall(ctx.MapType(typeof(BclHelpers)).GetMethod("GetUninitializedObject"));
                ctx.Cast(this.forType);
            }
            else if (this.constructType.IsClass && this.hasConstructor)
            {
                ctx.EmitCtor(this.constructType);
            }
            else
            {
                ctx.LoadValue(this.ExpectedType);
                ctx.EmitCall(ctx.MapType(typeof(TypeModel)).GetMethod("ThrowCannotCreateInstance", BindingFlags.Public | BindingFlags.Static));
                ctx.LoadNullRef();
                flag = false;
            }
            if (flag)
            {
                ctx.CopyValue();
                ctx.LoadReaderWriter();
                ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("NoteObject", BindingFlags.Public | BindingFlags.Static));
            }
            if (this.baseCtorCallbacks != null)
            {
                for (int i = 0; i < this.baseCtorCallbacks.Length; i++)
                {
                    EmitInvokeCallback(ctx, this.baseCtorCallbacks[i], true, null, this.forType);
                }
            }
        }

        public object Read(object value, ProtoReader source)
        {
            int num;
            if (this.isRootType && (value != null))
            {
                this.Callback(value, TypeModel.CallbackType.BeforeDeserialize, source.Context);
            }
            int num2 = 0;
            int num3 = 0;
            while ((num = source.ReadFieldHeader()) > 0)
            {
                bool flag = false;
                if (num < num2)
                {
                    num2 = num3 = 0;
                }
                for (int i = num3; i < this.fieldNumbers.Length; i++)
                {
                    if (this.fieldNumbers[i] == num)
                    {
                        IProtoSerializer serializer = this.serializers[i];
                        Type expectedType = serializer.ExpectedType;
                        if (value == null)
                        {
                            if (expectedType == this.forType)
                            {
                                value = this.CreateInstance(source, true);
                            }
                        }
                        else if (((expectedType != this.forType) && ((IProtoTypeSerializer) serializer).CanCreateInstance()) && expectedType.IsSubclassOf(value.GetType()))
                        {
                            value = ProtoReader.Merge(source, value, ((IProtoTypeSerializer) serializer).CreateInstance(source));
                        }
                        if (serializer.ReturnsValue)
                        {
                            value = serializer.Read(value, source);
                        }
                        else
                        {
                            serializer.Read(value, source);
                        }
                        num3 = i;
                        num2 = num;
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    if (value == null)
                    {
                        value = this.CreateInstance(source, true);
                    }
                    if (this.isExtensible)
                    {
                        source.AppendExtensionData((IExtensible) value);
                        continue;
                    }
                    source.SkipField();
                }
            }
            if (value == null)
            {
                value = this.CreateInstance(source, true);
            }
            if (this.isRootType)
            {
                this.Callback(value, TypeModel.CallbackType.AfterDeserialize, source.Context);
            }
            return value;
        }

        public void Write(object value, ProtoWriter dest)
        {
            if (this.isRootType)
            {
                this.Callback(value, TypeModel.CallbackType.BeforeSerialize, dest.Context);
            }
            IProtoSerializer moreSpecificSerializer = this.GetMoreSpecificSerializer(value);
            if (moreSpecificSerializer != null)
            {
                moreSpecificSerializer.Write(value, dest);
            }
            for (int i = 0; i < this.serializers.Length; i++)
            {
                IProtoSerializer serializer2 = this.serializers[i];
                if (serializer2.ExpectedType == this.forType)
                {
                    serializer2.Write(value, dest);
                }
            }
            if (this.isExtensible)
            {
                ProtoWriter.AppendExtensionData((IExtensible) value, dest);
            }
            if (this.isRootType)
            {
                this.Callback(value, TypeModel.CallbackType.AfterSerialize, dest.Context);
            }
        }

        private void WriteFieldHandler(CompilerContext ctx, Type expected, Local loc, CodeLabel handler, CodeLabel @continue, IProtoSerializer serializer)
        {
            ctx.MarkLabel(handler);
            Type expectedType = serializer.ExpectedType;
            if (expectedType == this.forType)
            {
                this.EmitCreateIfNull(ctx, loc);
                serializer.EmitRead(ctx, loc);
            }
            else
            {
                RuntimeTypeModel model = (RuntimeTypeModel) ctx.Model;
                if (((IProtoTypeSerializer) serializer).CanCreateInstance())
                {
                    CodeLabel label = ctx.DefineLabel();
                    ctx.LoadValue(loc);
                    ctx.BranchIfFalse(label, false);
                    ctx.LoadValue(loc);
                    ctx.TryCast(expectedType);
                    ctx.BranchIfTrue(label, false);
                    ctx.LoadReaderWriter();
                    ctx.LoadValue(loc);
                    ((IProtoTypeSerializer) serializer).EmitCreateInstance(ctx);
                    ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("Merge"));
                    ctx.Cast(expected);
                    ctx.StoreValue(loc);
                    ctx.MarkLabel(label);
                }
                ctx.LoadValue(loc);
                ctx.Cast(expectedType);
                serializer.EmitRead(ctx, null);
            }
            if (serializer.ReturnsValue)
            {
                ctx.StoreValue(loc);
            }
            ctx.Branch(@continue, false);
        }

        private bool CanHaveInheritance
        {
            get
            {
                if (!this.forType.IsClass && !this.forType.IsInterface)
                {
                    return false;
                }
                return !this.forType.IsSealed;
            }
        }

        public Type ExpectedType
        {
            get
            {
                return this.forType;
            }
        }

        bool IProtoSerializer.RequiresOldValue
        {
            get
            {
                return true;
            }
        }

        bool IProtoSerializer.ReturnsValue
        {
            get
            {
                return false;
            }
        }
    }
}

