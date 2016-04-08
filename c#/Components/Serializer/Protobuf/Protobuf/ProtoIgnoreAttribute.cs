namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf
{
    using System;

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public class ProtoIgnoreAttribute : Attribute
    {
    }
}

