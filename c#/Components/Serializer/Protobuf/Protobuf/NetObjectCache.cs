namespace ZPF.Infrastructure.Components.Serialize.Protobuf.Protobuf
{
    using ZPF.Infrastructure.Components.Serialize.Protobuf.Meta;
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    internal sealed class NetObjectCache
    {
        private Dictionary<object, int> objectKeys;
        internal const int Root = 0;
        private object rootObject;
        private Dictionary<string, int> stringKeys;
        private int trapStartIndex;
        private MutableList underlyingList;

        internal int AddObjectKey(object value, out bool existing)
        {
            int num;
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }
            if (value == this.rootObject)
            {
                existing = true;
                return 0;
            }
            string key = value as string;
            BasicList list = this.List;
            if (key == null)
            {
                if (this.objectKeys == null)
                {
                    this.objectKeys = new Dictionary<object, int>(ReferenceComparer.Default);
                    num = -1;
                }
                else if (!this.objectKeys.TryGetValue(value, out num))
                {
                    num = -1;
                }
            }
            else if (this.stringKeys == null)
            {
                this.stringKeys = new Dictionary<string, int>();
                num = -1;
            }
            else if (!this.stringKeys.TryGetValue(key, out num))
            {
                num = -1;
            }
            if (!(existing = num >= 0))
            {
                num = list.Add(value);
                if (key == null)
                {
                    this.objectKeys.Add(value, num);
                }
                else
                {
                    this.stringKeys.Add(key, num);
                }
            }
            return (num + 1);
        }

        internal object GetKeyedObject(int key)
        {
            if (key-- == 0)
            {
                if (this.rootObject == null)
                {
                    throw new ProtoException("No root object assigned");
                }
                return this.rootObject;
            }
            BasicList list = this.List;
            if ((key < 0) || (key >= list.Count))
            {
                throw new ProtoException("Internal error; a missing key occurred");
            }
            object obj2 = list[key];
            if (obj2 == null)
            {
                throw new ProtoException("A deferred key does not have a value yet");
            }
            return obj2;
        }

        internal void RegisterTrappedObject(object value)
        {
            if (this.rootObject == null)
            {
                this.rootObject = value;
            }
            else if (this.underlyingList != null)
            {
                for (int i = this.trapStartIndex; i < this.underlyingList.Count; i++)
                {
                    this.trapStartIndex = i + 1;
                    if (this.underlyingList[i] == null)
                    {
                        this.underlyingList[i] = value;
                        return;
                    }
                }
            }
        }

        internal void SetKeyedObject(int key, object value)
        {
            if (key-- == 0)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                if ((this.rootObject != null) && (this.rootObject != value))
                {
                    throw new ProtoException("The root object cannot be reassigned");
                }
                this.rootObject = value;
            }
            else
            {
                MutableList list = this.List;
                if (key < list.Count)
                {
                    object objA = list[key];
                    if (objA == null)
                    {
                        list[key] = value;
                    }
                    else if (!object.ReferenceEquals(objA, value))
                    {
                        throw new ProtoException("Reference-tracked objects cannot change reference");
                    }
                }
                else if (key != list.Add(value))
                {
                    throw new ProtoException("Internal error; a key mismatch occurred");
                }
            }
        }

        private MutableList List
        {
            get
            {
                if (this.underlyingList == null)
                {
                    this.underlyingList = new MutableList();
                }
                return this.underlyingList;
            }
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly NetObjectCache.ReferenceComparer Default = new NetObjectCache.ReferenceComparer();

            private ReferenceComparer()
            {
            }

            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return (x == y);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}

