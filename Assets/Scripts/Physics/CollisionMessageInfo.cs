using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace Game.Physics
{
    public enum CollisionType
    {
        Player = 0x01,
        DebugCollision = 0x0F,
    }

    enum CollisionCategoryFlags
    {
        Standard = 0x10,
        CollisionForce = 0x20,
        CollisionPoint = 0x40,
    }


    public struct CollisionMessageInfo : INetworkSerializable
    {
        public byte Flags;
        public ushort Damage;
        public Vector3 CollisionForce;
        public Vector3 CollisionPoint;
        public ulong SourceOwner;
        public ulong TargetOwner;

        public CollisionType GetCollisionType()
        {
            return (CollisionType)(Flags & 0x0F);
        }

        public bool HasCollisionForce()
        {
            return (Flags & (uint)CollisionCategoryFlags.CollisionForce) == (byte)CollisionCategoryFlags.CollisionForce;
        }

        

        public void SetFlag(bool set, uint flag)
        {
            var flags = (uint)Flags;
            if (set)
            {
                flags = flags | flag;
            }
            else
            {
                flags = flags & ~flag;
            }

            Flags = (byte)flags;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool GetFlag(uint flag)
        {
            var flags = (uint)Flags;
            return (flags & flag) != 0;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Flags);
            serializer.SerializeValue(ref Damage);

            if (GetFlag((uint)CollisionCategoryFlags.CollisionForce))
            {
                serializer.SerializeValue(ref CollisionForce);
            }
            if (GetFlag((uint)CollisionCategoryFlags.CollisionPoint))
            {
                serializer.SerializeValue(ref CollisionForce);
            }
        }
    }
}