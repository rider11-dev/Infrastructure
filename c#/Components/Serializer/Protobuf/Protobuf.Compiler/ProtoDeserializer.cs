namespace Components.Serialize.Protobuf.Compiler
{
    using Components.Serialize.Protobuf.Protobuf;

    using System;
    using System.Runtime.CompilerServices;

    internal delegate object ProtoDeserializer(object value, ProtoReader source);
}

