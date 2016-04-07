namespace Components.Serialize.Protobuf.Serializers
{
    using Components.Serialize.Protobuf.Compiler;
    using Components.Serialize.Protobuf.Meta;
    using Components.Serialize.Protobuf.Protobuf;
    using System;

    internal interface IProtoTypeSerializer : IProtoSerializer
    {
        void Callback(object value, TypeModel.CallbackType callbackType, SerializationContext context);
        bool CanCreateInstance();
        object CreateInstance(ProtoReader source);
        void EmitCallback(CompilerContext ctx, Local valueFrom, TypeModel.CallbackType callbackType);
        void EmitCreateInstance(CompilerContext ctx);
        bool HasCallbacks(TypeModel.CallbackType callbackType);
    }
}

