namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Meta
{
    using System;
    using System.Reflection;

    internal sealed class MutableList : BasicList
    {
        public void RemoveLast()
        {
            base.head.RemoveLastWithMutate();
        }

        public new object this[int index]
        {
            get
            {
                return base.head[index];
            }
            set
            {
                base.head[index] = value;
            }
        }
    }
}

