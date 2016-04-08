namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Serializers
{
    internal interface ISerializerProxy
    {
        IProtoSerializer Serializer { get; }
    }
}

