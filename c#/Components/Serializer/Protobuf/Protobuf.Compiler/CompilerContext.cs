namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Compiler
{
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf;
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers;
    using System;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal sealed class CompilerContext
    {
        private readonly string assemblyName;
        private readonly ILGenerator il;
        private readonly Local inputValue;
        private readonly bool isStatic;
        private readonly bool isWriter;
        private BasicList knownTrustedAssemblies;
        private BasicList knownUntrustedAssemblies;
        private MutableList locals;
        private readonly ILVersion metadataVersion;
        private readonly DynamicMethod method;
        private readonly RuntimeTypeModel.SerializerPair[] methodPairs;
        private readonly TypeModel model;
        private static int next;
        private int nextLabel;
        private readonly bool nonPublic;

        private CompilerContext(Type associatedType, bool isWriter, bool isStatic, TypeModel model, Type inputType)
        {
            Type[] typeArray;
            Type type;
            this.locals = new MutableList();
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            this.metadataVersion = ILVersion.Net2;
            this.isStatic = isStatic;
            this.isWriter = isWriter;
            this.model = model;
            this.nonPublic = true;
            if (isWriter)
            {
                type = typeof(void);
                typeArray = new Type[] { typeof(object), typeof(ProtoWriter) };
            }
            else
            {
                type = typeof(object);
                typeArray = new Type[] { typeof(object), typeof(ProtoReader) };
            }
            int num = Interlocked.Increment(ref next);
            this.method = new DynamicMethod("proto_" + num.ToString(), type, typeArray, associatedType.IsInterface ? typeof(object) : associatedType, true);
            this.il = this.method.GetILGenerator();
            if (inputType != null)
            {
                this.inputValue = new Local(null, inputType);
            }
        }

        internal CompilerContext(ILGenerator il, bool isStatic, bool isWriter, RuntimeTypeModel.SerializerPair[] methodPairs, TypeModel model, ILVersion metadataVersion, string assemblyName, Type inputType)
        {
            this.locals = new MutableList();
            if (il == null)
            {
                throw new ArgumentNullException("il");
            }
            if (methodPairs == null)
            {
                throw new ArgumentNullException("methodPairs");
            }
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }
            if (Helpers.IsNullOrEmpty(assemblyName))
            {
                throw new ArgumentNullException("assemblyName");
            }
            this.assemblyName = assemblyName;
            this.isStatic = isStatic;
            this.methodPairs = methodPairs;
            this.il = il;
            this.isWriter = isWriter;
            this.model = model;
            this.metadataVersion = metadataVersion;
            if (inputType != null)
            {
                this.inputValue = new Local(null, inputType);
            }
        }

        internal void Add()
        {
            this.Emit(OpCodes.Add);
        }

        internal bool AllowInternal(PropertyInfo property)
        {
            if (!this.NonPublic)
            {
                return this.InternalsVisible(property.DeclaringType.Assembly);
            }
            return true;
        }

        internal void BeginFinally()
        {
            this.il.BeginFinallyBlock();
        }

        internal CodeLabel BeginTry()
        {
            return new CodeLabel(this.il.BeginExceptionBlock(), this.nextLabel++);
        }

        internal void Branch(CodeLabel label, bool @short)
        {
            OpCode opcode = @short ? OpCodes.Br_S : OpCodes.Br;
            this.il.Emit(opcode, label.Value);
        }

        internal void BranchIfEqual(CodeLabel label, bool @short)
        {
            OpCode opcode = @short ? OpCodes.Beq_S : OpCodes.Beq;
            this.il.Emit(opcode, label.Value);
        }

        internal void BranchIfFalse(CodeLabel label, bool @short)
        {
            OpCode opcode = @short ? OpCodes.Brfalse_S : OpCodes.Brfalse;
            this.il.Emit(opcode, label.Value);
        }

        internal void BranchIfGreater(CodeLabel label, bool @short)
        {
            OpCode opcode = @short ? OpCodes.Bgt_S : OpCodes.Bgt;
            this.il.Emit(opcode, label.Value);
        }

        internal void BranchIfLess(CodeLabel label, bool @short)
        {
            OpCode opcode = @short ? OpCodes.Blt_S : OpCodes.Blt;
            this.il.Emit(opcode, label.Value);
        }

        internal void BranchIfTrue(CodeLabel label, bool @short)
        {
            OpCode opcode = @short ? OpCodes.Brtrue_S : OpCodes.Brtrue;
            this.il.Emit(opcode, label.Value);
        }

        public static ProtoDeserializer BuildDeserializer(IProtoSerializer head, TypeModel model)
        {
            Type expectedType = head.ExpectedType;
            CompilerContext ctx = new CompilerContext(expectedType, false, true, model, typeof(object));
            using (Local local = new Local(ctx, expectedType))
            {
                if (!expectedType.IsValueType)
                {
                    ctx.LoadValue(ctx.InputValue);
                    ctx.CastFromObject(expectedType);
                    ctx.StoreValue(local);
                }
                else
                {
                    ctx.LoadValue(ctx.InputValue);
                    CodeLabel label = ctx.DefineLabel();
                    CodeLabel label2 = ctx.DefineLabel();
                    ctx.BranchIfTrue(label, true);
                    ctx.LoadAddress(local, expectedType);
                    ctx.EmitCtor(expectedType);
                    ctx.Branch(label2, true);
                    ctx.MarkLabel(label);
                    ctx.LoadValue(ctx.InputValue);
                    ctx.CastFromObject(expectedType);
                    ctx.StoreValue(local);
                    ctx.MarkLabel(label2);
                }
                head.EmitRead(ctx, local);
                if (head.ReturnsValue)
                {
                    ctx.StoreValue(local);
                }
                ctx.LoadValue(local);
                ctx.CastToObject(expectedType);
            }
            ctx.Emit(OpCodes.Ret);
            return (ProtoDeserializer) ctx.method.CreateDelegate(typeof(ProtoDeserializer));
        }

        public static ProtoSerializer BuildSerializer(IProtoSerializer head, TypeModel model)
        {
            Type expectedType = head.ExpectedType;
            CompilerContext context = new CompilerContext(expectedType, true, true, model, typeof(object));
            context.LoadValue(context.InputValue);
            context.CastFromObject(expectedType);
            context.WriteNullCheckedTail(expectedType, head, null);
            context.Emit(OpCodes.Ret);
            return (ProtoSerializer) context.method.CreateDelegate(typeof(ProtoSerializer));
        }

        internal void Cast(Type type)
        {
            this.il.Emit(OpCodes.Castclass, type);
        }

        internal void CastFromObject(Type type)
        {
            if (!IsObject(type))
            {
                if (type.IsValueType)
                {
                    if (this.MetadataVersion == ILVersion.Net1)
                    {
                        this.il.Emit(OpCodes.Unbox, type);
                        this.il.Emit(OpCodes.Ldobj, type);
                    }
                    else
                    {
                        this.il.Emit(OpCodes.Unbox_Any, type);
                    }
                }
                else
                {
                    this.il.Emit(OpCodes.Castclass, type);
                }
            }
        }

        internal void CastToObject(Type type)
        {
            if (!IsObject(type))
            {
                if (type.IsValueType)
                {
                    this.il.Emit(OpCodes.Box, type);
                }
                else
                {
                    this.il.Emit(OpCodes.Castclass, this.MapType(typeof(object)));
                }
            }
        }

        internal void CheckAccessibility(MemberInfo member)
        {
            bool flag;
            if (member == null)
            {
                throw new ArgumentNullException("member");
            }
            MemberTypes memberType = member.MemberType;
            if (this.NonPublic)
            {
                return;
            }
            MemberTypes types2 = memberType;
            if (types2 <= MemberTypes.Method)
            {
                switch (types2)
                {
                    case MemberTypes.Constructor:
                    {
                        ConstructorInfo info2 = (ConstructorInfo) member;
                        flag = info2.IsPublic || ((info2.IsAssembly || info2.IsFamilyOrAssembly) && this.InternalsVisible(info2.DeclaringType.Assembly));
                        goto Label_01E6;
                    }
                    case MemberTypes.Field:
                    {
                        FieldInfo info = (FieldInfo) member;
                        flag = info.IsPublic || ((info.IsAssembly || info.IsFamilyOrAssembly) && this.InternalsVisible(info.DeclaringType.Assembly));
                        goto Label_01E6;
                    }
                    case MemberTypes.Method:
                    {
                        MethodInfo info3 = (MethodInfo) member;
                        flag = info3.IsPublic || ((info3.IsAssembly || info3.IsFamilyOrAssembly) && this.InternalsVisible(info3.DeclaringType.Assembly));
                        if (!flag && ((member is MethodBuilder) || (member.DeclaringType == this.MapType(typeof(TypeModel)))))
                        {
                            flag = true;
                        }
                        goto Label_01E6;
                    }
                }
            }
            else
            {
                if (types2 == MemberTypes.Property)
                {
                    flag = true;
                }
                else
                {
                    Type type;
                    if (types2 != MemberTypes.TypeInfo)
                    {
                        if (types2 == MemberTypes.NestedType)
                        {
                            type = (Type) member;
                            do
                            {
                                flag = (type.IsNestedPublic || type.IsPublic) || ((((type.DeclaringType == null) || type.IsNestedAssembly) || type.IsNestedFamORAssem) && this.InternalsVisible(type.Assembly));
                                if (!flag)
                                {
                                    break;
                                }
                            }
                            while ((type = type.DeclaringType) != null);
                            goto Label_01E6;
                        }
                        goto Label_01D5;
                    }
                    type = (Type) member;
                    flag = type.IsPublic || this.InternalsVisible(type.Assembly);
                }
                goto Label_01E6;
            }
        Label_01D5:
            throw new NotSupportedException(memberType.ToString());
        Label_01E6:
            if (!flag)
            {
                switch (memberType)
                {
                    case (MemberTypes.TypeInfo & MemberTypes.NestedType):
                        throw new InvalidOperationException("Non-public member cannot be used with full dll compilation: " + member.DeclaringType.FullName + "." + member.Name);
                }
                throw new InvalidOperationException("Non-public type cannot be used with full dll compilation: " + ((Type) member).FullName);
            }
        }

        internal void Constrain(Type type)
        {
            this.il.Emit(OpCodes.Constrained, type);
        }

        internal void ConvertFromInt32(ProtoTypeCode typeCode, bool uint32Overflow)
        {
            switch (typeCode)
            {
                case ProtoTypeCode.SByte:
                    this.Emit(OpCodes.Conv_Ovf_I1);
                    return;

                case ProtoTypeCode.Byte:
                    this.Emit(OpCodes.Conv_Ovf_U1);
                    return;

                case ProtoTypeCode.Int16:
                    this.Emit(OpCodes.Conv_Ovf_I2);
                    return;

                case ProtoTypeCode.UInt16:
                    this.Emit(OpCodes.Conv_Ovf_U2);
                    return;

                case ProtoTypeCode.Int32:
                    return;

                case ProtoTypeCode.UInt32:
                    this.Emit(uint32Overflow ? OpCodes.Conv_Ovf_U4 : OpCodes.Conv_U4);
                    return;

                case ProtoTypeCode.Int64:
                    this.Emit(OpCodes.Conv_I8);
                    return;

                case ProtoTypeCode.UInt64:
                    this.Emit(OpCodes.Conv_U8);
                    return;
            }
            throw new InvalidOperationException();
        }

        internal void ConvertToInt32(ProtoTypeCode typeCode, bool uint32Overflow)
        {
            switch (typeCode)
            {
                case ProtoTypeCode.SByte:
                case ProtoTypeCode.Byte:
                case ProtoTypeCode.Int16:
                case ProtoTypeCode.UInt16:
                    this.Emit(OpCodes.Conv_I4);
                    return;

                case ProtoTypeCode.Int32:
                    return;

                case ProtoTypeCode.UInt32:
                    this.Emit(uint32Overflow ? OpCodes.Conv_Ovf_I4_Un : OpCodes.Conv_Ovf_I4);
                    return;

                case ProtoTypeCode.Int64:
                    this.Emit(OpCodes.Conv_Ovf_I4);
                    return;

                case ProtoTypeCode.UInt64:
                    this.Emit(OpCodes.Conv_Ovf_I4_Un);
                    return;
            }
            throw new InvalidOperationException("ConvertToInt32 not implemented for: " + typeCode.ToString());
        }

        internal void CopyValue()
        {
            this.Emit(OpCodes.Dup);
        }

        internal void CreateArray(Type elementType, Local length)
        {
            this.LoadValue(length);
            this.il.Emit(OpCodes.Newarr, elementType);
        }

        internal CodeLabel DefineLabel()
        {
            return new CodeLabel(this.il.DefineLabel(), this.nextLabel++);
        }

        internal void DiscardValue()
        {
            this.Emit(OpCodes.Pop);
        }

        private void Emit(OpCode opcode)
        {
            this.il.Emit(opcode);
        }

        internal void EmitBasicRead(string methodName, Type expectedType)
        {
            MethodInfo method = this.MapType(typeof(ProtoReader)).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (((method == null) || (method.ReturnType != expectedType)) || (method.GetParameters().Length != 0))
            {
                throw new ArgumentException("methodName");
            }
            this.LoadReaderWriter();
            this.EmitCall(method);
        }

        internal void EmitBasicRead(Type helperType, string methodName, Type expectedType)
        {
            MethodInfo method = helperType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if (((method == null) || (method.ReturnType != expectedType)) || (method.GetParameters().Length != 1))
            {
                throw new ArgumentException("methodName");
            }
            this.LoadReaderWriter();
            this.EmitCall(method);
        }

        internal void EmitBasicWrite(string methodName, Local fromValue)
        {
            if (Helpers.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException("methodName");
            }
            this.LoadValue(fromValue);
            this.LoadReaderWriter();
            this.EmitCall(this.GetWriterMethod(methodName));
        }

        public void EmitCall(MethodInfo method)
        {
            this.CheckAccessibility(method);
            OpCode opcode = (method.IsStatic || method.DeclaringType.IsValueType) ? OpCodes.Call : OpCodes.Callvirt;
            this.il.EmitCall(opcode, method, null);
        }

        public void EmitCtor(ConstructorInfo ctor)
        {
            if (ctor == null)
            {
                throw new ArgumentNullException("ctor");
            }
            this.CheckAccessibility(ctor);
            this.il.Emit(OpCodes.Newobj, ctor);
        }

        public void EmitCtor(Type type)
        {
            this.EmitCtor(type, Helpers.EmptyTypes);
        }

        public void EmitCtor(Type type, params Type[] parameterTypes)
        {
            if (type.IsValueType && (parameterTypes.Length == 0))
            {
                this.il.Emit(OpCodes.Initobj, type);
            }
            else
            {
                ConstructorInfo ctor = Helpers.GetConstructor(type, parameterTypes, true);
                if (ctor == null)
                {
                    throw new InvalidOperationException("No suitable constructor found for " + type.FullName);
                }
                this.EmitCtor(ctor);
            }
        }

        internal void EmitWrite(Type helperType, string methodName, Local valueFrom)
        {
            if (Helpers.IsNullOrEmpty(methodName))
            {
                throw new ArgumentNullException("methodName");
            }
            MethodInfo method = helperType.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            if ((method == null) || (method.ReturnType != this.MapType(typeof(void))))
            {
                throw new ArgumentException("methodName");
            }
            this.LoadValue(valueFrom);
            this.LoadReaderWriter();
            this.EmitCall(method);
        }

        internal void EndFinally()
        {
            this.il.EndExceptionBlock();
        }

        internal void EndTry(CodeLabel label, bool @short)
        {
            OpCode opcode = @short ? OpCodes.Leave_S : OpCodes.Leave;
            this.il.Emit(opcode, label.Value);
        }

        internal MethodBuilder GetDedicatedMethod(int metaKey, bool read)
        {
            if (this.methodPairs == null)
            {
                return null;
            }
            for (int i = 0; i < this.methodPairs.Length; i++)
            {
                if (this.methodPairs[i].MetaKey == metaKey)
                {
                    if (!read)
                    {
                        return this.methodPairs[i].Serialize;
                    }
                    return this.methodPairs[i].Deserialize;
                }
            }
            throw new ArgumentException("Meta-key not found", "metaKey");
        }

        internal LocalBuilder GetFromPool(Type type)
        {
            int count = this.locals.Count;
            for (int i = 0; i < count; i++)
            {
                LocalBuilder builder = (LocalBuilder) this.locals[i];
                if ((builder != null) && (builder.LocalType == type))
                {
                    this.locals[i] = null;
                    return builder;
                }
            }
            return this.il.DeclareLocal(type);
        }

        public Local GetLocalWithValue(Type type, Local fromValue)
        {
            if (fromValue != null)
            {
                if (fromValue.Type == type)
                {
                    return fromValue.AsCopy();
                }
                this.LoadValue(fromValue);
                if (!type.IsValueType && ((fromValue.Type == null) || !type.IsAssignableFrom(fromValue.Type)))
                {
                    this.Cast(type);
                }
            }
            Local local = new Local(this, type);
            this.StoreValue(local);
            return local;
        }

        private MethodInfo GetWriterMethod(string methodName)
        {
            Type type = this.MapType(typeof(ProtoWriter));
            foreach (MethodInfo info in type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                if (!(info.Name != methodName))
                {
                    ParameterInfo[] parameters = info.GetParameters();
                    if ((parameters.Length == 2) && (parameters[1].ParameterType == type))
                    {
                        return info;
                    }
                }
            }
            throw new ArgumentException("No suitable method found for: " + methodName, "methodName");
        }

        private bool InternalsVisible(Assembly assembly)
        {
            if (Helpers.IsNullOrEmpty(this.assemblyName))
            {
                return false;
            }
            if ((this.knownTrustedAssemblies != null) && (this.knownTrustedAssemblies.IndexOfReference(assembly) >= 0))
            {
                return true;
            }
            if ((this.knownUntrustedAssemblies != null) && (this.knownUntrustedAssemblies.IndexOfReference(assembly) >= 0))
            {
                return false;
            }
            bool flag = false;
            Type attributeType = this.MapType(typeof(InternalsVisibleToAttribute));
            if (attributeType == null)
            {
                return false;
            }
            foreach (InternalsVisibleToAttribute attribute in assembly.GetCustomAttributes(attributeType, false))
            {
                if ((attribute.AssemblyName == this.assemblyName) || attribute.AssemblyName.StartsWith(this.assemblyName + ","))
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
            {
                if (this.knownTrustedAssemblies == null)
                {
                    this.knownTrustedAssemblies = new BasicList();
                }
                this.knownTrustedAssemblies.Add(assembly);
                return flag;
            }
            if (this.knownUntrustedAssemblies == null)
            {
                this.knownUntrustedAssemblies = new BasicList();
            }
            this.knownUntrustedAssemblies.Add(assembly);
            return flag;
        }

        private static bool IsObject(Type type)
        {
            return (type == typeof(object));
        }

        internal void LoadAddress(Local local, Type type)
        {
            if (type.IsValueType)
            {
                if (local == null)
                {
                    throw new InvalidOperationException("Cannot load the address of a struct at the head of the stack");
                }
                if (local == this.InputValue)
                {
                    this.il.Emit(OpCodes.Ldarga_S, this.isStatic ? ((byte) 0) : ((byte) 1));
                }
                else
                {
                    OpCode opcode = this.UseShortForm(local) ? OpCodes.Ldloca_S : OpCodes.Ldloca;
                    this.il.Emit(opcode, local.Value);
                }
            }
            else
            {
                this.LoadValue(local);
            }
        }

        internal void LoadArrayValue(Local arr, Local i)
        {
            Type elementType = arr.Type.GetElementType();
            this.LoadValue(arr);
            this.LoadValue(i);
            switch (Helpers.GetTypeCode(elementType))
            {
                case ProtoTypeCode.SByte:
                    this.Emit(OpCodes.Ldelem_I1);
                    return;

                case ProtoTypeCode.Byte:
                    this.Emit(OpCodes.Ldelem_U1);
                    return;

                case ProtoTypeCode.Int16:
                    this.Emit(OpCodes.Ldelem_I2);
                    return;

                case ProtoTypeCode.UInt16:
                    this.Emit(OpCodes.Ldelem_U2);
                    return;

                case ProtoTypeCode.Int32:
                    this.Emit(OpCodes.Ldelem_I4);
                    return;

                case ProtoTypeCode.UInt32:
                    this.Emit(OpCodes.Ldelem_U4);
                    return;

                case ProtoTypeCode.Int64:
                    this.Emit(OpCodes.Ldelem_I8);
                    return;

                case ProtoTypeCode.UInt64:
                    this.Emit(OpCodes.Ldelem_I8);
                    return;

                case ProtoTypeCode.Single:
                    this.Emit(OpCodes.Ldelem_R4);
                    return;

                case ProtoTypeCode.Double:
                    this.Emit(OpCodes.Ldelem_R8);
                    return;
            }
            if (elementType.IsValueType)
            {
                this.il.Emit(OpCodes.Ldelema, elementType);
                this.il.Emit(OpCodes.Ldobj, elementType);
            }
            else
            {
                this.Emit(OpCodes.Ldelem_Ref);
            }
        }

        internal void LoadLength(Local arr, bool zeroIfNull)
        {
            if (zeroIfNull)
            {
                CodeLabel label = this.DefineLabel();
                CodeLabel label2 = this.DefineLabel();
                this.LoadValue(arr);
                this.CopyValue();
                this.BranchIfTrue(label, true);
                this.DiscardValue();
                this.LoadValue(0);
                this.Branch(label2, true);
                this.MarkLabel(label);
                this.Emit(OpCodes.Ldlen);
                this.Emit(OpCodes.Conv_I4);
                this.MarkLabel(label2);
            }
            else
            {
                this.LoadValue(arr);
                this.Emit(OpCodes.Ldlen);
                this.Emit(OpCodes.Conv_I4);
            }
        }

        public void LoadNullRef()
        {
            this.Emit(OpCodes.Ldnull);
        }

        public void LoadReaderWriter()
        {
            this.Emit(this.isStatic ? OpCodes.Ldarg_1 : OpCodes.Ldarg_2);
        }

        internal void LoadSerializationContext()
        {
            this.LoadReaderWriter();
            this.LoadValue((this.isWriter ? typeof(ProtoWriter) : typeof(ProtoReader)).GetProperty("Context"));
        }

        public void LoadValue(Local local)
        {
            if (local != null)
            {
                if (local == this.InputValue)
                {
                    this.Emit(this.isStatic ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                }
                else
                {
                    switch (local.Value.LocalIndex)
                    {
                        case 0:
                            this.Emit(OpCodes.Ldloc_0);
                            return;

                        case 1:
                            this.Emit(OpCodes.Ldloc_1);
                            return;

                        case 2:
                            this.Emit(OpCodes.Ldloc_2);
                            return;

                        case 3:
                            this.Emit(OpCodes.Ldloc_3);
                            return;
                    }
                    OpCode opcode = this.UseShortForm(local) ? OpCodes.Ldloc_S : OpCodes.Ldloc;
                    this.il.Emit(opcode, local.Value);
                }
            }
        }

        internal void LoadValue(decimal value)
        {
            if (value == 0M)
            {
                this.LoadValue(typeof(decimal).GetField("Zero"));
            }
            else
            {
                int[] bits = decimal.GetBits(value);
                this.LoadValue(bits[0]);
                this.LoadValue(bits[1]);
                this.LoadValue(bits[2]);
                this.LoadValue((int) (bits[3] >> 0x1f));
                this.LoadValue((int) ((bits[3] >> 0x10) & 0xff));
                this.EmitCtor(this.MapType(typeof(decimal)), new Type[] { this.MapType(typeof(int)), this.MapType(typeof(int)), this.MapType(typeof(int)), this.MapType(typeof(bool)), this.MapType(typeof(byte)) });
            }
        }

        public void LoadValue(double value)
        {
            this.il.Emit(OpCodes.Ldc_R8, value);
        }

        internal void LoadValue(Guid value)
        {
            if (value == Guid.Empty)
            {
                this.LoadValue(typeof(Guid).GetField("Empty"));
            }
            else
            {
                byte[] buffer = value.ToByteArray();
                int num = ((buffer[0] | (buffer[1] << 8)) | (buffer[2] << 0x10)) | (buffer[3] << 0x18);
                this.LoadValue(num);
                short num2 = (short) (buffer[4] | (buffer[5] << 8));
                this.LoadValue((int) num2);
                num2 = (short) (buffer[6] | (buffer[7] << 8));
                this.LoadValue((int) num2);
                for (num = 8; num <= 15; num++)
                {
                    this.LoadValue((int) buffer[num]);
                }
                this.EmitCtor(this.MapType(typeof(Guid)), new Type[] { this.MapType(typeof(int)), this.MapType(typeof(short)), this.MapType(typeof(short)), this.MapType(typeof(byte)), this.MapType(typeof(byte)), this.MapType(typeof(byte)), this.MapType(typeof(byte)), this.MapType(typeof(byte)), this.MapType(typeof(byte)), this.MapType(typeof(byte)), this.MapType(typeof(byte)) });
            }
        }

        public void LoadValue(int value)
        {
            switch (value)
            {
                case -1:
                    this.Emit(OpCodes.Ldc_I4_M1);
                    return;

                case 0:
                    this.Emit(OpCodes.Ldc_I4_0);
                    return;

                case 1:
                    this.Emit(OpCodes.Ldc_I4_1);
                    return;

                case 2:
                    this.Emit(OpCodes.Ldc_I4_2);
                    return;

                case 3:
                    this.Emit(OpCodes.Ldc_I4_3);
                    return;

                case 4:
                    this.Emit(OpCodes.Ldc_I4_4);
                    return;

                case 5:
                    this.Emit(OpCodes.Ldc_I4_5);
                    return;

                case 6:
                    this.Emit(OpCodes.Ldc_I4_6);
                    return;

                case 7:
                    this.Emit(OpCodes.Ldc_I4_7);
                    return;

                case 8:
                    this.Emit(OpCodes.Ldc_I4_8);
                    return;
            }
            if ((value >= -128) && (value <= 0x7f))
            {
                this.il.Emit(OpCodes.Ldc_I4_S, (sbyte) value);
            }
            else
            {
                this.il.Emit(OpCodes.Ldc_I4, value);
            }
        }

        public void LoadValue(long value)
        {
            this.il.Emit(OpCodes.Ldc_I8, value);
        }

        public void LoadValue(FieldInfo field)
        {
            this.CheckAccessibility(field);
            OpCode opcode = field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld;
            this.il.Emit(opcode, field);
        }

        public void LoadValue(PropertyInfo property)
        {
            this.CheckAccessibility(property);
            this.EmitCall(Helpers.GetGetMethod(property, true, true));
        }

        public void LoadValue(float value)
        {
            this.il.Emit(OpCodes.Ldc_R4, value);
        }

        public void LoadValue(string value)
        {
            if (value == null)
            {
                this.LoadNullRef();
            }
            else
            {
                this.il.Emit(OpCodes.Ldstr, value);
            }
        }

        internal void LoadValue(Type type)
        {
            this.il.Emit(OpCodes.Ldtoken, type);
            this.EmitCall(this.MapType(typeof(Type)).GetMethod("GetTypeFromHandle"));
        }

        internal static void LoadValue(ILGenerator il, int value)
        {
            switch (value)
            {
                case -1:
                    il.Emit(OpCodes.Ldc_I4_M1);
                    return;

                case 0:
                    il.Emit(OpCodes.Ldc_I4_0);
                    return;

                case 1:
                    il.Emit(OpCodes.Ldc_I4_1);
                    return;

                case 2:
                    il.Emit(OpCodes.Ldc_I4_2);
                    return;

                case 3:
                    il.Emit(OpCodes.Ldc_I4_3);
                    return;

                case 4:
                    il.Emit(OpCodes.Ldc_I4_4);
                    return;

                case 5:
                    il.Emit(OpCodes.Ldc_I4_5);
                    return;

                case 6:
                    il.Emit(OpCodes.Ldc_I4_6);
                    return;

                case 7:
                    il.Emit(OpCodes.Ldc_I4_7);
                    return;

                case 8:
                    il.Emit(OpCodes.Ldc_I4_8);
                    return;
            }
            il.Emit(OpCodes.Ldc_I4, value);
        }

        internal int MapMetaKeyToCompiledKey(int metaKey)
        {
            if ((metaKey < 0) || (this.methodPairs == null))
            {
                return metaKey;
            }
            for (int i = 0; i < this.methodPairs.Length; i++)
            {
                if (this.methodPairs[i].MetaKey == metaKey)
                {
                    return i;
                }
            }
            throw new ArgumentException("Key could not be mapped: " + metaKey.ToString(), "metaKey");
        }

        internal Type MapType(Type type)
        {
            return this.model.MapType(type);
        }

        internal void MarkLabel(CodeLabel label)
        {
            this.il.MarkLabel(label.Value);
        }

        internal void ReadNullCheckedTail(Type type, IProtoSerializer tail, Local valueFrom)
        {
            Type type2;
            if (type.IsValueType && ((type2 = Helpers.GetUnderlyingType(type)) != null))
            {
                if (tail.RequiresOldValue)
                {
                    using (Local local = this.GetLocalWithValue(type, valueFrom))
                    {
                        this.LoadAddress(local, type);
                        this.EmitCall(type.GetMethod("GetValueOrDefault", Helpers.EmptyTypes));
                    }
                }
                tail.EmitRead(this, null);
                if (tail.ReturnsValue)
                {
                    this.EmitCtor(type, new Type[] { type2 });
                }
            }
            else
            {
                tail.EmitRead(this, valueFrom);
            }
        }

        internal void ReleaseToPool(LocalBuilder value)
        {
            int count = this.locals.Count;
            for (int i = 0; i < count; i++)
            {
                if (this.locals[i] == null)
                {
                    this.locals[i] = value;
                    return;
                }
            }
            this.locals.Add(value);
        }

        internal void Return()
        {
            this.Emit(OpCodes.Ret);
        }

        public void StoreValue(Local local)
        {
            if (local == this.InputValue)
            {
                byte arg = this.isStatic ? ((byte) 0) : ((byte) 1);
                this.il.Emit(OpCodes.Starg_S, arg);
            }
            else
            {
                switch (local.Value.LocalIndex)
                {
                    case 0:
                        this.Emit(OpCodes.Stloc_0);
                        return;

                    case 1:
                        this.Emit(OpCodes.Stloc_1);
                        return;

                    case 2:
                        this.Emit(OpCodes.Stloc_2);
                        return;

                    case 3:
                        this.Emit(OpCodes.Stloc_3);
                        return;
                }
                OpCode opcode = this.UseShortForm(local) ? OpCodes.Stloc_S : OpCodes.Stloc;
                this.il.Emit(opcode, local.Value);
            }
        }

        public void StoreValue(FieldInfo field)
        {
            this.CheckAccessibility(field);
            OpCode opcode = field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld;
            this.il.Emit(opcode, field);
        }

        public void StoreValue(PropertyInfo property)
        {
            this.CheckAccessibility(property);
            this.EmitCall(Helpers.GetSetMethod(property, true, true));
        }

        public void Subtract()
        {
            this.Emit(OpCodes.Sub);
        }

        public void Switch(CodeLabel[] jumpTable)
        {
            Label[] labels = new Label[jumpTable.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = jumpTable[i].Value;
            }
            this.il.Emit(OpCodes.Switch, labels);
        }

        internal void TryCast(Type type)
        {
            this.il.Emit(OpCodes.Isinst, type);
        }

        private bool UseShortForm(Local local)
        {
            return (local.Value.LocalIndex < 0x100);
        }

        public IDisposable Using(Local local)
        {
            return new UsingBlock(this, local);
        }

        internal void WriteNullCheckedTail(Type type, IProtoSerializer tail, Local valueFrom)
        {
            if (type.IsValueType)
            {
                if (Helpers.GetUnderlyingType(type) == null)
                {
                    tail.EmitWrite(this, valueFrom);
                    return;
                }
                using (Local local = this.GetLocalWithValue(type, valueFrom))
                {
                    this.LoadAddress(local, type);
                    this.LoadValue(type.GetProperty("HasValue"));
                    CodeLabel label = this.DefineLabel();
                    this.BranchIfFalse(label, false);
                    this.LoadAddress(local, type);
                    this.EmitCall(type.GetMethod("GetValueOrDefault", Helpers.EmptyTypes));
                    tail.EmitWrite(this, null);
                    this.MarkLabel(label);
                    return;
                }
            }
            this.LoadValue(valueFrom);
            this.CopyValue();
            CodeLabel label2 = this.DefineLabel();
            CodeLabel label3 = this.DefineLabel();
            this.BranchIfTrue(label2, true);
            this.DiscardValue();
            this.Branch(label3, false);
            this.MarkLabel(label2);
            tail.EmitWrite(this, null);
            this.MarkLabel(label3);
        }

        public Local InputValue
        {
            get
            {
                return this.inputValue;
            }
        }

        public ILVersion MetadataVersion
        {
            get
            {
                return this.metadataVersion;
            }
        }

        public TypeModel Model
        {
            get
            {
                return this.model;
            }
        }

        internal bool NonPublic
        {
            get
            {
                return this.nonPublic;
            }
        }

        public enum ILVersion
        {
            Net1,
            Net2
        }

        private sealed class UsingBlock : IDisposable
        {
            private CompilerContext ctx;
            private CodeLabel label;
            private Local local;

            public UsingBlock(CompilerContext ctx, Local local)
            {
                if (ctx == null)
                {
                    throw new ArgumentNullException("ctx");
                }
                if (local == null)
                {
                    throw new ArgumentNullException("local");
                }
                Type c = local.Type;
                if ((!c.IsValueType && !c.IsSealed) || ctx.MapType(typeof(IDisposable)).IsAssignableFrom(c))
                {
                    this.local = local;
                    this.ctx = ctx;
                    this.label = ctx.BeginTry();
                }
            }

            public void Dispose()
            {
                if ((this.local != null) && (this.ctx != null))
                {
                    this.ctx.EndTry(this.label, false);
                    this.ctx.BeginFinally();
                    Type type = this.ctx.MapType(typeof(IDisposable));
                    MethodInfo method = type.GetMethod("Dispose");
                    Type type2 = this.local.Type;
                    if (type2.IsValueType)
                    {
                        this.ctx.LoadAddress(this.local, type2);
                        if (this.ctx.MetadataVersion == CompilerContext.ILVersion.Net1)
                        {
                            this.ctx.LoadValue(this.local);
                            this.ctx.CastToObject(type2);
                        }
                        else
                        {
                            this.ctx.Constrain(type2);
                        }
                        this.ctx.EmitCall(method);
                    }
                    else
                    {
                        CodeLabel label = this.ctx.DefineLabel();
                        if (type.IsAssignableFrom(type2))
                        {
                            this.ctx.LoadValue(this.local);
                            this.ctx.BranchIfFalse(label, true);
                            this.ctx.LoadAddress(this.local, type2);
                        }
                        else
                        {
                            using (Local local = new Local(this.ctx, type))
                            {
                                this.ctx.LoadValue(this.local);
                                this.ctx.TryCast(type);
                                this.ctx.CopyValue();
                                this.ctx.StoreValue(local);
                                this.ctx.BranchIfFalse(label, true);
                                this.ctx.LoadAddress(local, type);
                            }
                        }
                        this.ctx.EmitCall(method);
                        this.ctx.MarkLabel(label);
                    }
                    this.ctx.EndFinally();
                    this.local = null;
                    this.ctx = null;
                    this.label = new CodeLabel();
                }
            }
        }
    }
}

