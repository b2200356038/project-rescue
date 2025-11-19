using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

namespace Game.Vehicle.Wheel
{
    [System.Flags]
    enum WheelVisualFlags : byte
    {
        None = 0,
        AngularVelocity = 1 << 0,  // 0x01
        SpringLength = 1 << 1,      // 0x02
        SteerAngle = 1 << 2,        // 0x04
    }

    public struct WheelVisualData : INetworkSerializable
    {
        public byte Flags;
        public short AngularVelocity;   
        public byte SpringLength;       
        public sbyte SteerAngle;         

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFlag(WheelVisualFlags flag, bool value)
        {
            if (value)
                Flags |= (byte)flag;
            else
                Flags &= (byte)~flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool HasFlag(WheelVisualFlags flag)
        {
            return (Flags & (byte)flag) != 0;
        }

        public void SetAngularVelocity(float value)
        {
            AngularVelocity = (short)Mathf.Clamp(value * 100f, -32767, 32767);
            SetFlag(WheelVisualFlags.AngularVelocity, true);
        }

        public float GetAngularVelocity()
        {
            return AngularVelocity / 100f;
        }

        public void SetSpringLength(float value)
        {
            SpringLength = (byte)Mathf.Clamp(value * 100f, 0, 255);
            SetFlag(WheelVisualFlags.SpringLength, true);
        }

        public float GetSpringLength()
        {
            return SpringLength / 100f;
        }

        public void SetSteerAngle(float value)
        {
            SteerAngle = (sbyte)Mathf.Clamp(value, -127, 127);
            SetFlag(WheelVisualFlags.SteerAngle, true);
        }

        public float GetSteerAngle()
        {
            return SteerAngle;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Flags);

            if (HasFlag(WheelVisualFlags.AngularVelocity))
            {
                serializer.SerializeValue(ref AngularVelocity);
            }

            if (HasFlag(WheelVisualFlags.SpringLength))
            {
                serializer.SerializeValue(ref SpringLength);
            }

            if (HasFlag(WheelVisualFlags.SteerAngle))
            {
                serializer.SerializeValue(ref SteerAngle);
            }
        }
    }
}